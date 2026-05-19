namespace LotteryDetection.Mobile.Models.Family;

public class CalendarEvent
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = string.Empty;
    public string Owner { get; set; } = string.Empty;
    public DateTime Start { get; set; }
    public DateTime? End { get; set; }
    public string Status { get; set; } = "Scheduled";
    public string Location { get; set; } = string.Empty;
    public string Color { get; set; } = "#6C63FF";
    public string Member { get; set; } = "home";
}