using System.Collections.Generic;
using System.Threading.Tasks;
using Mdk.Hub.Features.Projects.Configuration;
using Mdk.Hub.Features.Settings;
using Mdk.Hub.Utility;

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
    public required CanonicalPath ProjectPath { get; init; }
    public ProjectAdditionSource Source { get; init; }
}

/// <summary>
/// Event arguments for when navigation to a project is requested.
/// </summary>
public class ProjectNavigationRequestedEventArgs : System.EventArgs
{
    public required CanonicalPath ProjectPath { get; init; }
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
    /// Raised when a project is removed from the registry.
    /// </summary>
    event System.EventHandler<CanonicalPath>? ProjectRemoved;
    
    /// <summary>
    /// Raised when navigation to a project is requested (e.g., from a toast notification).
    /// View models should subscribe to this and handle the navigation.
    /// </summary>
    event System.EventHandler<ProjectNavigationRequestedEventArgs>? ProjectNavigationRequested;
    
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
    bool TryAddProject(CanonicalPath projectPath, out string? errorMessage);

    /// <summary>
    /// Removes a project from the registry.
    /// </summary>
    /// <param name="projectPath">Path to the .csproj file.</param>
    void RemoveProject(CanonicalPath projectPath);

    /// <summary>
    /// Loads and merges configuration from mdk.ini and mdk.local.ini for the specified project.
    /// Local settings override main settings where both are present.
    /// </summary>
    /// <param name="projectPath">Path to the .csproj file.</param>
    /// <returns>Merged project configuration, or null if no configuration files found.</returns>
    ProjectConfiguration? LoadConfiguration(CanonicalPath projectPath);

    /// <summary>
    /// Saves configuration changes to the specified INI file.
    /// </summary>
    /// <param name="projectPath">Path to the .csproj file.</param>
    /// <param name="interactive">Interactive/notification behavior value.</param>
    /// <param name="output">Output path value.</param>
    /// <param name="binaryPath">Binary path value.</param>
    /// <param name="minify">Minify value.</param>
    /// <param name="minifyExtraOptions">MinifyExtraOptions value.</param>
    /// <param name="trace">Trace value.</param>
    /// <param name="ignores">Ignores value.</param>
    /// <param name="namespaces">Namespaces value.</param>
    /// <param name="saveToLocal">True to save to mdk.local.ini, false to save to mdk.ini.</param>
    Task SaveConfiguration(CanonicalPath projectPath, string interactive, string output, string binaryPath, string minify, string minifyExtraOptions, string trace, string ignores, string namespaces, bool saveToLocal);

    /// <summary>
    /// Copies the deployed script to the clipboard.
    /// </summary>
    /// <param name="projectPath">Path to the .csproj file.</param>
    /// <returns>True if the script was copied successfully.</returns>
    Task<bool> CopyScriptToClipboardAsync(CanonicalPath projectPath);

    /// <summary>
    /// Opens the project folder in the system file explorer.
    /// </summary>
    /// <param name="projectPath">Path to the .csproj file.</param>
    /// <returns>True if the folder was opened successfully.</returns>
    bool OpenProjectFolder(CanonicalPath projectPath);

    /// <summary>
    /// Opens the output/deployment folder in the system file explorer.
    /// </summary>
    /// <param name="projectPath">Path to the .csproj file.</param>
    /// <returns>True if the folder was opened successfully.</returns>
    bool OpenOutputFolder(CanonicalPath projectPath);

    /// <summary>
    /// Navigates to and selects the specified project in the Hub UI.
    /// </summary>
    /// <param name="projectPath">Path to the .csproj file.</param>
    bool NavigateToProject(CanonicalPath projectPath);

    /// <summary>
    /// Creates a new Programmable Block script project.
    /// </summary>
    /// <param name="projectName">Name of the project (will be used for folder and project file).</param>
    /// <param name="location">Parent directory where the project folder will be created.</param>
    /// <returns>Result containing path to the created .csproj file and optional error message.</returns>
    Task<(CanonicalPath? ProjectPath, string? ErrorMessage)> CreateProgrammableBlockProjectAsync(string projectName, string location);

    /// <summary>
    /// Creates a new Mod project.
    /// </summary>
    /// <param name="projectName">Name of the project (will be used for folder and project file).</param>
    /// <param name="location">Parent directory where the project folder will be created.</param>
    /// <returns>Result containing path to the created .csproj file and optional error message.</returns>
    Task<(CanonicalPath? ProjectPath, string? ErrorMessage)> CreateModProjectAsync(string projectName, string location);
    
    /// <summary>
    /// Gets the settings service for storing user preferences.
    /// </summary>
    ISettings Settings { get; }
}
