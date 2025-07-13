using System.Text;
using System.Web;
using System.Xml;
using System.Xml.Serialization;

namespace Mdk.DocGen3.CodeDoc;

public class DocMixedContent
{
    static readonly Dictionary<string, string> ElementTranslations = new(StringComparer.OrdinalIgnoreCase)
    {
        {"c", "code"},
        {"i", "em"},
        {"b", "strong"},
        {"para", "p"},
        {"paramref", "code"},
        {"remarks", "p"}
    };

    static readonly HashSet<string> PlainHtmlElements = new(StringComparer.OrdinalIgnoreCase)
    {
        "em", "code", "strong", "ul", "ol", "li", "h1", "h2", "h3", "h4", "h5", "h6"
    };

    [XmlText(typeof(string))]
    [XmlAnyElement]
    public List<object> Items { get; set; } = new();

    public string? Render()
    {
        var sb = new StringBuilder();
        foreach (var item in Items)
            RenderItem(item, sb);
        var final = Normalize(sb.ToString());
        return string.IsNullOrEmpty(final) ? null : final;
    }

    void RenderItem(object item, StringBuilder sb)
    {
        switch (item)
        {
            case string str:
                sb.Append(HttpUtility.HtmlEncode(str));
                break;
            case XmlText xmlText:
                sb.Append(HttpUtility.HtmlEncode(xmlText.Value));
                break;
            case XmlElement xmlElement when ElementTranslations.TryGetValue(xmlElement.Name, out var translatedName):
            {
                sb.Append('<');
                sb.Append(translatedName);
                foreach (XmlAttribute attr in xmlElement.Attributes)
                {
                    sb.Append(' ');
                    sb.Append(attr.Name);
                    sb.Append("=\"");
                    sb.Append(attr.Value);
                    sb.Append('"');
                }
                sb.Append('>');
                foreach (XmlNode child in xmlElement.ChildNodes)
                    RenderItem(child, sb);
                sb.Append("</");
                sb.Append(translatedName);
                sb.Append('>');
                break;
            }
            case XmlElement xmlElement when IsPlainHtml(xmlElement):
                // Render plain HTML elements as is
                sb.Append('<');
                sb.Append(xmlElement.Name);
                foreach (XmlAttribute attr in xmlElement.Attributes)
                {
                    sb.Append(' ');
                    sb.Append(attr.Name);
                    sb.Append("=\"");
                    sb.Append(attr.Value);
                    sb.Append('"');
                }
                sb.Append('>');
                foreach (XmlNode child in xmlElement.ChildNodes)
                    RenderItem(child, sb);
                sb.Append("</");
                sb.Append(xmlElement.Name);
                sb.Append('>');
                break;
            case XmlElement xmlElement when string.Equals(xmlElement.Name, "see", StringComparison.OrdinalIgnoreCase):
                // TODO: Resolve cref/href to actual html link
                // For now, just render the content plainly
                foreach (XmlNode child in xmlElement.ChildNodes)
                    RenderItem(child, sb);
                break;
            case XmlElement xmlElement when string.Equals(xmlElement.Name, "para", StringComparison.OrdinalIgnoreCase):
                // Render paragraph as html
                sb.Append("<p>");
                foreach (XmlNode child in xmlElement.ChildNodes)
                    RenderItem(child, sb);
                sb.Append("</p>");
                break;
            case XmlWhitespace ws:
                // Ignore whitespace nodes
                break;
            default:
                throw new NotSupportedException($"Unsupported item type in DocMixedContent: {item.GetType().Name}");
        }
    }

    static bool IsPlainHtml(XmlElement xmlElement) => PlainHtmlElements.Contains(xmlElement.Name);

    static string? Normalize(string? input) => input is not null ? string.Join(" ", input.Split(" \r\n\t", StringSplitOptions.RemoveEmptyEntries)).Trim() : null;
}