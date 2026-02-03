using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using Mdk.Hub.Features.Projects.Overview;
using Mdk.Hub.Utility;

namespace Mdk.Hub.Features.Projects;

/// <summary>
///     Detects whether a project is a valid MDK2 project and determines its type.
///     A valid MDK2 project is identified by the presence of mdk.ini or mdk.local.ini configuration files.
/// </summary>
public static class ProjectDetector
{
    /// <summary>
    ///     Attempts to detect if a .csproj file is a valid MDK2 project.
    ///     Checks for mdk.ini or mdk.local.ini files to validate MDK2 project status.
    /// </summary>
    /// <param name="projectPath">Path to the .csproj file.</param>
    /// <param name="projectInfo">The detected project information if valid.</param>
    /// <returns>True if the project is a valid MDK2 project.</returns>
    public static bool TryDetectProject(string projectPath, out ProjectInfo? projectInfo)
    {
        projectInfo = null;

        if (string.IsNullOrWhiteSpace(projectPath) || !File.Exists(projectPath))
            return false;

        if (!projectPath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
            return false;

        try
        {
            // Check for mdk.ini or mdk.local.ini files (modern and legacy naming)
            var mainIniPath = IniFileFinder.FindMainIni(projectPath);
            var localIniPath = IniFileFinder.FindLocalIni(projectPath);

            if (mainIniPath == null && localIniPath == null)
                return false; // No MDK configuration files found

            // Read the .csproj to determine project type
            var document = XDocument.Load(projectPath);
            var packageReferences = document.Descendants()
                .Where(e => e.Name.LocalName == "PackageReference")
                .Select(e => e.Attribute("Include")?.Value)
                .Where(v => v != null)
                .ToList();

            // Determine project type based on which packager is present
            var hasPbPackager = packageReferences.Any(p => p == EnvironmentMetadata.PbPackagerPackageId);
            var hasModPackager = packageReferences.Any(p => p == EnvironmentMetadata.ModPackagerPackageId);

            // Default to IngameScript if can't determine from packages
            var projectType = hasModPackager ? ProjectType.Mod : ProjectType.ProgrammableBlock;
            var projectName = Path.GetFileNameWithoutExtension(projectPath);
            var lastReferenced = File.GetLastWriteTimeUtc(projectPath);

            projectInfo = new ProjectInfo
            {
                Name = projectName,
                ProjectPath = new CanonicalPath(projectPath),
                Type = projectType,
                LastReferenced = new DateTimeOffset(lastReferenced)
            };

            return true;
        }
        catch (XmlException)
        {
            // Corrupt XML - different from "not an MDK project"
            Debug.WriteLine($"Project file appears to be corrupted (invalid XML): {projectPath}");
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            // Can't read file
            Debug.WriteLine($"Cannot read project file (permission denied): {projectPath}");
            return false;
        }
        catch (IOException ex)
        {
            // Other I/O error
            Debug.WriteLine($"Cannot read project file (I/O error): {projectPath} - {ex.Message}");
            return false;
        }
        catch (Exception ex)
        {
            // Unexpected error - log it
            Debug.WriteLine($"Unexpected error reading project file: {projectPath} - {ex.GetType().Name}: {ex.Message}");
            return false;
        }
    }
}
