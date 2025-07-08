using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Semver;

// ReSharper disable UnusedAutoPropertyAccessor.Local
// ReSharper disable CollectionNeverUpdated.Local
// ReSharper disable UnusedMember.Local
// ReSharper disable ClassNeverInstantiated.Local

namespace Malforge.Mdk2.Setup;

public class DotNetDownloads
{
    const string ReleasesIndexUrl =
        "https://raw.githubusercontent.com/dotnet/core/main/release-notes/releases-index.json";

    DotNetDownloads(DotNetIndex dotNetIndex, DotNetReleases releases)
    {
        Index = dotNetIndex;
        Channel = releases;
        SelectedRelease = releases.Releases.Where(r => !r.ReleaseSemVersion.IsPrerelease)
            .OrderByDescending(r => r.ReleaseSemVersion, SemVersion.SortOrderComparer)
            .FirstOrDefault();

        var currentRid = RuntimeInformation.RuntimeIdentifier;
        if (currentRid.StartsWith("win-"))
        {
            // Windows installers (.exe)
            SelectedSdkInstallFile = SelectedRelease?.Sdk.Files.FirstOrDefault(f => f.Rid == currentRid && f.Name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase));
            SelectedRuntimeInstallFile = SelectedRelease?.Runtime.Files.FirstOrDefault(f => f.Rid == currentRid && f.Name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase));
        }
        else if (currentRid.StartsWith("linux-"))
        {
            // Linux archives (.tar.gz)
            SelectedSdkInstallFile = SelectedRelease?.Sdk.Files.FirstOrDefault(f => f.Rid == currentRid && f.Name.EndsWith(".tar.gz", StringComparison.OrdinalIgnoreCase));
            SelectedRuntimeInstallFile = SelectedRelease?.Runtime.Files.FirstOrDefault(f => f.Rid == currentRid && f.Name.EndsWith(".tar.gz", StringComparison.OrdinalIgnoreCase));
        }
        else throw new NotSupportedException($"Unsupported RID: {currentRid}");
    }

    public File? SelectedRuntimeInstallFile { get; }
    public File? SelectedSdkInstallFile { get; }
    public DotNetIndex Index { get; }
    public DotNetReleases Channel { get; }
    public Release? SelectedRelease { get; set; }

    public static async Task<DotNetDownloads> CreateAsync(string channel, CancellationToken cancellationToken = default)
    {
        var releasesIndex = await Download.DownloadJsonAsync<DotNetIndex>(ReleasesIndexUrl, cancellationToken);
        var release = releasesIndex?.ReleasesIndex.FirstOrDefault(r => r.ChannelVersion == channel);
        if (release == null)
            throw new InvalidOperationException($"Channel '{channel}' not found.");

        if (string.IsNullOrWhiteSpace(release.ReleasesJson))
            throw new InvalidOperationException($"Channel '{channel}' does not have a releases.json file.");

        var releases = await Download.DownloadJsonAsync<DotNetReleases>(release.ReleasesJson, cancellationToken);
        if (releases == null)
            throw new InvalidOperationException($"Channel '{channel}' releases.json file not found.");

        return new DotNetDownloads(releasesIndex!, releases!);
    }

    [method: JsonConstructor]
    public class AspnetcoreRuntime(
        string version,
        string versionDisplay,
        IReadOnlyList<string> versionAspnetcoremodule,
        string vsVersion,
        IReadOnlyList<File> files)
    {
        [JsonPropertyName("version")]
        public string Version { get; } = version;

        [JsonPropertyName("version-display")]
        public string VersionDisplay { get; } = versionDisplay;

        [JsonPropertyName("version-aspnetcoremodule")]
        public IReadOnlyList<string> VersionAspnetcoremodule { get; } = versionAspnetcoremodule;

        [JsonPropertyName("vs-version")]
        public string VsVersion { get; } = vsVersion;

        [JsonPropertyName("files")]
        public IReadOnlyList<File> Files { get; } = files;
    }

    [method: JsonConstructor]
    public class CveList(string cveId, string cveUrl)
    {
        [JsonPropertyName("cve-id")]
        public string CveId { get; } = cveId;

        [JsonPropertyName("cve-url")]
        public string CveUrl { get; } = cveUrl;
    }

    [method: JsonConstructor]
    public class File(string name, string rid, string url, string hash, string akams)
    {
        [JsonPropertyName("name")]
        public string Name { get; } = name;

        [JsonPropertyName("rid")]
        public string Rid { get; } = rid;

        [JsonPropertyName("url")]
        public string Url { get; } = url;

        [JsonPropertyName("hash")]
        public string Hash { get; } = hash;

        [JsonPropertyName("akams")]
        public string Akams { get; } = akams;
    }

    [method: JsonConstructor]
    public class Release(
        string releaseDate,
        string releaseVersion,
        bool? security,
        IReadOnlyList<CveList> cveList,
        string releaseNotes,
        Runtime runtime,
        Sdk sdk,
        IReadOnlyList<Sdk> sdks,
        AspnetcoreRuntime aspnetcoreRuntime,
        Windowsdesktop windowsdesktop)
    {
        SemVersion? _releaseSemVersion;

        [JsonPropertyName("release-date")]
        public string ReleaseDate { get; } = releaseDate;

        [JsonPropertyName("release-version")]
        public string ReleaseVersion { get; } = releaseVersion;

        [JsonIgnore]
        public SemVersion ReleaseSemVersion
        {
            get
            {
                if (_releaseSemVersion != null)
                    return _releaseSemVersion;
                if (!SemVersion.TryParse(ReleaseVersion, SemVersionStyles.Strict, out var semVersion))
                    throw new InvalidOperationException($"Invalid version: {ReleaseVersion}");
                return _releaseSemVersion = semVersion;
            }
        }

        [JsonPropertyName("security")]
        public bool? Security { get; } = security;

        [JsonPropertyName("cve-list")]
        public IReadOnlyList<CveList> CveList { get; } = cveList;

        [JsonPropertyName("release-notes")]
        public string ReleaseNotes { get; } = releaseNotes;

        [JsonPropertyName("runtime")]
        public Runtime Runtime { get; } = runtime;

        [JsonPropertyName("sdk")]
        public Sdk Sdk { get; } = sdk;

        [JsonPropertyName("sdks")]
        public IReadOnlyList<Sdk> Sdks { get; } = sdks;

        [JsonPropertyName("aspnetcore-runtime")]
        public AspnetcoreRuntime AspnetcoreRuntime { get; } = aspnetcoreRuntime;

        [JsonPropertyName("windowsdesktop")]
        public Windowsdesktop Windowsdesktop { get; } = windowsdesktop;
    }

    [method: JsonConstructor]
    public class DotNetReleases(
        string channelVersion,
        string latestRelease,
        string latestReleaseDate,
        string latestRuntime,
        string latestSdk,
        string supportPhase,
        string releaseType,
        string eolDate,
        string lifecyclePolicy,
        IReadOnlyList<Release> releases)
    {
        [JsonPropertyName("channel-version")]
        public string ChannelVersion { get; } = channelVersion;

        [JsonPropertyName("latest-release")]
        public string LatestRelease { get; } = latestRelease;

        [JsonPropertyName("latest-release-date")]
        public string LatestReleaseDate { get; } = latestReleaseDate;

        [JsonPropertyName("latest-runtime")]
        public string LatestRuntime { get; } = latestRuntime;

        [JsonPropertyName("latest-sdk")]
        public string LatestSdk { get; } = latestSdk;

        [JsonPropertyName("support-phase")]
        public string SupportPhase { get; } = supportPhase;

        [JsonPropertyName("release-type")]
        public string ReleaseType { get; } = releaseType;

        [JsonPropertyName("eol-date")]
        public string EolDate { get; } = eolDate;

        [JsonPropertyName("lifecycle-policy")]
        public string LifecyclePolicy { get; } = lifecyclePolicy;

        [JsonPropertyName("releases")]
        public IReadOnlyList<Release> Releases { get; } = releases;
    }

    [method: JsonConstructor]
    public class Runtime(
        string version,
        string versionDisplay,
        string vsVersion,
        string vsMacVersion,
        IReadOnlyList<File> files)
    {
        [JsonPropertyName("version")]
        public string Version { get; } = version;

        [JsonPropertyName("version-display")]
        public string VersionDisplay { get; } = versionDisplay;

        [JsonPropertyName("vs-version")]
        public string VsVersion { get; } = vsVersion;

        [JsonPropertyName("vs-mac-version")]
        public string VsMacVersion { get; } = vsMacVersion;

        [JsonPropertyName("files")]
        public IReadOnlyList<File> Files { get; } = files;
    }

    [method: JsonConstructor]
    public class Sdk(
        string version,
        string versionDisplay,
        string runtimeVersion,
        string vsVersion,
        string vsMacVersion,
        string vsSupport,
        string vsMacSupport,
        string csharpVersion,
        string fsharpVersion,
        string vbVersion,
        IReadOnlyList<File> files)
    {
        [JsonPropertyName("version")]
        public string Version { get; } = version;

        [JsonPropertyName("version-display")]
        public string VersionDisplay { get; } = versionDisplay;

        [JsonPropertyName("runtime-version")]
        public string RuntimeVersion { get; } = runtimeVersion;

        [JsonPropertyName("vs-version")]
        public string VsVersion { get; } = vsVersion;

        [JsonPropertyName("vs-mac-version")]
        public string VsMacVersion { get; } = vsMacVersion;

        [JsonPropertyName("vs-support")]
        public string VsSupport { get; } = vsSupport;

        [JsonPropertyName("vs-mac-support")]
        public string VsMacSupport { get; } = vsMacSupport;

        [JsonPropertyName("csharp-version")]
        public string CsharpVersion { get; } = csharpVersion;

        [JsonPropertyName("fsharp-version")]
        public string FsharpVersion { get; } = fsharpVersion;

        [JsonPropertyName("vb-version")]
        public string VbVersion { get; } = vbVersion;

        [JsonPropertyName("files")]
        public IReadOnlyList<File> Files { get; } = files;
    }

    [method: JsonConstructor]
    public class Sdk2(
        string version,
        string versionDisplay,
        string runtimeVersion,
        string vsVersion,
        string vsMacVersion,
        string vsSupport,
        string vsMacSupport,
        string csharpVersion,
        string fsharpVersion,
        string vbVersion,
        IReadOnlyList<File> files)
    {
        [JsonPropertyName("version")]
        public string Version { get; } = version;

        [JsonPropertyName("version-display")]
        public string VersionDisplay { get; } = versionDisplay;

        [JsonPropertyName("runtime-version")]
        public string RuntimeVersion { get; } = runtimeVersion;

        [JsonPropertyName("vs-version")]
        public string VsVersion { get; } = vsVersion;

        [JsonPropertyName("vs-mac-version")]
        public string VsMacVersion { get; } = vsMacVersion;

        [JsonPropertyName("vs-support")]
        public string VsSupport { get; } = vsSupport;

        [JsonPropertyName("vs-mac-support")]
        public string VsMacSupport { get; } = vsMacSupport;

        [JsonPropertyName("csharp-version")]
        public string CsharpVersion { get; } = csharpVersion;

        [JsonPropertyName("fsharp-version")]
        public string FsharpVersion { get; } = fsharpVersion;

        [JsonPropertyName("vb-version")]
        public string VbVersion { get; } = vbVersion;

        [JsonPropertyName("files")]
        public IReadOnlyList<File> Files { get; } = files;
    }

    [method: JsonConstructor]
    public class Windowsdesktop(string version, string versionDisplay, IReadOnlyList<File> files)
    {
        [JsonPropertyName("version")]
        public string Version { get; } = version;

        [JsonPropertyName("version-display")]
        public string VersionDisplay { get; } = versionDisplay;

        [JsonPropertyName("files")]
        public IReadOnlyList<File> Files { get; } = files;
    }


// Root myDeserializedClass = JsonSerializer.Deserialize<Root>(myJsonResponse);
    [method: JsonConstructor]
    public class ReleasesIndex(
        string channelVersion,
        string latestRelease,
        string latestReleaseDate,
        bool? security,
        string latestRuntime,
        string latestSdk,
        string product,
        string supportPhase,
        string eolDate,
        string releaseType,
        string releasesJson,
        string supportedOsJson)
    {
        [JsonPropertyName("channel-version")]
        public string ChannelVersion { get; } = channelVersion;

        [JsonPropertyName("latest-release")]
        public string LatestRelease { get; } = latestRelease;

        [JsonPropertyName("latest-release-date")]
        public string LatestReleaseDate { get; } = latestReleaseDate;

        [JsonPropertyName("security")]
        public bool? Security { get; } = security;

        [JsonPropertyName("latest-runtime")]
        public string LatestRuntime { get; } = latestRuntime;

        [JsonPropertyName("latest-sdk")]
        public string LatestSdk { get; } = latestSdk;

        [JsonPropertyName("product")]
        public string Product { get; } = product;

        [JsonPropertyName("support-phase")]
        public string SupportPhase { get; } = supportPhase;

        [JsonPropertyName("eol-date")]
        public string EolDate { get; } = eolDate;

        [JsonPropertyName("release-type")]
        public string ReleaseType { get; } = releaseType;

        [JsonPropertyName("releases.json")]
        public string ReleasesJson { get; } = releasesJson;

        [JsonPropertyName("supported-os.json")]
        public string SupportedOsJson { get; } = supportedOsJson;
    }

    [method: JsonConstructor]
    public class DotNetIndex(IReadOnlyList<ReleasesIndex> releasesIndex)
    {
        // string schema,
        // this.Schema = schema;

        // [JsonPropertyName("$schema")]
        // public string Schema { get; }

        [JsonPropertyName("releases-index")]
        public IReadOnlyList<ReleasesIndex> ReleasesIndex { get; } = releasesIndex;
    }
}