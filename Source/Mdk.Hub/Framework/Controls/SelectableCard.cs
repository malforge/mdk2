using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;

namespace Mdk.Hub.Framework.Controls;

/// <summary>
///     A selectable card control that displays content and can be clicked to execute a command.
///     Supports visual states for selection, disabled, and "needs attention" indicators.
/// </summary>
public class SelectableCard : ContentControl
{
    /// <summary>
    ///     Defines the <see cref="IsSelected" /> property.
    /// </summary>
    public static readonly StyledProperty<bool> IsSelectedProperty =
        AvaloniaProperty.Register<SelectableCard, bool>(nameof(IsSelected), defaultBindingMode: BindingMode.TwoWay);

    /// <summary>
    ///     Defines the <see cref="NeedsAttention" /> property.
    /// </summary>
    public static readonly StyledProperty<bool> NeedsAttentionProperty =
        AvaloniaProperty.Register<SelectableCard, bool>(nameof(NeedsAttention), defaultBindingMode: BindingMode.TwoWay);

    /// <summary>
    ///     Defines the <see cref="Command" /> property.
    /// </summary>
    public static readonly StyledProperty<ICommand?> CommandProperty =
        AvaloniaProperty.Register<SelectableCard, ICommand?>(nameof(Command));

    /// <summary>
    ///     Defines the <see cref="CommandParameter" /> property.
    /// </summary>
    public static readonly StyledProperty<object?> CommandParameterProperty =
        AvaloniaProperty.Register<SelectableCard, object?>(nameof(CommandParameter));

    /// <summary>
    ///     Gets or sets whether the card is selected.
    /// </summary>
    public bool IsSelected
    {
        get => GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
    }

    /// <summary>
    ///     Gets or sets whether the card needs attention (e.g., has unsaved changes).
    /// </summary>
    public bool NeedsAttention
    {
        get => GetValue(NeedsAttentionProperty);
        set => SetValue(NeedsAttentionProperty, value);
    }

    /// <summary>
    ///     Gets or sets the command to execute when the card is clicked.
    /// </summary>
    public ICommand? Command
    {
        get => GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    /// <summary>
    ///     Gets or sets the parameter to pass to the command.
    /// </summary>
    public object? CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    /// <inheritdoc />
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        if (!IsEnabled)
            return;

        if (Command?.CanExecute(CommandParameter) == true)
            Command.Execute(CommandParameter);

        e.Handled = true;
    }

    /// <inheritdoc />
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        UpdatePseudoClasses();
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == IsSelectedProperty || change.Property == IsEnabledProperty || change.Property == NeedsAttentionProperty)
            UpdatePseudoClasses();
    }

    void UpdatePseudoClasses()
    {
        PseudoClasses.Set(":selected", IsSelected);
        PseudoClasses.Set(":disabled", !IsEnabled);
        PseudoClasses.Set(":needs-attention", NeedsAttention);
    }
}
