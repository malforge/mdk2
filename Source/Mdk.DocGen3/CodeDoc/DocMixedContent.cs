using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace Mdk.DocGen3.CodeDoc;

public class DocMixedContent
{
    [XmlText(typeof(string))]
    [XmlAnyElement]
    public List<object> Items { get; set; } = new();

    public string? Render()
    {
        var sb = new StringBuilder();
        foreach (var item in Items)
            Render(item, sb);
        var final = Normalize(sb.ToString());
        return string.IsNullOrEmpty(final) ? null : final;
    }

    void Render(object item, StringBuilder sb)
    {
        switch (item)
        {
            case string str:
                sb.Append(str);
                break;
            case XmlText xmlText:
                sb.Append(xmlText.Value);
                break;
            case XmlElement { Name: "c" } xmlElement:
                sb.Append("<code>");
                foreach (XmlNode child in xmlElement.ChildNodes)
                    Render(child, sb);
                sb.Append("</code>");
                break;
            default:
                throw new NotSupportedException($"Unsupported item type in DocMixedContent: {item.GetType().Name}");
        }
    }

    static string? Normalize(string? input) => input is not null ? string.Join(" ", input.Split(" \r\n\t", StringSplitOptions.RemoveEmptyEntries)).Trim() : null;
}