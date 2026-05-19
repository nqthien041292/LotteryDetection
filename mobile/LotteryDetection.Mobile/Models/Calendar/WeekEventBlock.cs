namespace LotteryDetection.Mobile.Models.Calendar;

/// <summary>
///     Positioned event in the week timeline.
///     DayIndex 0..6 (Mon..Sun), StartHour decimal (e.g. 9.5 = 9:30), DurationHours.
/// </summary>
public sealed class WeekEventBlock
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public int DayIndex { get; init; }
    public double StartHour { get; init; }
    public double DurationHours { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Member { get; init; } = "home";
}
