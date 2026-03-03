namespace Mdk.Hub.Features.SpaceEngineers;

/// <summary>Cache representation of a <see cref="BlockInfo" />.</summary>
internal sealed class BlockInfoData
{
    /// <summary>TypeId without the <c>MyObjectBuilder_</c> prefix.</summary>
    public string TypeId { get; set; } = string.Empty;

    /// <summary>SubtypeId. May be empty.</summary>
    public string SubtypeId { get; set; } = string.Empty;

    /// <summary>Resolved English display name.</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Icon path relative to <c>Content/</c>, or <c>null</c>.</summary>
    public string? IconPath { get; set; }

    /// <summary>Grid size: <c>"Large"</c> or <c>"Small"</c>.</summary>
    public string CubeSize { get; set; } = "Large";

    /// <summary>DLC identifier required to use this block, or <c>null</c> if base-game.</summary>
    public string? Dlc { get; set; }
}