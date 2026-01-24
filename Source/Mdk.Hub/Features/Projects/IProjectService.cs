using System.Collections.Generic;
using System.Threading.Tasks;
using Mdk.Hub.Features.Projects.Configuration;

namespace Mdk.Hub.Features.Projects;

/// <summary>
/// Indicates how a project was added to the registry.
/// </summary>
public enum ProjectAdditionSource
{
    /// <summary>
    /// User manually added the project via UI.
    /// </summary>
    Manual,
    
    /// <summary>
    /// Project was added via build notification (IPC).
    /// </summary>
    BuildNotification,
    
    /// <summary>
    /// Project was loaded from registry on startup.
    /// </summary>
    Startup
}

/// <summary>
/// Event arguments for when a new project is added to the registry.
/// </summary>
public class ProjectAddedEventArgs : System.EventArgs
{
    public string ProjectPath { get; init; } = string.Empty;
    public ProjectAdditionSource Source { get; init; }
}

/// <summary>
/// Unified service for accessing project data and configuration.
/// Provides both project listing/management and configuration reading.
/// </summary>
public interface IProjectService
{
    /// <summary>
    /// Raised when a new project is added to the registry (typically via build notification).
    /// </summary>
    event System.EventHandler<ProjectAddedEventArgs>? ProjectAdded;
    
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

    /// <summary>
    /// Saves configuration changes to the specified INI file.
    /// </summary>
    /// <param name="projectPath">Path to the .csproj file.</param>
    /// <param name="output">Output path value.</param>
    /// <param name="binaryPath">Binary path value.</param>
    /// <param name="minify">Minify value.</param>
    /// <param name="minifyExtraOptions">MinifyExtraOptions value.</param>
    /// <param name="trace">Trace value.</param>
    /// <param name="ignores">Ignores value.</param>
    /// <param name="namespaces">Namespaces value.</param>
    /// <param name="saveToLocal">True to save to mdk.local.ini, false to save to mdk.ini.</param>
    Task SaveConfiguration(string projectPath, string output, string binaryPath, string minify, string minifyExtraOptions, string trace, string ignores, string namespaces, bool saveToLocal);
}
