using System;
using System.Threading.Tasks;
using Mdk.CommandLine.IngameScript.Pack.Api;
using Microsoft.CodeAnalysis;

namespace Mdk.CommandLine.IngameScript.Pack.DefaultProcessors;

/// <summary>
///     A processor that removes whitespace from the script.
/// </summary>
[RunAfter<CommentStripper>]
public class WhitespaceTrimmer : IScriptPostprocessor
{
    /// <inheritdoc />
    public async Task<Document> ProcessAsync(Document document, IPackContext context)
    {
        if (context.Parameters.PackVerb.MinifierLevel < MinifierLevel.Lite)
        {
            context.Console.Trace("Skipping whitespace trimming because the minifier level < Lite.");
            return document;
        }
        await Task.Yield();
        throw new NotImplementedException("Whitespace Trimmer is not implemented yet.");
    }
}