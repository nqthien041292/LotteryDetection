#if ANDROID
using System.Threading.Channels;
using Android.Media;

namespace LotteryDetectionMobile.Services.Voice;

/// <summary>
///     Android implementation of streaming audio recorder using AudioRecord.
///     Captures audio at 16kHz/16-bit/mono PCM for speech recognition.
/// </summary>
public class StreamingAudioRecorder : IStreamingAudioRecorder, IDisposable
{
    // Audio format: 16kHz, 16-bit, mono (matches Azure Speech SDK requirements)
    private const int SampleRate = 16000;
    private const ChannelIn ChannelConfig = ChannelIn.Mono;
    private const Encoding AudioFormat = Encoding.Pcm16bit;
    private const int ChunkDurationMs = 32;
    private const int ChunkSize = SampleRate * 2 * ChunkDurationMs / 1000; // 1024 bytes
    private AudioRecord? _audioRecord;
    private Channel<byte[]>? _channel;
    private CancellationTokenSource? _cts;
    private bool _disposed;
    private Task? _readTask;
    private PcmWavWriter? _wavWriter;

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (IsRecording)
        {
            IsRecording = false;
            _cts?.Cancel();
            Cleanup();
        }

        GC.SuppressFinalize(this);
    }

    public bool IsRecording { get; private set; }

    public string? OutputFilePath { get; private set; }

    public ChannelReader<byte[]> AudioChunks => _channel?.Reader ?? Channel.CreateBounded<byte[]>(1).Reader;

    public event Action<double>? OnAmplitudeChanged;

    public Task StartAsync(string outputFolder, CancellationToken ct)
    {
        if (IsRecording)
        {
            Console.WriteLine("[Android StreamingRecorder] Already recording");
            return Task.CompletedTask;
        }

        try
        {
            // Create bounded channel (drop oldest if consumer is slow)
            _channel = Channel.CreateBounded<byte[]>(new BoundedChannelOptions(100)
            {
                FullMode = BoundedChannelFullMode.DropOldest
            });

            // Open WAV file for single-mic-source file backup (parity with iOS file path)
            if (!string.IsNullOrWhiteSpace(outputFolder))
            {
                if (!Directory.Exists(outputFolder)) Directory.CreateDirectory(outputFolder);
                // Millisecond precision avoids collision when the user rapidly stops + restarts
                // recording within the same UTC second (FileMode.Create would silently truncate).
                var wavPath = Path.Combine(outputFolder, $"voice_{DateTime.UtcNow:yyyyMMdd_HHmmss_fff}.wav");
                _wavWriter = new PcmWavWriter(wavPath);
                OutputFilePath = wavPath;
                Console.WriteLine($"[Android StreamingRecorder] WAV writer opened: {wavPath}");
            }

            // Calculate minimum buffer size for stable recording
            var minBufferSize = AudioRecord.GetMinBufferSize(SampleRate, ChannelConfig, AudioFormat);
            if (minBufferSize <= 0)
                throw new InvalidOperationException(
                    $"Invalid buffer size: {minBufferSize}. Device may not support 16kHz recording.");

            // Use larger buffer to prevent underruns (at least 100ms)
            var bufferSize = Math.Max(minBufferSize, SampleRate * 2 / 10);

            _audioRecord = new AudioRecord(
                AudioSource.Mic,
                SampleRate,
                ChannelConfig,
                AudioFormat,
                bufferSize);

            if (_audioRecord.State != State.Initialized)
            {
                _audioRecord.Release();
                _audioRecord = null;
                throw new InvalidOperationException(
                    "AudioRecord failed to initialize. Ensure RECORD_AUDIO runtime permission is granted.");
            }

            _audioRecord.StartRecording();
            IsRecording = true;

            // Start background read loop
            _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            _readTask = Task.Run(() => ReadLoopAsync(_cts.Token), _cts.Token);

            Console.WriteLine("[Android StreamingRecorder] Started recording");
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Android StreamingRecorder] Start failed: {ex}");
            Cleanup();
            throw;
        }
    }

    public async Task StopAsync()
    {
        if (!IsRecording) return;

        Console.WriteLine("[Android StreamingRecorder] Stopping...");
        IsRecording = false;

        // Cancel read loop
        _cts?.Cancel();

        // Wait for read task to complete
        if (_readTask != null)
            try
            {
                await _readTask.WaitAsync(TimeSpan.FromSeconds(2));
            }
            catch (TimeoutException)
            {
                Console.WriteLine("[Android StreamingRecorder] Read task timeout on stop");
            }
            catch (OperationCanceledException)
            {
                // Expected
            }

        Cleanup();
        Console.WriteLine("[Android StreamingRecorder] Stopped");
    }

    private Task ReadLoopAsync(CancellationToken ct)
    {
        var buffer = new byte[ChunkSize];

        // Already on background thread (Task.Run), no need for async/await
        while (!ct.IsCancellationRequested && IsRecording && _audioRecord != null)
            try
            {
                // AudioRecord.Read is blocking but we're on background thread
                var bytesRead = _audioRecord.Read(buffer, 0, buffer.Length);

                if (bytesRead > 0 && _channel != null)
                {
                    // Copy to new array to avoid buffer reuse issues
                    var chunk = new byte[bytesRead];
                    Buffer.BlockCopy(buffer, 0, chunk, 0, bytesRead);
                    _channel.Writer.TryWrite(chunk);

                    // Tee to WAV file for single-mic-source file backup
                    _wavWriter?.Write(chunk, bytesRead);

                    var amplitude = ComputeAmplitude(buffer, bytesRead);
                    OnAmplitudeChanged?.Invoke(amplitude);
                }
                else if (bytesRead < 0)
                {
                    Console.WriteLine($"[Android StreamingRecorder] Read error code: {bytesRead}");
                    break;
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                Console.WriteLine($"[Android StreamingRecorder] Read error: {ex.Message}");
            }

        Console.WriteLine("[Android StreamingRecorder] Read loop exited");
        return Task.CompletedTask;
    }

    private static double ComputeAmplitude(byte[] buffer, int byteCount)
    {
        if (byteCount < 2) return 0;
        long sumSquares = 0;
        var sampleCount = byteCount / 2;
        for (var i = 0; i + 1 < byteCount; i += 2)
        {
            short sample = (short)(buffer[i] | (buffer[i + 1] << 8));
            sumSquares += sample * sample;
        }

        var rms = Math.Sqrt(sumSquares / (double)sampleCount) / 32767.0;
        // Speech RMS rarely exceeds ~0.3, so boost for visual range.
        return Math.Clamp(rms * 3.0, 0.0, 1.0);
    }

    private void Cleanup()
    {
        try
        {
            if (_audioRecord != null)
            {
                if (_audioRecord.RecordingState == RecordState.Recording) _audioRecord.Stop();
                _audioRecord.Release();
                _audioRecord = null;
            }

            _channel?.Writer.TryComplete();

            // Finalize WAV file (patches header sizes)
            if (_wavWriter != null)
            {
                _wavWriter.Dispose();
                _wavWriter = null;

                if (!string.IsNullOrEmpty(OutputFilePath) && File.Exists(OutputFilePath))
                {
                    var size = new FileInfo(OutputFilePath).Length;
                    Console.WriteLine($"[Android StreamingRecorder] WAV finalized: {size} bytes ({OutputFilePath})");
                }
            }

            _cts?.Dispose();
            _cts = null;
            _readTask = null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Android StreamingRecorder] Cleanup error: {ex.Message}");
        }
    }
}
#endif