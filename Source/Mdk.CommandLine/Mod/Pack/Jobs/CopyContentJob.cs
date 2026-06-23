using System;
using System.Collections.Generic;
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
            var physicalRelativePath = new FileInfo(document.FilePath!).GetPathRelativeTo(projectDirectory);
            var relativePath = ResolveContentRelativePath(physicalRelativePath, document.Folders, document.Name);

            context.Console.Trace($"Copying {relativePath} to {outputDirectory.FullName}");
            var outputPath = Path.Combine(outputDirectory.FullName, relativePath);
            var outputDirectoryPath = Path.GetDirectoryName(outputPath) ?? throw new InvalidOperationException("Output directory path not set.");
            await context.FileSystem.CreateFolderAsync(outputDirectoryPath);
            await context.FileSystem.CopyAsync(document.FilePath!, outputPath, true);
        }

        return context;
    }

    /// <summary>
    /// Resolves where a content document should be written, relative to the mod output directory.
    /// </summary>
    /// <param name="physicalRelativePath">
    /// The content file's path relative to the project directory (as produced by
    /// <see cref="FileSystemInfoExtensions.GetPathRelativeTo(System.IO.FileSystemInfo,System.IO.DirectoryInfo?)" />):
    /// <c>.\Content\Data\X.sbc</c> for a file under the project, or <c>..\Other\Content\X.sbc</c> for one
    /// linked in from outside it.
    /// </param>
    /// <param name="folders">The document's logical folder path (its <c>Link</c>, surfaced by Roslyn).</param>
    /// <param name="name">The document's file name.</param>
    /// <remarks>
    /// A file that physically lives under the project keeps its on-disk path - the behaviour the packer
    /// has always had. A file linked in from OUTSIDE the project - e.g. shared resources in a referenced
    /// shared project / submodule, declared as
    /// <code>&lt;AdditionalFiles Include="..\..\Shared\Content\..." Link="Content\..." /&gt;</code>
    /// - has a <c>..\</c>-prefixed physical path that would escape the output directory entirely. Such a
    /// file carries a logical path (its <c>Link</c>, surfaced as the document's Folders + Name); that is
    /// used instead so it packs under <c>Content\</c> exactly like a local file. The substitution only
    /// happens for the escaping case and only when a logical path is available, so existing mods that use
    /// local content are byte-for-byte unaffected.
    /// </remarks>
    internal static string ResolveContentRelativePath(string physicalRelativePath, IReadOnlyList<string> folders, string name)
    {
        var relativePrefix = $".{Path.DirectorySeparatorChar}";
        var content = $"Content{Path.DirectorySeparatorChar}";
        var relativeContent = $"{relativePrefix}{content}";

        var relativePath = physicalRelativePath;

        // Outside-project file: prefer its logical Link path so it doesn't escape the output directory.
        if (relativePath.StartsWith("..", StringComparison.Ordinal) && folders.Count > 0)
        {
            var parts = new string[folders.Count + 1];
            for (var i = 0; i < folders.Count; i++)
                parts[i] = folders[i];
            parts[folders.Count] = name;
            relativePath = Path.Combine(parts);
        }

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
