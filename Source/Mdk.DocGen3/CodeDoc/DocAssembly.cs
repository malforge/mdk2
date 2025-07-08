using System.Xml.Serialization;

namespace Mdk.DocGen3.CodeDoc;

public class DocAssembly
{
    [XmlElement("name")]
    public string? Name { get; set; }
}