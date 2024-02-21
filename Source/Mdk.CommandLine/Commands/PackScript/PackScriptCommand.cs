using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Mdk.CommandLine.IngameScript;
using Mdk.CommandLine.IngameScript.DefaultProcessors;
using Mdk.CommandLine.SharedApi;

namespace Mdk.CommandLine.Commands.PackScript;

/// <summary>
///     A command to process and pack a script into a single code file that is compatible with the Space Engineers
///     programmable block.
/// </summary>
public class PackScriptCommand : Command
{
    /// <inheritdoc />
    public override async Task ExecuteAsync(List<string> arguments, IConsole console)
    {
        var options = ReadOptions(arguments);
        if (options.ListProcessors)
        {
            var managerBuilder = ProcessingManager.Create();
            console.Print("- Default processors -");
            console.Print("Preprocessors:");
            if (managerBuilder.Preprocessors.Length == 0)
                console.Print("  (none)");
            else
            {
                foreach (var preprocessor in managerBuilder.Preprocessors)
                    console.Print($"  {preprocessor.Name}");
            }
            console.Print("Combiner:");
            console.Print($"  {managerBuilder.Combiner?.Name}");
            console.Print("Postprocessors:");
            if (managerBuilder.Postprocessors.Length == 0)
                console.Print("  (none)");
            else
            {
                foreach (var postprocessor in managerBuilder.Postprocessors)
                    console.Print($"  {postprocessor.Name}");
            }
            console.Print("Composer:");
            console.Print($"  {managerBuilder.Composer?.Name}");
            console.Print("Post-composition processors:");
            if (managerBuilder.PostCompositionProcessors.Length == 0)
                console.Print("  (none)");
            else
            {
                foreach (var postCompositionProcessor in managerBuilder.PostCompositionProcessors)
                    console.Print($"  {postCompositionProcessor.Name}");
            }
            console.Print("Producer:");
            console.Print($"  {managerBuilder.Producer?.Name}");
            return;
        }

        var packer = new ScriptPacker();
        await packer.PackAsync(options, console);
    }

    static PackOptions ReadOptions(List<string> arguments)
    {
        var minifier = MinifierLevel.None;
        var trimUnusedTypes = false;
        string? projectFile = null;
        string? output = null;
        var toClipboard = false;
        var listProcessors = false;

        while (arguments.Count > 0)
        {
            var arg = arguments.Dequeue();
            switch (arg)
            {
                case "-minifier":
                    if (!arguments.TryDequeue(out var minifierName)) throw new CommandLineException(-1, "No minifier specified.");
                    if (!Enum.TryParse(minifierName, true, out minifier)) throw new CommandLineException(-1, $"Unknown minifier: {minifierName}");
                    break;
                case "-trim":
                    trimUnusedTypes = true;
                    break;
                case "-toclipboard":
                    toClipboard = true;
                    break;
                case "-output":
                    if (!arguments.TryDequeue(out var outputValue)) throw new CommandLineException(-1, "No output file specified.");
                    if (output != null) throw new CommandLineException(-1, "Only one output path can be specified.");
                    output = outputValue;
                    break;
                case "-listprocessors":
                    listProcessors = true;
                    break;
                default:
                    if (projectFile != null) throw new CommandLineException(-1, "Only one project file can be specified.");
                    projectFile = arg;
                    break;
            }
        }

        if (projectFile == null && !listProcessors) throw new CommandLineException(-1, "No project file specified.");

        return new PackOptions
        {
            MinifierLevel = minifier,
            TrimUnusedTypes = trimUnusedTypes,
            ProjectFile = projectFile,
            Output = output,
            ToClipboard = toClipboard,
            ListProcessors = listProcessors
        };
    }

    public override void Help(IConsole console) =>
        console.Print("Usage: pack-script [options] <project-file>")
            .Print("  Processes and packs a script into a single code file that is compatible with the Space Engineers programmable block.")
            .Print()
            .Print("Options:")
            .Print("  -minifier <name>  Specifies the minifier to use. See below for valid names.")
            .Print("  -trim             Trims unused types from the output.")
            .Print("  -toclipboard      Copies the output to the clipboard instead of writing to a file.")
            .Print("  -output <folder>  Specifies the output folder. If not specified, the output will be written to the project directory.")
            .Print("  -listprocessors   Lists all default processors and their descriptions.")
            .Print()
            .Print("Minifier Levels:")
            .Print("  none             No minification.")
            .Print("  stripcomments    Removes comments but leaves everything else intact.")
            .Print("  lite             Removes comments and whitespace, but does not rename anything.")
            .Print("  full             Removes comments, whitespace, and renames everything.");
}