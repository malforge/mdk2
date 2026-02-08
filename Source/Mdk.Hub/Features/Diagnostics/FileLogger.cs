using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Mal.SourceGeneratedDI;
using Mdk.Hub.Features.Storage;

namespace Mdk.Hub.Features.Diagnostics;

/// <summary>
/// File-based logger that writes log entries to disk and maintains log rotation.
/// </summary>
[Singleton<ILogger>]
public class FileLogger : ILogger
{
    readonly Lock _lock = new();
    readonly IFileStorageService _fileStorage;
    readonly string _logDirectory;
    readonly string _logFilePath;
    readonly int _maxLogFiles = 7; // Keep last 7 days

    /// <summary>
    /// Initializes a new instance of the <see cref="FileLogger"/> class and sets up log file rotation.
    /// </summary>
    public FileLogger(IFileStorageService fileStorage)
    {
        _fileStorage = fileStorage;
        _logDirectory = Path.Combine(_fileStorage.GetLocalApplicationDataPath(), "Hub", "Logs");

        // Create directory if it doesn't exist
        _fileStorage.CreateDirectory(_logDirectory);

        // Log file name with date
        var logFileName = $"mdk-hub-{DateTime.Now:yyyy-MM-dd}.log";
        _logFilePath = Path.Combine(_logDirectory, logFileName);

        // Clean up old log files
        CleanupOldLogs();

        // Write startup message with version
        var version = GetType().Assembly.GetName().Version?.ToString() ?? "unknown";
        // Use AppContext.BaseDirectory instead of Assembly.Location for single-file apps
        var productVersion = "unknown";
        try
        {
            var executablePath = Environment.ProcessPath ?? AppContext.BaseDirectory;
            if (!string.IsNullOrEmpty(executablePath) && _fileStorage.FileExists(executablePath))
            {
                productVersion = System.Diagnostics.FileVersionInfo.GetVersionInfo(executablePath).ProductVersion ?? version;
            }
            else
            {
                productVersion = version;
            }
        }
        catch
        {
            productVersion = version;
        }
        
        WriteLog(new LogEntry
        {
            Timestamp = DateTimeOffset.Now,
            Level = LogLevel.Info,
            Message = $"========================================",
            FilePath = "",
            LineNumber = 0,
            MemberName = ""
        });
        WriteLog(new LogEntry
        {
            Timestamp = DateTimeOffset.Now,
            Level = LogLevel.Info,
            Message = $"MDK Hub {productVersion} - Session Started",
            FilePath = "",
            LineNumber = 0,
            MemberName = ""
        });
        WriteLog(new LogEntry
        {
            Timestamp = DateTimeOffset.Now,
            Level = LogLevel.Info,
            Message = $"========================================",
            FilePath = "",
            LineNumber = 0,
            MemberName = ""
        });
    }

    /// <summary>
    /// Logs a debug message.
    /// </summary>
    /// <param name="message">The message to log.</param>
    /// <param name="filePath">The source file path (automatically captured).</param>
    /// <param name="lineNumber">The source line number (automatically captured).</param>
    /// <param name="memberName">The calling member name (automatically captured).</param>
    public void Debug(string message, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string memberName = "")
        => WriteLog(new LogEntry { Timestamp = DateTimeOffset.Now, Level = LogLevel.Debug, Message = message, FilePath = filePath, LineNumber = lineNumber, MemberName = memberName });

    /// <summary>
    /// Logs an informational message.
    /// </summary>
    /// <param name="message">The message to log.</param>
    /// <param name="filePath">The source file path (automatically captured).</param>
    /// <param name="lineNumber">The source line number (automatically captured).</param>
    /// <param name="memberName">The calling member name (automatically captured).</param>
    public void Info(string message, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string memberName = "")
        => WriteLog(new LogEntry { Timestamp = DateTimeOffset.Now, Level = LogLevel.Info, Message = message, FilePath = filePath, LineNumber = lineNumber, MemberName = memberName });

    /// <summary>
    /// Logs a warning message.
    /// </summary>
    /// <param name="message">The message to log.</param>
    /// <param name="filePath">The source file path (automatically captured).</param>
    /// <param name="lineNumber">The source line number (automatically captured).</param>
    /// <param name="memberName">The calling member name (automatically captured).</param>
    public void Warning(string message, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string memberName = "")
        => WriteLog(new LogEntry { Timestamp = DateTimeOffset.Now, Level = LogLevel.Warning, Message = message, FilePath = filePath, LineNumber = lineNumber, MemberName = memberName });

    /// <summary>
    /// Logs an error message.
    /// </summary>
    /// <param name="message">The message to log.</param>
    /// <param name="filePath">The source file path (automatically captured).</param>
    /// <param name="lineNumber">The source line number (automatically captured).</param>
    /// <param name="memberName">The calling member name (automatically captured).</param>
    public void Error(string message, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string memberName = "")
        => WriteLog(new LogEntry { Timestamp = DateTimeOffset.Now, Level = LogLevel.Error, Message = message, FilePath = filePath, LineNumber = lineNumber, MemberName = memberName });

    /// <summary>
    /// Logs an error message with an associated exception.
    /// </summary>
    /// <param name="message">The message to log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="filePath">The source file path (automatically captured).</param>
    /// <param name="lineNumber">The source line number (automatically captured).</param>
    /// <param name="memberName">The calling member name (automatically captured).</param>
    public void Error(string message, Exception exception, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string memberName = "")
    {
        var fullMessage = $"{message}\nException: {exception.GetType().Name}: {exception.Message}\nStack Trace:\n{exception.StackTrace}";
        WriteLog(new LogEntry { Timestamp = DateTimeOffset.Now, Level = LogLevel.Error, Message = fullMessage, FilePath = filePath, LineNumber = lineNumber, MemberName = memberName, Exception = exception });
    }

    /// <summary>
    /// Gets the full path to the current log file.
    /// </summary>
    /// <returns>The log file path.</returns>
    public string GetLogFilePath() => _logFilePath;

    void WriteLog(LogEntry entry)
    {
        try
        {
            lock (_lock)
            {
                var timestamp = entry.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff");
                var location = !string.IsNullOrEmpty(entry.FileName)
                    ? $" [{entry.FileName}:{entry.LineNumber} {entry.MemberName}]"
                    : "";
                var logLine = $"[{timestamp}] [{entry.Level}]{location} {entry.Message}";
                _fileStorage.AppendAllText(_logFilePath, logLine + Environment.NewLine);

                // Also write to debug output for development
                System.Diagnostics.Debug.WriteLine(logLine);
            }
        }
        catch
        {
            // Can't log if logging fails - just ignore
        }
    }

    void CleanupOldLogs()
    {
        try
        {
            var logFiles = _fileStorage.GetFiles(_logDirectory, "mdk-hub-*.log")
                .Select(f => new { Path = f, LastWrite = _fileStorage.GetLastWriteTimeUtc(f) })
                .OrderByDescending(f => f.LastWrite)
                .ToList();

            // Keep only the most recent files
            foreach (var file in logFiles.Skip(_maxLogFiles))
            {
                try
                {
                    _fileStorage.DeleteFile(file.Path);
                }
                catch
                {
                    // Ignore deletion failures
                }
            }
        }
        catch
        {
            // Ignore cleanup failures
        }
    }
}
