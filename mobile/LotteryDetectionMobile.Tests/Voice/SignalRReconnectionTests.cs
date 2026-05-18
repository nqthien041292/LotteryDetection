using LotteryDetectionMobile.Tests.Mocks;
using FluentAssertions;

namespace LotteryDetectionMobile.Tests.Voice;

/// <summary>
///     Tests for SignalR reconnection behavior (Phase 5).
///     Tests session tracking and resumption logic.
/// </summary>
public class SignalRReconnectionTests
{
    [Fact]
    public async Task StartRealtimeSession_Should_Track_ActiveSessionId()
    {
        // Arrange
        var client = new MockSignalRClient();
        var sessionId = Guid.NewGuid();

        // Act
        await client.StartRealtimeSessionAsync(sessionId);

        // Assert
        client.ActiveRealtimeSessionId.Should().Be(sessionId);
    }

    [Fact]
    public async Task StopRealtimeSession_Should_Clear_ActiveSessionId()
    {
        // Arrange
        var client = new MockSignalRClient();
        var sessionId = Guid.NewGuid();
        await client.StartRealtimeSessionAsync(sessionId);

        // Act
        await client.StopRealtimeSessionAsync(sessionId);

        // Assert
        client.ActiveRealtimeSessionId.Should().BeNull();
    }

    [Fact]
    public async Task StartRealtimeSession_Should_Not_Track_When_Disconnected()
    {
        // Arrange
        var client = new MockSignalRClient();
        var sessionId = Guid.NewGuid();
        client.Disconnect();

        // Act
        await client.StartRealtimeSessionAsync(sessionId);

        // Assert
        client.ActiveRealtimeSessionId.Should().BeNull();
    }

    [Fact]
    public void Reconnect_Should_Update_ConnectionId()
    {
        // Arrange
        var client = new MockSignalRClient();
        var originalConnectionId = client.ConnectionId;
        client.Disconnect();

        // Act
        client.Reconnect();

        // Assert
        client.IsConnected.Should().BeTrue();
        client.ConnectionId.Should().NotBe(originalConnectionId);
        client.ConnectionId.Should().StartWith("reconnected-");
    }

    [Fact]
    public async Task StreamAudioChunk_Should_Fail_When_Disconnected()
    {
        // Arrange
        var client = new MockSignalRClient();
        client.Disconnect();

        // Act
        var act = () => client.StreamAudioChunkAsync(Guid.NewGuid(), new byte[1024]);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Not connected");
    }

    [Fact]
    public async Task StreamAudioChunk_Should_Succeed_After_Reconnect()
    {
        // Arrange
        var client = new MockSignalRClient();
        client.Disconnect();
        client.Reconnect();

        // Act
        await client.StreamAudioChunkAsync(Guid.NewGuid(), new byte[1024]);

        // Assert
        client.ChunksSent.Should().Be(1);
    }

    [Fact]
    public async Task Should_Track_Chunks_Sent_And_Failed()
    {
        // Arrange
        var client = new MockSignalRClient();
        var sessionId = Guid.NewGuid();

        // Fail for 3 chunks temporarily, then recover
        client.FailForChunks(3);

        // Act - Send 10 chunks
        for (var i = 0; i < 10; i++)
            try
            {
                await client.StreamAudioChunkAsync(sessionId, new byte[1024]);
            }
            catch
            {
                // Expected for first 3
            }

        // Assert - First 3 fail, remaining 7 succeed
        client.ChunksSent.Should().Be(7);
        client.ChunksFailed.Should().Be(3);
    }
}