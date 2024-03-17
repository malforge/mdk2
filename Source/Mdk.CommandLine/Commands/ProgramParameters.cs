using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Mdk.CommandLine.Commands.Help;
using Mdk.CommandLine.Commands.Pack;
using Mdk.CommandLine.Commands.Restore;
using Mdk.CommandLine.SharedApi;
using Mdk.CommandLine.Utility;

namespace Mdk.CommandLine.Commands;

/// <summary>
///     The parameters for the program.
/// </summary>
public class ProgramParameters : Parameters
{
    /// <summary>
    ///     Determines what operation to perform.
    /// </summary>
    public string? Verb { get; private set; }

    /// <summary>
    ///     Parameters specific to the <see cref="Verb" />.
    /// </summary>
    public VerbParameters? VerbParameters { get; private set; }

    /// <summary>
    ///     An optional log file to write to.
    /// </summary>
    public string? Log { get; set; }

    /// <summary>
    ///     Whether to enable trace logging.
    /// </summary>
    public bool Trace { get; set; }
    
    /// <summary>
    /// Whether to use interactive prompts through the external UI app - if available.
    /// </summary>
    public bool Interactive { get; set; }

    bool TryLoadVerb(string verb, Queue<string> queue, [MaybeNullWhen(true)] out string failureReason)
    {
        if (string.Equals(verb, "pack-script", StringComparison.OrdinalIgnoreCase))
        {
            var p = new PackParameters();
            if (!p.TryLoad(this, queue, out failureReason))
                return false;
            Verb = verb;
            VerbParameters = p;
            return true;
        }

        if (string.Equals(verb, "restore-script", StringComparison.OrdinalIgnoreCase))
        {
            var p = new RestoreParameters();
            if (!p.TryLoad(this, queue, out failureReason))
                return false;
            Verb = verb;
            VerbParameters = p;
            return true;
        }

        if (string.Equals(verb, "help", StringComparison.OrdinalIgnoreCase))
        {
            var p = new HelpParameters();
            if (!p.TryLoad(this, queue, out failureReason))
                return false;
            Verb = verb;
            VerbParameters = p;
            return true;
        }

        failureReason = $"Unknown verb: {verb}";
        return false;
    }

    /// <inheritdoc />
    public override bool TryLoad(Queue<string> args, [MaybeNullWhen(true)] out string failureReason)
    {
        var p = new ProgramParameters();
        while (args.Count > 0)
        {
            if (args.TryDequeue("-log"))
            {
                if (!args.TryDequeue(out var log))
                {
                    failureReason = "Missing log file path after -log";
                    return false;
                }
                p.Log = log;
                continue;
            }

            if (args.TryDequeue("-trace"))
            {
                p.Trace = true;
                continue;
            }
            
            if (args.TryDequeue("-interactive"))
            {
                p.Interactive = true;
                continue;
            }

            if (TryLoadVerb(args.Dequeue(), args, out failureReason))
                continue;

            return false;
        }

        if (p.Verb is null)
        {
            failureReason = "No verb specified.";
            return false;
        }

        failureReason = null;
        return true;
    }

    /// <inheritdoc />
    public override void Help(IConsole console)
    {
        var displayVersion = typeof(ProgramParameters).Assembly.GetName().Version?.ToString() ?? "0.0.0";
        console.Print($"MDK v{displayVersion}")
            .Print()
            .Print("Usage: mdk [options] <verb> [verb-options]")
            .Print()
            .Print("Options:")
            .Print("  -log <file>  Log output to the specified file.")
            .Print()
            .Print("Verbs:")
            .Print("  pack-script     Pack a programmable block script")
            .Print("  restore-script  Restore a packed programmable block script")
            .Print("  help            Display help for a verb")
            .Print()
            .Print("Use 'mdk help <verb>' for more information about a verb.")
            .Print("  Example: mdk help pack-script");
    }

    /// <summary>
    ///     Executes the command.
    /// </summary>
    /// <param name="console"></param>
    /// <param name="httpClient"></param>
    /// <param name="interaction"></param>
    /// <returns></returns>
    public async Task ExecuteAsync(IConsole console, IHttpClient httpClient, IInteraction interaction)
    {
        var verb = VerbParameters ?? throw new InvalidOperationException("Verb parameters are not set.");
        await verb.ExecuteAsync(console, httpClient, interaction);
    }
}