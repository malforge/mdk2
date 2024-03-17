using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using Mdk.CommandLine.SharedApi;

namespace Mdk.CommandLine;

public static class Nuget
{
    const string NugetRepoUrl = "https://api.nuget.org/v3-flatcontainer/{0}/index.json";

    public static async IAsyncEnumerable<SemanticVersion> GetPackageVersionsAsync(IHttpClient httpClient, string packageName)
    {
        var requestUrl = string.Format(NugetRepoUrl, packageName.ToLower());
        JsonDocument doc;
        JsonElement.ArrayEnumerator versions;
        try
        {
            var response = await httpClient.GetAsync(requestUrl);
            if (!response.IsSuccessStatusCode)
                yield break;

            var jsonResponse = await response.Content.ReadAsStringAsync();
            doc = JsonDocument.Parse(jsonResponse);
            versions = doc.RootElement.GetProperty("versions").EnumerateArray();
        }
        catch (Exception)
        {
            yield break;
        }

        foreach (var versionString in versions.Select(version => version.GetString()).OfType<string>())
        {
            if (SemanticVersion.TryParse(versionString, out var semVer))
                yield return semVer;
        }
    }
}