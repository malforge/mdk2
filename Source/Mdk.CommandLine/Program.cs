using System;
using System.Collections.Immutable;
using System.IO;
using System.Threading.Tasks;
using Mdk.CommandLine.CommandLine;
using Mdk.CommandLine.IngameScript.LegacyConversion;
using Mdk.CommandLine.IngameScript.Pack;
using Mdk.CommandLine.IngameScript.Restore;
using Mdk.CommandLine.Mod.Pack;
using Mdk.CommandLine.Mod.Restore;
using Mdk.CommandLine.Shared.Api;

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
            Loggers = [console, fileLogger]
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
        var peripherals = Peripherals.Create().FromArguments(args).Build();
        try
        {
            await RunAsync(peripherals);
            return 0;
        }
        catch (CommandLineException e)
        {
            peripherals.Console.Print(e.Message);
            return e.ErrorCode;
        }
        catch (Exception e)
        {
            peripherals.Console.Print(e.ToString());
            return -1;
        }
    }

    /// <summary>
    ///     Run the application with the specified peripherals.
    /// </summary>
    /// <param name="peripherals"></param>
    /// <exception cref="Exception"></exception>
    public static async Task<ImmutableArray<PackedProject>?> RunAsync(Peripherals peripherals)
    {
        if (peripherals.IsEmpty())
            throw new ArgumentException("The peripherals must be set.", nameof(peripherals));
        if (peripherals.Exception != null)
            throw peripherals.Exception;

        return await RunAsync(peripherals.Parameters, peripherals.Console, peripherals.HttpClient, peripherals.Interaction);
    }

    /// <summary>
    ///     Run the application with the specified parameters and services.
    /// </summary>
    /// <param name="parameters"></param>
    /// <param name="console"></param>
    /// <param name="httpClient"></param>
    /// <param name="interaction"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    /// <exception cref="CommandLineException"></exception>
    public static async Task<ImmutableArray<PackedProject>?> RunAsync(Parameters parameters, IConsole console, IHttpClient httpClient, IInteraction interaction)
    {
        switch (parameters.Verb)
        {
            case Verb.None:
            case Verb.Help:
                parameters.ShowHelp(console);
                return null;
            case Verb.Pack:
                return await PackAsync(parameters, console, interaction);
            case Verb.Restore:
                await RestoreAsync(parameters, console, httpClient, interaction);
                return null;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    static async Task<ImmutableArray<PackedProject>?> PackAsync(Parameters parameters, IConsole console, IInteraction interaction)
    {
        if (parameters.PackVerb.ProjectFile is null) throw new CommandLineException(-1, "No project file specified.");
        if (!File.Exists(parameters.PackVerb.ProjectFile)) throw new CommandLineException(-1, $"The specified project file '{parameters.RestoreVerb.ProjectFile}' does not exist.");

        if (parameters.PackVerb.DryRun)
            console.Print("Currently performing a dry run. No changes will be made.");

        var result = ImmutableArray.CreateBuilder<PackedProject>();

        await foreach (var project in MdkProject.LoadAsync(parameters.PackVerb.ProjectFile, console))
        {
            switch (project.Type)
            {
                case MdkProjectType.Mod:
                {
                    console.Print("Warning: Mod projects are currently in beta. Please report any issues you encounter.");
                    var packer = new ModPacker();
                    var packed = await packer.PackAsync(parameters, console, interaction);

                    if (!packed.IsDefaultOrEmpty)
                        result.AddRange(packed);
                    break;
                }

                case MdkProjectType.ProgrammableBlock:
                {
                    var packer = new ScriptPacker();
                    var packed = await packer.PackAsync(parameters, console, interaction);

                    if (!packed.IsDefaultOrEmpty)
                        result.AddRange(packed);
                    break;
                }

                case MdkProjectType.LegacyProgrammableBlock:
                    console.Print($"The project file {project.Project.Name} is a legacy ingame script project and cannot be packed.");
                    break;

                case MdkProjectType.Unknown:
                    console.Print($"The project file {project.Project.Name} does not seem to be an MDK project.");
                    break;
            }
        }

        if (result.Count == 0)
            return null;
        return result.ToImmutable();
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
                {
                    console.Print("Warning: Mod projects are currently in beta. Please report any issues you encounter.");
                    console.Print($"MDK is restoring mod project: {project.Project.Name}");
                    var restorer = new ModRestorer();
                    await restorer.RestoreAsync(parameters, project, console, httpClient, interaction);
                    break;
                }

                case MdkProjectType.ProgrammableBlock:
                {
                    console.Print($"MDK is restoring ingame script project: {project.Project.Name}");
                    var restorer = new ScriptRestorer();
                    await restorer.RestoreAsync(parameters, project, console, httpClient, interaction);
                    break;
                }

                case MdkProjectType.LegacyProgrammableBlock:
                    console.Print($"MDK is converting legacy ingame script project: {project.Project.Name}");
                    var converter = new LegacyConverter();
                    await converter.ConvertAsync(parameters, project, console, httpClient);
                    goto case MdkProjectType.ProgrammableBlock;

                case MdkProjectType.Unknown:
                    console.Print($"The project file {project.Project.Name} does not seem to be an MDK project.");
                    break;
            }
        }
    }

    /// <summary>
    ///     A struct that contains all the peripherals required for the application to run.
    /// </summary>
    /// <param name="parameters"></param>
    /// <param name="console"></param>
    /// <param name="interaction"></param>
    /// <param name="httpClient"></param>
    /// <param name="exception"></param>
    public readonly struct Peripherals(Parameters? parameters, IConsole? console, IInteraction? interaction, IHttpClient? httpClient, Exception? exception)
    {
        /// <summary>
        ///     Start building a new set of peripherals.
        /// </summary>
        /// <returns></returns>
        public static Builder Create()
        {
            return new Builder();
        }

        readonly Parameters? _parameters = parameters;
        readonly IConsole? _console = console;
        readonly IInteraction? _interaction = interaction;
        readonly IHttpClient? _httpClient = httpClient;

        /// <summary>
        ///     The parsed parameters for the application.
        /// </summary>
        public Parameters Parameters => _parameters ?? throw new InvalidOperationException("Peripherals have not been built.");

        /// <summary>
        ///     An instance of the console to use for output.
        /// </summary>
        public IConsole Console => _console ?? throw new InvalidOperationException("Peripherals have not been built.");

        /// <summary>
        ///     An instance of the interaction service to use for user interaction, usually through proper UI.
        /// </summary>
        public IInteraction Interaction => _interaction ?? throw new InvalidOperationException("Peripherals have not been built.");

        /// <summary>
        ///     An instance of the HTTP client to use for web requests.
        /// </summary>
        public IHttpClient HttpClient => _httpClient ?? throw new InvalidOperationException("Peripherals have not been built.");

        /// <summary>
        ///     If an exception occurred during the creation of the peripherals, it will be stored here.
        /// </summary>
        public Exception? Exception { get; } = exception;

        public bool IsEmpty()
        {
            return _parameters == null && _console == null && _interaction == null && _httpClient == null && Exception == null;
        }

        /// <summary>
        ///     A builder for creating a new set of peripherals.
        /// </summary>
        /// <param name="parameters"></param>
        /// <param name="console"></param>
        /// <param name="interaction"></param>
        /// <param name="httpClient"></param>
        /// <param name="args"></param>
        public readonly struct Builder(Parameters parameters, IConsole console, IInteraction interaction, IHttpClient httpClient, string[] args)
        {
            readonly Parameters _parameters = parameters;
            readonly IConsole _console = console;
            readonly IInteraction _interaction = interaction;
            readonly IHttpClient _httpClient = httpClient;
            readonly string[] _args = args;

            /// <summary>
            ///     Create the peripherals from the specified arguments. If you set any of the peripherals manually, they will take
            ///     precedence over the arguments.
            /// </summary>
            /// <param name="args"></param>
            /// <returns></returns>
            public Builder FromArguments(string[] args)
            {
                return new Builder(_parameters, _console, _interaction, _httpClient, args);
            }

            /// <summary>
            ///     Use the specified parameters for the peripherals, regardless of the arguments in <see cref="FromArguments" />.
            /// </summary>
            /// <param name="parameters"></param>
            /// <returns></returns>
            public Builder WithParameters(Parameters parameters)
            {
                return new Builder(parameters, _console, _interaction, _httpClient, _args);
            }

            /// <summary>
            ///     Use the specified console for the peripherals, regardless of the arguments in <see cref="FromArguments" />.
            /// </summary>
            /// <param name="console"></param>
            /// <returns></returns>
            public Builder WithConsole(IConsole console)
            {
                return new Builder(_parameters, console, _interaction, _httpClient, _args);
            }

            /// <summary>
            ///     Use the specified interaction for the peripherals, regardless of the arguments in <see cref="FromArguments" />.
            /// </summary>
            /// <param name="interaction"></param>
            /// <returns></returns>
            public Builder WithInteraction(IInteraction interaction)
            {
                return new Builder(_parameters, _console, interaction, _httpClient, _args);
            }

            /// <summary>
            ///     Use the specified HTTP client for the peripherals, regardless of the arguments in <see cref="FromArguments" />.
            /// </summary>
            /// <param name="client"></param>
            /// <returns></returns>
            public Builder WithHttpClient(IHttpClient client)
            {
                return new Builder(_parameters, _console, _interaction, client, _args);
            }

            /// <summary>
            ///     Build the peripherals. Make sure you have set all the required peripherals before calling this method, either
            ///     via <see cref="FromArguments" />, or individually with each <c>With*</c> method. Once you call this method, you
            ///     should check the <see cref="Peripherals.Exception" /> property to see if the peripherals were successfully created.
            /// </summary>
            /// <returns></returns>
            public Peripherals Build()
            {
                var console = _console;
                var parameters = _parameters;
                var interaction = _interaction;
                var httpClient = _httpClient;
                if (parameters == null)
                {
                    parameters = new Parameters();
                    try
                    {
                        parameters.ParseAndLoadConfigs(_args);
                    }
                    catch (CommandLineException e)
                    {
                        return new Peripherals(null, CreateConsole(), null, null, e);
                    }
                }

                console ??= CreateConsole(parameters);
                interaction ??= new Interaction(console, parameters.Interactive);
                httpClient ??= new WebHttpClient();

                if (parameters == null)
                    throw new InvalidOperationException("The parameters must be set.");
                if (console == null)
                    throw new InvalidOperationException("The console must be set.");
                if (interaction == null)
                    throw new InvalidOperationException("The interaction must be set.");
                if (httpClient == null)
                    throw new InvalidOperationException("The HTTP client must be set.");
                return new Peripherals(parameters, console, interaction, httpClient, null);
            }
        }
    }
}