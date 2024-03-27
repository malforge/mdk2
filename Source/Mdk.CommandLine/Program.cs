using System;
using System.Collections.Immutable;
using System.IO;
using System.Threading.Tasks;
using Mdk.CommandLine.CommandLine;
using Mdk.CommandLine.IngameScript.LegacyConversion;
using Mdk.CommandLine.IngameScript.Pack;
using Mdk.CommandLine.IngameScript.Restore;
using Mdk.CommandLine.SharedApi;

namespace Mdk.CommandLine;

/// <summary>
///     This is the main entry point for the command line application.
/// </summary>
public static class Program
{
    static IConsole CreateConsole(Parameters? parameters = null)
    {
        IConsole console = new DirectConsole();
        if (parameters?.Trace == true)
            ((DirectConsole)console).TraceEnabled = true;
        if (parameters?.Log == null)
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
        var parameters = new Parameters();
        try
        {
            parameters.Parse(args);
        }
        catch (CommandLineException e)
        {
            console = CreateConsole();
            console.Print(e.Message);
            parameters.ShowHelp(console);
            throw;
        }
        console = CreateConsole(parameters);
        var interaction = new Interaction(console, parameters.Interactive);
        using var httpClient = new WebHttpClient();
        try
        {
            await RunAsync(parameters, console, httpClient, interaction);
            return 0;
        }
        catch (CommandLineException e)
        {
            console.Print(e.Message);
            return e.ErrorCode;
        }
    }

    /// <summary>
    ///     Run the application with the specified parameters and services.
    /// </summary>
    /// <param name="parameters"></param>
    /// <param name="console"></param>
    /// <param name="httpClient"></param>
    /// <param name="interaction"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public static async Task RunAsync(Parameters parameters, IConsole console, IHttpClient httpClient, IInteraction interaction)
    {
        switch (parameters.Verb)
        {
            case Verb.None:
            case Verb.Help:
                parameters.ShowHelp(console);
                break;
            case Verb.Pack:
                var packer = new ScriptPacker();
                await packer.PackAsync(parameters, console, interaction);
                break;
            case Verb.Restore:
                await RestoreAsync(parameters, console, httpClient, interaction);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    static async Task RestoreAsync(Parameters parameters, IConsole console, IHttpClient httpClient, IInteraction interaction)
    {
        if (parameters.RestoreVerb.ProjectFile is null) throw new CommandLineException(-1, "No project file specified.");
        if (!File.Exists(parameters.RestoreVerb.ProjectFile)) throw new CommandLineException(-1, $"The specified project file '{parameters.RestoreVerb.ProjectFile}' does not exist.");

        if (parameters.RestoreVerb.DryRun)
            console.Print("Currently performing a dry run. No changes will be made.");

        await foreach (var project in MdkProject.LoadAsync(parameters.RestoreVerb.ProjectFile, console))
        {
            switch (project.Type)
            {
                case MdkProjectType.Mod:
                    console.Print($"Mod projects are not yet implemented: {project.Project.Name}");
                    break;

                case MdkProjectType.ProgrammableBlock:
                    console.Print($"Restoring ingame script project: {project.Project.Name}");
                    var restorer = new ScriptRestorer();
                    await restorer.RestoreAsync(parameters, project, console, httpClient, interaction);
                    break;

                case MdkProjectType.LegacyProgrammableBlock:
                    console.Print($"Converting legacy ingame script project: {project.Project.Name}");
                    var converter = new LegacyConverter();
                    await converter.ConvertAsync(parameters, project, console, httpClient);
                    goto case MdkProjectType.ProgrammableBlock;

                case MdkProjectType.Unknown:
                    console.Print($"The project file {project.Project.Name} does not seem to be an MDK project.");
                    break;
            }
        }
    }
}