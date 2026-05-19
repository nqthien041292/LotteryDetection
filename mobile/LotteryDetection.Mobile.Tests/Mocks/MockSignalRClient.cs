namespace LotteryDetection.Mobile.Tests.Mocks;

/// <summary>
///     Mock SignalR client for testing chunk transmission and failure scenarios.
/// </summary>
public class MockSignalRClient
{
    private int _failureCountdown;
    private bool _permanentFailure;

    public bool IsConnected { get; private set; } = true;

    public string? ConnectionId { get; private set; } = "test-connection-id";
    public Guid? ActiveRealtimeSessionId { get; private set; }

    /// <summary>
    ///     Number of chunks successfully sent.
    /// </summary>
    public int ChunksSent { get; private set; }

    /// <summary>
    ///     Number of failed chunk sends.
    /// </summary>
    public int ChunksFailed { get; private set; }

    /// <summary>
    ///     Configure to fail after N successful sends.
    /// </summary>
    public void FailAfterChunks(int successfulChunks)
    {
        _failureCountdown = successfulChunks;
        _permanentFailure = true;
    }

    /// <summary>
    ///     Configure to fail for N consecutive sends, then recover.
    /// </summary>
    public void FailForChunks(int failCount)
    {
        _failureCountdown = -failCount;
        _permanentFailure = false;
    }

    /// <summary>
    ///     Simulate connection disconnect.
    /// </summary>
    public void Disconnect()
    {
        IsConnected = false;
        ConnectionId = null;
    }

    /// <summary>
    ///     Simulate reconnection.
    /// </summary>
    public void Reconnect()
    {
        IsConnected = true;
        ConnectionId = "reconnected-" + Guid.NewGuid().ToString("N")[..8];
    }

    public Task StartRealtimeSessionAsync(Guid sessionId, CancellationToken ct = default)
    {
        if (!IsConnected) return Task.CompletedTask;
        ActiveRealtimeSessionId = sessionId;
        return Task.CompletedTask;
    }

    public Task StreamAudioChunkAsync(Guid sessionId, byte[] pcmData, CancellationToken ct = default)
    {
        if (!IsConnected)
        {
            ChunksFailed++;
            throw new InvalidOperationException("Not connected");
        }

        // Handle permanent failure simulation (fail after N successful)
        if (_permanentFailure && _failureCountdown > 0)
        {
            _failureCountdown--;
            ChunksSent++;
            return Task.CompletedTask;
        }

        if (_permanentFailure && _failureCountdown == 0)
        {
            ChunksFailed++;
            throw new Exception("Simulated permanent failure");
        }

        // Handle temporary failure simulation (fail for N chunks then recover)
        if (!_permanentFailure && _failureCountdown < 0)
        {
            _failureCountdown++;
            ChunksFailed++;
            throw new Exception("Simulated temporary failure");
        }

        ChunksSent++;
        return Task.CompletedTask;
    }

    public Task StopRealtimeSessionAsync(Guid sessionId, CancellationToken ct = default)
    {
        ActiveRealtimeSessionId = null;
        return Task.CompletedTask;
    }
}