using System.Collections.Immutable;
using Mdk.CommandLine.IngameScript.Pack.Api;
using Mdk.CommandLine.SharedApi;

namespace Mdk.CommandLine.IngameScript.Pack;

public class PackContext(IParameters parameters, IConsole console, IInteraction interaction, IFileFilter fileFilter, IImmutableSet<string> preprocessorSymbols)
    : IPackContext
{
    public IParameters Parameters { get; } = parameters;
    public IConsole Console { get; } = console;
    public IInteraction Interaction { get; } = interaction;
    public IFileFilter FileFilter { get; } = fileFilter;
    public IImmutableSet<string> PreprocessorSymbols { get; } = preprocessorSymbols;
}