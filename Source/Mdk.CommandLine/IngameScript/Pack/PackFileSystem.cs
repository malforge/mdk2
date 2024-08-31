using System.IO;
using System.Threading.Tasks;
using Mdk.CommandLine.SharedApi;

namespace Mdk.CommandLine.IngameScript.Pack;

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
}