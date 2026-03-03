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