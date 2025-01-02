using System.IO;
using System.Threading.Tasks;
using Mdk.CommandLine.Shared.Api;

namespace Mdk.CommandLine.Shared;

/// <inheritdoc />
public class PackFileSystem(string projectPath, string outputDirectory, string traceDirectory, IConsole console) : IFileSystem
{
    readonly IConsole _console = console;

    /// <inheritdoc />
    public string ProjectPath { get; } = projectPath;

    /// <inheritdoc />
    public string OutputDirectory { get; } = outputDirectory;

    /// <inheritdoc />
    public string TraceDirectory { get; } = traceDirectory;

    /// <inheritdoc />
    public async Task WriteAsync(string fileName, string text)
    {
        var fullPath = Path.Combine(OutputDirectory, fileName);
        var fileInfo = new FileInfo(fullPath);
        if (!fileInfo.Directory!.Exists)
            fileInfo.Directory.Create();
        await File.WriteAllTextAsync(fullPath, text).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task WriteTraceAsync(string fileName, string text)
    {
        var fullPath = Path.Combine(TraceDirectory, fileName);
        var fileInfo = new FileInfo(fullPath);
        if (!fileInfo.Directory!.Exists)
            fileInfo.Directory.Create();
        await File.WriteAllTextAsync(fullPath, text).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public bool Exists(string fileName)
    {
        return File.Exists(Path.Combine(OutputDirectory, fileName));
    }

    /// <inheritdoc />
    public async Task CopyAsync(string sourceFile, string targetFile, bool overwrite = false)
    {
        await using var sourceStream = new FileStream(sourceFile, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan);
        await using var targetStream = new FileStream(targetFile, overwrite ? FileMode.Create : FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan);
        await sourceStream.CopyToAsync(targetStream).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<bool> CreateFolderAsync(string path)
    {
        var directoryInfo = new DirectoryInfo(Path.Combine(OutputDirectory, path));
        if (directoryInfo.Exists)
            return Task.FromResult(false);
        directoryInfo.Create();
        return Task.FromResult(true);
    }
}