using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Mdk.CommandLine.IngameScript.Pack.Api;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;

namespace Mdk.CommandLine.IngameScript.Pack.DefaultProcessors;

/// <summary>
///     The TypeTrimmer processor removes unused types from the script.
/// </summary>
[RunAfter<PartialMerger>]
[RunAfter<RegionAnnotator>]
[RunAfter<TypeSorter>]
[RunAfter<SymbolProtectionAnnotator>]
public class TypeTrimmer : IScriptPostprocessor
{
    /// <inheritdoc />
    public async Task<Document> ProcessAsync(Document document, IPackContext context)
    {
        if (context.Parameters.PackVerb.MinifierLevel < MinifierLevel.Trim)
            return document;
        var analyzer = new UsageAnalyzer();
        var nodes = new Dictionary<ISymbol, Node>(SymbolEqualityComparer.Default);
        var symbolDefinitions = await analyzer.FindUsagesAsync(document);
        var symbolLookup = symbolDefinitions.GroupBy(d => d.Symbol, SymbolEqualityComparer.Default)
            .Where(g => g.Key != null)
            .ToDictionary(g => g.Key!, g => g.ToList(), SymbolEqualityComparer.Default);
        var rootNode = await document.GetSyntaxRootAsync();
        if (rootNode == null)
            return document;

        foreach (var definition in symbolDefinitions)
        {
            if (definition.Symbol is not ITypeSymbol typeSymbol)
                continue;
            if (typeSymbol.TypeKind == TypeKind.TypeParameter)
                continue;

            if (!nodes.TryGetValue(definition.Symbol, out var node))
                nodes[definition.Symbol] = node = new Node(definition);
            else
                node.Definitions.Add(definition);

            if (!definition.HasUsageData)
                continue;
            
            foreach (var usage in definition.Usage)
            {
                foreach (var location in usage.Locations)
                {
                    var enclosingSymbol = await FindTypeSymbolAsync(rootNode, location);
                    var enclosingSymbolDefinitions = symbolLookup[enclosingSymbol];
                    if (!nodes.TryGetValue(enclosingSymbol, out var referencingNode))
                        nodes[enclosingSymbol] = referencingNode = new Node(enclosingSymbolDefinitions);
                    if (node != referencingNode)
                        referencingNode.References.Add(node);
                }
            }
        }

        var program = symbolDefinitions.FirstOrDefault(d => d.FullName == "Program");
        if (program?.Symbol == null || !nodes.TryGetValue(program.Symbol, out var programNode))
            throw new InvalidOperationException("Cannot find entry point");

        var usedNodes = new List<Node>();
        var queue = new Queue<Node>();
        var visitedNodes = new HashSet<Node>();
        queue.Enqueue(programNode);
        while (queue.Count > 0)
        {
            var node = queue.Dequeue();
            if (!visitedNodes.Add(node))
                continue;
            usedNodes.Add(node);
            foreach (var reference in node.References)
                queue.Enqueue(reference);
        }

        var usedSymbolDefinitions = usedNodes.SelectMany(n => n.Definitions).ToImmutableHashSet();
        var unusedSymbolDefinitions = symbolDefinitions.Where(definition => IsEligibleForRemoval(definition) && !usedSymbolDefinitions.Contains(definition)).ToList();
        var nodesToRemove = unusedSymbolDefinitions.Select(definition => definition.FullName!).ToImmutableHashSet();

        var walker = new RemovalWalker(nodesToRemove);
        rootNode = walker.Visit(rootNode);
        foreach (var symbol in unusedSymbolDefinitions)
            rootNode = RemoveDefinition(rootNode, symbol);

        return document.WithSyntaxRoot(rootNode);
    }

    static SyntaxNode RemoveDefinition(SyntaxNode rootNode, SymbolDefinitionInfo symbol) => rootNode.RemoveNode(symbol.SyntaxNode, SyntaxRemoveOptions.KeepUnbalancedDirectives)!;

    static bool IsEligibleForRemoval(SymbolDefinitionInfo definition)
    {
        if (definition.IsProtected)
            return false;
        var symbol = definition.Symbol;
        if (!(symbol?.IsDefinition ?? false))
            return false;
        if (symbol is not ITypeSymbol typeSymbol)
            return false;
        if (typeSymbol.TypeKind == TypeKind.TypeParameter)
            return false;
        return true;
    }

    static ISymbol FindTypeSymbol(ISymbol symbol)
    {
        if (symbol is ITypeSymbol)
            return symbol;
        return symbol.ContainingType;
    }

    static async Task<ISymbol> FindTypeSymbolAsync(SyntaxNode rootNode, ReferenceLocation location)
    {
        var semanticModel = await location.Document.GetSemanticModelAsync();
        if (semanticModel == null)
            throw new InvalidOperationException("Failed to get semantic model for reference location.");
        var syntaxNode = rootNode.FindNode(location.Location.SourceSpan);
        var typeDeclarationNode = syntaxNode.AncestorsAndSelf().FirstOrDefault(node => node is TypeDeclarationSyntax);
        if (typeDeclarationNode != null)
        {
            var symbol = semanticModel.GetDeclaredSymbol(typeDeclarationNode);
            if (symbol != null)
                return symbol;
        }

        var enclosingSymbol = semanticModel.GetEnclosingSymbol(location.Location.SourceSpan.Start);
        if (enclosingSymbol == null)
            throw new InvalidOperationException("Failed to get enclosing symbol for reference location.");

        return FindTypeSymbol(enclosingSymbol);
    }

    class Node
    {
        public Node(SymbolDefinitionInfo definition)
        {
            Definitions = new HashSet<SymbolDefinitionInfo>
            {
                definition
            };
        }

        public Node(IEnumerable<SymbolDefinitionInfo> definitions)
        {
            Definitions = new HashSet<SymbolDefinitionInfo>(definitions);
        }

        public HashSet<SymbolDefinitionInfo> Definitions { get; }

        public HashSet<Node> References { get; } = [];

        public override string ToString() => Definitions.FirstOrDefault()?.FullName ?? "";
    }
}