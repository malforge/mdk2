using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Mal.DependencyInjection;
using Mdk.Hub.Features.Diagnostics;

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
    public async Task<string?> GetLatestReleaseAsync(string owner, string repo, CancellationToken cancellationToken = default)
    {
        _logger.Info($"Checking GitHub for latest release of {owner}/{repo}");

        try
        {
            var url = $"https://api.github.com/repos/{owner}/{repo}/releases/latest";
            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.Info($"No releases found for {owner}/{repo}");
                return null;
            }

            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var doc = JsonDocument.Parse(content);

            var tagName = doc.RootElement.GetProperty("tag_name").GetString();
            if (tagName != null)
            {
                _logger.Info($"Latest release of {owner}/{repo}: {tagName}");
                return tagName;
            }

            _logger.Warning($"No tag_name found in release for {owner}/{repo}");
            return null;
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