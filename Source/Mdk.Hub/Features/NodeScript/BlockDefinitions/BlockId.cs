using System;

namespace Mdk.Hub.Features.NodeScript.BlockDefinitions;

/// <summary>
///     Identifies a Space Engineers block by TypeId and SubtypeId.
///     TypeIds never include the <c>MyObjectBuilder_</c> prefix.
/// </summary>
/// <param name="TypeId">The block type (e.g., <c>Refinery</c>).</param>
/// <param name="SubtypeId">The block subtype (e.g., <c>LargeRefinery</c>). May be an empty string.</param>
public readonly record struct BlockId(string TypeId, string SubtypeId)
{
    /// <summary>Returns the canonical <c>TypeId/SubtypeId</c> string representation.</summary>
    public override string ToString() => $"{TypeId}/{SubtypeId}";

    /// <summary>
    ///     Parses a <c>TypeId/SubtypeId</c> string, stripping the <c>MyObjectBuilder_</c> prefix from the TypeId if present.
    /// </summary>
    public static BlockId Parse(string value)
    {
        var slash = value.IndexOf('/');
        if (slash < 0)
            return new BlockId(Normalize(value), string.Empty);
        return new BlockId(Normalize(value[..slash]), value[(slash + 1)..]);
    }

    static string Normalize(string typeId)
    {
        const string prefix = "MyObjectBuilder_";
        return typeId.StartsWith(prefix, StringComparison.Ordinal) ? typeId[prefix.Length..] : typeId;
    }
}
