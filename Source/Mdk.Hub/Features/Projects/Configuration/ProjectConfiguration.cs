using System;
using System.IO;
using Mdk.Hub.Utility;

namespace Mdk.Hub.Features.Projects.Configuration;

/// <summary>
/// Represents the merged configuration from mdk.ini and mdk.local.ini for a project.
/// </summary>
public class ProjectConfiguration
{
    /// <summary>
    /// The raw INI data from mdk.ini (null if file doesn't exist).
    /// </summary>
    public Ini? MainIni { get; init; }
    
    /// <summary>
    /// The raw INI data from mdk.local.ini (null if file doesn't exist).
    /// </summary>
    public Ini? LocalIni { get; init; }
    
    /// <summary>
    /// Path to the mdk.ini file (if it exists).
    /// </summary>
    public string? MainIniPath { get; init; }
    
    /// <summary>
    /// Path to the mdk.local.ini file (if it exists).
    /// </summary>
    public string? LocalIniPath { get; init; }
    
    /// <summary>
    /// Path to the project (.csproj) file.
    /// </summary>
    public string ProjectPath { get; init; } = string.Empty;

    // Configuration properties with source tracking
    
    /// <summary>
    /// Project type: "programmableblock" or "mod".
    /// </summary>
    public ConfigurationValue<string> Type { get; init; }
    
    /// <summary>
    /// Minification level: "none", "trim", "stripcomments", "lite", or "full".
    /// </summary>
    public ConfigurationValue<string> Minify { get; init; }
    
    /// <summary>
    /// Extra minification options: "none" or "nomembertrimming".
    /// </summary>
    public ConfigurationValue<string> MinifyExtraOptions { get; init; }
    
    /// <summary>
    /// Enable trace output: "on" or "off".
    /// </summary>
    public ConfigurationValue<bool> Trace { get; init; }
    
    /// <summary>
    /// Comma-separated list of glob patterns to ignore.
    /// </summary>
    public ConfigurationValue<string> Ignores { get; init; }
    
    /// <summary>
    /// Comma-separated list of allowed namespaces.
    /// </summary>
    public ConfigurationValue<string> Namespaces { get; init; }
    
    /// <summary>
    /// Output path: "auto" or specific path.
    /// </summary>
    public ConfigurationValue<string> Output { get; init; }
    
    /// <summary>
    /// Binary path override: "auto" or specific path.
    /// </summary>
    public ConfigurationValue<string> BinaryPath { get; init; }

    /// <summary>
    /// Gets the resolved output path, converting "auto" to the actual deployment location.
    /// </summary>
    /// <returns>The resolved path, or null if output is not set and no default can be determined.</returns>
    public string? GetResolvedOutputPath()
    {
        var output = Output.Value;
        
        // If not set or not "auto", return as-is
        if (string.IsNullOrWhiteSpace(output) || !string.Equals(output, "auto", StringComparison.OrdinalIgnoreCase))
            return output;

        // Resolve "auto" based on project type
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var projectName = System.IO.Path.GetFileNameWithoutExtension(ProjectPath);
        
        var type = Type.Value.ToLowerInvariant();
        return type switch
        {
            "programmableblock" => System.IO.Path.Combine(appData, "SpaceEngineers", "IngameScripts", "local", projectName),
            "mod" => System.IO.Path.Combine(appData, "SpaceEngineers", "Mods", projectName),
            _ => null
        };
    }
}
