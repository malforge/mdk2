using System;
using System.Collections.Generic;
using Mdk.Hub.Features.NodeScript.Nodes;
using Mdk.Hub.Framework;

namespace Mdk.Hub.Features.NodeScript.Editors;

/// <summary>
///     ViewModel for editing Blocks node properties.
/// </summary>
public class BlocksNodeEditorViewModel : ViewModel
{
    readonly BlocksNodeViewModel _node;
    string? _pattern;
    string? _blockType;
    string? _groupName;
    string? _customDataSection;

    /// <summary>
    ///     Initializes a new instance of the <see cref="BlocksNodeEditorViewModel"/> class.
    /// </summary>
    /// <param name="node">The blocks node to edit.</param>
    public BlocksNodeEditorViewModel(BlocksNodeViewModel node)
    {
        _node = node;
        
        // Load current values from node
        _pattern = node.Pattern;
        _blockType = node.BlockType;
        _groupName = node.GroupName;
        _customDataSection = node.CustomDataSection;
        
        SaveCommand = new RelayCommand(Save);
        CancelCommand = new RelayCommand(Cancel);
    }

    /// <summary>
    ///     Gets the list of available block types from Space Engineers.
    /// </summary>
    public IEnumerable<string> AvailableBlockTypes { get; } = new[]
    {
        "MyObjectBuilder_Assembler",
        "MyObjectBuilder_BatteryBlock",
        "MyObjectBuilder_Beacon",
        "MyObjectBuilder_ButtonPanel",
        "MyObjectBuilder_CargoContainer",
        "MyObjectBuilder_Cockpit",
        "MyObjectBuilder_Collector",
        "MyObjectBuilder_ConveyorSorter",
        "MyObjectBuilder_Door",
        "MyObjectBuilder_Drill",
        "MyObjectBuilder_GasGenerator",
        "MyObjectBuilder_GasTank",
        "MyObjectBuilder_Gyro",
        "MyObjectBuilder_InteriorLight",
        "MyObjectBuilder_JumpDrive",
        "MyObjectBuilder_LandingGear",
        "MyObjectBuilder_LaserAntenna",
        "MyObjectBuilder_LightingBlock",
        "MyObjectBuilder_MedicalRoom",
        "MyObjectBuilder_MergeBlock",
        "MyObjectBuilder_MotorStator",
        "MyObjectBuilder_MotorAdvancedStator",
        "MyObjectBuilder_OreDetector",
        "MyObjectBuilder_OxygenFarm",
        "MyObjectBuilder_OxygenGenerator",
        "MyObjectBuilder_OxygenTank",
        "MyObjectBuilder_Parachute",
        "MyObjectBuilder_Piston",
        "MyObjectBuilder_ProgrammableBlock",
        "MyObjectBuilder_Projector",
        "MyObjectBuilder_RadioAntenna",
        "MyObjectBuilder_Reactor",
        "MyObjectBuilder_Refinery",
        "MyObjectBuilder_ReflectorLight",
        "MyObjectBuilder_RemoteControl",
        "MyObjectBuilder_SensorBlock",
        "MyObjectBuilder_ShipConnector",
        "MyObjectBuilder_ShipGrinder",
        "MyObjectBuilder_ShipWelder",
        "MyObjectBuilder_SmallGatlingGun",
        "MyObjectBuilder_SmallMissileLauncher",
        "MyObjectBuilder_SmallMissileLauncherReload",
        "MyObjectBuilder_SolarPanel",
        "MyObjectBuilder_SoundBlock",
        "MyObjectBuilder_SpaceBall",
        "MyObjectBuilder_TextPanel",
        "MyObjectBuilder_Thrust",
        "MyObjectBuilder_TimerBlock",
        "MyObjectBuilder_UpgradeModule",
        "MyObjectBuilder_Warhead",
        "MyObjectBuilder_Wheel"
    };

    /// <summary>
    ///     Gets or sets the block name pattern filter.
    /// </summary>
    public string? Pattern
    {
        get => _pattern;
        set
        {
            if (SetProperty(ref _pattern, value))
                OnPropertyChanged(nameof(HasNoFilters));
        }
    }

    /// <summary>
    ///     Gets or sets the block type filter.
    /// </summary>
    public string? BlockType
    {
        get => _blockType;
        set
        {
            if (SetProperty(ref _blockType, value))
                OnPropertyChanged(nameof(HasNoFilters));
        }
    }

    /// <summary>
    ///     Gets or sets the group name filter.
    /// </summary>
    public string? GroupName
    {
        get => _groupName;
        set
        {
            if (SetProperty(ref _groupName, value))
                OnPropertyChanged(nameof(HasNoFilters));
        }
    }

    /// <summary>
    ///     Gets or sets the CustomData INI section filter.
    /// </summary>
    public string? CustomDataSection
    {
        get => _customDataSection;
        set
        {
            if (SetProperty(ref _customDataSection, value))
                OnPropertyChanged(nameof(HasNoFilters));
        }
    }

    /// <summary>
    ///     Gets whether no filters are set (warning condition).
    /// </summary>
    public bool HasNoFilters =>
        string.IsNullOrWhiteSpace(Pattern) &&
        string.IsNullOrWhiteSpace(BlockType) &&
        string.IsNullOrWhiteSpace(GroupName) &&
        string.IsNullOrWhiteSpace(CustomDataSection);

    /// <summary>
    ///     Gets the command to save changes.
    /// </summary>
    public RelayCommand SaveCommand { get; }

    /// <summary>
    ///     Gets the command to cancel editing.
    /// </summary>
    public RelayCommand CancelCommand { get; }

    void Save()
    {
        // Update node with all filter values (null if empty)
        _node.Pattern = string.IsNullOrWhiteSpace(Pattern) ? null : Pattern;
        _node.BlockType = string.IsNullOrWhiteSpace(BlockType) ? null : BlockType;
        _node.GroupName = string.IsNullOrWhiteSpace(GroupName) ? null : GroupName;
        _node.CustomDataSection = string.IsNullOrWhiteSpace(CustomDataSection) ? null : CustomDataSection;
        
        CloseRequested?.Invoke();
    }

    void Cancel()
    {
        CloseRequested?.Invoke();
    }

    /// <summary>
    ///     Raised when the editor should be closed.
    /// </summary>
    public event Action? CloseRequested;
}
