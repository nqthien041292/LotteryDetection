namespace LotteryDetection.Mobile.Models.Voice;

public class VoiceTaskListItem
{
    public Guid TaskId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Assignee { get; set; }
    public string Priority { get; set; }
    public string DueDateTime { get; set; }
    public string DueDateFormatted => DateTimeFormatHelper.FormatDueDateTime(DueDateTime);
    public string Category { get; set; }
    public DateTime CreatedAt { get; set; }
    public string TranscriptPreview { get; set; } = string.Empty;
}