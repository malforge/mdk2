using System.Threading.Tasks;
using Mdk.CommandLine.IngameScript.Pack.Api;
using Mdk.CommandLine.SharedApi;
using Microsoft.CodeAnalysis;

namespace Mdk.CommandLine.IngameScript.Pack.DefaultProcessors;

/// <summary>
///     A processor that removes comments from the script.
/// </summary>
[RunAfter<TypeTrimmer>]
public class CommentStripper : IScriptPostprocessor
{
    /// <inheritdoc />
    public async Task<Document> ProcessAsync(Document document, IPackContext context)
    {
        if (context.Parameters.PackVerb.MinifierLevel < MinifierLevel.StripComments)
        {
            context.Console.Trace("Skipping comment stripping because the minifier level < StripComments.");
            return document;
        }
        var simplifier = new CommentStrippingRewriter();
        return await simplifier.ProcessAsync(document);
    }
}