using System;
using System.Linq;
using System.Threading.Tasks;
using Mdk.CommandLine.IngameScript.Api;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

namespace Mdk.CommandLine.IngameScript.DefaultProcessors;

/// <summary>
///     Merges partial types into a single type.
/// </summary>
public class PartialMerger : IScriptPostprocessor
{
    public async Task<(CSharpSyntaxTree, CSharpCompilation)> ProcessAsync(CSharpCompilation compilation, CSharpSyntaxTree syntaxTree, ScriptProjectMetadata metadata)
    {
        while (true)
        {
            var root = await syntaxTree.GetRootAsync();
            var current = root.DescendantNodes().FirstOrDefault(t => t is TypeDeclarationSyntax { Modifiers: { } modifiers } && modifiers.Any(m => m.ValueText == "partial"));
            if (current == null)
                return (syntaxTree, compilation);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var partialIdentifier = FullIdentifierOf((TypeDeclarationSyntax)current);
            var parts = root.DescendantNodes().OfType<TypeDeclarationSyntax>().Where(t => FullIdentifierOf(t) == partialIdentifier).ToList();
            if (parts.Count <= 1)
                continue;
            var allBaseLists = parts.SelectMany(t => t.ChildNodes().OfType<BaseListSyntax>()).ToList();
            
            var allSymbols = allBaseLists
                .SelectMany(b => b.Types)
                .Select(t => semanticModel.GetSymbolInfo(t.Type).Symbol)
                .OfType<ITypeSymbol>()
                .Distinct<ITypeSymbol>(SymbolEqualityComparer.Default)
                .OrderByDescending(s => s.TypeKind == TypeKind.Class)
                .ThenBy(s => s.Name)
                .ToList();

            // Step 3: Create new BaseListSyntax with the final result
            var separatedSyntaxList = new SeparatedSyntaxList<BaseTypeSyntax>();
            foreach (var symbol in allSymbols)
            {
                // Assuming fully qualified names for simplicity; adjust as needed for your context
                var typeName = SyntaxFactory.ParseTypeName(symbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));
                var baseType = SyntaxFactory.SimpleBaseType(typeName);

                separatedSyntaxList = separatedSyntaxList.Add(baseType);
            }

            // Create the new BaseListSyntax
            var finalBaseList = SyntaxFactory.BaseList(separatedSyntaxList);
            switch (current)
            {
                // case ClassDeclarationSyntax when classes.Count > 1:
                //     throw new InvalidOperationException("Cannot derive a single class from multiple partial classes.");

                case ClassDeclarationSyntax classDeclarationSyntax:
                {
                    var allMembers = parts.SelectMany(t => t.ChildNodes().OfType<MemberDeclarationSyntax>()).ToList();
                    var newType = classDeclarationSyntax
                        .WithBaseList(finalBaseList).NormalizeWhitespace()
                        .WithModifiers(SyntaxFactory.TokenList(classDeclarationSyntax.Modifiers.Where(m => m.ValueText != "partial")))
                        .WithMembers(SyntaxFactory.List(allMembers));

                    
                    var newRoot = root.TrackNodes(parts);
                        
                    current = newRoot.GetCurrentNode(current)!;
                    parts = parts.Select(p => newRoot.GetCurrentNode(p)).ToList();
                    
                    newRoot = newRoot.ReplaceNode(current, newType)
                        .RemoveNodes(parts.Skip(1), SyntaxRemoveOptions.KeepNoTrivia)!;
                    
                    // current = newRoot.GetCurrentNode(current)!;
                    // newRoot = newRoot.ReplaceNode(current, newType);
                    
                    // var newRoot = root.ReplaceNode(current, newType)
                    //     .RemoveNodes(parts, SyntaxRemoveOptions.KeepNoTrivia)!;
                    var newSyntaxTree = (CSharpSyntaxTree)syntaxTree.WithRootAndOptions(newRoot, syntaxTree.Options);
                    compilation = compilation.ReplaceSyntaxTree(syntaxTree, newSyntaxTree);
                    syntaxTree = newSyntaxTree;
                    break;
                }

                case InterfaceDeclarationSyntax interfaceDeclarationSyntax:
                {
                    var allMembers = parts.SelectMany(t => t.ChildNodes().OfType<MemberDeclarationSyntax>()).ToList();

                    var newType = interfaceDeclarationSyntax
                        .WithBaseList(finalBaseList)
                        .WithModifiers(SyntaxFactory.TokenList(interfaceDeclarationSyntax.Modifiers.Where(m => m.ValueText != "partial")))
                        .WithMembers(SyntaxFactory.List(allMembers));

                    var newRoot = root.ReplaceNode(current, newType)
                        .RemoveNodes(parts, SyntaxRemoveOptions.KeepNoTrivia)!;
                    var newSyntaxTree = (CSharpSyntaxTree)syntaxTree.WithRootAndOptions(newRoot, syntaxTree.Options);
                    compilation = compilation.ReplaceSyntaxTree(syntaxTree, newSyntaxTree);
                    syntaxTree = newSyntaxTree;
                    break;
                }

                case StructDeclarationSyntax structDeclarationSyntax:
                {
                    var allMembers = parts.SelectMany(t => t.ChildNodes().OfType<MemberDeclarationSyntax>()).ToList();

                    var newType = structDeclarationSyntax
                        .WithBaseList(finalBaseList)
                        .WithModifiers(SyntaxFactory.TokenList(structDeclarationSyntax.Modifiers.Where(m => m.ValueText != "partial")))
                        .WithMembers(SyntaxFactory.List(allMembers));

                    var newRoot = root.ReplaceNode(current, newType)
                        .RemoveNodes(parts, SyntaxRemoveOptions.KeepNoTrivia)!;
                    var newSyntaxTree = (CSharpSyntaxTree)syntaxTree.WithRootAndOptions(newRoot, syntaxTree.Options);
                    compilation = compilation.ReplaceSyntaxTree(syntaxTree, newSyntaxTree);
                    syntaxTree = newSyntaxTree;
                    break;
                }

                default:
                    throw new NotSupportedException($"The type {current.GetType().Name} is not supported.");
            }
        }
    }

    static string FullIdentifierOf(TypeDeclarationSyntax typeDeclarationSyntax)
    {
        var parent = typeDeclarationSyntax.Parent;
        while (parent != null)
        {
            if (parent is NamespaceDeclarationSyntax namespaceDeclarationSyntax)
                return $"{namespaceDeclarationSyntax.Name}.{IdentifierOf(typeDeclarationSyntax)}";
            parent = parent.Parent;
        }

        return IdentifierOf(typeDeclarationSyntax);
    }

    static string IdentifierOf(TypeDeclarationSyntax typeDeclarationSyntax)
    {
        switch (typeDeclarationSyntax)
        {
            case ClassDeclarationSyntax classDeclarationSyntax:
                return classDeclarationSyntax.Identifier.ValueText;
            case InterfaceDeclarationSyntax interfaceDeclarationSyntax:
                return interfaceDeclarationSyntax.Identifier.ValueText;
            case StructDeclarationSyntax structDeclarationSyntax:
                return structDeclarationSyntax.Identifier.ValueText;
            default:
                throw new NotSupportedException($"The type {typeDeclarationSyntax.GetType().Name} is not supported.");
        }
    }
}