using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Threading.Tasks;
using Mdk.CommandLine.Commands.Pack;
using Mdk.CommandLine.Commands.Restore;
using Mdk.CommandLine.SharedApi;
using Mdk.CommandLine.Utility;

namespace Mdk.CommandLine.Commands.Help;

/// <summary>
///     The parameters for the help command.
/// </summary>
public class HelpParameters : VerbParameters
{
    /// <summary>
    ///     What verb to display help for, or null to display general help.
    /// </summary>
    public string? Verb { get; set; }

    /// <summary>
    ///     Whether to list available processors.
    /// </summary>
    public bool ListProcessors { get; set; }

    /// <inheritdoc />
    public override bool TryLoad(Queue<string> args, [MaybeNullWhen(true)] out string failureReason)
    {
        while (args.Count > 0)
        {
            if (TryParseGlobalOptions(args, out failureReason))
                continue;

            if (args.TryDequeue("-list-processors"))
            {
                ListProcessors = true;
                continue;
            }

            if (Verb is not null)
            {
                failureReason = "Only one verb can be specified.";
                return false;
            }
            Verb = args.Dequeue();
        }

        failureReason = null;
        return true;
    }

    /// <inheritdoc />
    public override void Help(IConsole console) =>
        console.Print("Usage: mdk help [options] [verb]")
            .Print()
            .Print("Options:")
            .Print("  -log <file>    Log output to the specified file.")
            .Print("  -trace         Enable trace output.")
            // .Print("  -list-processors")
            // .Print("                 List available processors.")
            .Print()
            .Print("Verb:")
            .Print("  The verb to display help for.");

    /// <inheritdoc />
    public override Task ExecuteAsync(IConsole console, IHttpClient httpClient, IInteraction interaction)
    {
        string header;
        var version = typeof(Program).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (version != null)
        {
            var lastPlusIndex = version.LastIndexOf('+');
            if (lastPlusIndex >= 0) version = version[..lastPlusIndex];
            header = $"MDK v{version}";
        }
        else
            header = "MDK Development Version";

        console.Print(header)
            .Print(new string('=', header.Length))
            .Print();

        if (string.Equals(Verb, "pack", StringComparison.OrdinalIgnoreCase))
        {
            var verbParameters = new PackParameters();
            verbParameters.Help(console);
        }
        else if (string.Equals(Verb, "restore", StringComparison.OrdinalIgnoreCase))
        {
            var verbParameters = new RestoreParameters();
            verbParameters.Help(console);
        }
        else
            Help(console);

        return Task.CompletedTask;
    }
}