using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Mdk.Hub.Features.Projects.Actions;

/// <summary>
///     Converts a boolean deployment status to a display string.
/// </summary>
public class BoolToDeploymentStatusConverter : IValueConverter
{
    /// <summary>
    ///     Converts a boolean value to a deployment status string.
    /// </summary>
    /// <param name="value">The boolean deployment status.</param>
    /// <param name="targetType">The target type (unused).</param>
    /// <param name="parameter">Converter parameter (unused).</param>
    /// <param name="culture">Culture information (unused).</param>
    /// <returns>"Deployed" if true, "Not Deployed" if false, or "Unknown" if value is not a boolean.</returns>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isDeployed)
            return isDeployed ? "Deployed" : "Not Deployed";
        return "Unknown";
    }

    /// <summary>
    ///     Not implemented. This converter does not support two-way binding.
    /// </summary>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}
