using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Mdk.CommandLine.IngameScript;
using Mdk.CommandLine.SharedApi;
using Mdk.CommandLine.Utility;

namespace Mdk.CommandLine.Commands.PackScript;

/// <summary>
/// The parameters for the pack-script command.
/// </summary>
public class PackScriptParameters : VerbParameters
{
    /// <summary>
    /// The project file to pack.
    /// </summary>
    public string? ProjectFile { get; set; }
    
    /// <summary>
    /// An optional output folder to write to. May be "auto" to auto-detect the output folder from Steam.
    /// </summary>
    public string? Output { get; set; }
    
    /// <summary>
    /// The minifier level to use, if any.
    /// </summary>
    public MinifierLevel MinifierLevel { get; set; }
    
    /// <summary>
    /// Whether to trim away unused types.
    /// </summary>
    public bool TrimUnusedTypes { get; set; }
    
    /// <summary>
    /// Whether to allow interactive prompts.
    /// </summary>
    public bool Interactive { get; set; }

    /// <inheritdoc />
    public override bool TryLoad(Queue<string> args, [MaybeNullWhen(true)] out string failureReason)
    {
        var p = new PackScriptParameters();
        while (args.Count > 0)
        {
            if (TryParseGlobalOptions(args, out failureReason))
                continue;

            if (args.TryDequeue("-minifier"))
            {
                if (!args.TryDequeue(out MinifierLevel level))
                {
                    failureReason = "No or unknown minifier specified.";
                    return false;
                }
                p.MinifierLevel = level;
                continue;
            }

            if (args.TryDequeue("-trim"))
            {
                p.TrimUnusedTypes = true;
                continue;
            }
            if (args.TryDequeue("-interactive"))
            {
                p.Interactive = true;
                continue;
            }
            if (args.TryDequeue("-output"))
            {
                if (!args.TryDequeue(out var output))
                {
                    failureReason = "No output file specified.";
                    return false;
                }
                p.Output = string.Equals(output, "auto")? null : output;
                continue;
            }
            
            if (p.ProjectFile is not null)
            {
                failureReason = "Only one project file can be specified.";
                return false;
            }
            
            p.ProjectFile = args.Dequeue();
        }

        if (p.ProjectFile is null)
        {
            failureReason = "No project file specified.";
            return false;
        }

        failureReason = null;
        return true;
    }

    /// <summary>
    /// Displays help for the pack-script command.
    /// </summary>
    /// <param name="console"></param>
    public override void Help(IConsole console) =>
        console.Print("Usage: mdk pack-script [options] <project-file>")
            .Print()
            .Print("Options:")
            .Print("  -minifier <level>  Set the minifier level.")
            .Print("                      - none, strip-comments, lite, full")
            .Print("  -trim              Trim unused types.")
            .Print("  -interactive       Run the minifier in interactive mode.")
            .Print("  -output <path>     Write the output to the specified folder.")
            .Print()
            .Print("The project file is the path to the script file to pack.")
            .Print()
            .Print("Example:")
            .Print("  mdk pack-script -minifier full -output /path/to/output /path/to/project.csproj");

    /// <inheritdoc />
    public override async Task ExecuteAsync(IConsole console)
    {
        var packer = new ScriptPacker();
        await packer.PackAsync(this, console);
    }
}