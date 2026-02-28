using System;
using System.Collections.Generic;
using System.Linq;
using Mal.SourceGeneratedDI;
using Mdk.Hub.Features.NodeScript.Selector;
using Mdk.Hub.Framework;

namespace Mdk.Hub.Features.NodeScript.NodeSelector;

/// <summary>
///     View model for the node type selector overlay.
/// </summary>
[Instance]
[ViewModelFor<NodeSelectorView>]
public class NodeSelectorViewModel : SelectorViewModel<NodeSelectorItem>
{
    static readonly List<NodeSelectorItem> AllNodes =
    [
        new("Blocks",          "Data Sources",  "Blocks",         "References a set of terminal blocks."),
        new("On Argument",     "Triggers",      "OnArgument",     "Fires when the script receives a specific argument."),
        new("Wait For State",  "Flow Control",  "WaitForState",   "Pauses execution until a condition is met."),
    ];

    readonly List<NodeSelectorItem> _allNodes;

    /// <summary>
    ///     Initializes a new instance of <see cref="NodeSelectorViewModel" />.
    /// </summary>
    public NodeSelectorViewModel()
    {
        _allNodes = AllNodes;
        BuildCategories();
        RefreshItems();
    }

    /// <inheritdoc />
    public override string Title => "Add Node";

    /// <summary>
    ///     Gets the node type ID of the confirmed selection, or <c>null</c> if cancelled.
    /// </summary>
    public string? SelectedNodeTypeId { get; private set; }

    /// <inheritdoc />
    protected override void OnConfirm()
    {
        SelectedNodeTypeId = SelectedItem?.NodeTypeId;
        base.OnConfirm();
    }

    /// <inheritdoc />
    protected override void OnCancel()
    {
        SelectedNodeTypeId = null;
        base.OnCancel();
    }

    /// <inheritdoc />
    protected override void RefreshItems()
    {
        FilteredItemsCollection.Clear();
        IEnumerable<NodeSelectorItem> source = SelectedCategory == null || SelectedCategory == AllCategory
            ? _allNodes
            : _allNodes.Where(n => n.Category == SelectedCategory.Name);
        if (!string.IsNullOrWhiteSpace(SearchText))
            source = source.Where(n => n.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
        foreach (var item in source.OrderBy(n => n.Name, StringComparer.OrdinalIgnoreCase))
            FilteredItemsCollection.Add(item);
        SelectedItem = null;
    }

    void BuildCategories()
    {
        Categories.Add(AllCategory);
        foreach (var cat in _allNodes.Select(n => n.Category).Distinct(StringComparer.OrdinalIgnoreCase))
            Categories.Add(new SelectorCategoryItem(cat));
        SelectedCategory = AllCategory;
    }
}
