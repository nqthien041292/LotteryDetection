namespace LotteryDetection.Mobile.Models.Calendar;

public enum SyncStatusKind
{
    Synced,
    Paused,
    Error
}

/// <summary>Calendar sync indicator state for the header chip.</summary>
public sealed class SyncState
{
    public SyncStatusKind Kind { get; init; } = SyncStatusKind.Synced;
    public string Provider { get; init; } = "Google Calendar";
    public DateTime? LastSyncAt { get; init; }

    public string DotColor => Kind switch
    {
        SyncStatusKind.Synced => "#22C55E",
        SyncStatusKind.Paused => "#F59E0B",
        _ => "#EF4444"
    };

    public string Label
    {
        get
        {
            var rel = LastSyncAt.HasValue ? RelativeTime(LastSyncAt.Value) : null;
            return Kind switch
            {
                SyncStatusKind.Synced => rel != null ? $"{Provider} · synced {rel}" : $"{Provider} · synced",
                SyncStatusKind.Paused => $"{Provider} · paused",
                _ => $"{Provider} · sync error"
            };
        }
    }

    private static string RelativeTime(DateTime when)
    {
        var span = DateTime.Now - when;
        if (span.TotalSeconds < 60) return "just now";
        if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes}m ago";
        if (span.TotalHours < 24) return $"{(int)span.TotalHours}h ago";
        return $"{(int)span.TotalDays}d ago";
    }
}
