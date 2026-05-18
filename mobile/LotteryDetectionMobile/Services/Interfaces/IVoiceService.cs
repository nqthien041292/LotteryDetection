using LotteryDetectionMobile.Models.Voice;

namespace LotteryDetectionMobile.Services.Interfaces;

public interface IVoiceService
{
    string? LastRecordingPath { get; }

    /// <summary>
    ///     VoiceTaskId from the last completed upload. Used for confirming task creation.
    /// </summary>
    Guid? LastVoiceTaskId { get; }

    Task<Guid> StartAsync(CancellationToken cancellationToken);
    Task UploadChunkAsync(Guid sessionId, Stream stream, string contentType, CancellationToken cancellationToken);
    Task<string> CompleteAsync(Guid sessionId, CancellationToken cancellationToken);
    Task StartRecordingAsync(CancellationToken cancellationToken);
    Task StopRecordingAsync();
    IAsyncEnumerable<(string Text, bool IsFinal)> StreamTranscriptAsync(CancellationToken cancellationToken);

    /// <summary>
    ///     Confirms a voice task for creation in Microsoft 365.
    /// </summary>
    Task<ConfirmTaskResult> ConfirmTaskAsync(Guid taskId, CancellationToken cancellationToken);

    /// <summary>
    ///     Gets task preview with AI-extracted entities from backend.
    ///     Returns null if request fails.
    /// </summary>
    Task<VoiceTaskPreview?> GetPreviewAsync(Guid taskId, CancellationToken cancellationToken);
}