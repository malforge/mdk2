using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Mdk.CommandLine.IngameScript.Pack.Api;
using Mdk.CommandLine.Shared.Api;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;

namespace Mdk.CommandLine.IngameScript.Pack.DefaultProcessors;

/// <summary>
///     The TypeTrimmer processor removes unused types from the script.
/// </summary>
[RunAfter<CodeSmallifier>]
public class TypeTrimmer : IDocumentProcessor
{
    public async Task<Document> ProcessAsync(Document document, IPackContext context)
    {
        while (true)
        {
            var rootNode = await document.GetSyntaxRootAsync();
            if (rootNode == null)
                return document;
            var semanticModel = await document.GetSemanticModelAsync();
            if (semanticModel == null)
                return document;
            var solution = document.Project.Solution;
            
            var allTypeDeclarations = rootNode.DescendantNodes().OfType<TypeDeclarationSyntax>();
            var unusedTypes = new List<TypeDeclarationSyntax>();
            foreach (var typeDeclaration in allTypeDeclarations)
            {
                if (semanticModel.GetDeclaredSymbol(typeDeclaration) is not INamedTypeSymbol symbol)
                    continue;
                if (!IsEligibleForRemoval(typeDeclaration, symbol))
                    continue;
                var references = (await SymbolFinder.FindReferencesAsync(symbol, solution)).ToList();
                
                // Check for extension class usage
                if (symbol is { IsDefinition: true } and ITypeSymbol { TypeKind: TypeKind.Class, IsStatic: true, ContainingType: null } typeSymbol)
                {
                    var members = typeSymbol.GetMembers().Where(m => m is IMethodSymbol { IsStatic: true, IsExtensionMethod: true }).ToArray();
                    foreach (var member in members)
                        references.AddRange(await SymbolFinder.FindReferencesAsync(member, document.Project.Solution));
                }
                
                references.RemoveAll(reference => !reference.Locations.Any());
                references.RemoveAll(reference => reference.Locations.All(location => IsSelfReferencingType(rootNode, typeDeclaration, location)));
                
                if (references.Count > 0)
                    continue;
                
                unusedTypes.Add(typeDeclaration);
                context.Console.Trace($"Unused type: {symbol.Name}");
            }
            
            if (unusedTypes.Count == 0)
                break;
            
            rootNode = rootNode.RemoveNodes(unusedTypes, SyntaxRemoveOptions.KeepNoTrivia);
            if (rootNode == null)
                throw new InvalidOperationException("Failed to remove unused types.");
            document = document.WithSyntaxRoot(rootNode);
        }
        return document;
    }

    static bool IsEligibleForRemoval(TypeDeclarationSyntax typeDeclaration, INamedTypeSymbol symbol)
    {
        if (symbol.GetFullName(DeclarationFullNameFlags.WithoutNamespaceName) == "Program")
            return false;
        if (typeDeclaration.Identifier.ShouldBePreserved())
            return false;
        if (!symbol.IsDefinition)
            return false;
        if (symbol.TypeKind == TypeKind.TypeParameter)
            return false;
        return true;
    }
    
    static bool IsSelfReferencingType(SyntaxNode root, TypeDeclarationSyntax typeDeclaration, ReferenceLocation referenceLocation)
    {
        var referenceNode = root.FindNode(referenceLocation.Location.SourceSpan);
        var referenceTypeDeclaration = referenceNode.Ancestors().OfType<TypeDeclarationSyntax>().FirstOrDefault();
        return referenceTypeDeclaration == typeDeclaration;
    }
}