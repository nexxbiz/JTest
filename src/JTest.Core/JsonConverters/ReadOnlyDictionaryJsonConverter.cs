using System.Text.Json;
using System.Text.Json.Serialization;

namespace JTest.Core.JsonConverters;

public sealed class ReadOnlyDictionaryJsonConverter<TValue> : JsonConverter<IReadOnlyDictionary<string, TValue>>
{
    public override IReadOnlyDictionary<string, TValue>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (!JsonDocument.TryParseValue(ref reader, out var doc))
        {
            throw new JsonException("Failed to parse JsonDocument");
        }

        var result = JsonSerializer.Deserialize<Dictionary<string, TValue>>(
            doc.RootElement.GetRawText(),
            options
        );

        return result?.AsReadOnly();
    }

    public override void Write(Utf8JsonWriter writer, IReadOnlyDictionary<string, TValue> value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        foreach (var property in value)
        {
            writer.WritePropertyName(
                property.Key
            );
            writer.WriteRawValue(
                JsonSerializer.SerializeToElement(property.Value).GetRawText()
            );
        }

        writer.WriteEndObject();
    }
}
