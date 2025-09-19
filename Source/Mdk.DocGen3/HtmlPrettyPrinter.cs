using System.Text;
using System.Web;
using System.Xml;
using System.Xml.Linq;
using Microsoft.AspNetCore.Mvc;

namespace Mdk.DocGen3;

public class HtmlPrettyPrinter
{
    static readonly string[] NewLines = {"\r\n", "\n", "\r"};

    static readonly HashSet<string> InlineElements = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "a", "abbr", "b", "br", "code", "em", "i", "img", "input", "label", "select", "span", "strong", "sub", "sup", "small", "time", "textarea", "button",
        "h1", "h2", "h3", "h4", "h5", "h6", "title"
    };

    static readonly HashSet<string> RawElements = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "pre", "script", "style", "textarea", "code"
    };

    public int MaxLineLength { get; set; } = 240;

    public string Reformat(string validXHtml)
    {
        var sb = new StringBuilder();
        var doc = XDocument.Parse(validXHtml);

        // This will write the doctype (if any), the root element, and any siblings:
        foreach (var node in doc.Nodes())
        {
            FormatNode(node, sb, 0, false);
        }

        return sb.ToString();
    }

    static readonly ISet<string> VoidElements = new HashSet<string>
    {
        "area", "base", "br", "col", "embed", "hr", "img", "input",
        "link", "meta", "param", "source", "track", "wbr"
    };

// // Your existing sets:
// static readonly ISet<string> RawElements = new HashSet<string> { /* e.g. "script","style","pre","textarea","title" */ };
// static readonly ISet<string> InlineElements = new HashSet<string> {
//     /* your filtered list: "a","abbr","b","br","code","em","i","img","input",
//        "label","select","span","strong","sub","sup","small","time","textarea","button" */
// };

    void FormatNode(XNode node, StringBuilder sb, int depth, bool isInlineContext)
    {
        string indent = new string(' ', depth * 2);

        switch (node)
        {
            case XElement el when RawElements.Contains(el.Name.LocalName):
                // Emit raw element all on one line, no inner formatting
                if (!isInlineContext)
                    sb.AppendLine().Append(indent);
                sb.Append('<').Append(el.Name.LocalName);
                foreach (var attr in el.Attributes())
                {
                    sb.Append(' ')
                        .Append(attr.Name.LocalName)
                        .Append("=\"")
                        .Append(Escape(attr.Value))
                        .Append('"');
                }
                sb.Append('>')
                    .Append(el.Value) // raw inner text
                    .Append("</").Append(el.Name.LocalName).Append('>');
                // if (!isInlineContext) sb.AppendLine();
                break;

            case XElement el when VoidElements.Contains(el.Name.LocalName):
                // Self-closing void element
                if (!isInlineContext)
                    sb.AppendLine().Append(indent);
                sb.Append('<').Append(el.Name.LocalName);
                foreach (var attr in el.Attributes())
                {
                    sb.Append(' ')
                        .Append(attr.Name.LocalName)
                        .Append("=\"")
                        .Append(Escape(attr.Value))
                        .Append('"');
                }
                sb.Append(" />");
                // if (!isInlineContext) sb.AppendLine();
                break;

            case XElement el when InlineElements.Contains(el.Name.LocalName):
                // Inline element: open, recurse children with isInlineContext=true, close, no extra newlines
                if (!isInlineContext)
                    sb.AppendLine().Append(indent);
                sb.Append('<').Append(el.Name.LocalName);
                foreach (var attr in el.Attributes())
                {
                    sb.Append(' ')
                        .Append(attr.Name.LocalName)
                        .Append("=\"")
                        .Append(Escape(attr.Value))
                        .Append('"');
                }
                sb.Append('>');

                // children inherit inline context
                foreach (var child in el.Nodes())
                    FormatNode(child, sb, depth, true);

                sb.Append("</").Append(el.Name.LocalName).Append('>');
                // if (!isInlineContext) sb.AppendLine();
                break;

            case XElement el:
                // Block element: surround with newlines and indent children deeper
                if (!isInlineContext)
                    sb.AppendLine().Append(indent);
                sb.Append('<').Append(el.Name.LocalName);
                foreach (var attr in el.Attributes())
                {
                    sb.Append(' ')
                        .Append(attr.Name.LocalName)
                        .Append("=\"")
                        .Append(Escape(attr.Value))
                        .Append('"');
                }
                sb.Append('>');

                foreach (var child in el.Nodes())
                    FormatNode(child, sb, depth + 1, false);

                sb.AppendLine().Append(indent).Append("</").Append(el.Name.LocalName).Append(">");
                break;

            case XText txt:
                var text = txt.Value;
                if (isInlineContext)
                    sb.Append(HttpUtility.HtmlEncode(text.Trim()));
                else
                {
                    var lines = text.Split(NewLines, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var line in lines)
                    {
                        sb.AppendLine()
                            .Append(indent)
                            .Append(HttpUtility.HtmlEncode(line.Trim()));
                    }
                }
                break;

            case XComment com:
                if (!isInlineContext)
                    sb.AppendLine().Append(indent);
                sb.Append("<!-- ")
                    .Append(com.Value.Trim())
                    .Append(" -->");
                // if (!isInlineContext) sb.AppendLine();
                break;

            case XProcessingInstruction pi:
                if (!isInlineContext)
                    sb.AppendLine().Append(indent);
                sb.Append("<?")
                    .Append(pi.Target).Append(' ')
                    .Append(pi.Data)
                    .Append("?>");
                break;

            case XDocumentType dt:
                if (!isInlineContext)
                    sb.AppendLine().Append(indent);
                sb.Append("<!DOCTYPE ")
                    .Append(dt.Name);
                if (!string.IsNullOrEmpty(dt.PublicId))
                    sb.Append(" PUBLIC \"").Append(dt.PublicId).Append('"');
                if (!string.IsNullOrEmpty(dt.SystemId))
                    sb.Append(" \"").Append(dt.SystemId).Append('"');
                sb.Append('>');
                // if (!isInlineContext) sb.AppendLine();
                break;

            default:
                if (!isInlineContext)
                    sb.AppendLine();
                // fallback
                sb.Append(node);
                break;
        }
    }

// Utility to escape attribute values
    string Escape(string s) =>
        s.Replace("&", "&amp;")
            .Replace("\"", "&quot;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;");
}