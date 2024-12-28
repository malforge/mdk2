using System.IO;
using System.Threading.Tasks;
using Mdk.CommandLine.Utility;
using Microsoft.CodeAnalysis.CSharp;

namespace Mdk.CommandLine.Mod.Pack.Jobs;

/// <summary>
/// This job processes the scripts in the project using the processors provided, and writes the results to the output directory.
/// </summary>
internal class ProcessScriptsJob : ModJob
{
    /// <inheritdoc />
    public override async Task ExecuteAsync(IModPackContext context)
    {
        context.Console.Trace("Processing scripts");
        var projectDirectory = new DirectoryInfo(context.FileSystem.ProjectPath);
        var outputDirectory = new DirectoryInfo(context.FileSystem.OutputDirectory);
        outputDirectory = new DirectoryInfo(Path.Combine(outputDirectory.FullName, "Data", "Scripts", outputDirectory.Name));
        foreach (var doc in context.ScriptDocuments)
        {
            var document = doc;
            var relativePath = new FileInfo(document.FilePath!).GetPathRelativeTo(projectDirectory);
            var syntaxTree = (CSharpSyntaxTree?)await document.GetSyntaxTreeAsync();
            if (syntaxTree == null)
                continue;
            var root = await syntaxTree.GetRootAsync();
            document = document.WithSyntaxRoot(root);
            foreach (var processor in context.Processors)
            {
                context.Console.Trace($"Running {nameof(processor)} {processor.GetType().Name} on {document.Name}");
                document = await processor.ProcessAsync(document, context);
            }

            var outputPath = Path.Combine(outputDirectory.FullName, relativePath);
            context.Console.Trace($"Writing {relativePath} to {outputDirectory.FullName}");
            await context.FileSystem.WriteAsync(outputPath, (await document.GetTextAsync()).ToString());
        }
    }
}