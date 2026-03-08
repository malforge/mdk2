using Avalonia;
using Mdk.Hub.Framework;

namespace Mdk.Hub.Features.NodeScript.Nodes;

/// <summary>
///     Base class for nodes with no expandable inline editor (fixed header + connectors only).
/// </summary>
public abstract class SimpleNodeViewModel : ViewModel
{
    Point _location;

    /// <summary>Gets or sets the node location on the canvas.</summary>
    public Point Location
    {
        get => _location;
        set => SetProperty(ref _location, value);
    }

    /// <summary>Gets the display title of this node.</summary>
    public abstract string Title { get; }
}
