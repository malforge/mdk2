using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Mdk.Hub.Utility;

/// <summary>
///     JSON converter for CanonicalPath that serializes/deserializes as a string.
/// </summary>
public class CanonicalPathJsonConverter : JsonConverter<CanonicalPath>
{
    public override CanonicalPath Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var path = reader.GetString();
        return string.IsNullOrEmpty(path) ? default : new CanonicalPath(path);
    }

    public override void Write(Utf8JsonWriter writer, CanonicalPath value, JsonSerializerOptions options) => writer.WriteStringValue(value.Value);
}
