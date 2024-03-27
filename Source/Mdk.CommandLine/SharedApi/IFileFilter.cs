namespace Mdk.CommandLine.SharedApi;

/// <summary>
///     Determines whether a file path should be included in a process.
/// </summary>
public interface IFileFilter
{
    /// <summary>
    ///     Determines whether the specified path matches the filter.
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    bool IsMatch(string path);
}