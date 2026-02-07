using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Mdk.CommandLine.Shared;

/// <summary>
/// Provides access to global MDK Hub settings
/// </summary>
public static class GlobalSettings
{
    static string SettingsFilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "MDK2", "settings.json");

    /// <summary>
    /// Gets the custom auto output path for scripts from global settings, or null if not set
    /// </summary>
    public static string? GetCustomAutoScriptOutputPath()
    {
        return GetCustomAutoPath("CustomAutoScriptOutputPath");
    }

    /// <summary>
    /// Gets the custom auto output path for mods from global settings, or null if not set
    /// </summary>
    public static string? GetCustomAutoModOutputPath()
    {
        return GetCustomAutoPath("CustomAutoModOutputPath");
    }

    /// <summary>
    /// Gets the custom auto binary path from global settings, or null if not set
    /// </summary>
    public static string? GetCustomAutoBinaryPath()
    {
        return GetCustomAutoPath("CustomAutoBinaryPath");
    }

    static string? GetCustomAutoPath(string key)
    {
        try
        {
            if (!File.Exists(SettingsFilePath))
                return null;

            var json = File.ReadAllText(SettingsFilePath);
            var settings = JsonNode.Parse(json)?.AsObject();
            
            // Settings are stored in a HubSettings object
            if (settings?.TryGetPropertyValue("HubSettings", out var hubSettingsNode) == true)
            {
                var hubSettings = hubSettingsNode?.AsObject();
                if (hubSettings?.TryGetPropertyValue(key, out var value) == true)
                {
                    var customPath = value?.GetValue<string>();
                    if (!string.IsNullOrWhiteSpace(customPath) && customPath != "auto")
                        return customPath;
                }
            }
        }
        catch
        {
            // If we can't read settings, just return null (use default)
        }

        return null;
    }
}
