using System.Threading;
using System.Threading.Tasks;
using Mal.DependencyInjection;
using Mdk.Hub.Features.Diagnostics;

namespace Mdk.Hub.Features.Updates;

/// <summary>
/// Service for querying NuGet package information.
/// </summary>
[Dependency<INuGetService>]
public class NuGetService(ILogger logger) : INuGetService
{
    readonly ILogger _logger = logger;

    /// <inheritdoc/>
    public async Task<string?> GetLatestVersionAsync(string packageId, CancellationToken cancellationToken = default)
    {
        _logger.Info($"Checking NuGet for latest version of {packageId}");

        // TODO: Query NuGet API
        // https://api.nuget.org/v3-flatcontainer/{packageId}/index.json
        await Task.Delay(100, cancellationToken); // Simulate network delay

        _logger.Debug($"Stub: Returning fake version for {packageId}");
        return "1.0.0-stub";
    }
}
