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
    public async Task<Document> ProcessAsync(Document document, ScriptProjectMetadata metadata)
    {
        var syntaxTree = (CSharpSyntaxTree?)await document.GetSyntaxTreeAsync();
        if (syntaxTree == null)
            return document;

        while (true)
        {
            var editor = await DocumentEditor.CreateAsync(document);
            var root = editor.GetChangedRoot();
            var current = root.DescendantNodes().FirstOrDefault(t => t is TypeDeclarationSyntax { Modifiers: { } modifiers } && modifiers.Any(m => m.ValueText == "partial"));
            if (current == null)
                return document;
            var partialIdentifier = FullIdentifierOf((TypeDeclarationSyntax)current);
            var parts = root.DescendantNodes().OfType<TypeDeclarationSyntax>().Where(t => FullIdentifierOf(t) == partialIdentifier).ToList();
            if (parts.Count <= 1)
                continue;
            var allBaseLists = parts.SelectMany(t => t.ChildNodes().OfType<BaseListSyntax>()).ToList();
            var allModifiers = parts.SelectMany(t => t.Modifiers).Select(m => m.ValueText).Distinct().Where(m => m != "partial").ToList();

            var allSymbols = allBaseLists
                .SelectMany(b => b.Types)
                .Select(t => editor.SemanticModel.GetSymbolInfo(t.Type).Symbol)
                .OfType<ITypeSymbol>()
                .Distinct<ITypeSymbol>(SymbolEqualityComparer.Default)
                .OrderByDescending(s => s.TypeKind == TypeKind.Class)
                .ThenBy(s => s.Name)
                .ToList();

            var separatedSyntaxList = new SeparatedSyntaxList<BaseTypeSyntax>();
            var finalModifiers = SyntaxFactory.TokenList(allModifiers.Select(m => SyntaxFactory.ParseToken(m)));
            foreach (var symbol in allSymbols)
            {
                var typeName = SyntaxFactory.ParseTypeName(symbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));
                var baseType = SyntaxFactory.SimpleBaseType(typeName);

                separatedSyntaxList = separatedSyntaxList.Add(baseType);
            }

            var finalBaseList = SyntaxFactory.BaseList(separatedSyntaxList);
            switch (current)
            {
                case ClassDeclarationSyntax classDeclarationSyntax:
                {
                    var allMembers = parts.SelectMany(t => t.ChildNodes().OfType<MemberDeclarationSyntax>()).ToList();
                    var newType = classDeclarationSyntax;
                    if (finalBaseList.Types.Count > 0)
                        newType = newType.WithBaseList(finalBaseList);
                    newType = newType
                        .WithModifiers(finalModifiers)
                        .NormalizeWhitespace()
                        .WithMembers(SyntaxFactory.List(allMembers));

                    if (newType.GetTrailingTrivia().All(t => t.IsKind(SyntaxKind.EndOfLineTrivia)))
                        newType = newType.WithTrailingTrivia(newType.GetTrailingTrivia().Add(SyntaxFactory.EndOfLine(Environment.NewLine)));

                    editor.ReplaceNode(current, newType);
                    foreach (var node in parts.Skip(1))
                        editor.RemoveNode(node);

                    document = editor.GetChangedDocument();
                    break;
                }

                case InterfaceDeclarationSyntax interfaceDeclarationSyntax:
                {
                    var allMembers = parts.SelectMany(t => t.ChildNodes().OfType<MemberDeclarationSyntax>()).ToList();
                    var newType = interfaceDeclarationSyntax;
                    if (finalBaseList.Types.Count > 0)
                        newType = newType.WithBaseList(finalBaseList);
                    newType = newType
                        .WithModifiers(finalModifiers)
                        .NormalizeWhitespace()
                        .WithMembers(SyntaxFactory.List(allMembers));

                    editor.ReplaceNode(current, newType);
                    foreach (var node in parts.Skip(1))
                        editor.RemoveNode(node);

                    document = editor.GetChangedDocument();
                    break;
                }

                case StructDeclarationSyntax structDeclarationSyntax:
                {
                    var allMembers = parts.SelectMany(t => t.ChildNodes().OfType<MemberDeclarationSyntax>()).ToList();
                    var newType = structDeclarationSyntax;
                    if (finalBaseList.Types.Count > 0)
                        newType = newType.WithBaseList(finalBaseList);
                    newType = newType
                        .WithModifiers(finalModifiers)
                        .NormalizeWhitespace()
                        .WithMembers(SyntaxFactory.List(allMembers));

                    editor.ReplaceNode(current, newType);
                    foreach (var node in parts.Skip(1))
                        editor.RemoveNode(node);

                    document = editor.GetChangedDocument();
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