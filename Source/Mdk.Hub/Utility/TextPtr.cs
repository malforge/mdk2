using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Mdk.Hub.Utility;

/// <summary>
///     A pointer into a string, allowing for efficient traversal and comparison.
/// </summary>
[DebuggerDisplay("{ToDebugString(),nq}")]
public readonly struct TextPtr(string? text, int index = 0) : IEquatable<TextPtr>
{
    public readonly string? Text = text;
    public readonly int Index = index;

    public static TextPtr operator +(TextPtr ptr, int offset) => new(ptr.Text, ptr.Index + offset);

    public static TextPtr operator ++(TextPtr ptr) => new(ptr.Text, ptr.Index + 1);

    public static TextPtr operator -(TextPtr ptr, int offset) => new(ptr.Text, ptr.Index - offset);

    public static TextPtr operator --(TextPtr ptr) => new(ptr.Text, ptr.Index - 1);

    public static int operator -(TextPtr left, TextPtr right)
    {
        if (!ReferenceEquals(left.Text, right.Text))
            throw new InvalidOperationException("Cannot subtract pointers to different texts.");
        return left.Index - right.Index;
    }

    public static bool operator ==(TextPtr left, TextPtr right) => ReferenceEquals(left.Text, right.Text) && left.Index == right.Index;

    public static bool operator !=(TextPtr left, TextPtr right) => !(left == right);

    public static bool operator <(TextPtr left, TextPtr right)
    {
        if (!ReferenceEquals(left.Text, right.Text))
            throw new InvalidOperationException("Cannot compare pointers to different texts.");
        return left.Index < right.Index;
    }

    public static bool operator >(TextPtr left, TextPtr right)
    {
        if (!ReferenceEquals(left.Text, right.Text))
            throw new InvalidOperationException("Cannot compare pointers to different texts.");
        return left.Index > right.Index;
    }

    public static bool operator <=(TextPtr left, TextPtr right)
    {
        if (!ReferenceEquals(left.Text, right.Text))
            throw new InvalidOperationException("Cannot compare pointers to different texts.");
        return left.Index <= right.Index;
    }

    public static bool operator >=(TextPtr left, TextPtr right)
    {
        if (!ReferenceEquals(left.Text, right.Text))
            throw new InvalidOperationException("Cannot compare pointers to different texts.");
        return left.Index >= right.Index;
    }

    public override bool Equals(object? obj) => obj is TextPtr ptr && this == ptr;

    public bool IsOutOfBounds() => Text == null || Index < 0 || Index >= Text.Length;

    public char this[int offset]
    {
        get
        {
            var index = Index + offset;
            if (Text == null || index < 0 || index >= Text.Length)
                return '\0';
            return Text[index];
        }
    }

    public bool Equals(TextPtr other) => this == other;

    public override int GetHashCode() => HashCode.Combine(Text, Index);


    string ToDebugString()
    {
        const int contextLength = 20;
        if (Text is null)
            return "<null>";
        if (IsOutOfBounds())
            return $"<out of bounds: {Index}>";
        if (Text.Length == 0)
            return "<empty>";

        var start = Math.Max(0, Index - contextLength);
        var end = Math.Min(Text.Length, Index + contextLength + 1);
        var window = Text.Substring(start, end - start);

        var leftClipped = start > 0;
        var rightClipped = end < Text.Length;

        var pointerPos = Index - start;

        var sb = new StringBuilder();

        const char ellipsis = '…';
        if (leftClipped) sb.Append(ellipsis);

        sb.Append(window.AsSpan(0, pointerPos));
        sb.Append('[').Append(window[pointerPos]).Append(']');
        sb.Append(window.AsSpan(pointerPos + 1));

        if (rightClipped) sb.Append(ellipsis);

        return sb.ToString();
    }

    public override string ToString()
    {
        // Return the substring from the current index to the end of the text
        if (Text == null || Index < 0 || Index >= Text.Length)
            return string.Empty;
        return Text[Index..];
    }

    public string ToString(int length)
    {
        if (Text == null || Index < 0 || Index >= Text.Length || length <= 0)
            return string.Empty;
        var maxLength = Math.Min(length, Text.Length - Index);
        return Text.Substring(Index, maxLength);
    }

    public string ToString(in TextPtr end)
    {
        if (Text == null || !ReferenceEquals(Text, end.Text) || Index < 0 || Index >= Text.Length || end.Index < 0 || end.Index > Text.Length || end.Index < Index)
            return string.Empty;
        return Text.Substring(Index, end.Index - Index);
    }


    static bool FastIsInList(char c, IReadOnlyList<char> chars)
    {
        // ReSharper disable once ForCanBeConvertedToForeach
        // ReSharper disable once LoopCanBeConvertedToQuery
        for (var i = 0; i < chars.Count; i++)
        {
            if (chars[i] == c)
                return true;
        }
        return false;
    }

    public TextPtr SkipForward(params IReadOnlyList<char> chars)
    {
        var ptr = this;
        while (!ptr.IsOutOfBounds() && FastIsInList(ptr[0], chars))
            ptr += 1;
        return ptr;
    }

    public TextPtr TrimForward()
    {
        var ptr = this;
        while (!ptr.IsOutOfBounds() && char.IsWhiteSpace(ptr[0]))
            ptr += 1;
        return ptr;
    }

    public TextPtr SkipBackward(params IReadOnlyList<char> chars)
    {
        var ptr = this;
        while (!ptr.IsOutOfBounds() && FastIsInList(ptr[-1], chars))
            ptr -= 1;
        return ptr;
    }

    public TextPtr TrimBackward()
    {
        var ptr = this;
        while (!ptr.IsOutOfBounds() && char.IsWhiteSpace(ptr[-1]))
            ptr -= 1;
        return ptr;
    }

    /// <summary>
    ///     Determines if the pointer is currently at the start of a line, which is either the beginning of the text string
    ///     or immediately following a newline character ('\n', '\r', or their combination).
    /// </summary>
    /// <returns>
    ///     A boolean value indicating whether the pointer is at the start of a line.
    /// </returns>
    public bool IsStartOfLine()
    {
        var previous = this - 1;
        return Index < 0 || previous.IsNewline();
    }

    /// <summary>
    ///     Determines if the pointer is currently at an end-of-line position, which is either outside the bounds of the string
    ///     or at a newline character ('\n', '\r', or their combination).
    /// </summary>
    /// <returns>
    ///     A boolean value indicating whether the pointer is at an end-of-line position.
    /// </returns>
    public bool IsEndOfLine() => Index >= (Text?.Length ?? 0) || IsNewline();

    /// <summary>
    ///     Determines if the pointer is currently at a newline character, whether it be '\n', '\r', or a combination of both.
    /// </summary>
    /// <returns></returns>
    public bool IsNewline()
    {
        if (IsOutOfBounds())
            return false;
        if (this[0] == '\n')
            return true;
        if (this[0] == '\r')
        {
            // Check for \r\n sequence
            if (!IsOutOfBounds() && this[1] == '\n')
                return true;
            return true; // Just \r is also a newline
        }
        return false;
    }

    /// <summary>
    ///     If the pointer is at a newline character (either '\n', '\r', or '\r\n'), advances the pointer past the newline.
    ///     If not at a newline, returns the pointer unchanged.
    /// </summary>
    /// <returns></returns>
    public TextPtr SkipNewline()
    {
        var ptr = this;
        if (ptr.IsOutOfBounds())
            return ptr;
        if (ptr[0] == '\n')
            return ptr + 1;
        if (ptr[0] == '\r')
        {
            if (!ptr.IsOutOfBounds() && ptr[1] == '\n')
                return ptr + 2; // Skip \r\n
            return ptr + 1; // Just skip \r
        }
        return ptr;
    }

    /// <summary>
    ///     Advances the pointer to the end of the current line or the next newline character, whichever comes first.
    ///     Optionally skips the newline character if the <paramref name="skipNewLine" /> parameter is set to true.
    /// </summary>
    /// <param name="skipNewLine">If true, the pointer will move past the newline character at the end of the line.</param>
    /// <returns>
    ///     A new <see cref="TextPtr" /> pointing to the end of the current line or past the newline character depending
    ///     on the value of <paramref name="skipNewLine" />.
    /// </returns>
    public TextPtr ToEndOfLine(bool skipNewLine = false)
    {
        var ptr = this;
        while (!ptr.IsEndOfLine())
            ptr += 1;
        if (skipNewLine)
            ptr = ptr.SkipNewline();
        return ptr;
    }

    /// <summary>
    ///     Moves the pointer to the start of the current line by traversing backward until the preceding newline character
    ///     is found or the beginning of the text is reached.
    /// </summary>
    /// <returns>The updated pointer positioned at the start of the current line.</returns>
    public TextPtr ToStartOfLine()
    {
        var ptr = this - 1;
        if (ptr.IsEndOfLine())
            return this;
        while (!ptr.IsOutOfBounds() && !ptr.IsNewline())
            ptr--;
        return ptr + 1;
    }
}
