using System.Xml.Serialization;

namespace Mdk.DocGen3.CodeSecurity;

/// <summary>
///     Represents an action that can be performed on a block
/// </summary>
public class Action
{
    [XmlAttribute("name")]
    public string? Name { get; set; }

    [XmlAttribute("text")]
    public string? Text { get; set; }
}