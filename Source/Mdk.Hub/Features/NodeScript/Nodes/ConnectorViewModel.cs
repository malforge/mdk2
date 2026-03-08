using Avalonia;
using Mdk.Hub.Framework;

namespace Mdk.Hub.Features.NodeScript.Nodes;

/// <summary>
///     Represents a single input or output connector port on a node.
/// </summary>
public class ConnectorViewModel : ViewModel
{
    Point _anchor;
    bool _isConnected;

    /// <summary>Gets an optional display label for this connector.</summary>
    public string Title { get; init; } = "";

    /// <summary>Gets whether this connector is an output (vs input).</summary>
    public bool IsOutput { get; init; }

    /// <summary>Gets or sets the canvas anchor point. Updated automatically by NodifyM.</summary>
    public Point Anchor
    {
        get => _anchor;
        set => SetProperty(ref _anchor, value);
    }

    /// <summary>Gets or sets whether this connector has at least one active connection.</summary>
    public bool IsConnected
    {
        get => _isConnected;
        set => SetProperty(ref _isConnected, value);
    }
}
