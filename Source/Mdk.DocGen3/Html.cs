using System.Net;
using System.Xml.Linq;

namespace Mdk.DocGen3;

public static class Html
{
    public static XElement WithClass(this XElement element, params string[] classes)
    {
        if (classes.Length > 0)
        {
            var currentClasses = element.Attribute("class")?.Value;
            var newClasses = currentClasses is null ? string.Join(" ", classes) : $"{currentClasses} {string.Join(" ", classes)}";
            element.SetAttributeValue("class", newClasses);
        }
        return element;
    }
//     public static string Element(string tag, Dictionary<string, string>? attributes, string content, params string[] classes)
//     {
//         if (classes.Length > 0)
//         {
//             attributes ??= new Dictionary<string, string>();
//             attributes["class"] = string.Join(" ", classes);
//         }
//
//         if (attributes is { Count: > 0 })
//         {
//             var attrString = string.Join(" ", attributes.Select(kv => $"{kv.Key}=\"{WebUtility.HtmlEncode(kv.Value)}\""));
//             return $"<{tag} {attrString}>{content}</{tag}>";
//         }
//         return $"<{tag}>{content}</{tag}>";
//     }
//
//     public static string A(string href, string text, params string[] classes) =>
//         Element("a",
//             new Dictionary<string, string>
//             {
//                 ["href"] = href
//             },
//             WebUtility.HtmlEncode(text),
//             classes);
//
//     public static string H(int i, string title, params string[] classes) => Element($"h{i}", null, WebUtility.HtmlEncode(title), classes);
//
//     public static string Div(string text, params string[] classes) =>
//         Element("div", null, WebUtility.HtmlEncode(text), classes);
//
//     public static HtmlTagBuilder Make() => new();
// }
//
// public class HtmlTagBuilder
// {
//     readonly List<Node> _nodes = new();
//     
//     /** Tag **/
//     public HtmlTagBuilder Tag(string tagName, string? cssClass, Func<HtmlTagBuilder, HtmlTagBuilder> contentFunc)
//     {
//         var childBuilder = new HtmlTagBuilder();
//         var content = contentFunc(childBuilder);
//         var tagNode = new TagNode(tagName, cssClass);
//         tagNode.Children.AddRange(content._nodes);
//         _nodes.Add(tagNode);
//         return this;
//     }
//
//     public HtmlTagBuilder Tag(string tagName, string? cssClass, string content)
//     {
//         var tagNode = new TagNode(tagName, cssClass);
//         tagNode.Children.Add(new TextNode(content));
//         _nodes.Add(tagNode);
//         return this;
//     }
//
//     public HtmlTagBuilder Tag(string tagName, Func<HtmlTagBuilder, HtmlTagBuilder> contentFunc) => Tag(tagName, null, contentFunc);
//
//     public HtmlTagBuilder Tag(string tagName, string content) => Tag(tagName, null, content);
//
//     /**
//      * /Tag *
//      * Div *
//      */
//     public HtmlTagBuilder Div(string? cssClass, Func<HtmlTagBuilder, HtmlTagBuilder> contentFunc) => Tag("div", cssClass, contentFunc);
//
//     public HtmlTagBuilder Div(string? cssClass, string content) => Tag("div", cssClass, content);
//
//     public HtmlTagBuilder Div(Func<HtmlTagBuilder, HtmlTagBuilder> contentFunc) => Div(null, contentFunc);
//
//     public HtmlTagBuilder Div(string content) => Div(null, content);
//
//     /**
//      * /Div *
//      * Span *
//      */
//     public HtmlTagBuilder Span(string? cssClass, Func<HtmlTagBuilder, HtmlTagBuilder> contentFunc) =>
//         Tag("span", cssClass, contentFunc);
//
//     public HtmlTagBuilder Span(string? cssClass, string content) => Tag("span", cssClass, content);
//
//     public HtmlTagBuilder Span(Func<HtmlTagBuilder, HtmlTagBuilder> contentFunc) => Span(null, contentFunc);
//
//     public HtmlTagBuilder Span(string content) => Span(null, content);
//
//     /**
//      * /Span *
//      * H *
//      */
//     public HtmlTagBuilder H(int level, string? cssClass, Func<HtmlTagBuilder, HtmlTagBuilder> contentFunc) =>
//         Tag($"h{level}", cssClass, contentFunc);
//
//     public HtmlTagBuilder H(int level, string? cssClass, string content) => Tag($"h{level}", cssClass, content);
//
//     public HtmlTagBuilder H(int level, Func<HtmlTagBuilder, HtmlTagBuilder> contentFunc) => H(level, null, contentFunc);
//
//     public HtmlTagBuilder H(int level, string content) => H(level, null, content);
//
//     public override string ToString() => ToString(0);
//
//     public string ToString(int indentLevel) => string.Join("\n", _nodes.Select(n => n.Render(indentLevel)));
//
//     /** /H **/
//     abstract class Node
//     {
//         public abstract string Render(int indentLevel);
//     }
//
//     class TextNode(string text) : Node
//     {
//         readonly string _text = text;
//
//         public override string Render(int indentLevel)
//         {
//             var indent = new string(' ', indentLevel * 2);
//             return $"{indent}{WebUtility.HtmlEncode(_text)}";
//         }
//     }
//
//     class TagNode(string tagName, string? cssClass) : Node
//     {
//         public string TagName { get; } = tagName;
//         public string? CssClass { get; } = cssClass;
//         public List<Node> Children { get; } = new();
//
//         public override string Render(int indentLevel)
//         {
//             // If there is no content, return an empty tag
//             var classAttr = CssClass != null ? $" class=\"{WebUtility.HtmlEncode(CssClass)}\"" : string.Empty;
//             if (Children.Count == 0)
//                 return $"<{TagName}{classAttr} />";
//
//             var indent = new string(' ', indentLevel * 2);
//             var content = string.Join("\n", Children.Select(c => c.Render(indentLevel + 1)));
//             return $"{indent}<{TagName}{classAttr}>\n{content}\n{indent}</{TagName}>";
//         }
//     }
}