using System.Collections.Immutable;
using Mdk.CommandLine.IngameScript.Pack.Api;
using Mdk.CommandLine.SharedApi;

namespace Mdk.CommandLine.IngameScript.Pack;

/// <summary>
/// A shared context containing all the information required to pack a script.
/// </summary>
/// <param name="parameters"></param>
/// <param name="console"></param>
/// <param name="interaction"></param>
/// <param name="fileFilter"></param>
/// <param name="preprocessorSymbols"></param>
public class PackContext(IParameters parameters, IConsole console, IInteraction interaction, IFileFilter fileFilter, IImmutableSet<string> preprocessorSymbols)
    : IPackContext
{
    /// <summary>
    /// The parameters that were passed to the program, by command line arguments or ini files.
    /// </summary>
    public IParameters Parameters { get; } = parameters;
    
    /// <summary>
    /// The console to be used for output.
    /// </summary>
    public IConsole Console { get; } = console;
    
    /// <summary>
    /// UI interaction service: Affected by the <see cref="IParameters.Interactive"/> flag - and what platform we are running on.
    /// </summary>
    public IInteraction Interaction { get; } = interaction;
    
    /// <summary>
    /// A filter that can be used to determine which files should be included in the pack.
    /// </summary>
    public IFileFilter FileFilter { get; } = fileFilter;
    
    /// <summary>
    /// Preprocessor symbols that should be defined when processing the script.
    /// </summary>
    public IImmutableSet<string> PreprocessorSymbols { get; } = preprocessorSymbols;
}