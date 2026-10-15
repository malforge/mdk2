using System;
using System.Linq;
using System.Threading.Tasks;
using Mdk.CommandLine.Shared.Api;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Mdk.CommandLine.IngameScript.Pack.DefaultProcessors;

/// <summary>
///     Rewrites members whose body is a single expression as expression bodied members, which saves the braces and the
///     <c>return</c> keyword.
/// </summary>
/// <remarks>
///     The script is compiled as C# 6, which has expression bodies for methods (including <c>void</c> methods whose body
///     is a single expression statement), operators, conversions, read only properties and read only indexers. It does
///     not have them for accessors, constructors or destructors, so those keep their blocks. A body containing anything
///     but whitespace - a comment, a preprocessor directive, a preserved region - is left alone, because an expression
///     body has nowhere to put it.
/// </remarks>
[RunAfter<CommentStripper>]
[RunBefore<WhitespaceTrimmer>]
public class ExpressionBodyCompactor : IDocumentProcessor
{
    /// <inheritdoc />
    public async Task<Document> ProcessAsync(Document document, IPackContext context)
    {
        if (context.Parameters.PackVerb.MinifierLevel < MinifierLevel.Lite)
        {
            context.Console.Trace("Skipping expression body compaction because the minifier level < Lite.");
            return document;
        }

        var root = await document.GetSyntaxRootAsync();
        if (root == null)
            return document;

        var rewriter = new Rewriter();
        var newRoot = rewriter.Visit(root);
        if (newRoot == null || rewriter.Count == 0)
        {
            context.Console.Trace("No bodies could be turned into expression bodies.");
            return document;
        }

        context.Console.Trace($"Turned {rewriter.Count} body/bodies into expression bodies.");
        return document.WithSyntaxRoot(newRoot);
    }

    class Rewriter : CSharpSyntaxRewriter
    {
        /// <summary>
        ///     The number of bodies which were rewritten.
        /// </summary>
        public int Count { get; private set; }

        public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            if (base.VisitMethodDeclaration(node) is not MethodDeclarationSyntax visited)
                return null;
            // A method may only drop the return keyword as well when it has nothing to return.
            var expression = GetBodyExpression(visited, visited.Body, IsVoid(visited.ReturnType));
            if (expression == null)
                return visited;
            return Compact(visited, visited.Body!, expression,
                (declaration, arrow, semicolon) => declaration.WithBody(null).WithExpressionBody(arrow).WithSemicolonToken(semicolon));
        }

        public override SyntaxNode? VisitOperatorDeclaration(OperatorDeclarationSyntax node)
        {
            if (base.VisitOperatorDeclaration(node) is not OperatorDeclarationSyntax visited)
                return null;
            var expression = GetBodyExpression(visited, visited.Body, false);
            if (expression == null)
                return visited;
            return Compact(visited, visited.Body!, expression,
                (declaration, arrow, semicolon) => declaration.WithBody(null).WithExpressionBody(arrow).WithSemicolonToken(semicolon));
        }

        public override SyntaxNode? VisitConversionOperatorDeclaration(ConversionOperatorDeclarationSyntax node)
        {
            if (base.VisitConversionOperatorDeclaration(node) is not ConversionOperatorDeclarationSyntax visited)
                return null;
            var expression = GetBodyExpression(visited, visited.Body, false);
            if (expression == null)
                return visited;
            return Compact(visited, visited.Body!, expression,
                (declaration, arrow, semicolon) => declaration.WithBody(null).WithExpressionBody(arrow).WithSemicolonToken(semicolon));
        }

        public override SyntaxNode? VisitPropertyDeclaration(PropertyDeclarationSyntax node)
        {
            if (base.VisitPropertyDeclaration(node) is not PropertyDeclarationSyntax visited)
                return null;
            // A property with an initializer keeps its accessor list: the initializer belongs to the auto property.
            if (visited.Initializer != null)
                return visited;
            var expression = GetGetterExpression(visited, visited.AccessorList);
            if (expression == null)
                return visited;
            return Compact(visited, visited.AccessorList!, expression,
                (declaration, arrow, semicolon) => declaration.WithAccessorList(null).WithExpressionBody(arrow).WithSemicolonToken(semicolon));
        }

        public override SyntaxNode? VisitIndexerDeclaration(IndexerDeclarationSyntax node)
        {
            if (base.VisitIndexerDeclaration(node) is not IndexerDeclarationSyntax visited)
                return null;
            var expression = GetGetterExpression(visited, visited.AccessorList);
            if (expression == null)
                return visited;
            return Compact(visited, visited.AccessorList!, expression,
                (declaration, arrow, semicolon) => declaration.WithAccessorList(null).WithExpressionBody(arrow).WithSemicolonToken(semicolon));
        }

        /// <summary>
        ///     Replaces the body of a declaration with an expression body, keeping whatever followed the body behind the
        ///     new semicolon and closing up the space the body used to take.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="node"></param>
        /// <param name="body">The block or accessor list which the expression body replaces.</param>
        /// <param name="expression"></param>
        /// <param name="rebuild"></param>
        /// <returns></returns>
        T Compact<T>(T node, SyntaxNode body, ExpressionSyntax expression, Func<T, ArrowExpressionClauseSyntax, SyntaxToken, T> rebuild) where T : SyntaxNode
        {
            var trailingTrivia = body.GetLastToken().TrailingTrivia;
            // Whatever sat between the declaration and its body (a line break and an indent, usually) goes away with it.
            var headerToken = body.GetFirstToken().GetPreviousToken();
            node = node.ReplaceToken(headerToken, headerToken.WithTrailingTrivia(SyntaxFactory.Space));

            var arrow = SyntaxFactory.ArrowExpressionClause(
                SyntaxFactory.Token(SyntaxKind.EqualsGreaterThanToken).WithTrailingTrivia(SyntaxFactory.Space),
                expression.WithoutTrivia());
            var semicolon = SyntaxFactory.Token(SyntaxKind.SemicolonToken).WithTrailingTrivia(trailingTrivia);

            Count++;
            return rebuild(node, arrow, semicolon);
        }

        /// <summary>
        ///     The single expression a block consists of, or <c>null</c> when the block cannot become an expression body.
        /// </summary>
        /// <param name="declaration"></param>
        /// <param name="body"></param>
        /// <param name="allowExpressionStatement">
        ///     Whether a bare expression statement counts, which it does for a <c>void</c> method but not for one which has
        ///     to return a value.
        /// </param>
        /// <returns></returns>
        static ExpressionSyntax? GetBodyExpression(MemberDeclarationSyntax declaration, BlockSyntax? body, bool allowExpressionStatement)
        {
            if (body == null || declaration.AttributeLists.Count > 0)
                return null;
            if (!CanRewrite(declaration, body))
                return null;
            if (body.Statements.Count != 1)
                return null;
            return body.Statements[0] switch
            {
                ReturnStatementSyntax { Expression: { } returned } => returned,
                ExpressionStatementSyntax expressionStatement when allowExpressionStatement => expressionStatement.Expression,
                _ => null
            };
        }

        /// <summary>
        ///     The single expression the only accessor of a property or indexer returns, or <c>null</c> when it cannot
        ///     become an expression body.
        /// </summary>
        /// <param name="declaration"></param>
        /// <param name="accessorList"></param>
        /// <returns></returns>
        static ExpressionSyntax? GetGetterExpression(BasePropertyDeclarationSyntax declaration, AccessorListSyntax? accessorList)
        {
            if (accessorList == null || declaration.AttributeLists.Count > 0)
                return null;
            if (!CanRewrite(declaration, accessorList))
                return null;
            if (accessorList.Accessors.Count != 1)
                return null;
            var accessor = accessorList.Accessors[0];
            // Only a plain getter: a setter cannot have an expression body in C# 6, and an accessor with a modifier of
            // its own would lose it.
            if (!accessor.IsKind(SyntaxKind.GetAccessorDeclaration) || accessor.Modifiers.Count > 0 || accessor.AttributeLists.Count > 0)
                return null;
            if (accessor.Body is not { Statements.Count: 1 } accessorBody)
                return null;
            return accessorBody.Statements[0] is ReturnStatementSyntax { Expression: { } returned } ? returned : null;
        }

        /// <summary>
        ///     Determines whether the declaration and the body it is about to lose may be rewritten at all: a preserved
        ///     declaration is left as written, and so is a body holding anything an expression body has no room for.
        /// </summary>
        /// <param name="declaration"></param>
        /// <param name="body"></param>
        /// <returns></returns>
        static bool CanRewrite(MemberDeclarationSyntax declaration, SyntaxNode body)
        {
            if (declaration.ShouldBePreserved())
                return false;
            if (body.ContainsDirectives)
                return false;
            return !body.DescendantTrivia(descendIntoTrivia: true)
                .Any(trivia => !trivia.IsKind(SyntaxKind.WhitespaceTrivia) && !trivia.IsKind(SyntaxKind.EndOfLineTrivia));
        }

        static bool IsVoid(TypeSyntax returnType) => returnType is PredefinedTypeSyntax predefined && predefined.Keyword.IsKind(SyntaxKind.VoidKeyword);
    }
}
