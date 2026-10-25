using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Mdk.CommandLine.CommandLine;
using Mdk.CommandLine.Shared.AttributeTrimming;
using Mdk.CommandLine.Utility;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Mdk.CommandLine.Mod.Pack.Jobs;

/// <summary>
///     This job processes the scripts in the project using the processors provided, and writes the results to the output
///     directory.
/// </summary>
internal class ProcessScriptsJob : ModJob
{
    /// <inheritdoc />
    public override async Task<ModPackContext> ExecuteAsync(ModPackContext context)
    {
        context.Console.Trace("Processing scripts");
        var projectDirectory = new DirectoryInfo(context.FileSystem.ProjectPath);
        var outputDirectory = new DirectoryInfo(context.FileSystem.OutputDirectory);
        outputDirectory = new DirectoryInfo(Path.Combine(outputDirectory.FullName, "Data", "Scripts", outputDirectory.Name));

        var documents = context.ScriptDocuments;

        var log = new OrderedLog(context.Console, documents.Length);

        try
        {
            await Parallel.ForEachAsync(Enumerable.Range(0, documents.Length),
                async (index, _) =>
                {
                    try
                    {
                        await ProcessDocumentAsync(documents[index], context.WithConsole(log[index]), projectDirectory, outputDirectory);
                    }
                    finally
                    {
                        log.Push(index);
                    }
                });
        }
        finally
        {
            await log.CompleteAsync();
        }

        return context;
    }

    static async Task ProcessDocumentAsync(Document document, ModPackContext context, DirectoryInfo projectDirectory, DirectoryInfo outputDirectory)
    {
        var relativePath = ResolveScriptRelativePath(document.FilePath!, projectDirectory);

        var syntaxTree = (CSharpSyntaxTree?)await document.GetSyntaxTreeAsync();
        if (syntaxTree == null)
            return;
        var root = await syntaxTree.GetRootAsync();
        var wasEmptiedByAttributeTrimming = root.HasAnnotations(AttributeTrimmingProcessor.EmptiedDocumentAnnotationKind);
        document = document.WithSyntaxRoot(root);
        foreach (var processor in context.Processors)
        {
            context.Console.Trace($"Running {nameof(processor)} {processor.GetType().Name} on {document.Name}");
            document = await processor.ProcessAsync(document, context);
        }

        var processedRoot = await document.GetSyntaxRootAsync();
        if (wasEmptiedByAttributeTrimming
            && processedRoot is CompilationUnitSyntax compilationUnit
            && IsStructurallyEmpty(compilationUnit))
        {
            context.Console.Trace($"Skipping empty script document {relativePath}");
            return;
        }

        var outputPath = PathEx.CombineInside(outputDirectory.FullName, relativePath);
        context.Console.Trace($"Writing {relativePath} to {outputDirectory.FullName}");
        await context.FileSystem.WriteAsync(outputPath, (await document.GetTextAsync()).ToString());
    }

    /// <summary>
    ///     Resolves where a script document is written, relative to the mod's script folder.
    /// </summary>
    /// <param name="filePath">The full path of the script document.</param>
    /// <param name="projectDirectory">The directory of the project being packed.</param>
    /// <remarks>
    ///     A script from outside the project is placed under a folder named for the shared project (<c>.projitems</c>)
    ///     that holds it. Without one, a script on the same drive keeps its relative path and a script on another
    ///     drive goes under its own folder's name.
    /// </remarks>
    internal static string ResolveScriptRelativePath(string filePath, DirectoryInfo projectDirectory)
    {
        var file = new FileInfo(filePath);
        var relativePath = file.GetPathRelativeTo(projectDirectory);
        if (!PathEx.LeavesBase(relativePath))
            return relativePath;

        var sharedProject = SharedProjectFile.FindFor(file, projectDirectory);
        if (sharedProject != null)
            return Path.Combine(".", Path.GetFileNameWithoutExtension(sharedProject.Name), Path.GetRelativePath(sharedProject.DirectoryName!, file.FullName));

        if (!Path.IsPathRooted(relativePath))
            return relativePath;

        return file.Directory?.Parent == null
            ? Path.Combine(".", file.Name)
            : Path.Combine(".", file.Directory.Name, file.Name);
    }

    static bool IsStructurallyEmpty(CompilationUnitSyntax root)
    {
        return root.Members.Count == 0
               && root.AttributeLists.Count == 0
               && root.Externs.Count == 0
               && !root.DescendantTrivia(descendIntoTrivia: true).Any(trivia => trivia.IsDirective);
    }
}