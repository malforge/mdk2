using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Mdk.Hub.Features.NodeScript.Nodes;

/// <summary>
///     ViewModel for a Block node (data source for blocks in Space Engineers).
/// </summary>
public partial class BlockNodeViewModel : ObservableObject
{
    [ObservableProperty]
    Point _location;

    [ObservableProperty]
    string _pattern = "* Door";

    [ObservableProperty]
    string _selectionType = "Wildcard";

    [ObservableProperty]
    string _blockType = "Any";

    /// <summary>
    ///     Gets the node title for display.
    /// </summary>
    public string Title => $"Block: {Pattern}";
}
