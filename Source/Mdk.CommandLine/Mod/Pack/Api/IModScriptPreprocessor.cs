﻿using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace Mdk.CommandLine.Mod.Pack.Api;

/// <summary>
///     A processor that works on the syntax tree of the individual code files before they are combined.
/// </summary>
public interface IModScriptPreprocessor
{
    /// <summary>
    ///     Processes the syntax tree of an individual code file before it is combined.
    /// </summary>
    /// <param name="document">The document to process.</param>
    /// <param name="context">The context for the pack command, containing parameters and services useful for the processor.</param>
    /// <returns></returns>
    Task<Document> ProcessAsync(Document document, IPackContext context);
}