using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Mal.DependencyInjection;
using Mdk.Hub.Features.Diagnostics;
using Mdk.Hub.Features.Interop;
using Mdk.Hub.Features.Projects.Configuration;
using Mdk.Hub.Features.Projects.Overview;
using Mdk.Hub.Features.Settings;
using Mdk.Hub.Features.Shell;
using Mdk.Hub.Features.Snackbars;
using Mdk.Hub.Features.Updates;
using Mdk.Hub.Utility;

namespace Mdk.Hub.Features.Projects;

[Dependency<IProjectService>]
public class ProjectService : IProjectService
{
    readonly IProjectRegistry _registry;
    readonly ILogger _logger;
    readonly IShell _shell;
    readonly ISnackbarService _snackbarService;
    readonly ISettings _settings;
    readonly ProjectUpdateChecker _updateChecker;
    readonly Dictionary<CanonicalPath, (bool needsUpdate, int updateCount)> _updateStates = new();
    readonly object _updateStatesLock = new();
    
    ProjectStateData _state;

    public event EventHandler<ProjectAddedEventArgs>? ProjectAdded;
    public event EventHandler<CanonicalPath>? ProjectRemoved;
    public event EventHandler<ProjectNavigationRequestedEventArgs>? ProjectNavigationRequested;
    public event EventHandler<ProjectUpdateAvailableEventArgs>? ProjectUpdateAvailable;
    public event EventHandler? StateChanged;

    public ISettings Settings => _settings;
    
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

    public ProjectService(ILogger logger, IProjectRegistry registry, IInterProcessCommunication ipc, IShell shell, ISnackbarService snackbarService, ISettings settings, IUpdateCheckService updateCheckService)
    {
        _registry = registry;
        _logger = logger;
        _shell = shell;
        _snackbarService = snackbarService;
        _settings = settings;
        _updateChecker = new ProjectUpdateChecker(logger);
        _state = new ProjectStateData(default, canMakeScript: true, canMakeMod: true);
        
        _updateChecker.ProjectUpdateAvailable += OnProjectUpdateAvailable;
        
        // When version data is available, start checking projects
        updateCheckService.WhenVersionCheckCompleted(versionData =>
        {
            _updateChecker.OnVersionDataAvailable(versionData);
            
            // Queue all existing projects for checking
            var projects = _registry.GetProjects();
            _updateChecker.QueueProjectsCheck(projects.Select(p => p.ProjectPath));
        });
        
        // Subscribe to IPC messages
        ipc.MessageReceived += async (_, e) => await HandleBuildNotificationAsync(e.Message);
        
        // Handle startup arguments when shell is ready
        shell.WhenStarted(args => HandleStartupArguments(args));
    }

    void OnProjectUpdateAvailable(object? sender, ProjectUpdateAvailableEventArgs e)
    {
        lock (_updateStatesLock)
        {
            _updateStates[e.ProjectPath] = (true, e.AvailableUpdates.Count);
        }
        
        // Forward event to subscribers (view models, etc.)
        ProjectUpdateAvailable?.Invoke(this, e);
    }

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
            ProjectAdded?.Invoke(this, new ProjectAddedEventArgs 
            { 
                ProjectPath = projectPath,
                Source = ProjectAdditionSource.Manual
            });
            return true;
        }
        return false;
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

        errorMessage = "The selected project is not a valid MDK2 project. MDK2 projects must reference either Mal.Mdk2.PbPackager or Mal.Mdk2.ModPackager.";
        return false;
    }

    public void RemoveProject(CanonicalPath projectPath)
    {
        if (projectPath.IsEmpty())
            return;
        _registry.RemoveProject(projectPath.Value!);
        
        // Raise event so UI can refresh
        ProjectRemoved?.Invoke(this, projectPath);
    }

    public ProjectConfiguration? LoadConfiguration(CanonicalPath projectPath)
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
        var warnings = new List<string>();

        if (mainIniPath != null && File.Exists(mainIniPath))
        {
            if (Ini.TryParse(File.ReadAllText(mainIniPath), out var parsed))
                mainIni = parsed;
            else
            {
                warnings.Add($"Main configuration file is corrupt or malformed: {Path.GetFileName(mainIniPath)}");
                _logger.Warning($"Failed to parse main INI file: {mainIniPath}");
            }
        }
        else if (mainIniPath != null)
        {
            warnings.Add($"Main configuration file not found: {Path.GetFileName(mainIniPath)}");
            _logger.Info($"Main INI file not found: {mainIniPath}");
        }

        if (localIniPath != null && File.Exists(localIniPath))
        {
            if (Ini.TryParse(File.ReadAllText(localIniPath), out var parsed))
                localIni = parsed;
            else
            {
                warnings.Add($"Local configuration file is corrupt or malformed: {Path.GetFileName(localIniPath)}");
                _logger.Warning($"Failed to parse local INI file: {localIniPath}");
            }
        }

        return new ProjectConfiguration
        {
            MainIni = mainIni,
            LocalIni = localIni,
            MainIniPath = mainIniPath,
            LocalIniPath = localIniPath,
            ProjectPath = projectPath,
            ConfigurationWarnings = warnings,
            
            // Merge configuration values (local overrides main)
            Type = GetConfigValue(mainIni, localIni, "type", "programmableblock"),
            Minify = GetConfigValue(mainIni, localIni, "minify", "none"),
            MinifyExtraOptions = GetConfigValue(mainIni, localIni, "minifyextraoptions", "none"),
            Trace = GetConfigBoolValue(mainIni, localIni, "trace", false),
            Ignores = GetConfigValue(mainIni, localIni, "ignores", "obj/**/*,MDK/**/*,**/*.debug.cs"),
            Namespaces = GetConfigValue(mainIni, localIni, "namespaces", "IngameScript"),
            Output = GetConfigValue(mainIni, localIni, "output", "auto"),
            BinaryPath = GetConfigValue(mainIni, localIni, "binarypath", "auto"),
            Interactive = GetConfigValue(mainIni, localIni, "interactive", "")
        };
    }

    static ConfigurationValue<string> GetConfigValue(Ini? mainIni, Ini? localIni, string key, string defaultValue)
    {
        var mdkSection = "mdk";
        
        // Check local first (highest priority)
        if (localIni != null && localIni[mdkSection].TryGet(key, out string? localValue))
            return new ConfigurationValue<string>(localValue, SourceLayer.Local);
        
        // Check main second
        if (mainIni != null && mainIni[mdkSection].TryGet(key, out string? mainValue))
            return new ConfigurationValue<string>(mainValue, SourceLayer.Main);
        
        // Fall back to default
        return new ConfigurationValue<string>(defaultValue, SourceLayer.Default);
    }

    static ConfigurationValue<bool> GetConfigBoolValue(Ini? mainIni, Ini? localIni, string key, bool defaultValue)
    {
        var mdkSection = "mdk";
        
        // Check local first (highest priority)
        if (localIni != null && localIni[mdkSection].TryGet(key, out bool localValue))
            return new ConfigurationValue<bool>(localValue, SourceLayer.Local);
        
        // Check main second
        if (mainIni != null && mainIni[mdkSection].TryGet(key, out bool mainValue))
            return new ConfigurationValue<bool>(mainValue, SourceLayer.Main);
        
        // Fall back to default
        return new ConfigurationValue<bool>(defaultValue, SourceLayer.Default);
    }

    public async Task SaveConfiguration(CanonicalPath projectPath, string interactive, string output, string binaryPath, string minify, string minifyExtraOptions, string trace, string ignores, string namespaces, bool saveToLocal)
    {
        if (projectPath.IsEmpty())
            return;

        var mainIniPath = IniFileFinder.FindMainIni(projectPath.Value!);
        var localIniPath = IniFileFinder.FindLocalIni(projectPath.Value!);
        var targetIniPath = saveToLocal ? localIniPath : mainIniPath;
        
        // Ensure we have a target path
        if (string.IsNullOrWhiteSpace(targetIniPath))
        {
            // Create the path if it doesn't exist
            var projectDir = Path.GetDirectoryName(projectPath.Value);
            if (string.IsNullOrWhiteSpace(projectDir))
                throw new InvalidOperationException("Cannot determine project directory");
                
            targetIniPath = Path.Combine(projectDir, saveToLocal ? "mdk.local.ini" : "mdk.ini");
        }

        // Load the target INI file or create a new one
        Ini targetIni;
        if (File.Exists(targetIniPath) && Ini.TryParse(await File.ReadAllTextAsync(targetIniPath), out var parsed))
            targetIni = parsed;
        else
            targetIni = new Ini();

        // Update values in the [mdk] section
        targetIni = targetIni
            .WithKey("mdk", "interactive", interactive)
            .WithKey("mdk", "output", output)
            .WithKey("mdk", "binarypath", binaryPath)
            .WithKey("mdk", "minify", minify)
            .WithKey("mdk", "minifyextraoptions", minifyExtraOptions)
            .WithKey("mdk", "trace", trace)
            .WithKey("mdk", "ignores", ignores)
            .WithKey("mdk", "namespaces", namespaces);

        // Write the file
        await File.WriteAllTextAsync(targetIniPath, targetIni.ToString());
    }
    
    async Task HandleBuildNotificationAsync(InterConnectMessage message)
    {
        // Parse project path from message arguments
        // Expected format: script/mod <ProjectName> <ProjectPath> <OptionalMessage> [--simulate]
        if (message.Arguments.Length < 2)
        {
            _logger.Warning($"Build notification has insufficient arguments: {string.Join(", ", message.Arguments)}");
            return;
        }
        
        var projectName = message.Arguments[0]; // First argument is project name
        var projectPath = message.Arguments[1]; // Second argument is the project path
        
        // Check for --simulate flag in arguments
        bool isSimulated = message.Arguments.Any(arg => 
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
            
            ProjectInfo? projectInfo;
            
            if (isSimulated)
            {
                // For simulated projects, create fake ProjectInfo without validation
                var projectType = message.Type == NotificationType.Script 
                    ? ProjectType.IngameScript 
                    : ProjectType.Mod;
                
                projectInfo = new ProjectInfo
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
                ProjectAdded?.Invoke(this, new ProjectAddedEventArgs 
                { 
                    ProjectPath = new CanonicalPath(projectPath),
                    Source = ProjectAdditionSource.BuildNotification
                });
                
                // Handle build notification based on user preference
                if (message.Type == NotificationType.Script)
                {
                    HandleBuildNotification(projectName, projectPath, isNewProject: true);
                }
            }
            else
            {
                // Real project - validate it
                var flags = ProjectFlags.None;
                if (TryAddProjectInternal(projectPath, out var errorMessage, flags))
                {
                    _logger.Info($"Successfully added new project: {projectPath}");
                    
                    // Raise event for build notification addition
                    ProjectAdded?.Invoke(this, new ProjectAddedEventArgs 
                    { 
                        ProjectPath = new CanonicalPath(projectPath),
                        Source = ProjectAdditionSource.BuildNotification
                    });
                    
                    // Handle build notification based on user preference
                    if (message.Type == NotificationType.Script)
                    {
                        HandleBuildNotification(projectName, projectPath, isNewProject: true);
                    }
                }
                else
                {
                    _logger.Error($"Failed to add project: {errorMessage}");
                }
            }
        }
        else
        {
            // Existing project - handle build notification based on preference
            _logger.Debug($"Build notification for existing project: {projectPath}");
            
            // Handle build notification based on user preference
            if (message.Type == NotificationType.Script)
            {
                HandleBuildNotification(projectName, projectPath, isNewProject: false);
            }
        }
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
        _ = HandleBuildNotificationAsync(message); // Fire and forget
    }

    public async Task<bool> CopyScriptToClipboardAsync(CanonicalPath projectPath)
    {
        if (projectPath.IsEmpty())
            return false;

        try
        {
            var config = LoadConfiguration(projectPath);
            if (config == null)
                return false;

            var outputPath = config.GetResolvedOutputPath();
            if (string.IsNullOrEmpty(outputPath) || !Directory.Exists(outputPath))
                return false;

            var scriptFile = Path.Combine(outputPath, "Script.cs");
            if (!File.Exists(scriptFile))
                return false;

            var content = await File.ReadAllTextAsync(scriptFile);

            // Get clipboard from TopLevel
            var topLevel = Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
                ? Avalonia.Controls.TopLevel.GetTopLevel(desktop.MainWindow)
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

            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
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

    public bool OpenOutputFolder(CanonicalPath projectPath)
    {
        if (projectPath.IsEmpty())
            return false;
            
        try
        {
            var config = LoadConfiguration(projectPath);
            if (config == null)
                return false;

            var outputPath = config.GetResolvedOutputPath();
            if (string.IsNullOrEmpty(outputPath) || !Directory.Exists(outputPath))
                return false;

            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
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

    public bool NavigateToProject(CanonicalPath projectPath)
    {
        if (projectPath.IsEmpty())
            return false;
            
        var project = _registry.GetProjects().FirstOrDefault(p => p.ProjectPath == projectPath);
        if (project is null)
        {
            _logger.Warning($"Cannot navigate to project, not found: {projectPath}");
            return false;
        }
        
        // Raise event for view models to handle navigation
        ProjectNavigationRequested?.Invoke(this, new ProjectNavigationRequestedEventArgs 
        { 
            ProjectPath = projectPath
        });
        _logger.Info($"Navigation requested for project: {projectPath}");
        return true;
    }

    public void ClearProjectUpdateState(CanonicalPath projectPath)
    {
        lock (_updateStatesLock)
        {
            _updateStates.Remove(projectPath);
        }
        _logger.Debug($"Cleared update state for project: {projectPath}");
        
        // Fire event so view models can update their UI state
        ProjectUpdateAvailable?.Invoke(this, new ProjectUpdateAvailableEventArgs
        {
            ProjectPath = projectPath,
            AvailableUpdates = Array.Empty<PackageUpdateInfo>()
        });
    }
    
    void HandleBuildNotification(string projectName, string projectPath, bool isNewProject)
    {
        // Load configuration to check notification preference
        var config = LoadConfiguration(new CanonicalPath(projectPath));
        var preference = config?.Interactive.Value ?? "";
        
        // If not set in INI, default to "OpenHub" (teaches new users about Hub)
        // But UI will show "ShowNotification" as the default choice when they open settings
        if (string.IsNullOrWhiteSpace(preference))
        {
            preference = "OpenHub";
            _logger.Info($"Build notification preference not set for {projectName}, using OpenHub");
        }
        else
        {
            _logger.Info($"Build notification preference for {projectName}: {preference}");
        }
        
        switch (preference.ToLowerInvariant())
        {
            case "shownotification":
            case "showtoast": // Legacy compatibility
                // Show snackbar notification
                ShowScriptDeployedSnackbar(projectName, projectPath);
                break;
                
            case "openhub":
                // For new projects, ProjectAdded event already handles selection with cooldown logic
                // For existing projects, navigate explicitly
                if (!isNewProject)
                {
                    NavigateToProject(new CanonicalPath(projectPath));
                }
                // else: let OnProjectAdded handle it (respects user activity cooldown)
                break;
                
            case "donothing":
                // Silent - do nothing
                _logger.Info($"Build notification suppressed (DoNothing preference) for: {projectName}");
                break;
                
            default:
                // Unknown preference - default to OpenHub for new projects, ShowNotification for existing
                _logger.Warning($"Unknown notification preference '{preference}', using default");
                if (isNewProject)
                {
                    // Let OnProjectAdded handle selection
                }
                else
                {
                    ShowScriptDeployedSnackbar(projectName, projectPath);
                }
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
                Action = _ => NavigateToProject(new CanonicalPath(projectPath)),
                Context = projectPath,
                IsClosingAction = true
            },
            new()
            {
                Text = "Copy to clipboard",
                Action = async ctx =>
                {
                    if (ctx is string path)
                    {
                        var success = await CopyScriptToClipboardAsync(new CanonicalPath(path));
                        if (success)
                        {
                            _snackbarService.Show("Script copied to clipboard.", timeout: 2000);
                        }
                    }
                },
                Context = projectPath,
                IsClosingAction = true
            },
            new()
            {
                Text = "Show me",
                Action = ctx =>
                {
                    if (ctx is string path)
                    {
                        OpenOutputFolder(new CanonicalPath(path));
                    }
                },
                Context = projectPath,
                IsClosingAction = true
            }
        };
        
        _snackbarService.Show(message, actions, timeout: 15000);
    }

    public async Task<(CanonicalPath? ProjectPath, string? ErrorMessage)> CreateProgrammableBlockProjectAsync(string projectName, string location)
    {
        return await CreateProjectInternalAsync(projectName, location, "mdk2pbscript");
    }

    public async Task<(CanonicalPath? ProjectPath, string? ErrorMessage)> CreateModProjectAsync(string projectName, string location)
    {
        return await CreateProjectInternalAsync(projectName, location, "mdk2mod");
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
            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"new {templateName} -n \"{projectName}\" -o \"{projectPath}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = System.Diagnostics.Process.Start(startInfo);
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
}
