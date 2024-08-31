using System.Collections.Immutable;
using Mdk.CommandLine.IngameScript.Pack.Api;
using Mdk.CommandLine.SharedApi;

namespace Mdk.CommandLine.IngameScript.Pack;

/// <inheritdoc />
/// <param name="parameters"></param>
/// <param name="console"></param>
/// <param name="interaction"></param>
/// <param name="fileFilter"></param>
/// <param name="preprocessorSymbols"></param>
public class PackContext(IParameters parameters, IConsole console, IInteraction interaction, IFileFilter fileFilter, IFileSystem fileSystem, IImmutableSet<string> preprocessorSymbols)
    : IPackContext
{
    /// <inheritdoc />
    public IParameters Parameters { get; } = parameters;
    
    /// <inheritdoc />
    public IConsole Console { get; } = console;
    
    /// <inheritdoc />
    public IInteraction Interaction { get; } = interaction;
    
    /// <inheritdoc />
    public IFileFilter FileFilter { get; } = fileFilter;

    /// <inheritdoc />
    public IFileSystem FileSystem { get; } = fileSystem;

    /// <inheritdoc />
    public IImmutableSet<string> PreprocessorSymbols { get; } = preprocessorSymbols;
}

