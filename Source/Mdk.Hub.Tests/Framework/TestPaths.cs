using System.IO;

namespace Mdk.Hub.Tests.Framework;

/// <summary>
///     Builds absolute paths that are rooted on whichever platform the tests are running on.
/// </summary>
/// <remarks>
///     <see cref="Mdk.Hub.Utility.CanonicalPath" /> resolves anything that isn't rooted against the
///     current directory, so a literal such as <c>C:\Projects\Test.csproj</c> is an absolute path on
///     Windows but a relative one on Linux. Tests that assert on the resulting value must build their
///     paths for the running platform rather than hardcoding a drive letter.
/// </remarks>
public static class TestPaths
{
    /// <summary>
    ///     Combines the given segments into an absolute path for the current platform.
    /// </summary>
    /// <param name="segments">Path segments, without a leading root.</param>
    /// <returns>An absolute path, e.g. <c>C:\Projects\Test.csproj</c> or <c>/Projects/Test.csproj</c>.</returns>
    public static string Of(params string[] segments)
    {
        var root = OperatingSystem.IsWindows() ? @"C:\" : "/";
        return Path.Combine(root, Path.Combine(segments));
    }
}
