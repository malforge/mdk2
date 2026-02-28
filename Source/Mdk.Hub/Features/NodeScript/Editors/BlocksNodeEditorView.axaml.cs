using Avalonia.Controls;
using Mal.SourceGeneratedDI;
using Mdk.Hub.Framework;

namespace Mdk.Hub.Features.NodeScript.Editors;

/// <summary>
///     View for editing Blocks node properties.
/// </summary>
[Instance]
public partial class BlocksNodeEditorView : UserControl
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="BlocksNodeEditorView"/> class.
    /// </summary>
    public BlocksNodeEditorView()
    {
        InitializeComponent();
    }
}
