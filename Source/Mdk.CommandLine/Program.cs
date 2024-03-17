using System.Collections.Immutable;
using System.Threading.Tasks;
using Mdk.CommandLine.Commands;
using Mdk.CommandLine.SharedApi;

namespace Mdk.CommandLine;

/// <summary>
///     This is the main entry point for the command line application.
/// </summary>
public static class Program
{
    static IConsole CreateConsole(ProgramParameters parameters)
    {
        IConsole console = new DirectConsole();
        if (parameters.Trace)
            ((DirectConsole)console).TraceEnabled = true;
        if (parameters.Log == null)
            return console;
        
        var fileLogger = new FileConsole(parameters.Log, parameters.Trace);
        console = new CompositeConsole
        {
            Loggers = ImmutableArray.Create(console, fileLogger)
        };
        return console;
    }

    /// <summary>
    ///     The main entry point for the command line application.
    /// </summary>
    /// <param name="args"></param>
    /// <returns></returns>
    public static async Task<int> Main(string[] args)
    {
        IConsole console;
        var parameters = new ProgramParameters();
        if (!parameters.TryLoad(args, out var failureReason))
        {
            console = CreateConsole(parameters);
            console.Print(failureReason)
                .Print();
            parameters.Help(console);
            return -1;
        }
        console = CreateConsole(parameters);
        var interaction = new Interaction(console, parameters.Interactive);
        using var httpClient = new WebHttpClient();
        try
        {
            await parameters.ExecuteAsync(console, httpClient, interaction);
            return 0;
        }
        catch (CommandLineException e)
        {
            console.Print(e.Message);
            return e.ErrorCode;
        }
    }
}