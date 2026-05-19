namespace LotteryDetectionMobile.Models.Voice;

/// <summary>
///     Preview data for a voice task with AI-extracted entities.
///     Mirrors backend VoiceTaskPreviewDto.
/// </summary>
public class VoiceTaskPreview
{
    public Guid TaskId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Transcript { get; set; } = string.Empty;
    public string Intent { get; set; }
    public double? Confidence { get; set; }
    public string Title { get; set; }
    public string Assignee { get; set; }
    public string DueDateTime { get; set; }
    public string DueDateFormatted => DateTimeFormatHelper.FormatDueDateTime(DueDateTime);
    public string Priority { get; set; }
    public string Location { get; set; }
    public string Category { get; set; }
    public string Notes { get; set; }
    public int? DurationMinutes { get; set; }
}
