namespace Mdk.CommandLine.SharedApi;

/// <summary>
/// Extensions for <see cref="IConsole"/>.
/// </summary>
public static class ConsoleExtensions
{
    /// <summary>
    ///    Print a trace message, which is only printed if enabled by the user. This variant only prints if the condition is true.
    /// </summary>
    /// <param name="console"></param>
    /// <param name="condition"></param>
    /// <param name="message"></param>
    /// <param name="wrapIndent"></param>
    /// <returns></returns>
    public static IConsole TraceIf(this IConsole console, bool condition, string message, int wrapIndent = 4)
    {
        if (!condition)
            return console;
        if (console.TraceEnabled)
            console.Print(message, wrapIndent);
        return console;
    }
    
    /// <summary>
    ///   Print an informational message. This variant only prints if the condition is true.
    /// </summary>
    /// <param name="console"></param>
    /// <param name="condition"></param>
    /// <param name="message"></param>
    /// <param name="wrapIndent"></param>
    /// <returns></returns>
    public static IConsole PrintIf(this IConsole console, bool condition, string message, int wrapIndent = 4)
    {
        if (!condition)
            return console;
        console.Print(message, wrapIndent);
        return console;
    }
}