using System.Collections.Generic;

namespace Mdk.Hub.Features.SpaceEngineers;

/// <summary>
///     Cache metadata used for staleness detection.
///     Stores the last-write UTC timestamps (ISO 8601) of each source file.
/// </summary>
internal sealed class BlocksCacheMeta
{
    /// <summary>Maps source file path to its last-write UTC time as ISO 8601 string.</summary>
    public Dictionary<string, string> Sources { get; set; } = new();
}