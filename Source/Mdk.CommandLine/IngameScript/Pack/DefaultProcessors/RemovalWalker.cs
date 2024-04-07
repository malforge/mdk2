using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Mdk.CommandLine.IngameScript.Pack.DefaultProcessors;

class RemovalWalker(ImmutableHashSet<string> nodesToRemove) : CSharpSyntaxRewriter
{
    readonly ImmutableHashSet<string> _nodesToRemove = nodesToRemove;

    public override SyntaxNode? VisitClassDeclaration(ClassDeclarationSyntax node)
    {
        Debug.WriteLine(node.Identifier.ToString());
        var result = base.VisitClassDeclaration(node);
        var fullName = node.GetFullName(DeclarationFullNameFlags.WithoutNamespaceName);
        if (fullName != null && _nodesToRemove.Contains(fullName))
            return null;
        return result;
    }

    public override SyntaxNode? VisitStructDeclaration(StructDeclarationSyntax node)
    {
        Debug.WriteLine(node.Identifier.ToString());
        var result = base.VisitStructDeclaration(node);
        var fullName = node.GetFullName(DeclarationFullNameFlags.WithoutNamespaceName);
        if (fullName != null && _nodesToRemove.Contains(fullName))
            return null;
        return result;
    }

    public override SyntaxNode? VisitInterfaceDeclaration(InterfaceDeclarationSyntax node)
    {
        Debug.WriteLine(node.Identifier.ToString());
        var result = base.VisitInterfaceDeclaration(node);
        var fullName = node.GetFullName(DeclarationFullNameFlags.WithoutNamespaceName);
        if (fullName != null && _nodesToRemove.Contains(fullName))
            return null;
        return result;
    }

    public override SyntaxNode? VisitDelegateDeclaration(DelegateDeclarationSyntax node)
    {
        Debug.WriteLine(node.Identifier.ToString());
        var result = base.VisitDelegateDeclaration(node);
        var fullName = node.GetFullName(DeclarationFullNameFlags.WithoutNamespaceName);
        if (fullName != null && _nodesToRemove.Contains(fullName))
            return null;
        return result;
    }
}