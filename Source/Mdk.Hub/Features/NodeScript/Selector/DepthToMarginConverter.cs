using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

namespace Mdk.Hub.Features.NodeScript.Selector;

/// <summary>
///     Converts a category depth (int) to a Thickness used as padding/margin for indentation.
/// </summary>
public sealed class DepthToMarginConverter : IValueConverter
{
    /// <summary>Shared singleton instance.</summary>
    public static readonly DepthToMarginConverter Instance = new();

    DepthToMarginConverter() { }

    /// <inheritdoc />
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var depth = value is int d ? d : 0;
        return new Thickness(8 + depth * 16, 4, 8, 4);
    }

    /// <inheritdoc />
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
