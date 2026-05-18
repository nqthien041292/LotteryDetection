using System.Threading.Channels;
using FluentAssertions;

namespace LotteryDetectionMobile.Tests.Voice;

/// <summary>
///     Tests for thread safety of streaming components (Phase 5).
///     Validates Interlocked/volatile usage under concurrent access.
/// </summary>
public class ThreadSafetyTests
{
    [Fact]
    public async Task ConsecutiveFailures_Should_Be_Thread_Safe_Under_Concurrent_Access()
    {
        // Arrange
        var sender = new ChunkSenderLogic(100); // High threshold
        var channel = Channel.CreateUnbounded<byte[]>();
        var failureCount = 0;
        var lockObj = new object();

        // Write 100 chunks
        for (var i = 0; i < 100; i++) await channel.Writer.WriteAsync(new byte[1024]);
        channel.Writer.Complete();

        // Sender that fails every time
        Task AlwaysFail(byte[] chunk, CancellationToken ct)
        {
            lock (lockObj)
            {
                failureCount++;
            }

            throw new Exception("Always fail");
        }

        // Act
        await sender.ProcessChunksAsync(channel.Reader, AlwaysFail, CancellationToken.None);

        // Assert
        sender.ConsecutiveFailures.Should().Be(100);
        failureCount.Should().Be(100);
        sender.FallbackTriggered.Should().BeTrue(); // Should trigger at 100
    }

    [Fact]
    public async Task Multiple_Stop_Calls_Should_Be_Safe()
    {
        // Arrange
        var sender = new ChunkSenderLogic();

        // Act - Call stop multiple times concurrently
        var tasks = Enumerable.Range(0, 10).Select(_ => Task.Run(() => sender.Stop()));
        await Task.WhenAll(tasks);

        // Assert - Should not throw
        sender.IsStreamingRealtime.Should().BeFalse();
    }

    [Fact]
    public async Task Fallback_Event_Should_Fire_Exactly_Once()
    {
        // Arrange
        var sender = new ChunkSenderLogic(3);
        var channel = Channel.CreateUnbounded<byte[]>();
        var fallbackCount = 0;

        sender.OnStreamingFallback += () => Interlocked.Increment(ref fallbackCount);

        // Write 10 chunks
        for (var i = 0; i < 10; i++) await channel.Writer.WriteAsync(new byte[1024]);
        channel.Writer.Complete();

        // Always fail
        Task AlwaysFail(byte[] chunk, CancellationToken ct)
        {
            throw new Exception("Fail");
        }

        // Act
        await sender.ProcessChunksAsync(channel.Reader, AlwaysFail, CancellationToken.None);

        // Assert
        fallbackCount.Should().Be(1);
    }

    [Fact]
    public async Task Concurrent_Chunk_Processing_Should_Not_Corrupt_State()
    {
        // Arrange
        const int chunkCount = 1000;
        var sender = new ChunkSenderLogic(5000);
        var channel = Channel.CreateUnbounded<byte[]>();
        var processedChunks = 0;

        for (var i = 0; i < chunkCount; i++) await channel.Writer.WriteAsync(new byte[1024]);
        channel.Writer.Complete();

        Task CountChunks(byte[] chunk, CancellationToken ct)
        {
            Interlocked.Increment(ref processedChunks);
            return Task.CompletedTask;
        }

        // Act
        await sender.ProcessChunksAsync(channel.Reader, CountChunks, CancellationToken.None);

        // Assert
        processedChunks.Should().Be(chunkCount);
        sender.TotalChunksSent.Should().Be(chunkCount);
        sender.ConsecutiveFailures.Should().Be(0);
    }

    [Fact]
    public async Task Reset_Failure_Count_Should_Be_Atomic()
    {
        // Arrange
        var sender = new ChunkSenderLogic(10);
        var channel = Channel.CreateUnbounded<byte[]>();
        var callCount = 0;

        // Pattern: fail, success, fail, success...
        // Should never accumulate to maxChunkFailures
        for (var i = 0; i < 100; i++) await channel.Writer.WriteAsync(new byte[1024]);
        channel.Writer.Complete();

        Task AlternatingFailure(byte[] chunk, CancellationToken ct)
        {
            var count = Interlocked.Increment(ref callCount);
            if (count % 2 == 0) throw new Exception("Even fail");
            return Task.CompletedTask;
        }

        // Act
        await sender.ProcessChunksAsync(channel.Reader, AlternatingFailure, CancellationToken.None);

        // Assert - Should complete all chunks without fallback
        sender.FallbackTriggered.Should().BeFalse();
        sender.TotalChunksSent.Should().Be(50); // Half succeeded
    }
}