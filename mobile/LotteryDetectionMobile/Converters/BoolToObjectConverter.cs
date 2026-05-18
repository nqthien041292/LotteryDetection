using System.Globalization;

namespace LotteryDetectionMobile.Converters;

/// <summary>
///     Generic boolean → value converter. <c>ConverterParameter</c> is "trueValue|falseValue" (pipe-separated).
///     Numeric and color literals are auto-parsed; otherwise the string is returned verbatim.
/// </summary>
public sealed class BoolToObjectConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (parameter is not string s) return value;
        var split = s.Split('|', 2);
        if (split.Length != 2) return value;

        var pick = value is bool b && b ? split[0] : split[1];
        return Coerce(pick, targetType);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value;
    }

    private static object? Coerce(string token, Type targetType)
    {
        if (targetType == typeof(double) && double.TryParse(token, NumberStyles.Any, CultureInfo.InvariantCulture, out var d))
            return d;
        if (targetType == typeof(int) && int.TryParse(token, NumberStyles.Any, CultureInfo.InvariantCulture, out var i))
            return i;
        if (targetType == typeof(Color))
            return Color.FromArgb(token);
        if (targetType == typeof(bool) && bool.TryParse(token, out var bv))
            return bv;
        return token;
    }
}

public sealed class InvertBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b ? !b : value!;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b ? !b : value!;
}

public sealed class IsNullConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value == null;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public sealed class IsNotNullConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value != null;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

