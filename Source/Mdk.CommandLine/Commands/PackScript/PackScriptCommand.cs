using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Mdk.CommandLine.IngameScript;
using Mdk.CommandLine.SharedApi;

namespace Mdk.CommandLine.Commands.PackScript;

/// <summary>
///     A command to process and pack a script into a single code file that is compatible with the Space Engineers
///     programmable block.
/// </summary>
public class PackScriptCommand : Command
{
    public override async Task ExecuteAsync(List<string> arguments, IConsole console)
    {
        var options = ReadOptions(arguments);
        var packer = new ScriptPacker();
        await packer.PackAsync(options, console);
    }

    static PackOptions ReadOptions(List<string> arguments)
    {
        var minifier = MinifierLevel.None;
        var trimUnusedTypes = false;
        string? projectFile = null;
        string? output = null;
        bool toClipboard = false;

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
                default:
                    if (projectFile != null) throw new CommandLineException(-1, "Only one project file can be specified.");
                    projectFile = arg;
                    break;
            }
        }

        if (projectFile == null) throw new CommandLineException(-1, "No project file specified.");

        if (string.Equals(output, "auto", StringComparison.OrdinalIgnoreCase))
        {
            
        }
        
        return new PackOptions
        {
            MinifierLevel = minifier,
            TrimUnusedTypes = trimUnusedTypes,
            ProjectFile = projectFile,
            Output = output,
            ToClipboard = toClipboard
        };
    }

    public override void Help(IConsole console) =>
        console.Print("Usage: pack-script [options] <project-file>")
            .Print("  Processes and packs a script into a single code file that is compatible with the Space Engineers programmable block.")
            .Print()
            .Print("Options:")
            .Print("  -minifier <name>  Specifies the minifier to use. See below for valid names.")
            .Print("  -trim             Trims unused types from the output.")
            .Print()
            .Print("Minifier Levels:")
            .Print("  none             No minification.")
            .Print("  stripcomments    Removes comments but leaves everything else intact.")
            .Print("  lite             Removes comments and whitespace, but does not rename anything.")
            .Print("  full             Removes comments, whitespace, and renames everything.");
}