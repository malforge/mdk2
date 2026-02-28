using Avalonia.Controls;
using Mal.SourceGeneratedDI;
using Mdk.Hub.Features.Input;
using Mdk.Hub.Features.NodeScript.Selector;

namespace Mdk.Hub.Features.NodeScript.NodeSelector;

/// <summary>
///     Thin wrapper view for the node type selector overlay.
/// </summary>
[Instance]
public partial class NodeSelectorView : UserControl
{
    /// <summary>
    ///     Initializes a new instance of <see cref="NodeSelectorView" />.
    /// </summary>
    public NodeSelectorView(IKeyScopeService keyScopeService)
    {
        InitializeComponent();
        Selector.KeyScopeService = keyScopeService;
    }
}
