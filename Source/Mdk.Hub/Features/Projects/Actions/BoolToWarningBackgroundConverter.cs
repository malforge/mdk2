using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Mdk.Hub.Features.Projects.Actions;

public class BoolToWarningBackgroundConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isWarning && isWarning)
            return "#33ff6666"; // Semi-transparent red background for warning
        return "Transparent";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
