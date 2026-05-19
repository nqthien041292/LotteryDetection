namespace LotteryDetection.Mobile.Models.Family;

public class Reward
{
    public string Id { get; set; } = string.Empty;
    public string Emoji { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Cost { get; set; }
    public bool Affordable { get; set; }
}
