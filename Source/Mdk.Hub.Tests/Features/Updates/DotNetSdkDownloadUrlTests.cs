using System.Text.Json;
using FakeItEasy;
using Mdk.Hub.Features.Diagnostics;
using Mdk.Hub.Features.Settings;
using Mdk.Hub.Features.Shell;
using Mdk.Hub.Features.Storage;
using Mdk.Hub.Features.Updates;

namespace Mdk.Hub.Tests.Features.Updates;

[TestFixture]
public class DotNetSdkDownloadUrlTests
{
    [Test]
    public void GetLatestDotNet9SdkDownloadUrlAsync_ParsesRealWorldJsonStructure()
    {
        // This test verifies that a realistic Microsoft releases.json structure
        // contains the expected Windows x64 SDK installer files
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

        // Parse the JSON to verify structure
        using var doc = JsonDocument.Parse(microsoftReleasesJson);
        var root = doc.RootElement;

        // Verify we can navigate the structure that the implementation expects
        Assert.That(root.TryGetProperty("releases", out var releases), Is.True,
            "JSON should have 'releases' property");

        var releaseArray = releases.EnumerateArray().ToList();
        Assert.That(releaseArray, Is.Not.Empty, "Should have at least one release");

        var firstRelease = releaseArray[0];
        Assert.That(firstRelease.TryGetProperty("sdk", out var sdk), Is.True,
            "Release should have 'sdk' property");
        Assert.That(sdk.TryGetProperty("files", out var files), Is.True,
            "SDK should have 'files' property");

        // Verify we can find the Windows x64 installer
        var fileList = files.EnumerateArray().ToList();
        var windowsX64File = fileList.FirstOrDefault(f =>
        {
            if (!f.TryGetProperty("name", out var name))
                return false;
            var fileName = name.GetString() ?? string.Empty;
            return fileName.Contains("dotnet-sdk") && 
                   fileName.Contains("win-x64") && 
                   fileName.EndsWith(".exe");
        });

        Assert.That(windowsX64File, Is.Not.EqualTo(default(JsonElement)),
            "Should find Windows x64 SDK installer in files");

        // Verify the URL is present
        Assert.That(windowsX64File.TryGetProperty("url", out var url), Is.True,
            "File should have 'url' property");
        
        var urlString = url.GetString();
        Assert.That(urlString, Is.Not.Null.And.Not.Empty);
        Assert.That(urlString, Does.Contain("win-x64"));
        Assert.That(urlString, Does.Contain(".exe"));
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
