using System.Xml.Linq;

namespace Mdk.DocGen3;

public class Html(string lang, IEnumerable<HtmlElement> elements) : HtmlContainerElement("html", elements)
{
    public string Lang { get; } = lang;

    public XDocument ToXDocument() =>
        new(new XDocumentType("html", null, null, null), Render());

    public override string ToString() => ToXDocument().ToString(SaveOptions.OmitDuplicateNamespaces);

    public override IEnumerable<XNode> Render()
    {
        var htmlElement = (XElement) base.Render().Single();
        htmlElement.Add(new XAttribute("lang", Lang));
        yield return htmlElement;
    }

    public static Html Document(string lang, params IEnumerable<HtmlElement> elements) => new(lang, elements);

    public static Html Document(params IEnumerable<HtmlElement> elements) => Document("en", elements);

    public static HtmlHead Head() => new();

    public static HtmlContainerElement Body(params IEnumerable<HtmlElement> children) => new("body", children);

    public static HtmlContainerElement Aside(params IEnumerable<HtmlElement> children) => new("aside", children);

    public static HtmlContainerElement Section(params IEnumerable<HtmlElement> children) => new("section", children);

    public static HtmlContainerElement Header(params IEnumerable<HtmlElement> children) => new("header", children);

    public static HtmlContainerElement Footer(params IEnumerable<HtmlElement> children) => new("footer", children);

    public static HtmlContainerElement Main(params IEnumerable<HtmlElement> children) => new("main", children);

    public static HtmlContainerElement Nav(params IEnumerable<HtmlElement> children) => new("nav", children);

    public static HtmlContainerElement Article(params IEnumerable<HtmlElement> children) => new("article", children);

    public static HtmlContainerElement SectionHeader(params IEnumerable<HtmlElement> children) => new("section-header", children);

    public static HtmlContainerElement SectionFooter(params IEnumerable<HtmlElement> children) => new("section-footer", children);

    public static HtmlContainerElement SectionContent(params IEnumerable<HtmlElement> children) => new("section-content", children);

    public static HtmlContainerElement Span(params IEnumerable<HtmlElement> children) => new("span", children);

    public static HtmlContainerElement P(params IEnumerable<HtmlElement> children) => new("p", children);

    public static HtmlContainerElement H1(params IEnumerable<HtmlElement> children) => new("h1", children);

    public static HtmlContainerElement H2(params IEnumerable<HtmlElement> children) => new("h2", children);

    public static HtmlContainerElement H3(params IEnumerable<HtmlElement> children) => new("h3", children);

    public static HtmlContainerElement H4(params IEnumerable<HtmlElement> children) => new("h4", children);

    public static HtmlContainerElement H5(params IEnumerable<HtmlElement> children) => new("h5", children);

    public static HtmlContainerElement H6(params IEnumerable<HtmlElement> children) => new("h6", children);

    public static HtmlContainerElement Ul(params IEnumerable<HtmlElement> children) => new("ul", children);

    public static HtmlContainerElement Ol(params IEnumerable<HtmlElement> children) => new("ol", children);

    public static HtmlContainerElement Li(params IEnumerable<HtmlElement> children) => new("li", children);

    public static HtmlContainerElement Table(params IEnumerable<HtmlElement> children) => new("table", children);

    public static HtmlContainerElement Thead(params IEnumerable<HtmlElement> children) => new("thead", children);

    public static HtmlContainerElement Tbody(params IEnumerable<HtmlElement> children) => new("tbody", children);

    public static HtmlContainerElement Tfoot(params IEnumerable<HtmlElement> children) => new("tfoot", children);

    public static HtmlContainerElement Tr(params IEnumerable<HtmlElement> children) => new("tr", children);

    public static HtmlContainerElement Th(params IEnumerable<HtmlElement> children) => new("th", children);

    public static HtmlContainerElement Td(params IEnumerable<HtmlElement> children) => new("td", children);

    public static HtmlContainerElement Div(params IEnumerable<HtmlElement> children) => new("div", children);

    public static HtmlContainerElement A(string? href, params IEnumerable<HtmlElement> children) =>
        new HtmlContainerElement("a", children).WithAttribute("href", href);

    public static HtmlElement ForEach<T>(IEnumerable<T>? items, Func<T, HtmlContainerElement> renderFn)
    {
        var collection = new HtmlElementCollection(items?.Select(renderFn) ?? []);
        return collection;
    }
}

public class HtmlText(string text) : HtmlElement("$TEXT")
{
    public string Text { get; } = text;

    public override IEnumerable<XNode> Render()
    {
        yield return new XText(Text);
    }
}

public class HtmlElementCollection(IEnumerable<HtmlElement> children) : HtmlElement("$COLLECTION")
{
    protected List<HtmlElement> Children { get; } = [..children];

    public override IEnumerable<XNode> Render()
    {
        foreach (var child in Children.SelectMany(c => c.Render()))
            yield return child;
    }
}

public class HtmlElement(string tag) : IXmlRenderer
{
    public List<string> Classes { get; } = new();
    public Dictionary<string, object> Attributes { get; } = new();
    protected string Tag { get; } = tag;

    public virtual IEnumerable<XNode> Render()
    {
        yield return new XElement(Tag,
            Classes.Count > 0 ? new XAttribute("class", string.Join(" ", Classes)) : null,
            Attributes.Count > 0 ? Attributes.Select(attr => new XAttribute(attr.Key, attr.Value)) : null);
    }

    public static implicit operator HtmlElement(string text) => new HtmlText(text);
}

public class HtmlContainerElement(string tag, params IEnumerable<HtmlElement> children) : HtmlElement(tag)
{
    protected List<HtmlElement> Children { get; } = [..children];

    public override IEnumerable<XNode> Render()
    {
        var element = (XElement) base.Render().Single();
        foreach (var node in Children.SelectMany(child => child.Render()))
            element.Add(node);
        yield return element;
    }
}

public static class HtmlExtensions
{
    public static T WithClass<T>(this T element, string className) where T : HtmlElement
    {
        if (!string.IsNullOrEmpty(className))
        {
            if (element is HtmlContainerElement container)
                container.Classes.Add(className);
        }
        return element;
    }

    public static T WithAttribute<T>(this T element, string name, object? value) where T : HtmlElement
    {
        if (!string.IsNullOrEmpty(name) && value != null)
            element.Attributes[name] = value;
        return element;
    }
}

public class HtmlHead() : HtmlElement("head")
{
    readonly List<Meta> _metas = new();
    string? _charset;
    string? _title;

    public HtmlHead Title(string? title)
    {
        _title = title;
        return this;
    }

    public HtmlHead Charset(string? charset)
    {
        _charset = charset;
        return this;
    }

    public HtmlHead Meta(string name, string? content)
    {
        if (content is null) return this;
        _metas.Add(new Meta(name, content));
        return this;
    }

    public HtmlHead StyleSheet(string? href)
    {
        if (href is null) return this;
        _metas.Add(new Meta("stylesheet", href));
        return this;
    }

    public override IEnumerable<XNode> Render()
    {
        var headElement = (XElement) base.Render().Single();

        if (!string.IsNullOrEmpty(_title))
            headElement.Add(new XElement("title", _title));

        if (!string.IsNullOrEmpty(_charset))
            headElement.Add(new XElement("meta", new XAttribute("charset", _charset)));

        foreach (var meta in _metas.OfType<IXmlRenderer>())
            headElement.Add(meta.Render());

        yield return headElement;
    }
}

public class Meta(string name, string content) : IXmlRenderer
{
    public string Name { get; } = name;
    public string Content { get; } = content;

    IEnumerable<XNode> IXmlRenderer.Render()
    {
        yield return new XElement("meta", new XAttribute("name", Name), new XAttribute("content", Content));
    }
}

interface IXmlRenderer
{
    IEnumerable<XNode> Render();
}