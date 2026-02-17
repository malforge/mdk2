using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Mdk.CommandLine.Shared;

/// <summary>
///     Utility for replacing MDK macros in text content.
/// </summary>
public static partial class MacroReplacer
{
    /// <summary>
    ///     Replaces all macros in the given content with their corresponding values.
    ///     Macros are in the form $NAME$ and are replaced with values from the dictionary.
    ///     Unknown macros are left unchanged.
    /// </summary>
    /// <param name="content">The content containing macros to replace.</param>
    /// <param name="macros">Dictionary of macro names to their replacement values.</param>
    /// <returns>The content with macros replaced.</returns>
    public static string Replace(string content, IReadOnlyDictionary<string, string> macros)
    {
        if (macros.Count == 0)
            return content;
        
        return GetMacroRegex().Replace(content,
            match => macros.TryGetValue(match.Value, out var value) ? value : match.Value);
    }

    [GeneratedRegex(@"\$[A-Z_][A-Z0-9_]*\$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex GetMacroRegex();
}
