using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Mal.SourceGeneratedDI;
using Mdk.Hub.Features.Diagnostics;
using Mdk.Hub.Features.Interop;
using Mdk.Hub.Features.Projects.Configuration;
using Mdk.Hub.Features.Projects.Overview;
using Mdk.Hub.Features.Settings;
using Mdk.Hub.Features.Shell;
using Mdk.Hub.Features.Snackbars;
using Mdk.Hub.Features.Storage;
using Mdk.Hub.Features.Updates;
using Mdk.Hub.Utility;
using NuGet.Versioning;

namespace Mdk.Hub.Features.Projects;

/// <summary>
///     Core service for managing MDK projects, including creation, configuration, and updates.
/// </summary>
[Singleton<IProjectService>]
public class ProjectService : IProjectService
{
    readonly IFileStorageService _fileStorage;
    readonly ILogger _logger;
    readonly INuGetService _nugetService;
    readonly IProjectRegistry _registry;
    readonly ISettings _settings;
    readonly IShell _shell;
    readonly ISnackbarService _snackbarService;
    readonly ProjectUpdateChecker _updateChecker;
    readonly Dictionary<CanonicalPath, (bool needsUpdate, int updateCount, IReadOnlyList<PackageUpdateInfo> updates)> _updateStates = new();
    readonly Lock _updateStatesLock = new();

    ProjectStateData _state;

    /// <summary>
    ///     Initializes a new instance of the ProjectService class.
    /// </summary>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <param name="registry">Project registry for persistence.</param>
    /// <param name="ipc">Inter-process communication for build notifications.</param>
    /// <param name="shell">Shell service for UI interactions.</param>
    /// <param name="snackbarService">Service for displaying snackbar notifications.</param>
    /// <param name="settings">Settings service.</param>
    /// <param name="updateManager">Update manager for checking package updates.</param>
    /// <param name="nugetService">NuGet service for package operations.</param>
    /// <param name="fileStorage">File storage service for filesystem operations.</param>
    public ProjectService(ILogger logger, IProjectRegistry registry, IInterProcessCommunication ipc, IShell shell, ISnackbarService snackbarService, ISettings settings, IUpdateManager updateManager, INuGetService nugetService, IFileStorageService fileStorage)
    {
        _registry = registry;
        _logger = logger;
        _shell = shell;
        _snackbarService = snackbarService;
        _settings = settings;
        _fileStorage = fileStorage;
        Settings = settings;
        _nugetService = nugetService;
        _updateChecker = new ProjectUpdateChecker(logger, this, updateManager, registry);
        _state = new ProjectStateData(default, true, true);

        _updateChecker.ProjectUpdateAvailable += OnProjectUpdateAvailable;

        // Subscribe to IPC messages
        ipc.MessageReceived += (_, e) => HandleBuildNotification(e.Message);

        // Handle startup arguments when shell is ready
        shell.WhenReady(HandleStartupArguments);
    }

    /// <inheritdoc />
    public event EventHandler<ProjectAddedEventArgs>? ProjectAdded;
    /// <inheritdoc />
    public event EventHandler<CanonicalPath>? ProjectRemoved;
    /// <inheritdoc />
    public event EventHandler<ProjectNavigationRequestedEventArgs>? ProjectNavigationRequested;
    /// <inheritdoc />
    public event EventHandler<ProjectUpdateAvailableEventArgs>? ProjectUpdateAvailable;
    /// <inheritdoc />
    public event EventHandler? StateChanged;

    /// <inheritdoc />
    public ISettings Settings { get; }

    /// <inheritdoc />
    public ProjectStateData State
    {
        get => _state;
        set
        {
            if (_state != value)
            {
                _state = value;
                StateChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<ProjectInfo> GetProjects()
    {
        var projects = _registry.GetProjects();

        lock (_updateStatesLock)
        {
            // Populate update state for each project
            return projects.Select(p =>
            {
                var hasUpdateState = _updateStates.TryGetValue(p.ProjectPath, out var state);
                return p with
                {
                    NeedsUpdate = hasUpdateState && state.needsUpdate,
                    UpdateCount = hasUpdateState ? state.updateCount : 0
                };
            }).ToList();
        }
    }

    /// <inheritdoc />
    public bool TryAddProject(CanonicalPath projectPath, out string? errorMessage)
    {
        if (projectPath.IsEmpty())
        {
            errorMessage = "Project path cannot be empty.";
            return false;
        }

        if (TryAddProjectInternal(projectPath.Value!, out errorMessage))
        {
            // Raise event for manual addition
            ProjectAdded?.Invoke(this,
                new ProjectAddedEventArgs
                {
                    ProjectPath = projectPath,
                    Source = ProjectAdditionSource.Manual
                });
            return true;
        }
        return false;
    }

    /// <inheritdoc />
    public void RemoveProject(CanonicalPath projectPath)
    {
        if (projectPath.IsEmpty())
            return;
        _registry.RemoveProject(projectPath.Value!);

        // Raise event so UI can refresh
        ProjectRemoved?.Invoke(this, projectPath);
    }

    /// <summary>
    ///     Saves project configuration back to INI files, preserving comments and custom keys.
    /// </summary>
    /// <inheritdoc />
    public async Task SaveProjectDataAsync(ProjectData projectData)
    {
        const string mdkSection = "mdk";

        // Update main INI
        var mainIni = projectData.MainIni ?? new Ini();
        if (projectData.Config.Main != null)
        {
            mainIni = UpdateIniFromLayer(mainIni, mdkSection, projectData.Config.Main, true);

            var mainIniPath = projectData.MainIniPath;
            if (string.IsNullOrEmpty(mainIniPath))
            {
                var projectDir = Path.GetDirectoryName(projectData.ProjectPath.Value);
                if (string.IsNullOrEmpty(projectDir))
                    throw new InvalidOperationException("Cannot determine project directory");
                mainIniPath = Path.Combine(projectDir, "mdk.ini");
            }

            await File.WriteAllTextAsync(mainIniPath, mainIni.ToString());
        }

        // Update local INI
        var localIni = projectData.LocalIni ?? new Ini();
        if (projectData.Config.Local != null)
        {
            // Special behavior: Output and BinaryPath are machine-specific and should be in local
            // If both Main and Local are null, explicitly write "auto" to local to make it visible
            var forceAutoForNullPaths = (projectData.Config.Main?.Output == null && projectData.Config.Local.Output == null)
                                        || (projectData.Config.Main?.BinaryPath == null && projectData.Config.Local.BinaryPath == null);

            localIni = UpdateIniFromLayer(localIni, mdkSection, projectData.Config.Local, true, forceAutoForNullPaths);

            var localIniPath = projectData.LocalIniPath;
            if (string.IsNullOrEmpty(localIniPath))
            {
                var projectDir = Path.GetDirectoryName(projectData.ProjectPath.Value);
                if (string.IsNullOrEmpty(projectDir))
                    throw new InvalidOperationException("Cannot determine project directory");
                localIniPath = Path.Combine(projectDir, "mdk.local.ini");
            }

            await File.WriteAllTextAsync(localIniPath, localIni.ToString());
        }
    }

    /// <inheritdoc />
    public async Task<ProjectData> NormalizeConfigurationAsync(ProjectData projectData)
    {
        if (projectData == null) throw new ArgumentNullException(nameof(projectData));

        _logger.Info($"Starting configuration normalization for {projectData.Name}");

        // Create backup files
        if (projectData.MainIniPath != null && File.Exists(projectData.MainIniPath))
        {
            var backupPath = projectData.MainIniPath + ".backup";
            File.Copy(projectData.MainIniPath, backupPath, true);
            _logger.Info($"Created backup: {backupPath}");
        }

        if (projectData.LocalIniPath != null && File.Exists(projectData.LocalIniPath))
        {
            var backupPath = projectData.LocalIniPath + ".backup";
            File.Copy(projectData.LocalIniPath, backupPath, true);
            _logger.Info($"Created backup: {backupPath}");
        }

        // Build correctly structured layers
        var newMain = new ProjectConfigLayer
        {
            // Main should have: Type, Namespaces, Ignores, Minify, MinifyExtraOptions, Trace
            Type = projectData.Config.Main?.Type ?? projectData.Config.Local?.Type,
            Namespaces = projectData.Config.Main?.Namespaces ?? projectData.Config.Local?.Namespaces,
            Ignores = projectData.Config.Main?.Ignores ?? projectData.Config.Local?.Ignores,
            Minify = projectData.Config.Main?.Minify ?? projectData.Config.Local?.Minify,
            MinifyExtraOptions = projectData.Config.Main?.MinifyExtraOptions ?? projectData.Config.Local?.MinifyExtraOptions,
            Trace = projectData.Config.Main?.Trace ?? projectData.Config.Local?.Trace,
            // Main should NOT have these:
            Output = null,
            BinaryPath = null,
            Interactive = null
        };

        var newLocal = new ProjectConfigLayer
        {
            // Local should have: Output, BinaryPath, Interactive
            Output = projectData.Config.Local?.Output ?? projectData.Config.Main?.Output,
            BinaryPath = projectData.Config.Local?.BinaryPath ?? projectData.Config.Main?.BinaryPath,
            Interactive = projectData.Config.Local?.Interactive ?? projectData.Config.Main?.Interactive,
            // Local should NOT have these:
            Type = null,
            Namespaces = null,
            Ignores = null,
            Minify = null,
            MinifyExtraOptions = null,
            Trace = null
        };

        // Build migrated INIs: start with existing, remove misplaced known keys, update correct keys
        var mainIni = projectData.MainIni ?? new Ini();
        // Remove known "local" keys from main
        mainIni = mainIni.WithoutKey("mdk", "output");
        mainIni = mainIni.WithoutKey("mdk", "binarypath");
        mainIni = mainIni.WithoutKey("mdk", "interactive");
        // Update known "main" keys
        mainIni = UpdateIniFromLayer(mainIni, "mdk", newMain, false);

        var localIni = projectData.LocalIni ?? new Ini();
        // Remove known "main" keys from local
        localIni = localIni.WithoutKey("mdk", "type");
        localIni = localIni.WithoutKey("mdk", "namespaces");
        localIni = localIni.WithoutKey("mdk", "ignores");
        localIni = localIni.WithoutKey("mdk", "minify");
        localIni = localIni.WithoutKey("mdk", "minifyextraoptions");
        localIni = localIni.WithoutKey("mdk", "trace");
        // Update known "local" keys
        localIni = UpdateIniFromLayer(localIni, "mdk", newLocal, false);

        // Normalization is complete - keys are in the right files with their existing comments preserved

        // Create normalized ProjectData
        var migratedData = new ProjectData
        {
            Name = projectData.Name,
            MainIni = mainIni,
            LocalIni = localIni,
            MainIniPath = projectData.MainIniPath,
            LocalIniPath = projectData.LocalIniPath,
            ProjectPath = projectData.ProjectPath,
            Config = new ProjectConfig
            {
                Default = projectData.Config.Default,
                Main = newMain,
                Local = newLocal
            }
        };

        _logger.Info($"Configuration normalization complete for {projectData.Name}");

        return migratedData;
    }

    /// <summary>
    ///     Loads project configuration into the new ProjectData model with typed layers.
    /// </summary>
    /// <inheritdoc />
    public async Task<ProjectData?> LoadProjectDataAsync(CanonicalPath projectPath)
    {
        if (projectPath.IsEmpty())
            return null;

        if (!File.Exists(projectPath.Value))
            return null;

        var mainIniPath = IniFileFinder.FindMainIni(projectPath.Value!);
        var localIniPath = IniFileFinder.FindLocalIni(projectPath.Value!);

        // Need at least one INI file to proceed
        if (mainIniPath == null && localIniPath == null)
            return null;

        Ini? mainIni = null;
        Ini? localIni = null;

        if (mainIniPath != null && File.Exists(mainIniPath))
        {
            if (Ini.TryParse(await File.ReadAllTextAsync(mainIniPath), out var parsed))
                mainIni = parsed;
            else
                _logger.Warning($"Failed to parse main INI file: {mainIniPath}");
        }

        if (localIniPath != null && File.Exists(localIniPath))
        {
            if (Ini.TryParse(await File.ReadAllTextAsync(localIniPath), out var parsed))
                localIni = parsed;
            else
                _logger.Warning($"Failed to parse local INI file: {localIniPath}");
        }

        // Create default layer with hardcoded defaults
        var defaultLayer = new ProjectConfigLayer
        {
            Type = null, // Type must be set explicitly in INI
            Interactive = InteractiveMode.ShowNotification,
            Trace = false,
            Minify = MinifierLevel.None,
            MinifyExtraOptions = MinifierExtraOptions.None,
            Ignores = ImmutableArray.Create("obj/**/*", "MDK/**/*", "**/*.debug.cs"),
            Namespaces = ImmutableArray.Create("IngameScript"),
            Output = null,
            BinaryPath = null
        };

        // Parse main layer
        var mainLayer = ParseLayer(mainIni);

        // Parse local layer
        var localLayer = ParseLayer(localIni);

        return new ProjectData
        {
            Name = Path.GetFileNameWithoutExtension(projectPath.Value),
            MainIni = mainIni,
            LocalIni = localIni,
            MainIniPath = mainIniPath,
            LocalIniPath = localIniPath,
            ProjectPath = projectPath,
            Config = new ProjectConfig
            {
                Default = defaultLayer,
                Main = mainLayer,
                Local = localLayer
            }
        };
    }

    /// <inheritdoc />
    public async Task<bool> CopyScriptToClipboardAsync(CanonicalPath projectPath)
    {
        if (projectPath.IsEmpty())
            return false;

        try
        {
            var projectData = await LoadProjectDataAsync(projectPath);
            if (projectData == null)
                return false;

            var config = projectData.Config.GetEffective();
            var outputPath = config.Output?.Value;

            // Resolve "auto" to actual path
            if (string.Equals(outputPath, "auto", StringComparison.OrdinalIgnoreCase) || string.IsNullOrEmpty(outputPath))
            {
                // Check global custom settings first
                var hubSettings = _settings.GetValue(SettingsKeys.HubSettings, new HubSettings());
                var customPath = hubSettings.CustomAutoScriptOutputPath;
                
                if (!string.IsNullOrWhiteSpace(customPath) && !string.Equals(customPath, "auto", StringComparison.OrdinalIgnoreCase))
                {
                    // Use custom global path
                    outputPath = customPath;
                }
                else
                {
                    // Fall back to default SE path
                    var projectName = Path.GetFileNameWithoutExtension(projectPath.Value);
                    if (string.IsNullOrEmpty(projectName))
                        return false;

                    outputPath = config.Type == ProjectType.ProgrammableBlock
                        ? Path.Combine(_fileStorage.GetSpaceEngineersDataPath(), "IngameScripts", "local", projectName)
                        : null;
                    
                    if (string.IsNullOrEmpty(outputPath))
                        return false;
                }
            }
            
            // For non-auto paths, append project name as subfolder
            if (!string.IsNullOrEmpty(outputPath))
            {
                var projectName = Path.GetFileNameWithoutExtension(projectPath.Value);
                if (!string.IsNullOrEmpty(projectName))
                    outputPath = Path.Combine(outputPath, projectName);
            }

            if (string.IsNullOrEmpty(outputPath) || !Directory.Exists(outputPath))
                return false;

            var scriptFile = Path.Combine(outputPath, "Script.cs");
            if (!File.Exists(scriptFile))
                return false;

            var content = await File.ReadAllTextAsync(scriptFile);

            // Get clipboard from TopLevel
            var topLevel = Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
                ? TopLevel.GetTopLevel(desktop.MainWindow)
                : null;

            if (topLevel?.Clipboard != null)
            {
                await topLevel.Clipboard.SetTextAsync(content);
                _logger.Info($"Script copied to clipboard: {content.Length} characters");
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to copy script to clipboard: {ex.Message}");
            return false;
        }
    }

    /// <inheritdoc />
    public bool OpenProjectFolder(CanonicalPath projectPath)
    {
        if (projectPath.IsEmpty())
            return false;

        try
        {
            if (!File.Exists(projectPath.Value))
                return false;

            var folder = Path.GetDirectoryName(projectPath.Value);
            if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder))
                return false;

            Process.Start(new ProcessStartInfo
            {
                FileName = folder,
                UseShellExecute = true
            });

            _logger.Info($"Opened project folder: {folder}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to open project folder: {ex.Message}");
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> OpenOutputFolderAsync(CanonicalPath projectPath)
    {
        if (projectPath.IsEmpty())
            return false;

        try
        {
            var projectData = await LoadProjectDataAsync(projectPath);
            if (projectData == null)
                return false;

            var config = projectData.Config.GetEffective();
            var outputPath = config.Output?.Value;

            // Resolve "auto" to actual path
            if (string.Equals(outputPath, "auto", StringComparison.OrdinalIgnoreCase) || string.IsNullOrEmpty(outputPath))
            {
                // Check global custom settings first
                var hubSettings = _settings.GetValue(SettingsKeys.HubSettings, new HubSettings());
                var customPath = config.Type == ProjectType.ProgrammableBlock
                    ? hubSettings.CustomAutoScriptOutputPath
                    : config.Type == ProjectType.Mod
                        ? hubSettings.CustomAutoModOutputPath
                        : null;
                
                if (!string.IsNullOrWhiteSpace(customPath) && !string.Equals(customPath, "auto", StringComparison.OrdinalIgnoreCase))
                {
                    // Use custom global path
                    outputPath = customPath;
                }
                else
                {
                    // Fall back to default SE path
                    var projectName = Path.GetFileNameWithoutExtension(projectPath.Value);
                    if (string.IsNullOrEmpty(projectName))
                        return false;

                    outputPath = config.Type == ProjectType.ProgrammableBlock
                        ? Path.Combine(_fileStorage.GetSpaceEngineersDataPath(), "IngameScripts", "local", projectName)
                        : config.Type == ProjectType.Mod
                            ? Path.Combine(_fileStorage.GetSpaceEngineersDataPath(), "Mods", projectName)
                            : null;
                    
                    if (string.IsNullOrEmpty(outputPath))
                        return false;
                }
            }
            
            // For non-auto paths, append project name as subfolder
            if (!string.IsNullOrEmpty(outputPath))
            {
                var projectName = Path.GetFileNameWithoutExtension(projectPath.Value);
                if (!string.IsNullOrEmpty(projectName))
                    outputPath = Path.Combine(outputPath, projectName);
            }

            if (string.IsNullOrEmpty(outputPath) || !Directory.Exists(outputPath))
                return false;

            Process.Start(new ProcessStartInfo
            {
                FileName = outputPath,
                UseShellExecute = true
            });

            _logger.Info($"Opened output folder: {outputPath}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to open output folder: {ex.Message}");
            return false;
        }
    }

    /// <inheritdoc />
    public bool OpenProjectInIde(CanonicalPath projectPath)
    {
        if (projectPath.IsEmpty())
            return false;

        try
        {
            if (!File.Exists(projectPath.Value))
                return false;

            Process.Start(new ProcessStartInfo
            {
                FileName = projectPath.Value,
                UseShellExecute = true
            });

            _logger.Info($"Opened project in IDE: {projectPath}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to open project in IDE: {ex.Message}");
            return false;
        }
    }

    /// <inheritdoc />
    /// <inheritdoc />
    public bool NavigateToProject(CanonicalPath projectPath, bool openOptions = false, bool bringToFront = false)
    {
        if (projectPath.IsEmpty())
            return false;

        var project = _registry.GetProjects().FirstOrDefault(p => p.ProjectPath == projectPath);
        if (project is null)
        {
            _logger.Warning($"Cannot navigate to project, not found: {projectPath}");
            return false;
        }

        // Update state directly - this raises StateChanged event
        State = new ProjectStateData(projectPath, _state.CanMakeScript, _state.CanMakeMod);

        _logger.Info($"Navigated to project: {projectPath}{(openOptions ? " (with options)" : "")}{(bringToFront ? " (bringing to front)" : "")}");
        
        // Prioritize update check for selected project
        _updateChecker.QueueProjectCheck(projectPath, priority: true);
        
        // Bring window to front if requested
        if (bringToFront)
            _shell.BringToFront();

        // Raise event for additional navigation actions (e.g., opening options drawer)
        // Note: ProjectNavigationRequested is for actions AFTER selection, not FOR selection
        ProjectNavigationRequested?.Invoke(this,
            new ProjectNavigationRequestedEventArgs
            {
                ProjectPath = projectPath,
                OpenOptions = openOptions
            });

        return true;
    }

    /// <inheritdoc />
    public void ClearProjectUpdateState(CanonicalPath projectPath)
    {
        lock (_updateStatesLock)
            _updateStates.Remove(projectPath);
        _logger.Debug($"Cleared update state for project: {projectPath}");

        // Fire event so view models can update their UI state
        ProjectUpdateAvailable?.Invoke(this,
            new ProjectUpdateAvailableEventArgs
            {
                ProjectPath = projectPath,
                AvailableUpdates = Array.Empty<PackageUpdateInfo>()
            });
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, string> GetMdkPackageVersions(CanonicalPath projectPath)
    {
        var result = new Dictionary<string, string>();

        if (projectPath.IsEmpty() || !File.Exists(projectPath.Value))
            return result;

        try
        {
            var doc = XDocument.Load(projectPath.Value!);
            var ns = doc.Root?.Name.Namespace ?? XNamespace.None;

            // Find all PackageReference elements with MDK package IDs
            var packageReferences = doc.Descendants(ns + "PackageReference")
                .Where(e => e.Attribute("Include")?.Value.StartsWith(EnvironmentMetadata.PackagePrefix, StringComparison.OrdinalIgnoreCase) == true);

            foreach (var pkgRef in packageReferences)
            {
                var packageId = pkgRef.Attribute("Include")?.Value;
                var version = pkgRef.Attribute("Version")?.Value;

                if (!string.IsNullOrWhiteSpace(packageId) && !string.IsNullOrWhiteSpace(version))
                    result[packageId] = version;
            }

            _logger.Debug($"Found {result.Count} MDK package(s) in {projectPath}: {string.Join(", ", result.Select(kvp => $"{kvp.Key} {kvp.Value}"))}");
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to parse project file for package versions: {projectPath}", ex);
        }

        return result;
    }

    /// <inheritdoc />
    public IReadOnlyList<PackageUpdateInfo>? GetCachedUpdates(CanonicalPath projectPath)
    {
        lock (_updateStatesLock)
        {
            if (_updateStates.TryGetValue(projectPath, out var state) && state.needsUpdate)
                return state.updates;
        }
        return null;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PackageUpdateInfo>> CheckForPackageUpdatesAsync(CanonicalPath projectPath, CancellationToken cancellationToken = default)
    {
        var updates = new List<PackageUpdateInfo>();

        if (projectPath.IsEmpty())
            return updates;

        try
        {
            // Get current package versions from .csproj
            var currentVersions = GetMdkPackageVersions(projectPath);
            if (currentVersions.Count == 0)
            {
                _logger.Debug($"No MDK packages found in {projectPath}");
                return updates;
            }

            _logger.Info($"Checking for updates for {currentVersions.Count} package(s) in {projectPath.Value}");

            // Check all packages for updates in parallel
            var checkTasks = currentVersions.Select(async kvp =>
            {
                var (packageId, currentVersion) = kvp;
                try
                {
                    var latestVersion = await _nugetService.GetLatestVersionAsync(packageId, _settings.GetValue(SettingsKeys.HubSettings, new HubSettings()).IncludePrereleaseUpdates, cancellationToken);

                    if (latestVersion == null)
                    {
                        _logger.Warning($"Could not determine latest version for {packageId}");
                        return null;
                    }

                    // Use semantic version comparison
                    if (NuGetVersion.TryParse(currentVersion, out var currentVer) && NuGetVersion.TryParse(latestVersion, out var latestVer) && latestVer > currentVer)
                    {
                        _logger.Info($"Update available for {packageId}: {currentVersion} -> {latestVersion}");
                        return new PackageUpdateInfo
                        {
                            PackageId = packageId,
                            CurrentVersion = currentVersion,
                            LatestVersion = latestVersion
                        };
                    }
                    _logger.Debug($"{packageId} is up to date ({currentVersion})");
                    return null;
                }
                catch (OperationCanceledException)
                {
                    _logger.Info($"Package update check cancelled for {packageId}");
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.Error($"Error checking {packageId} for updates", ex);
                    return null;
                }
            });

            var results = await Task.WhenAll(checkTasks);
            updates.AddRange(results.Where(r => r != null)!);

            _logger.Info($"Update check complete for {projectPath.Value}: {updates.Count} update(s) available");
        }
        catch (OperationCanceledException)
        {
            _logger.Info($"Package update check cancelled for {projectPath}");
            throw;
        }
        catch (Exception ex)
        {
            _logger.Error($"Unexpected error checking for package updates: {projectPath}", ex);
        }

        return updates;
    }

    /// <inheritdoc />
    public async Task<bool> UpdatePackagesAsync(CanonicalPath projectPath, IReadOnlyList<PackageUpdateInfo> packagesToUpdate, CancellationToken cancellationToken = default)
    {
        if (projectPath.IsEmpty() || !File.Exists(projectPath.Value))
        {
            _logger.Warning($"Cannot update packages: project file not found at {projectPath}");
            return false;
        }

        if (packagesToUpdate.Count == 0)
        {
            _logger.Info("No packages to update");
            return true;
        }

        try
        {
            _logger.Info($"Updating {packagesToUpdate.Count} package(s) in {projectPath.Value}");

            // Load and modify the .csproj file
            var doc = XDocument.Load(projectPath.Value!);
            var ns = doc.Root?.Name.Namespace ?? XNamespace.None;
            var modified = false;

            foreach (var update in packagesToUpdate)
            {
                // Find the PackageReference element
                var packageRef = doc.Descendants(ns + "PackageReference")
                    .FirstOrDefault(e => e.Attribute("Include")?.Value == update.PackageId);

                if (packageRef != null)
                {
                    var versionAttr = packageRef.Attribute("Version");
                    if (versionAttr != null)
                    {
                        _logger.Info($"Updating {update.PackageId}: {versionAttr.Value} -> {update.LatestVersion}");
                        versionAttr.Value = update.LatestVersion;
                        modified = true;
                    }
                    else
                        _logger.Warning($"PackageReference for {update.PackageId} found but has no Version attribute");
                }
                else
                    _logger.Warning($"PackageReference for {update.PackageId} not found in project file");
            }

            if (!modified)
            {
                _logger.Warning("No packages were updated in the project file");
                return false;
            }

            // Save the modified project file
            doc.Save(projectPath.Value!);
            _logger.Info($"Saved updated project file: {projectPath.Value}");

            // Run dotnet restore to apply the changes
            _logger.Info($"Running dotnet restore for {projectPath.Value}");
            var restoreSuccess = await RunDotnetRestoreAsync(projectPath, cancellationToken);

            if (restoreSuccess)
            {
                _logger.Info($"Package update complete for {projectPath.Value}");

                // Clear update state since packages are now up to date
                ClearProjectUpdateState(projectPath);
                return true;
            }
            _logger.Error($"dotnet restore failed for {projectPath.Value}");
            return false;
        }
        catch (OperationCanceledException)
        {
            _logger.Info($"Package update cancelled for {projectPath}");
            throw;
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to update packages for {projectPath}", ex);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<(CanonicalPath? ProjectPath, string? ErrorMessage)> CreateProgrammableBlockProjectAsync(string projectName, string location) => await CreateProjectInternalAsync(projectName, location, "mdk2pbscript");

    /// <inheritdoc />
    public async Task<(CanonicalPath? ProjectPath, string? ErrorMessage)> CreateModProjectAsync(string projectName, string location) => await CreateProjectInternalAsync(projectName, location, "mdk2mod");

    static ProjectConfigLayer ParseLayer(Ini? ini)
    {
        if (ini == null)
            return new ProjectConfigLayer();

        var section = ini["mdk"];

        return new ProjectConfigLayer
        {
            Type = ParseProjectType(section.TryGet("type", out string? type) ? type : null),
            Interactive = ParseInteractiveMode(section.TryGet("interactive", out string? interactive) ? interactive : null),
            Trace = ParseBool(section.TryGet("trace", out string? trace) ? trace : null),
            Minify = ParseMinifierLevel(section.TryGet("minify", out string? minify) ? minify : null),
            MinifyExtraOptions = ParseMinifierExtraOptions(section.TryGet("minifyextraoptions", out string? minifyExtra) ? minifyExtra : null),
            Ignores = ParseStringList(section.TryGet("ignores", out string? ignores) ? ignores : null),
            Namespaces = ParseStringList(section.TryGet("namespaces", out string? namespaces) ? namespaces : null),
            Output = ParsePath(section.TryGet("output", out string? output) ? output : null),
            BinaryPath = ParsePath(section.TryGet("binarypath", out string? binaryPath) ? binaryPath : null)
        };
    }

    static CanonicalPath? ParsePath(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        value = value.Trim();

        // "auto" keyword means null (auto-detect)
        if (value.Equals("auto", StringComparison.OrdinalIgnoreCase))
            return null;

        // Real path
        try
        {
            return new CanonicalPath(value);
        }
        catch
        {
            // Invalid path, treat as null
            return null;
        }
    }

    static ProjectType? ParseProjectType(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        return Enum.TryParse<ProjectType>(value, true, out var result) ? result : null;
    }

    static InteractiveMode? ParseInteractiveMode(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        return Enum.TryParse<InteractiveMode>(value, true, out var result) ? result : null;
    }

    static bool? ParseBool(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        return value.Trim().ToLowerInvariant() switch
        {
            "on" or "true" or "yes" or "1" => true,
            "off" or "false" or "no" or "0" => false,
            _ => null
        };
    }

    static MinifierLevel? ParseMinifierLevel(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        return Enum.TryParse<MinifierLevel>(value, true, out var result) ? result : null;
    }

    static MinifierExtraOptions? ParseMinifierExtraOptions(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        return Enum.TryParse<MinifierExtraOptions>(value, true, out var result) ? result : null;
    }

    static ImmutableArray<string>? ParseStringList(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var items = value.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(item => item.Trim())
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .ToArray();

        return items.Length > 0 ? ImmutableArray.Create(items) : null;
    }

    void OnProjectUpdateAvailable(object? sender, ProjectUpdateAvailableEventArgs e)
    {
        lock (_updateStatesLock)
            _updateStates[e.ProjectPath] = (true, e.AvailableUpdates.Count, e.AvailableUpdates);

        // If this is the currently selected project, has updates, and hub is in background, show a snackbar
        if (e.AvailableUpdates.Count > 0 && 
            _state.SelectedProject == e.ProjectPath && 
            _shell.IsInBackground)
        {
            var projectName = _registry.GetProjects().FirstOrDefault(p => p.ProjectPath == e.ProjectPath)?.Name ?? "Project";
            var updateCount = e.AvailableUpdates.Count;
            
            _logger.Info($"Showing update notification for selected project {projectName}: {updateCount} update(s) available (hub in background)");
            
            _snackbarService.Show(
                $"{updateCount} package update{(updateCount > 1 ? "s" : "")} available for {projectName}",
                new[] { new SnackbarAction
                {
                    Text = "Show Me",
                    Action = _ => NavigateToProject(e.ProjectPath, bringToFront: true)
                }},
                timeout: 8000);
        }

        // Forward event to subscribers (view models, etc.)
        ProjectUpdateAvailable?.Invoke(this, e);
    }

    bool TryAddProjectInternal(string projectPath, out string? errorMessage, ProjectFlags flags = ProjectFlags.None)
    {
        if (ProjectDetector.TryDetectProject(projectPath, out var projectInfo))
        {
            // Apply flags to the project
            var projectWithFlags = projectInfo! with { Flags = flags };
            _registry.AddOrUpdateProject(projectWithFlags);
            errorMessage = null;
            return true;
        }

        errorMessage = "The selected project is not a valid MDK² project. MDK² projects must have a mdk.ini or mdk.local.ini configuration file.";
        return false;
    }

    Task HandleBuildNotification(InterConnectMessage message)
    {
        // Parse project path from message arguments
        // Expected format: script/mod <ProjectName> <ProjectPath> <OptionalMessage> [--simulate]
        if (message.Arguments.Length < 2)
        {
            _logger.Warning($"Build notification has insufficient arguments: {string.Join(", ", message.Arguments)}");
            return Task.CompletedTask;
        }

        var projectName = message.Arguments[0]; // First argument is project name
        var projectPath = message.Arguments[1]; // Second argument is the project path

        // Check for --simulate flag in arguments
        var isSimulated = message.Arguments.Any(arg =>
            string.Equals(arg, "--simulate", StringComparison.OrdinalIgnoreCase));

        if (isSimulated)
            _logger.Info($"Handling SIMULATED build notification for project: {projectPath}");
        else
            _logger.Info($"Handling build notification for project: {projectPath}");

        // Check if project already exists
        var existingProject = _registry.GetProjects().FirstOrDefault(p => p.IsPath(projectPath));

        if (existingProject == null)
        {
            // New project - try to add it
            _logger.Info($"New project detected: {projectPath}");

            if (isSimulated)
            {
                // For simulated projects, create fake ProjectInfo without validation
                var projectType = message.Type == NotificationType.Script
                    ? ProjectType.ProgrammableBlock
                    : ProjectType.Mod;

                var projectInfo = new ProjectInfo
                {
                    Name = projectName,
                    ProjectPath = new CanonicalPath(projectPath),
                    Type = projectType,
                    LastReferenced = DateTimeOffset.Now,
                    Flags = ProjectFlags.Simulated
                };

                _registry.AddOrUpdateProject(projectInfo);
                _logger.Info($"Successfully added simulated project: {projectPath}");

                // Raise event for build notification addition
                ProjectAdded?.Invoke(this,
                    new ProjectAddedEventArgs
                    {
                        ProjectPath = new CanonicalPath(projectPath),
                        Source = ProjectAdditionSource.BuildNotification
                    });

                // Handle build notification based on user preference
                if (message.Type == NotificationType.Script)
                    HandleBuildNotification(projectName, projectPath, true);
            }
            else
            {
                // Real project - validate it
                var flags = ProjectFlags.None;
                if (TryAddProjectInternal(projectPath, out var errorMessage, flags))
                {
                    _logger.Info($"Successfully added new project: {projectPath}");

                    // Raise event for build notification addition
                    ProjectAdded?.Invoke(this,
                        new ProjectAddedEventArgs
                        {
                            ProjectPath = new CanonicalPath(projectPath),
                            Source = ProjectAdditionSource.BuildNotification
                        });

                    // Handle build notification based on user preference
                    if (message.Type == NotificationType.Script)
                        HandleBuildNotification(projectName, projectPath, true);
                }
                else
                    _logger.Error($"Failed to add project: {errorMessage}");
            }
        }
        else
        {
            // Existing project - handle build notification based on preference
            _logger.Debug($"Build notification for existing project: {projectPath}");

            // Handle build notification based on user preference
            if (message.Type == NotificationType.Script)
                HandleBuildNotification(projectName, projectPath, false);
        }
        return Task.CompletedTask;
    }

    void HandleStartupArguments(string[] args)
    {
        // First instance was launched with arguments - treat like an IPC message
        if (args.Length == 0)
            return;

        _logger.Info($"Handling startup arguments: {string.Join(" ", args)}");

        // Parse as if it's a notification: type projectName projectPath [message] [--simulate]
        if (args.Length < 3)
        {
            _logger.Warning($"Insufficient startup arguments: {string.Join(" ", args)}");
            return;
        }

        // Determine notification type
        NotificationType type;
        if (string.Equals(args[0], "script", StringComparison.OrdinalIgnoreCase))
            type = NotificationType.Script;
        else if (string.Equals(args[0], "mod", StringComparison.OrdinalIgnoreCase))
            type = NotificationType.Mod;
        else
        {
            _logger.Warning($"Unknown notification type in startup args: {args[0]}");
            return;
        }

        // Create message and handle it
        var message = new InterConnectMessage(type, args.Skip(1).ToArray());
        _ = HandleBuildNotification(message); // Fire and forget
    }

    async Task<bool> RunDotnetRestoreAsync(CanonicalPath projectPath, CancellationToken cancellationToken)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"restore \"{projectPath.Value}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                _logger.Error("Failed to start dotnet restore process");
                return false;
            }

            // Read output asynchronously
            var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);

            await process.WaitForExitAsync(cancellationToken);

            var output = await outputTask;
            var error = await errorTask;

            if (process.ExitCode == 0)
            {
                _logger.Info($"dotnet restore succeeded for {projectPath.Value}");
                if (!string.IsNullOrWhiteSpace(output))
                    _logger.Debug($"dotnet restore output: {output}");
                return true;
            }
            _logger.Error($"dotnet restore failed with exit code {process.ExitCode}");
            if (!string.IsNullOrWhiteSpace(error))
                _logger.Error($"dotnet restore error: {error}");
            return false;
        }
        catch (OperationCanceledException)
        {
            _logger.Info($"dotnet restore cancelled for {projectPath}");
            throw;
        }
        catch (Exception ex)
        {
            _logger.Error($"Error running dotnet restore for {projectPath}", ex);
            return false;
        }
    }

    async void HandleBuildNotification(string projectName, string projectPath, bool isNewProject)
    {
        try
        {
            await HandleBuildNotificationAsync(projectName, projectPath, isNewProject);
        }
        catch (Exception e)
        {
            _logger.Error($"Error handling build notification for {projectPath}", e);
        }
    }

    async Task HandleBuildNotificationAsync(string projectName, string projectPath, bool isNewProject)
    {
        // Load configuration to check notification preference
        var projectData = await LoadProjectDataAsync(new CanonicalPath(projectPath));
        var preference = projectData?.Config.GetEffective().Interactive?.ToString() ?? "";

        // If not set in INI, default to "ShowNotification" (less intrusive)
        if (string.IsNullOrWhiteSpace(preference))
        {
            preference = "ShowNotification";
            _logger.Info($"Build notification preference not set for {projectName}, using ShowNotification");
        }
        else
            _logger.Info($"Build notification preference for {projectName}: {preference}");

        // Determine if we should bring hub to front
        var bringToFront = preference.Equals("OpenHub", StringComparison.OrdinalIgnoreCase);
        
        // Always navigate to the project (select it) to trigger update check prioritization
        NavigateToProject(new CanonicalPath(projectPath), bringToFront: bringToFront);

        switch (preference.ToLowerInvariant())
        {
            case "shownotification":
            case "showtoast": // Legacy compatibility
                // Show snackbar notification
                ShowScriptDeployedSnackbar(projectName, projectPath);
                break;

            case "openhub":
                // Window already brought to front by NavigateToProject call above
                break;

            case "donothing":
                // Silent - do nothing
                _logger.Info($"Build notification suppressed (DoNothing preference) for: {projectName}");
                break;

            default:
                // Unknown preference - default to ShowNotification
                _logger.Warning($"Unknown notification preference '{preference}', using default");
                ShowScriptDeployedSnackbar(projectName, projectPath);
                break;
        }
    }

    void ShowScriptDeployedSnackbar(string projectName, string projectPath)
    {
        var message = $"Your script \"{projectName}\" has been successfully deployed.";
        var actions = new List<SnackbarAction>
        {
            new()
            {
                Text = "Open in Hub",
                Action = _ => NavigateToProject(new CanonicalPath(projectPath), bringToFront: true),
                Context = projectPath,
                IsClosingAction = true
            },
            new()
            {
                Text = "Copy to clipboard",
                Action = OnCopyToClipboard,
                Context = projectPath,
                IsClosingAction = true
            },
            new()
            {
                Text = "Show me",
                Action = OnShowMe,
                Context = projectPath,
                IsClosingAction = true
            }
        };

        _snackbarService.Show(message, actions);
    }

    async void OnShowMe(object? ctx)
    {
        try
        {
            if (ctx is string path)
                await OpenOutputFolderAsync(new CanonicalPath(path));
        }
        catch (Exception e)
        {
            _logger.Error("Failed to open output folder", e);
        }
    }

    async void OnCopyToClipboard(object? ctx)
    {
        try
        {
            if (ctx is string path)
            {
                var success = await CopyScriptToClipboardAsync(new CanonicalPath(path));
                if (success)
                    _snackbarService.Show("Script copied to clipboard.", 2000);
            }
        }
        catch (Exception e)
        {
            _logger.Error($"Failed to copy script to clipboard: {e.Message}");
            _shell.ShowToast("Failed to copy script to clipboard.");
        }
    }

    async Task<(CanonicalPath? ProjectPath, string? ErrorMessage)> CreateProjectInternalAsync(string projectName, string location, string templateName)
    {
        try
        {
            // Validate inputs
            if (string.IsNullOrWhiteSpace(projectName))
                return (null, "Project name cannot be empty.");

            if (string.IsNullOrWhiteSpace(location) || !Directory.Exists(location))
                return (null, "Invalid location specified.");

            var projectPath = Path.Combine(location, projectName);
            if (Directory.Exists(projectPath))
                return (null, $"A directory named '{projectName}' already exists at this location.");

            // Create the project using dotnet new
            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"new {templateName} -n \"{projectName}\" -o \"{projectPath}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process == null)
                return (null, "Failed to start dotnet process.");

            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                var error = await process.StandardError.ReadToEndAsync();
                _logger.Error($"Failed to create project '{projectName}': {error}");
                return (null, $"dotnet new failed: {error}");
            }

            // Find the .csproj file - get actual casing from disk
            var csprojFiles = Directory.GetFiles(projectPath, "*.csproj", SearchOption.TopDirectoryOnly);
            if (csprojFiles.Length == 0)
            {
                _logger.Error($"Created project but cannot find .csproj in: {projectPath}");
                return (null, "Project was created but .csproj file not found.");
            }

            var csprojPath = csprojFiles[0]; // Use the actual file path from disk (preserves casing)
            _logger.Info($"Successfully created project: {csprojPath}");
            return (new CanonicalPath(csprojPath), null);
        }
        catch (Exception ex)
        {
            _logger.Error($"Exception while creating project: {ex}");
            return (null, $"Unexpected error: {ex.Message}");
        }
    }

    static Ini UpdateIniFromLayer(Ini ini, string section, ProjectConfigLayer layer, bool removeNulls = false, bool forceAutoForNullPaths = false)
    {
        // Type - convert enum to lowercase string
        ini = UpdateIniValue(ini, section, "type", layer.Type?.ToString().ToLowerInvariant(), removeNulls);

        // Interactive - convert enum to PascalCase string (OpenHub, ShowNotification, DoNothing)
        ini = UpdateIniValue(ini, section, "interactive", layer.Interactive?.ToString(), removeNulls);

        // Trace - convert bool to "on"/"off"
        ini = UpdateIniValue(ini, section, "trace", layer.Trace.HasValue ? layer.Trace.Value ? "on" : "off" : null, removeNulls);

        // Minify - convert enum to lowercase string
        ini = UpdateIniValue(ini, section, "minify", layer.Minify?.ToString().ToLowerInvariant(), removeNulls);

        // MinifyExtraOptions - convert enum to lowercase string
        ini = UpdateIniValue(ini, section, "minifyextraoptions", layer.MinifyExtraOptions?.ToString().ToLowerInvariant(), removeNulls);

        // Ignores - convert array to comma-separated string
        ini = UpdateIniValue(ini, section, "ignores", layer.Ignores.HasValue ? string.Join(",", layer.Ignores.Value) : null, removeNulls);

        // Namespaces - convert array to comma-separated string
        ini = UpdateIniValue(ini, section, "namespaces", layer.Namespaces.HasValue ? string.Join(",", layer.Namespaces.Value) : null, removeNulls);

        // Output - CanonicalPath? (null = "auto" in INI)
        var outputValue = layer.Output?.Value;
        if (forceAutoForNullPaths && outputValue == null)
            outputValue = "auto";
        ini = UpdateIniValue(ini, section, "output", outputValue, removeNulls);

        // BinaryPath - CanonicalPath? (null = "auto" in INI)  
        var binaryPathValue = layer.BinaryPath?.Value;
        if (forceAutoForNullPaths && binaryPathValue == null)
            binaryPathValue = "auto";
        ini = UpdateIniValue(ini, section, "binarypath", binaryPathValue, removeNulls);

        return ini;
    }

    static Ini UpdateIniValue(Ini ini, string section, string key, string? value, bool removeNulls)
    {
        if (value == null && removeNulls)
            return ini.WithoutKey(section, key);
        if (value != null)
            return ini.WithKey(section, key, value);
        return ini;
    }
}