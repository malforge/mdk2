using System;
using System.Runtime.CompilerServices;

namespace Mdk.Hub.Features.Diagnostics;

public interface ILogger
{
    void Debug(string message, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string memberName = "");
    void Info(string message, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string memberName = "");
    void Warning(string message, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string memberName = "");
    void Error(string message, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string memberName = "");
    void Error(string message, Exception exception, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string memberName = "");
    
    string GetLogFilePath();
}

public enum LogLevel
{
    Debug,
    Info,
    Warning,
    Error
}

public readonly struct LogEntry
{
    public DateTimeOffset Timestamp { get; init; }
    public LogLevel Level { get; init; }
    public string Message { get; init; }
    public string FilePath { get; init; }
    public int LineNumber { get; init; }
    public string MemberName { get; init; }
    public Exception? Exception { get; init; }
    
    public string FileName => System.IO.Path.GetFileName(FilePath);
}
