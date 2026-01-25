using System;
using Mdk.Hub.Features.Projects.Overview;
using Mdk.Hub.Utility;

namespace Mdk.Hub.Features.Projects;

/// <summary>
/// Flags for project behavior and testing.
/// </summary>
[Flags]
public enum ProjectFlags
{
    None = 0,
    
    /// <summary>
    /// Project is simulated for testing - won't be persisted to registry.
    /// </summary>
    Simulated = 1 << 0
}

/// <summary>
/// Data model representing a project managed by MDK Hub.
/// This is the data model that gets transformed into view models like ProjectListItem.
/// </summary>
public record ProjectInfo
{
    public required string Name { get; init; }
    public required CanonicalPath ProjectPath { get; init; }
    public required ProjectType Type { get; init; }
    public required DateTimeOffset LastReferenced { get; init; }
    public ProjectFlags Flags { get; init; } = ProjectFlags.None;
    public bool NeedsUpdate { get; init; }
    public int UpdateCount { get; init; }

    public bool IsPath(string projectPath) => ProjectPath == new CanonicalPath(projectPath);
}