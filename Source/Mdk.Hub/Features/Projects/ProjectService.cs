using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Mal.DependencyInjection;
using Mdk.Hub.Features.Projects.Configuration;
using Mdk.Hub.Features.Projects.Overview;
using Mdk.Hub.Utility;

namespace Mdk.Hub.Features.Projects;

[Dependency<IProjectService>]
public class ProjectService : IProjectService
{
    readonly ProjectRegistry _registry;

    public ProjectService()
    {
        _registry = new ProjectRegistry();
    }

    public IReadOnlyList<ProjectInfo> GetProjects()
    {
        return _registry.GetProjects();
    }

    public bool TryAddProject(string projectPath, out string? errorMessage)
    {
        if (ProjectDetector.TryDetectProject(projectPath, out var projectInfo))
        {
            _registry.AddOrUpdateProject(projectInfo!);
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

        if (mainIniPath != null && File.Exists(mainIniPath))
        {
            if (Ini.TryParse(File.ReadAllText(mainIniPath), out var parsed))
                mainIni = parsed;
        }

        if (localIniPath != null && File.Exists(localIniPath))
        {
            if (Ini.TryParse(File.ReadAllText(localIniPath), out var parsed))
                localIni = parsed;
        }

        return new ProjectConfiguration
        {
            MainIni = mainIni,
            LocalIni = localIni,
            MainIniPath = mainIniPath,
            LocalIniPath = localIniPath,
            ProjectPath = projectPath,
            
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

    public async Task SaveConfiguration(string projectPath, string output, string binaryPath, string minify, string minifyExtraOptions, string trace, string ignores, string namespaces, bool saveToLocal)
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
        if (File.Exists(targetIniPath) && Ini.TryParse(File.ReadAllText(targetIniPath), out var parsed))
            targetIni = parsed;
        else
            targetIni = new Ini();

        // Update values in the [mdk] section
        targetIni = targetIni
            .WithKey("mdk", "output", output)
            .WithKey("mdk", "binarypath", binaryPath)
            .WithKey("mdk", "minify", minify)
            .WithKey("mdk", "minifyextraoptions", minifyExtraOptions)
            .WithKey("mdk", "trace", trace)
            .WithKey("mdk", "ignores", ignores)
            .WithKey("mdk", "namespaces", namespaces);

        // Write the file
        File.WriteAllText(targetIniPath, targetIni.ToString());
    }
}
