using System.Globalization;
using LotteryDetection.Mobile.Views.Components;

namespace LotteryDetection.Mobile.Converters;

/// <summary>
///     Converts a family-member id (alex/sam/jordan/riley/home) into a Color drawn from the active theme palette.
/// </summary>
public sealed class MemberToColorConverter : IValueConverter
{
    public string Slot { get; set; } = "Bg";

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var slot = (parameter as string ?? Slot ?? "Bg").ToLowerInvariant() switch
        {
            "text" => MemberPalette.Slot.Text,
            "dot" => MemberPalette.Slot.Dot,
            _ => MemberPalette.Slot.Bg
        };
        var memberId = value as string ?? "home";
        return MemberPalette.Resolve(memberId, slot);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value!;
    }
}

/// <summary>
///     True/false → 1.0 / 0.55 opacity for done-task fade.
/// </summary>
public sealed class DoneOpacityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is bool b && b ? 0.55 : 1.0;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value!;
    }
}

/// <summary>
///     Returns true when the bound string has any non-whitespace content (used to hide empty subtitles).
/// </summary>
public sealed class HasTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is string s && !string.IsNullOrWhiteSpace(s);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value!;
    }
}

/// <summary>
///     Extracts the first character of a string (for avatars).
/// </summary>
public sealed class FirstCharConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string s && !string.IsNullOrWhiteSpace(s))
            return s.Trim()[0].ToString().ToUpperInvariant();
        return "?";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value!;
    }
}

/// <summary>
///     Returns true when the bound integer is greater-than-or-equal to ConverterParameter (used for week-strip dot density).
/// </summary>
public sealed class AtLeastConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var v = value is int i ? i : 0;
        var threshold = 1;
        if (parameter is string s && int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
            threshold = parsed;
        else if (parameter is int p) threshold = p;
        return v >= threshold;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value!;
    }
}

/// <summary>
///     Inverts a boolean value (used to toggle paired Buttons that swap on active state).
/// </summary>
public sealed class InverseBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is bool b ? !b : true;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is bool b ? !b : false;
    }
}

/// <summary>
///     Returns true when the bound integer equals zero (used to swap empty-state copy on a non-empty layout).
/// </summary>
public sealed class IsZeroConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is int i && i == 0;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value!;
    }
}
