using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Mdk.Hub.Features.Shell;
using Mdk.Hub.Features.SpaceEngineers;
using Mdk.Hub.Framework;

namespace Mdk.Hub.Features.NodeScript.BlockSelector;

/// <summary>
///     Represents a category entry in the block selector's left panel.
/// </summary>
public sealed record BlockCategoryItem(string Name, int Depth = 0);

/// <summary>
///     Represents a selectable block in the block selector grid.
/// </summary>
public sealed record BlockItem(
    string TypeId,
    string SubtypeId,
    string DisplayName,
    bool IsLargeGrid = true,
    bool IsDlc = false,
    string? IconPath = null)
{
    /// <summary>Full definition ID (TypeId/SubtypeId) used when selecting a specific block.</summary>
    public string Id => $"{TypeId}/{SubtypeId}";
}

/// <summary>
///     View model for the block type selector overlay.
/// </summary>
[ViewModelFor<BlockSelectorView>]
public class BlockSelectorViewModel : OverlayModel
{
    static class State
    {
        public static string? CategoryName { get; set; }
        public static string SearchText { get; set; } = "";
        public static bool IsTypeOnly { get; set; } = true;
    }

    static readonly BlockCategoryItem AllCategory = new("All");

    readonly ISpaceEngineersDataService _seData;
    List<BlockItem> _allBlocks = [];
    // Maps category display name → set of block IDs in that category.
    // A block can appear in multiple categories (e.g., DLC Blocks + its functional category).
    Dictionary<string, HashSet<string>> _categoryBlockIds = new();

    BlockCategoryItem? _selectedCategory;
    BlockItem? _selectedBlock;
    string? _selectedBlockId;
    string _searchText = "";
    bool _isTypeOnly = State.IsTypeOnly;
    bool _forceTypeOnly;

    /// <summary>
    ///     Initializes a new instance of <see cref="BlockSelectorViewModel" />.
    /// </summary>
    public BlockSelectorViewModel(ISpaceEngineersDataService seData)
    {
        _seData = seData;
        Categories = [];
        FilteredBlocks = [];
        OkCommand = new RelayCommand(Ok, CanOk);
        CancelCommand = new RelayCommand(Cancel);
        _searchText = State.SearchText;
    }

    /// <summary>
    ///     Loads SE data and populates categories and blocks. Call once after construction.
    /// </summary>
    public async Task LoadAsync()
    {
        var blocksResult = await _seData.GetAllBlocksAsync();
        var categoriesResult = await _seData.GetCategoriesAsync();

        if (blocksResult.TryGetValue(out var blocks) && categoriesResult.TryGetValue(out var categories))
        {
            _allBlocks = blocks
                .Select(b => new BlockItem(
                    TypeId: b.Id.TypeId,
                    SubtypeId: b.Id.SubtypeId,
                    DisplayName: b.DisplayName,
                    IsLargeGrid: b.CubeSize == "Large",
                    IsDlc: b.Dlc != null,
                    IconPath: b.IconPath))
                .ToList();

            // Build category → block ID sets (blocks can belong to multiple categories)
            _categoryBlockIds = new Dictionary<string, HashSet<string>>();
            foreach (var cat in categories)
            {
                if (!_categoryBlockIds.TryGetValue(cat.DisplayName, out var ids))
                    _categoryBlockIds[cat.DisplayName] = ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var id in cat.Items)
                    ids.Add($"{id.TypeId}/{id.SubtypeId}");
            }

            BuildCategories(categories);
        }
        else
        {
            // Fall back: empty block list — error state visible via empty grid
            _allBlocks = [];
            Categories.Add(AllCategory);
        }

        var restoredCategory = Categories.FirstOrDefault(c => c.Name == State.CategoryName) ?? AllCategory;
        SelectedCategory = restoredCategory;
    }

    /// <summary>
    ///     Gets the list of selectable categories shown in the left panel.
    /// </summary>
    public ObservableCollection<BlockCategoryItem> Categories { get; }

    /// <summary>
    ///     Gets the list of blocks filtered by the currently selected category and search text.
    ///     In type-only mode, deduplicated to one representative per TypeId.
    /// </summary>
    public ObservableCollection<BlockItem> FilteredBlocks { get; }

    /// <summary>
    ///     Gets or sets the currently selected category. Changing this refreshes <see cref="FilteredBlocks" />.
    /// </summary>
    public BlockCategoryItem? SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            if (!SetProperty(ref _selectedCategory, value)) return;
            RefreshFilteredBlocks();
        }
    }

    /// <summary>
    ///     Gets or sets the block currently highlighted in the grid.
    /// </summary>
    public BlockItem? SelectedBlock
    {
        get => _selectedBlock;
        set
        {
            if (!SetProperty(ref _selectedBlock, value)) return;
            ((RelayCommand)OkCommand).NotifyCanExecuteChanged();
        }
    }

    /// <summary>
    ///     Gets the block ID that was confirmed, or <c>null</c> if the selector was cancelled.
    ///     In type-only mode this is the <see cref="BlockItem.TypeId" />; otherwise the full
    ///     <see cref="BlockItem.Id" /> (TypeId/SubtypeId).
    /// </summary>
    public string? SelectedBlockId => _selectedBlockId;

    /// <summary>
    ///     Gets or sets the search text used to filter the block grid.
    /// </summary>
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (!SetProperty(ref _searchText, value)) return;
            RefreshFilteredBlocks();
        }
    }

    /// <summary>
    ///     Gets or sets whether the selector should return any block of the selected type
    ///     rather than a specific block definition. When on, the grid is deduplicated to one
    ///     entry per TypeId, preferring the first non-DLC large-grid variant.
    /// </summary>
    public bool IsTypeOnly
    {
        get => _isTypeOnly;
        set
        {
            if (!SetProperty(ref _isTypeOnly, value)) return;
            OnPropertyChanged(nameof(IsSpecificBlock));
            RefreshFilteredBlocks();
        }
    }

    /// <summary>
    ///     Gets or sets whether the selector shows specific block definitions.
    ///     This is the inverse of <see cref="IsTypeOnly" />.
    /// </summary>
    public bool IsSpecificBlock
    {
        get => !_isTypeOnly;
        set => IsTypeOnly = !value;
    }

    /// <summary>
    ///     Gets or sets whether type-only mode is enforced by the caller.
    ///     When <c>true</c>, the toggle is hidden and <see cref="IsTypeOnly" /> is locked on.
    /// </summary>
    public bool ForceTypeOnly
    {
        get => _forceTypeOnly;
        set
        {
            _forceTypeOnly = value;
            if (value) IsTypeOnly = true;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanToggleTypeOnly));
        }
    }

    /// <summary>
    ///     Gets whether the user can toggle <see cref="IsTypeOnly" />.
    ///     <c>false</c> when <see cref="ForceTypeOnly" /> is set by the caller.
    /// </summary>
    public bool CanToggleTypeOnly => !_forceTypeOnly;

    /// <summary>
    ///     Gets the command that confirms the current selection and dismisses the overlay.
    /// </summary>
    public ICommand OkCommand { get; }

    /// <summary>
    ///     Gets the command that dismisses the overlay without making a selection.
    /// </summary>
    public ICommand CancelCommand { get; }

    void BuildCategories(IReadOnlyList<SpaceEngineers.BlockCategory> seCategories)
    {
        Categories.Add(AllCategory);
        foreach (var cat in seCategories)
            Categories.Add(new BlockCategoryItem(cat.DisplayName, cat.IsSubCategory ? 1 : 0));
    }

    void RefreshFilteredBlocks()
    {
        FilteredBlocks.Clear();
        IEnumerable<BlockItem> source = _selectedCategory is null || _selectedCategory == AllCategory
            ? _allBlocks
            : _categoryBlockIds.TryGetValue(_selectedCategory.Name, out var ids)
                ? _allBlocks.Where(b => ids.Contains($"{b.TypeId}/{b.SubtypeId}"))
                : [];
        if (!string.IsNullOrWhiteSpace(_searchText))
            source = source.Where(b => b.DisplayName.Contains(_searchText, StringComparison.OrdinalIgnoreCase));
        if (_isTypeOnly)
            source = source
                .GroupBy(b => b.TypeId)
                .Select(g => g.OrderBy(b => b.IsDlc).ThenByDescending(b => b.IsLargeGrid).First());
        source = source.OrderBy(b => b.DisplayName, StringComparer.OrdinalIgnoreCase);
        foreach (var block in source)
            FilteredBlocks.Add(block);
        SelectedBlock = null;
    }

    void SaveState()
    {
        State.CategoryName = _selectedCategory == AllCategory ? null : _selectedCategory?.Name;
        State.SearchText = _searchText;
        State.IsTypeOnly = _isTypeOnly;
    }

    bool CanOk() => _selectedBlock is not null;

    void Ok()
    {
        SaveState();
        _selectedBlockId = _isTypeOnly ? _selectedBlock?.TypeId : _selectedBlock?.Id;
        Dismiss();
    }

    void Cancel()
    {
        SaveState();
        Dismiss();
    }
}

