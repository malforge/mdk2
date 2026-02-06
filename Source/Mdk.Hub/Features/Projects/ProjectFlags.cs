using System;

namespace Mdk.Hub.Features.Projects;

/// <summary>
///     Flags for project behavior and testing.
/// </summary>
[Flags]
public enum ProjectFlags
{
    /// <summary>
    ///     No special flags set.
    /// </summary>
    None = 0,

    /// <summary>
    ///     Project is simulated for testing - won't be persisted to registry.
    /// </summary>
    Simulated = 1 << 0
}