using System;
using System.Collections.Immutable;
using System.Linq;
using Mdk.CommandLine.Shared.Api;
using Microsoft.CodeAnalysis;

namespace Mdk.CommandLine.Mod.Pack;

internal class ModPackContext(IParameters parameters, IConsole console, IInteraction interaction, IFileFilter fileFilter, IFileFilter outputCleanFilter, IFileSystem fileSystem, IImmutableSet<string> preprocessorSymbols, Project? project, ImmutableArray<Document> scriptDocuments, ImmutableArray<TextDocument> contentDocuments, ProcessorSet<IDocumentProcessor>? processors)
    : IPackContext
{
    readonly Project? _project = project;
    readonly ProcessorSet<IDocumentProcessor>? _processors = processors;
    readonly ImmutableArray<Document> _scriptDocuments = scriptDocuments;
    readonly ImmutableArray<TextDocument> _contentDocuments = contentDocuments;

    public IParameters Parameters { get; } = parameters;
    public IConsole Console { get; } = console;
    public IInteraction Interaction { get; } = interaction;
    public IFileFilter FileFilter { get; } = fileFilter;
    public IFileFilter OutputCleanFilter { get; } = outputCleanFilter;
    public IFileSystem FileSystem { get; } = fileSystem;
    public IImmutableSet<string> PreprocessorSymbols { get; } = preprocessorSymbols;
    public Project Project => _project ?? throw new InvalidOperationException("The project has not been set.");
    public ImmutableArray<Document> ScriptDocuments => !_scriptDocuments.IsDefault? _scriptDocuments : throw new InvalidOperationException("The script documents have not been set.");
    public ImmutableArray<TextDocument> ContentDocuments => _contentDocuments.IsDefault ? ImmutableArray<TextDocument>.Empty : _contentDocuments;
    public ProcessorSet<IDocumentProcessor> Processors => _processors ?? throw new InvalidOperationException("The processors have not been set.");

    public ModPackContext WithProject(Project project) => new ModPackContext(Parameters, Console, Interaction, FileFilter, OutputCleanFilter, FileSystem, PreprocessorSymbols, project, _scriptDocuments, _contentDocuments, _processors);
    public ModPackContext WithScriptDocuments(ImmutableArray<Document> scriptDocuments) => new ModPackContext(Parameters, Console, Interaction, FileFilter, OutputCleanFilter, FileSystem, PreprocessorSymbols, _project, scriptDocuments, _contentDocuments, _processors);
    public ModPackContext WithContentDocuments(ImmutableArray<TextDocument> contentDocuments) => new ModPackContext(Parameters, Console, Interaction, FileFilter, OutputCleanFilter, FileSystem, PreprocessorSymbols, _project, _scriptDocuments, contentDocuments, _processors);
    public ModPackContext WithProcessors(ProcessorSet<IDocumentProcessor> processors) => new ModPackContext(Parameters, Console, Interaction, FileFilter, OutputCleanFilter, FileSystem, PreprocessorSymbols, _project, _scriptDocuments, _contentDocuments, processors);
    
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

// /// <inheritdoc />
// internal class ModPackContext(IPackContext innerContext, Project project, ImmutableArray<Document> codeDocuments, ImmutableArray<TextDocument> contentDocuments, ProcessorSet<IDocumentProcessor> processors) : IPackContext
// {
//     /// <summary>
//     ///     The project being packed.
//     /// </summary>
//     public Project Project { get; } = project;
//
//     /// <summary>
//     ///     Script documents in the project.
//     /// </summary>
//     public ImmutableArray<Document> ScriptDocuments { get; } = codeDocuments;
//
//     /// <summary>
//     ///     Content documents in the project.
//     /// </summary>
//     public ImmutableArray<TextDocument> ContentDocuments { get; } = contentDocuments;
//
//     /// <summary>
//     ///     The processors to apply to the code documents.
//     /// </summary>
//     public ProcessorSet<IDocumentProcessor> Processors { get; } = processors;
//
//     /// <inheritdoc />
//     public IParameters Parameters => innerContext.Parameters;
//
//     /// <inheritdoc />
//     public IConsole Console => innerContext.Console;
//
//     /// <inheritdoc />
//     public IInteraction Interaction => innerContext.Interaction;
//
//     /// <inheritdoc />
//     public IFileFilter FileFilter => innerContext.FileFilter;
//
//     /// <inheritdoc />
//     public IFileFilter OutputCleanFilter => innerContext.OutputCleanFilter;
//
//     /// <inheritdoc />
//     public IFileSystem FileSystem => innerContext.FileSystem;
//
//     /// <inheritdoc />
//     public IImmutableSet<string> PreprocessorSymbols => innerContext.PreprocessorSymbols;
//
//     /// <summary>
//     ///     Trace the context to the console (if tracing is enabled).
//     /// </summary>
//     public void Trace()
//     {
//         Console.Trace("There are:")
//             .Trace($"  {ScriptDocuments.Length} documents")
//             .TraceIf(ContentDocuments.Length > 0, $"  {ContentDocuments.Length} content documents")
//             .Trace($"  {Processors.Count} processors")
//             .TraceIf(Processors.Count > 0, $"    {string.Join("\n    ", Processors.Select(p => p.GetType().Name))}");
//     }
//
//     public ModPackContext WithProject(Project project1)
//     {
//         throw new System.NotImplementedException();
//     }
// }