using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Mdk.CommandLine.Utility;

/// <summary>
/// Provides utility methods for working with file paths.
/// </summary>
public static class PathEx
{
    static readonly char[] PathSeparators = { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };

    /// <summary>
    /// Combines a base path with a relative path, ensuring the result is within the base path.
    /// </summary>
    /// <param name="basePath">The base path to combine with.</param>
    /// <param name="path">The relative path to combine.</param>
    /// <returns>The combined path, ensuring it is within the base path.</returns>
    public static string CombineInside(string basePath, string path)
    {
        if (Path.IsPathRooted(path))
            return path;

        basePath = Path.GetFullPath(basePath);
        var segments = path.Split(PathSeparators, StringSplitOptions.RemoveEmptyEntries);
        var stack = new Stack<string>();
        foreach (var segment in segments)
        {
            if (segment == ".")
                continue;
            if (segment == "..")
            {
                if (stack.Count > 0)
                    stack.Pop();
            }
            else
                stack.Push(segment);
        }

        var sanitizedSegments = stack.Reverse().ToArray();
        return Path.Combine(basePath, Path.Combine(sanitizedSegments));
    }
}