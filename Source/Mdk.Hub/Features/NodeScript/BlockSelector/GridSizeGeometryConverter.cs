using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Mdk.Hub.Features.NodeScript.BlockSelector;

/// <summary>
///     Converts <see cref="BlockItem.IsLargeGrid" /> to a <see cref="Geometry" /> for the grid-size indicator.
///     Large grid: outlined square only.
///     Small grid: filled outer square with a hole (EvenOdd) at lower-left — half-size inner square.
/// </summary>
public sealed class GridSizeGeometryConverter : IValueConverter
{
    // Outer square: 18×18. Inner (hole) square: 9×9, lower-left.
    static readonly Geometry LargeGridGeometry = Geometry.Parse("M 0,0 L 18,0 L 18,18 L 0,18 Z M 1.5,1.5 L 1.5,16.5 L 16.5,16.5 L 16.5,1.5 Z");
    static readonly Geometry SmallGridGeometry;

    /// <summary>Singleton instance for use in XAML.</summary>
    public static readonly GridSizeGeometryConverter Instance = new();

    static GridSizeGeometryConverter()
    {
        // Outer rect filled, inner rect (lower-left, 9×9 with 1.5px inset) punched out via EvenOdd.
        var group = new GeometryGroup { FillRule = FillRule.EvenOdd };
        group.Children.Add(Geometry.Parse("M 0,0 L 18,0 L 18,18 L 0,18 Z"));
        group.Children.Add(Geometry.Parse("M 1.5,9 L 1.5,16.5 L 9,16.5 L 9,9 Z"));
        SmallGridGeometry = group;
    }

    /// <inheritdoc />
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? LargeGridGeometry : SmallGridGeometry;

    /// <inheritdoc />
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
