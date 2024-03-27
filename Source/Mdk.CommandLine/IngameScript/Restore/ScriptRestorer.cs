using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Mdk.CommandLine.CommandLine;
using Mdk.CommandLine.SharedApi;
using Mdk.CommandLine.Utility;

namespace Mdk.CommandLine.IngameScript.Restore;

public class ScriptRestorer
{
    static readonly XNamespace MsbuildNs = "http://schemas.microsoft.com/developer/msbuild/2003";

    public async Task RestoreAsync(Parameters parameters, MdkProject project, IConsole console, IHttpClient httpClient, IInteraction interaction)
    {
        var projectFileName = parameters.RestoreVerb.ProjectFile ?? throw new InvalidOperationException("Project file not specified.");
        XDocument document;
        using (var reader = new StreamReader(projectFileName))
            document = await XDocument.LoadAsync(reader, LoadOptions.None, CancellationToken.None);

        await CheckNugetPackageVersionAsync(document, projectFileName, console, httpClient, interaction);
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

        string? versionFile;
        try
        {
            var appDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MDK2");
            versionFile = Path.Combine(appDataFolder, Path.GetFileNameWithoutExtension(projectFileName) + ".version");
            if (File.Exists(versionFile))
            {
                var fileChanged = File.GetLastWriteTimeUtc(versionFile);
                var timeSince = DateTime.UtcNow - fileChanged;
                if (timeSince < TimeSpan.FromDays(1))
                {
                    console.Trace("Version file is recent (less than a day old), skipping check.");
                    return;
                }
            }
        }
        catch (Exception e)
        {
            console.Trace("Failed to read version file: " + e.Message);
            versionFile = null;
        }

        var versions = Nuget.GetPackageVersionsAsync(httpClient, "Mal.Mdk2.PbPackager", projectFileName);
        Nuget.Version lastVersion;
        if (version.IsPrerelease())
            lastVersion = await versions.FirstOrDefaultAsync();
        else
            lastVersion = await versions.Where(v => !v.SemanticVersion.IsPrerelease()).FirstOrDefaultAsync();

        if (versionFile != null && lastVersion.SemanticVersion > version)
        {
            try
            {
                var directoryInfo = new FileInfo(versionFile).Directory!;
                if (!directoryInfo.Exists)
                    directoryInfo.Create();
                await File.WriteAllTextAsync(versionFile, lastVersion.SemanticVersion.ToString());
            }
            catch (Exception e)
            {
                console.Trace("Failed to write version file: " + e.Message);
            }
        }

        if (lastVersion.SemanticVersion > version)
            interaction.Nuget("Mal.Mdk2.PbPackager", versionString, lastVersion.SemanticVersion.ToString());
    }
}