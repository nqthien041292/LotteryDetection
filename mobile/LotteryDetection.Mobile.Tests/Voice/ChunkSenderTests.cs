using System.Threading.Channels;
using LotteryDetection.Mobile.Tests.Mocks;
using FluentAssertions;

namespace LotteryDetection.Mobile.Tests.Voice;

/// <summary>
///     Tests for chunk sender fallback logic (Phase 5).
/// </summary>
public class ChunkSenderTests
{
    [Fact]
    public async Task Should_Send_All_Chunks_When_No_Failures()
    {
        // Arrange
        var sender = new ChunkSenderLogic();
        var mockSignalR = new MockSignalRClient();
        var recorder = new MockStreamingAudioRecorder();

        await recorder.StartAsync(CancellationToken.None);
        recorder.EmitChunks(10);
        await recorder.StopAsync();

        // Act
        await sender.ProcessChunksAsync(
            recorder.AudioChunks,
            (chunk, ct) => mockSignalR.StreamAudioChunkAsync(Guid.NewGuid(), chunk, ct),
            CancellationToken.None);

        // Assert
        sender.TotalChunksSent.Should().Be(10);
        sender.FallbackTriggered.Should().BeFalse();
        sender.ConsecutiveFailures.Should().Be(0);
        mockSignalR.ChunksSent.Should().Be(10);
    }

    [Fact]
    public async Task Should_Trigger_Fallback_After_MaxChunkFailures()
    {
        // Arrange
        var sender = new ChunkSenderLogic();
        var recorder = new MockStreamingAudioRecorder();
        var fallbackTriggered = false;
        var failureCount = 0;

        sender.OnStreamingFallback += () => fallbackTriggered = true;

        await recorder.StartAsync(CancellationToken.None);
        recorder.EmitChunks(20);
        await recorder.StopAsync();

        // Sender that fails after 3 successful sends
        var successCount = 0;

        Task FailAfter3Successes(byte[] chunk, CancellationToken ct)
        {
            successCount++;
            if (successCount > 3)
            {
                failureCount++;
                throw new Exception("Simulated failure");
            }

            return Task.CompletedTask;
        }

        // Act
        await sender.ProcessChunksAsync(
            recorder.AudioChunks,
            FailAfter3Successes,
            CancellationToken.None);

        // Assert - 3 successful, then 5 failures trigger fallback
        sender.TotalChunksSent.Should().Be(3);
        sender.FallbackTriggered.Should().BeTrue();
        fallbackTriggered.Should().BeTrue();
        failureCount.Should().Be(5); // Exactly 5 failures to trigger fallback
    }

    [Fact]
    public async Task Should_Reset_FailureCount_On_Successful_Send()
    {
        // Arrange
        var sender = new ChunkSenderLogic();
        var recorder = new MockStreamingAudioRecorder();
        var sendCount = 0;

        await recorder.StartAsync(CancellationToken.None);
        recorder.EmitChunks(10);
        await recorder.StopAsync();

        // Sender that fails every other time for first 6 chunks, then succeeds
        Task SendWithIntermittentFailures(byte[] chunk, CancellationToken ct)
        {
            sendCount++;
            if (sendCount <= 6 && sendCount % 2 == 0) throw new Exception("Intermittent failure");
            return Task.CompletedTask;
        }

        // Act
        await sender.ProcessChunksAsync(
            recorder.AudioChunks,
            SendWithIntermittentFailures,
            CancellationToken.None);

        // Assert - Should not trigger fallback because failures never reach 5 consecutive
        sender.FallbackTriggered.Should().BeFalse();
        sender.TotalChunksSent.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Should_Recover_From_Temporary_Failures()
    {
        // Arrange
        var sender = new ChunkSenderLogic();
        var mockSignalR = new MockSignalRClient();
        var recorder = new MockStreamingAudioRecorder();

        await recorder.StartAsync(CancellationToken.None);
        recorder.EmitChunks(10);
        await recorder.StopAsync();

        // Fail for 3 chunks, then recover
        mockSignalR.FailForChunks(3);

        // Act
        await sender.ProcessChunksAsync(
            recorder.AudioChunks,
            (chunk, ct) => mockSignalR.StreamAudioChunkAsync(Guid.NewGuid(), chunk, ct),
            CancellationToken.None);

        // Assert
        sender.FallbackTriggered.Should().BeFalse();
        mockSignalR.ChunksFailed.Should().Be(3);
        mockSignalR.ChunksSent.Should().Be(7); // 10 - 3 failed
    }

    [Fact]
    public async Task Should_Stop_When_Cancelled()
    {
        // Arrange
        var sender = new ChunkSenderLogic();
        var channel = Channel.CreateUnbounded<byte[]>();
        var cts = new CancellationTokenSource();

        // Write some chunks
        for (var i = 0; i < 5; i++) await channel.Writer.WriteAsync(new byte[1024]);

        var chunksSent = 0;

        Task SendWithDelay(byte[] chunk, CancellationToken ct)
        {
            chunksSent++;
            if (chunksSent == 2) cts.Cancel();
            return Task.CompletedTask;
        }

        // Act
        await sender.ProcessChunksAsync(channel.Reader, SendWithDelay, cts.Token);

        // Assert
        sender.TotalChunksSent.Should().BeLessThanOrEqualTo(3);
        sender.FallbackTriggered.Should().BeFalse();
    }

    [Fact]
    public async Task IsStreamingRealtime_Should_Be_True_While_Processing()
    {
        // Arrange
        var sender = new ChunkSenderLogic();
        var channel = Channel.CreateUnbounded<byte[]>();
        var streamingStatesDuringProcessing = new List<bool>();

        await channel.Writer.WriteAsync(new byte[1024]);
        await channel.Writer.WriteAsync(new byte[1024]);
        channel.Writer.Complete();

        Task SendAndRecord(byte[] chunk, CancellationToken ct)
        {
            streamingStatesDuringProcessing.Add(sender.IsStreamingRealtime);
            return Task.CompletedTask;
        }

        // Act
        sender.IsStreamingRealtime.Should().BeFalse(); // Before processing
        await sender.ProcessChunksAsync(channel.Reader, SendAndRecord, CancellationToken.None);

        // Assert
        streamingStatesDuringProcessing.Should().AllSatisfy(state => state.Should().BeTrue());
        sender.IsStreamingRealtime.Should().BeFalse(); // After processing
    }

    [Fact]
    public void Stop_Should_Set_IsStreamingRealtime_To_False()
    {
        // Arrange
        var sender = new ChunkSenderLogic();

        // Act
        sender.Stop();

        // Assert
        sender.IsStreamingRealtime.Should().BeFalse();
    }
}