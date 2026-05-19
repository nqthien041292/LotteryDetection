using System.Globalization;
using LotteryDetection.Mobile.Views.Components;

namespace LotteryDetection.Mobile.Converters;

/// <summary>
///     Maps a 0..1 progress fraction to a width in DIPs. ConverterParameter is the max width (default 240).
/// </summary>
public sealed class ProgressWidthConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var max = 240d;
        if (parameter is string s && double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
            max = parsed;
        else if (parameter is double d) max = d;
        var f = value is double v ? v : 0;
        return Math.Max(4, max * Math.Clamp(f, 0, 1));
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value!;
    }
}

public sealed class EarnedOpacityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is bool earned && !earned ? 0.4 : 1.0;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value!;
    }
}

public sealed class EarnedStrokeConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var earned = value is bool b && b;
        return earned
            ? ResourceLookup.Color("FamilyCream3Light", "FamilyCream3Dark", Color.FromArgb("#D5E1F2"))
            : Colors.Transparent;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value!;
    }
}

public sealed class EarnedBackgroundConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var earned = value is bool b && b;
        return earned
            ? ResourceLookup.Color("FamilyPaperLight", "FamilyPaperDark", Colors.White)
            : ResourceLookup.Color("FamilyCream2Light", "FamilyCream2Dark", Color.FromArgb("#E8F0FB"));
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value!;
    }
}

public sealed class EarnedBadgeBackgroundConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var earned = value is bool b && b;
        return earned
            ? ResourceLookup.Color("FamilyPrimaryTintLight", "FamilyPrimaryTintDark", Color.FromArgb("#E0EAFF"))
            : ResourceLookup.Color("FamilyCream3Light", "FamilyCream3Dark", Color.FromArgb("#D5E1F2"));
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value!;
    }
}
