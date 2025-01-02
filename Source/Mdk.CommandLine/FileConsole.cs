using System;
using System.IO;
using Mdk.CommandLine.Shared.Api;

namespace Mdk.CommandLine;

/// <summary>
///     A console that writes to a file.
/// </summary>
/// <param name="fileName"></param>
/// <param name="enableTrace"></param>
public class FileConsole(string fileName, bool enableTrace) : IConsole
{
    readonly string _fileName = fileName;

    /// <inheritdoc />
    public bool TraceEnabled { get; } = enableTrace;

    /// <inheritdoc />
    public IConsole Trace(string? message = null, int wrapIndent = 4)
    {
        if (!TraceEnabled) return this;
        Print(message, wrapIndent);
        return this;
    }

    /// <inheritdoc />
    public IConsole Print(string? message = null, int wrapIndent = 4)
    {
        if (message == null)
        {
            File.AppendAllText(_fileName, Environment.NewLine);
            return this;
        }
        var time = DateTimeOffset.Now;
        message = $"{time:yyyy-MM-dd HH:mm:ss.fff} {message}";
        File.AppendAllText(_fileName, message);
        return this;
    }
}