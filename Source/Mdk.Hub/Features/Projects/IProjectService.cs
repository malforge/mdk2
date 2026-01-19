using System.Collections.Generic;
using Mdk.Hub.Features.Projects.Configuration;

namespace Mdk.Hub.Features.Projects;

/// <summary>
/// Unified service for accessing project data and configuration.
/// Provides both project listing/management and configuration reading.
/// </summary>
public interface IProjectService
{
    /// <summary>
    /// Gets all projects known to MDK Hub.
    /// </summary>
    /// <returns>Collection of project information.</returns>
    IReadOnlyList<ProjectInfo> GetProjects();

    /// <summary>
    /// Attempts to add an existing project to the registry.
    /// </summary>
    /// <param name="projectPath">Path to the .csproj file.</param>
    /// <param name="errorMessage">Error message if the project is invalid.</param>
    /// <returns>True if the project was added successfully.</returns>
    bool TryAddProject(string projectPath, out string? errorMessage);

    /// <summary>
    /// Removes a project from the registry.
    /// </summary>
    /// <param name="projectPath">Path to the .csproj file.</param>
    void RemoveProject(string projectPath);

    /// <summary>
    /// Loads and merges configuration from mdk.ini and mdk.local.ini for the specified project.
    /// Local settings override main settings where both are present.
    /// </summary>
    /// <param name="projectPath">Path to the .csproj file.</param>
    /// <returns>Merged project configuration, or null if no configuration files found.</returns>
    ProjectConfiguration? LoadConfiguration(string projectPath);
}
