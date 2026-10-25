using System;
using System.IO;

namespace Mdk.CommandLine.Mod.Pack;

/// <summary>
///     Finds the shared project (<c>.projitems</c>) a file from outside the project belongs to.
/// </summary>
internal static class SharedProjectFile
{
    // Windows paths are case-insensitive, Linux paths are not.
    static readonly StringComparison PathComparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

    /// <summary>
    ///     Finds the <c>.projitems</c> in the nearest folder at or above <paramref name="file" /> that holds any,
    ///     stopping before the first folder that contains <paramref name="projectDirectory" />.
    /// </summary>
    /// <param name="file">The file to find the shared project for.</param>
    /// <param name="projectDirectory">The directory of the project being packed.</param>
    /// <returns>The shared project file, or <c>null</c> when there is none or that folder holds more than one.</returns>
    public static FileInfo? FindFor(FileInfo file, DirectoryInfo projectDirectory)
    {
        var projectPath = Path.TrimEndingDirectorySeparator(projectDirectory.FullName) + Path.DirectorySeparatorChar;
        for (var directory = file.Directory; directory != null; directory = directory.Parent)
        {
            var directoryPath = Path.TrimEndingDirectorySeparator(directory.FullName) + Path.DirectorySeparatorChar;
            if (projectPath.StartsWith(directoryPath, PathComparison))
                return null;
            if (!directory.Exists)
                continue;
            var sharedProjects = directory.GetFiles("*.projitems");
            if (sharedProjects.Length > 0)
                return sharedProjects.Length == 1 ? sharedProjects[0] : null;
        }

        return null;
    }
}
