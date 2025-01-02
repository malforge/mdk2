using System.Collections.Generic;
using Mdk.CommandLine.CommandLine;
using Mdk.CommandLine.IngameScript.Pack;

namespace Mdk.CommandLine.Shared.Api;

/// <summary>
///     Parameters used to determine what to do and how to do it.
/// </summary>
public interface IParameters
{
    /// <summary>
    ///     A verb that determines what action to take.
    /// </summary>
    Verb Verb { get; }

    /// <summary>
    ///     An optional log file to write to.
    /// </summary>
    string? Log { get; }

    /// <summary>
    ///     Whether to enable trace logging.
    /// </summary>
    bool Trace { get; }

    /// <summary>
    ///     Whether to use interactive prompts through the external UI app - if available.
    /// </summary>
    bool Interactive { get; }

    /// <summary>
    ///     Detailed parameters for the help verb.
    /// </summary>
    IHelpVerbParameters HelpVerb { get; }

    /// <summary>
    ///     Detailed parameters for the pack verb.
    /// </summary>
    IPackVerbParameters PackVerb { get; }

    /// <summary>
    ///     Detailed parameters for the restore verb.
    /// </summary>
    IRestoreVerbParameters RestoreVerb { get; }

    /// <summary>
    ///     Parameters for the help verb.
    /// </summary>
    public interface IHelpVerbParameters
    {
        /// <summary>
        ///     What verb to display help for, or <see cref="Verb.None" /> to display general help.
        /// </summary>
        Verb Verb { get; }
    }

    /// <summary>
    ///     Parameters for the pack verb.
    /// </summary>
    public interface IPackVerbParameters
    {
        /// <summary>
        ///     The project file to pack.
        /// </summary>
        string? ProjectFile { get; }

        /// <summary>
        ///     An optional path to the game's bin directory.
        /// </summary>
        string? GameBin { get; }

        /// <summary>
        ///     An optional output folder to write to.
        /// </summary>
        string? Output { get; }
        
        /// <summary>
        ///    Whether to perform a dry run, which will not actually create the final output.
        /// </summary>
        bool DryRun { get; }

        /// <summary>
        ///     What level of minification to use, if any.
        /// </summary>
        MinifierLevel MinifierLevel { get; set; }
                
        /// <summary>
        /// What configuration to run the pack for.
        /// </summary>
        /// <remarks>
        /// This will affect removal of debug code.
        /// </remarks>
        string? Configuration { get; }

        /// <summary>
        ///     A list of paths to ignore when packing, in the form of a glob pattern (e.g. "bin/**/*").
        /// </summary>
        IReadOnlyList<string> Ignores { get; }
        
        /// <summary>
        /// A list of paths to not clean from the output directory when packing, in the form of a glob pattern (e.g. "bin/**/*").
        /// </summary>
        IReadOnlyList<string> DoNotClean { get; }
        
        /// <summary>
        ///     A dictionary of macros to use when packing.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Macros take the form of $NAME$ and are replaced with the value from the dictionary.
        ///     </para>
        ///     <para>
        ///         Defaults are provided for $MDK_DATETIME$, $MDK_DATE$ and $MDK_TIME$.
        ///     </para>
        /// </remarks>
        IReadOnlyDictionary<string, string> Macros { get; }
    }

    /// <summary>
    ///     Parameters for the restore verb.
    /// </summary>
    public interface IRestoreVerbParameters
    {
        /// <summary>
        ///     The project file to restore.
        /// </summary>
        string? ProjectFile { get; }

        /// <summary>
        ///     Whether to perform a dry run, which will not make actual changes to the project.
        /// </summary>
        bool DryRun { get; }
    }
}
