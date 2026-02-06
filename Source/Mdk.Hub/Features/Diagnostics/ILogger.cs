using System;
using System.Runtime.CompilerServices;

namespace Mdk.Hub.Features.Diagnostics;

/// <summary>
///     Provides diagnostic logging capabilities with caller context tracking.
/// </summary>
public interface ILogger
{
    /// <summary>
    ///     Logs a debug-level message with caller context information.
    /// </summary>
    void Debug(string message, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string memberName = "");
    
    /// <summary>
    ///     Logs an informational message with caller context information.
    /// </summary>
    void Info(string message, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string memberName = "");
    
    /// <summary>
    ///     Logs a warning message with caller context information.
    /// </summary>
    void Warning(string message, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string memberName = "");
    
    /// <summary>
    ///     Logs an error message with caller context information.
    /// </summary>
    void Error(string message, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string memberName = "");
    
    /// <summary>
    ///     Logs an error message with exception details and caller context information.
    /// </summary>
    void Error(string message, Exception exception, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string memberName = "");

    /// <summary>
    ///     Gets the full file path to the log file.
    /// </summary>
    string GetLogFilePath();
}