using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using Mal.SourceGeneratedDI;
using Mdk.Hub.Features.Input;

namespace Mdk.Hub.Features.NodeScript.BlockSelector;

/// <summary>
///     Full-window overlay view for selecting a block type.
/// </summary>
[Instance]
public partial class BlockSelectorView : UserControl
{
    readonly IKeyScopeService _keyScopeService;
    IDisposable? _keyScope;

    /// <summary>
    ///     Initializes a new instance of the <see cref="BlockSelectorView" /> class.
    /// </summary>
    public BlockSelectorView(IKeyScopeService keyScopeService)
    {
        _keyScopeService = keyScopeService;
        InitializeComponent();
    }

    /// <inheritdoc />
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (TopLevel.GetTopLevel(this) is { } topLevel)
            _keyScope = _keyScopeService.PushScope(topLevel, new KeyScopeBinding(Key.F, KeyModifiers.Control, FocusSearch));
        Dispatcher.UIThread.Post(() => SearchBox.Focus(), DispatcherPriority.Loaded);
    }

    /// <inheritdoc />
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _keyScope?.Dispose();
        _keyScope = null;
    }

    void FocusSearch() => SearchBox.Focus();
}

