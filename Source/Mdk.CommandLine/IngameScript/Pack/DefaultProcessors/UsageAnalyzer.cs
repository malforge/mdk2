using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols;

namespace Mdk.CommandLine.IngameScript.Pack.DefaultProcessors;

class UsageAnalyzer
{
    readonly SymbolAnalyzer _symbolAnalyzer = new();

    public async Task<ImmutableArray<SymbolDefinitionInfo>> FindUsagesAsync(Document document)
    {
        var symbolDefinitions = await _symbolAnalyzer.FindSymbolsAsync(document);

        for (var index = 0; index < symbolDefinitions.Length; index++)
            symbolDefinitions[index] = await WithUsageDataAsync(symbolDefinitions[index], document);

        return symbolDefinitions.ToImmutableArray();
    }

    async Task<SymbolDefinitionInfo> WithUsageDataAsync(SymbolDefinitionInfo definition, Document document)
    {
        if (definition.Symbol == null)
            return definition;

        var references = (await SymbolFinder.FindReferencesAsync(definition.Symbol, document.Project.Solution))
            .ToImmutableArray();
        definition = definition.WithUsageData(references);

        // Check for extension class usage
        var symbol = definition.Symbol;
        if (symbol is { IsDefinition: true } and ITypeSymbol { TypeKind: TypeKind.Class, IsStatic: true, ContainingType: null } typeSymbol)
        {
            var members = typeSymbol.GetMembers().Where(m => m is IMethodSymbol { IsStatic: true, IsExtensionMethod: true }).ToArray();
            foreach (var member in members)
                references = references.AddRange(await SymbolFinder.FindReferencesAsync(member, document.Project.Solution));
            definition = definition.WithUsageData(references);
        }

        return definition;
    }
}