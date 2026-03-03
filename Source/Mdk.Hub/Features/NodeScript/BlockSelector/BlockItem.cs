using Avalonia.Media.Imaging;
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