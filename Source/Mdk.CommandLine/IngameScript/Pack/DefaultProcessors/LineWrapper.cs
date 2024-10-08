﻿using Microsoft.CodeAnalysis;
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
                //if (!IsAtStartOfLine())
                //{
                ClearLineInfo();
                node = node.WithLeadingTrivia(node.GetLeadingTrivia().Insert(0, SyntaxFactory.EndOfLine("\n").WithAdditionalAnnotations(new SyntaxAnnotation("MDK", "preserve"))));
                //}
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
                //if (!IsAtStartOfLine())
                //{
                ClearLineInfo();
                token = token.WithLeadingTrivia(token.LeadingTrivia.Insert(0, SyntaxFactory.EndOfLine("\n").WithAdditionalAnnotations(new SyntaxAnnotation("MDK", "preserve"))));
                //}
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