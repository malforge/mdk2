namespace Mdk.DocGen3.Pages.Base;

public abstract class PageGeneratorLayout<TModel> : CodeGenerator, IPageGeneratorLayout
{
    protected const string DefaultContentName = "main";

    IReadOnlyDictionary<string, IPageGenerator>? _context;

    TModel? _model;

    public TModel Model => _model ?? throw new InvalidOperationException("Model is not set.");

    object? IPageGeneratorLayout.Model => Model;
    
    protected T GetSubModel<T>(string key) => (T?)this[key].Model ?? throw new InvalidOperationException($"SubModel '{key}' is not set or of incorrect type.");
    
    protected IPageGenerator this[string key] => _context?[key] ?? throw new KeyNotFoundException("PageGenerator is not correctly initialized.");

    public string Render(PageGenerator<TModel> content) => Render(new Dictionary<string, IPageGenerator> { [DefaultContentName] = content });

    public string Render(IReadOnlyDictionary<string, IPageGenerator> content)
    {
        _context = content;
        try
        {
            var html = OnRender();
            if (string.IsNullOrWhiteSpace(html))
                throw new InvalidOperationException("Rendered HTML is empty or null.");
            var formattedHtml = PrettyPrint(html);
            return formattedHtml;
        }
        finally
        {
            _context = null;
            _model = default;
        }
    }

    protected IPageGenerator Content(string name = DefaultContentName)
    {
        if (_context == null)
            throw new InvalidOperationException("PageGeneratorLayout is not correctly initialized.");

        if (string.IsNullOrEmpty(name)) throw new ArgumentException("Value cannot be null or empty.", nameof(name));

        if (!_context.TryGetValue(name, out var content))
            throw new KeyNotFoundException($"Content with name '{name}' not found in the context.");

        return content;
    }

    protected string RenderBody(string name = DefaultContentName) => Content(name).RenderSegment();
}