﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Mdk.CommandLine.CommandLine;
using Mdk.CommandLine.Shared.Api;
using Mdk.CommandLine.Utility;

namespace Mdk.CommandLine.IngameScript.LegacyConversion;

/// <summary>
/// Converts a legacy MDK project to MDK2.
/// </summary>
public class LegacyConverter
{
    static readonly XNamespace MsbuildNs = "http://schemas.microsoft.com/developer/msbuild/2003";

    /// <summary>
    /// Converts a legacy MDK project to MDK2.
    /// </summary>
    /// <param name="parameters"></param>
    /// <param name="project"></param>
    /// <param name="console"></param>
    /// <param name="httpClient"></param>
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
        var packageVersions = await Nuget.GetPackageVersionsAsync(
                httpClient, 
                "Mal.Mdk2.PbPackager", 
                project.Project.FilePath ?? throw new InvalidOperationException("The project file path is invalid."),
                TimeSpan.FromSeconds(30))
            .ToListAsync();
        if (packageVersions.Count == 0)
            throw new CommandLineException(-1, "The Mal.Mdk2.PbPackager nuget package could not be found on the nuget web site.");
        
        // First see if we can find a non-preview version
        var packageVersion = packageVersions.FirstOrDefault(v => !v.SemanticVersion.IsPrerelease());
        if (packageVersion.SemanticVersion.IsEmpty())
        {
            console.Trace("No non-preview version of Mal.Mdk2.PbPackager found. Finding the latest preview version.");
            packageVersion = packageVersions.First();
        }
        else
            console.Trace("Finding the latest non-preview version of Mal.Mdk2.PbPackager.");
        
        var distinctSources = packageVersions.Select(v => v.Source).Distinct().ToList();
        if (distinctSources.Count == 1)
            console.Trace($"Found version {packageVersion}.");
        else
            console.Trace($"Found version {packageVersion} from {packageVersion.Source}.");

        var projectFileContent = await File.ReadAllTextAsync(project.Project.FilePath!);
        var document = XDocument.Parse(projectFileContent);
        
        // If the project doesn't reference the Mal.Mdk2.PbPackager package, we will add it
        if (!document.Elements(MsbuildNs, "Project", "ItemGroup", "PackageReference")
                .Any(e => string.Equals((string?)e.Attribute("Include"), "Mal.Mdk2.PbPackager", StringComparison.OrdinalIgnoreCase)
                          || string.Equals((string?)e.Element("Include"), "Mal.Mdk2.PbPackager", StringComparison.OrdinalIgnoreCase)))
        {
            var packageReference = new XElement(XName.Get("PackageReference", MsbuildNs.NamespaceName))
                .AddAttribute("Include", "Mal.Mdk2.PbPackager")
                .AddAttribute("Version", packageVersion.SemanticVersion.ToString());
            
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
        var iniContent = 
            $"""
            [mdk]
            ; This is a programmable block script project.
            ; You should not change this.
            type=programmableblock
            
            ; Toggle trace (on|off) (verbose output)
            trace=off
            
            ; What type of minification to use (none|trim|stripcomments|lite|full)
            minify={minify.ToString().ToLowerInvariant()}
            
            ; A list of files and folder to ignore when creating the script.
            ; This is a comma separated list of glob patterns. 
            ; See https://code.visualstudio.com/docs/editor/glob-patterns
            ignores={string.Join(',', ignores)}
            """;
        
        await File.WriteAllTextAsync(basicIniFileName, iniContent);
        
        if (dryRun)
        {
            console.Print($"Dry run: The following {Path.GetFileName(basicIniFileName)} file would be written:");
            console.Print(iniContent);
            return;
        }
        
        await File.WriteAllTextAsync(basicIniFileName, iniContent);
    }

    static async Task WriteLocalIni(IConsole console, string outputPath, string localIniFileName, bool dryRun /*string gameBinPath, */)
    {
        var iniContent = 
            $"""
            [mdk]
            ; Where to output the script to (auto|specific path)
            output={outputPath}
            ; Override the game bin path (auto|specific path)
            binarypath=auto
            """;
        
        if (dryRun)
        {
            console.Print($"Dry run: The following {Path.GetFileName(localIniFileName)} file would be written:");
            console.Print(iniContent);
            return;
        }
        
        await File.WriteAllTextAsync(localIniFileName, iniContent);
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