using System.Collections.Immutable;

namespace Mdk.CommandLine.Shared.Api;

/// <summary>
///     A shared context containing all the information required to pack a script.
/// </summary>
public interface IPackContext
{
    /// <summary>
    ///     The parameters that were passed to the program, by command line arguments or ini files.
    /// </summary>
    public IParameters Parameters { get; }

    /// <summary>
    ///     The console to be used for output.
    /// </summary>
    public IConsole Console { get; }

    /// <summary>
    ///     UI interaction service: Affected by the <see cref="IParameters.Interactive" /> flag - and what platform we are
    ///     running on.
    /// </summary>
    public IInteraction Interaction { get; }

    /// <summary>
    ///     A filter that can be used to determine which files should be included in the pack.
    /// </summary>
    /// <remarks>
    /// If a file or path passes this filter, it will be included in the pack.
    /// </remarks>
    public IFileFilter FileFilter { get; }

    /// <summary>
    ///     A filter that can be used to determine which files should be removed when cleaning the output directory.
    /// </summary>
    /// <remarks>
    /// If a file or path passes this filter, it will be removed from the output directory.
    /// </remarks>
    public IFileFilter OutputCleanFilter { get; }

    /// <summary>
    ///     Allows the writing of new files to the output.
    /// </summary>
    public IFileSystem FileSystem { get; }

    /// <summary>
    ///     Preprocessor symbols that should be defined when processing the script.
    /// </summary>
    public IImmutableSet<string> PreprocessorSymbols { get; }
}