namespace Mdk.Hub.Features.Diagnostics;

/// <summary>
/// Specifies the severity level of a log message.
/// </summary>
public enum LogLevel
{
    /// <summary>
    /// Debug-level messages used for detailed diagnostic information.
    /// </summary>
    Debug,
    
    /// <summary>
    /// Informational messages that highlight application progress.
    /// </summary>
    Info,
    
    /// <summary>
    /// Warning messages that indicate potential issues or unexpected behavior.
    /// </summary>
    Warning,
    
    /// <summary>
    /// Error messages that indicate failures or critical problems.
    /// </summary>
    Error
}