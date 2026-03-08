using System.Collections.Generic;
using System.Linq;
using Mdk.Hub.Features.NodeScript.Nodes;
using Mdk.Hub.Framework;

namespace Mdk.Hub.Features.NodeScript;

/// <summary>
///     Represents a connection between an output connector and an input connector.
/// </summary>
public class ConnectionViewModel : ViewModel
{
    readonly NodeScriptEditorViewModel _editor;

    /// <summary>
    ///     Initializes a new instance of <see cref="ConnectionViewModel" />.
    /// </summary>
    public ConnectionViewModel(ConnectorViewModel source, ConnectorViewModel target, NodeScriptEditorViewModel editor)
    {
        Source = source;
        Target = target;
        _editor = editor;
        DisconnectCommand = new RelayCommand(Disconnect);
    }

    /// <summary>Gets the source (output) connector.</summary>
    public ConnectorViewModel Source { get; }

    /// <summary>Gets the target (input) connector.</summary>
    public ConnectorViewModel Target { get; }

    /// <summary>Gets the command that removes this connection.</summary>
    public RelayCommand DisconnectCommand { get; }

    void Disconnect()
    {
        _editor.RemoveConnection(this);
    }
}
