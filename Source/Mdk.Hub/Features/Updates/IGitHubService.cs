using System.Threading;
using System.Threading.Tasks;

namespace Mdk.Hub.Features.Updates;

/// <summary>
///     Service for querying GitHub repository information.
/// </summary>
public interface IGitHubService
{
    /// <summary>
    ///     Gets the latest release version from a GitHub repository.
    /// </summary>
    /// <param name="owner">The repository owner (e.g., "malforge").</param>
    /// <param name="repo">The repository name (e.g., "mdk2").</param>
    /// <param name="includePrerelease">Whether to include prerelease versions.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A tuple with the version string and whether it's a prerelease, or null if not found.</returns>
    Task<(string Version, bool IsPrerelease)?> GetLatestReleaseAsync(string owner, string repo, bool includePrerelease = false, CancellationToken cancellationToken = default);
}