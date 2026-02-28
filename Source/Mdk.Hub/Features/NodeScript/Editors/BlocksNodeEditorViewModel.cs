using System;
using System.Threading.Tasks;
using Mal.SourceGeneratedDI;
using Mdk.Hub.Features.NodeScript.BlockSelector;
using Mdk.Hub.Features.NodeScript.Nodes;
using Mdk.Hub.Features.Shell;
using Mdk.Hub.Framework;

namespace Mdk.Hub.Features.NodeScript.Editors;

/// <summary>
///     ViewModel for editing Blocks node properties.
/// </summary>
[ViewModelFor<BlocksNodeEditorView>]
public class BlocksNodeEditorViewModel : OverlayModel
{
    readonly BlocksNodeViewModel _node;
    readonly IBlockPickerService _blockPicker;
    readonly IOverlayService _overlayService;
    string? _pattern;
    string? _blockType;
    string? _groupName;
    string? _customDataSection;

    /// <summary>
    ///     Initializes a new instance of the <see cref="BlocksNodeEditorViewModel"/> class.
    /// </summary>
    public BlocksNodeEditorViewModel(BlocksNodeViewModel node, IBlockPickerService blockPicker, IOverlayService overlayService)
    {
        _node = node;
        _blockPicker = blockPicker;
        _overlayService = overlayService;

        _pattern = node.Pattern;
        _blockType = node.BlockType;
        _groupName = node.GroupName;
        _customDataSection = node.CustomDataSection;

        SaveCommand = new RelayCommand(Save);
        CancelCommand = new RelayCommand(Cancel);
        BrowseBlockTypeCommand = new AsyncRelayCommand(BrowseBlockTypeAsync);
    }

    /// <summary>Gets the command that opens the block type picker.</summary>
    public AsyncRelayCommand BrowseBlockTypeCommand { get; }

    /// <summary>Gets or sets the block name pattern filter.</summary>
    public string? Pattern
    {
        get => _pattern;
        set
        {
            if (SetProperty(ref _pattern, value))
                OnPropertyChanged(nameof(HasNoFilters));
        }
    }

    /// <summary>Gets or sets the block type filter.</summary>
    public string? BlockType
    {
        get => _blockType;
        set
        {
            if (SetProperty(ref _blockType, value))
                OnPropertyChanged(nameof(HasNoFilters));
        }
    }

    /// <summary>Gets or sets the group name filter.</summary>
    public string? GroupName
    {
        get => _groupName;
        set
        {
            if (SetProperty(ref _groupName, value))
                OnPropertyChanged(nameof(HasNoFilters));
        }
    }

    /// <summary>Gets or sets the CustomData INI section filter.</summary>
    public string? CustomDataSection
    {
        get => _customDataSection;
        set
        {
            if (SetProperty(ref _customDataSection, value))
                OnPropertyChanged(nameof(HasNoFilters));
        }
    }

    /// <summary>Gets whether no filters are set (warning condition).</summary>
    public bool HasNoFilters =>
        string.IsNullOrWhiteSpace(Pattern) &&
        string.IsNullOrWhiteSpace(BlockType) &&
        string.IsNullOrWhiteSpace(GroupName) &&
        string.IsNullOrWhiteSpace(CustomDataSection);

    /// <summary>Gets the command to save changes.</summary>
    public RelayCommand SaveCommand { get; }

    /// <summary>Gets the command to cancel editing.</summary>
    public RelayCommand CancelCommand { get; }

    async Task BrowseBlockTypeAsync()
    {
        var picked = await _blockPicker.PickAsync(_overlayService);
        if (picked is not null)
            BlockType = picked.TypeId;
    }

    void Save()
    {
        _node.Pattern = string.IsNullOrWhiteSpace(Pattern) ? null : Pattern;
        _node.BlockType = string.IsNullOrWhiteSpace(BlockType) ? null : BlockType;
        _node.GroupName = string.IsNullOrWhiteSpace(GroupName) ? null : GroupName;
        _node.CustomDataSection = string.IsNullOrWhiteSpace(CustomDataSection) ? null : CustomDataSection;
        Dismiss();
    }

    void Cancel() => Dismiss();
}
