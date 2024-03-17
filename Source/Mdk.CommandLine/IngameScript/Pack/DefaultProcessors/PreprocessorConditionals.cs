using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mdk.CommandLine.IngameScript.Pack.Api;
using Mdk.CommandLine.Utility;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Mdk.CommandLine.IngameScript.Pack.DefaultProcessors;

/// <summary>
///     Removes code within #if DEBUG blocks
/// </summary>
public class PreprocessorConditionals : IScriptPreprocessor
{
    /// <summary>
    ///     Removes code within #if DEBUG blocks
    /// </summary>
    /// <param name="document"></param>
    /// <param name="metadata"></param>
    /// <returns></returns>
    public async Task<Document> ProcessAsync(Document document, ScriptProjectMetadata metadata)
    {
        var sourceText = await document.GetTextAsync();
        if (sourceText.Length == 0)
            return document;
        var linesBuilder = ImmutableArray.CreateBuilder<TextLine>();
        List<Token> tokens = new();
        var root = new RootBlock();
        Stack<Block> stack = new();
        stack.Push(root);
        var needsMacroCheck = false;
        foreach (var line in sourceText.Lines)
        {
            tokens.Clear();
            if (!TryTokenize(sourceText, line.SpanIncludingLineBreak, tokens))
            {
                linesBuilder.Add(line);
                continue;
            }

            if (linesBuilder.Count > 0)
            {
                var textBlock = new TextBlock();
                textBlock.Lines.AddRange(linesBuilder);
                stack.Peek().Children.Add(textBlock);
                linesBuilder.Clear();
            }

            if (tokens[0].Kind == Kind.If)
            {
                var ifBlock = new IfBlock(tokens.Skip(1).ToImmutableArray());
                stack.Peek().Children.Add(ifBlock);
                stack.Push(ifBlock);
                needsMacroCheck = true;
            }
            else if (tokens[0].Kind == Kind.Elif)
            {
                var elifBlock = new ElifBlock(tokens.Skip(1).ToImmutableArray());
                var previous = stack.Pop();
                if (previous is IfBlock ifBlock)
                    ifBlock.Else = elifBlock;
                else if (previous is ElifBlock pastElifBlock)
                    pastElifBlock.Else = elifBlock;
                else
                    throw new InvalidOperationException("Unexpected #elif");
                stack.Push(elifBlock);
            }
            else if (tokens[0].Kind == Kind.Else)
            {
                var elseBlock = new ElseBlock();
                var previous = stack.Pop();
                if (previous is IfBlock ifBlock)
                    ifBlock.Else = elseBlock;
                else if (previous is ElifBlock pastElifBlock)
                    pastElifBlock.Else = elseBlock;
                else
                    throw new InvalidOperationException("Unexpected #else");
                stack.Push(elseBlock);
            }
            else if (tokens[0].Kind == Kind.Endif)
                stack.Pop();
        }
        
        if (!needsMacroCheck)
            return document;

        if (linesBuilder.Count > 0)
        {
            var textBlock = new TextBlock();
            textBlock.Lines.AddRange(linesBuilder);
            stack.Peek().Children.Add(textBlock);
        }

        var result = new StringBuilder();
        root.Evaluate(metadata.PreprocessorMacros ?? ImmutableHashSet<string>.Empty, result);

        return document.WithText(SourceText.From(result.ToString()));
    }

    bool TryTokenize(SourceText text, TextSpan span, List<Token> tokens)
    {
        var ptr = new TextPtr(text, span)
            .SkipWhitespace();

        while (!ptr.IsOutOfBounds())
        {
            TextPtr end;
            if (ptr.Is("#if"))
            {
                end = ptr.Advance(3);
                var token = new Token(Kind.If, TextSpan.FromBounds(ptr.Position, end.Position));
                tokens.Add(token);
                ptr = end.SkipWhitespace();
                continue;
            }

            if (ptr.Is("#else"))
            {
                end = ptr.Advance(5);
                var token = new Token(Kind.Else, TextSpan.FromBounds(ptr.Position, end.Position));
                tokens.Add(token);
                ptr = end.SkipWhitespace();
                continue;
            }

            if (ptr.Is("#elif"))
            {
                end = ptr.Advance(5);
                var token = new Token(Kind.Elif, TextSpan.FromBounds(ptr.Position, end.Position));
                tokens.Add(token);
                ptr = end.SkipWhitespace();
                continue;
            }

            if (ptr.Is("#endif"))
            {
                end = ptr.Advance(6);
                var token = new Token(Kind.Endif, TextSpan.FromBounds(ptr.Position, end.Position));
                tokens.Add(token);
                ptr = end.SkipWhitespace();
                continue;
            }

            if (ptr.Is('('))
            {
                end = ptr.Advance(1);
                var token = new Token(Kind.LParen, TextSpan.FromBounds(ptr.Position, end.Position));
                tokens.Add(token);
                ptr = end.SkipWhitespace();
                continue;
            }

            if (ptr.Is(')'))
            {
                end = ptr.Advance(1);
                var token = new Token(Kind.RParen, TextSpan.FromBounds(ptr.Position, end.Position));
                tokens.Add(token);
                ptr = end.SkipWhitespace();
                continue;
            }

            if (ptr.Is('!'))
            {
                end = ptr.Advance(1);
                var token = new Token(Kind.Not, TextSpan.FromBounds(ptr.Position, end.Position));
                tokens.Add(token);
                ptr = end.SkipWhitespace();
                continue;
            }

            if (ptr.StartsWith("&&"))
            {
                end = ptr.Advance(2);
                var token = new Token(Kind.And, TextSpan.FromBounds(ptr.Position, end.Position));
                tokens.Add(token);
                ptr = end.SkipWhitespace();
                continue;
            }

            if (ptr.StartsWith("||"))
            {
                end = ptr.Advance(2);
                var token = new Token(Kind.Or, TextSpan.FromBounds(ptr.Position, end.Position));
                tokens.Add(token);
                ptr = end.SkipWhitespace();
                continue;
            }

            if (ptr.TryReadWord(out var word, out end))
            {
                var token = new Token(Kind.Identifier, TextSpan.FromBounds(ptr.Position, end.Position), word);
                tokens.Add(token);
                ptr = end.SkipWhitespace();
                continue;
            }

            return false;
        }


        if (tokens.Count == 0)
            return false;
        var primaryToken = tokens[0];
        switch (primaryToken.Kind)
        {
            case Kind.If:
            case Kind.Elif:
            case Kind.Else:
            case Kind.Endif:
                return true;
            default:
                return false;
        }
    }


    enum Kind
    {
        None,
        If,
        Else,
        Elif,
        Endif,
        LParen,
        RParen,
        Not,
        And,
        Or,
        Identifier
    }


    readonly struct Token(Kind kind, TextSpan span, string? value = null)
    {
        public readonly Kind Kind = kind;
        public readonly TextSpan Span = span;
        public readonly string? Value = value;
    }

    abstract class Block
    {
        public List<Block> Children { get; } = new();
        public abstract void Evaluate(ImmutableHashSet<string> macros, StringBuilder result);
    }

    class RootBlock : Block
    {
        public override void Evaluate(ImmutableHashSet<string> macros, StringBuilder result)
        {
            foreach (var child in Children)
                child.Evaluate(macros, result);
        }
    }

    class TextBlock : Block
    {
        public List<TextLine> Lines { get; } = new();

        public override void Evaluate(ImmutableHashSet<string> macros, StringBuilder result)
        {
            foreach (var line in Lines)
                result.Append(line.Text?.ToString(line.SpanIncludingLineBreak) ?? "");
        }
    }

    abstract class ConditionalBlock : Block { }

    class IfBlock : ConditionalBlock
    {
        public IfBlock(ImmutableArray<Token> expression)
        {
            // Convert the infix expression to postfix
            var stack = new Stack<Token>();
            var postfix = new List<Token>();
            foreach (var token in expression)
            {
                switch (token.Kind)
                {
                    case Kind.Identifier:
                        postfix.Add(token);
                        break;
                    case Kind.LParen:
                        stack.Push(token);
                        break;
                    case Kind.RParen:
                        while (stack.Count > 0 && stack.Peek().Kind != Kind.LParen)
                            postfix.Add(stack.Pop());
                        stack.Pop();
                        break;
                    case Kind.Not:
                    case Kind.And:
                    case Kind.Or:
                        while (stack.Count > 0 && Precedence(stack.Peek().Kind) >= Precedence(token.Kind))
                            postfix.Add(stack.Pop());
                        stack.Push(token);
                        break;
                }
            }
            while (stack.Count > 0)
                postfix.Add(stack.Pop());
            Expression = postfix.ToImmutableArray();
        }

        public ImmutableArray<Token> Expression { get; }
        public ConditionalBlock? Else { get; set; }

        static int Precedence(Kind kind) =>
            kind switch
            {
                Kind.Not => 3,
                Kind.And => 2,
                Kind.Or => 1,
                _ => 0
            };

        public override void Evaluate(ImmutableHashSet<string> macros, StringBuilder result)
        {
            var condition = EvaluateExpression(macros, Expression);
            if (condition)
            {
                foreach (var child in Children)
                    child.Evaluate(macros, result);
            }
            else
                Else?.Evaluate(macros, result);
        }

        bool EvaluateExpression(ImmutableHashSet<string> macros, ImmutableArray<Token> expression)
        {
            var stack = new Stack<bool>();
            var i = 0;
            while (i < expression.Length)
            { 
                var token = expression[i];
                switch (token.Kind)
                {
                    case Kind.Identifier:
                        stack.Push(macros.Contains(token.Value!));
                        break;
                    case Kind.Not:
                        stack.Push(!stack.Pop());
                        break;
                    case Kind.And:
                    {
                        var right = stack.Pop();
                        var left = stack.Pop();
                        stack.Push(left && right);
                        break;
                    }
                    case Kind.Or:
                    {
                        var right = stack.Pop();
                        var left = stack.Pop();
                        stack.Push(left || right);
                        break;
                    }
                }
                i++;
            }
            return stack.Pop();
        }
    }

    class ElifBlock(ImmutableArray<Token> expression) : IfBlock(expression);

    class ElseBlock : ConditionalBlock
    {
        public override void Evaluate(ImmutableHashSet<string> macros, StringBuilder result)
        {
            foreach (var child in Children)
                child.Evaluate(macros, result);
        }
    }
}