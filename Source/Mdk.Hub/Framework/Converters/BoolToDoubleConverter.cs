using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Mdk.Hub.Framework.Converters;

/// <summary>
///     Converts a boolean value to a double (0.0 or 1.0).
/// </summary>
public class BoolToDoubleConverter : IValueConverter
{
    /// <summary>
    ///     Singleton instance of the converter.
    /// </summary>
    public static readonly BoolToDoubleConverter Instance = new();

    /// <inheritdoc />
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
            return boolValue ? 1.0 : 0.0;
        
        return 0.0;
    }

    /// <inheritdoc />
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
