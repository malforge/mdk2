using System.Collections.Immutable;
using System.Linq;
using Mdk.CommandLine.Shared.Api;
using Microsoft.CodeAnalysis;

namespace Mdk.CommandLine.Mod.Pack;

/// <inheritdoc />
internal class ModPackContext(IPackContext innerContext, Project project, ImmutableArray<Document> codeDocuments, ImmutableArray<TextDocument> contentDocuments, ProcessorSet<IDocumentProcessor> processors) : IModPackContext
{
    /// <inheritdoc />
    public Project Project { get; } = project;

    /// <inheritdoc />
    public ImmutableArray<Document> ScriptDocuments { get; } = codeDocuments;

    /// <inheritdoc />
    public ImmutableArray<TextDocument> ContentDocuments { get; } = contentDocuments;

    /// <inheritdoc />
    public ProcessorSet<IDocumentProcessor> Processors { get; } = processors;

    /// <inheritdoc />
    public IParameters Parameters => innerContext.Parameters;

    /// <inheritdoc />
    public IConsole Console => innerContext.Console;

    /// <inheritdoc />
    public IInteraction Interaction => innerContext.Interaction;

    /// <inheritdoc />
    public IFileFilter FileFilter => innerContext.FileFilter;

    /// <inheritdoc />
    public IFileFilter OutputCleanFilter => innerContext.OutputCleanFilter;

    /// <inheritdoc />
    public IFileSystem FileSystem => innerContext.FileSystem;

    /// <inheritdoc />
    public IImmutableSet<string> PreprocessorSymbols => innerContext.PreprocessorSymbols;

    /// <summary>
    ///     Trace the context to the console (if tracing is enabled).
    /// </summary>
    public void Trace()
    {
        Console.Trace("There are:")
            .Trace($"  {ScriptDocuments.Length} documents")
            .TraceIf(ContentDocuments.Length > 0, $"  {ContentDocuments.Length} content documents")
            .Trace($"  {Processors.Count} processors")
            .TraceIf(Processors.Count > 0, $"    {string.Join("\n    ", Processors.Select(p => p.GetType().Name))}");
    }
}