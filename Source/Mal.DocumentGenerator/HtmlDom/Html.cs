using System.Net;
using System.Xml.Linq;

namespace Mal.DocumentGenerator.HtmlDom;

public static class Html
{
    public static XElement A(string href) => new("a", new XAttribute("href", href));
    public static XElement A(string href, XElement e0, params XElement[] elements) => NewWithContent("a", e0, elements);
    public static XElement A(string href, string text) => new("a", new XAttribute("href", href), WebUtility.HtmlEncode(text));
    public static XElement Br() => new("br");
    public static XElement C(string content) => new("c", WebUtility.HtmlEncode(content));
    public static XElement Code() => new("code");
    public static XElement Code(XElement e0, params XElement[] elements) => NewWithContent("div", e0, elements);
    public static XElement Code(string content) => new("code", WebUtility.HtmlEncode(content));
    public static XElement Div() => new("div");
    public static XElement Div(XElement e0, params XElement[] elements) => NewWithContent("div", e0, elements);
    public static XElement H1() => new("h1");
    public static XElement H1(string content) => new("h1", WebUtility.HtmlEncode(content));
    public static XElement H2() => new("h2");
    public static XElement H2(string content) => new("h2", WebUtility.HtmlEncode(content));
    public static XElement H3() => new("h3");
    public static XElement H3(string content) => new("h3", WebUtility.HtmlEncode(content));
    public static XElement H4() => new("h4");
    public static XElement H4(string content) => new("h4", WebUtility.HtmlEncode(content));
    public static XElement H5() => new("h5");
    public static XElement H5(string content) => new("h5", WebUtility.HtmlEncode(content));
    public static XElement H6() => new("h6");
    public static XElement H6(string content) => new("h6", WebUtility.HtmlEncode(content));
    public static XElement Hr() => new("hr");
    public static XElement Img(string src) => new("img", new XAttribute("src", src));
    public static XElement Img(string src, string alt) => new("img", new XAttribute("src", src), new XAttribute("alt", alt));
    public static XElement Li() => new("li");
    public static XElement Li(XElement e0, params XElement[] elements) => NewWithContent("li", e0, elements);
    public static XElement Li(string content) => new("li", WebUtility.HtmlEncode(content));
    public static XElement Ol() => new("ol");
    public static XElement Ol(XElement e0, params XElement[] elements) => NewWithContent("ol", e0, elements);
    public static XElement P() => new("p");
    public static XElement P(XElement e0, params XElement[] elements) => NewWithContent("p", e0, elements);
    public static XElement P(string content) => new("p", WebUtility.HtmlEncode(content));
    public static XElement Pre(string content) => new("pre", WebUtility.HtmlEncode(content));
    public static XElement Raw(string html) => XElement.Parse(html);
    public static XElement Span() => new("span");
    public static XElement Span(XElement e0, params XElement[] elements) => NewWithContent("span", e0, elements);
    public static XElement Span(string content) => new("span", WebUtility.HtmlEncode(content));
    public static XElement Table() => new("table");
    public static XElement Table(XElement e0, params XElement[] elements) => NewWithContent("table", e0, elements);
    public static XElement Td() => new("td");
    public static XElement Td(XElement e0, params XElement[] elements) => NewWithContent("td", e0, elements);
    public static XElement Th() => new("th");
    public static XElement Th(XElement e0, params XElement[] elements) => NewWithContent("th", e0, elements);
    public static XElement Tr() => new("tr");
    public static XElement Tr(XElement e0, params XElement[] elements) => NewWithContent("tr", e0, elements);
    public static XElement Ul() => new("ul");
    public static XElement Ul(XElement e0, params XElement[] elements) => NewWithContent("ul", e0, elements);
    public static XElement Strong() => new("strong");
    public static XElement Strong(string content) => new("strong", WebUtility.HtmlEncode(content));
    public static XElement Strong(XElement e0, params XElement[] elements) => NewWithContent("strong", e0, elements);
    public static XElement Em() => new("em");
    public static XElement Em(string content) => new("em", WebUtility.HtmlEncode(content));
    public static XElement Em(XElement e0, params XElement[] elements) => NewWithContent("em", e0, elements);
    public static XText Text(string textValue) => new(WebUtility.HtmlEncode(textValue));

    public static XElement WithClass(this XElement element, string className) => element.WithAttr("class", className);
    public static XElement WithId(this XElement element, string id) => element.WithAttr("id", id);
    public static XElement WithName(this XElement element, string name) => element.WithAttr("name", name);

    public static XElement WithAttr(this XElement element, string name, string value)
    {
        if (element.Attribute(name) != null)
            element.Attribute(name)!.Value = value;
        else
            element.Add(new XAttribute(name, value));
        return element;
    }

    static XElement NewWithContent(string elementName, XElement e0, XElement[] elements)
    {
        var element = new XElement(elementName);
        element.Add(e0);
        foreach (var e in elements)
            element.Add(e);
        return element;
    }
}