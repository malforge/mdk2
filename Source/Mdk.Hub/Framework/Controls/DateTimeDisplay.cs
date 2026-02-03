using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;

namespace Mdk.Hub.Framework.Controls;

/// <summary>
///     A control that displays a <see cref="DateTimeOffset" /> value in different formats (relative, local, UTC).
///     Users can click to cycle between available display modes.
/// </summary>
public class DateTimeDisplay : TemplatedControl
{
    /// <summary>
    ///     Defines the <see cref="Value" /> property.
    /// </summary>
    public static readonly StyledProperty<DateTimeOffset?> ValueProperty =
        AvaloniaProperty.Register<DateTimeDisplay, DateTimeOffset?>(nameof(Value));

    /// <summary>
    ///     Defines the <see cref="AllowRelative" /> property.
    /// </summary>
    public static readonly StyledProperty<bool> AllowRelativeProperty =
        AvaloniaProperty.Register<DateTimeDisplay, bool>(nameof(AllowRelative), true);

    /// <summary>
    ///     Defines the <see cref="AllowLocal" /> property.
    /// </summary>
    public static readonly StyledProperty<bool> AllowLocalProperty =
        AvaloniaProperty.Register<DateTimeDisplay, bool>(nameof(AllowLocal), true);

    /// <summary>
    ///     Defines the <see cref="AllowUtc" /> property.
    /// </summary>
    public static readonly StyledProperty<bool> AllowUtcProperty =
        AvaloniaProperty.Register<DateTimeDisplay, bool>(nameof(AllowUtc), true);

    /// <summary>
    ///     Defines the <see cref="CurrentMode" /> property.
    /// </summary>
    public static readonly StyledProperty<DateTimeDisplayMode> CurrentModeProperty =
        AvaloniaProperty.Register<DateTimeDisplay, DateTimeDisplayMode>(nameof(CurrentMode), DateTimeDisplayMode.Relative);

    /// <summary>
    ///     Defines the <see cref="DisplayText" /> property.
    /// </summary>
    public static readonly StyledProperty<string> DisplayTextProperty =
        AvaloniaProperty.Register<DateTimeDisplay, string>(nameof(DisplayText), string.Empty);

    /// <summary>
    ///     Gets or sets the date/time value to display.
    /// </summary>
    public DateTimeOffset? Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    /// <summary>
    ///     Gets or sets whether relative time display is allowed (e.g., "5 minutes ago").
    /// </summary>
    public bool AllowRelative
    {
        get => GetValue(AllowRelativeProperty);
        set => SetValue(AllowRelativeProperty, value);
    }

    /// <summary>
    ///     Gets or sets whether local time display is allowed.
    /// </summary>
    public bool AllowLocal
    {
        get => GetValue(AllowLocalProperty);
        set => SetValue(AllowLocalProperty, value);
    }

    /// <summary>
    ///     Gets or sets whether UTC time display is allowed.
    /// </summary>
    public bool AllowUtc
    {
        get => GetValue(AllowUtcProperty);
        set => SetValue(AllowUtcProperty, value);
    }

    /// <summary>
    ///     Gets or sets the current display mode.
    /// </summary>
    public DateTimeDisplayMode CurrentMode
    {
        get => GetValue(CurrentModeProperty);
        set => SetValue(CurrentModeProperty, value);
    }

    /// <summary>
    ///     Gets the formatted display text based on the current mode and value.
    /// </summary>
    public string DisplayText
    {
        get => GetValue(DisplayTextProperty);
        private set => SetValue(DisplayTextProperty, value);
    }

    /// <inheritdoc />
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        var button = e.NameScope.Find<Button>("PART_CycleButton");
        if (button != null)
            button.Click += OnCycleClick;

        UpdateDisplayText();
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ValueProperty || change.Property == CurrentModeProperty || change.Property == AllowRelativeProperty || change.Property == AllowLocalProperty || change.Property == AllowUtcProperty)
            UpdateDisplayText();
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
