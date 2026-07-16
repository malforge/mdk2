using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Mdk.CommandLine.Shared.Api;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Mdk.CommandLine.IngameScript.Pack.DefaultProcessors;

/// <summary>
///     The purpose of this processor is to smallify the code without making it unreadable.
///     It removes unnecessary "internal" and "private" modifiers, strips "readonly" from fields,
///     and compacts contiguous field declarations of the same type and modifiers.
/// </summary>
[RunAfter<PartialMerger>]
[RunAfter<RegionAnnotator>]
[RunAfter<TypeSorter>]
[RunAfter<SymbolProtectionAnnotator>]
public class CodeSmallifier : IDocumentProcessor
{
    public async Task<Document> ProcessAsync(Document document, IPackContext context)
    {
        if (context.Parameters.PackVerb.MinifierLevel < MinifierLevel.Trim)
        {
            context.Console.Trace("Skipping code smallification because the minifier level < Trim.");
            return document;
        }
        
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
                if (!modifier.IsKind(SyntaxKind.None))
                {
                    newNode = newNode.WithModifiers(newNode.Modifiers.Remove(modifier))
                        .WithLeadingTrivia(newNode.GetLeadingTrivia().AddRange(modifier.LeadingTrivia));
                }

                return CompactFieldDeclarations(newNode);
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
                if (!modifier.IsKind(SyntaxKind.None))
                {
                    newNode = newNode.WithModifiers(newNode.Modifiers.Remove(modifier))
                        .WithLeadingTrivia(newNode.GetLeadingTrivia().AddRange(modifier.LeadingTrivia));
                }

                return CompactFieldDeclarations(newNode);
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
            return StripPrivate(newNode);
        }

        public override SyntaxNode VisitPropertyDeclaration(PropertyDeclarationSyntax node)
        {
            var newNode = (PropertyDeclarationSyntax?)base.VisitPropertyDeclaration(node);
            if (newNode == null)
                return node;
            return StripPrivate(newNode);
        }

        public override SyntaxNode VisitFieldDeclaration(FieldDeclarationSyntax node)
        {
            var newNode = (FieldDeclarationSyntax?)base.VisitFieldDeclaration(node);
            if (newNode == null)
                return node;

            // Strip "readonly" unconditionally.
            int readonlyIndex = -1;
            for (var i = 0; i < newNode.Modifiers.Count; i++)
            {
                if (newNode.Modifiers[i].IsKind(SyntaxKind.ReadOnlyKeyword))
                {
                    readonlyIndex = i;
                    break;
                }
            }
            if (readonlyIndex >= 0)
            {
                var readonlyModifier = newNode.Modifiers[readonlyIndex];
                var remaining = newNode.Modifiers.RemoveAt(readonlyIndex);
                if (remaining.Count > 0)
                {
                    // Move the removed modifier's leading trivia onto the first remaining
                    // modifier so subsequent compaction can pick it up.
                    remaining = remaining.Replace(remaining[0], remaining[0].WithLeadingTrivia(
                        readonlyModifier.LeadingTrivia.Concat(remaining[0].LeadingTrivia)));
                }
                newNode = newNode.WithModifiers(remaining);
                if (remaining.Count == 0)
                    newNode = newNode.WithLeadingTrivia(readonlyModifier.LeadingTrivia);
            }

            return StripPrivate(newNode);
        }

        public override SyntaxNode VisitEventDeclaration(EventDeclarationSyntax node)
        {
            var newNode = (EventDeclarationSyntax?)base.VisitEventDeclaration(node);
            if (newNode == null)
                return node;
            return StripPrivate(newNode);
        }

        public override SyntaxNode VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
        {
            var newNode = (ConstructorDeclarationSyntax?)base.VisitConstructorDeclaration(node);
            if (newNode == null)
                return node;
            return StripPrivate(newNode);
        }

        static bool IsSimpleField(FieldDeclarationSyntax field)
        {
            if (field.AttributeLists.Count > 0)
                return false;
            // Fields inside a "mdk preserve" region must stay exactly as written, so they are never
            // eligible for compaction. This also stops a non-preserved neighbour from being folded
            // into a preserved declaration.
            if (field.ShouldBePreserved())
                return false;
            return true;
        }

        static bool ModifiersEqual(FieldDeclarationSyntax a, FieldDeclarationSyntax b) =>
            a.Modifiers.Select(m => m.Kind()).SequenceEqual(b.Modifiers.Select(m => m.Kind()));

        /// <summary>
        ///     Strips redundant <c>private</c> from member declarations when no other accessibility
        ///     modifier is present.  Leading trivia from the removed keyword is preserved on the node
        ///     or the first remaining modifier.
        /// </summary>
        static T StripPrivate<T>(T node) where T : MemberDeclarationSyntax
        {
            var modifiers = node.Modifiers;
            // If any explicit accessibility modifier is present, private (or not) is intentional.
            var hasAccessibility = modifiers.Any(m =>
                m.IsKind(SyntaxKind.PublicKeyword) ||
                m.IsKind(SyntaxKind.ProtectedKeyword) ||
                m.IsKind(SyntaxKind.InternalKeyword));
            if (hasAccessibility)
                return node;

            // Collect private modifier indices from the end so removal doesn't shift earlier ones.
            var privateIndices = new List<int>();
            for (var i = modifiers.Count - 1; i >= 0; i--)
            {
                if (modifiers[i].IsKind(SyntaxKind.PrivateKeyword))
                    privateIndices.Add(i);
            }
            if (privateIndices.Count == 0)
                return node;

            var remaining = modifiers;
            foreach (var idx in privateIndices)
            {
                var trivia = remaining[idx].LeadingTrivia;
                remaining = remaining.RemoveAt(idx);
                // Attach the removed keyword's leading trivia to the first remaining modifier,
                // or to the node itself when nothing is left.
                if (remaining.Count > 0)
                    remaining = remaining.Replace(remaining[0],
                        remaining[0].WithLeadingTrivia(trivia.Concat(remaining[0].LeadingTrivia)));
            }

            var result = (T)node.WithModifiers(remaining);
            if (remaining.Count == 0)
                result = (T)(SyntaxNode)result.WithLeadingTrivia(
                    privateIndices.Select(i => modifiers[i].LeadingTrivia).SelectMany(t => t));
            return result;
        }

        static T CompactFieldDeclarations<T>(T node) where T : TypeDeclarationSyntax
        {
            /*
             * Example:
             *   // Before:
             *   class Program
             *   {
             *       string A;
             *       string[] C;
             *       string B;
             *       int D;
             *       int E;
             *   }
             *
             *   // After (ideal):
             *   class Program
             *   {
             *       string A, B;
             *       string[] C;
             *       int D, E;
             *   }
             */
            var members = node.Members;
            var updated = new List<MemberDeclarationSyntax>(members.Count);
            var i = 0;
            while (i < members.Count)
            {
                if (members[i] is not FieldDeclarationSyntax field || !IsSimpleField(field))
                {
                    updated.Add(members[i]);
                    i++;
                    continue;
                }

                var segmentStart = i;
                // Only merge contiguous simple fields to avoid reordering across other members.
                while (i < members.Count && members[i] is FieldDeclarationSyntax nextField && IsSimpleField(nextField))
                    i++;
                var segmentEnd = i;

                var groups = new List<(FieldDeclarationSyntax field, List<VariableDeclaratorSyntax> vars)>();
                for (var j = segmentStart; j < segmentEnd; j++)
                {
                    var current = (FieldDeclarationSyntax)members[j];
                    // Group by modifiers and type so "string" and "string[]" stay separate.
                    var matchIndex = groups.FindIndex(g =>
                        ModifiersEqual(g.field, current) &&
                        g.field.Declaration.Type.IsEquivalentTo(current.Declaration.Type));

                    if (matchIndex < 0)
                        groups.Add((current, current.Declaration.Variables.ToList()));
                    else
                        groups[matchIndex].vars.AddRange(current.Declaration.Variables);
                }

                for (var g = 0; g < groups.Count; g++)
                {
                    var (seed, vars) = groups[g];
                    var declaration = seed.Declaration.WithVariables(SyntaxFactory.SeparatedList(vars));
                    var rebuilt = seed.WithDeclaration(declaration);
                    // Keep leading/trailing trivia from the segment boundaries.
                    if (g == 0)
                        rebuilt = rebuilt.WithLeadingTrivia(members[segmentStart].GetLeadingTrivia());
                    if (g == groups.Count - 1)
                        rebuilt = rebuilt.WithTrailingTrivia(members[segmentEnd - 1].GetTrailingTrivia());
                    updated.Add(rebuilt);
                }
            }

            return (T)node.WithMembers(SyntaxFactory.List(updated));
        }
    }
}
