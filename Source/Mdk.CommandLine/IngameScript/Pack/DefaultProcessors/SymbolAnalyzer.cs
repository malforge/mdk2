using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace Mdk.CommandLine.IngameScript.Pack.DefaultProcessors;

class SymbolAnalyzer
{
    public HashSet<string> ProtectedSymbols { get; } =
    [
        "Program",
        "Program.Main",
        "Program.Save"
    ];

    public HashSet<string> ProtectedNames { get; } =
    [
        ".ctor",
        ".cctor",
        "Finalize"
    ];

    static bool IsNotSpecialDefinitions(SymbolDefinitionInfo s) => s.Symbol is { IsOverride: false } && !s.Symbol.IsInterfaceImplementation();

    public async Task<SymbolDefinitionInfo[]> FindSymbolsAsync(Document document)
    {
        var root = await document.GetSyntaxRootAsync();
        var semanticModel = await document.GetSemanticModelAsync();
        if (root == null || semanticModel == null)
            return Array.Empty<SymbolDefinitionInfo>();

        return root.DescendantNodes().Where(node => node.IsSymbolDeclaration())
            .Select(n => new SymbolDefinitionInfo(semanticModel.GetDeclaredSymbol(n), n))
            .Where(IsNotSpecialDefinitions)
            .Select(WithUpdatedProtectionFlag)
            .ToArray();
    }

    SymbolDefinitionInfo WithUpdatedProtectionFlag(SymbolDefinitionInfo d)
    {
        if (d.Symbol == null)
            return d;

        return ProtectedNames.Contains(d.Symbol.Name)
               || ProtectedSymbols.Contains(d.Symbol.GetFullName(DeclarationFullNameFlags.WithoutNamespaceName))
               || d.SyntaxNode.ShouldBePreserved()
            ? d.AsProtected()
            : d.AsUnprotected();
    }
}