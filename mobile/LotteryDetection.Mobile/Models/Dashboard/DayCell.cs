namespace LotteryDetection.Mobile.Models.Dashboard;

/// <summary>
///     One cell of the home-dashboard week strip (bundle screens-home.jsx week strip).
/// </summary>
public class DayCell
{
    public string Label { get; init; } = string.Empty;

    public int Day { get; init; }

    public bool IsToday { get; init; }

    public int DotCount { get; init; }

    /// <summary>Date this cell represents — used to drill into the calendar.</summary>
    public DateTime Date { get; init; }
}
