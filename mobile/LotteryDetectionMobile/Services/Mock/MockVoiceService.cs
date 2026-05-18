using System.Runtime.CompilerServices;
using LotteryDetectionMobile.Models.Voice;
using LotteryDetectionMobile.Services.Interfaces;

namespace LotteryDetectionMobile.Services.Mock;

public class MockVoiceService : IVoiceService
{
    private readonly string[] sampleChunks =
    {
        "Pick up groceries",
        "for pasta night",
        "and check homework",
        "before dinner",
        "add garlic bread"
    };

    public static IVoiceService Instance { get; } = new MockVoiceService();

    public Task<Guid> StartAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(Guid.NewGuid());
    }

    public Task UploadChunkAsync(Guid sessionId, Stream stream, string contentType, CancellationToken cancellationToken)
    {
        // Mock implementation ignores upload.
        return Task.CompletedTask;
    }

    public Task<string> CompleteAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        return Task.FromResult($"mock://voice/{sessionId}");
    }

    public Task StartRecordingAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task StopRecordingAsync()
    {
        return Task.CompletedTask;
    }

    public async IAsyncEnumerable<(string Text, bool IsFinal)> StreamTranscriptAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var index = 0;
        while (!cancellationToken.IsCancellationRequested)
        {
            yield return (sampleChunks[index], true); // Mock always returns final
            index = (index + 1) % sampleChunks.Length;
            await Task.Delay(750, cancellationToken);
        }
    }

    public string? LastRecordingPath => null;

    public Guid? LastVoiceTaskId => null;

    public Task<ConfirmTaskResult> ConfirmTaskAsync(Guid taskId, CancellationToken cancellationToken)
    {
        return Task.FromResult(new ConfirmTaskResult
        {
            TaskId = taskId,
            Status = "Confirmed",
            Message = "Mock task confirmed"
        });
    }

    public Task<VoiceTaskPreview?> GetPreviewAsync(Guid taskId, CancellationToken cancellationToken)
    {
        return Task.FromResult<VoiceTaskPreview?>(new VoiceTaskPreview
        {
            TaskId = taskId,
            Status = "TranscriptionCompleted",
            Title = "Mock task",
            Assignee = "Me",
            DueDateTime = DateTime.Today.ToString("yyyy-MM-dd"),
            Priority = "Medium",
            Notes = "Mock preview data"
        });
    }
}