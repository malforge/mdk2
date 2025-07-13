using System.Xml.Serialization;

namespace Mdk.DocGen3.CodeDoc;

public class DocMember
{
    [XmlAttribute("name")]
    public string? Name { get; set; }

    [XmlElement("summary")]
    public DocMixedContent? Summary { get; set; }

    [XmlElement("remarks")]
    public DocMixedContent? Remarks { get; set; }

    [XmlElement("returns")]
    public DocMixedContent? Returns { get; set; }

    [XmlElement("param")]
    public List<DocParam> Params { get; } = new();

    public string? RenderSummary() => Summary?.Render();
    public string? RenderRemarks() => Remarks?.Render();
    public string? RenderReturns() => Returns?.Render();
}