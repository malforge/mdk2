using System.Diagnostics;

namespace Mdk.DocGen3.CodeSecurity;

[DebuggerDisplay("{ToDebugString(),nq}")]
public readonly struct TextPtr(string text, int start) : IEquatable<TextPtr>, IComparable<TextPtr>
{
    public readonly string Text = text;
    public readonly int Start = start;

    public char this[int index]
    {
        get
        {
            index += Start;
            if (index < 0 || index >= Text.Length) return '\0';
            return Text[index];
        }
    }

    public bool IsOutOfBounds() => Start < 0 || Start >= Text.Length;

    public static TextPtr operator +(TextPtr ptr, int offset) => new(ptr.Text, ptr.Start + offset);
    
    public static TextPtr operator +(TextPtr left, TextPtr right)
    {
        if (left.Text != right.Text)
            throw new InvalidOperationException("Cannot add pointers from different texts");
        return new TextPtr(left.Text, left.Start + right.Start);
    }

    public static TextPtr operator -(TextPtr ptr, int offset) => new(ptr.Text, ptr.Start - offset);
    
    public static TextPtr operator -(TextPtr left, TextPtr right)
    {
        if (left.Text != right.Text)
            throw new InvalidOperationException("Cannot subtract pointers from different texts");
        return new TextPtr(left.Text, left.Start - right.Start);
    }

    public static TextPtr operator ++(TextPtr ptr) => new(ptr.Text, ptr.Start + 1);

    public static TextPtr operator --(TextPtr ptr) => new(ptr.Text, ptr.Start - 1);

    public static bool operator ==(TextPtr left, TextPtr right) => left.Equals(right);

    public static bool operator !=(TextPtr left, TextPtr right) => !left.Equals(right);

    public static bool operator <(TextPtr left, TextPtr right) => left.CompareTo(right) < 0;

    public static bool operator >(TextPtr left, TextPtr right) => left.CompareTo(right) > 0;

    public static bool operator <=(TextPtr left, TextPtr right) => left.CompareTo(right) <= 0;

    public static bool operator >=(TextPtr left, TextPtr right) => left.CompareTo(right) >= 0;

    public bool Equals(TextPtr other) => Text == other.Text && Start == other.Start;

    public int CompareTo(TextPtr other)
    {
        if (Text == other.Text) return Start.CompareTo(other.Start);
        return string.Compare(Text, other.Text, StringComparison.Ordinal);
    }

    public override bool Equals(object? obj)
    {
        if (obj is TextPtr other) return Equals(other);
        return false;
    }
    
    public override int GetHashCode() => HashCode.Combine(Text, Start);

    public bool StartsWith(string text)
    {
        if (text.Length > Text.Length - Start)
            return false;

        for (var i = 0; i < text.Length; i++)
        {
            if (this[i] != text[i])
                return false;
        }

        return true;
    }

    public TextPtr FindAny(char[] chars)
    {
        for (var i = Start; i < Text.Length; i++)
        {
            if (chars.Contains(Text[i]))
                return new TextPtr(Text, i);
        }

        return End();
    }
    
    public TextPtr End() => new TextPtr(Text, Text.Length);

    public string TakeTo(TextPtr end)
    {
        if (end.Start < Start)
            throw new ArgumentOutOfRangeException(nameof(end), "End pointer is before start pointer");

        var length = end.Start - Start;
        if (length < 0)
            throw new ArgumentOutOfRangeException(nameof(end), "End pointer is before start pointer");

        return Text.Substring(Start, length);
    }

    public TextPtr SkipWhitespace(bool skipNewLines = false)
    {
        var ptr = this;
        if (skipNewLines)
        {
            while (!ptr.IsOutOfBounds() && char.IsWhiteSpace(ptr[0]))
            {
                ptr++;
            }
            return ptr;
        }
        
        while (!ptr.IsOutOfBounds() && char.IsWhiteSpace(ptr[0]) && !ptr.IsNewLine()) ptr++;

        return ptr;
    }

    public bool IsNewLine()
    {
        if (IsOutOfBounds())
            return false;

        var ch = this[0];
        return ch == '\r' || ch == '\n';
    }

    public TextPtr SkipNewLine()
    {
        if (this[0] == '\r' && this[1] == '\n') return this + 2;
        if (this[0] == '\r' || this[0] == '\n') return this + 1;
        return this;
    }

    public TextPtr FindEndOfLine()
    {
        var ptr = this;
        while (!ptr.IsOutOfBounds() && !ptr.IsNewLine())
            ptr++;
        return ptr;
    }

    public TextPtr TrimEnd()
    {
        var ptr = this;
        while (!ptr.IsOutOfBounds() && char.IsWhiteSpace(ptr[0]))
            ptr--;
        return ptr;
    }

    public bool IsEndOfLine()
    {
        return IsOutOfBounds() || IsNewLine();
    }

    private string ToDebugString()
    {
        // Check if pointer is out of bounds
        if (IsOutOfBounds())
        {
            return $"[Out of bounds: position {Start} in string of length {Text?.Length ?? 0}]";
        }

        const int contextSize = 10; // Show 10 characters before and after
        int startPos = Math.Max(0, Start - contextSize);
        int endPos = Math.Min(Text.Length, Start + contextSize + 1);
    
        // Build the debug string with a marker for the current position
        var debugBuilder = new System.Text.StringBuilder();
    
        // Add prefix ellipsis if we're not showing from the beginning
        if (startPos > 0)
            debugBuilder.Append("...");
    
        // Add characters before the current position
        for (int i = startPos; i < Start; i++)
        {
            AppendCharacter(debugBuilder, Text[i]);
        }
    
        // Add the current character with highlighting
        debugBuilder.Append('[');
        if (Start < Text.Length)
            AppendCharacter(debugBuilder, Text[Start]);
        else
            debugBuilder.Append("EOF");
        debugBuilder.Append(']');
    
        // Add characters after the current position
        for (int i = Start + 1; i < endPos; i++)
        {
            AppendCharacter(debugBuilder, Text[i]);
        }
    
        // Add suffix ellipsis if we're not showing until the end
        if (endPos < Text.Length)
            debugBuilder.Append("...");
    
        return debugBuilder.ToString();
    }

// Helper method to append a character, handling special characters
    private static void AppendCharacter(System.Text.StringBuilder builder, char c)
    {
        switch (c)
        {
            case '\r':
                builder.Append("\\r");
                break;
            case '\n':
                builder.Append("\\n");
                break;
            case '\t':
                builder.Append("\\t");
                break;
            case '\0':
                builder.Append("\\0");
                break;
            default:
                builder.Append(c);
                break;
        }
    }

    public TextPtr Find(char c)
    {
        for (var i = Start; i < Text.Length; i++)
        {
            if (Text[i] == c)
                return new TextPtr(Text, i);
        }

        return End();
    }
}
