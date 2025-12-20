using System;
using System.IO;

namespace Mdk.CommandLine.Utility;

/// <summary>
/// Utility for finding INI configuration files with support for both new simplified naming
/// (mdk.ini, mdk.local.ini) and legacy project-name-based naming ({projectName}.mdk.ini).
/// </summary>
public static class IniFileFinder
{
    /// <summary>
    /// Find the main INI file for a project. Checks new naming convention first, then falls back to legacy naming.
    /// </summary>
    /// <param name="projectFilePath">Full path to the .csproj file</param>
    /// <returns>Path to the INI file if found, null otherwise</returns>
    public static string? FindMainIni(string projectFilePath)
    {
        if (string.IsNullOrEmpty(projectFilePath))
            return null;

        var projectDirectory = Path.GetDirectoryName(projectFilePath);
        if (string.IsNullOrEmpty(projectDirectory))
            return null;

        // Priority 1: Check for new naming convention (mdk.ini)
        var newStylePath = Path.Combine(projectDirectory, "mdk.ini");
        if (File.Exists(newStylePath))
            return newStylePath;

        // Priority 2: Fall back to legacy naming ({projectName}.mdk.ini)
        var legacyPath = Path.ChangeExtension(projectFilePath, ".mdk.ini");
        if (File.Exists(legacyPath))
            return legacyPath;

        return null;
    }

    /// <summary>
    /// Find the local INI file for a project. Checks new naming convention first, then falls back to legacy naming.
    /// </summary>
    /// <param name="projectFilePath">Full path to the .csproj file</param>
    /// <returns>Path to the local INI file if found, null otherwise</returns>
    public static string? FindLocalIni(string projectFilePath)
    {
        if (string.IsNullOrEmpty(projectFilePath))
            return null;

        var projectDirectory = Path.GetDirectoryName(projectFilePath);
        if (string.IsNullOrEmpty(projectDirectory))
            return null;

        // Priority 1: Check for new naming convention (mdk.local.ini)
        var newStylePath = Path.Combine(projectDirectory, "mdk.local.ini");
        if (File.Exists(newStylePath))
            return newStylePath;

        // Priority 2: Fall back to legacy naming ({projectName}.mdk.local.ini)
        var legacyPath = Path.ChangeExtension(projectFilePath, ".mdk.local.ini");
        if (File.Exists(legacyPath))
            return legacyPath;

        return null;
    }

    /// <summary>
    /// Get paths for the new naming convention. Does not check if files exist.
    /// </summary>
    /// <param name="projectFilePath">Full path to the .csproj file</param>
    /// <returns>Tuple of (mainIni, localIni) paths using new naming convention</returns>
    public static (string mainIni, string localIni) GetNewIniPaths(string projectFilePath)
    {
        if (string.IsNullOrEmpty(projectFilePath))
            throw new ArgumentException("Project file path cannot be null or empty", nameof(projectFilePath));

        var projectDirectory = Path.GetDirectoryName(projectFilePath);
        if (string.IsNullOrEmpty(projectDirectory))
            throw new ArgumentException("Cannot determine project directory from path", nameof(projectFilePath));

        var mainIni = Path.Combine(projectDirectory, "mdk.ini");
        var localIni = Path.Combine(projectDirectory, "mdk.local.ini");

        return (mainIni, localIni);
    }

    /// <summary>
    /// Get paths for the legacy naming convention. Does not check if files exist.
    /// </summary>
    /// <param name="projectFilePath">Full path to the .csproj file</param>
    /// <returns>Tuple of (mainIni, localIni) paths using legacy naming convention</returns>
    public static (string mainIni, string localIni) GetLegacyIniPaths(string projectFilePath)
    {
        if (string.IsNullOrEmpty(projectFilePath))
            throw new ArgumentException("Project file path cannot be null or empty", nameof(projectFilePath));

        var mainIni = Path.ChangeExtension(projectFilePath, ".mdk.ini");
        var localIni = Path.ChangeExtension(projectFilePath, ".mdk.local.ini");

        return (mainIni, localIni);
    }
}
