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
using Mdk.Hub.Features.Shell;
using Mdk.Hub.Utility;

namespace Mdk.Hub.Features.Projects;

[Dependency<IProjectService>]
public class ProjectService : IProjectService
{
    readonly IProjectRegistry _registry;
    readonly ILogger _logger;

    public event EventHandler<ProjectAddedEventArgs>? ProjectAdded;

    public ProjectService(ILogger logger, IProjectRegistry registry, IInterProcessCommunication ipc, IShell shell)
    {
        _registry = registry;
        _logger = logger;
        
        // Subscribe to IPC messages
        ipc.MessageReceived += async (_, e) => await HandleBuildNotificationAsync(e.Message);
        
        // Handle startup arguments when shell is ready
        shell.WhenStarted(args => HandleStartupArguments(args));
    }

    public IReadOnlyList<ProjectInfo> GetProjects()
    {
        return _registry.GetProjects();
    }

    public bool TryAddProject(string projectPath, out string? errorMessage)
    {
        if (TryAddProjectInternal(projectPath, out errorMessage))
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

    public void RemoveProject(string projectPath)
    {
        _registry.RemoveProject(projectPath);
    }

    public ProjectConfiguration? LoadConfiguration(string projectPath)
    {
        if (string.IsNullOrWhiteSpace(projectPath) || !File.Exists(projectPath))
            return null;

        var mainIniPath = IniFileFinder.FindMainIni(projectPath);
        var localIniPath = IniFileFinder.FindLocalIni(projectPath);

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
            BinaryPath = GetConfigValue(mainIni, localIni, "binarypath", "auto")
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

    public async Task SaveConfiguration(string projectPath, string interactive, string output, string binaryPath, string minify, string minifyExtraOptions, string trace, string ignores, string namespaces, bool saveToLocal)
    {
        if (string.IsNullOrWhiteSpace(projectPath))
            throw new ArgumentNullException(nameof(projectPath));

        var mainIniPath = IniFileFinder.FindMainIni(projectPath);
        var localIniPath = IniFileFinder.FindLocalIni(projectPath);
        var targetIniPath = saveToLocal ? localIniPath : mainIniPath;
        
        // Ensure we have a target path
        if (string.IsNullOrWhiteSpace(targetIniPath))
        {
            // Create the path if it doesn't exist
            var projectDir = Path.GetDirectoryName(projectPath);
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
        var existingProject = _registry.GetProjects().FirstOrDefault(p => 
            string.Equals(p.ProjectPath, projectPath, StringComparison.OrdinalIgnoreCase));
        
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
                    ProjectPath = projectPath,
                    Type = projectType,
                    LastReferenced = DateTimeOffset.Now,
                    Flags = ProjectFlags.Simulated
                };
                
                _registry.AddOrUpdateProject(projectInfo);
                _logger.Info($"Successfully added simulated project: {projectPath}");
                
                // Raise event for build notification addition
                ProjectAdded?.Invoke(this, new ProjectAddedEventArgs 
                { 
                    ProjectPath = projectPath,
                    Source = ProjectAdditionSource.BuildNotification
                });
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
                        ProjectPath = projectPath,
                        Source = ProjectAdditionSource.BuildNotification
                    });
                }
                else
                {
                    _logger.Error($"Failed to add project: {errorMessage}");
                }
            }
        }
        else
        {
            // Existing project - update last referenced
            _logger.Debug($"Build notification for existing project: {projectPath}");
            // TODO: Handle based on user's build notification preference (Phase 2+)
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
}
