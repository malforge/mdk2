using System.Xml.Serialization;

namespace Mdk.DocGen3.CodeSecurity;

/// <summary>
///     Represents a property of a block
/// </summary>
public class Property
{
    [XmlAttribute("name")]
    public string? Name { get; set; }

    [XmlAttribute("type")]
    public string? Type { get; set; }
}