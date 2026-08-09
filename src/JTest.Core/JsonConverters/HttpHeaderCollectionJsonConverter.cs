using JTest.Core.Steps.Configuration;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace JTest.Core.JsonConverters;

/// <summary>
/// Reads a step's request <c>headers</c> in either shape:
///
/// <code>
/// "headers": { "X-Token": "abc", "X-Count": 5 }                 // object map (natural, and how the
///                                                               // response contract exposes headers)
/// "headers": [ { "name": "X-Token", "value": "abc" } ]          // explicit array
/// </code>
///
/// Only the array form was accepted, so the obvious authoring shape failed at load with a raw
/// deserialization error on a file <c>jtest validate</c> had called valid. Scalar values are coerced
/// to text, exactly as for <c>query</c> — a header carries text, so <c>5</c> means "5".
/// </summary>
public sealed class HttpHeaderCollectionJsonConverter : JsonConverter<IEnumerable<HttpStepRequestHeaderConfiguration>>
{
    public override IEnumerable<HttpStepRequestHeaderConfiguration> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.StartObject => ReadObjectMap(ref reader),
            JsonTokenType.StartArray => ReadArray(ref reader, options),
            _ => throw new JsonException(
                $"'headers' must be an object of name/value pairs or an array of {{ name, value }} objects; got '{reader.TokenType}'.")
        };
    }

    private static List<HttpStepRequestHeaderConfiguration> ReadObjectMap(ref Utf8JsonReader reader)
    {
        var result = new List<HttpStepRequestHeaderConfiguration>();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                return result;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException($"Expected a header name but got '{reader.TokenType}'.");
            }

            var name = reader.GetString()!;
            reader.Read();
            result.Add(new HttpStepRequestHeaderConfiguration(name, ScalarStringMapJsonConverter.ReadScalar(ref reader, name)));
        }

        throw new JsonException("Unexpected end of JSON while reading 'headers'.");
    }

    private static List<HttpStepRequestHeaderConfiguration> ReadArray(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        var result = new List<HttpStepRequestHeaderConfiguration>();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
            {
                return result;
            }

            var header = JsonSerializer.Deserialize<HttpStepRequestHeaderConfiguration>(ref reader, WithoutThisConverter(options))
                ?? throw new JsonException("A header entry must be an object with 'name' and 'value'.");

            result.Add(header);
        }

        throw new JsonException("Unexpected end of JSON while reading 'headers'.");
    }

    /// <summary>Deserialize entries with the ambient options minus this converter, avoiding recursion.</summary>
    private static JsonSerializerOptions WithoutThisConverter(JsonSerializerOptions options)
    {
        var copy = new JsonSerializerOptions(options);
        for (var i = copy.Converters.Count - 1; i >= 0; i--)
        {
            if (copy.Converters[i] is HttpHeaderCollectionJsonConverter)
                copy.Converters.RemoveAt(i);
        }
        return copy;
    }

    public override void Write(Utf8JsonWriter writer, IEnumerable<HttpStepRequestHeaderConfiguration> value, JsonSerializerOptions options)
    {
        // Round-trips as the object map, the documented shape.
        writer.WriteStartObject();
        foreach (var header in value)
        {
            writer.WritePropertyName(header.Name);
            writer.WriteStringValue(header.Value);
        }
        writer.WriteEndObject();
    }
}
