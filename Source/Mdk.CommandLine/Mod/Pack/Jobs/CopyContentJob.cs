using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Mdk.CommandLine.CommandLine;
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
            var relativePath = ResolveContentRelativePath(document.FilePath!, projectDirectory, document.Folders, document.Name);

            context.Console.Trace($"Copying {relativePath} to {outputDirectory.FullName}");
            var outputPath = PathEx.CombineInside(outputDirectory.FullName, relativePath);
            var outputDirectoryPath = Path.GetDirectoryName(outputPath) ?? throw new InvalidOperationException("Output directory path not set.");
            await context.FileSystem.CreateFolderAsync(outputDirectoryPath);
            await context.FileSystem.CopyAsync(document.FilePath!, outputPath, true);
        }

        return context;
    }

    /// <summary>
    /// Resolves where a content document should be written, relative to the mod output directory.
    /// </summary>
    /// <param name="filePath">The full path of the content file.</param>
    /// <param name="projectDirectory">The directory of the project being packed.</param>
    /// <param name="folders">The document's logical folder path (its <c>Link</c>, surfaced by Roslyn).</param>
    /// <param name="name">The document's file name.</param>
    /// <remarks>
    /// A file under the project keeps its path there. A file from outside the project is placed by its
    /// <c>Link</c> when it has one, otherwise by its path inside the shared project (<c>.projitems</c>) that holds
    /// it. A leading <c>Content\</c> is dropped either way.
    /// </remarks>
    /// <exception cref="CommandLineException">A file on another drive has neither a Link nor a shared project.</exception>
    internal static string ResolveContentRelativePath(string filePath, DirectoryInfo projectDirectory, IReadOnlyList<string> folders, string name)
    {
        var relativePrefix = $".{Path.DirectorySeparatorChar}";
        var content = $"Content{Path.DirectorySeparatorChar}";
        var relativeContent = $"{relativePrefix}{content}";

        var file = new FileInfo(filePath);
        var relativePath = file.GetPathRelativeTo(projectDirectory);
        var isOnAnotherDrive = Path.IsPathRooted(relativePath);
        var isOutsideProject = PathEx.LeavesBase(relativePath);

        if (isOutsideProject && folders.Count > 0)
        {
            var parts = new string[folders.Count + 1];
            for (var i = 0; i < folders.Count; i++)
                parts[i] = folders[i];
            parts[folders.Count] = name;
            relativePath = Path.Combine(parts);
        }
        else if (isOutsideProject && SharedProjectFile.FindFor(file, projectDirectory) is { } sharedProject)
            relativePath = Path.GetRelativePath(sharedProject.DirectoryName!, file.FullName);
        else if (isOnAnotherDrive)
            throw new CommandLineException(-1, $"{file.FullName} is on another drive from the project and has no Link, so there is no way to tell where it belongs in the mod.");

        // Remove the Content folder from the path: An early version of the packer copied all
        // the files from the root of the project as long as it wasn't code, which confused
        // people _and_ caused issues of certain temporary files also being copied.
        if (relativePath.StartsWith(relativeContent, StringComparison.OrdinalIgnoreCase))
            relativePath = relativePath[relativeContent.Length..];
        else if (relativePath.StartsWith(content, StringComparison.OrdinalIgnoreCase))
            relativePath = relativePath[content.Length..];

        if (!relativePath.StartsWith(relativePrefix, StringComparison.Ordinal))
            relativePath = $"{relativePrefix}{relativePath}"; // Really just for visual flavor

        return relativePath;
    }
}
