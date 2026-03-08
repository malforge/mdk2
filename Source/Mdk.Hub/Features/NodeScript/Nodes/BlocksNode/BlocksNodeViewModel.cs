using System.Collections.Generic;
using System.Windows.Input;
using Mdk.Hub.Framework;

namespace Mdk.Hub.Features.NodeScript.Nodes;

/// <summary>
///     ViewModel for a Blocks node (data source for blocks in Space Engineers).
/// </summary>
[ViewModelFor<BlocksNodeView>]
public class BlocksNodeViewModel : ComplexNodeViewModel
{
    string? _pattern;
    string? _blockType;
    string? _groupName;
    string? _customDataSection;

    /// <inheritdoc />
    public override string Title => "Blocks";

    /// <summary>Gets or sets the block name pattern filter (optional).</summary>
    public string? Pattern
    {
        get => _pattern;
        set
        {
            if (SetProperty(ref _pattern, value))
            {
                OnPropertyChanged(nameof(Summary));
                OnPropertyChanged(nameof(HasNoFilters));
            }
        }
    }

    /// <summary>Gets or sets the block type filter (optional).</summary>
    public string? BlockType
    {
        get => _blockType;
        set
        {
            if (SetProperty(ref _blockType, value))
            {
                OnPropertyChanged(nameof(Summary));
                OnPropertyChanged(nameof(HasNoFilters));
                OnPropertyChanged(nameof(BlockTypeDisplay));
            }
        }
    }

    /// <summary>Gets or sets the group name filter (optional).</summary>
    public string? GroupName
    {
        get => _groupName;
        set
        {
            if (SetProperty(ref _groupName, value))
            {
                OnPropertyChanged(nameof(Summary));
                OnPropertyChanged(nameof(HasNoFilters));
            }
        }
    }

    /// <summary>Gets or sets the CustomData INI section filter (optional, advanced).</summary>
    public string? CustomDataSection
    {
        get => _customDataSection;
        set
        {
            if (SetProperty(ref _customDataSection, value))
            {
                OnPropertyChanged(nameof(Summary));
                OnPropertyChanged(nameof(HasNoFilters));
            }
        }
    }

    /// <summary>Gets or sets the command to open the block type picker. Set by the editor when the node is created.</summary>
    public ICommand? BrowseBlockTypeCommand { get; set; }

    /// <summary>Gets the display label for the block type picker button.</summary>
    public string BlockTypeDisplay => string.IsNullOrWhiteSpace(BlockType) ? "Any type" : BlockType;

    /// <summary>Gets whether no filters are set (meaning the node will match all blocks).</summary>
    public bool HasNoFilters =>
        string.IsNullOrWhiteSpace(Pattern) &&
        string.IsNullOrWhiteSpace(BlockType) &&
        string.IsNullOrWhiteSpace(GroupName) &&
        string.IsNullOrWhiteSpace(CustomDataSection);

    /// <summary>Gets the output connectors for this node (one: the block collection).</summary>
    public IReadOnlyList<ConnectorViewModel> Outputs { get; } =
        [new ConnectorViewModel { IsOutput = true }];

    /// <inheritdoc />
    public override string Summary
    {
        get
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(Pattern)) parts.Add($"Name: {Pattern}");
            if (!string.IsNullOrWhiteSpace(BlockType)) parts.Add($"Type: {BlockType}");
            if (!string.IsNullOrWhiteSpace(GroupName)) parts.Add($"Group: {GroupName}");
            if (!string.IsNullOrWhiteSpace(CustomDataSection)) parts.Add($"INI: {CustomDataSection}");
            return parts.Count > 0 ? string.Join(", ", parts) : "(all blocks)";
        }
    }
}

