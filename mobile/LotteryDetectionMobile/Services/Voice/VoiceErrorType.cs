namespace LotteryDetectionMobile.Services.Voice;

/// <summary>
///     Error types for voice processing operations.
/// </summary>
public enum VoiceErrorType
{
    Connection,
    SessionStart,
    Streaming,
    Transcription,
    Backend
}