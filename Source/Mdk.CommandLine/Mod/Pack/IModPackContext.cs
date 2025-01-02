using System.Collections.Immutable;
using Mdk.CommandLine.Shared.Api;
using Microsoft.CodeAnalysis;

namespace Mdk.CommandLine.Mod.Pack;

/// <summary>
///     An extended pack context specifically for mod packing.
/// </summary>
internal interface IModPackContext : IPackContext
{
    /// <summary>
    ///     The project being packed.
    /// </summary>
    Project Project { get; }

    /// <summary>
    ///     Script documents in the project.
    /// </summary>
    ImmutableArray<Document> ScriptDocuments { get; }

    /// <summary>
    ///     Content documents in the project.
    /// </summary>
    ImmutableArray<TextDocument> ContentDocuments { get; }

    /// <summary>
    ///     The processors to apply to the code documents.
    /// </summary>
    ProcessorSet<IDocumentProcessor> Processors { get; }
}