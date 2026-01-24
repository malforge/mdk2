using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using System.Windows.Input;

namespace Mdk.Hub.Framework.Controls;

public class SelectableCard : ContentControl
{
    public static readonly StyledProperty<bool> IsSelectedProperty =
        AvaloniaProperty.Register<SelectableCard, bool>(nameof(IsSelected), defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public static readonly StyledProperty<bool> NeedsAttentionProperty =
        AvaloniaProperty.Register<SelectableCard, bool>(nameof(NeedsAttention), defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public static readonly StyledProperty<ICommand?> CommandProperty =
        AvaloniaProperty.Register<SelectableCard, ICommand?>(nameof(Command));

    public static readonly StyledProperty<object?> CommandParameterProperty =
        AvaloniaProperty.Register<SelectableCard, object?>(nameof(CommandParameter));

    public bool IsSelected
    {
        get => GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
    }

    public bool NeedsAttention
    {
        get => GetValue(NeedsAttentionProperty);
        set => SetValue(NeedsAttentionProperty, value);
    }

    public ICommand? Command
    {
        get => GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public object? CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        
        if (!IsEnabled)
            return;

        if (Command?.CanExecute(CommandParameter) == true)
        {
            Command.Execute(CommandParameter);
        }
        
        e.Handled = true;
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        UpdatePseudoClasses();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == IsSelectedProperty || change.Property == IsEnabledProperty || change.Property == NeedsAttentionProperty)
        {
            UpdatePseudoClasses();
        }
    }

    void UpdatePseudoClasses()
    {
        PseudoClasses.Set(":selected", IsSelected);
        PseudoClasses.Set(":disabled", !IsEnabled);
        PseudoClasses.Set(":needs-attention", NeedsAttention);
    }
}
