using Mdk.CommandLine.SharedApi;
using Microsoft.Extensions.FileSystemGlobbing;

namespace Mdk.CommandLine.IngameScript.Pack;

/// <summary>
///     A file filter that determines whether a file should be included in a pack operation.
/// </summary>
public class PackInclusionFilter : IFileFilter
{
    readonly Matcher _ignoreMatcher;
    readonly string _rootPath;

    /// <summary>
    ///     Creates a new instance of <see cref="PackInclusionFilter" />.
    /// </summary>
    /// <param name="parameters"></param>
    /// <param name="rootPath"></param>
    public PackInclusionFilter(IParameters parameters, string rootPath)
    {
        _rootPath = rootPath;
        var ignoreMatcher = new Matcher();
        foreach (var ignore in parameters.PackVerb.Ignores)
            ignoreMatcher.AddInclude(ignore);
        _ignoreMatcher = ignoreMatcher;
    }

    /// <inheritdoc />
    public bool IsMatch(string path) => !_ignoreMatcher.Match(_rootPath, path).HasMatches;
}