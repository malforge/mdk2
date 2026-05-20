using System;
using System.Collections.Immutable;
using Mdk.CommandLine.Shared.Api;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Mdk.CommandLine.Shared;

/// <inheritdoc />
/// <param name="parameters"></param>
/// <param name="console"></param>
/// <param name="interaction"></param>
/// <param name="fileFilter"></param>
/// <param name="preprocessorSymbols"></param>
public class PackContext(IParameters parameters, IConsole console, IInteraction interaction, IFileFilter? fileFilter, IFileFilter? outputCleanFilter, IFileSystem fileSystem, IImmutableSet<string> preprocessorSymbols)
    : IPackContext
{
    /// <inheritdoc />
    public IParameters Parameters { get; } = parameters;
    
    /// <inheritdoc />
    public IConsole Console { get; } = console;
    
    /// <inheritdoc />
    public IInteraction Interaction { get; } = interaction;
    
    /// <inheritdoc />
    public IFileFilter FileFilter { get; } = fileFilter ?? Shared.FileFilter.Passthrough;

    /// <inheritdoc />
    public IFileFilter OutputCleanFilter { get; } = outputCleanFilter ?? Shared.FileFilter.Passthrough;

    /// <inheritdoc />
    public IFileSystem FileSystem { get; } = fileSystem;

    /// <inheritdoc />
    public IImmutableSet<string> PreprocessorSymbols { get; } = preprocessorSymbols;

    /// <summary>
    ///     Builds the set of preprocessor symbols (`#if` symbols) for a project from the C# parse options
    ///     produced by MSBuild — these come from the resolved <c>DefineConstants</c> for the active
    ///     configuration, not from the configuration name itself.
    /// </summary>
    public static IImmutableSet<string> ResolvePreprocessorSymbols(Project project)
    {
        var builder = ImmutableHashSet.CreateBuilder<string>(StringComparer.OrdinalIgnoreCase);
        if (project.ParseOptions is CSharpParseOptions csharpParseOptions)
        {
            foreach (var symbol in csharpParseOptions.PreprocessorSymbolNames)
            {
                if (!string.IsNullOrEmpty(symbol))
                    builder.Add(symbol);
            }
        }
        return builder.ToImmutable();
    }
}

