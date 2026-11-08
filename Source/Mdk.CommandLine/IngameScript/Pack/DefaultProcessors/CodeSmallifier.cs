using System;
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
///     compacts contiguous field declarations of the same type and modifiers, and drops the braces
///     around blocks whose single statement is a valid embedded statement.
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

        var semanticModel = await document.GetSemanticModelAsync() ?? throw new InvalidOperationException("Failed to get semantic model.");
        var rewriter = new CodeSmallifierRewriter(semanticModel);
        var newRoot = rewriter.Visit(root);
        // One block per pass, rescanning the current tree each time, so nested blocks like
        // "if (a) { if (b) { F(); } }" collapse fully without resurrecting an already unwrapped child.
        while (true)
        {
            var block = newRoot.DescendantNodes().OfType<BlockSyntax>().FirstOrDefault(CodeSmallifierRewriter.CanUnwrapBlock);
            if (block == null)
                break;
            newRoot = newRoot.ReplaceNode(block, CodeSmallifierRewriter.UnwrappedStatement(block));
        }
        return document.WithSyntaxRoot(newRoot);
    }

    class CodeSmallifierRewriter : CSharpSyntaxRewriter
    {
        readonly Stack<TypeDeclarationSyntax> _parentTypes = new();
        readonly SemanticModel _semanticModel;

        public CodeSmallifierRewriter(SemanticModel semanticModel)
        {
            _semanticModel = semanticModel;
        }

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

            // Strip "readonly" only for reference types (classes), not for structs.
            // For mutable structs, removing readonly changes runtime behavior because the
            // compiler emits defensive copies for mutating operations on readonly struct fields.
            // Use GetDeclaredSymbol on the variable declarator to get the actual type, rather than
            // GetTypeInfo on the TypeSyntax, which returns a placeholder type with incorrect IsValueType.
            bool shouldStripReadonly = false;
            foreach (var variable in newNode.Declaration.Variables)
            {
                var fieldSymbol = _semanticModel.GetDeclaredSymbol(variable) as IFieldSymbol;
                var actualType = fieldSymbol?.Type;
                // Check if the type is a reference type (class), not a value type (struct)
                if (actualType?.IsReferenceType == true)
                {
                    shouldStripReadonly = true;
                    break;
                }
            }

            var readonlyIndex = newNode.Modifiers.IndexOf(SyntaxKind.ReadOnlyKeyword);
            if (readonlyIndex >= 0 && shouldStripReadonly)
                newNode = newNode.WithoutModifierAt(readonlyIndex);

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

        /// <summary>
        ///     A block may be unwrapped when it sits in a place where the braces are optional - right after an
        ///     <c>if</c>, <c>else</c>, <c>for</c>, <c>foreach</c>, <c>while</c>, <c>do</c> or a switch section - and
        ///     its single statement is a valid embedded statement: C# forbids a declaration or a labeled statement
        ///     directly after those keywords without braces (CS1023). The braces of "if (a) { if (b) F(); } else G();"
        ///     are also kept, because dropping them would silently rebind the else from the outer to the inner if.
        ///     Member bodies are never unwrapped: a method body without braces is not valid C#.
        /// </summary>
        internal static bool CanUnwrapBlock(BlockSyntax block)
        {
            if (block.ContainsDirectives || block.ShouldBePreserved() || block.Statements.Count != 1)
                return false;
            var statement = block.Statements[0];
            if (statement is LabeledStatementSyntax or LocalDeclarationStatementSyntax)
                return false;
            if (statement is IfStatementSyntax { Else: null }
                && block.Parent is IfStatementSyntax { Else: not null } outerIf
                && outerIf.Statement == block)
                return false;
            return block.Parent switch
            {
                IfStatementSyntax ifStatement => ifStatement.Statement == block,
                ElseClauseSyntax elseClause => elseClause.Statement == block,
                ForStatementSyntax forStatement => forStatement.Statement == block,
                ForEachStatementSyntax forEachStatement => forEachStatement.Statement == block,
                WhileStatementSyntax whileStatement => whileStatement.Statement == block,
                DoStatementSyntax doStatement => doStatement.Statement == block,
                SwitchSectionSyntax section => section.Statements.LastOrDefault() == block,
                _ => false
            };
        }

        /// <summary>
        ///     The statement a block is unwrapped into. The braces and their newlines go, but any non-whitespace
        ///     trivia on the brace lines - comments - is kept, so nothing is silently dropped. The statement itself
        ///     keeps its own leading trivia, so it stays on its line at its indent.
        /// </summary>
        internal static StatementSyntax UnwrappedStatement(BlockSyntax block)
        {
            var statement = block.Statements[0];
            var lineBreak = LineBreak(block);
            var leading = new List<SyntaxTrivia>(KeepBraceTrivia(block.OpenBraceToken.LeadingTrivia));
            leading.AddRange(KeepBraceTrivia(block.OpenBraceToken.TrailingTrivia));
            leading.AddRange(statement.GetLeadingTrivia());
            // The token before the brace may end in a line comment with no newline after it (a one-liner like
            // "if (a) // c { F(); }"). Unwrapping would then place F(); inside that comment, so break the line.
            var previous = block.OpenBraceToken.GetPreviousToken().TrailingTrivia;
            if (previous.Any() && previous[^1].IsKind(SyntaxKind.SingleLineCommentTrivia))
                leading.Add(lineBreak);
            var statementIsInline = statement.GetLeadingTrivia().All(t => !t.IsKind(SyntaxKind.EndOfLineTrivia))
                && block.OpenBraceToken.TrailingTrivia.All(t => !t.IsKind(SyntaxKind.EndOfLineTrivia));
            if (statementIsInline
                && (previous.Any(t => t.IsKind(SyntaxKind.EndOfLineTrivia))
                    || block.OpenBraceToken.LeadingTrivia.Any(t => t.IsKind(SyntaxKind.EndOfLineTrivia))))
            {
                // An inline statement whose brace is on its own line: keep the brace's indent so the
                // statement doesn't land in column zero.
                var openLeading = block.OpenBraceToken.LeadingTrivia;
                var indentStart = openLeading.Count;
                while (indentStart > 0 && openLeading[indentStart - 1].IsKind(SyntaxKind.WhitespaceTrivia))
                    indentStart--;
                for (var i = indentStart; i < openLeading.Count; i++)
                    leading.Add(openLeading[i]);
            }
            var trailing = new List<SyntaxTrivia>(statement.GetTrailingTrivia());
            trailing.AddRange(KeepBraceTrivia(block.CloseBraceToken.LeadingTrivia));
            var closeTrailing = block.CloseBraceToken.TrailingTrivia;
            trailing.AddRange(KeepBraceTrivia(closeTrailing));
            // A closing brace at the end of a line carries the newline to the next token. If the kept
            // trivia doesn't already end a line, that newline must be kept or the statement glues to
            // whatever follows the block.
            var lastKind = trailing.Count > 0 ? trailing[^1].Kind() : (SyntaxKind?)null;
            var endsLine = lastKind == SyntaxKind.EndOfLineTrivia || lastKind == SyntaxKind.SingleLineCommentTrivia;
            if (!endsLine && closeTrailing.Any(t => t.IsKind(SyntaxKind.EndOfLineTrivia)))
                trailing.Add(lineBreak);
            // Trailing whitespace would end up at the end of the line, so drop it.
            while (trailing.Count > 0 && trailing[^1].IsKind(SyntaxKind.WhitespaceTrivia))
                trailing.RemoveAt(trailing.Count - 1);
            if (trailing.Count > 1 && trailing[^1].IsKind(SyntaxKind.EndOfLineTrivia) && trailing[^2].IsKind(SyntaxKind.WhitespaceTrivia))
                trailing.RemoveAt(trailing.Count - 2);
            CoalesceWhitespace(leading);
            CoalesceWhitespace(trailing);

            // Statements have no WithLeadingTrivia, so the trivia is carried on the statement's edge
            // tokens. ReplaceToken rebuilds the tree, so the last token has to be re-fetched from the
            // intermediate node - the original one is no longer in the tree.
            var first = statement.GetFirstToken();
            var leadingTrivia = SyntaxTriviaList.Create(EnsureCommentNewlines(leading, lineBreak).ToArray());
            if (first == statement.GetLastToken())
                return statement.ReplaceToken(first, first.WithLeadingTrivia(leadingTrivia)
                    .WithTrailingTrivia(SyntaxTriviaList.Create(EnsureCommentNewlines(trailing, lineBreak).ToArray())));
            var newStatement = statement.ReplaceToken(first, first.WithLeadingTrivia(leadingTrivia));
            var last = newStatement.GetLastToken();
            return newStatement.ReplaceToken(last, last.WithTrailingTrivia(SyntaxTriviaList.Create(EnsureCommentNewlines(trailing, lineBreak).ToArray())));
        }

        /// <summary>
        ///     The trivia of a brace being dropped: newlines and whitespace go away with the brace, but comments
        ///     and anything else non-whitespace stay. A newline is kept when it terminates a line comment, without
        ///     which the comment would swallow the following code, and the whitespace before a surviving comment
        ///     is kept so the comment doesn't move to column zero.
        /// </summary>
        static IReadOnlyList<SyntaxTrivia> KeepBraceTrivia(SyntaxTriviaList trivia)
        {
            var items = trivia.ToArray();
            var kept = new List<SyntaxTrivia>();
            for (var i = 0; i < items.Length; i++)
            {
                var item = items[i];
                if (item.IsKind(SyntaxKind.WhitespaceTrivia))
                {
                    var next = i + 1 < items.Length ? items[i + 1].Kind() : SyntaxKind.None;
                    if (next is SyntaxKind.SingleLineCommentTrivia or SyntaxKind.MultiLineCommentTrivia)
                        kept.Add(item);
                    continue;
                }
                if (item.IsKind(SyntaxKind.EndOfLineTrivia) && (kept.Count == 0 || !kept[^1].IsKind(SyntaxKind.SingleLineCommentTrivia)))
                    continue;
                kept.Add(item);
            }
            return kept;
        }

        /// <summary>
        ///     Merges adjacent whitespace trivia, which can result from combining the trivia of several dropped tokens.
        /// </summary>
        static void CoalesceWhitespace(List<SyntaxTrivia> trivia)
        {
            for (var i = trivia.Count - 2; i >= 0; i--)
            {
                if (trivia[i].IsKind(SyntaxKind.WhitespaceTrivia) && trivia[i + 1].IsKind(SyntaxKind.WhitespaceTrivia))
                    trivia.RemoveAt(i);
            }
        }

        /// <summary>
        ///     A line comment must be followed by a newline, otherwise it swallows the code on its line.
        /// </summary>
        static IReadOnlyList<SyntaxTrivia> EnsureCommentNewlines(IReadOnlyList<SyntaxTrivia> trivia, SyntaxTrivia lineBreak)
        {
            var items = trivia.ToArray();
            var result = new List<SyntaxTrivia>(items.Length);
            for (var i = 0; i < items.Length; i++)
            {
                result.Add(items[i]);
                var next = i + 1 < items.Length ? items[i + 1].Kind() : SyntaxKind.None;
                if (items[i].IsKind(SyntaxKind.SingleLineCommentTrivia) && next != SyntaxKind.EndOfLineTrivia)
                    result.Add(lineBreak);
            }
            return result;
        }

        /// <summary>
        ///     The line terminator of the code around the block, so inserted newlines match the surrounding file.
        /// </summary>
        static SyntaxTrivia LineBreak(BlockSyntax block)
        {
            foreach (var trivia in new[]
                     {
                         block.OpenBraceToken.LeadingTrivia,
                         block.OpenBraceToken.TrailingTrivia,
                         block.CloseBraceToken.LeadingTrivia,
                         block.CloseBraceToken.TrailingTrivia,
                         block.Statements[0].GetLeadingTrivia(),
                         block.Statements[0].GetTrailingTrivia()
                     })
            {
                var eol = trivia.FirstOrDefault(t => t.IsKind(SyntaxKind.EndOfLineTrivia));
                if (eol.IsKind(SyntaxKind.EndOfLineTrivia))
                    return eol;
            }
            return SyntaxFactory.EndOfLine(Environment.NewLine);
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
            // If any explicit accessibility modifier is present, private (or not) is intentional.
            var hasAccessibility = node.Modifiers.Any(m =>
                m.IsKind(SyntaxKind.PublicKeyword) ||
                m.IsKind(SyntaxKind.ProtectedKeyword) ||
                m.IsKind(SyntaxKind.InternalKeyword));
            if (hasAccessibility)
                return node;

            while (true)
            {
                var index = node.Modifiers.IndexOf(SyntaxKind.PrivateKeyword);
                if (index < 0)
                    return node;
                node = node.WithoutModifierAt(index);
            }
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
