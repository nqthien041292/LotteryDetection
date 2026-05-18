namespace LotteryDetectionMobile.Models.Voice;

public class CompleteVoiceUploadResult
{
    public Guid SessionId { get; set; }

    public string FilePath { get; set; } = string.Empty;

    public Guid? VoiceTaskId { get; set; }
}