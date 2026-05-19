namespace LotteryDetection.Mobile.Models.Dashboard;

/// <summary>
///     Single recent-activity row for the home dashboard "Family pulse" card.
/// </summary>
public class FamilyPulseEntry
{
    public string Member { get; init; } = "home";

    public string Headline { get; init; } = string.Empty;

    public string Subline { get; init; } = string.Empty;
}
