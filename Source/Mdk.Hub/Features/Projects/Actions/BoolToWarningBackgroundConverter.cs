using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Mdk.Hub.Features.Projects.Actions;

/// <summary>
/// Converts a boolean value to a warning background color. True values produce a semi-transparent red background.
/// </summary>
public class BoolToWarningBackgroundConverter : IValueConverter
{
    /// <summary>
    /// Converts a boolean value to a background color string.
    /// </summary>
    /// <param name="value">The boolean value to convert.</param>
    /// <param name="targetType">The target type (not used).</param>
    /// <param name="parameter">The parameter (not used).</param>
    /// <param name="culture">The culture (not used).</param>
    /// <returns>A semi-transparent red color if value is true; otherwise, "Transparent".</returns>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isWarning && isWarning)
            return "#33ff6666"; // Semi-transparent red background for warning
        return "Transparent";
    }

    /// <summary>
    /// Not supported. Throws <see cref="NotImplementedException"/>.
    /// </summary>
    /// <param name="value">The value (not used).</param>
    /// <param name="targetType">The target type (not used).</param>
    /// <param name="parameter">The parameter (not used).</param>
    /// <param name="culture">The culture (not used).</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotImplementedException">Always thrown.</exception>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}
