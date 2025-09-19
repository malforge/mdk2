using System.Collections;
using System.Diagnostics;
using System.Web;

namespace Mdk.DocGen3.Pages.Base;

public abstract class CodeGenerator
{
    readonly HtmlPrettyPrinter _prettyPrinter = new();

    protected abstract string OnRender();

    protected static string? Esc(string? input) => HttpUtility.HtmlEncode(input);

    protected string PrettyPrint(string html) => _prettyPrinter.Reformat(html);

    static IEnumerable<string> EvaluateStrings(IEnumerable<object?> cssClasses) =>
        cssClasses
            .SelectMany(c => c switch
            {
                string str => [str.Trim()],
                IEnumerable enumerable => enumerable.Cast<object?>()
                    .SelectMany(inner => EvaluateStrings([inner])),
                null => [],
                _ => [c.ToString() ?? string.Empty]
            })
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Distinct();

    protected static string Join(string? separator, params IEnumerable<object?>? items) => items is null ? "" : string.Join(separator ?? "", EvaluateStrings(items));

    protected static string Css(params IEnumerable<object?>? cssClasses) => Join(" ", cssClasses);
}