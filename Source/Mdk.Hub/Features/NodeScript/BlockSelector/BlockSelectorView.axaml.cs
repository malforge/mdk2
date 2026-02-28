using Avalonia.Controls;
using Mal.SourceGeneratedDI;

namespace Mdk.Hub.Features.NodeScript.BlockSelector;

/// <summary>
///     Full-window overlay view for selecting a block type.
/// </summary>
[Instance]
public partial class BlockSelectorView : UserControl
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="BlockSelectorView" /> class.
    /// </summary>
    public BlockSelectorView()
    {
        InitializeComponent();
    }
}
