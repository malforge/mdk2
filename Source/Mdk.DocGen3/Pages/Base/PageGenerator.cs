using System.Collections;
using System.Web;

namespace Mdk.DocGen3.Pages.Base;

public abstract class PageGenerator : IGenerator
{
    protected const string DefaultContentName = "main";

    readonly HtmlPrettyPrinter _prettyPrinter = new();
    IReadOnlyDictionary<string, PageGenerator>? _context;

    public PageGenerator? Layout { get; set; }

    protected PageGenerator this[string key] => _context?[key] ?? throw new KeyNotFoundException("PageGenerator is not correctly initialized.");

    protected abstract string OnRender();

    protected static string? Esc(string? input) => HttpUtility.HtmlEncode(input);

    static IEnumerable<string> EvaluateStrings(IEnumerable<object?> cssClasses)
    {
        return cssClasses
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
    }

    protected static string Join(string? separator, params IEnumerable<object?>? items) => items is null? "" : string.Join(separator ?? "", EvaluateStrings(items));

    protected static string Css(params IEnumerable<object?>? cssClasses) => Join(" ", cssClasses);

    public string Render() => Render(new Dictionary<string, PageGenerator>());

    string IGenerator.Render(IGenerator content) =>
        content is PageGenerator pageGenerator
            ? Render(new Dictionary<string, PageGenerator> {[DefaultContentName] = pageGenerator})
            : throw new ArgumentException("Content must be of type PageGenerator.", nameof(content));

    public string Render(IReadOnlyDictionary<string, IGenerator> content) => throw new NotImplementedException();

    public string Render(PageGenerator content) => Render(new Dictionary<string, PageGenerator> {[DefaultContentName] = content});

    public string Render(IReadOnlyDictionary<string, PageGenerator> content)
    {
        _context = content;
        try
        {
            var html = OnRender();
            if (string.IsNullOrWhiteSpace(html))
                throw new InvalidOperationException("Rendered HTML is empty or null.");
            var layout = Layout;
            if (layout is not null)
                html = layout.Render(this);

            var formattedHtml = _prettyPrinter.Reformat(html);
            return formattedHtml;
        }
        finally
        {
            _context = null;
        }
    }

    protected PageGenerator Content(string name = DefaultContentName)
    {
        if (_context == null)
            throw new InvalidOperationException("PageGeneratorLayout is not correctly initialized.");

        if (string.IsNullOrEmpty(name)) throw new ArgumentException("Value cannot be null or empty.", nameof(name));

        if (!_context.TryGetValue(name, out var content))
            throw new KeyNotFoundException($"Content with name '{name}' not found in the context.");

        return content;
    }

    protected string RenderBody(string name = DefaultContentName) => Content(name).Render();
}

public abstract class PageGenerator<T> : PageGenerator where T : PageGenerator
{
    protected new T this[string key] => (T) base[key];

    protected new T Content(string name = DefaultContentName) => (T) base.Content(name);
}