using System;
using Mdk.Hub.Features.Projects.Overview;

namespace Mdk.Hub.Features.Projects;

/// <summary>
/// Data model representing a project managed by MDK Hub.
/// This is the data model that gets transformed into view models like ProjectListItem.
/// </summary>
public record ProjectInfo
{
    public required string Name { get; init; }
    public required string ProjectPath { get; init; }
    public required ProjectType Type { get; init; }
    public required DateTimeOffset LastReferenced { get; init; }
}
