using System.Collections.Immutable;
using Mdk.CommandLine.SharedApi;

namespace Mdk.CommandLine;

/// <summary>
///     A logger that routes log messages to multiple loggers.
/// </summary>
public class CompositeConsole : IConsole
{
    /// <summary>
    ///     The loggers that will receive log messages.
    /// </summary>
    public required ImmutableArray<IConsole> Loggers { get; init; }

    /// <summary>
    ///     Whether trace messages are enabled.
    /// </summary>
    /// <remarks>
    ///     The composite console will return true if any of the loggers have trace enabled,
    ///     but not all loggers have to have trace enabled.
    /// </remarks>
    public bool TraceEnabled
    {
        get
        {
            foreach (var logger in Loggers)
            {
                if (logger.TraceEnabled)
                    return true;
            }
            return false;
        }
    }

    /// <inheritdoc />
    public IConsole Trace(string? message = null, int wrapIndent = 4)
    {
        foreach (var logger in Loggers)
        {
            if (logger.TraceEnabled)
                logger.Trace(message);
        }
        return this;
    }

    /// <inheritdoc />
    public IConsole Print(string? message = null, int wrapIndent = 4)
    {
        foreach (var logger in Loggers)
            logger.Print(message, wrapIndent);
        return this;
    }
}