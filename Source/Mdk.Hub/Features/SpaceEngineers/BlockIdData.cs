namespace Mdk.Hub.Features.SpaceEngineers;

/// <summary>Cache representation of a <see cref="BlockId" />.</summary>
internal sealed class BlockIdData
{
    /// <summary>TypeId without the <c>MyObjectBuilder_</c> prefix.</summary>
    public string TypeId { get; set; } = string.Empty;

    /// <summary>SubtypeId. May be empty.</summary>
    public string SubtypeId { get; set; } = string.Empty;
}