using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Mdk.Hub.Features.Projects.Overview;
using Mdk.Hub.Utility;

namespace Mdk.Hub.Features.Projects;

/// <summary>
/// Detects whether a project is a valid MDK2 project and determines its type.
/// </summary>
public static class ProjectDetector
{
    /// <summary>
    /// Attempts to detect if a .csproj file is a valid MDK2 project.
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
            var document = XDocument.Load(projectPath);
            var packageReferences = document.Descendants()
                .Where(e => e.Name.LocalName == "PackageReference")
                .Select(e => e.Attribute("Include")?.Value)
                .Where(v => v != null)
                .ToList();

            // Check for MDK2 packages
            var hasPbPackager = packageReferences.Any(p => p == "Mal.Mdk2.PbPackager");
            var hasModPackager = packageReferences.Any(p => p == "Mal.Mdk2.ModPackager");

            if (!hasPbPackager && !hasModPackager)
                return false;

            // Determine project type based on which packager is present
            var projectType = hasPbPackager ? ProjectType.IngameScript : ProjectType.Mod;
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
        catch (System.Xml.XmlException)
        {
            // Corrupt XML - different from "not an MDK project"
            System.Diagnostics.Debug.WriteLine($"Project file appears to be corrupted (invalid XML): {projectPath}");
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            // Can't read file
            System.Diagnostics.Debug.WriteLine($"Cannot read project file (permission denied): {projectPath}");
            return false;
        }
        catch (IOException ex)
        {
            // Other I/O error
            System.Diagnostics.Debug.WriteLine($"Cannot read project file (I/O error): {projectPath} - {ex.Message}");
            return false;
        }
        catch (Exception ex)
        {
            // Unexpected error - log it
            System.Diagnostics.Debug.WriteLine($"Unexpected error reading project file: {projectPath} - {ex.GetType().Name}: {ex.Message}");
            return false;
        }
    }
}
