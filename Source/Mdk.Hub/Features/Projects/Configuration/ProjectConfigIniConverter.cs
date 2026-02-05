using Mdk.Hub.Utility;

namespace Mdk.Hub.Features.Projects.Configuration;

/// <summary>
///     Utility for converting between ProjectConfigLayer (typed) and INI format (string key-value pairs).
/// </summary>
static class ProjectConfigIniConverter
{
    /// <summary>
    ///     Updates an INI file from a ProjectConfigLayer, converting typed values to INI string format.
    /// </summary>
    public static Ini UpdateIniFromLayer(Ini ini, string section, ProjectConfigLayer layer, bool removeNulls = false, bool forceAutoForNullPaths = false)
    {
        // Type - convert enum to lowercase string
        ini = UpdateIniValue(ini, section, "type", layer.Type?.ToString().ToLowerInvariant(), removeNulls);
        
        // Interactive - convert enum to PascalCase string (OpenHub, ShowNotification, DoNothing)
        ini = UpdateIniValue(ini, section, "interactive", layer.Interactive?.ToString(), removeNulls);
        
        // Trace - convert bool to "on"/"off"
        ini = UpdateIniValue(ini, section, "trace", layer.Trace.HasValue ? (layer.Trace.Value ? "on" : "off") : null, removeNulls);
        
        // Minify - convert enum to lowercase string
        ini = UpdateIniValue(ini, section, "minify", layer.Minify?.ToString().ToLowerInvariant(), removeNulls);
        
        // MinifyExtraOptions - convert enum to lowercase string
        ini = UpdateIniValue(ini, section, "minifyextraoptions", layer.MinifyExtraOptions?.ToString().ToLowerInvariant(), removeNulls);
        
        // Ignores - convert array to comma-separated string
        ini = UpdateIniValue(ini, section, "ignores", layer.Ignores.HasValue ? string.Join(",", layer.Ignores.Value) : null, removeNulls);
        
        // Namespaces - convert array to comma-separated string
        ini = UpdateIniValue(ini, section, "namespaces", layer.Namespaces.HasValue ? string.Join(",", layer.Namespaces.Value) : null, removeNulls);
        
        // Output - CanonicalPath? (null = "auto" in INI)
        string? outputValue = layer.Output?.Value;
        if (forceAutoForNullPaths && outputValue == null)
            outputValue = "auto";
        ini = UpdateIniValue(ini, section, "output", outputValue, removeNulls);
        
        // BinaryPath - CanonicalPath? (null = "auto" in INI)  
        string? binaryPathValue = layer.BinaryPath?.Value;
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
