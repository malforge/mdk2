using System;
using System.Threading.Tasks;
using Mdk.CommandLine.IngameScript.Pack.Api;
using Microsoft.CodeAnalysis;

namespace Mdk.CommandLine.IngameScript.Pack.DefaultProcessors;

[RunAfter<PartialMerger>]
[RunAfter<RegionAnnotator>]
[RunAfter<TypeSorter>]
[RunAfter<SymbolProtectionAnnotator>]
public class TypeTrimmer : IScriptPostprocessor
{
    public async Task<Document> ProcessAsync(Document document, IPackContext context)
    {
        if (context.Parameters.PackVerb.MinifierLevel < MinifierLevel.Trim)
            return document;
        await Task.Yield();
        throw new NotImplementedException("TypeTrimmer is not implemented yet.");
    }
}