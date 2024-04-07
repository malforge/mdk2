 using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Simplification;

namespace Mdk.CommandLine.IngameScript.Pack.DefaultProcessors;

/// <summary>
///     Simplified version of minifying rewriter that only removes trivia.
/// </summary>
class CommentStrippingRewriter() : ProgramRewriter(true)
{
    /// <inheritdoc />
    public override SyntaxToken VisitToken(SyntaxToken token)
    {
        token = base.VisitToken(token);

        var newTrivia = new List<SyntaxTrivia>();
        var trivia = token.LeadingTrivia;
        TrimTrivia(trivia, newTrivia, false);
        var previousToken = token.GetPreviousToken();
        if (TokenCollisionDetector.IsColliding(token.Kind(), previousToken.Kind()))
        {
            if (token.LeadingTrivia.Sum(t => t.FullSpan.Length) + previousToken.TrailingTrivia.Where(t => t.IsKind(SyntaxKind.WhitespaceTrivia)).Sum(t => t.FullSpan.Length) == 0)
                newTrivia.Add(SyntaxFactory.Whitespace(" "));
        }
        token = token.WithLeadingTrivia(newTrivia);
        trivia = token.TrailingTrivia;
        TrimTrivia(trivia, newTrivia, true);
        token = token.WithTrailingTrivia(newTrivia);

        return token;
    }

    /// <summary>
    ///     Removes trivia surrounding a meaningful token.
    /// </summary>
    /// <param name="trivia">List of trivia tokens to be processed.</param>
    /// <param name="newTrivia">List of trivia tokens to be left in the script.</param>
    /// <param name="trailingMode">If true, use special behaviour for trailing trivia.</param>
    static void TrimTrivia(SyntaxTriviaList trivia, List<SyntaxTrivia> newTrivia, bool trailingMode)
    {
        var lastPreserved = false;
        newTrivia.Clear();
        for (var index = 0; index < trivia.Count; index++)
        {
            var triviaItem = trivia[index];
            if (triviaItem.ShouldBePreserved())
            {
                lastPreserved = true;
                newTrivia.Add(triviaItem);
                continue;
            }
            if (lastPreserved && triviaItem.IsKind(SyntaxKind.WhitespaceTrivia))
            {
                lastPreserved = false;
                continue;
            }
            switch (triviaItem.Kind())
            {
                case SyntaxKind.SingleLineCommentTrivia:
                case SyntaxKind.MultiLineCommentTrivia:
                case SyntaxKind.DocumentationCommentExteriorTrivia:
                case SyntaxKind.SingleLineDocumentationCommentTrivia:
                case SyntaxKind.MultiLineDocumentationCommentTrivia:
                    while (index < trivia.Count && !trivia[index].IsKind(SyntaxKind.EndOfLineTrivia))
                        index++;
                    if (trailingMode)
                        index--;
                    while (newTrivia.Count > 0 && newTrivia[^1].IsKind(SyntaxKind.WhitespaceTrivia))
                        newTrivia.RemoveAt(newTrivia.Count - 1);
                    continue;

                default:
                    newTrivia.Add(triviaItem);
                    break;
            }
        }
    }

    /// <summary>
    ///     Process the document and remove all comments.
    /// </summary>
    /// <param name="document"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public async Task<Document> ProcessAsync(Document document)
    {
        var newDocument = await Simplifier.ReduceAsync(document).ConfigureAwait(false);

        var root = await newDocument.GetSyntaxRootAsync();
        root = Visit(root);
        if (root is null)
            throw new InvalidOperationException("Failed to rewrite the script.");
        return newDocument.WithSyntaxRoot(root);
    }
}