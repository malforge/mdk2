using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Mdk.CommandLine.Commands;
using Mdk.CommandLine.Commands.Help;
using Mdk.CommandLine.Commands.PackScript;
using Mdk.CommandLine.SharedApi;

namespace Mdk.CommandLine;

/// <summary>
///     This is the main entry point for the command line application.
/// </summary>
public static class Program
{
    /// <summary>
    ///     A dictionary of all available commands.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, Command> Commands = new Dictionary<string, Command>(StringComparer.OrdinalIgnoreCase)
    {
        ["help"] = new HelpCommand(),
        ["pack-script"] = new PackScriptCommand()
    }.AsReadOnly();

    /// <summary>
    ///     The main entry point for the command line application.
    /// </summary>
    /// <param name="args"></param>
    /// <returns></returns>
    public static async Task<int> Main(string[] args)
    {
        List<string> arguments = [..args];

        IConsole console = new DirectConsole();
        try
        {
            var (logFile, enableTrace) = GetLogOptions(arguments);

            if (!arguments.TryDequeue(out var commandName))
                commandName = "help";

            if (enableTrace)
                ((DirectConsole)console).TraceEnabled = true;
            if (logFile != null)
            {
                var fileLogger = new FileConsole(logFile, enableTrace);
                console = new CompositeConsole
                {
                    Loggers = ImmutableArray.Create(console, fileLogger)
                };
            }

            if (!Commands.TryGetValue(commandName, out var command))
            {
                console.Print($"Unknown command: {commandName}")
                    .Print();
                Commands["help"].Help(console);
                return -1;
            }

            await command.ExecuteAsync(arguments, console);
            return 0;
        }
        catch (CommandLineException e)
        {
            console.Print(e.Message);
            return e.ErrorCode;
        }
    }

    static (string? logFile, bool enableTrace) GetLogOptions(List<string> arguments)
    {
        string? logFile = null;
        var enableTrace = false;
        var logIndex = arguments.FindIndex(x => x.Equals("-log", StringComparison.OrdinalIgnoreCase));
        if (logIndex != -1)
        {
            if (logIndex + 1 >= arguments.Count)
                throw new CommandLineException(-1, "Missing log file path after -log");

            logFile = arguments[logIndex + 1];
            arguments.RemoveAt(logIndex);
            arguments.RemoveAt(logIndex);
        }

        var logLevelIndex = arguments.FindIndex(x => x.Equals("-trace", StringComparison.OrdinalIgnoreCase));
        if (logLevelIndex != -1)
        {
            enableTrace = true;
            arguments.RemoveAt(logLevelIndex);
        }

        return (logFile, enableTrace);
    }
}