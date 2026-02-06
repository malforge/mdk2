using System;
using Mdk.Hub.Features.Projects.Overview;
using Mdk.Hub.Utility;

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

/// <summary>
///     Represents a project registered in the MDK Hub.
///     Contains metadata and state information for display and management.
/// </summary>
public record ProjectInfo
{
    /// <summary>
    ///     Gets the display name of the project.
    /// </summary>
    public required string Name { get; init; }
    
    /// <summary>
    ///     Gets the canonical path to the .csproj file.
    /// </summary>
    public required CanonicalPath ProjectPath { get; init; }
    
    /// <summary>
    ///     Gets the type of project (Programmable Block script or Mod).
    /// </summary>
    public required ProjectType Type { get; init; }
    
    /// <summary>
    ///     Gets the timestamp when the project was last opened or modified.
    /// </summary>
    public required DateTimeOffset LastReferenced { get; init; }
    
    /// <summary>
    ///     Gets behavioral flags for the project (e.g., Simulated for testing).
    /// </summary>
    public ProjectFlags Flags { get; init; } = ProjectFlags.None;
    
    /// <summary>
    ///     Gets whether MDK package updates are available for this project.
    /// </summary>
    public bool NeedsUpdate { get; init; }
    
    /// <summary>
    ///     Gets the number of outdated packages in this project.
    /// </summary>
    public int UpdateCount { get; init; }

    /// <summary>
    ///     Determines whether the given path matches this project's path.
    /// </summary>
    /// <param name="projectPath">The path to compare.</param>
    /// <returns>True if the paths match.</returns>
    public bool IsPath(string projectPath) => ProjectPath == new CanonicalPath(projectPath);
}
