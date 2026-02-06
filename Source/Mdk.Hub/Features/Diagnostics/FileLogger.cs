using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Mal.SourceGeneratedDI;

namespace Mdk.Hub.Features.Diagnostics;

[Singleton<ILogger>]
public class FileLogger : ILogger
{
    readonly Lock _lock = new();
    readonly string _logDirectory;
    readonly string _logFilePath;
    readonly int _maxLogFiles = 7; // Keep last 7 days

    public FileLogger()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _logDirectory = Path.Combine(appData, "MDK2", "Hub", "Logs");

        // Create directory if it doesn't exist
        Directory.CreateDirectory(_logDirectory);

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
            if (!string.IsNullOrEmpty(executablePath) && File.Exists(executablePath))
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

    public void Debug(string message, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string memberName = "")
        => WriteLog(new LogEntry { Timestamp = DateTimeOffset.Now, Level = LogLevel.Debug, Message = message, FilePath = filePath, LineNumber = lineNumber, MemberName = memberName });

    public void Info(string message, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string memberName = "")
        => WriteLog(new LogEntry { Timestamp = DateTimeOffset.Now, Level = LogLevel.Info, Message = message, FilePath = filePath, LineNumber = lineNumber, MemberName = memberName });

    public void Warning(string message, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string memberName = "")
        => WriteLog(new LogEntry { Timestamp = DateTimeOffset.Now, Level = LogLevel.Warning, Message = message, FilePath = filePath, LineNumber = lineNumber, MemberName = memberName });

    public void Error(string message, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string memberName = "")
        => WriteLog(new LogEntry { Timestamp = DateTimeOffset.Now, Level = LogLevel.Error, Message = message, FilePath = filePath, LineNumber = lineNumber, MemberName = memberName });

    public void Error(string message, Exception exception, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string memberName = "")
    {
        var fullMessage = $"{message}\nException: {exception.GetType().Name}: {exception.Message}\nStack Trace:\n{exception.StackTrace}";
        WriteLog(new LogEntry { Timestamp = DateTimeOffset.Now, Level = LogLevel.Error, Message = fullMessage, FilePath = filePath, LineNumber = lineNumber, MemberName = memberName, Exception = exception });
    }

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
                File.AppendAllText(_logFilePath, logLine + Environment.NewLine);

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
            var logFiles = Directory.GetFiles(_logDirectory, "mdk-hub-*.log")
                .Select(f => new FileInfo(f))
                .OrderByDescending(f => f.LastWriteTime)
                .ToList();

            // Keep only the most recent files
            foreach (var file in logFiles.Skip(_maxLogFiles))
            {
                try
                {
                    file.Delete();
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
