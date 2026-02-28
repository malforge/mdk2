using System;
using Avalonia;
using Mdk.Hub.Features.NodeScript.Editors;
using Mdk.Hub.Framework;

namespace Mdk.Hub.Features.NodeScript.Nodes;

/// <summary>
///     ViewModel for a Blocks node (data source for blocks in Space Engineers).
/// </summary>
public class BlocksNodeViewModel : ViewModel, INodeEditor
{
    Point _location;
    string? _pattern;
    string? _blockType;
    string? _groupName;
    string? _customDataSection;

    /// <summary>
    ///     Gets or sets the node location on the canvas.
    /// </summary>
    public Point Location
    {
        get => _location;
        set => SetProperty(ref _location, value);
    }

    /// <summary>
    ///     Gets or sets the block name pattern filter (optional).
    /// </summary>
    public string? Pattern
    {
        get => _pattern;
        set
        {
            if (SetProperty(ref _pattern, value))
                OnPropertyChanged(nameof(Title));
        }
    }

    /// <summary>
    ///     Gets or sets the block type filter (optional).
    /// </summary>
    public string? BlockType
    {
        get => _blockType;
        set
        {
            if (SetProperty(ref _blockType, value))
                OnPropertyChanged(nameof(Title));
        }
    }

    /// <summary>
    ///     Gets or sets the group name filter (optional).
    /// </summary>
    public string? GroupName
    {
        get => _groupName;
        set
        {
            if (SetProperty(ref _groupName, value))
                OnPropertyChanged(nameof(Title));
        }
    }

    /// <summary>
    ///     Gets or sets the CustomData INI section filter (optional, advanced).
    /// </summary>
    public string? CustomDataSection
    {
        get => _customDataSection;
        set
        {
            if (SetProperty(ref _customDataSection, value))
                OnPropertyChanged(nameof(Title));
        }
    }

    /// <summary>
    ///     Gets the node title for display.
    /// </summary>
    public string Title
    {
        get
        {
            // Build title from active filters
            var parts = new System.Collections.Generic.List<string>();
            if (!string.IsNullOrWhiteSpace(Pattern)) parts.Add($"Name:{Pattern}");
            if (!string.IsNullOrWhiteSpace(BlockType)) parts.Add($"Type:{BlockType}");
            if (!string.IsNullOrWhiteSpace(GroupName)) parts.Add($"Group:{GroupName}");
            if (!string.IsNullOrWhiteSpace(CustomDataSection)) parts.Add($"INI:{CustomDataSection}");

            return parts.Count > 0 ? $"Blocks: {string.Join(", ", parts)}" : "Blocks: (all)";
        }
    }

    /// <inheritdoc />
    public (object viewModel, Type viewType) GetEditor()
    {
        var editorViewModel = new BlocksNodeEditorViewModel(this);
        return (editorViewModel, typeof(BlocksNodeEditorView));
    }
}
