using System.Text;

namespace Mdk.CommandLine;

/// <summary>
///     Utility methods for <see cref="StringBuilder" />.
/// </summary>
public static class StringBuilderExtensions
{
    /// <summary>
    ///     Gets the character at the specified index, or <c>'\0'</c> if the index is out of range.
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="index"></param>
    /// <returns></returns>
    public static char At(this StringBuilder builder, int index)
    {
        if (index < 0 || index >= builder.Length)
            return '\0';
        return builder[index];
    }

    /// <summary>
    ///     Finds the index of the next new line character, or <c>-1</c> if no new line is found.
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="start"></param>
    /// <returns></returns>
    public static int FindNewLine(this StringBuilder builder, int start)
    {
        for (var i = start; i < builder.Length; i++)
        {
            if (builder[i] == '\r' && builder.At(i + 1) == '\n')
                return i;
            if (builder[i] == '\n')
                return i;
        }

        return -1;
    }

    /// <summary>
    ///     Determines if the <see cref="StringBuilder" /> starts with the specified value at the specified index.
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="start"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public static bool StartsWith(this StringBuilder builder, int start, string value)
    {
        if (start < 0 || start + value.Length > builder.Length)
            return false;
        for (var i = 0; i < value.Length; i++)
        {
            if (builder[start + i] != value[i])
                return false;
        }
        return true;
    }

    /// <summary>
    ///     If the character at the specified index is a new line, returns the index of the next character after the new line.
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="index"></param>
    /// <returns></returns>
    public static int SkipNewLine(this StringBuilder builder, int index)
    {
        if (builder[index] == '\r' && builder.At(index + 1) == '\n')
            return index + 2;
        if (builder[index] == '\n')
            return index + 1;
        return index;
    }

    /// <summary>
    /// Removes an indentation level from the <see cref="StringBuilder" />.
    /// </summary>
    /// <param name="buffer"></param>
    /// <param name="indentation"></param>
    public static StringBuilder Unindent(this StringBuilder buffer, int indentation)
    {
        var indentString = new string(' ', indentation);
        if (!IsIndented(buffer, indentString))
            return buffer;

        var startOfLine = 0;
        var endOfLine = buffer.FindNewLine(0);
        while (endOfLine != -1)
        {
            if (IsLineWhitespace(buffer, startOfLine, endOfLine))
            {
                startOfLine = buffer.SkipNewLine(endOfLine);
                endOfLine = buffer.FindNewLine(startOfLine);
                continue;
            }

            if (buffer.StartsWith(startOfLine, indentString))
            {
                buffer.Remove(startOfLine, indentation);
                startOfLine = buffer.SkipNewLine(endOfLine - indentation);
                endOfLine = buffer.FindNewLine(startOfLine);
                continue;
            }

            startOfLine = buffer.FindNewLine(endOfLine);
            endOfLine = buffer.FindNewLine(startOfLine);
        }
        
        return buffer;
    }

    static bool IsIndented(StringBuilder buffer, string indentString)
    {
        var startOfLine = 0;
        var endOfLine = buffer.FindNewLine(0);
        while (endOfLine != -1)
        {
            if (IsLineWhitespace(buffer, startOfLine, endOfLine))
            {
                startOfLine = buffer.SkipNewLine(endOfLine);
                endOfLine = buffer.FindNewLine(startOfLine);
                continue;
            }

            if (!buffer.StartsWith(startOfLine, indentString))
                return false;
            startOfLine = buffer.SkipNewLine(endOfLine);
            endOfLine = buffer.FindNewLine(startOfLine);
        }

        return true;
    }

    static bool IsLineWhitespace(StringBuilder buffer, int startOfLine, int endOfLine)
    {
        for (var i = startOfLine; i < endOfLine; i++)
        {
            if (!char.IsWhiteSpace(buffer[i]))
                return false;
        }
        return true;
    }


    /// <summary>
    ///     Converts tabs to spaces in the <see cref="StringBuilder" />, aligning to the specified indentation.
    /// </summary>
    /// <param name="buffer"></param>
    /// <param name="indentation"></param>
    public static StringBuilder ConvertTabsToSpaces(this StringBuilder buffer, int indentation)
    {
        var chIndex = 0;
        for (var i = 0; i < buffer.Length; i++)
        {
            if (buffer[i] == '\r' && buffer.At(i + 1) == '\n')
            {
                i++;
                chIndex = 0;
                continue;
            }

            if (buffer[0] == '\n')
            {
                chIndex = 0;
                continue;
            }

            if (buffer[i] != '\t') continue;
            var charsToNextTabStop = indentation - chIndex % indentation;
            buffer.Remove(i, 1).Insert(i, new string(' ', charsToNextTabStop));
        }
        return buffer;
    }
}