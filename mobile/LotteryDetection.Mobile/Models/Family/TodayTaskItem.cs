namespace LotteryDetection.Mobile.Models.Family;

/// <summary>
///     UI-shaped task row used by the Today screen — pre-resolves owner avatar id, formatted "when", and reward points.
/// </summary>
public class TodayTaskItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public string Title { get; set; } = string.Empty;

    public string Subtitle { get; set; } = string.Empty;

    public string OwnerLabel { get; set; } = string.Empty;

    /// <summary>alex / sam / jordan / riley / home</summary>
    public string Member { get; set; } = "home";

    public string When { get; set; } = string.Empty;

    /// <summary>high / med / low</summary>
    public string Priority { get; set; } = "med";

    public int Points { get; set; }

    public bool Done { get; set; }

    public bool IsToday { get; set; }
}
