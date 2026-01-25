using System.Threading;
using System.Threading.Tasks;
using Mal.DependencyInjection;
using Mdk.Hub.Features.Diagnostics;

namespace Mdk.Hub.Features.Updates;

/// <summary>
/// Service for querying GitHub repository information.
/// </summary>
[Dependency<IGitHubService>]
public class GitHubService(ILogger logger) : IGitHubService
{
    readonly ILogger _logger = logger;

    /// <inheritdoc/>
    public async Task<string?> GetLatestReleaseAsync(string owner, string repo, CancellationToken cancellationToken = default)
    {
        _logger.Info($"Checking GitHub for latest release of {owner}/{repo}");

        // TODO: Query GitHub API
        // https://api.github.com/repos/{owner}/{repo}/releases/latest
        await Task.Delay(100, cancellationToken); // Simulate network delay

        _logger.Debug($"Stub: Returning fake version for {owner}/{repo}");
        return "2.0.0-stub";
    }
}
