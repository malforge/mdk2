using System;
using System.Threading.Tasks;
using Mdk.CommandLine.IngameScript.Pack.Api;
using Microsoft.CodeAnalysis;

namespace Mdk.CommandLine.IngameScript.Pack.DefaultProcessors;

[RunAfter<WhitespaceTrimmer>]
public class SymbolRenamer : IScriptPostprocessor
{
    public async Task<Document> ProcessAsync(Document document, IPackContext context)
    {
        if (context.Parameters.PackVerb.MinifierLevel < MinifierLevel.Full)
            return document;
        await Task.Yield();
        throw new NotImplementedException("SymbolRenamer is not implemented yet.");
    }
}