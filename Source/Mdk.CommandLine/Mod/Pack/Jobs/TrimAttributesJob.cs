using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Mdk.CommandLine.CommandLine;
using Mdk.CommandLine.Shared.AttributeTrimming;
using Microsoft.CodeAnalysis;

namespace Mdk.CommandLine.Mod.Pack.Jobs;

internal sealed class TrimAttributesJob : ModJob
{
    public override async Task<ModPackContext> ExecuteAsync(ModPackContext context)
    {
        var processor = new AttributeTrimmingProcessor();
        var result = await processor.ProcessWithResultAsync(context.Project, context);
        ThrowIfFailed(result.Diagnostics, context);
        return context.WithProject(result.Project);
    }

    static void ThrowIfFailed(ImmutableArray<AttributeTrimmingDiagnostic> diagnostics, ModPackContext context)
    {
        var errors = diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToImmutableArray();
        if (errors.Length == 0)
            return;

        foreach (var diagnostic in errors)
            context.Console.Print(diagnostic.ToString());

        throw new CommandLineException(-2, "Attribute trimming failed.");
    }
}
