using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Mdk.CommandLine.Shared.Api;

namespace Mdk.CommandLine.Shared;

/// <summary>
///     A file system for dry runs that traces what would be written to the output instead of writing it.
/// </summary>
/// <remarks>
///     Trace files are still written, since they go to the trace directory rather than the output. Files and folders
///     it would have written count as existing for the rest of the pack.
/// </remarks>
public class DryRunFileSystem(IFileSystem inner, IConsole console) : IFileSystem
{
    readonly HashSet<string> _writtenFiles = new(StringComparer.OrdinalIgnoreCase);
    readonly HashSet<string> _createdFolders = new(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc />
    public string ProjectPath => inner.ProjectPath;

    /// <inheritdoc />
    public string OutputDirectory => inner.OutputDirectory;

    /// <inheritdoc />
    public string TraceDirectory => inner.TraceDirectory;

    /// <inheritdoc />
    public Task WriteAsync(string fileName, string text)
    {
        var fullPath = Path.GetFullPath(Path.Combine(OutputDirectory, fileName));
        _writtenFiles.Add(fullPath);
        console.Trace($"Dry run: would write {fullPath}");
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task WriteTraceAsync(string fileName, string text) => inner.WriteTraceAsync(fileName, text);

    /// <inheritdoc />
    public bool Exists(string fileName) =>
        _writtenFiles.Contains(Path.GetFullPath(Path.Combine(OutputDirectory, fileName))) || inner.Exists(fileName);

    /// <inheritdoc />
    public Task CopyAsync(string sourceFile, string targetFile, bool overwrite = false)
    {
        _writtenFiles.Add(Path.GetFullPath(targetFile));
        console.Trace($"Dry run: would copy {sourceFile} to {targetFile}");
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<bool> CreateFolderAsync(string path)
    {
        var fullPath = Path.GetFullPath(Path.Combine(OutputDirectory, path));
        if (Directory.Exists(fullPath) || !_createdFolders.Add(fullPath))
            return Task.FromResult(false);
        console.Trace($"Dry run: would create folder {fullPath}");
        return Task.FromResult(true);
    }
}
