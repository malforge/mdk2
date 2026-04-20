using System.Text.Json;
using Mdk.Hub.Features.Updates;

namespace Mdk.Hub.Tests.Features.Updates;

[TestFixture]
public class DotNetSdkDownloadUrlTests
{
    [Test]
    public void SelectWindowsX64InstallerUrl_ParsesRealWorldJsonStructure()
    {
        // Verifies that SelectWindowsX64InstallerUrl correctly identifies the
        // Windows x64 SDK installer from a realistic Microsoft releases.json payload.
        var microsoftReleasesJson = """
            {
              "releases-index": [
                {
                  "channel-version": "9.0",
                  "latest-release": "9.0.4",
                  "latest-release-date": "2024-05-14",
                  "security": false,
                  "latest-sdk": "9.0.104",
                  "latest-sdk-date": "2024-05-14",
                  "latest-runtime": "9.0.4",
                  "latest-runtime-date": "2024-05-14"
                }
              ],
              "releases": [
                {
                  "release-version": "9.0.4",
                  "release-date": "2024-05-14",
                  "runtime": {
                    "version": "9.0.4",
                    "files": []
                  },
                  "sdk": {
                    "version": "9.0.104",
                    "files": [
                      {
                        "name": "dotnet-sdk-win-x64.exe",
                        "rid": "win-x64",
                        "url": "https://builds.dotnet.microsoft.com/dotnet/Sdk/9.0.104/dotnet-sdk-9.0.104-win-x64.exe",
                        "hash": "somehash"
                      },
                      {
                        "name": "dotnet-sdk-win-x64.zip",
                        "rid": "win-x64",
                        "url": "https://builds.dotnet.microsoft.com/dotnet/Sdk/9.0.104/dotnet-sdk-9.0.104-win-x64.zip"
                      },
                      {
                        "name": "dotnet-sdk-osx-x64.pkg",
                        "rid": "osx-x64",
                        "url": "https://builds.dotnet.microsoft.com/dotnet/Sdk/9.0.104/dotnet-sdk-9.0.104-osx-x64.pkg"
                      },
                      {
                        "name": "dotnet-sdk-linux-x64.tar.gz",
                        "rid": "linux-x64",
                        "url": "https://builds.dotnet.microsoft.com/dotnet/Sdk/9.0.104/dotnet-sdk-9.0.104-linux-x64.tar.gz"
                      }
                    ]
                  }
                }
              ]
            }
            """;

        // Parse the JSON and call the production method directly
        using var doc = JsonDocument.Parse(microsoftReleasesJson);

        var result = UpdateManager.SelectWindowsX64InstallerUrl(doc.RootElement);

        Assert.That(result, Is.Not.Null.And.Not.Empty);
        Assert.That(result, Does.Contain("win-x64"));
        Assert.That(result, Does.Contain(".exe"));
        Assert.That(result, Does.StartWith("https://"));
    }

    [Test]
    [Explicit("Integration test - requires network access to Microsoft API")]
    public async Task GetLatestDotNet9SdkDownloadUrlAsync_IntegrationTest_CanFetchFromApi()
    {
        // This test verifies that we can actually fetch and parse the real Microsoft API response
        using var httpClient = new HttpClient();
        httpClient.Timeout = TimeSpan.FromSeconds(10);

        var releasesUrl = "https://dotnetcli.blob.core.windows.net/dotnet/release-metadata/9.0/releases.json";
        var releasesJson = await httpClient.GetStringAsync(releasesUrl);
        
        // Should be valid JSON
        using var doc = JsonDocument.Parse(releasesJson);
        Assert.That(doc.RootElement.TryGetProperty("releases", out _), Is.True,
            "API response should have 'releases' property");
    }
}
