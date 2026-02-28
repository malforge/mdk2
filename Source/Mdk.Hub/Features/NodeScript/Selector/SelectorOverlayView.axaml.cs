using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Mdk.Hub.Features.Input;

namespace Mdk.Hub.Features.NodeScript.Selector;

/// <summary>
///     Shared overlay view for all selector overlays (block selector, node selector, etc.).
///     Outer views set <see cref="ItemTemplate" />, <see cref="FilterContent" />, and optionally
///     <see cref="ItemsPanel" /> to customise the items area and header filter slot.
///     Keyboard scope (Enter = confirm, Escape = cancel, Ctrl+F = focus search) and double-click
///     to confirm are handled here.
/// </summary>
public partial class SelectorOverlayView : UserControl
{
    /// <summary>The DataTemplate used to render each item in the items panel.</summary>
    public static readonly StyledProperty<IDataTemplate?> ItemTemplateProperty =
        AvaloniaProperty.Register<SelectorOverlayView, IDataTemplate?>(nameof(ItemTemplate));

    /// <summary>The panel template used to lay out items (defaults to VirtualizingStackPanel).</summary>
    public static readonly StyledProperty<ITemplate<Panel?>?> ItemsPanelProperty =
        AvaloniaProperty.Register<SelectorOverlayView, ITemplate<Panel?>?>(nameof(ItemsPanel));

    /// <summary>Optional content placed in the header after the search box (e.g. filter toggles).</summary>
    public static readonly StyledProperty<object?> FilterContentProperty =
        AvaloniaProperty.Register<SelectorOverlayView, object?>(nameof(FilterContent));

    IDisposable? _keyScope;
    IKeyScopeService? _keyScopeService;

    /// <summary>
    ///     Initializes a new instance of <see cref="SelectorOverlayView" />.
    /// </summary>
    public SelectorOverlayView()
    {
        InitializeComponent();

        // Wire styled properties to inner controls once the template is applied
        this.GetObservable(ItemTemplateProperty)
            .Subscribe(t => PART_Items.ItemTemplate = t);
        this.GetObservable(ItemsPanelProperty)
            .Subscribe(p => { if (p != null) PART_Items.ItemsPanel = p; });
        this.GetObservable(FilterContentProperty)
            .Subscribe(c => PART_FilterContent.Content = c);

        PART_Items.DoubleTapped += OnItemsDoubleTapped;
    }

    /// <summary>Gets or sets the DataTemplate used to render each item.</summary>
    public IDataTemplate? ItemTemplate
    {
        get => GetValue(ItemTemplateProperty);
        set => SetValue(ItemTemplateProperty, value);
    }

    /// <summary>Gets or sets the items panel template (null = default VirtualizingStackPanel).</summary>
    public ITemplate<Panel?>? ItemsPanel
    {
        get => GetValue(ItemsPanelProperty);
        set => SetValue(ItemsPanelProperty, value);
    }

    /// <summary>Gets or sets content placed in the header filter slot.</summary>
    public object? FilterContent
    {
        get => GetValue(FilterContentProperty);
        set => SetValue(FilterContentProperty, value);
    }

    /// <summary>
    ///     The <see cref="IKeyScopeService" /> to use for keyboard shortcut registration.
    ///     Must be set before the view is attached to the visual tree.
    /// </summary>
    public IKeyScopeService? KeyScopeService
    {
        get => _keyScopeService;
        set => _keyScopeService = value;
    }

    /// <inheritdoc />
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (_keyScopeService != null && TopLevel.GetTopLevel(this) is { } topLevel)
        {
            _keyScope = _keyScopeService.PushScope(topLevel,
                new KeyScopeBinding(Key.Enter, KeyModifiers.None, TryConfirm),
                new KeyScopeBinding(Key.Escape, KeyModifiers.None, TryCancel),
                new KeyScopeBinding(Key.F, KeyModifiers.Control, FocusSearch));
        }
        Dispatcher.UIThread.Post(() => PART_SearchBox.Focus(), DispatcherPriority.Loaded);
        if (DataContext is SelectorViewModelBase vm)
            _ = vm.ActivateAsync();
    }

    /// <inheritdoc />
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _keyScope?.Dispose();
        _keyScope = null;
    }

    void TryConfirm()
    {
        if (DataContext is SelectorViewModelBase vm && vm.ConfirmCommand.CanExecute(null))
            vm.ConfirmCommand.Execute(null);
    }

    void TryCancel()
    {
        if (DataContext is SelectorViewModelBase vm)
            vm.CancelCommand.Execute(null);
    }

    void FocusSearch() => PART_SearchBox.Focus();

    void OnItemsDoubleTapped(object? sender, TappedEventArgs e) => TryConfirm();
}
