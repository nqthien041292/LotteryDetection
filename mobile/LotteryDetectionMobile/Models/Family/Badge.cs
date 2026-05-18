namespace LotteryDetectionMobile.Models.Family;

public class Badge
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Points { get; set; }
    public string Icon { get; set; } = string.Empty;
    public bool IsUnlocked { get; set; }
}