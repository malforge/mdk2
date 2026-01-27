using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Mdk.Hub.Features.Projects.Actions;

public class BoolToDeploymentStatusConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isDeployed)
            return isDeployed ? "Deployed" : "Not Deployed";
        return "Unknown";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}