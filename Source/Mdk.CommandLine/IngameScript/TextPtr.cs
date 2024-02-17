using System;
using Microsoft.CodeAnalysis.Text;

namespace Mdk.CommandLine.IngameScript;

internal readonly struct TextPtr(SourceText text, TextSpan span, int position)
{
    public readonly SourceText Text = text;
    public readonly TextSpan Span = span;
    public readonly int Position = Math.Clamp(position, span.Start - 1, span.End);

    public static implicit operator int(TextPtr ptr)
    {
        return ptr.Position;
    }

    public static TextPtr operator +(TextPtr ptr, int offset)
    {
        return new TextPtr(ptr.Text, ptr.Span, ptr.Position + offset);
    }

    public static TextPtr operator -(TextPtr ptr, int offset)
    {
        return new TextPtr(ptr.Text, ptr.Span, ptr.Position - offset);
    }

    public static TextPtr operator ++(TextPtr ptr)
    {
        return new TextPtr(ptr.Text, ptr.Span, ptr.Position + 1);
    }

    public static TextPtr operator --(TextPtr ptr)
    {
        return new TextPtr(ptr.Text, ptr.Span, ptr.Position - 1);
    }

    public bool IsOutOfBounds()
    {
        return Position < Span.Start || Position >= Span.End;
    }

    public bool IsBeforeStart()
    {
        return Position < Span.Start;
    }

    public bool IsAfterEnd()
    {
        return Position >= Span.End;
    }

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

    public bool StartsWith(string value)
    {
        if (IsOutOfBounds())
            return false;
        if (value.Length > Span.End - Position)
            return false;
        for (var i = Position; i < Position + value.Length; i++)
            if (Text[i] != value[i - Position])
                return false;
        return true;
    }

    public bool StartsWithIgnoreCase(string value)
    {
        if (IsOutOfBounds())
            return false;
        if (value.Length > Span.End - Position)
            return false;
        for (var i = Position; i < Position + value.Length; i++)
            if (char.ToUpperInvariant(Text[i]) != char.ToUpperInvariant(value[i - Position]))
                return false;
        return true;
    }

    public bool IsAtNewLine()
    {
        return StartsWith("\r\n") || this[0] == '\n';
    }

    public TextPtr SkipNewLine()
    {
        if (StartsWith("\r\n"))
            return this + 2;
        if (this[0] == '\n')
            return this + 1;
        return this;
    }
}