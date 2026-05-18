using System.Runtime.CompilerServices;
using System.Threading.Channels;
using LotteryDetectionMobile.Models.Voice;
using LotteryDetectionMobile.Services.Auth;
using LotteryDetectionMobile.Services.Interfaces;
using LotteryDetectionMobile.Services.Logging;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;

namespace LotteryDetectionMobile.Services.Voice;

/// <summary>
///     Hybrid voice service: records audio, uploads to server, and receives real-time updates via SignalR.
///     Supports parallel file recording (backup) and real-time PCM streaming.
/// </summary>
public class HybridVoiceService : IVoiceService, IAsyncDisposable
{
    private readonly ILogger<HybridVoiceService>? _logger;
    private readonly VoiceApiOptions _options;
    private readonly IPlatformAudioRecorder? _recorder;
    private readonly SignalRVoiceClient _signalRClient;
    private readonly object _streamingLock = new();
    private readonly IStreamingAudioRecorder? _streamingRecorder;
    private readonly VoiceUploadApiClient _uploadClient;
    private Task? _chunkSenderTask;
    private int _consecutiveChunkFailures; // Use Interlocked for atomic operations

    private Guid? _currentSessionId;

    // Fallback tracking (volatile for thread-safe read/write)
    private volatile bool _realtimeStreamingActive;
    private CancellationTokenSource? _streamingCts;
    private Channel<(string Text, bool IsFinal)>? _transcriptChannel;

    public HybridVoiceService(
        VoiceApiOptions? options = null,
        ILogger<HybridVoiceService>? logger = null,
        IStreamingAudioRecorder? streamingRecorder = null,
        IPlatformAudioRecorder? recorder = null)
    {
        _options = options ?? new VoiceApiOptions();
        _logger = logger;
        _streamingRecorder = streamingRecorder;
        _recorder = recorder;
        _uploadClient = new VoiceUploadApiClient(null, _options);
        _signalRClient = new SignalRVoiceClient(_options);

        Console.WriteLine(
            $"[HybridVoiceService] Created with streamingRecorder: {(streamingRecorder != null ? streamingRecorder.GetType().Name : "null")}");

        // Bubble up amplitude events for waveform visualization
        if (_streamingRecorder != null)
            _streamingRecorder.OnAmplitudeChanged += amp => OnAmplitudeChanged?.Invoke(amp);

        // Wire up SignalR events
        _signalRClient.OnTranscriptChunk += HandleTranscriptChunk;
        _signalRClient.OnStatusChanged += (s, st, t) => OnStatusChanged?.Invoke(s, st, t);
        _signalRClient.OnIntentExtracted += (s, i, e, c) => OnIntentExtracted?.Invoke(s, i, e, c);
        _signalRClient.OnTaskCreated += (t, ti, i) => OnTaskCreated?.Invoke(t, ti, i);
        _signalRClient.OnError += (s, c, m) => OnError?.Invoke(s, c, m);
        _signalRClient.OnConnectionStateChanged += state => OnConnectionStateChanged?.Invoke(state);
        _signalRClient.OnCriticalError += (type, message) => OnCriticalError?.Invoke(type.ToString(), message);
    }

    public bool IsConnected => _signalRClient.IsConnected;
    public string? ConnectionId => _signalRClient.ConnectionId;

    /// <summary>
    ///     Whether real-time streaming is currently active.
    /// </summary>
    public bool IsStreamingRealtime => _realtimeStreamingActive;

    public async ValueTask DisposeAsync()
    {
        // Cleanup streaming resources
        lock (_streamingLock)
        {
            _streamingCts?.Cancel();
            _streamingCts?.Dispose();
            _streamingCts = null;
        }

        // Dispose streaming recorder if it implements IDisposable
        if (_streamingRecorder is IDisposable disposable) disposable.Dispose();

        if (_currentSessionId.HasValue && _signalRClient.IsConnected)
            try
            {
                await _signalRClient.LeaveSessionAsync(_currentSessionId.Value);
            }
            catch
            {
                // Ignore errors during cleanup
            }

        await _signalRClient.DisposeAsync();
    }

    public string? LastRecordingPath =>
#if ANDROID
        // Android: streaming recorder owns the file (single mic consumer eliminates contention)
        _streamingRecorder?.OutputFilePath ?? _recorder?.LatestFilePath;
#else
        _recorder?.LatestFilePath;
#endif
    public Guid? LastVoiceTaskId { get; private set; }

    public async Task<Guid> StartAsync(CancellationToken cancellationToken)
    {
        var log = RemoteLogService.Instance;
        log.Info("HybridVoice", "StartAsync - beginning");

        // Try to connect SignalR first for real-time updates
        await EnsureConnectedAsync(cancellationToken);
        log.Info("HybridVoice", $"SignalR connected: {_signalRClient.IsConnected}");

        // Start upload session
        log.Info("HybridVoice", "Calling _uploadClient.StartAsync...");
        var result = await _uploadClient.StartAsync(cancellationToken);
        _currentSessionId = result.SessionId;
        log.Info("HybridVoice", $"Session created: {result.SessionId}");

        // Create new channel for transcript streaming
        _transcriptChannel = Channel.CreateUnbounded<(string, bool)>();

        // Join SignalR session group for updates
        if (_signalRClient.IsConnected)
            try
            {
                await _signalRClient.JoinSessionAsync(result.SessionId, cancellationToken);
                log.Info("HybridVoice", "Joined SignalR session group");
            }
            catch (Exception ex)
            {
                log.Error("HybridVoice", $"Failed to join SignalR session: {ex.Message}", ex);
                _logger?.LogWarning(ex, "Failed to join SignalR session");
            }

        return result.SessionId;
    }

    public async Task StartRecordingAsync(CancellationToken cancellationToken)
    {
        var log = RemoteLogService.Instance;
        var folder = FileSystem.AppDataDirectory;
        log.Info("HybridVoice", $"StartRecordingAsync - folder: {folder}");

        // Android: only one AudioSource.Mic consumer is allowed. The streaming recorder
        // owns the mic and writes the WAV file directly from PCM chunks. The MediaRecorder-based
        // _recorder is only used as a fallback when streaming is unavailable.
        // iOS: AVAudioSession lets both recorders cooperate, so file recording starts unconditionally.
#if ANDROID
        var streamingWillOwnMic = _streamingRecorder != null && _currentSessionId.HasValue;
        if (!streamingWillOwnMic && _recorder != null)
        {
            await _recorder.StartAsync(folder, cancellationToken);
            log.Info("HybridVoice", $"File recording started (Android fallback): {_recorder.LatestFilePath}");
        }
#else
        if (_recorder != null)
        {
            await _recorder.StartAsync(folder, cancellationToken);
            log.Info("HybridVoice", $"File recording started: {_recorder.LatestFilePath}");
        }
#endif

        // Start real-time streaming if available and session exists
        if (_streamingRecorder != null && _currentSessionId.HasValue)
            try
            {
                CancellationTokenSource cts;
                lock (_streamingLock)
                {
                    _streamingCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    cts = _streamingCts;
                }

                await _streamingRecorder.StartAsync(folder, cts.Token);
                log.Info("HybridVoice",
                    $"Streaming recorder started. OutputFilePath={_streamingRecorder.OutputFilePath ?? "(null)"}");

                // Start realtime session on backend
                if (_signalRClient.IsConnected)
                {
                    await _signalRClient.StartRealtimeSessionAsync(_currentSessionId.Value, cancellationToken);
                    log.Info("HybridVoice", "Realtime session started");
                }

                // Spawn chunk sender task
                lock (_streamingLock)
                {
                    _chunkSenderTask = SendAudioChunksAsync(_currentSessionId.Value, cts.Token);
                }
            }
            catch (Exception ex)
            {
                log.Error("HybridVoice", $"Streaming failed: {ex.Message}", ex);
                _logger?.LogWarning(ex, "Failed to start streaming - continuing with file recording only");

#if ANDROID
                // Android fallback: if streaming failed and we skipped MediaRecorder above, start it now
                if (streamingWillOwnMic && _recorder != null)
                {
                    try
                    {
                        await _recorder.StartAsync(folder, cancellationToken);
                        log.Info("HybridVoice",
                            $"Started MediaRecorder fallback after streaming failure: {_recorder.LatestFilePath}");
                    }
                    catch (Exception fallbackEx)
                    {
                        log.Error("HybridVoice", $"MediaRecorder fallback failed: {fallbackEx.Message}", fallbackEx);
                    }
                }
#endif
            }
        else
            log.Info("HybridVoice",
                $"No streaming recorder or session. streamingRecorder={_streamingRecorder != null}, sessionId={_currentSessionId}");
    }

    public async Task StopRecordingAsync()
    {
        var log = RemoteLogService.Instance;
        log.Info("HybridVoice", "StopRecordingAsync - beginning");

        // Get references under lock to avoid race conditions
        CancellationTokenSource? cts;
        Task? senderTask;
        lock (_streamingLock)
        {
            cts = _streamingCts;
            senderTask = _chunkSenderTask;
        }

        // Stop streaming first
        cts?.Cancel();

        if (_streamingRecorder != null)
            try
            {
                await _streamingRecorder.StopAsync();
                log.Info("HybridVoice", "Streaming recorder stopped");
            }
            catch (Exception ex)
            {
                log.Error("HybridVoice", $"Streaming stop error: {ex.Message}", ex);
            }

        // Stop realtime session on backend
        if (_currentSessionId.HasValue && _signalRClient.IsConnected)
            try
            {
                await _signalRClient.StopRealtimeSessionAsync(_currentSessionId.Value);
                log.Info("HybridVoice", "Realtime session stopped on backend");
            }
            catch (Exception ex)
            {
                log.Error("HybridVoice", $"Realtime session stop error: {ex.Message}", ex);
            }

        // Wait for chunk sender to finish
        if (senderTask != null)
            try
            {
                await senderTask.WaitAsync(TimeSpan.FromSeconds(2));
            }
            catch (TimeoutException)
            {
                log.Warn("HybridVoice", "Chunk sender timeout, forcing completion");
            }
            catch (OperationCanceledException)
            {
                // Expected
            }

        // Stop file recording (must happen while audio session is still active)
        if (_recorder != null)
        {
            await _recorder.StopAsync();
            log.Info("HybridVoice", $"Recording stopped, path: {_recorder.LatestFilePath}");
        }

        // Cleanup under lock
        lock (_streamingLock)
        {
            _streamingCts?.Dispose();
            _streamingCts = null;
            _chunkSenderTask = null;
        }

        // Complete and reset transcript channel
        _transcriptChannel?.Writer.TryComplete();
        _transcriptChannel = null;
        log.Info("HybridVoice", "Transcript channel reset");
    }

    public Task UploadChunkAsync(Guid sessionId, Stream stream, string contentType, CancellationToken cancellationToken)
    {
        RemoteLogService.Instance.Info("HybridVoice",
            $"UploadChunkAsync - sessionId: {sessionId}, contentType: {contentType}");
        return _uploadClient.UploadChunkAsync(sessionId, stream, contentType, cancellationToken);
    }

    public async Task<string> CompleteAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        var log = RemoteLogService.Instance;
        log.Info("HybridVoice", $"CompleteAsync - sessionId: {sessionId}");
        // Pass connectionId so backend can send targeted SignalR updates
        var connectionId = _signalRClient.ConnectionId;
        log.Info("HybridVoice", $"CompleteAsync - connectionId: {connectionId}");

        var result = await _uploadClient.CompleteAsync(sessionId, connectionId, cancellationToken);
        LastVoiceTaskId = result.VoiceTaskId;
        log.Info("HybridVoice", $"CompleteAsync - filePath: {result.FilePath}, voiceTaskId: {result.VoiceTaskId}");
        return result.FilePath;
    }

    public async Task<ConfirmTaskResult> ConfirmTaskAsync(Guid taskId, CancellationToken cancellationToken)
    {
        var log = RemoteLogService.Instance;
        log.Info("HybridVoice", $"ConfirmTaskAsync - taskId: {taskId}");
        return await _uploadClient.ConfirmTaskAsync(taskId, cancellationToken);
    }

    public async Task<VoiceTaskPreview?> GetPreviewAsync(Guid taskId, CancellationToken cancellationToken)
    {
        return await _uploadClient.GetPreviewAsync(taskId, cancellationToken);
    }

    public async IAsyncEnumerable<(string Text, bool IsFinal)> StreamTranscriptAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (_transcriptChannel == null) yield break;

        // Read from channel that receives SignalR chunks
        await foreach (var chunk in _transcriptChannel.Reader.ReadAllAsync(cancellationToken)) yield return chunk;
    }

    // Events proxied from SignalR for ViewModel binding
    public event Action<Guid, string, string?>? OnStatusChanged;
    public event Action<Guid, string, object, double>? OnIntentExtracted;
    public event Action<Guid, string, string>? OnTaskCreated;
    public event Action<Guid, string, string>? OnError;
    public event Action<HubConnectionState>? OnConnectionStateChanged;

    /// <summary>
    ///     Fired when real-time streaming fails and falls back to file upload mode.
    /// </summary>
    public event Action? OnStreamingFallback;

    /// <summary>
    ///     Fired when a critical error occurs that needs user attention.
    /// </summary>
    public event Action<string, string>? OnCriticalError;

    /// <summary>
    ///     Fired with normalized RMS amplitude (0..1) for each captured audio chunk.
    /// </summary>
    public event Action<double>? OnAmplitudeChanged;

    /// <summary>
    ///     Creates VoiceApiOptions with auth token provider from DI container.
    ///     Used by ViewModels that need to create API clients without full DI access.
    /// </summary>
    public static VoiceApiOptions CreateVoiceApiOptions()
    {
        var authService = GetAuthService();
        return new VoiceApiOptions(authService);
    }

    /// <summary>
    ///     Gets the auth service from DI or creates a default instance.
    /// </summary>
    private static IAuthService GetAuthService()
    {
        var services = IPlatformApplication.Current?.Services;
        return services?.GetService<IAuthService>() ?? new EntraIdAuthService();
    }

    /// <summary>
    ///     Ensures SignalR connection is established.
    /// </summary>
    public async Task EnsureConnectedAsync(CancellationToken ct = default)
    {
        if (!_signalRClient.IsConnected)
            try
            {
                await _signalRClient.ConnectAsync(ct);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to connect to SignalR - continuing without real-time updates");
            }
    }

    private async Task SendAudioChunksAsync(Guid sessionId, CancellationToken ct)
    {
        if (_streamingRecorder == null) return;

        Console.WriteLine("[HybridVoiceService] SendAudioChunksAsync - starting chunk sender");
        _realtimeStreamingActive = true;
        Interlocked.Exchange(ref _consecutiveChunkFailures, 0);
        var chunkCount = 0;
        var maxFailures = _options.MaxChunkFailures;

        try
        {
            await foreach (var chunk in _streamingRecorder.AudioChunks.ReadAllAsync(ct))
            {
                if (!_realtimeStreamingActive || ct.IsCancellationRequested) break;

                try
                {
                    await _signalRClient.StreamAudioChunkAsync(sessionId, chunk, ct);
                    Interlocked.Exchange(ref _consecutiveChunkFailures, 0); // Reset on success
                    chunkCount++;

                    // Log periodically to avoid flooding
                    if (chunkCount % 100 == 0)
                        Console.WriteLine($"[HybridVoiceService] SendAudioChunksAsync - sent {chunkCount} chunks");
                }
                catch (Exception ex)
                {
                    var failures = Interlocked.Increment(ref _consecutiveChunkFailures);
                    _logger?.LogWarning(ex, "Chunk send failed ({Count}/{Max})", failures, maxFailures);

                    if (failures >= maxFailures)
                    {
                        Console.WriteLine(
                            "[HybridVoiceService] SendAudioChunksAsync - too many failures, falling back to file upload");
                        _logger?.LogError("Too many chunk failures, falling back to file upload");
                        _realtimeStreamingActive = false;

                        // Thread-safe event invocation
                        var handler = OnStreamingFallback;
                        handler?.Invoke();
                        break;
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on stop
        }

        _realtimeStreamingActive = false;
        Console.WriteLine($"[HybridVoiceService] SendAudioChunksAsync - finished, total chunks: {chunkCount}");

        if (chunkCount == 0)
            RemoteLogService.Instance.Warn("HybridVoice",
                "Zero chunks streamed — likely mic contention or silent input");
    }

    private void HandleTranscriptChunk(Guid sessionId, string text, bool isFinal)
    {
        Console.WriteLine(
            $"[HybridVoiceService] TranscriptChunk: session={sessionId}, current={_currentSessionId}, text={text}, final={isFinal}");

        // Capture local reference to avoid null race with StopRecordingAsync
        var channel = _transcriptChannel;
        if (sessionId == _currentSessionId && channel != null)
            channel.Writer.TryWrite((text, isFinal));
        // Don't complete channel on final - there may be more sentences
        // Channel is completed when recording stops
        else
            Console.WriteLine("[HybridVoiceService] TranscriptChunk ignored: sessionId mismatch or channel null");
    }
}