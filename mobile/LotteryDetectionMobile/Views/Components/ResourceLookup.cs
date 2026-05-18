namespace LotteryDetectionMobile.Views.Components;

/// <summary>
///     Helper for resolving theme-aware resource keys at runtime when binding via DynamicResource isn't ergonomic.
/// </summary>
internal static class ResourceLookup
{
    public static Color Color(string lightKey, string darkKey, Color fallback)
    {
        var key = Application.Current?.RequestedTheme == AppTheme.Dark ? darkKey : lightKey;
        if (Application.Current?.Resources.TryGetValue(key, out var value) == true && value is Color color)
            return color;
        return fallback;
    }

    public static T Resource<T>(string key, T fallback) where T : class
    {
        if (Application.Current?.Resources.TryGetValue(key, out var value) == true && value is T typed)
            return typed;
        return fallback;
    }
}
