using System;
using System.IO;

namespace Mdk.Hub.Features.Diagnostics;

/// <summary>
/// Represents a single log entry with timestamp, level, and source location information.
/// </summary>
public readonly struct LogEntry
{
    /// <summary>
    /// Gets the timestamp when the log entry was created.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; }
    
    /// <summary>
    /// Gets the severity level of the log entry.
    /// </summary>
    public LogLevel Level { get; init; }
    
    /// <summary>
    /// Gets the log message.
    /// </summary>
    public string Message { get; init; }
    
    /// <summary>
    /// Gets the full path of the source file where the log was created.
    /// </summary>
    public string FilePath { get; init; }
    
    /// <summary>
    /// Gets the line number in the source file where the log was created.
    /// </summary>
    public int LineNumber { get; init; }
    
    /// <summary>
    /// Gets the member name (method, property, etc.) where the log was created.
    /// </summary>
    public string MemberName { get; init; }
    
    /// <summary>
    /// Gets the associated exception, if any.
    /// </summary>
    public Exception? Exception { get; init; }

    /// <summary>
    /// Gets the file name (without path) from the <see cref="FilePath"/>.
    /// </summary>
    public string FileName => Path.GetFileName(FilePath);
}