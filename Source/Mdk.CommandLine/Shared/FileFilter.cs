using System.Collections.Generic;
using Mdk.CommandLine.Shared.Api;
using Microsoft.Extensions.FileSystemGlobbing;

namespace Mdk.CommandLine.Shared;

/// <inheritdoc />
public class FileFilter : IFileFilter
{
    /// <summary>
    ///     A filter that allows all files to pass through.
    /// </summary>
    public static readonly IFileFilter Passthrough = new NullFilter();

    readonly Matcher _ignoreMatcher;
    readonly string _rootPath;

    /// <summary>
    ///     Creates a new instance of <see cref="FileFilter" />.
    /// </summary>
    /// <param name="filters"></param>
    /// <param name="rootPath"></param>
    public FileFilter(IReadOnlyList<string>? filters, string rootPath)
    {
        _rootPath = rootPath;
        var ignoreMatcher = new Matcher();
        if (filters != null)
        {
            foreach (var ignore in filters)
                ignoreMatcher.AddInclude(ignore);
        }

        _ignoreMatcher = ignoreMatcher;
    }

    /// <inheritdoc />
    public bool IsMatch(string? path)
    {
        if (string.IsNullOrEmpty(path))
            return false;
        return !_ignoreMatcher.Match(_rootPath, path).HasMatches;
    }

    class NullFilter : IFileFilter
    {
        public bool IsMatch(string path)
        {
            return false;
        }
    }
}