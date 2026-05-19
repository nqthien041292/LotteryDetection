namespace LotteryDetection.Mobile.Models.Calendar;

/// <summary>
///     One cell in the month grid: day number (null = leading/trailing pad),
///     today/selected flags, and up to three member ids whose dots render under the day.
/// </summary>
public sealed class MonthCell
{
    public int? Day { get; init; }
    public DateTime? Date { get; init; }
    public bool IsToday { get; init; }
    public bool IsSelected { get; set; }
    public IReadOnlyList<string> MemberDots { get; init; } = Array.Empty<string>();
    public int OverflowCount { get; init; }
}
