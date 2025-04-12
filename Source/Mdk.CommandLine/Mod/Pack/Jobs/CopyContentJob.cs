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

            // Remove the Content folder from the path: An early version of the packer copied all
            // the files from the root of the project as long as it wasn't code, which confused
            // people _and_ caused issues of certain temporary files also being copied.
            if (relativePath.StartsWith(@".\Content\", StringComparison.OrdinalIgnoreCase))
                relativePath = relativePath[10..];
            else if (relativePath.StartsWith(@"Content\", StringComparison.OrdinalIgnoreCase))
                relativePath = relativePath[8..];

            if (!relativePath.StartsWith(@".\", StringComparison.OrdinalIgnoreCase))
                relativePath = @$".\{relativePath}"; // Really just for visual flavor
            
            context.Console.Trace($"Copying {relativePath} to {outputDirectory.FullName}");
            var outputPath = Path.Combine(outputDirectory.FullName, relativePath);
            var outputDirectoryPath = Path.GetDirectoryName(outputPath) ?? throw new InvalidOperationException("Output directory path not set.");
            await context.FileSystem.CreateFolderAsync(outputDirectoryPath);
            await context.FileSystem.CopyAsync(document.FilePath!, outputPath, true);
        }
        
        return context;
    }
}