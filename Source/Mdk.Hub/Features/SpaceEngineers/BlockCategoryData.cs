using System.Collections.Generic;

namespace Mdk.Hub.Features.SpaceEngineers;

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