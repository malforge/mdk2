using System;
using System.Threading.Tasks;
using Mdk.CommandLine.IngameScript.Pack.Api;
using Microsoft.CodeAnalysis;

namespace Mdk.CommandLine.IngameScript.Pack.DefaultProcessors;

/// <summary>
///     A processor that renames symbols in the script to single or double character names, in order to reduce the script's
///     size.
/// </summary>
[RunAfter<WhitespaceTrimmer>]
public class SymbolRenamer : IScriptPostprocessor
{
    /// <inheritdoc />
    public async Task<Document> ProcessAsync(Document document, IPackContext context)
    {
        if (context.Parameters.PackVerb.MinifierLevel < MinifierLevel.Full)
        {
            context.Console.Trace("Skipping symbol renaming because the minifier level < Full.");
            return document;
        }
        await Task.Yield();
        throw new NotImplementedException("SymbolRenamer is not implemented yet.");
    }
}