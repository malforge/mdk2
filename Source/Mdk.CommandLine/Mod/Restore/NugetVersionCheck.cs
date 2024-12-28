using System;
using System.IO;
using System.IO.Hashing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Mdk.CommandLine.Shared.Api;
using Mdk.CommandLine.Utility;

namespace Mdk.CommandLine.Mod.Restore;

/// <summary>
///     A job that checks the nuget package version and notifies the user if a newer version is available.
/// </summary>
static class NugetVersionCheck
{
    // static readonly XNamespace MsbuildNs = "http://schemas.microsoft.com/developer/msbuild/2003";

    /// <summary>
    ///     Checks the nuget package version and notifies the user if a newer version is available.
    /// </summary>
    /// <param name="document"></param>
    /// <param name="projectFileName"></param>
    /// <param name="packageName"></param>
    /// <param name="console"></param>
    /// <param name="httpClient"></param>
    /// <param name="interaction"></param>
    public static async Task CheckAsync(XDocument document, string projectFileName, string packageName, IConsole console, IHttpClient httpClient, IInteraction interaction)
    {
        if (!TryFindCurrentVersion(document, packageName, console, out var version))
            return;

        Nuget.Version lastVersion;
        try
        {
            lastVersion = await FindLatestVersionAsync(projectFileName, packageName, console, httpClient, version);
        }
        catch (Exception e)
        {
            console.Print($"Failed the check for the latest version of {packageName}: {e.Message}");
            return;
        }

        string? versionFile;
        try
        {
            versionFile = GetVersionFileName(projectFileName);
            console.Trace($"Version file: {versionFile}");
            if (File.Exists(versionFile))
            {
                var lastDetectedVersion = LoadLastDetectedVersion(versionFile, packageName, console);
                var fileChanged = File.GetLastWriteTimeUtc(versionFile);
                var timeSince = DateTime.UtcNow - fileChanged;
                if (!lastDetectedVersion.IsEmpty()
                    && lastDetectedVersion > lastVersion.SemanticVersion)
                {
                    console.Trace("Last detected version is newer than the latest version, will not notify.");
                    return;
                }

                if (!lastDetectedVersion.IsEmpty()
                    && lastDetectedVersion == lastVersion.SemanticVersion
                    && timeSince < TimeSpan.FromDays(1))
                {
                    console.Trace("Last detected version is the same as the latest version and the file was changed less than a day ago, will not notify.");
                    return;
                }
            }
        }
        catch (Exception e)
        {
            console.Trace($"Failed to read version file: {e.Message}");
            versionFile = null;
        }

        if (versionFile != null && lastVersion.SemanticVersion > version)
        {
            try
            {
                await SaveLastDetectedVersionAsync(versionFile, projectFileName, packageName, lastVersion.SemanticVersion);
            }
            catch (Exception e)
            {
                console.Trace($"Failed to write version file: {e.Message}");
            }
        }

        if (lastVersion.SemanticVersion > version)
            interaction.Nuget(packageName, version.ToString(), lastVersion.SemanticVersion.ToString());
    }

    static bool TryFindCurrentVersion(XDocument document, string packageName, IConsole console, out SemanticVersion version)
    {
        console.Trace($"Looking for package {packageName} in the project...");
        var pbPackagerReference = document.ElementsByLocalName("Project", "ItemGroup", "PackageReference")
            .FirstOrDefault(e => e.Attribute("Include")?.Value == packageName);

        var versionString = pbPackagerReference?.Attribute("Version")?.Value;
        if (versionString is null)
        {
            console.Trace("Package not found.");
            version = SemanticVersion.Empty;
            return false;
        }
        if (!SemanticVersion.TryParse(versionString, out version))
        {
            console.Trace("Found the package, but failed to parse the version.");
            return false;
        }
        console.Trace($"Found the package, currently at version {version}.");
        return true;
    }

    static async Task<Nuget.Version> FindLatestVersionAsync(string projectFileName, string packageName, IConsole console, IHttpClient httpClient, SemanticVersion version)
    {
        console.Trace($"Looking for the latest version of {packageName}...");
        var versions = Nuget.GetPackageVersionsAsync(httpClient, packageName, projectFileName, TimeSpan.FromSeconds(10));
        if (console.TraceEnabled)
        {
            versions = versions.Select(v =>
            {
                console.Trace($"Found version {v.SemanticVersion} ({v.Source})");
                return v;
            });
        }

        Nuget.Version lastVersion;
        if (version.IsPrerelease())
            lastVersion = await versions.FirstOrDefaultAsync();
        else
            lastVersion = await versions.Where(v => !v.SemanticVersion.IsPrerelease()).FirstOrDefaultAsync();

        console.Trace($"Latest version of {packageName}: {lastVersion.SemanticVersion} ({lastVersion.Source})");
        return lastVersion;
    }

    /// <summary>
    ///     Generate a reasonably enough unique file name for the version file.
    /// </summary>
    /// <returns></returns>
    static string GetVersionFileName(string projectFileName)
    {
        projectFileName = Path.GetFullPath(projectFileName).ToUpperInvariant();
        var hash = new XxHash128();
        hash.Append(Encoding.Unicode.GetBytes(projectFileName));
        var bytes = hash.GetHashAndReset();
        var appDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MDK2");
        return Path.Combine(appDataFolder, $"{new Guid(bytes):N}.version");
    }

    static SemanticVersion LoadLastDetectedVersion(string versionFile, string packageName, IConsole console)
    {
        try
        {
            var ini = Ini.FromFile(versionFile);
            var lastDetectedVersion = ini[packageName]["lastdetectedversion"].ToString();
            if (lastDetectedVersion != null && SemanticVersion.TryParse(lastDetectedVersion, out var version))
                return version;
        }
        catch (Exception e)
        {
            console.Trace($"Failed to read version file: {e.Message}");
        }
        return SemanticVersion.Empty;
    }

    static async Task SaveLastDetectedVersionAsync(string versionFile, string projectFileName, string packageName, SemanticVersion version)
    {
        var directoryInfo = new FileInfo(versionFile).Directory!;
        if (!directoryInfo.Exists)
            directoryInfo.Create();

        var maxRetries = 5;
        var delay = 1000;

        for (var attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                var exists = File.Exists(versionFile);
                await using var stream = File.Open(versionFile, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
                var content = exists? await new StreamReader(stream).ReadToEndAsync() : string.Empty;
                Ini ini;
                ini = Ini.TryParse(content, out ini) ? ini : new Ini();
                content = ini.WithKey("mdk", "projectpath", projectFileName)
                    .WithKey(packageName, "lastdetectedversion", version.ToString())
                    .WithKey(packageName, "lastdetectionutctime", DateTime.UtcNow.ToString("O"))
                    .ToString();

                stream.Position = 0;
                stream.SetLength(0);
                await stream.WriteAsync(Encoding.UTF8.GetBytes(content));
                break;
            }
            catch (IOException)
            {
                if (attempt == maxRetries - 1)
                    throw;
                await Task.Delay(delay);
            }
        }
    }
}