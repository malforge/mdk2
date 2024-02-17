using System;
using System.Linq;
using System.Threading.Tasks;
using Mdk.CommandLine.IngameScript.Api;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Mdk.CommandLine.IngameScript.DefaultProcessors;

/// <summary>
/// Merges partial types into a single type.
/// </summary>
public class PartialMerger : IScriptPostprocessor
{
    public Task<CSharpSyntaxTree> ProcessAsync(CSharpSyntaxTree syntaxTree, ScriptProjectMetadata metadata)
    {
        // Find all partial types
        var partialTypes = syntaxTree.GetRoot().DescendantNodes().OfType<MemberDeclarationSyntax>().Where(t => t.Modifiers.Any(m => m.ValueText == "partial")).ToArray();
        // If there are no partial types, return the document as is
        if (partialTypes.Length == 0)
            return Task.FromResult(syntaxTree);

        var partialTypeGroups = partialTypes.GroupBy(FullIdentifierOf).Where(g => g.Count() > 1).ToList();

        var root = syntaxTree.GetRoot();
        foreach (var partialGroup in partialTypeGroups)
        {
            var allBaseLists = partialGroup.SelectMany(t => t.ChildNodes().OfType<BaseListSyntax>()).ToList();
            
            // var partialType = partialGroup.First();
            // var partialTypeMembers = partialType.ChildNodes().OfType<MemberDeclarationSyntax>().ToArray();
            // foreach (var otherPartialType in partialGroup.Skip(1))
            // {
            //     var otherPartialTypeMembers = otherPartialType.ChildNodes().OfType<MemberDeclarationSyntax>().ToArray();
            //     foreach (var member in otherPartialTypeMembers)
            //     {
            //         if (partialTypeMembers.Any(m => m is MethodDeclarationSyntax method && method.Identifier.ValueText == member.Identifier.ValueText))
            //             continue;
            //         partialType = partialType.AddMembers(member);
            //     }
            // }
            //
            // document = document.WithRoot(document.GetRoot().ReplaceNode(partialType, partialType));
        }

        return Task.FromResult(syntaxTree);
    }

    private static string FullIdentifierOf(MemberDeclarationSyntax memberDeclarationSyntax)
    {
        var parent = memberDeclarationSyntax.Parent;
        while (parent != null)
        {
            if (parent is NamespaceDeclarationSyntax namespaceDeclarationSyntax)
                return $"{namespaceDeclarationSyntax.Name}.{IdentifierOf(memberDeclarationSyntax)}";
            parent = parent.Parent;
        }

        return IdentifierOf(memberDeclarationSyntax);
    }

    private static string IdentifierOf(MemberDeclarationSyntax memberDeclarationSyntax)
    {
        switch (memberDeclarationSyntax)
        {
            case ClassDeclarationSyntax classDeclarationSyntax:
                return classDeclarationSyntax.Identifier.ValueText;
            case InterfaceDeclarationSyntax interfaceDeclarationSyntax:
                return interfaceDeclarationSyntax.Identifier.ValueText;
            case StructDeclarationSyntax structDeclarationSyntax:
                return structDeclarationSyntax.Identifier.ValueText;
            default:
                throw new NotSupportedException($"The type {memberDeclarationSyntax.GetType().Name} is not supported.");
        }
    }
}