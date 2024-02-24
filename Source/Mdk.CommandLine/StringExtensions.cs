using System;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Primitives;

namespace Mdk.CommandLine;

/// <summary>
/// Utility methods for <see cref="StringBuilder"/>.
/// </summary>
public static class StringExtensions
{
    /// <summary>
    /// A simple string search that returns the index of the first occurrence of any of the specified values. Compares using <see cref="StringComparison.Ordinal"/>.
    /// </summary>
    /// <param name="sourceText"></param>
    /// <param name="values"></param>
    /// <returns></returns>
    public static int IndexOfAny(this StringSegment sourceText, params string[] values)
    {
        var minLength = values.Min(v => v.Length);
        var endIndex = sourceText.Offset + sourceText.Length;
        for (var i = 0; i < sourceText.Length - minLength; i++)
        {
            var span = sourceText.Subsegment(i, sourceText.Length - i);
            foreach (var value in values)
            {
                var endOfValue = sourceText.Offset + i + value.Length;
                if (endOfValue > endIndex)
                    continue;
                
                if (span.StartsWith(value, StringComparison.Ordinal))
                    return i;
            }
        }
        return -1;
    }
    
    /// <summary>
    /// A simple string search that returns the index of the first occurrence of any of the specified values. Compares using <see cref="StringComparison.Ordinal"/>.
    /// </summary>
    /// <param name="sourceText"></param>
    /// <param name="values"></param>
    /// <returns></returns>
    public static int IndexOfAny(this string sourceText, params string[] values)
    {
        for (var i = 0; i < sourceText.Length; i++)
        {
            var span = sourceText.AsSpan(i, sourceText.Length - i);
            foreach (var value in values)
            {
                if (span.StartsWith(value, StringComparison.Ordinal))
                    return i;
            }
        }
        return -1;
    }
    
    /// <summary>
    /// A simple string search that returns the index of the first occurrence of any of the specified values. Compares using <see cref="StringComparison.Ordinal"/>.
    /// </summary>
    /// <param name="sourceText"></param>
    /// <param name="values"></param>
    /// <returns></returns>
    public static int IndexOfAny(ref this ReadOnlySpan<char> sourceText, params string[] values)
    {
        for (var i = 0; i < sourceText.Length; i++)
        {
            var span = sourceText[i..];
            foreach (var value in values)
            {
                if (span.StartsWith(value, StringComparison.Ordinal))
                    return i;
            }
        }
        return -1;
    }
}