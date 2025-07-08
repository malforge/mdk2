using System.Xml.Serialization;

namespace Mdk.DocGen3.CodeSecurity;

/// <summary>
///     Represents a block type with its actions and properties
/// </summary>
public class Block
{
    [XmlAttribute("type")]
    public string? Type { get; set; }

    [XmlAttribute("typedefinition")]
    public string? TypeDefinition { get; set; }

    [XmlElement("action")]
    public List<Action> Actions { get; } = new();

    [XmlElement("property")]
    public List<Property> Properties { get; } = new();
}