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
    /// Determines whether a path relative to some base points outside that base.
    /// </summary>
    /// <param name="relativePath">A path relative to the base, or a rooted path when none exists.</param>
    /// <returns><c>true</c> when the path is rooted or its first segment is <c>..</c>.</returns>
    public static bool LeavesBase(string relativePath)
    {
        if (Path.IsPathRooted(relativePath))
            return true;
        var segments = relativePath.Split(PathSeparators, StringSplitOptions.RemoveEmptyEntries);
        return segments.SkipWhile(segment => segment == ".").FirstOrDefault() == "..";
    }

    /// <summary>
    /// Combines a base path with a relative path, ensuring the result is within the base path.
    /// </summary>
    /// <param name="basePath">The base path to combine with.</param>
    /// <param name="path">The relative path to combine.</param>
    /// <returns>The combined path, ensuring it is within the base path.</returns>
    /// <exception cref="ArgumentException"><paramref name="path" /> is rooted.</exception>
    public static string CombineInside(string basePath, string path)
    {
        if (Path.IsPathRooted(path))
            throw new ArgumentException($"'{path}' is not a relative path, so it cannot be placed inside '{basePath}'.", nameof(path));

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