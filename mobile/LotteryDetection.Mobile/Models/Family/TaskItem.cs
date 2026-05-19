namespace LotteryDetection.Mobile.Models.Family;

public class TaskItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Owner { get; set; } = string.Empty;
    public string Status { get; set; } = "Open";
    public string Priority { get; set; } = "Normal";
    public DateTime? DueDate { get; set; }
    public int Points { get; set; }
    public bool IsPinned { get; set; }
    public bool IsSelected { get; set; }
    public string AssigneeId { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public DateTime? UpdatedAt { get; set; }
    public IEnumerable<string> Tags { get; set; } = Enumerable.Empty<string>();
}