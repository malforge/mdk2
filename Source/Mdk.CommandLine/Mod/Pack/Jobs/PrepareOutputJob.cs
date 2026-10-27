using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Mdk.CommandLine.CommandLine;
using Mdk.CommandLine.Shared;
using Mdk.CommandLine.Utility;

namespace Mdk.CommandLine.Mod.Pack.Jobs;

/// <summary>
///     This job prepares the output directory. If it doesn't exist, it will be created. If it does exist, it will be
///     cleaned.
/// </summary>
/// <remarks>
///     Cleaning involves deleting all files and directories in the output directory. Only files that match the output
///     clean filter will be deleted, any other files will be left in place. On a dry run nothing is created or deleted.
/// </remarks>
internal class PrepareOutputJob : ModJob
{
    public override async Task<ModPackContext> ExecuteAsync(ModPackContext context)
    {
        var outputDirectory = new DirectoryInfo(context.FileSystem.OutputDirectory);
        var isDryRun = context.Parameters.PackVerb.DryRun;
        context.Console.Trace($"Preparing output directory {outputDirectory.FullName}");
        if (outputDirectory.FullName.Length <= 4)
            throw new CommandLineException(-1, "The output directory is the root of the drive.");

        if (!outputDirectory.Exists)
        {
            if (isDryRun)
                context.Console.Trace("Dry run: output directory does not exist, would create it.");
            else
            {
                context.Console.Trace("Output directory does not exist, creating it.");
                outputDirectory.Create();
            }
            await MetaFile.WriteAsync(context.FileSystem, MdkProjectType.Mod);
            return context;
        }

        if (!context.FileSystem.HasMetaFile(outputDirectory.FullName) && outputDirectory.EnumerateFileSystemInfos().Any())
            throw new CommandLineException(-1, $"Expected to find a file named 'mdk.meta' in the output directory {outputDirectory.FullName}, which is used to identify MDK mods. The directory is not empty, so it's likely not the correct directory.");

        context.Console.Trace("Output directory exists, cleaning it.");

        var stack = new Stack<FileSystemInfo>();
        foreach (var info in outputDirectory.EnumerateFileSystemInfos())
            stack.Push(info);
        var folders = new List<DirectoryInfo>();
        var keptFiles = new List<string>();
        var pathToMetaFile = Path.Combine(outputDirectory.FullName, "mdk.meta");
        while (stack.Count > 0)
        {
            var current = stack.Pop();
            if (current is DirectoryInfo directory)
            {
                foreach (var info in directory.EnumerateFileSystemInfos())
                    stack.Push(info);
                folders.Add(directory);
                continue;
            }

            var relativePath = current.GetPathRelativeTo(outputDirectory);
            if (string.Equals(current.FullName, pathToMetaFile, StringComparison.OrdinalIgnoreCase) || !context.OutputCleanFilter.IsMatch(current.FullName))
            {
                context.Console.Trace($"Leaving {relativePath} in place.");
                keptFiles.Add(current.FullName);
                continue;
            }

            if (isDryRun)
            {
                context.Console.Trace($"Dry run: would delete {relativePath}");
                continue;
            }

            try
            {
                context.Console.Trace($"Deleting {relativePath}");
                current.Delete();
            }
            catch (Exception e)
            {
                context.Console.Print($"Failed to delete {current.FullName}: {e.Message}");
            }
        }

        for (var i = folders.Count - 1; i >= 0; i--)
        {
            var folder = folders[i];
            var relativePath = folder.GetPathRelativeTo(outputDirectory);
            if (isDryRun)
            {
                var folderPrefix = folder.FullName + Path.DirectorySeparatorChar;
                if (keptFiles.Any(file => file.StartsWith(folderPrefix, StringComparison.OrdinalIgnoreCase)))
                    context.Console.Trace($"Leaving {relativePath} in place because it's not empty.");
                else
                    context.Console.Trace($"Dry run: would delete {relativePath}");
                continue;
            }

            if (folder.EnumerateFileSystemInfos().Any())
            {
                context.Console.Trace($"Leaving {relativePath} in place because it's not empty.");
                continue;
            }

            try
            {
                context.Console.Trace($"Deleting {relativePath}");
                folder.Delete();
            }
            catch (Exception e)
            {
                context.Console.Print($"Failed to delete {folder.FullName}: {e.Message}");
            }
        }

        await MetaFile.WriteAsync(context.FileSystem, MdkProjectType.Mod);
        
        return context;
    }
}