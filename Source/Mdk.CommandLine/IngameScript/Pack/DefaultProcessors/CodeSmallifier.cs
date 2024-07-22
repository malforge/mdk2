using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Mdk.CommandLine.IngameScript.Pack.Api;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Mdk.CommandLine.IngameScript.Pack.DefaultProcessors;

/// <summary>
///     The purpose of this processor is to smallify the code without making it unreadable.
///     It currently just removes unnecessary "internal" and "private" modifiers.
/// </summary>
[RunAfter<PartialMerger>]
[RunAfter<RegionAnnotator>]
[RunAfter<TypeSorter>]
[RunAfter<SymbolProtectionAnnotator>]
public class CodeSmallifier : IScriptPostprocessor
{
    public async Task<Document> ProcessAsync(Document document, IPackContext context)
    {
        var root = await document.GetSyntaxRootAsync();
        if (root == null)
            return document;

        var newRoot = new CodeSmallifierRewriter().Visit(root);
        return document.WithSyntaxRoot(newRoot);
    }

    class CodeSmallifierRewriter : CSharpSyntaxRewriter
    {
        readonly Stack<TypeDeclarationSyntax> _parentTypes = new();

        public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            _parentTypes.Push(node);
            try
            {
                var newNode = (ClassDeclarationSyntax?)base.VisitClassDeclaration(node);
                if (newNode == null)
                    return node;
                var modifierKind = _parentTypes.Count > 0 ? SyntaxKind.PrivateKeyword : SyntaxKind.InternalKeyword;
                var modifier = newNode.Modifiers.FirstOrDefault(m => m.IsKind(modifierKind));
                if (modifier.IsKind(SyntaxKind.None))
                    return newNode;

                return newNode.WithModifiers(newNode.Modifiers.Remove(modifier))
                    .WithLeadingTrivia(newNode.GetLeadingTrivia().AddRange(modifier.LeadingTrivia));
            }
            finally
            {
                _parentTypes.Pop();
            }
        }

        public override SyntaxNode VisitInterfaceDeclaration(InterfaceDeclarationSyntax node)
        {
            _parentTypes.Push(node);
            try
            {
                var newNode = (InterfaceDeclarationSyntax?)base.VisitInterfaceDeclaration(node);
                if (newNode == null)
                    return node;
                var modifierKind = _parentTypes.Count > 0 ? SyntaxKind.PrivateKeyword : SyntaxKind.InternalKeyword;
                var modifier = newNode.Modifiers.FirstOrDefault(m => m.IsKind(modifierKind));
                if (modifier.IsKind(SyntaxKind.None))
                    return newNode;

                return newNode.WithModifiers(newNode.Modifiers.Remove(modifier))
                    .WithLeadingTrivia(newNode.GetLeadingTrivia().AddRange(modifier.LeadingTrivia));
            }
            finally
            {
                _parentTypes.Pop();
            }
        }

        public override SyntaxNode VisitStructDeclaration(StructDeclarationSyntax node)
        {
            _parentTypes.Push(node);
            try
            {
                var newNode = (StructDeclarationSyntax?)base.VisitStructDeclaration(node);
                if (newNode == null)
                    return node;
                var modifierKind = _parentTypes.Count > 0 ? SyntaxKind.PrivateKeyword : SyntaxKind.InternalKeyword;
                var modifier = newNode.Modifiers.FirstOrDefault(m => m.IsKind(modifierKind));
                if (modifier.IsKind(SyntaxKind.None))
                    return newNode;

                return newNode.WithModifiers(newNode.Modifiers.Remove(modifier))
                    .WithLeadingTrivia(newNode.GetLeadingTrivia().AddRange(modifier.LeadingTrivia));
            }
            finally
            {
                _parentTypes.Pop();
            }
        }

        public override SyntaxNode VisitEnumDeclaration(EnumDeclarationSyntax node)
        {
            var newNode = (EnumDeclarationSyntax?)base.VisitEnumDeclaration(node);
            if (newNode == null)
                return node;
            var modifierKind = _parentTypes.Count > 0 ? SyntaxKind.PrivateKeyword : SyntaxKind.InternalKeyword;
            var modifier = newNode.Modifiers.FirstOrDefault(m => m.IsKind(modifierKind));
            if (modifier.IsKind(SyntaxKind.None))
                return newNode;

            return newNode.WithModifiers(newNode.Modifiers.Remove(modifier))
                .WithLeadingTrivia(newNode.GetLeadingTrivia().AddRange(modifier.LeadingTrivia));
        }

        public override SyntaxNode VisitDelegateDeclaration(DelegateDeclarationSyntax node)
        {
            var newNode = (DelegateDeclarationSyntax?)base.VisitDelegateDeclaration(node);
            if (newNode == null)
                return node;
            var modifierKind = _parentTypes.Count > 0 ? SyntaxKind.PrivateKeyword : SyntaxKind.InternalKeyword;
            var modifier = newNode.Modifiers.FirstOrDefault(m => m.IsKind(modifierKind));
            if (modifier.IsKind(SyntaxKind.None))
                return newNode;

            return newNode.WithModifiers(newNode.Modifiers.Remove(modifier))
                .WithLeadingTrivia(newNode.GetLeadingTrivia().AddRange(modifier.LeadingTrivia));
        }

        public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            var newNode = (MethodDeclarationSyntax?)base.VisitMethodDeclaration(node);
            if (newNode == null)
                return node;
            if (newNode.Modifiers.Count == 1 && newNode.Modifiers[0].IsKind(SyntaxKind.PrivateKeyword))
            {
                var modifierToRemove = newNode.Modifiers[0];
                newNode = newNode.WithModifiers(newNode.Modifiers.RemoveAt(0));
                newNode = newNode.WithLeadingTrivia(newNode.GetLeadingTrivia().AddRange(modifierToRemove.LeadingTrivia));
            }
            return newNode;
        }

        public override SyntaxNode VisitPropertyDeclaration(PropertyDeclarationSyntax node)
        {
            var newNode = (PropertyDeclarationSyntax?)base.VisitPropertyDeclaration(node);
            if (newNode == null)
                return node;
            if (newNode.Modifiers.Count == 1 && newNode.Modifiers[0].IsKind(SyntaxKind.PrivateKeyword))
            {
                var modifierToRemove = newNode.Modifiers[0];
                newNode = newNode.WithModifiers(newNode.Modifiers.RemoveAt(0));
                newNode = newNode.WithLeadingTrivia(newNode.GetLeadingTrivia().AddRange(modifierToRemove.LeadingTrivia));
            }
            return newNode;
        }

        public override SyntaxNode VisitFieldDeclaration(FieldDeclarationSyntax node)
        {
            var newNode = (FieldDeclarationSyntax?)base.VisitFieldDeclaration(node);
            if (newNode == null)
                return node;
            if (newNode.Modifiers.Count == 1 && newNode.Modifiers[0].IsKind(SyntaxKind.PrivateKeyword))
            {
                var modifierToRemove = newNode.Modifiers[0];
                newNode = newNode.WithModifiers(newNode.Modifiers.RemoveAt(0));
                newNode = newNode.WithLeadingTrivia(newNode.GetLeadingTrivia().AddRange(modifierToRemove.LeadingTrivia));
            }
            return newNode;
        }

        public override SyntaxNode VisitEventDeclaration(EventDeclarationSyntax node)
        {
            var newNode = (EventDeclarationSyntax?)base.VisitEventDeclaration(node);
            if (newNode == null)
                return node;
            if (newNode.Modifiers.Count == 1 && newNode.Modifiers[0].IsKind(SyntaxKind.PrivateKeyword))
            {
                var modifierToRemove = newNode.Modifiers[0];
                newNode = newNode.WithModifiers(newNode.Modifiers.RemoveAt(0));
                newNode = newNode.WithLeadingTrivia(newNode.GetLeadingTrivia().AddRange(modifierToRemove.LeadingTrivia));
            }
            return newNode;
        }

        public override SyntaxNode VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
        {
            var newNode = (ConstructorDeclarationSyntax?)base.VisitConstructorDeclaration(node);
            if (newNode == null)
                return node;
            if (newNode.Modifiers.Count == 1 && newNode.Modifiers[0].IsKind(SyntaxKind.PrivateKeyword))
            {
                var modifierToRemove = newNode.Modifiers[0];
                newNode = newNode.WithModifiers(newNode.Modifiers.RemoveAt(0));
                newNode = newNode.WithLeadingTrivia(newNode.GetLeadingTrivia().AddRange(modifierToRemove.LeadingTrivia));
            }
            return newNode;
        }
    }
}