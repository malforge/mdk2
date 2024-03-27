using System.Collections.Generic;
using System.Collections.Immutable;
using Mdk.CommandLine.SharedApi;

namespace Mdk.CommandLine.IngameScript.Pack.Api;

public interface IPackContext
{
    IParameters Parameters { get; }
    IConsole Console { get; }
    IInteraction Interaction { get; }
    IFileFilter FileFilter { get; }
    IImmutableSet<string> PreprocessorSymbols { get; }
}