using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using Mal.SourceGeneratedDI;
using Mdk.Hub.Features.Input;

namespace Mdk.Hub.Features.NodeScript.BlockSelector;

/// <summary>
///     Thin wrapper view for the block type selector overlay.
/// </summary>
[Instance]
public partial class BlockSelectorView : UserControl
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="BlockSelectorView" /> class.
    /// </summary>
    public BlockSelectorView(IKeyScopeService keyScopeService)
    {
        InitializeComponent();
        Selector.KeyScopeService = keyScopeService;
    }

    /// <inheritdoc />
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        // Apply block-grid class to the items ListBox so our scoped styles take effect
        Selector.PART_Items.Classes.Add("block-grid");
    }
}


