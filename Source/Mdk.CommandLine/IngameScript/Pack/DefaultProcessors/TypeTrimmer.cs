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
        var shouldTrimMembers = (context.Parameters.PackVerb.MinifierExtraOptions & MinifierExtraOptions.NoMemberTrimming) == 0;
        
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
            var unusedMembers = new List<SyntaxNode>();
            var fieldReplacements = new Dictionary<FieldDeclarationSyntax, FieldDeclarationSyntax>();
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

            var allFieldDeclarations = rootNode.DescendantNodes().OfType<FieldDeclarationSyntax>();
            foreach (var fieldDeclaration in allFieldDeclarations)
            {
                if (fieldDeclaration.ShouldBePreserved())
                    continue;
                var declaration = fieldDeclaration.Declaration;
                if (declaration == null)
                    continue;
                var variables = declaration.Variables;
                if (variables.Count == 0)
                    continue;

                var keptVariables = new List<VariableDeclaratorSyntax>(variables.Count);
                var removedAny = false;
                foreach (var variable in variables)
                {
                    if (variable.Identifier.ShouldBePreserved())
                    {
                        keptVariables.Add(variable);
                        continue;
                    }
                    if (semanticModel.GetDeclaredSymbol(variable) is not IFieldSymbol fieldSymbol)
                    {
                        keptVariables.Add(variable);
                        continue;
                    }
                    if (!IsEligibleForRemoval(variable, fieldSymbol))
                    {
                        keptVariables.Add(variable);
                        continue;
                    }
                    var references = (await SymbolFinder.FindReferencesAsync(fieldSymbol, solution)).ToList();
                    references.RemoveAll(reference => !reference.Locations.Any());
                    references.RemoveAll(reference => reference.Locations.All(location => IsSelfReferencingMember(rootNode, variable, location)));

                    if (references.Count > 0)
                    {
                        keptVariables.Add(variable);
                        continue;
                    }

                    removedAny = true;
                    context.Console.Trace($"Unused field: {fieldSymbol.Name}");
                }

                if (!removedAny)
                    continue;

                if (keptVariables.Count == 0)
                {
                    unusedMembers.Add(fieldDeclaration);
                    continue;
                }

                var newVariables = default(SeparatedSyntaxList<VariableDeclaratorSyntax>);
                newVariables = newVariables.AddRange(keptVariables);
                var newDeclaration = fieldDeclaration.WithDeclaration(declaration.WithVariables(newVariables));
                fieldReplacements[fieldDeclaration] = newDeclaration;
            }

            var allMethodDeclarations = rootNode.DescendantNodes().OfType<MethodDeclarationSyntax>();
            foreach (var methodDeclaration in allMethodDeclarations)
            {
                if (methodDeclaration.Identifier.ShouldBePreserved() || methodDeclaration.ShouldBePreserved())
                    continue;
                if (semanticModel.GetDeclaredSymbol(methodDeclaration) is not IMethodSymbol methodSymbol)
                    continue;
                if (!IsEligibleForRemoval(methodSymbol))
                    continue;

                var references = (await SymbolFinder.FindReferencesAsync(methodSymbol, solution)).ToList();
                references.RemoveAll(reference => !reference.Locations.Any());
                references.RemoveAll(reference => reference.Locations.All(location => IsSelfReferencingMember(rootNode, methodDeclaration, location)));

                if (references.Count > 0)
                    continue;

                unusedMembers.Add(methodDeclaration);
                context.Console.Trace($"Unused method: {methodSymbol.Name}");
            }

            var allConstructorDeclarations = rootNode.DescendantNodes().OfType<ConstructorDeclarationSyntax>();
            foreach (var constructorDeclaration in allConstructorDeclarations)
            {
                if (constructorDeclaration.ShouldBePreserved())
                    continue;
                if (semanticModel.GetDeclaredSymbol(constructorDeclaration) is not IMethodSymbol ctorSymbol)
                    continue;
                if (!IsEligibleForRemoval(ctorSymbol))
                    continue;

                var references = (await SymbolFinder.FindReferencesAsync(ctorSymbol, solution)).ToList();
                references.RemoveAll(reference => !reference.Locations.Any());
                references.RemoveAll(reference => reference.Locations.All(location => IsSelfReferencingMember(rootNode, constructorDeclaration, location)));

                if (references.Count > 0)
                    continue;

                unusedMembers.Add(constructorDeclaration);
                context.Console.Trace($"Unused constructor: {ctorSymbol.ContainingType.Name}({string.Join(", ", ctorSymbol.Parameters.Select(p => p.Type.Name))})");
            }
            
            if (unusedTypes.Count == 0 && unusedMembers.Count == 0 && fieldReplacements.Count == 0)
                break;
            
            var trackedNodes = unusedTypes.Cast<SyntaxNode>().Concat(unusedMembers).Concat(fieldReplacements.Keys).ToArray();
            var trackedRoot = rootNode.TrackNodes(trackedNodes);

            if (fieldReplacements.Count > 0)
            {
                trackedRoot = trackedRoot.ReplaceNodes(fieldReplacements.Keys, (original, _) => fieldReplacements[original]);
            }

            if (unusedTypes.Count > 0 || unusedMembers.Count > 0)
            {
                var nodesToRemove = unusedTypes.Cast<SyntaxNode>()
                    .Concat(unusedMembers)
                    .Select(node => trackedRoot.GetCurrentNode(node))
                    .Where(node => node != null)
                    .Select(node => node!)
                    .ToArray();
                trackedRoot = trackedRoot.RemoveNodes(nodesToRemove, SyntaxRemoveOptions.KeepNoTrivia);
            }

            rootNode = trackedRoot;
            if (rootNode == null)
                throw new InvalidOperationException("Failed to remove unused types.");
            document = document.WithSyntaxRoot(rootNode);
        }
        return document;
    }

    static bool IsEligibleForRemoval(TypeDeclarationSyntax typeDeclaration, INamedTypeSymbol symbol)
    {
        if (symbol.GetFullName(DeclarationFullNameFlags.WithoutNamespaceName) == "Program"
            || typeDeclaration.Identifier.ShouldBePreserved()
            || !symbol.IsDefinition
            || symbol.TypeKind == TypeKind.TypeParameter)
            return false;
        return true;
    }
    
    static bool IsSelfReferencingType(SyntaxNode root, TypeDeclarationSyntax typeDeclaration, ReferenceLocation referenceLocation)
    {
        var referenceNode = root.FindNode(referenceLocation.Location.SourceSpan);
        var referenceTypeDeclaration = referenceNode.Ancestors().OfType<TypeDeclarationSyntax>().FirstOrDefault();
        return referenceTypeDeclaration == typeDeclaration;
    }

    static bool IsSelfReferencingMember(SyntaxNode root, SyntaxNode declarationNode, ReferenceLocation referenceLocation)
    {
        var referenceNode = root.FindNode(referenceLocation.Location.SourceSpan);
        return referenceNode.AncestorsAndSelf().Any(node => node == declarationNode);
    }

    static bool IsEligibleForRemoval(VariableDeclaratorSyntax variableDeclarator, IFieldSymbol symbol)
    {
        if (variableDeclarator.ShouldBePreserved()
            || !symbol.IsDefinition)
            return false;
        return true;
    }

    static bool IsEligibleForRemoval(IMethodSymbol symbol)
    {
        if (!symbol.IsDefinition
            || symbol.IsOverride
            || symbol.IsInterfaceImplementation()
            || IsScriptMainCallback(symbol)
            || symbol.MethodKind == MethodKind.StaticConstructor)
            return false;
        return true;
    }

    static bool IsScriptMainCallback(IMethodSymbol symbol)
    {
        if (!string.Equals(symbol.Name, "Main", StringComparison.Ordinal))
            return false;
        var containingType = symbol.ContainingType;
        if (containingType == null)
            return false;
        return InheritsFrom(containingType, "MyGridProgram");
    }

    static bool InheritsFrom(INamedTypeSymbol typeSymbol, string baseTypeName)
    {
        var current = typeSymbol.BaseType;
        while (current != null)
        {
            if (string.Equals(current.Name, baseTypeName, StringComparison.Ordinal))
                return true;
            current = current.BaseType;
        }
        return false;
    }
}
