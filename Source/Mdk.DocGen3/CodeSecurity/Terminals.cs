using System.Xml.Serialization;

namespace Mdk.DocGen3.CodeSecurity;

/// <summary>
///     Root element containing all terminal blocks
/// </summary>
[XmlRoot("terminals")]
public class Terminals
{
    [XmlElement("block")]
    public List<Block> Blocks { get; } = new();

    public static Terminals Load(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentNullException(nameof(fileName));

        var serializer = new XmlSerializer(typeof(Terminals));
        using var reader = new StreamReader(fileName);
        return (Terminals?)serializer.Deserialize(reader) ?? throw new InvalidOperationException($"Failed to deserialize {fileName}");
    }
}