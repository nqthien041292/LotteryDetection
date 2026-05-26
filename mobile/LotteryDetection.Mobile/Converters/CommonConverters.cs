using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace LotteryDetection.Mobile.Converters;

public class BooleanToColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool b && parameter is string param)
        {
            var parts = param.Split('|');
            if (parts.Length == 2)
            {
                var colorStr = b ? parts[0] : parts[1];
                if (colorStr == "Transparent") return Colors.Transparent;
                if (colorStr == "White") return Colors.White;
                return Color.FromArgb(colorStr);
            }
        }
        return Colors.Transparent;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class EqualityToColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (parameter is string param)
        {
            var parts = param.Split('|');
            if (parts.Length == 3)
            {
                var targetValue = parts[0];
                var trueColor = parts[1];
                var falseColor = parts[2];

                bool isEqual = value?.ToString() == targetValue;
                var colorStr = isEqual ? trueColor : falseColor;
                
                if (colorStr == "Transparent") return Colors.Transparent;
                if (colorStr == "White") return Colors.White;
                return Color.FromArgb(colorStr);
            }
        }
        return Colors.Transparent;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class BooleanToTextConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool b && parameter is string param)
        {
            var parts = param.Split('|');
            if (parts.Length == 2)
            {
                return b ? parts[0] : parts[1];
            }
        }
        return string.Empty;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class InvertBooleanConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is bool b && !b;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is bool b && !b;
    }
}
