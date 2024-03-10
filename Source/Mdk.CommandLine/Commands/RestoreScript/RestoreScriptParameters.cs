using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Mdk.CommandLine.SharedApi;
using Mdk.CommandLine.Utility;

namespace Mdk.CommandLine.Commands.RestoreScript;

/// <summary>
/// The parameters for the restore-script command.
/// </summary>
public class RestoreScriptParameters : VerbParameters
{
    /// <summary>
    /// The project file to restore.
    /// </summary>
    public string? ProjectFile { get; set; }
    
    /// <summary>
    /// Whether to allow interactive prompts.
    /// </summary>
    public bool Interactive { get; set; }

    /// <inheritdoc />
    public override bool TryLoad(Queue<string> args, [MaybeNullWhen(true)] out string failureReason)
    {
        var p = new RestoreScriptParameters();
        while (args.Count > 0)
        {
            if (TryParseGlobalOptions(args, out failureReason))
                continue;

            if (args.TryDequeue("-interactive"))
            {
                p.Interactive = true;
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

    /// <inheritdoc />
    public override void Help(IConsole console) =>
        console.Print("Usage: mdk restore-script [options] <project-file>")
            .Print()
            .Print("Checks the script in the specified project file for compatibility with the current version of MDK, "
                   + "checks nuget packages for updates, etcetera.")
            .Print()
            .Print("Options:")
            .Print("  -interactive  Prompt for confirmation before restoring the script.")
            .Print("  -log <file>   Log to the specified file.")
            .Print("  -trace        Enable trace logging.")
            .Print()
            .Print("Example:")
            .Print("  mdk restore-script -interactive MyProject.csproj");

    /// <inheritdoc />
    public override Task ExecuteAsync(IConsole console)
    {
        throw new System.NotImplementedException();
    }
}