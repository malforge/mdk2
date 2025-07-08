using System.Xml.Serialization;

namespace Mdk.DocGen3.CodeDoc;

public class DocParam
{
    [XmlAttribute("name")]
    public string? Name { get; set; }

    [XmlText(typeof(string))]
    [XmlAnyElement]
    public List<object> Items { get; set; } = new();
}