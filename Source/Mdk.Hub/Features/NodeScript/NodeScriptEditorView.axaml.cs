using Avalonia.Controls;
using Mal.SourceGeneratedDI;

namespace Mdk.Hub.Features.NodeScript;

/// <summary>
///     View for the node-based script editor.
/// </summary>
[Instance]
public partial class NodeScriptEditorView : UserControl
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="NodeScriptEditorView"/> class.
    /// </summary>
    public NodeScriptEditorView()
    {
        InitializeComponent();
    }
}
