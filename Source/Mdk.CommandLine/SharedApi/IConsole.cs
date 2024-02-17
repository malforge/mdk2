namespace Mdk.CommandLine.SharedApi;

public interface IConsole
{
    /// <summary>
    ///     Whether trace messages are enabled.
    /// </summary>
    bool TraceEnabled { get; }

    /// <summary>
    ///     Print a trace message, which is only printed if enabled by the user.
    /// </summary>
    /// <param name="message"></param>
    /// <param name="wrapIndent">The number of spaces to indent wrapped lines. Set to -1 to attempt an autodetect.</param>
    IConsole Trace(string? message = null, int wrapIndent = 4);

    /// <summary>
    ///     Print an informational message.
    /// </summary>
    /// <param name="message"></param>
    /// <param name="wrapIndent">The number of spaces to indent wrapped lines. Set to -1 to attempt an autodetect.</param>
    IConsole Print(string? message = null, int wrapIndent = 4);
}