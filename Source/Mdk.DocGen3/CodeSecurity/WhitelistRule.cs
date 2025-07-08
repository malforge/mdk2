using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.RegularExpressions;

namespace Mdk.DocGen3.CodeSecurity;

public partial class WhitelistRule
{
    private static readonly Regex Regex = MyRegex();
    readonly Regex _regex;

    WhitelistRule(Regex regex)
    {
        _regex = regex;
    }

    public bool IsMatch(string typeKey)
    {
        if (string.IsNullOrWhiteSpace(typeKey))
            throw new ArgumentNullException(nameof(typeKey));

        return _regex.IsMatch(typeKey);
    }

    public static bool TryParse(string line, [NotNullWhen(true)] out WhitelistRule? rule)
    {
        if (!Regex.IsMatch(line))
        {
            rule = null;
            return false;
        }

        var sb = new StringBuilder(line.Length * 2);
        sb.Append("\\A");
        foreach (var c in line)
        {
            switch (c)
            {
                case '.':
                    sb.Append("[.+]");
                    break;

                case '*':
                    sb.Append(".*");
                    break;

                default:
                    // Use Regex.Escape to escape letters like '<' or '(' or ',' or spaces, etc.
                    sb.Append(Regex.Escape(c.ToString()));
                    break;
            }
        }
        sb.Append("\\z");

        var pattern = sb.ToString();
        var regex = new Regex(
            pattern,
            RegexOptions.ExplicitCapture | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace);

        rule = new WhitelistRule(regex);
        return true;
    }


    [GeneratedRegex(
        """
        ^(?<pattern>(?:<[^>]*?>)?\w+(?:<\w+(?:\,\s*\w+)*>)?(?:[.+](?!operator)(?:<[^>]*?>)?\w+(?:<\w+(?:\,\s*\w+)*>)?)*)

        		(?:
        		(?:\.operator\s*[+\-*/%=!]{1,2})?
        		(?:\((?:(?:(?:,\s*)?)(?:(?:params|out|ref)\s+)?\w+(?:[.+]\w+)*(?:\[])?)*\))?
        		|(?<descendants>[.+]\*)
        		)

        		,\s*(?<assembly>\w+(?:\.\w+)*)
        """,
        RegexOptions.ExplicitCapture | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace)]
    private static partial Regex MyRegex();
}