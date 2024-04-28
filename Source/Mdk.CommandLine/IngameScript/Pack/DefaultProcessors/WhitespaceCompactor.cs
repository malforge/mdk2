using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Mdk.CommandLine.IngameScript.Pack.DefaultProcessors;

class WhitespaceCompactor() : ProgramRewriter(false)
{
    public override SyntaxTrivia VisitTrivia(SyntaxTrivia trivia)
    {
        trivia = base.VisitTrivia(trivia);
        if (trivia.IsKind(SyntaxKind.EndOfLineTrivia) && trivia.ToString() == "\r\n")
            trivia = trivia.CopyAnnotationsTo(SyntaxFactory.EndOfLine("\n"));
        return trivia;
    }
    
    public override SyntaxToken VisitToken(SyntaxToken currentToken)
    {
        var previousToken = currentToken.GetPreviousToken();
        currentToken = base.VisitToken(currentToken);
        
        currentToken = currentToken.WithLeadingTrivia(currentToken.LeadingTrivia.Where(t => AnnotationExtensions.ShouldBePreserved((SyntaxTrivia)t)));
        currentToken = currentToken.WithTrailingTrivia(currentToken.TrailingTrivia.Where(t => t.ShouldBePreserved()));
        
        if (currentToken.LeadingTrivia.Sum(t => t.FullSpan.Length) + previousToken.TrailingTrivia.Where(t => t.ShouldBePreserved()).Sum(t => t.FullSpan.Length) == 0)
        {
            if (TokenCollisionDetector.IsColliding(previousToken.Kind(), currentToken.Kind()))
                currentToken = currentToken.WithLeadingTrivia(SyntaxFactory.Whitespace(" "));
        }
        
        return currentToken;
    }
}