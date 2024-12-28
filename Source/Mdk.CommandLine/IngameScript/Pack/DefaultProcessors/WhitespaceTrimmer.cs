using System;
using System.Threading.Tasks;
using Mdk.CommandLine.IngameScript.Pack.Api;
using Mdk.CommandLine.Shared.Api;
using Microsoft.CodeAnalysis;

namespace Mdk.CommandLine.IngameScript.Pack.DefaultProcessors;

/// <summary>
///     A processor that removes whitespace from the script.
/// </summary>
[RunAfter<SymbolRenamer>]
public class WhitespaceTrimmer : IDocumentProcessor
{
    /// <inheritdoc />
    public async Task<Document> ProcessAsync(Document document, IPackContext context)
    {
        if (context.Parameters.PackVerb.MinifierLevel < MinifierLevel.Lite)
        {
            context.Console.Trace("Skipping whitespace trimming because the minifier level < Lite.");
            return document;
        }
        
        var root = await document.GetSyntaxRootAsync();
        var rewriter = new WhitespaceCompactor();
        var newRoot = rewriter.Visit(root) ?? throw new InvalidOperationException("Failed to rewrite the syntax tree.");
        
        var lineBreaker = new LineWrapper();
        newRoot = lineBreaker.Visit(newRoot) ?? throw new InvalidOperationException("Failed to rewrite the syntax tree.");
        
        document = document.WithSyntaxRoot(newRoot);
        return document;
    }
}