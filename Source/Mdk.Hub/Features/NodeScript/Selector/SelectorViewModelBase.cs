using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Mdk.Hub.Features.Shell;
using Mdk.Hub.Framework;

namespace Mdk.Hub.Features.NodeScript.Selector;

/// <summary>
///     Non-generic base for selector overlay view models.
///     Exposes all properties needed by <see cref="SelectorOverlayView" /> for AXAML binding.
/// </summary>
public abstract class SelectorViewModelBase : OverlayModel
{
    /// <summary>The "All" sentinel category shared by all selectors.</summary>
    protected static readonly SelectorCategoryItem AllCategory = new("All");

    string _searchText = "";
    SelectorCategoryItem? _selectedCategory;
    object? _selectedItem;

    /// <summary>
    ///     Initializes a new instance of <see cref="SelectorViewModelBase" />.
    /// </summary>
    protected SelectorViewModelBase()
    {
        ConfirmCommand = new RelayCommand(OnConfirm, CanConfirm);
        CancelCommand = new RelayCommand(OnCancel);
        Categories = [];
    }

    /// <summary>Called by the view when it is attached to the visual tree. Override to load data.</summary>
    public virtual Task ActivateAsync() => Task.CompletedTask;

    /// <summary>Gets the title shown in the overlay header.</summary>
    public abstract string Title { get; }

    /// <summary>Gets the category list shown in the left panel.</summary>
    public ObservableCollection<SelectorCategoryItem> Categories { get; }

    /// <summary>Gets or sets the active category; changing it refreshes the item list.</summary>
    public SelectorCategoryItem? SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            if (!SetProperty(ref _selectedCategory, value)) return;
            RefreshItems();
        }
    }

    /// <summary>Gets or sets the search text; changing it refreshes the item list.</summary>
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (!SetProperty(ref _searchText, value)) return;
            RefreshItems();
        }
    }

    /// <summary>Gets the filtered item collection for binding (runtime type is <c>ObservableCollection&lt;TItem&gt;</c>).</summary>
    public abstract IEnumerable FilteredItems { get; }

    /// <summary>Gets or sets the currently highlighted item (untyped for AXAML two-way binding).</summary>
    public object? SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (!SetProperty(ref _selectedItem, value)) return;
            OnSelectedItemChanged(value);
            ((RelayCommand)ConfirmCommand).NotifyCanExecuteChanged();
        }
    }

    /// <summary>Gets the command that confirms the current selection and dismisses the overlay.</summary>
    public ICommand ConfirmCommand { get; }

    /// <summary>Gets the command that dismisses the overlay without a selection.</summary>
    public ICommand CancelCommand { get; }

    /// <summary>Called when <see cref="SelectedItem" /> changes. Override to react in subclasses.</summary>
    protected virtual void OnSelectedItemChanged(object? item) { }

    /// <summary>Override to determine whether confirmation is allowed.</summary>
    protected virtual bool CanConfirm() => _selectedItem != null;

    /// <summary>Override to perform work before the overlay is confirmed.</summary>
    protected virtual void OnConfirm() => Dismiss();

    /// <summary>Override to perform work before the overlay is cancelled.</summary>
    protected virtual void OnCancel() => Dismiss();

    /// <summary>Rebuild <see cref="FilteredItems" /> from current search text and category.</summary>
    protected abstract void RefreshItems();
}
