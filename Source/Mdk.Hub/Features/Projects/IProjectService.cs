using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Mdk.Hub.Features.Projects.Configuration;
using Mdk.Hub.Features.Settings;
using Mdk.Hub.Utility;

namespace Mdk.Hub.Features.Projects;

/// <summary>
///     Unified service for accessing project data and configuration.
///     Provides both project listing/management and configuration reading.
/// </summary>
public interface IProjectService
{
    /// <summary>
    ///     Gets or sets the current project state (selected project and capabilities).
    ///     Setting this property raises StateChanged event.
    /// </summary>
    ProjectStateData State { get; set; }

    /// <summary>
    ///     Gets the settings service for storing user preferences.
    /// </summary>
    ISettings Settings { get; }

    /// <summary>
    ///     Raised when a new project is added to the registry (typically via build notification).
    /// </summary>
    event EventHandler<ProjectAddedEventArgs>? ProjectAdded;

    /// <summary>
    ///     Raised when a project is removed from the registry.
    /// </summary>
    event EventHandler<CanonicalPath>? ProjectRemoved;

    /// <summary>
    ///     Raised when navigation to a project is requested (e.g., from a toast notification).
    ///     View models should subscribe to this and handle the navigation.
    /// </summary>
    event EventHandler<ProjectNavigationRequestedEventArgs>? ProjectNavigationRequested;

    /// <summary>
    ///     Raised when updates are available for a project.
    /// </summary>
    event EventHandler<ProjectUpdateAvailableEventArgs>? ProjectUpdateAvailable;

    /// <summary>
    ///     Raised when project state (selected project, capabilities) changes.
    /// </summary>
    event EventHandler? StateChanged;

    /// <summary>
    ///     Gets all projects known to MDK Hub.
    /// </summary>
    /// <returns>Collection of project information.</returns>
    IReadOnlyList<ProjectInfo> GetProjects();

    /// <summary>
    ///     Attempts to add an existing project to the registry.
    /// </summary>
    /// <param name="projectPath">Path to the .csproj file.</param>
    /// <param name="errorMessage">Error message if the project is invalid.</param>
    /// <returns>True if the project was added successfully.</returns>
    bool TryAddProject(CanonicalPath projectPath, out string? errorMessage);

    /// <summary>
    ///     Removes a project from the registry.
    /// </summary>
    /// <param name="projectPath">Path to the .csproj file.</param>
    void RemoveProject(CanonicalPath projectPath);

    /// <summary>
    ///     Loads typed project configuration data with separate layers for Default, Main, and Local.
    /// </summary>
    /// <param name="projectPath">Path to the .csproj file.</param>
    /// <returns>Project configuration data with all layers, or null if project files not found.</returns>
    Task<ProjectData?> LoadProjectDataAsync(CanonicalPath projectPath);

    /// <summary>
    /// Normalizes the project configuration by making sure the key properties are stored in the
    /// appropriate layers, where things that logically should be machine-specific (e.g. output paths) are in the Local layer,
    /// and things that should be shared (e.g. script class name) are in the Main layer.
    /// </summary>
    /// <param name="projectData"></param>
    /// <returns></returns>
    Task<ProjectData> NormalizeConfigurationAsync(ProjectData projectData);    
    
    /// <summary>
    ///     Saves project configuration data back to INI files, preserving comments and custom keys.
    /// </summary>
    /// <param name="projectData">The project data to save.</param>
    Task SaveProjectDataAsync(ProjectData projectData);

    /// <summary>
    ///     Copies the deployed script to the clipboard.
    /// </summary>
    /// <param name="projectPath">Path to the .csproj file.</param>
    /// <returns>True if the script was copied successfully.</returns>
    Task<bool> CopyScriptToClipboardAsync(CanonicalPath projectPath);

    /// <summary>
    ///     Opens the project folder in the system file explorer.
    /// </summary>
    /// <param name="projectPath">Path to the .csproj file.</param>
    /// <returns>True if the folder was opened successfully.</returns>
    bool OpenProjectFolder(CanonicalPath projectPath);

    /// <summary>
    ///     Opens the output/deployment folder in the system file explorer.
    /// </summary>
    /// <param name="projectPath">Path to the .csproj file.</param>
    /// <returns>True if the folder was opened successfully.</returns>
    Task<bool> OpenOutputFolderAsync(CanonicalPath projectPath);

    /// <summary>
    ///     Opens the project in the default IDE by launching the .csproj file.
    /// </summary>
    /// <param name="projectPath">Path to the .csproj file.</param>
    /// <returns>True if the project was opened successfully.</returns>
    bool OpenProjectInIde(CanonicalPath projectPath);

    /// <summary>
    ///     Navigates to and selects the specified project in the Hub UI.
    /// </summary>
    /// <param name="projectPath">Path to the .csproj file.</param>
    /// <param name="openOptions">Whether to open the options drawer after navigation.</param>
    /// <param name="bringToFront">Whether to bring the Hub window to front and activate it.</param>
    bool NavigateToProject(CanonicalPath projectPath, bool openOptions = false, bool bringToFront = false);

    /// <summary>
    ///     Clears the update state for a project after packages have been successfully updated.
    /// </summary>
    /// <param name="projectPath">Path to the .csproj file.</param>
    void ClearProjectUpdateState(CanonicalPath projectPath);

    /// <summary>
    ///     Gets the current versions of MDK packages referenced in the project file.
    /// </summary>
    /// <param name="projectPath">Path to the .csproj file.</param>
    /// <returns>Dictionary of package IDs to their current versions, or empty if file cannot be read.</returns>
    IReadOnlyDictionary<string, string> GetMdkPackageVersions(CanonicalPath projectPath);

    /// <summary>
    ///     Gets cached update information for a project if available.
    /// </summary>
    /// <param name="projectPath">Path to the .csproj file.</param>
    /// <returns>Cached update information, or null if not available.</returns>
    IReadOnlyList<PackageUpdateInfo>? GetCachedUpdates(CanonicalPath projectPath);

    /// <summary>
    ///     Checks for available updates for MDK packages in the project.
    /// </summary>
    /// <param name="projectPath">Path to the .csproj file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of packages with available updates, or empty if all up-to-date or if check fails.</returns>
    Task<IReadOnlyList<PackageUpdateInfo>> CheckForPackageUpdatesAsync(CanonicalPath projectPath, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Updates MDK packages in the project file to their latest versions.
    /// </summary>
    /// <param name="projectPath">Path to the .csproj file.</param>
    /// <param name="packagesToUpdate">List of packages to update with their new versions.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the update was successful, false otherwise.</returns>
    Task<bool> UpdatePackagesAsync(CanonicalPath projectPath, IReadOnlyList<PackageUpdateInfo> packagesToUpdate, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Creates a new project.
    /// </summary>
    /// <param name="projectName">Name of the project (will be used for folder and project file).</param>
    /// <param name="location">Parent directory where the project folder will be created.</param>
    /// <param name="templateName">Name of the project template to use (e.g., "mdk2pbscript" or "mdk2mod").</param>
    /// <returns>Result containing path to the created .csproj file and optional error message.</returns>
    Task<(CanonicalPath? ProjectPath, string? ErrorMessage)> CreateProjectAsync(string projectName, string location, string templateName);
}
