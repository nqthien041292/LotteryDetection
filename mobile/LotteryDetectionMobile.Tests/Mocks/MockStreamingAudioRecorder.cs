using System.Threading.Channels;

namespace LotteryDetectionMobile.Tests.Mocks;

/// <summary>
///     Mock implementation of IStreamingAudioRecorder for testing.
///     Allows controlled emission of audio chunks.
/// </summary>
public class MockStreamingAudioRecorder
{
    private Channel<byte[]>? _channel;

    public bool IsRecording { get; private set; }

    public ChannelReader<byte[]> AudioChunks => _channel?.Reader ?? throw new InvalidOperationException("Not started");

    /// <summary>
    ///     Number of chunks emitted.
    /// </summary>
    public int ChunksEmitted { get; private set; }

    public Task StartAsync(CancellationToken ct)
    {
        _channel = Channel.CreateBounded<byte[]>(new BoundedChannelOptions(100)
        {
            FullMode = BoundedChannelFullMode.DropOldest
        });
        IsRecording = true;
        ChunksEmitted = 0;
        return Task.CompletedTask;
    }

    public Task StopAsync()
    {
        IsRecording = false;
        _channel?.Writer.TryComplete();
        return Task.CompletedTask;
    }

    /// <summary>
    ///     Emit a test audio chunk (1024 bytes of zeros).
    /// </summary>
    public bool EmitChunk()
    {
        if (!IsRecording || _channel == null) return false;

        var chunk = new byte[1024];
        var result = _channel.Writer.TryWrite(chunk);
        if (result) ChunksEmitted++;
        return result;
    }

    /// <summary>
    ///     Emit multiple chunks at once.
    /// </summary>
    public int EmitChunks(int count)
    {
        var emitted = 0;
        for (var i = 0; i < count; i++)
            if (EmitChunk())
                emitted++;
        return emitted;
    }
}