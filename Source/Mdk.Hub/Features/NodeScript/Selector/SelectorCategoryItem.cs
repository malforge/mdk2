namespace Mdk.Hub.Features.NodeScript.Selector;

/// <summary>
///     Represents a category entry in a selector overlay's left panel.
/// </summary>
/// <param name="Name">The display name of the category.</param>
/// <param name="Depth">The indentation depth; 0 = top-level, 1 = sub-category.</param>
public sealed record SelectorCategoryItem(string Name, int Depth = 0);
