using System;
using System.Collections.Generic;
using Mdk.CommandLine.IngameScript.Pack;
using Mdk.CommandLine.SharedApi;
using Mdk.CommandLine.Utility;

namespace Mdk.CommandLine.CommandLine;

/// <summary>
///     The default implementation of <see cref="IParameters" />.
/// </summary>
public class Parameters : IParameters
{
    /// <summary>
    ///     Detailed parameters for the help verb.
    /// </summary>
    public HelpVerbParameters HelpVerb { get; } = new();

    /// <summary>
    ///     Detailed parameters for the pack verb.
    /// </summary>
    public PackVerbParameters PackVerb { get; } = new();

    /// <summary>
    ///     Detailed parameters for the restore verb.
    /// </summary>
    public RestoreVerbParameters RestoreVerb { get; } = new();

    /// <inheritdoc />
    public Verb Verb { get; set; }

    /// <inheritdoc />
    public string? Log { get; set; }

    /// <inheritdoc />
    public bool Trace { get; set; }

    /// <inheritdoc />
    public bool Interactive { get; set; }
    
    IParameters.IHelpVerbParameters IParameters.HelpVerb => HelpVerb;
    IParameters.IPackVerbParameters IParameters.PackVerb => PackVerb;
    IParameters.IRestoreVerbParameters IParameters.RestoreVerb => RestoreVerb;

    static string Unescape(string value) => value.Replace("&quot;", "\"");

    /// <summary>
    ///     Parses parameters from the provided list of command line arguments.
    /// </summary>
    /// <param name="args"></param>
    /// <exception cref="CommandLineException"></exception>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public void Parse(string[] args)
    {
        var queue = new Queue<string>(args);
        var verb = Verb.None;
        while (queue.TryDequeue(out var arg))
        {
            bool matches(string what)
            {
                return string.Equals(arg, what, StringComparison.OrdinalIgnoreCase);
            }

            if (matches("-log"))
            {
                if (!queue.TryDequeue(out var log))
                    throw new CommandLineException(-1, "No log file specified.");
                Log = log;
                continue;
            }

            if (matches("-trace"))
            {
                Trace = true;
                continue;
            }

            if (matches("-interactive"))
            {
                Interactive = true;
                continue;
            }

            switch (verb)
            {
                case Verb.None:
                    if (arg.StartsWith('-'))
                        throw new CommandLineException(-1, $"Unknown option '{arg}', or option specified before verb.");
                    if (!Enum.TryParse(arg, true, out verb))
                        throw new CommandLineException(-1, $"Unknown verb '{arg}'.");
                    Verb = verb;
                    break;

                case Verb.Help:
                    if (arg.StartsWith('-'))
                        throw new CommandLineException(-1, $"Unknown option '{arg}' for verb '{verb}'.");
                    if (HelpVerb.Verb != Verb.None)
                        throw new CommandLineException(-1, "Only one verb can be specified.");
                    if (!Enum.TryParse(arg, true, out verb))
                        throw new CommandLineException(-1, $"Unknown verb '{arg}'.");
                    HelpVerb.Verb = verb;
                    break;

                case Verb.Pack:
                    if (matches("-minifier"))
                    {
                        if (!queue.TryDequeue(out MinifierLevel level))
                            throw new CommandLineException(-1, "No or unknown minifier specified.");
                        PackVerb.MinifierLevel = level;
                        continue;
                    }
                    if (matches("-output"))
                    {
                        if (!queue.TryDequeue(out var output))
                            throw new CommandLineException(-1, "No output file specified.");
                        PackVerb.Output = string.Equals(output, "auto") ? null : output;
                        continue;
                    }
                    if (matches("-gamebin"))
                    {
                        if (!queue.TryDequeue(out var gameBin))
                            throw new CommandLineException(-1, "No game bin folder specified.");
                        PackVerb.GameBin = gameBin;
                        continue;
                    }
                    if (matches("-trim"))
                    {
                        PackVerb.TrimUnusedTypes = true;
                        continue;
                    }
                    if (matches("-ignore"))
                    {
                        if (!queue.TryDequeue(out var ignore))
                            throw new CommandLineException(-1, "No ignore specified.");
                        var ignores = ignore.Split(';');
                        PackVerb.Ignores.AddRange(ignores);
                        continue;
                    }
                    if (matches("-configuration"))
                    {
                        if (!queue.TryDequeue(out var configuration))
                            throw new CommandLineException(-1, "No configuration specified.");
                        PackVerb.Configuration = configuration;
                        continue;
                    }
                    if (matches("-macro"))
                    {
                        if (!queue.TryDequeue(out var macro))
                            throw new CommandLineException(-1, "No macro specified.");
                        var equalsIndex = macro.IndexOf('=');
                        if (equalsIndex == -1)
                            throw new CommandLineException(-1, "Invalid macro format.");
                        var name = macro[..equalsIndex].Trim();
                        if (string.IsNullOrWhiteSpace(name))
                            throw new CommandLineException(-1, "Invalid macro name.");
                        var value = macro[(equalsIndex + 1)..].Trim();
                        if (value.StartsWith('"') && value.EndsWith('"'))
                            value = Unescape(value[1..^1]);
                        PackVerb.Macros[name] = value;
                        continue;
                    }
                    if (arg.StartsWith('-'))
                        throw new CommandLineException(-1, $"Unknown option '{arg}' for verb '{verb}'.");
                    if (PackVerb.ProjectFile != null)
                        throw new CommandLineException(-1, "Only one project file can be specified.");
                    PackVerb.ProjectFile = arg;
                    break;

                case Verb.Restore:
                    if (matches("-dry-run"))
                    {
                        RestoreVerb.DryRun = true;
                        continue;
                    }
                    if (arg.StartsWith('-'))
                        throw new CommandLineException(-1, $"Unknown option '{arg}' for verb '{verb}'.");
                    if (RestoreVerb.ProjectFile != null)
                        throw new CommandLineException(-1, "Only one project file can be specified.");
                    RestoreVerb.ProjectFile = arg;
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        if (verb == Verb.None)
            throw new CommandLineException(-1, "No verb specified.");

        switch (verb)
        {
            case Verb.Pack:
                if (PackVerb.ProjectFile == null)
                    throw new CommandLineException(-1, "No project file specified.");
                break;

            case Verb.Restore:
                if (RestoreVerb.ProjectFile == null)
                    throw new CommandLineException(-1, "No project file specified.");
                break;
        }
    }

    /// <summary>
    ///     Attempts to load the parameters from the specified arguments.
    /// </summary>
    /// <remarks>
    ///     Will only overwrite values that are specified in the ini file, allowing for partial configuration.
    /// </remarks>
    /// <param name="ini"></param>
    public void Load(Ini ini)
    {
        var section = ini["mdk"];
        if (section.HasKey("log"))
            Log = section["log"].ToString();
        if (section.HasKey("trace"))
            Trace = section["trace"].ToBool();
        if (section.HasKey("interactive"))
            Interactive = section["interactive"].ToBool();
        if (section.HasKey("game-bin"))
            PackVerb.GameBin = section["game-bin"].ToString();
        if (section.HasKey("output"))
            PackVerb.Output = section["output"].ToString();
        if (section.HasKey("minify"))
            PackVerb.MinifierLevel = section["minifier"].ToEnum<MinifierLevel>();
        if (section.HasKey("trim"))
            PackVerb.TrimUnusedTypes = section["trim"].ToBool();
        if (section.HasKey("ignores"))
        {
            var ignores = section["ignores"].ToString()?.Split(';');
            if (ignores is { Length: > 0 })
                PackVerb.Ignores.AddRange(ignores);
        }
    }

    /// <summary>
    ///     Show help as defined by the parameters.
    /// </summary>
    /// <param name="console"></param>
    public void ShowHelp(IConsole console)
    {
        var displayVersion = typeof(Parameters).Assembly.GetName().Version?.ToString() ?? "0.0.0";
        console.Print($"MDK v{displayVersion}")
            .Print();
        switch (HelpVerb.Verb)
        {
            case Verb.Help:
                switch (HelpVerb.Verb)
                {
                    case Verb.Pack:
                        ShowHelpAboutPack(console);
                        break;
                    case Verb.Restore:
                        ShowHelpAboutRestore(console);
                        break;
                    default:
                        ShowGeneralHelp(console);
                        break;
                }
                break;
            case Verb.Pack:
                break;
            case Verb.Restore:
                break;
            default:
                ShowGeneralHelp(console);
                break;
        }
    }

    void ShowHelpAboutRestore(IConsole console) =>
        console.Print("Usage: mdk restore <project-file> [options]")
            .Print()
            .Print("Checks the script in the specified project file for compatibility with the current version of MDK, "
                   + "checks nuget packages for updates, etcetera.")
            .Print()
            .Print("Options:")
            .Print("  -interactive  Prompt for confirmation before restoring the script.")
            .Print("  -log <file>   Log to the specified file.")
            .Print("  -trace        Enable trace logging.")
            .Print("  -dry-run      Don't actually restore the script, just print what would be done.")
            .Print()
            .Print("Example:")
            .Print("  mdk restore MyProject.csproj -interactive");

    void ShowHelpAboutPack(IConsole console) =>
        console.Print("Usage: mdk pack <project-file> [options]")
            .Print("Packs a script or mod project into a workshop-ready package.")
            .Print()
            .Print("Options (for script projects):")
            .Print("  -minifier <level>  Set the minifier level.")
            .Print("                      - none, strip-comments, lite, full")
            .Print("  -trim              Trim unused types.")
            .Print("  -gamebin <path>    Path to the game's bin folder.")
            .Print("                      \"auto\" to auto-detect the bin folder from Steam (default).")
            .Print("  -output <path>     Write the output to the specified folder.")
            .Print("                      \"auto\" to auto-detect the output folder from Steam (default).")
            .Print("  -ignore <paths>    A semi-colon separated list of paths to ignore when packing (globbing "
                   + "format, eg. 'obj/**/*' ignores all files and folders in the `obj` folder.).")
            .Print("  -macro <name=value> Define a macro to be replaced in the script. Can be used multiple times.")
            .Print("  -interactive       Prompt for confirmation before packing the script.")
            .Print("  -log <file>        Log to the specified file.")
            .Print("  -trace             Enable trace logging.")
            .Print()
            .Print("Options (for mod projects):")
            .Print("  To be determined: Mod packing is pending implementation.")
            .Print()
            .Print("Example:")
            .Print("  mdk pack /path/to/project.csproj -minifier full -output auto");

    void ShowGeneralHelp(IConsole console) =>
        console.Print("Usage: mdk [options] <verb> [verb-options]")
            .Print()
            .Print("Options:")
            .Print("  -log <file>  Log output to the specified file.")
            .Print("  -trace       Enable trace output.")
            .Print("  -interactive Prompt for confirmation before executing the verb.")
            .Print()
            .Print("Verbs:")
            .Print("  help [verb]  Display help for a verb.")
            .Print("  pack         Pack a project into a single script.")
            .Print("  restore      Restore a project.")
            .Print("  version      Display the version of MDK.")
            .Print()
            .Print("Use 'mdk help <verb>' for more information on a verb.");

    /// <summary>
    ///     Dumps trace information to the console.
    /// </summary>
    /// <param name="console"></param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public void DumpTrace(IConsole console)
    {
        console.Trace($"> Verb: {Verb}")
            .TraceIf(Log != null, "> Log: {Log}")
            .TraceIf(Trace, "> Trace")
            .TraceIf(Interactive, "> Interactive");
        switch (Verb)
        {
            case Verb.Help:
                console.Trace($"> Help.Verb: {HelpVerb.Verb}");
                break;
            case Verb.Pack:
                console.Trace($"> Pack.ProjectFile: {PackVerb.ProjectFile}")
                    .TraceIf(PackVerb.GameBin != null, $"> Pack.GameBin: {PackVerb.GameBin}")
                    .TraceIf(PackVerb.Output != null, $"> Pack.Output: {PackVerb.Output}")
                    .TraceIf(PackVerb.MinifierLevel != MinifierLevel.None, $"> Pack.MinifierLevel: {PackVerb.MinifierLevel}")
                    .TraceIf(PackVerb.TrimUnusedTypes, "> Pack.TrimUnusedTypes")
                    .TraceIf(PackVerb.Ignores.Count > 0, $"> Pack.Ignores: {string.Join(", ", PackVerb.Ignores)}")
                    .TraceIf(PackVerb.Macros.Count > 0, $"> Pack.Macros: {string.Join(", ", PackVerb.Macros)}")
                    .TraceIf(PackVerb.Configuration != "Release", $"> Pack.Configuration: {PackVerb.Configuration}");
                break;
            case Verb.Restore:
                console.Trace($"> Restore.ProjectFile: {RestoreVerb.ProjectFile}")
                    .TraceIf(RestoreVerb.DryRun, "> Restore.DryRun");
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    /// <summary>
    ///     Default implementation of <see cref="IParameters.IHelpVerbParameters" />.
    /// </summary>
    public class HelpVerbParameters : IParameters.IHelpVerbParameters
    {
        /// <inheritdoc />
        public Verb Verb { get; set; }
    }

    /// <summary>
    ///     Default implementation of <see cref="IParameters.IPackVerbParameters" />.
    /// </summary>
    public class PackVerbParameters : IParameters.IPackVerbParameters
    {
        /// <inheritdoc cref="IParameters.IPackVerbParameters.Ignores" />
        public List<string> Ignores { get; } = new();

        /// <inheritdoc cref="IParameters.IPackVerbParameters.Macros" />
        public Dictionary<string, string> Macros { get; } = new(StringComparer.OrdinalIgnoreCase);

        /// <inheritdoc />
        public string? ProjectFile { get; set; }

        /// <inheritdoc />
        public string? GameBin { get; set; }

        /// <inheritdoc />
        public string? Output { get; set; }

        /// <inheritdoc />
        public MinifierLevel MinifierLevel { get; set; }

        /// <inheritdoc />
        public bool TrimUnusedTypes { get; set; }

        /// <inheritdoc />
        public string? Configuration { get; set; } = "Release";

        /// <inheritdoc />
        IReadOnlyList<string> IParameters.IPackVerbParameters.Ignores => Ignores;

        /// <inheritdoc />
        IReadOnlyDictionary<string, string> IParameters.IPackVerbParameters.Macros => Macros;
    }

    /// <summary>
    ///     Default implementation of <see cref="IParameters.IRestoreVerbParameters" />.
    /// </summary>
    public class RestoreVerbParameters : IParameters.IRestoreVerbParameters
    {
        /// <inheritdoc />
        public string? ProjectFile { get; set; }

        /// <inheritdoc />
        public bool DryRun { get; set; }
    }
}