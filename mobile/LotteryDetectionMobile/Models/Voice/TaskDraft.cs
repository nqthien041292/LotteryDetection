namespace LotteryDetectionMobile.Models.Voice;

public class TaskDraft
{
    public string Title { get; set; } = string.Empty;
    public string Owner { get; set; } = string.Empty;
    public DateTime When { get; set; } = DateTime.Now.AddHours(2);
    public string Priority { get; set; } = "Med";
    public string Category { get; set; } = "Home";
    public string? SuggestionText { get; set; }
    public string? Notes { get; set; }
}
