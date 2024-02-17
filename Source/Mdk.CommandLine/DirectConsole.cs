using System;
using System.Linq;
using Mdk.CommandLine.SharedApi;
using Microsoft.Extensions.Primitives;

namespace Mdk.CommandLine;

/// <summary>
/// A helper class for writing to the console and wrapping text.
/// </summary>
public class DirectConsole : IConsole
{
    public bool TraceEnabled { get; set; }

    public IConsole Trace(string? message = null, int wrapIndent = 4)
    {
        if (!TraceEnabled)
            return this;
        
        Print(message, wrapIndent);
        return this;
    }

    public IConsole Print(string? message = null, int wrapIndent = 4)
    {
        if (message == null)
        {
            Console.WriteLine();
            return this;
        }

        var maxWidth = Console.WindowWidth;
        if (maxWidth == 0)
        {
            Console.WriteLine(message);
            return this;
        }
        var individualLines = message.Split(["\r\n", "\r", "\n"], StringSplitOptions.None);
        foreach (var line in individualLines)
        {
            var indent = wrapIndent < 0 ? line.TakeWhile(t => t == ' ').Count() : wrapIndent;
            var source = new StringSegment(line);
            var lineMaxWidth = maxWidth - indent;
            var isFirst = true;
            while (source.Length > 0)
            {
                PopNextLine(ref source, out var nextLine, lineMaxWidth);
                if (!isFirst && indent > 0)
                    Console.Write(new string(' ', indent));
                Console.WriteLine(nextLine);
                isFirst = false;
            }
        }
        return this;
    }

    static void PopNextLine(ref StringSegment source, out StringSegment line, int maxWidth)
    {
        // If the text is shorter than the maximum width, return the entire text.
        if (source.Length <= maxWidth)
        {
            line = source;
            source = default;
            return;
        }

        // Start at the maximum width and work backwards until we find a space.
        // If we don't find a space, we will have to split the word.
        for (var i = maxWidth; i > 0; i--)
            if (source[i] == ' ')
            {
                line = source.Subsegment(0, i);
                source = source.Subsegment(i + 1);
                return;
            }

        // If we didn't find a space, split the word.
        line = source.Subsegment(0, maxWidth);
        source = source.Subsegment(maxWidth);
    }
}