using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Mal.DependencyInjection;
using Mdk.Hub.Features.Diagnostics;
using NuGet.Versioning;

namespace Mdk.Hub.Features.Updates;

/// <summary>
///     Service for querying GitHub repository information.
/// </summary>
[Singleton<IGitHubService>]
public class GitHubService(ILogger logger) : IGitHubService
{
    readonly HttpClient _httpClient = CreateHttpClient();
    readonly ILogger _logger = logger;

    /// <inheritdoc />
    public async Task<(string Version, bool IsPrerelease)?> GetLatestReleaseAsync(string owner, string repo, bool includePrerelease = false, CancellationToken cancellationToken = default)
    {
        _logger.Info($"Checking GitHub for latest release of {owner}/{repo} (prerelease: {includePrerelease})");

        try
        {
            string url;
            if (includePrerelease)
            {
                // Get all releases and find the latest (including prereleases)
                url = $"https://api.github.com/repos/{owner}/{repo}/releases";
            }
            else
            {
                // Get latest stable release
                url = $"https://api.github.com/repos/{owner}/{repo}/releases/latest";
            }

            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.Info($"No releases found for {owner}/{repo}");
                return null;
            }

            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var doc = JsonDocument.Parse(content);

            if (includePrerelease)
            {
                // Parse array and find the actual latest release by semantic version
                if (doc.RootElement.ValueKind == JsonValueKind.Array && doc.RootElement.GetArrayLength() > 0)
                {
                    _logger.Debug($"Found {doc.RootElement.GetArrayLength()} releases in GitHub API response");
                    
                    // Collect all releases with parsed versions
                    var releases = doc.RootElement.EnumerateArray()
                        .Select(release =>
                        {
                            var tagName = release.GetProperty("tag_name").GetString();
                            var isPrerelease = release.GetProperty("prerelease").GetBoolean();
                            
                            // Strip common prefixes to get version string
                            var versionString = tagName;
                            if (versionString?.StartsWith("hub-v", StringComparison.OrdinalIgnoreCase) == true)
                                versionString = versionString.Substring(5); // "hub-v" is 5 chars
                            else if (versionString?.StartsWith("v", StringComparison.OrdinalIgnoreCase) == true)
                                versionString = versionString.Substring(1);
                            
                            // Try to parse as semantic version
                            if (versionString != null && NuGetVersion.TryParse(versionString, out var version))
                            {
                                _logger.Debug($"  ✓ {tagName} → {versionString} (parsed OK, prerelease={isPrerelease})");
                                return (TagName: tagName, IsPrerelease: isPrerelease, Version: version);
                            }
                            
                            _logger.Debug($"  ✗ {tagName} → {versionString} (parse FAILED)");
                            return (TagName: tagName, IsPrerelease: isPrerelease, Version: (NuGetVersion?)null);
                        })
                        .Where(r => r.Version != null && r.TagName != null)
                        .OrderByDescending(r => r.Version)
                        .ToList();
                    
                    _logger.Debug($"After filtering: {releases.Count} valid releases");
                    
                    if (releases.Count > 0)
                    {
                        var latest = releases[0];
                        _logger.Info($"Latest release of {owner}/{repo}: {latest.TagName} (prerelease: {latest.IsPrerelease})");
                        return (latest.TagName!, latest.IsPrerelease);
                    }
                }

                _logger.Warning($"No releases found in array for {owner}/{repo}");
                return null;
            }
            else
            {
                // Parse single release object
                var tagName = doc.RootElement.GetProperty("tag_name").GetString();
                if (tagName != null)
                {
                    _logger.Info($"Latest stable release of {owner}/{repo}: {tagName}");
                    return (tagName, false);
                }

                _logger.Warning($"No tag_name found in release for {owner}/{repo}");
                return null;
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.Error($"HTTP error checking {owner}/{repo}: {ex.Message}");
            return null;
        }
        catch (JsonException ex)
        {
            _logger.Error($"JSON parse error for {owner}/{repo}: {ex.Message}");
            return null;
        }
        catch (Exception ex)
        {
            _logger.Error($"Unexpected error checking {owner}/{repo}: {ex.Message}");
            return null;
        }
    }

    static HttpClient CreateHttpClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        // GitHub API requires User-Agent header
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("MDK-Hub", "2.0"));
        return client;
    }
}