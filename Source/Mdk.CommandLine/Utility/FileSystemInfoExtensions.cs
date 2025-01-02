using System.IO;

namespace Mdk.CommandLine.Utility;

public static class FileSystemInfoExtensions
{
    public static string GetPathRelativeTo(this FileSystemInfo info, FileInfo? basePath)
    {
        if (basePath?.Directory == null)
            return info.FullName;
        return info.GetPathRelativeTo(basePath.Directory);
    }

    public static string GetPathRelativeTo(this FileSystemInfo info, DirectoryInfo? basePath)
    {
        if (basePath == null)
            return info.FullName;
        return info.GetPathRelativeTo(basePath.FullName);
    }

    public static string GetPathRelativeTo(this FileSystemInfo info, string? basePath)
    {
        if (string.IsNullOrWhiteSpace(basePath))
            return info.FullName;
        var result = Path.GetRelativePath(basePath, info.FullName);
        if (result.StartsWith('.') || result.StartsWith(Path.DirectorySeparatorChar) || result.StartsWith(Path.AltDirectorySeparatorChar) || Path.IsPathRooted(result))
            return result;
        return $".{Path.DirectorySeparatorChar}{Path.GetRelativePath(basePath, info.FullName)}";
    }
}