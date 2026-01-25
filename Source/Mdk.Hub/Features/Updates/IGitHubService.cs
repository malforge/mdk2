using System.Threading;
using System.Threading.Tasks;

namespace Mdk.Hub.Features.Updates;

/// <summary>
/// Service for querying GitHub repository information.
/// </summary>
public interface IGitHubService
{
    /// <summary>
    /// Gets the latest release version from a GitHub repository.
    /// </summary>
    /// <param name="owner">The repository owner (e.g., "malforge").</param>
    /// <param name="repo">The repository name (e.g., "mdk2").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The latest release version string, or null if not found.</returns>
    Task<string?> GetLatestReleaseAsync(string owner, string repo, CancellationToken cancellationToken = default);
}
