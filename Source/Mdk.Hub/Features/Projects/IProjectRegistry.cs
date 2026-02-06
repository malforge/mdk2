using System.Collections.Generic;

namespace Mdk.Hub.Features.Projects;

/// <summary>
///     Interface for managing the registry of known MDK projects.
/// </summary>
public interface IProjectRegistry
{
    /// <summary>
    ///     Gets all registered projects.
    /// </summary>
    IReadOnlyList<ProjectInfo> GetProjects();

    /// <summary>
    ///     Adds or updates a project in the registry.
    /// </summary>
    void AddOrUpdateProject(ProjectInfo project);

    /// <summary>
    ///     Removes a project from the registry.
    /// </summary>
    void RemoveProject(string projectPath);
}