namespace Mdk.Hub.Features.NodeScript.NodeSelector;

/// <summary>
///     Represents a node type available in the node selector.
/// </summary>
/// <param name="Name">The display name of the node.</param>
/// <param name="Category">The category this node belongs to.</param>
/// <param name="Description">A short description of what the node does.</param>
/// <param name="NodeTypeId">Internal identifier used when creating the node.</param>
public sealed record NodeSelectorItem(string Name, string Category, string NodeTypeId, string Description = "");
