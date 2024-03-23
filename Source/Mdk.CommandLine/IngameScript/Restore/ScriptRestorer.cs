﻿using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Mdk.CommandLine.Commands.Restore;
using Mdk.CommandLine.SharedApi;
using Mdk.CommandLine.Utility;

namespace Mdk.CommandLine.IngameScript.Restore;

public class ScriptRestorer
{
    static readonly XNamespace MsbuildNs = "http://schemas.microsoft.com/developer/msbuild/2003";

    public async Task RestoreAsync(RestoreParameters restoreParameters, MdkProject project, IConsole console, IHttpClient httpClient, IInteraction interaction)
    {
        var projectFileName = restoreParameters.ProjectFile ?? throw new InvalidOperationException("Project file not specified.");
        XDocument document;
        using (var reader = new StreamReader(projectFileName))
            document = await XDocument.LoadAsync(reader, LoadOptions.None, CancellationToken.None);

        await CheckNugetPackageVersionAsync(document, httpClient, interaction);
    }

    async Task CheckNugetPackageVersionAsync(XDocument document, IHttpClient httpClient, IInteraction interaction)
    {
        // Find the Mal.Mdk2.PbPackager nuget package reference in the project
        var pbPackagerReference = document.Elements(MsbuildNs, "Project", "ItemGroup", "PackageReference")
            .FirstOrDefault(e => e.Attribute("Include")?.Value == "Mal.Mdk2.PbPackager");

        // Get the version of the Mal.Mdk2.PbPackager nuget package
        var versionString = pbPackagerReference?.Attribute("Version")?.Value;
        if (versionString is null)
            return;
        if (!SemanticVersion.TryParse(versionString, out var version))
            return;

        // Detect version from Nuget
        var versions = Nuget.GetPackageVersionsAsync(httpClient, "Mal.Mdk2.PbPackager");
        SemanticVersion lastVersion;
        if (version.IsPrerelease())
            lastVersion = await versions.OrderByDescending(v => v).FirstOrDefaultAsync();
        else
            lastVersion = await versions.Where(v => !v.IsPrerelease()).OrderByDescending(v => v).FirstOrDefaultAsync();

        if (lastVersion > version)
            interaction.Nuget("Mal.Mdk2.PbPackager", versionString, lastVersion.ToString());
    }
}