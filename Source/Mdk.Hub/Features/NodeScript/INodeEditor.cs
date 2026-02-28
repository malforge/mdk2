using System;

namespace Mdk.Hub.Features.NodeScript;

/// <summary>
///     Interface for nodes that support property editing.
/// </summary>
public interface INodeEditor
{
    /// <summary>
    ///     Gets the editor for this node's properties.
    /// </summary>
    /// <returns>A tuple containing the ViewModel and View type for the editor.</returns>
    (object viewModel, Type viewType) GetEditor();
}
