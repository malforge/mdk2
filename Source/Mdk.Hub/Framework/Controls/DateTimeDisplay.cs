using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;

namespace Mdk.Hub.Framework.Controls;

public class DateTimeDisplay : TemplatedControl
{
    public static readonly StyledProperty<DateTimeOffset?> ValueProperty =
        AvaloniaProperty.Register<DateTimeDisplay, DateTimeOffset?>(nameof(Value));

    public static readonly StyledProperty<bool> AllowRelativeProperty =
        AvaloniaProperty.Register<DateTimeDisplay, bool>(nameof(AllowRelative), defaultValue: true);

    public static readonly StyledProperty<bool> AllowLocalProperty =
        AvaloniaProperty.Register<DateTimeDisplay, bool>(nameof(AllowLocal), defaultValue: true);

    public static readonly StyledProperty<bool> AllowUtcProperty =
        AvaloniaProperty.Register<DateTimeDisplay, bool>(nameof(AllowUtc), defaultValue: true);

    public static readonly StyledProperty<DateTimeDisplayMode> CurrentModeProperty =
        AvaloniaProperty.Register<DateTimeDisplay, DateTimeDisplayMode>(nameof(CurrentMode), defaultValue: DateTimeDisplayMode.Relative);

    public static readonly StyledProperty<string> DisplayTextProperty =
        AvaloniaProperty.Register<DateTimeDisplay, string>(nameof(DisplayText), defaultValue: string.Empty);

    public DateTimeOffset? Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public bool AllowRelative
    {
        get => GetValue(AllowRelativeProperty);
        set => SetValue(AllowRelativeProperty, value);
    }

    public bool AllowLocal
    {
        get => GetValue(AllowLocalProperty);
        set => SetValue(AllowLocalProperty, value);
    }

    public bool AllowUtc
    {
        get => GetValue(AllowUtcProperty);
        set => SetValue(AllowUtcProperty, value);
    }

    public DateTimeDisplayMode CurrentMode
    {
        get => GetValue(CurrentModeProperty);
        set => SetValue(CurrentModeProperty, value);
    }

    public string DisplayText
    {
        get => GetValue(DisplayTextProperty);
        private set => SetValue(DisplayTextProperty, value);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        
        var button = e.NameScope.Find<Button>("PART_CycleButton");
        if (button != null)
            button.Click += OnCycleClick;
        
        UpdateDisplayText();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ValueProperty || 
            change.Property == CurrentModeProperty ||
            change.Property == AllowRelativeProperty ||
            change.Property == AllowLocalProperty ||
            change.Property == AllowUtcProperty)
        {
            UpdateDisplayText();
        }
    }

    void OnCycleClick(object? sender, RoutedEventArgs e)
    {
        var availableModes = GetAvailableModes();
        if (availableModes.Count <= 1)
            return;

        var currentIndex = availableModes.IndexOf(CurrentMode);
        var nextIndex = (currentIndex + 1) % availableModes.Count;
        CurrentMode = availableModes[nextIndex];
    }

    List<DateTimeDisplayMode> GetAvailableModes()
    {
        var modes = new List<DateTimeDisplayMode>();
        
        if (AllowRelative) modes.Add(DateTimeDisplayMode.Relative);
        if (AllowLocal) modes.Add(DateTimeDisplayMode.Local);
        if (AllowUtc) modes.Add(DateTimeDisplayMode.Utc);
        
        // If no modes are enabled, default to relative
        if (modes.Count == 0)
            modes.Add(DateTimeDisplayMode.Relative);
        
        return modes;
    }

    void UpdateDisplayText()
    {
        if (!Value.HasValue)
        {
            DisplayText = "—";
            return;
        }

        var availableModes = GetAvailableModes();
        if (!availableModes.Contains(CurrentMode))
            CurrentMode = availableModes[0];

        DisplayText = CurrentMode switch
        {
            DateTimeDisplayMode.Relative => FormatRelative(Value.Value),
            DateTimeDisplayMode.Local => Value.Value.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"),
            DateTimeDisplayMode.Utc => Value.Value.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss 'UTC'"),
            _ => Value.Value.ToString()
        };
    }

    static string FormatRelative(DateTimeOffset dateTime)
    {
        var now = DateTimeOffset.Now;
        var difference = now - dateTime;

        if (difference.TotalSeconds < 60)
            return "just now";
        if (difference.TotalMinutes < 60)
        {
            var minutes = (int)difference.TotalMinutes;
            return $"{minutes} minute{(minutes == 1 ? "" : "s")} ago";
        }
        if (difference.TotalHours < 24)
        {
            var hours = (int)difference.TotalHours;
            return $"{hours} hour{(hours == 1 ? "" : "s")} ago";
        }
        if (difference.TotalDays < 30)
        {
            var days = (int)difference.TotalDays;
            return $"{days} day{(days == 1 ? "" : "s")} ago";
        }
        if (difference.TotalDays < 365)
        {
            var months = (int)(difference.TotalDays / 30);
            return $"{months} month{(months == 1 ? "" : "s")} ago";
        }
        
        var years = (int)(difference.TotalDays / 365);
        return $"{years} year{(years == 1 ? "" : "s")} ago";
    }
}
