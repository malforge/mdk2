using System.Collections.Generic;
using System.Threading.Tasks;
using Mdk.CommandLine.Shared.Api;
using Microsoft.CodeAnalysis;

namespace Mdk.CommandLine.IngameScript.Pack.Api;

/// <summary>
///     A combiner takes multiple code files and weaves them into a single syntax tree, in preparation
///     for creating the script file a Programmable Block can use.
/// </summary>
public interface IScriptCombiner
{
    /// <summary>
    ///     Combines the specified documents into a single syntax tree.
    /// </summary>
    /// <param name="project">The project the documents are part of.</param>
    /// <param name="documents">The documents to process.</param>
    /// <param name="context">The context for the pack command, containing parameters and services useful for the combiner.</param>
    /// <returns></returns>
    Task<Document> CombineAsync(Project project, IReadOnlyList<Document> documents, IPackContext context);
}