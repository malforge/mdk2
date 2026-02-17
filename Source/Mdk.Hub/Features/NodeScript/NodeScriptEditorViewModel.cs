using System.Threading.Tasks;
using Mal.SourceGeneratedDI;
using Mdk.Hub.Framework;

namespace Mdk.Hub.Features.NodeScript;

/// <summary>
///     ViewModel for the node-based script editor.
/// </summary>
[Instance]
[ViewModelFor<NodeScriptEditorView>]
public class NodeScriptEditorViewModel : ViewModel, ISupportClosing, IHaveATitle
{
    string _title = "Node Script Editor";

    /// <summary>
    ///     Initializes a new instance of the <see cref="NodeScriptEditorViewModel" /> class.
    /// </summary>
    public NodeScriptEditorViewModel() { }

    /// <summary>
    ///     Gets or sets the editor title.
    /// </summary>
    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    /// <inheritdoc />
    public Task<bool> WillCloseAsync() =>
        // For now, always allow closing
        // TODO: Check for unsaved changes and prompt user
        Task.FromResult(true);

    /// <inheritdoc />
    public Task DidCloseAsync() => Task.CompletedTask;
}