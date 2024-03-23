using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Mdk.CommandLine.SharedApi;
using Mdk.CommandLine.Utility;

namespace Mdk.CommandLine.Commands;

/// <summary>
///     Base class for verb parameters.
/// </summary>
public abstract class VerbParameters : Parameters
{
    /// <summary>
    ///     Parameters global to all verbs.
    /// </summary>
    /// <remarks>
    ///     When parsing verb parameters, some of the parameters may be global. Those are to be stored here.
    /// </remarks>
    public ProgramParameters? Global { get; private set; }

    /// <summary>
    ///     Attempts to load the parameters from the specified arguments. Global parameters are stored in the provided
    ///     <see cref="ProgramParameters" />.
    /// </summary>
    /// <param name="parent"></param>
    /// <param name="args"></param>
    /// <param name="failureReason"></param>
    /// <returns></returns>
    public bool TryLoad(ProgramParameters parent, Queue<string> args, [MaybeNullWhen(true)] out string failureReason)
    {
        Global = parent;
        return TryLoad(args, out failureReason);
    }

    /// <summary>
    ///     A helper method to parse global parameters. Call this first when overriding
    ///     <see cref="Parameters.TryLoad(Queue{string},out string)" /> method in order to preserve their support.
    /// </summary>
    /// <param name="args"></param>
    /// <param name="failureReason"></param>
    /// <returns></returns>
    protected bool TryParseGlobalOptions(Queue<string> args, [MaybeNullWhen(true)] out string failureReason)
    {
        if (Global == null)
        {
            failureReason = "Global parameters are not available.";
            return false;
        }

        if (args.TryDequeue("-log"))
        {
            if (!args.TryDequeue(out var log))
            {
                failureReason = "Log file path is missing.";
                return false;
            }
            Global!.Log = log;
            failureReason = null;
            return true;
        }

        if (args.TryDequeue("-trace"))
        {
            Global!.Trace = true;
            failureReason = null;
            return true;
        }
        
        if (args.TryDequeue("-interactive"))
        {
            Global!.Interactive = true;
            failureReason = null;
            return true;
        }

        failureReason = "Unknown global parameter.";
        return false;
    }

    /// <summary>
    ///     Executes the command.
    /// </summary>
    /// <param name="console"></param>
    /// <param name="httpClient"></param>
    /// <param name="interaction"></param>
    /// <returns></returns>
    public abstract Task ExecuteAsync(IConsole console, IHttpClient httpClient, IInteraction interaction);
}