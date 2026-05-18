namespace LotteryDetectionMobile.Models.Voice;

public class StartVoiceUploadResult
{
    public Guid SessionId { get; set; }

    public string TempPath { get; set; } = string.Empty;
}