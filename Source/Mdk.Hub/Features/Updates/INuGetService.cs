using System.Threading;
using System.Threading.Tasks;

namespace Mdk.Hub.Features.Updates;

/// <summary>
///     Service for querying NuGet package information.
/// </summary>
public interface INuGetService
{
    /// <summary>
    ///     Gets the latest version of a NuGet package.
    /// </summary>
    /// <param name="packageId">The package ID (e.g., "Mal.Mdk2.PbAnalyzers").</param>
    /// <param name="includePrerelease">Whether to include prerelease versions.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The latest version string, or null if not found.</returns>
    Task<string?> GetLatestVersionAsync(string packageId, bool includePrerelease, CancellationToken cancellationToken = default);
}
