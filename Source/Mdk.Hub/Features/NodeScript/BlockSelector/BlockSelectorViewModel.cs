using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Mal.SourceGeneratedDI;
using Mdk.Hub.Features.Images;
using Mdk.Hub.Features.NodeScript.Selector;
using Mdk.Hub.Features.SpaceEngineers;
using Mdk.Hub.Framework;

namespace Mdk.Hub.Features.NodeScript.BlockSelector;

/// <summary>
///     Represents a selectable block in the block selector grid.
/// </summary>
public sealed class BlockItem(
    string typeId,
    string subtypeId,
    string displayName,
    bool isLargeGrid = true,
    bool isDlc = false,
    string? iconPath = null) : Model
{
    Bitmap? _icon;

    /// <summary>Full definition ID (TypeId/SubtypeId) used when selecting a specific block.</summary>
    public string Id => $"{TypeId}/{SubtypeId}";
    /// <summary>The block TypeId without the MyObjectBuilder_ prefix.</summary>
    public string TypeId { get; } = typeId;
    /// <summary>The block SubtypeId.</summary>
    public string SubtypeId { get; } = subtypeId;
    /// <summary>Display name shown in the selector.</summary>
    public string DisplayName { get; } = displayName;
    /// <summary>Whether this is a large-grid variant.</summary>
    public bool IsLargeGrid { get; } = isLargeGrid;
    /// <summary>Whether this is a small-grid variant.</summary>
    public bool IsSmallGrid => !IsLargeGrid;
    /// <summary>Whether this block requires DLC.</summary>
    public bool IsDlc { get; } = isDlc;
    /// <summary>Absolute path to the block's .dds icon file, if available.</summary>
    public string? IconPath { get; } = iconPath;

    /// <summary>Decoded icon bitmap, populated asynchronously after construction.</summary>
    public Bitmap? Icon
    {
        get => _icon;
        set => SetProperty(ref _icon, value);
    }
}

/// <summary>
///     View model for the block type selector overlay.
/// </summary>
[Instance]
[ViewModelFor<BlockSelectorView>]
public class BlockSelectorViewModel : SelectorViewModel<BlockItem>
{
    static class State
    {
        public static string? CategoryName { get; set; }
        public static string SearchText { get; set; } = "";
        public static bool IsTypeOnly { get; set; } = true;
    }

    readonly ISpaceEngineersDataService _seData;
    readonly IImageService _images;
    List<BlockItem> _allBlocks = [];
    Dictionary<string, HashSet<string>> _categoryBlockIds = new();

    bool _isTypeOnly = State.IsTypeOnly;
    bool _forceTypeOnly;

    /// <summary>
    ///     Initializes a new instance of <see cref="BlockSelectorViewModel" />.
    /// </summary>
    public BlockSelectorViewModel(ISpaceEngineersDataService seData, IImageService images)
    {
        _seData = seData;
        _images = images;
        SearchText = State.SearchText;
    }

    /// <inheritdoc />
    public override string Title => "Select Block Type";

    /// <inheritdoc />
    public override Task ActivateAsync() => LoadAsync();

    /// <summary>
    ///     Loads SE data and populates categories and blocks.
    /// </summary>
    public async Task LoadAsync()
    {
        var blocksResult = await _seData.GetAllBlocksAsync();
        var categoriesResult = await _seData.GetCategoriesAsync();

        if (blocksResult.TryGetValue(out var blocks) && categoriesResult.TryGetValue(out var categories))
        {
            _allBlocks = blocks
                .Select(b => new BlockItem(
                    b.Id.TypeId,
                    b.Id.SubtypeId,
                    b.DisplayName,
                    b.CubeSize == "Large",
                    b.Dlc != null,
                    b.IconPath))
                .ToList();

            _categoryBlockIds = new Dictionary<string, HashSet<string>>();
            foreach (var cat in categories)
            {
                if (!_categoryBlockIds.TryGetValue(cat.DisplayName, out var ids))
                    _categoryBlockIds[cat.DisplayName] = ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var id in cat.Items)
                    ids.Add($"{id.TypeId}/{id.SubtypeId}");
            }

            BuildCategories(categories);
            LoadIconsInBackground(_allBlocks);
        }
        else
        {
            _allBlocks = [];
            Categories.Add(AllCategory);
        }

        var restoredCategory = Categories.FirstOrDefault(c => c.Name == State.CategoryName) ?? AllCategory;
        SelectedCategory = restoredCategory;
    }

    /// <summary>
    ///     Gets the block ID that was confirmed, or <c>null</c> if the selector was cancelled.
    ///     In type-only mode this is the <see cref="BlockItem.TypeId" />; otherwise the full
    ///     <see cref="BlockItem.Id" /> (TypeId/SubtypeId).
    /// </summary>
    public string? SelectedBlockId { get; private set; }

    /// <summary>
    ///     Gets or sets whether the selector returns any block of the selected type
    ///     rather than a specific block definition.
    /// </summary>
    public bool IsTypeOnly
    {
        get => _isTypeOnly;
        set
        {
            if (!SetProperty(ref _isTypeOnly, value)) return;
            OnPropertyChanged(nameof(IsSpecificBlock));
            RefreshItems();
        }
    }

    /// <summary>Inverse of <see cref="IsTypeOnly" />.</summary>
    public bool IsSpecificBlock
    {
        get => !_isTypeOnly;
        set => IsTypeOnly = !value;
    }

    /// <summary>
    ///     When <c>true</c>, type-only mode is locked on and the toggle is hidden.
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

    /// <summary>Whether the user can toggle <see cref="IsTypeOnly" />.</summary>
    public bool CanToggleTypeOnly => !_forceTypeOnly;

    /// <inheritdoc />
    protected override void OnConfirm()
    {
        SaveState();
        SelectedBlockId = _isTypeOnly ? SelectedItem?.TypeId : SelectedItem?.Id;
        base.OnConfirm();
    }

    /// <inheritdoc />
    protected override void OnCancel()
    {
        SaveState();
        base.OnCancel();
    }

    /// <inheritdoc />
    protected override void RefreshItems()
    {
        FilteredItemsCollection.Clear();
        IEnumerable<BlockItem> source = SelectedCategory is null || SelectedCategory == AllCategory
            ? _allBlocks
            : _categoryBlockIds.TryGetValue(SelectedCategory.Name, out var ids)
                ? _allBlocks.Where(b => ids.Contains($"{b.TypeId}/{b.SubtypeId}"))
                : [];
        if (!string.IsNullOrWhiteSpace(SearchText))
            source = source.Where(b => b.DisplayName.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
        if (_isTypeOnly)
            source = source
                .GroupBy(b => b.TypeId)
                .Select(g => g.OrderBy(b => b.IsDlc).ThenByDescending(b => b.IsLargeGrid).First());
        source = source.OrderBy(b => b.DisplayName, StringComparer.OrdinalIgnoreCase);
        foreach (var block in source)
            FilteredItemsCollection.Add(block);
        SelectedItem = null;
    }

    void SaveState()
    {
        State.CategoryName = SelectedCategory == AllCategory ? null : SelectedCategory?.Name;
        State.SearchText = SearchText;
        State.IsTypeOnly = _isTypeOnly;
    }

    void LoadIconsInBackground(List<BlockItem> blocks)
    {
        foreach (var block in blocks)
        {
            if (block.IconPath is not { } path)
                continue;
            _ = Task.Run(async () =>
            {
                var bitmap = await _images.LoadDdsAsync(path);
                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => block.Icon = bitmap);
            });
        }
    }

    void BuildCategories(IReadOnlyList<SpaceEngineers.BlockCategory> seCategories)
    {
        Categories.Add(AllCategory);
        foreach (var cat in seCategories)
            Categories.Add(new SelectorCategoryItem(cat.DisplayName, cat.IsSubCategory ? 1 : 0));
    }
}

