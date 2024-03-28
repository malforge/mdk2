using System;
using System.IO;
using System.IO.Hashing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Mdk.CommandLine.CommandLine;
using Mdk.CommandLine.SharedApi;
using Mdk.CommandLine.Utility;

namespace Mdk.CommandLine.IngameScript.Restore;

/// <summary>
/// Runs checks and restores MDK projects if necessary.
/// </summary>
public class ScriptRestorer: ProjectJob
{
    // This job might be somewhat misnamed, as it doesn't actually restore anything, but it does check for updates.
    
    static readonly XNamespace MsbuildNs = "http://schemas.microsoft.com/developer/msbuild/2003";

    /// <summary>
    /// Run the restoration job.
    /// </summary>
    /// <param name="parameters"></param>
    /// <param name="project"></param>
    /// <param name="console"></param>
    /// <param name="httpClient"></param>
    /// <param name="interaction"></param>
    /// <exception cref="InvalidOperationException"></exception>
    public async Task RestoreAsync(Parameters parameters, MdkProject project, IConsole console, IHttpClient httpClient, IInteraction interaction)
    {
        UpdateParametersFromConfig(parameters, project.Project, console);
        var projectFileName = parameters.RestoreVerb.ProjectFile ?? throw new InvalidOperationException("Project file not specified.");
        XDocument document;
        using (var reader = new StreamReader(projectFileName))
            document = await XDocument.LoadAsync(reader, LoadOptions.None, CancellationToken.None);

        await CheckNugetPackageVersionAsync(document, projectFileName, console, httpClient, interaction);
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

    async Task CheckNugetPackageVersionAsync(XDocument document, string projectFileName, IConsole console, IHttpClient httpClient, IInteraction interaction)
    {
        var pbPackagerReference = document.Elements(MsbuildNs, "Project", "ItemGroup", "PackageReference")
            .FirstOrDefault(e => e.Attribute("Include")?.Value == "Mal.Mdk2.PbPackager");

        var versionString = pbPackagerReference?.Attribute("Version")?.Value;
        if (versionString is null)
            return;
        if (!SemanticVersion.TryParse(versionString, out var version))
            return;

        var versions = Nuget.GetPackageVersionsAsync(httpClient, "Mal.Mdk2.PbPackager", projectFileName);
        Nuget.Version lastVersion;
        if (version.IsPrerelease())
            lastVersion = await versions.FirstOrDefaultAsync();
        else
            lastVersion = await versions.Where(v => !v.SemanticVersion.IsPrerelease()).FirstOrDefaultAsync();

        console.Trace($"Latest version: {lastVersion.SemanticVersion} ({lastVersion.Source})");

        string? versionFile;
        try
        {
            versionFile = GetVersionFileName(projectFileName);
            console.Trace($"Version file: {versionFile}");
            if (File.Exists(versionFile))
            {
                var lastDetectedVersion = LoadLastDetectedVersion(versionFile, console);
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
                var directoryInfo = new FileInfo(versionFile).Directory!;
                if (!directoryInfo.Exists)
                    directoryInfo.Create();
                await File.WriteAllTextAsync(versionFile,
                    $"""
                     [mdk]
                     projectpath={projectFileName}
                     
                     [Mal.Mdk2.PbPackager]
                     lastdetectedversion = {lastVersion.SemanticVersion}
                     lastdetectedsource = {lastVersion.Source}
                     lastdetectionutctime = {DateTime.UtcNow:O}
                     """);
            }
            catch (Exception e)
            {
                console.Trace($"Failed to write version file: {e.Message}");
            }
        }

        if (lastVersion.SemanticVersion > version)
            interaction.Nuget("Mal.Mdk2.PbPackager", versionString, lastVersion.SemanticVersion.ToString());
    }

    static SemanticVersion LoadLastDetectedVersion(string versionFile, IConsole console)
    {
        try
        {
            var ini = Ini.FromFile(versionFile);
            var lastDetectedVersion = ini["Mal.Mdk2.PbPackager"]["lastdetectedversion"].ToString();
            if (lastDetectedVersion != null && SemanticVersion.TryParse(lastDetectedVersion, out var version))
                return version;
        }
        catch (Exception e)
        {
            console.Trace($"Failed to read version file: {e.Message}");
        }
        return SemanticVersion.Empty;
    }
}