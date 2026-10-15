using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Mdk.CommandLine.Utility;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Mdk.CommandLine.Mod.Pack.Jobs;

internal class FindAndFilterDocumentsJob : ModJob
{
    static readonly StringComparer PathComparer = OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

    public override Task<ModPackContext> ExecuteAsync(ModPackContext context)
    {
        context.Console.Trace("Finding and filtering documents");
        var project = context.Project;

        bool documentIsNotIgnored(TextDocument doc)
        {
            return context.FileFilter.IsMatch(doc.FilePath!);
        }

        // Remove ignored documents from the project
        project = project.RemoveDocuments([..project.Documents.Where(doc => !documentIsNotIgnored(doc)).Select(doc => doc.Id)]);

        // Make sure the project is class library (convert it if necessary)
        if (project.CompilationOptions?.OutputKind != OutputKind.DynamicallyLinkedLibrary)
        {
            context.Console.Trace("Converting the project to a class library (in memory only)");
            if (project.CompilationOptions == null)
                project = project.WithCompilationOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            else
                project = project.WithCompilationOptions(project.CompilationOptions.WithOutputKind(OutputKind.DynamicallyLinkedLibrary));
        }

        var codeDocuments = project.Documents.Where(documentIsNotIgnored).ToImmutableArray();
        var configurationPaths = MdkConfigurationPaths(project.FilePath);
        var additionalDocuments = project.AdditionalDocuments.Where(documentIsNotIgnored).ToArray();
        var contentDocuments = additionalDocuments.Where(doc => !IsMdkConfiguration(doc.FilePath, configurationPaths)).ToImmutableArray();
        if (contentDocuments.Length < additionalDocuments.Length)
            context.Console.Trace($"Excluding {additionalDocuments.Length - contentDocuments.Length} MDK configuration file(s) from the mod content");

        return Task.FromResult(context
            .WithProject(project)
            .WithScriptDocuments(codeDocuments)
            .WithContentDocuments(contentDocuments)
        );
    }

    /// <summary>
    ///     The paths of MDK's own ini files for a project, normalised for comparison.
    /// </summary>
    /// <remarks>
    ///     The packager props declare <c>&lt;AdditionalFiles Include="**\*.ini"/&gt;</c> so the analyzers can read the
    ///     configuration, which also hands those files to the packer as content documents to deploy. Only the four
    ///     paths <see cref="IniFileFinder" /> resolves are listed, so an ini a mod genuinely ships under
    ///     <c>Content\</c> still reaches the output. A project path with no directory to resolve them against
    ///     yields an empty set, which keeps every document as content rather than guessing.
    /// </remarks>
    /// <param name="projectFilePath"></param>
    /// <returns></returns>
    internal static ImmutableHashSet<string> MdkConfigurationPaths(string? projectFilePath)
    {
        if (string.IsNullOrEmpty(projectFilePath) || string.IsNullOrEmpty(Path.GetDirectoryName(projectFilePath)))
            return ImmutableHashSet.Create<string>(PathComparer);

        var (mainIni, localIni) = IniFileFinder.GetNewIniPaths(projectFilePath);
        var (legacyMainIni, legacyLocalIni) = IniFileFinder.GetLegacyIniPaths(projectFilePath);
        return ImmutableHashSet.Create(PathComparer, [
            Path.GetFullPath(mainIni),
            Path.GetFullPath(localIni),
            Path.GetFullPath(legacyMainIni),
            Path.GetFullPath(legacyLocalIni)
        ]);
    }

    /// <summary>
    ///     Determines whether an additional document is one of MDK's own ini files rather than mod content.
    /// </summary>
    /// <param name="documentPath"></param>
    /// <param name="configurationPaths">The paths from <see cref="MdkConfigurationPaths" />.</param>
    /// <returns></returns>
    internal static bool IsMdkConfiguration(string? documentPath, ImmutableHashSet<string> configurationPaths) =>
        !string.IsNullOrEmpty(documentPath) && configurationPaths.Contains(Path.GetFullPath(documentPath));
}