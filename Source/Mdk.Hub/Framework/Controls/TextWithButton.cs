using System.Windows.Input;
using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Data;

namespace Mdk.Hub.Framework.Controls;

/// <summary>
///     A read-only text display with an adjacent action button.
///     Useful for picker-style fields where the value is selected via a dialog/overlay.
/// </summary>
public class TextWithButton : TemplatedControl
{
    /// <summary>Defines the <see cref="Value" /> property.</summary>
    public static readonly StyledProperty<string?> ValueProperty =
        AvaloniaProperty.Register<TextWithButton, string?>(nameof(Value));

    /// <summary>Defines the <see cref="Placeholder" /> property.</summary>
    public static readonly StyledProperty<string?> PlaceholderProperty =
        AvaloniaProperty.Register<TextWithButton, string?>(nameof(Placeholder));

    /// <summary>Defines the <see cref="ButtonContent" /> property.</summary>
    public static readonly StyledProperty<object?> ButtonContentProperty =
        AvaloniaProperty.Register<TextWithButton, object?>(nameof(ButtonContent), "Browse\u2026");

    /// <summary>Defines the <see cref="Command" /> property.</summary>
    public static readonly StyledProperty<ICommand?> CommandProperty =
        AvaloniaProperty.Register<TextWithButton, ICommand?>(nameof(Command), defaultBindingMode: BindingMode.TwoWay);

    /// <summary>Defines the <see cref="DisplayText" /> property (computed, read-only from outside).</summary>
    public static readonly StyledProperty<string> DisplayTextProperty =
        AvaloniaProperty.Register<TextWithButton, string>(nameof(DisplayText), string.Empty);

    /// <summary>Defines the <see cref="IsShowingPlaceholder" /> property (computed, read-only from outside).</summary>
    public static readonly StyledProperty<bool> IsShowingPlaceholderProperty =
        AvaloniaProperty.Register<TextWithButton, bool>(nameof(IsShowingPlaceholder));

    /// <summary>Gets or sets the displayed value. When null or empty, <see cref="Placeholder" /> is shown instead.</summary>
    public string? Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    /// <summary>Gets or sets the placeholder text shown when <see cref="Value" /> is null or empty.</summary>
    public string? Placeholder
    {
        get => GetValue(PlaceholderProperty);
        set => SetValue(PlaceholderProperty, value);
    }

    /// <summary>Gets or sets the content of the action button.</summary>
    public object? ButtonContent
    {
        get => GetValue(ButtonContentProperty);
        set => SetValue(ButtonContentProperty, value);
    }

    /// <summary>Gets or sets the command executed when the button is clicked.</summary>
    public ICommand? Command
    {
        get => GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    /// <summary>Gets the resolved display text: <see cref="Value" /> when set, otherwise <see cref="Placeholder" />.</summary>
    public string DisplayText
    {
        get => GetValue(DisplayTextProperty);
        private set => SetValue(DisplayTextProperty, value);
    }

    /// <summary>Gets whether the placeholder is currently being shown.</summary>
    public bool IsShowingPlaceholder
    {
        get => GetValue(IsShowingPlaceholderProperty);
        private set => SetValue(IsShowingPlaceholderProperty, value);
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ValueProperty || change.Property == PlaceholderProperty)
            UpdateDisplayText();
    }

    void UpdateDisplayText()
    {
        var hasValue = !string.IsNullOrEmpty(Value);
        IsShowingPlaceholder = !hasValue;
        DisplayText = hasValue ? Value! : Placeholder ?? string.Empty;
    }
}
