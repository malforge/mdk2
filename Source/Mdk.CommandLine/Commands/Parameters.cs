using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Mdk.CommandLine.SharedApi;

namespace Mdk.CommandLine.Commands;

/// <summary>
/// A base class for command line parameters.
/// </summary>
public abstract class Parameters
{
    /// <summary>
    /// Attempts to load the parameters from the specified arguments.
    /// </summary>
    /// <param name="args"></param>
    /// <param name="failureReason"></param>
    /// <returns></returns>
    public bool TryLoad(string[] args, [MaybeNullWhen(true)] out string failureReason)
    {
        var queue = new Queue<string>(args);
        return TryLoad(queue, out failureReason);
    }

    /// <summary>
    /// Attempts to load the parameters from the specified arguments.
    /// </summary>
    /// <param name="args"></param>
    /// <param name="failureReason"></param>
    /// <returns></returns>
    public abstract bool TryLoad(Queue<string> args, [MaybeNullWhen(true)] out string failureReason);

    /// <summary>
    /// Display help for this particular parameter set.
    /// </summary>
    /// <param name="console"></param>
    public abstract void Help(IConsole console);
}
