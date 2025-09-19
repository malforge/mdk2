namespace Mdk.DocGen3.Pages.Base;

public abstract class PageGenerator<TModel> : CodeGenerator, IPageGenerator //: IGenerator
{
    protected const string DefaultContentName = "main";

    TModel? _model;

    public TModel Model
    {
        get => _model ?? throw new InvalidOperationException("Model is not set.");
        set => _model = value ?? throw new ArgumentNullException(nameof(value));
    }
    
    object? IPageGenerator.Model => _model;

    public IPageGeneratorLayout? Layout { get; set; }

    public string Render()
    {
        try
        {
            string? html;
            if (Layout is not null)
            {
                html = Layout.Render(new Dictionary<string, IPageGenerator> { [DefaultContentName] = this });
                if (string.IsNullOrWhiteSpace(html))
                    throw new InvalidOperationException("Rendered HTML is empty or null.");
                return PrettyPrint(html);
            }
            html = OnRender();
            if (string.IsNullOrWhiteSpace(html))
                throw new InvalidOperationException("Rendered HTML is empty or null.");
            return PrettyPrint(html);
        }
        finally
        {
            _model = default;
        }
    }

    public string RenderSegment() => OnRender();
    
    string IPageGenerator.Render() => Render();
}

// public abstract class PageGenerator<T> : PageGenerator where T : PageGenerator
// {
//     protected new T this[string key] => (T) base[key];
//
//     protected new T Content(string name = DefaultContentName) => (T) base.Content(name);
// }