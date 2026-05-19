using System.Globalization;

namespace LotteryDetection.Mobile.Models.Voice;

/// <summary>
///     Formats ISO 8601 date strings for display. Handles single dates and date ranges (start/end).
///     Always renders in en-US with 12-hour time (h:mm tt) and no seconds.
/// </summary>
public static class DateTimeFormatHelper
{
    private static readonly CultureInfo EnUs = CultureInfo.GetCultureInfo("en-US");

    public static string FormatDueDateTime(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return null;

        // Handle date range: "2026-02-15T08:00:00/2026-02-15T10:00:00"
        if (raw.Contains('/'))
        {
            var parts = raw.Split('/', 2);
            var start = TryFormat(parts[0]);
            var end = TryFormat(parts[1]);
            if (start != null && end != null)
            {
                if (DateTime.TryParse(parts[0], EnUs, DateTimeStyles.None, out var s) &&
                    DateTime.TryParse(parts[1], EnUs, DateTimeStyles.None, out var e) &&
                    s.Date == e.Date)
                    return $"{s.ToString("MMM d", EnUs)}, {s.ToString("h:mm tt", EnUs)} - {e.ToString("h:mm tt", EnUs)}";
                return $"{start} - {end}";
            }
        }

        return TryFormat(raw) ?? raw;
    }

    private static string TryFormat(string iso)
    {
        return DateTime.TryParse(iso?.Trim(), EnUs, DateTimeStyles.None, out var dt)
            ? dt.ToString("MMM d, h:mm tt", EnUs)
            : null;
    }
}
