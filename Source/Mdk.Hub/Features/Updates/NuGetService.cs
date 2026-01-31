using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Mal.DependencyInjection;
using Mdk.Hub.Features.Diagnostics;

namespace Mdk.Hub.Features.Updates;

/// <summary>
///     Service for querying NuGet package information.
/// </summary>
[Singleton<INuGetService>]
public class NuGetService(ILogger logger) : INuGetService
{
    readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromSeconds(10) };
    readonly ILogger _logger = logger;

    /// <inheritdoc />
    public async Task<string?> GetLatestVersionAsync(string packageId, CancellationToken cancellationToken = default)
    {
        _logger.Info($"Checking NuGet for latest version of {packageId}");

        try
        {
            var url = $"https://api.nuget.org/v3-flatcontainer/{packageId.ToLowerInvariant()}/index.json";
            var response = await _httpClient.GetStringAsync(url, cancellationToken);

            var doc = JsonDocument.Parse(response);
            var versions = doc.RootElement.GetProperty("versions").EnumerateArray()
                .Select(v => v.GetString())
                .Where(v => v != null)
                .ToList();

            if (versions.Count == 0)
            {
                _logger.Warning($"No versions found for {packageId}");
                return null;
            }

            var latestVersion = versions[^1]; // Last version in the array
            _logger.Info($"Latest version of {packageId}: {latestVersion}");
            return latestVersion;
        }
        catch (HttpRequestException ex)
        {
            _logger.Error($"HTTP error checking {packageId}: {ex.Message}");
            return null;
        }
        catch (JsonException ex)
        {
            _logger.Error($"JSON parse error for {packageId}: {ex.Message}");
            return null;
        }
        catch (Exception ex)
        {
            _logger.Error($"Unexpected error checking {packageId}: {ex.Message}");
            return null;
        }
    }
}