using LotteryDetectionMobile.Services.Configuration;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;

namespace LotteryDetectionMobile.Services.Voice;

/// <summary>
///     SignalR client for real-time voice processing updates.
/// </summary>
public class SignalRVoiceClient : IAsyncDisposable
{
    private readonly HubConnection _connection;
    private readonly ILogger<SignalRVoiceClient>? _logger;
    private readonly Func<Task<string?>>? _tokenProvider;
    private Guid? _activeRealtimeSessionId;
    private Guid? _currentSessionId;

    /// <summary>
    ///     Creates SignalR client with VoiceApiOptions (uses async token provider).
    /// </summary>
    public SignalRVoiceClient(VoiceApiOptions options, ILogger<SignalRVoiceClient>? logger = null)
        : this(options.BaseUrl, options.GetBearerTokenAsync, logger)
    {
    }

    /// <summary>
    ///     Creates SignalR client with async token provider for automatic refresh.
    /// </summary>
    public SignalRVoiceClient(string baseUrl, Func<Task<string?>>? tokenProvider,
        ILogger<SignalRVoiceClient>? logger = null)
    {
        _logger = logger;
        _tokenProvider = tokenProvider;

        var hubUrl = $"{baseUrl.TrimEnd('/')}/hubs/voice";

        var builder = new HubConnectionBuilder()
            .WithUrl(hubUrl, httpOptions =>
            {
                // Use async token provider for automatic refresh on connect/reconnect
                if (_tokenProvider != null) httpOptions.AccessTokenProvider = async () => await _tokenProvider();
#if DEBUG
                // Trust the LotteryDetection.Web.Host dev cert (https://localhost:44301).
                httpOptions.HttpMessageHandlerFactory = _ => DevHttpsHelper.CreateHandler(true);
                httpOptions.WebSocketConfiguration = ws =>
                    ws.RemoteCertificateValidationCallback = (_, _, _, _) => true;
#endif
            })
            .WithAutomaticReconnect(new[]
                { TimeSpan.Zero, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10) });

        _connection = builder.Build();

        RegisterHandlers();
        RegisterConnectionEvents();
    }

    public string? ConnectionId { get; private set; }
    public bool IsConnected => _connection.State == HubConnectionState.Connected;
    public HubConnectionState State => _connection.State;

    public async ValueTask DisposeAsync()
    {
        await _connection.DisposeAsync();
    }

    // Events for ViewModel binding
    public event Action<Guid, string, bool>? OnTranscriptChunk;
    public event Action<Guid, string, string?>? OnStatusChanged;
    public event Action<Guid, string, object, double>? OnIntentExtracted;
    public event Action<Guid, string, string>? OnTaskCreated;
    public event Action<Guid, string, string>? OnError;
    public event Action<HubConnectionState>? OnConnectionStateChanged;
    public event Action<VoiceErrorType, string>? OnCriticalError;

    private void RegisterHandlers()
    {
        _connection.On<Guid, string, bool>("OnTranscriptChunk", (sessionId, text, isFinal) =>
        {
            _logger?.LogDebug("SignalR: Transcript chunk received - {Text}", text);
            OnTranscriptChunk?.Invoke(sessionId, text, isFinal);
        });

        _connection.On<Guid, string, string?>("OnStatusChanged", (sessionId, status, transcript) =>
        {
            _logger?.LogInformation("SignalR: Status changed - {SessionId} -> {Status}", sessionId, status);
            OnStatusChanged?.Invoke(sessionId, status, transcript);
        });

        _connection.On<Guid, string, object, double>("OnIntentExtracted", (sessionId, intent, entities, confidence) =>
        {
            _logger?.LogInformation("SignalR: Intent extracted - {Intent} ({Confidence:P0})", intent, confidence);
            OnIntentExtracted?.Invoke(sessionId, intent, entities, confidence);
        });

        _connection.On<Guid, string, string>("OnTaskCreated", (taskId, title, intent) =>
        {
            _logger?.LogInformation("SignalR: Task created - {TaskId} - {Title}", taskId, title);
            OnTaskCreated?.Invoke(taskId, title, intent);
        });

        _connection.On<Guid, string, string>("OnError", (sessionId, errorCode, message) =>
        {
            _logger?.LogError("SignalR: Voice error - {ErrorCode}: {Message}", errorCode, message);
            OnError?.Invoke(sessionId, errorCode, message);
        });

        _connection.On<string, DateTime>("OnConnected", (connectionId, serverTime) =>
        {
            ConnectionId = connectionId;
            _logger?.LogInformation("SignalR: Connected with ID {ConnectionId}", connectionId);
        });
    }

    private void RegisterConnectionEvents()
    {
        _connection.Reconnecting += error =>
        {
            _logger?.LogWarning(error, "SignalR: Reconnecting...");
            OnConnectionStateChanged?.Invoke(HubConnectionState.Reconnecting);
            return Task.CompletedTask;
        };

        _connection.Reconnected += async connectionId =>
        {
            ConnectionId = connectionId;
            _logger?.LogInformation("SignalR: Reconnected with ID {ConnectionId}", connectionId);

            // Rejoin the current session after reconnection
            if (_currentSessionId.HasValue)
                try
                {
                    await _connection.InvokeAsync("JoinSession", _currentSessionId.Value);
                    _logger?.LogInformation("SignalR: Rejoined session {SessionId} after reconnect",
                        _currentSessionId.Value);
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "SignalR: Failed to rejoin session after reconnect");
                }

            // Resume realtime session if was active
            if (_activeRealtimeSessionId.HasValue)
                try
                {
                    await _connection.InvokeAsync("StartRealtimeSession", _activeRealtimeSessionId.Value);
                    _logger?.LogInformation("SignalR: Resumed realtime session {SessionId} after reconnect",
                        _activeRealtimeSessionId.Value);
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "SignalR: Failed to resume realtime session after reconnect");
                    _activeRealtimeSessionId = null;
                }

            OnConnectionStateChanged?.Invoke(HubConnectionState.Connected);
        };

        _connection.Closed += error =>
        {
            _logger?.LogWarning(error, "SignalR: Disconnected");
            ConnectionId = null;
            OnConnectionStateChanged?.Invoke(HubConnectionState.Disconnected);
            return Task.CompletedTask;
        };
    }

    public async Task ConnectAsync(CancellationToken ct = default)
    {
        if (_connection.State != HubConnectionState.Disconnected)
        {
            _logger?.LogDebug("SignalR: Already connected or connecting");
            return;
        }

        try
        {
            _logger?.LogInformation("SignalR: Connecting to voice hub...");
            await _connection.StartAsync(ct);
            OnConnectionStateChanged?.Invoke(HubConnectionState.Connected);
            _logger?.LogInformation("SignalR: Connected successfully");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "SignalR: Failed to connect");
            throw;
        }
    }

    public async Task JoinSessionAsync(Guid sessionId, CancellationToken ct = default)
    {
        if (_connection.State != HubConnectionState.Connected)
        {
            _logger?.LogWarning("SignalR: Cannot join session - not connected");
            return;
        }

        await _connection.InvokeAsync("JoinSession", sessionId, ct);
        _currentSessionId = sessionId;
        _logger?.LogDebug("SignalR: Joined session {SessionId}", sessionId);
    }

    public async Task LeaveSessionAsync(Guid sessionId, CancellationToken ct = default)
    {
        if (_connection.State != HubConnectionState.Connected)
        {
            _currentSessionId = null;
            return;
        }

        await _connection.InvokeAsync("LeaveSession", sessionId, ct);
        _currentSessionId = null;
        _logger?.LogDebug("SignalR: Left session {SessionId}", sessionId);
    }

    /// <summary>
    ///     Starts a real-time streaming session on the backend.
    /// </summary>
    public async Task StartRealtimeSessionAsync(Guid sessionId, CancellationToken ct = default)
    {
        if (_connection.State != HubConnectionState.Connected)
        {
            _logger?.LogWarning("SignalR: Cannot start realtime session - not connected");
            OnCriticalError?.Invoke(VoiceErrorType.Connection, "Not connected to server");
            return;
        }

        try
        {
            await _connection.InvokeAsync("StartRealtimeSession", sessionId, ct);
            _activeRealtimeSessionId = sessionId;
            _logger?.LogDebug("SignalR: Started realtime session {SessionId}", sessionId);
        }
        catch (Exception ex) when (ex.GetType().Name == "HubException")
        {
            _logger?.LogError(ex, "SignalR: Backend rejected realtime session");
            OnCriticalError?.Invoke(VoiceErrorType.SessionStart, ex.Message);
            throw;
        }
    }

    /// <summary>
    ///     Streams a PCM audio chunk to the backend. Uses fire-and-forget for low latency.
    /// </summary>
    public async Task StreamAudioChunkAsync(Guid sessionId, byte[] pcmData, CancellationToken ct = default)
    {
        if (_connection.State != HubConnectionState.Connected) return;

        // SendAsync is fire-and-forget - no response wait, lower latency
        await _connection.SendAsync("StreamAudioChunk", sessionId, pcmData, ct);
    }

    /// <summary>
    ///     Stops the real-time streaming session on the backend.
    /// </summary>
    public async Task StopRealtimeSessionAsync(Guid sessionId, CancellationToken ct = default)
    {
        _activeRealtimeSessionId = null;

        if (_connection.State != HubConnectionState.Connected)
        {
            _logger?.LogWarning("SignalR: Cannot stop realtime session - not connected");
            return;
        }

        await _connection.InvokeAsync("StopRealtimeSession", sessionId, ct);
        _logger?.LogDebug("SignalR: Stopped realtime session {SessionId}", sessionId);
    }
}