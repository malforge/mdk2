using System.Net;
using System.Xml.Linq;

namespace Mal.DocumentGenerator.HtmlDom;

public static class Html
{
    public static XElement Div() => new("div");

    public static XElement A(string href) => new("a", new XAttribute("href", href));
    
    public static XElement A(string href, string text) => new("a", new XAttribute("href", href), WebUtility.HtmlEncode(text));

    public static XText Text(string textValue) => new(WebUtility.HtmlEncode(textValue));

    public static XElement C(string content) => new("c", WebUtility.HtmlEncode(content));

    public static XElement Raw(string html) => XElement.Parse(html);

    public static XElement Pre(string content) => new("pre", WebUtility.HtmlEncode(content));

    public static XElement WithClass(this XElement element, string className)
    {
        if (element.Attribute("class") != null)
            element.Attribute("class")!.Value += " " + className;
        else
            element.Add(new XAttribute("class", className));
        return element;
    }

    public static XElement WithId(this XElement element, string id)
    {
        if (element.Attribute("id") != null)
            element.Attribute("id")!.Value = id;
        else
            element.Add(new XAttribute("id", id));
        return element;
    }

    public static XElement Ul() => new("ul");
    public static XElement Ol() => new("ol");
    public static XElement Li() => new("li");
    public static XElement Li(string content) => new("li", WebUtility.HtmlEncode(content));
    public static XElement P() => new("p");
    public static XElement P(string content) => new("p", WebUtility.HtmlEncode(content));

    public static XElement P(XElement e0, params XElement[] elements)
    {
        var element = new XElement("p");
        element.Add(e0);
        foreach (var e in elements)
            element.Add(e);
        return element;
    }
}