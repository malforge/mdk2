using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Mdk.Hub.Features.Shell;
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
    string Category,
    bool IsLargeGrid = true,
    bool IsDlc = false)
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

    static readonly List<BlockItem> MockBlocks =
    [
        // Power
        new("WindTurbine",   "LargeBlockWindTurbine",       "Wind Turbine",              "Power"),
        new("Reactor",       "LargeBlockSmallGenerator",    "Reactor (Small)",            "Power"),
        new("Reactor",       "LargeBlockLargeGenerator",    "Reactor (Large)",            "Power"),
        new("Reactor",       "SmallBlockSmallGenerator",    "Reactor",                   "Power",  IsLargeGrid: false),
        new("BatteryBlock",  "LargeBlockBatteryBlock",      "Battery Block",             "Power"),
        new("BatteryBlock",  "SmallBlockBatteryBlock",      "Battery Block",             "Power",  IsLargeGrid: false),
        new("SolarPanel",    "LargeBlockSolarPanel",        "Solar Panel",               "Power"),
        new("SolarPanel",    "SmallBlockSolarPanel",        "Solar Panel",               "Power",  IsLargeGrid: false),
        new("Reactor",       "LargeBlockLargeGenerator_Sci","Reactor (Sci-Fi)",          "Power",  IsDlc: true),

        // Production
        new("Refinery",      "LargeRefineryBlock",          "Refinery",                  "Production"),
        new("Refinery",      "LargeBlastFurnace",           "Arc Furnace",               "Production"),
        new("Refinery",      "LargeBlastFurnace2",          "Basic Refinery",            "Production"),
        new("Assembler",     "LargeAssembler",              "Assembler",                 "Production"),
        new("Assembler",     "BasicAssembler",              "Basic Assembler",           "Production"),
        new("Assembler",     "LargeAssembler_SciFi",        "Assembler (Sci-Fi)",        "Production", IsDlc: true),
        new("SurvivalKit",   "SurvivalKit",                 "Survival Kit",              "Production"),

        // Movement
        new("Thrust",        "LargeBlockLargeThrust",            "Ion Thruster (Large)",          "Movement"),
        new("Thrust",        "LargeBlockSmallThrust",            "Ion Thruster (Small)",          "Movement"),
        new("Thrust",        "LargeBlockLargeHydrogenThrust",    "Hydrogen Thruster (Large)",     "Movement"),
        new("Thrust",        "LargeBlockSmallHydrogenThrust",    "Hydrogen Thruster (Small)",     "Movement"),
        new("Thrust",        "LargeBlockLargeAtmosphericThrust", "Atmospheric Thruster (Large)",  "Movement"),
        new("Thrust",        "LargeBlockSmallAtmosphericThrust", "Atmospheric Thruster (Small)",  "Movement"),
        new("Gyro",          "LargeBlockGyro",              "Gyroscope",                 "Movement"),
        new("Gyro",          "SmallBlockGyro",              "Gyroscope",                 "Movement", IsLargeGrid: false),

        // Weapons
        new("LargeGatlingTurret",   "LargeGatlingTurret",   "Gatling Turret",   "Weapons"),
        new("LargeRocketTurret",    "LargeRocketTurret",    "Rocket Turret",    "Weapons"),
        new("InteriorTurret",       "LargeInteriorTurret",  "Interior Turret",  "Weapons"),
        new("SmallGatlingGun",      "SmallGatlingGun",      "Gatling Gun",      "Weapons"),
        new("SmallMissileLauncher", "SmallMissileLauncher", "Rocket Launcher",  "Weapons"),

        // Communication
        new("LaserAntenna",  "LargeBlockLaserAntenna",  "Laser Antenna",  "Communication"),
        new("RadioAntenna",  "LargeBlockRadioAntenna",  "Antenna",        "Communication"),
        new("Beacon",        "LargeBlockBeacon",        "Beacon",         "Communication"),
        new("Beacon",        "SmallBlockBeacon",        "Beacon",         "Communication", IsLargeGrid: false),

        // Utility
        new("MyProgrammableBlock", "LargeProgrammableBlock",       "Programmable Block", "Utility"),
        new("TimerBlock",          "LargeTimerBlock",              "Timer Block",        "Utility"),
        new("SensorBlock",         "LargeBlockSensor",             "Sensor",             "Utility"),
        new("EventControllerBlock","LargeEventControllerBlock",    "Event Controller",   "Utility"),
    ];

    BlockCategoryItem? _selectedCategory;
    BlockItem? _selectedBlock;
    string? _selectedBlockId;
    string _searchText = "";
    bool _isTypeOnly = State.IsTypeOnly;
    bool _forceTypeOnly;

    /// <summary>
    ///     Initializes a new instance of <see cref="BlockSelectorViewModel" />.
    /// </summary>
    public BlockSelectorViewModel()
    {
        Categories = [];
        FilteredBlocks = [];
        OkCommand = new RelayCommand(Ok, CanOk);
        CancelCommand = new RelayCommand(Cancel);
        BuildCategories();
        _searchText = State.SearchText;
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

    void BuildCategories()
    {
        Categories.Add(AllCategory);
        foreach (var cat in MockBlocks.Select(b => b.Category).Distinct().Order())
            Categories.Add(new BlockCategoryItem(cat));
    }

    void RefreshFilteredBlocks()
    {
        FilteredBlocks.Clear();
        IEnumerable<BlockItem> source = _selectedCategory is null || _selectedCategory == AllCategory
            ? MockBlocks
            : MockBlocks.Where(b => b.Category == _selectedCategory.Name);
        if (!string.IsNullOrWhiteSpace(_searchText))
            source = source.Where(b => b.DisplayName.Contains(_searchText, StringComparison.OrdinalIgnoreCase));
        if (_isTypeOnly)
            source = source
                .GroupBy(b => b.TypeId)
                .Select(g => g.OrderBy(b => b.IsDlc).ThenByDescending(b => b.IsLargeGrid).First());
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

