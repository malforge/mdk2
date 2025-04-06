﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Mdk.CommandLine.Shared.Api;

namespace Mdk.CommandLine;

/// <summary>
///     A helper class for interacting with NuGet.
/// </summary>
public static class Nuget
{
    const string NugetRepoUrl = "https://api.nuget.org/v3-flatcontainer/{0}/index.json";

    /// <summary>
    ///     Get all versions of a package from NuGet.
    /// </summary>
    /// <param name="httpClient"></param>
    /// <param name="packageName"></param>
    /// <param name="projectFileName"></param>
    /// <param name="timeout"></param>
    /// <returns></returns>
    public static async IAsyncEnumerable<Version> GetPackageVersionsAsync(IHttpClient httpClient, string packageName, string projectFileName, TimeSpan timeout)
    {
        var sources = await GetNugetSources(projectFileName).ToListAsync();
        
        var allResults = await Task.WhenAll(
            sources.Select(async source => await LoadVersionsFromSourceAsync(source, httpClient, packageName, timeout).ToListAsync())); 
        
        var compiledAndSortedResults = allResults
            .SelectMany(x => x)
            .Distinct()
            .OrderByDescending(k => k);

        foreach (var result in compiledAndSortedResults)
            yield return result;
    }

    static async IAsyncEnumerable<Version> LoadVersionsFromSourceAsync((string url, string displayName) source, IHttpClient httpClient, string packageName, TimeSpan timeout)
    {
        if (source.url.StartsWith("http:") || source.url.StartsWith("https:"))
        {
            await foreach (var p in LoadVersionsFromWebAsync(source, httpClient, packageName, timeout))
                yield return p;
        }
        else
        {
            await foreach (var p in LoadVersionsFromDiskAsync(source, packageName))
                yield return p;
        }
    }

    static async IAsyncEnumerable<Version> LoadVersionsFromDiskAsync((string url, string displayName) source, string packageName)
    {
        if (!Directory.Exists(source.url))
            yield break;
        await Task.Yield();
        foreach (var file in Directory.EnumerateFiles(source.url, $"{packageName}.*.nupkg"))
        {
            var versionString = Path.GetFileNameWithoutExtension(file)[(packageName.Length + 1)..];
            if (SemanticVersion.TryParse(versionString, out var semVer))
                yield return new Version(packageName, semVer, source.url, source.displayName);
        }
    }

    static async IAsyncEnumerable<Version> LoadVersionsFromWebAsync((string url, string displayName) source, IHttpClient httpClient, string packageName, TimeSpan timeout)
    {
        var requestUrl = string.Format(source.url, packageName.ToLower());
        JsonElement.ArrayEnumerator versions;
        try
        {
            var response = await httpClient.GetAsync(requestUrl, timeout);
            if (!response.IsSuccessStatusCode)
                yield break;

            var jsonResponse = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(jsonResponse);
            versions = doc.RootElement.GetProperty("versions").EnumerateArray();
        }
        catch (Exception)
        {
            yield break;
        }

        foreach (var versionString in versions.Select(version => version.GetString()).OfType<string>())
        {
            if (SemanticVersion.TryParse(versionString, out var semVer))
                yield return new Version(packageName, semVer, source.url, source.displayName);
        }
    }

    public static async IAsyncEnumerable<(string url, string displayName)> GetNugetSources(string projectFileName)
    {
        var projectPath = Path.GetDirectoryName(projectFileName)!;
        var searchPaths = AncestorFolders(projectPath)
            .Append(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Nuget"))
            .Select(path => Path.Combine(path, "NuGet.Config"));

        foreach (var configPath in searchPaths)
        {
            if (!File.Exists(configPath))
                continue;
            XDocument document;
            try
            {
                await using var stream = File.OpenRead(configPath);
                document = await XDocument.LoadAsync(stream, LoadOptions.None, CancellationToken.None);
            }
            catch
            {
                continue;
            }

            var sources = document.Root?.Elements("packageSources").Elements("add")
                .Select(e => (url: e.Attribute("value")?.Value, displayName: e.Attribute("key")?.Value))
                .Where(e => e.url != null)
                .Select(e => (e.url!, e.displayName ?? e.url!));
            if (sources == null)
                continue;
            foreach (var source in sources)
                yield return source;
        }

        yield return (NugetRepoUrl, "NuGet.org");
    }

    static IEnumerable<string> AncestorFolders(string path)
    {
        var directory = new DirectoryInfo(path);
        while (directory != null)
        {
            yield return directory.FullName;
            directory = directory.Parent;
        }
    }

    /// <summary>
    ///     A version for a package, and where it came from.
    /// </summary>
    /// <param name="packageName"></param>
    /// <param name="semanticVersion"></param>
    /// <param name="source"></param>
    /// <param name="displayName"></param>
    public readonly struct Version(string packageName, SemanticVersion semanticVersion, string source, string displayName)
        : IComparable<Version>, IEquatable<Version>
    {
        /// <summary>
        ///     The name of the package.
        /// </summary>
        public readonly string PackageName = packageName;

        /// <summary>
        ///     The version of the package.
        /// </summary>
        public readonly SemanticVersion SemanticVersion = semanticVersion;

        /// <summary>
        ///     Where the package was found.
        /// </summary>
        public readonly string Source = source;

        /// <summary>
        ///     A display name for the source.
        /// </summary>
        public readonly string DisplayName = displayName;

        public int CompareTo(Version other)
        {
            var packageNameComparison = string.Compare(PackageName, other.PackageName, StringComparison.OrdinalIgnoreCase);
            if (packageNameComparison != 0)
                return packageNameComparison;
            var semanticVersionComparison = SemanticVersion.CompareTo(other.SemanticVersion);
            if (semanticVersionComparison != 0)
                return semanticVersionComparison;
            return semanticVersionComparison;
        }

        public bool Equals(Version other) => PackageName == other.PackageName && SemanticVersion.Equals(other.SemanticVersion) && Source == other.Source && DisplayName == other.DisplayName;

        public override bool Equals(object? obj) => obj is Version other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(PackageName, SemanticVersion, Source, DisplayName);

        public static bool operator ==(Version left, Version right) => left.Equals(right);

        public static bool operator !=(Version left, Version right) => !left.Equals(right);
    }
}