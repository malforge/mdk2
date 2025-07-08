using System;
using System.Threading;
using System.Threading.Tasks;
using Semver;

namespace Malforge.Mdk2.Setup;

public static class Nuget
{
    public static async Task<SemVersion> GetPackageVersionAsync(string packageName, string source = "https://api.nuget.org/v3", CancellationToken cancellationToken = default)
    {
        var url = $"{source}/registration5-gz-semver2/{packageName.ToLowerInvariant()}/index.json";
        var json = await Download.DownloadJsonAsync(url, cancellationToken).ConfigureAwait(false);
        if (json == null)
            throw new InvalidOperationException($"Failed to download package version for {packageName} from {url}");

        var version = json["items"]?[0]?["items"]?[0]?["catalogEntry"]?["version"]?.ToString();
        if (version == null)
            throw new InvalidOperationException($"Failed to find version for package {packageName} in the downloaded JSON");

        return SemVersion.Parse(version);
    }
}