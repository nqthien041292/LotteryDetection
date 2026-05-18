using System.Threading.Channels;

namespace LotteryDetectionMobile.Tests.Voice;

/// <summary>
///     Extracted chunk sender logic for testing.
///     This mirrors HybridVoiceService.SendAudioChunksAsync logic.
/// </summary>
public class ChunkSenderLogic
{
    private readonly int _maxChunkFailures;
    private int _consecutiveChunkFailures;
    private volatile bool _realtimeStreamingActive;

    public ChunkSenderLogic(int maxChunkFailures = 5)
    {
        _maxChunkFailures = maxChunkFailures;
    }

    public bool IsStreamingRealtime => _realtimeStreamingActive;
    public int ConsecutiveFailures => _consecutiveChunkFailures;
    public int TotalChunksSent { get; private set; }
    public bool FallbackTriggered { get; private set; }

    public event Action? OnStreamingFallback;

    /// <summary>
    ///     Processes chunks from the reader and sends via the sender function.
    ///     Returns when all chunks are processed or fallback is triggered.
    /// </summary>
    public async Task ProcessChunksAsync(
        ChannelReader<byte[]> reader,
        Func<byte[], CancellationToken, Task> sender,
        CancellationToken ct)
    {
        _realtimeStreamingActive = true;
        Interlocked.Exchange(ref _consecutiveChunkFailures, 0);
        TotalChunksSent = 0;
        FallbackTriggered = false;

        try
        {
            await foreach (var chunk in reader.ReadAllAsync(ct))
            {
                if (!_realtimeStreamingActive || ct.IsCancellationRequested) break;

                try
                {
                    await sender(chunk, ct);
                    Interlocked.Exchange(ref _consecutiveChunkFailures, 0);
                    TotalChunksSent++;
                }
                catch (Exception)
                {
                    var failures = Interlocked.Increment(ref _consecutiveChunkFailures);

                    if (failures >= _maxChunkFailures)
                    {
                        _realtimeStreamingActive = false;
                        FallbackTriggered = true;

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
    }

    /// <summary>
    ///     Stops streaming.
    /// </summary>
    public void Stop()
    {
        _realtimeStreamingActive = false;
    }
}