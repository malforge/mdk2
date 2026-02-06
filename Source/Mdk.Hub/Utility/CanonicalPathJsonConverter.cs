using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Mdk.Hub.Utility;

/// <summary>
///     JSON converter for CanonicalPath that serializes/deserializes as a string.
/// </summary>
public class CanonicalPathJsonConverter : JsonConverter<CanonicalPath>
{
    /// <summary>
    ///     Reads a CanonicalPath value from JSON.
    /// </summary>
    /// <param name="reader">The JSON reader.</param>
    /// <param name="typeToConvert">The type to convert.</param>
    /// <param name="options">The serializer options.</param>
    /// <returns>The deserialized CanonicalPath.</returns>
    public override CanonicalPath Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var path = reader.GetString();
        return string.IsNullOrEmpty(path) ? default : new CanonicalPath(path);
    }

    /// <summary>
    ///     Writes a CanonicalPath value to JSON.
    /// </summary>
    /// <param name="writer">The JSON writer.</param>
    /// <param name="value">The value to serialize.</param>
    /// <param name="options">The serializer options.</param>
    public override void Write(Utf8JsonWriter writer, CanonicalPath value, JsonSerializerOptions options) => writer.WriteStringValue(value.Value);
}
