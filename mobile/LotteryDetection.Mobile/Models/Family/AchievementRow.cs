namespace LotteryDetection.Mobile.Models.Family;

/// <summary>UI row for the family leaderboard / member tiles — pre-resolves member colour, rank, and "you" flag.</summary>
public class AchievementRow
{
    public string MemberId { get; set; } = "home";
    public string Name { get; set; } = string.Empty;
    public int Streak { get; set; }
    public int Weekly { get; set; }
    public int Rank { get; set; }
    public string Medal { get; set; } = string.Empty;
    public bool IsMe { get; set; }
}

/// <summary>UI row for badges grid.</summary>
public class BadgeRow
{
    public string Id { get; set; } = string.Empty;
    public string Emoji { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool Earned { get; set; }
    public Color BadgeColor { get; set; } = Microsoft.Maui.Graphics.Colors.SteelBlue;
}
