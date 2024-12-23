using System.Collections.Immutable;
using Mdk.CommandLine.SharedApi;

namespace Mdk.CommandLine.Mod.Pack.Api;

/// <summary>
///     A shared context containing all the information required to pack a script.
/// </summary>
public interface IPackContext
{
    /// <summary>
    ///     The parameters that were passed to the program, by command line arguments or ini files.
    /// </summary>
    IParameters Parameters { get; }

    /// <summary>
    ///     The console to be used for output.
    /// </summary>
    IConsole Console { get; }

    /// <summary>
    ///     UI interaction service: Affected by the <see cref="IParameters.Interactive" /> flag - and what platform we are
    ///     running on.
    /// </summary>
    IInteraction Interaction { get; }

    /// <summary>
    ///     A filter that can be used to determine which files should be included in the pack.
    /// </summary>
    IFileFilter FileFilter { get; }

    /// <summary>
    ///     Allows the writing of new files to the output.
    /// </summary>
    IFileSystem FileSystem { get; }

    /// <summary>
    ///     Preprocessor symbols that should be defined when processing the script.
    /// </summary>
    IImmutableSet<string> PreprocessorSymbols { get; }
}