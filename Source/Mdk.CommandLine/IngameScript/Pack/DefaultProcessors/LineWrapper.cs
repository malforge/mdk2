using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Mdk.CommandLine.IngameScript.Pack.DefaultProcessors;

class LineWrapper() : ProgramRewriter(true)
{
    bool _isPreservedBlock;
    LinePosition _lastNewLine;

    public int LineWidth { get; set; } = 120;

    void ClearLineInfo() => _lastNewLine = LinePosition.Zero;

    int GetCharacterIndexFor(LinePosition linePosition)
    {
        var character = linePosition.Character;
        if (linePosition.Line == _lastNewLine.Line)
            character -= _lastNewLine.Character;
        else
            MoveToStartOfLine(linePosition);

        return character;
    }

    void SetLineshift(LinePosition linePosition) => _lastNewLine = linePosition;

    void MoveToStartOfLine(LinePosition linePosition) => _lastNewLine = new LinePosition(linePosition.Line, 0);

    public override SyntaxNode VisitInterpolatedStringExpression(InterpolatedStringExpressionSyntax node)
    {
        if (node.ShouldBePreserved())
        {
            if (!_isPreservedBlock)
            {
                _isPreservedBlock = true;
                if (!IsAtStartOfLine(node))
                {
                    ClearLineInfo();
                    node = node.WithLeadingTrivia(node.GetLeadingTrivia().Insert(0, SyntaxFactory.EndOfLine("\n").WithAdditionalAnnotations(new SyntaxAnnotation("MDK", "preserve"))));
                }
            }

            return node;
        }
        
        _isPreservedBlock = false;
        
        var span = node.GetLocation().GetLineSpan();
        var endPosition = GetCharacterIndexFor(span.EndLinePosition);

        if (node.Span.Length < LineWidth && endPosition > LineWidth)
        {
            node = node.WithLeadingTrivia(SyntaxFactory.EndOfLine("\n").WithAdditionalAnnotations(new SyntaxAnnotation("MDK", "preserve")));
            SetLineshift(span.EndLinePosition);
        }

        return node;
    }

    bool IsAtStartOfLine(SyntaxNode node)
    {
        // If the leading trivia of _this_ node contains a newline, or the trailing trivia of the previous token contains a newline, we are at the start of a line.
        var leadingTrivia = node.GetLeadingTrivia();
        if (leadingTrivia.Any(SyntaxKind.EndOfLineTrivia))
            return true;
        var previousToken = node.GetFirstToken().GetPreviousToken();
        if (previousToken.TrailingTrivia.Any(SyntaxKind.EndOfLineTrivia))
            return true;
        return false;
    }

    public override SyntaxToken VisitToken(SyntaxToken token)
    {
        token = base.VisitToken(token);
        if (token.IsKind(SyntaxKind.None))
            return token;

        if (token.ShouldBePreserved())
        {
            if (!_isPreservedBlock)
            {
                _isPreservedBlock = true;
                if (!IsAtStartOfLine(token.Parent!))
                {
                    ClearLineInfo();
                    token = token.WithLeadingTrivia(token.LeadingTrivia.Insert(0, SyntaxFactory.EndOfLine("\n").WithAdditionalAnnotations(new SyntaxAnnotation("MDK", "preserve"))));
                }
            }

            return token;
        }

        _isPreservedBlock = false;

        if (token.IsKind(SyntaxKind.None))
            return token;

        var span = token.GetLocation().GetLineSpan();
        var endPosition = GetCharacterIndexFor(span.EndLinePosition);

        if (token.Span.Length < LineWidth && endPosition > LineWidth)
        {
            token = token.WithLeadingTrivia(SyntaxFactory.EndOfLine("\n").WithAdditionalAnnotations(new SyntaxAnnotation("MDK", "preserve")));
            SetLineshift(span.EndLinePosition);
        }

        return token;
    }
}