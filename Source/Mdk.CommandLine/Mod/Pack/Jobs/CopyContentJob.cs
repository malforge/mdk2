using System;
using System.IO;
using System.Threading.Tasks;
using Mdk.CommandLine.Utility;

namespace Mdk.CommandLine.Mod.Pack.Jobs;

/// <summary>
/// This job copies content files (non-code files) to the output directory.
/// </summary>
internal class CopyContentJob : ModJob
{
    /// <inheritdoc />
    public override async Task<ModPackContext> ExecuteAsync(ModPackContext context)
    {
        context.Console.Trace("Copying content");
        var projectDirectory = new DirectoryInfo(context.FileSystem.ProjectPath);
        var outputDirectory = new DirectoryInfo(context.FileSystem.OutputDirectory);
        var contentDocuments = context.ContentDocuments;
        if (contentDocuments.Length == 0)
            return context;

        foreach (var document in contentDocuments)
        {
            var relativePath = new FileInfo(document.FilePath!).GetPathRelativeTo(projectDirectory);
            context.Console.Trace($"Copying {relativePath} to {outputDirectory.FullName}");
            var outputPath = Path.Combine(outputDirectory.FullName, relativePath);
            var outputDirectoryPath = Path.GetDirectoryName(outputPath) ?? throw new InvalidOperationException("Output directory path not set.");
            await context.FileSystem.CreateFolderAsync(outputDirectoryPath);
            await context.FileSystem.CopyAsync(document.FilePath!, outputPath, true);
        }
        
        return context;
    }
}