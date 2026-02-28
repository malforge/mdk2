using System.Collections.Generic;

namespace Mdk.Hub.Features.SpaceEngineers;

/// <summary>Cache file format for serialized block definition data.</summary>
internal sealed class BlocksCacheData
{
    /// <summary>All loaded block categories.</summary>
    public List<BlockCategoryData> Categories { get; set; } = [];

    /// <summary>All loaded block definitions.</summary>
    public List<BlockInfoData> Blocks { get; set; } = [];
}

/// <summary>Cache representation of a <see cref="BlockCategory" />.</summary>
internal sealed class BlockCategoryData
{
    /// <summary>Sort key and unique identifier.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Resolved English display name (leading spaces stripped).</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Whether this category was indented (3 leading spaces in SBC).</summary>
    public bool IsSubCategory { get; set; }

    /// <summary>Block IDs belonging to this category in SBC file order.</summary>
    public List<BlockIdData> Items { get; set; } = [];
}

/// <summary>Cache representation of a <see cref="BlockId" />.</summary>
internal sealed class BlockIdData
{
    /// <summary>TypeId without the <c>MyObjectBuilder_</c> prefix.</summary>
    public string TypeId { get; set; } = string.Empty;

    /// <summary>SubtypeId. May be empty.</summary>
    public string SubtypeId { get; set; } = string.Empty;
}

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
}

/// <summary>
///     Cache metadata used for staleness detection.
///     Stores the last-write UTC timestamps (ISO 8601) of each source file.
/// </summary>
internal sealed class BlocksCacheMeta
{
    /// <summary>Maps source file path to its last-write UTC time as ISO 8601 string.</summary>
    public Dictionary<string, string> Sources { get; set; } = new();
}
