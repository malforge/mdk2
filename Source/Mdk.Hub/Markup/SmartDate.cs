using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml;

namespace Mdk.Hub.Markup;

public class SmartDateExtension : MarkupExtension
{
    public override object ProvideValue(IServiceProvider serviceProvider) => new SmartDateConverter();
}

public class SmartDateConverter : IValueConverter
{
    object? IValueConverter.Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is DateTime dateTime) return Convert(dateTime, culture);
        if (value is DateTimeOffset dateTimeOffset) return Convert(dateTimeOffset.DateTime, culture);
        return value;
    }

    object? IValueConverter.ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();

    public string Convert(DateTime dateTime, CultureInfo culture) => Convert(new DateTimeOffset(dateTime), culture);
    
    public string Convert(DateTimeOffset dateTimeOffset, CultureInfo culture)
    {
        // Rules:
        // - If less than 1 minute ago, show "Just now"
        // - If less than 1 hour ago, show "X minutes ago"
        // - If today, show clock time (e.g., "3:45 PM")
        // - If yesterday, show "Yesterday at [time]"
        // - If within the last 7 days, show "X days ago"
        // Else show date (e.g., "MM/dd/yyyy" or "dd/MM/yyyy" based on culture)
        var now = DateTimeOffset.Now;
        var timeSpan = now - dateTimeOffset;
        if (timeSpan.TotalMinutes < 1)
            return "Just now";
        if (timeSpan.TotalHours < 1)
            return $"{(int)timeSpan.TotalMinutes} minutes ago";
        if (dateTimeOffset.Date == now.Date)
            return dateTimeOffset.ToString("t", culture);
        if (dateTimeOffset.Date == now.AddDays(-1).Date)
            return $"Yesterday at {dateTimeOffset.ToString("t", culture)}";
        if (timeSpan.TotalDays < 7)
            return $"{(int)timeSpan.TotalDays} days ago";
        return dateTimeOffset.ToString("d", culture);
    }
}