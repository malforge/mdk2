using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Mdk.CommandLine.CommandLine;
using Mdk.CommandLine.SharedApi;
using Mdk.CommandLine.Utility;

namespace Mdk.CommandLine.IngameScript.LegacyConversion;

public class LegacyConverter
{
    static readonly XNamespace MsbuildNs = "http://schemas.microsoft.com/developer/msbuild/2003";
    // const string MsbuildNs = "http://schemas.microsoft.com/developer/msbuild/2003";

    public async Task ConvertAsync(Parameters parameters, MdkProject project, IConsole console, IHttpClient httpClient)
    {
        await ConvertConfigAsync(project, console, parameters.RestoreVerb.DryRun);
        await AddNugetReferencesAsync(project, console, httpClient, parameters.RestoreVerb.DryRun);
    }
    
    static async Task ConvertConfigAsync(MdkProject project, IConsole console, bool dryRun)
    {
        var projectDirectory = Path.GetDirectoryName(project.Project.FilePath) ?? throw new InvalidOperationException("The project file path is invalid.");
        var optionsPropsFileName = Path.Combine(projectDirectory, "mdk", "mdk.options.props");
        var pathsPropsFileName = Path.Combine(projectDirectory, "mdk", "mdk.paths.props");

        // var gameBinPath = "auto";
        var outputPath = "auto";
        bool trimTypes;
        List<string> ignores = ["obj/**/*", "MDK/**/*", "**/*.debug.cs"];
        var minify = LegacyMinifierLevel.None;

        console.Trace("Loading legacy property file...");
        using (var reader = new StreamReader(optionsPropsFileName))
        {
            var options = await XDocument.LoadAsync(reader, LoadOptions.None, default);
            var trimTypesString = (string?)options.Element(MsbuildNs, "Project", "PropertyGroup", "MDKTrimTypes", "Enabled");
            trimTypes = string.Equals(trimTypesString ?? "", "yes", StringComparison.OrdinalIgnoreCase);
            var minifyString = (string?)options.Element(MsbuildNs, "Project", "PropertyGroup", "MDKMinify", "Level");
            if (minifyString != null && Enum.TryParse<LegacyMinifierLevel>(minifyString, true, out var minifyLevel))
                minify = minifyLevel;
            var ignoredFolders = options.Elements(MsbuildNs, "Project", "PropertyGroup", "MDKIgnoredFolders", "Folder").ToList();
            var ignoredFiles = options.Elements(MsbuildNs, "Project", "PropertyGroup", "MDKIgnoredFiles", "File").ToList();

            ignores.AddRange(ignoredFolders.Select(folder => folder.Value + "/**/*"));
            ignores.AddRange(ignoredFiles.Select(file => file.Value));
        }
        console.Trace("Success.");

        console.Trace("Loading legacy paths file...");
        if (File.Exists(pathsPropsFileName))
        {
            using var reader = new StreamReader(pathsPropsFileName);
            var paths = await XDocument.LoadAsync(reader, LoadOptions.None, default);
            // var useGameBinPathString = (string?)paths.Element(MsbuildNs, "Project", "PropertyGroup", "MDKUseGameBinPath");
            // var gameBinPathString = (string?)paths.Element(MsbuildNs, "Project", "PropertyGroup", "MDKGameBinPath");
            var outputPathString = (string?)paths.Element(MsbuildNs, "Project", "PropertyGroup", "MDKOutputPath");

            // if (gameBinPathString != null && string.Equals(useGameBinPathString ?? "", "yes", StringComparison.OrdinalIgnoreCase))
            //     gameBinPath = gameBinPathString;
            if (outputPathString != null)
                outputPath = outputPathString;
        }
        console.Trace("Success.");

        var projectFileName = Path.GetFileName(project.Project.FilePath) ?? throw new InvalidOperationException("The project file name is invalid.");
        var mainIniFileName = Path.Combine(projectDirectory, Path.ChangeExtension(projectFileName, "mdk.ini"));
        var localIniFileName = Path.Combine(projectDirectory,Path.ChangeExtension(projectFileName, ".mdk.local.ini"));

        console.Trace("Writing MDK ini files...");
        await WriteMainIni(console, minify, trimTypes, ignores, mainIniFileName, dryRun);
        await WriteLocalIni(console, outputPath, /*gameBinPath, */localIniFileName, dryRun);

        console.Trace("Adding or updating .gitignore file...");
        await AddOrUpdateGitIgnore(console, projectDirectory, projectFileName, dryRun);
        
        console.Print("The project has been successfully converted to MDK2.");
    }

    async Task AddNugetReferencesAsync(MdkProject project, IConsole console, IHttpClient httpClient, bool dryRun)
    {
        var packageVersions = await Nuget.GetPackageVersionsAsync(httpClient, "Mal.Mdk2.PbPackager").OrderByDescending(v => v).ToListAsync();
        if (packageVersions.Count == 0)
            throw new CommandLineException(-1, "The Mal.Mdk2.PbPackager nuget package could not be found on the nuget web site.");
        
        // First see if we can find a non-preview version
        var packageVersion = packageVersions.FirstOrDefault(v => !v.IsPrerelease());
        if (packageVersion.IsEmpty())
        {
            console.Trace("No non-preview version of Mal.Mdk2.PbPackager found. Using the latest preview version.");
            packageVersion = packageVersions.First();
        }
        else
            console.Trace("Using the latest non-preview version of Mal.Mdk2.PbPackager.");
        console.Trace($"Using version {packageVersion}.");

        var projectFileContent = await File.ReadAllTextAsync(project.Project.FilePath!);
        var document = XDocument.Parse(projectFileContent);
        
        // If the project doesn't reference the Mal.Mdk2.PbPackager package, we will add it
        if (!document.Elements(MsbuildNs, "Project", "ItemGroup", "PackageReference")
                .Any(e => string.Equals((string?)e.Attribute("Include"), "Mal.Mdk2.PbPackager", StringComparison.OrdinalIgnoreCase)
                          || string.Equals((string?)e.Element("Include"), "Mal.Mdk2.PbPackager", StringComparison.OrdinalIgnoreCase)))
        {
            var packageReference = new XElement(XName.Get("PackageReference", MsbuildNs.NamespaceName))
                .AddAttribute("Include", "Mal.Mdk2.PbPackager")
                .AddAttribute("Version", packageVersion.ToString());
            
            // Find the first item group which has package references and add the new package reference to it - or create a new item group
            var itemGroup = document.Elements(MsbuildNs, "Project", "ItemGroup", "PackageReference").FirstOrDefault()?.Parent;
            if (itemGroup == null)
            {
                itemGroup = new XElement(XName.Get("ItemGroup", MsbuildNs.NamespaceName));
                document.Element(MsbuildNs + "Project")?.Add(itemGroup);
            }
            itemGroup.Add(packageReference);
            
            if (dryRun)
            {
                console.Print($"Dry run: The following package reference would be added to {Path.GetFileName(project.Project.FilePath!)}:");
                console.Print(packageReference.ToString());
                return;
            }

            await File.WriteAllTextAsync(project.Project.FilePath!, document.ToString());
        }
    }
    
    static async Task WriteMainIni(IConsole console, LegacyMinifierLevel minify, bool trimTypes, List<string> ignores, string basicIniFileName, bool dryRun)
    {
        var ini = new Ini()
            .WithSection("mdk",
                section => section
                    .WithKey("type", "programmableblock")
                    .WithKey("minify", minify.ToString().ToLowerInvariant())
                    .WithKey("trim", trimTypes ? "yes" : "no")
                    .WithKey("ignores", string.Join(';', ignores))
            );

        if (dryRun)
        {
            console.Print($"Dry run: The following {Path.GetFileName(basicIniFileName)} file would be written:");
            console.Print(ini.ToString());
            return;
        }
        
        await File.WriteAllTextAsync(basicIniFileName, ini.ToString());
    }

    static async Task WriteLocalIni(IConsole console, string outputPath, string localIniFileName, bool dryRun /*string gameBinPath, */)
    {
        var ini = new Ini()
            .WithSection("mdk",
                section => section
                    .WithKey("output", outputPath)
                    // .WithKey("gamebin", gameBinPath)
            );
        
        if (dryRun)
        {
            console.Print($"Dry run: The following {Path.GetFileName(localIniFileName)} file would be written:");
            console.Print(ini.ToString());
            return;
        }
        
        await File.WriteAllTextAsync(localIniFileName, ini.ToString());
    }

    static async Task AddOrUpdateGitIgnore(IConsole console, string projectDirectory, string projectFileName, bool dryRun)
    {
        var gitIgnoreFileName = Path.Combine(projectDirectory, ".gitignore");
        var ignoreContent = $"# MDK{Environment.NewLine}{projectFileName}.mdk.local.ini";
        if (File.Exists(gitIgnoreFileName))
        {
            if (dryRun)
            {
                console.Print($"Dry run: The following line would be added to {Path.GetFileName(gitIgnoreFileName)}:");
                console.Print(ignoreContent);
                return;
            }
            
            var gitIgnore = await File.ReadAllTextAsync(gitIgnoreFileName);
            if (!gitIgnore.Contains(projectFileName + ".mdk.local.ini"))
                await File.AppendAllTextAsync(gitIgnoreFileName, Environment.NewLine + ignoreContent);
        }
        else
        {
            if (dryRun)
            {
                console.Print($"Dry run: The following {Path.GetFileName(gitIgnoreFileName)} file would be written:");
                console.Print(ignoreContent);
                return;
            }
            
            await File.WriteAllTextAsync(gitIgnoreFileName, ignoreContent);
        }
    }
}