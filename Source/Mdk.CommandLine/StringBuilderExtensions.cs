using System.Text;

namespace Mdk.CommandLine;

/// <summary>
/// Utility methods for <see cref="StringBuilder"/>.
/// </summary>
public static class StringBuilderExtensions
{
    /// <summary>
    /// Gets the character at the specified index, or <c>'\0'</c> if the index is out of range.
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
    /// Finds the index of the next new line character, or <c>-1</c> if no new line is found.
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
    /// Determines if the <see cref="StringBuilder"/> starts with the specified value at the specified index.
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
    /// If the character at the specified index is a new line, returns the index of the next character after the new line.
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
}