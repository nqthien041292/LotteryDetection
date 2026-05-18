namespace LotteryDetectionMobile.Models.Voice;

public class PagedVoiceTaskListResult
{
    public List<VoiceTaskListItem> Items { get; set; } = new();
    public int TotalCount { get; set; }
}