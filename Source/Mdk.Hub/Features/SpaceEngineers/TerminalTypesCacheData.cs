using System.Collections.Generic;

namespace Mdk.Hub.Features.SpaceEngineers;

/// <summary>
///     Cache data for the terminal block type ID set produced by the Cecil DLL scan.
///     Stored as <c>se-terminal-types.json</c> alongside the SBC block data cache.
/// </summary>
internal sealed class TerminalTypesCacheData
{
    /// <summary>Normalized TypeIds of all terminal block types (e.g., <c>"Thrust"</c>, <c>"Gyro"</c>).</summary>
    public List<string> TypeIds { get; set; } = [];
}