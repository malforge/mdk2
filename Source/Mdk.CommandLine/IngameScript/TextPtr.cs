using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Mdk.CommandLine.IngameScript;

readonly struct TextPtr(SourceText text, TextSpan span, int position = -1)
{
    public readonly SourceText Text = text;
    public readonly TextSpan Span = span;
    public readonly int Position = position >= 0? Math.Clamp(position, span.Start - 1, span.End) : span.Start;

    public static implicit operator int(TextPtr ptr) => ptr.Position;

    public static TextPtr operator +(TextPtr ptr, int offset) => new(ptr.Text, ptr.Span, ptr.Position + offset);

    public static TextPtr operator -(TextPtr ptr, int offset) => new(ptr.Text, ptr.Span, ptr.Position - offset);

    public static TextPtr operator ++(TextPtr ptr) => new(ptr.Text, ptr.Span, ptr.Position + 1);

    public static TextPtr operator --(TextPtr ptr) => new(ptr.Text, ptr.Span, ptr.Position - 1);

    public bool IsOutOfBounds() => Position < Span.Start || Position >= Span.End;

    public bool IsBeforeStart() => Position < Span.Start;

    public bool IsAfterEnd() => Position >= Span.End;

    public char this[int offset]
    {
        get
        {
            var index = Position + offset;
            if (index < Span.Start || index >= Span.End)
                return '\0';
            return Text[index];
        }
    }

    public bool Is(string value)
    {
        if (IsOutOfBounds())
            return false;
        if (!StartsWith(value))
            return false;
        var end = this + value.Length;
        return end.IsAtWordBoundary();
    }
    
    public bool IsIgnoreCase(string value)
    {
        if (IsOutOfBounds())
            return false;
        if (!StartsWithIgnoreCase(value))
            return false;
        var end = this + value.Length;
        return end.IsAtWordBoundary();
    }
    
    public bool StartsWith(string value)
    {
        if (IsOutOfBounds())
            return false;
        if (value.Length > Span.End - Position)
            return false;
        for (var i = Position; i < Position + value.Length; i++)
        {
            if (Text[i] != value[i - Position])
                return false;
        }
        return true;
    }

    public bool StartsWithIgnoreCase(string value)
    {
        if (IsOutOfBounds())
            return false;
        if (value.Length > Span.End - Position)
            return false;
        for (var i = Position; i < Position + value.Length; i++)
        {
            if (char.ToUpperInvariant(Text[i]) != char.ToUpperInvariant(value[i - Position]))
                return false;
        }
        return true;
    }

    public bool IsAtNewLine() => StartsWith("\r\n") || this[0] == '\n';

    public TextPtr SkipNewLine()
    {
        if (StartsWith("\r\n"))
            return this + 2;
        if (this[0] == '\n')
            return this + 1;
        return this;
    }

    public TextPtr SkipWhitespace(bool skipNewLines = false)
    {
        var ptr = this;
        while (ptr.IsAtWhitespace(skipNewLines))
            ptr++;
        return ptr;
    }

    public bool IsAtWhitespace(bool includeNewlines = false) => (!IsOutOfBounds() && char.IsWhiteSpace(this[0])) || (includeNewlines && IsAtNewLine());

    public TextPtr Advance(int n) => this + n;

    public bool Is(char ch) => this[0] == ch;

    public bool TryReadWord([MaybeNullWhen(false)] out string word, out TextPtr end)
    {
        var start = this;
        if (start.IsOutOfBounds())
        {
            word = null;
            end = start;
            return false;
        }
        end = start;
        if (!char.IsLetter(start[0]) && start[0] != '_')
        {
            word = null;
            return false;
        }
        end++;
        while (!end.IsOutOfBounds() && (char.IsLetterOrDigit(end[0]) || end[0] == '_'))
            end++;
        word = Text.ToString(new TextSpan(start.Position, end.Position - start.Position));
        return true;
    }

    public bool IsAtWordBoundary()
    {
        if (IsOutOfBounds())
            return true;

        var currentIsLetterOrDigit = char.IsLetterOrDigit(this[0]);
        var previousIsLetterOrDigit = char.IsLetterOrDigit(this[-1]);
        return currentIsLetterOrDigit != previousIsLetterOrDigit;
    }

    public override string ToString()
    {
        if (IsOutOfBounds())
            return "<out of bounds>";
        return Text.ToString(new TextSpan(Position, Math.Min(10, Span.End - Position)));
    }
}