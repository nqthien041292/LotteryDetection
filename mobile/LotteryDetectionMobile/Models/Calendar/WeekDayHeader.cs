namespace LotteryDetectionMobile.Models.Calendar;

/// <summary>One column header in the week timeline: short label, day-of-month number, today flag.</summary>
public sealed class WeekDayHeader
{
    public string Label { get; init; } = string.Empty;
    public int DayNumber { get; init; }
    public bool IsToday { get; init; }
    public DateTime Date { get; init; }
}
