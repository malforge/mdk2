using System.Collections;
using System.Collections.ObjectModel;

namespace Mdk.Hub.Features.NodeScript.Selector;

/// <summary>
///     Generic base for selector overlay view models.
///     Provides a type-safe <see cref="SelectedItem" /> and <see cref="FilteredItemsCollection" />.
/// </summary>
/// <typeparam name="TItem">The type of items shown in the selector grid/list.</typeparam>
public abstract class SelectorViewModel<TItem> : SelectorViewModelBase where TItem : class
{
    /// <summary>
    ///     Initializes a new instance of <see cref="SelectorViewModel{TItem}" />.
    /// </summary>
    protected SelectorViewModel()
    {
        FilteredItemsCollection = [];
    }

    /// <summary>Gets the mutable collection of items after filtering. Subclasses populate this in <see cref="SelectorViewModelBase.RefreshItems"/>.</summary>
    protected ObservableCollection<TItem> FilteredItemsCollection { get; }

    /// <inheritdoc />
    public override IEnumerable FilteredItems => FilteredItemsCollection;

    /// <summary>
    ///     Gets or sets the currently highlighted item (typed). Hides the untyped base property.
    /// </summary>
    public new TItem? SelectedItem
    {
        get => base.SelectedItem as TItem;
        set => base.SelectedItem = value;
    }
}
