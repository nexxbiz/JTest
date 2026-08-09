using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace JTest.Core.JsonConverters;

/// <summary>
/// Reads a JSON object whose values may be strings, numbers, booleans or null into a
/// <c>string</c>-valued map — used for a step's <c>query</c>.
///
/// `{ "query": { "take": 1 } }` is what an author naturally writes, and it is unambiguous: a query
/// string carries text, so a number means the text "1". Refusing it produced a raw deserialization
/// crash at run time on a file that `jtest validate` had already called valid. Anything without an
/// obvious textual form (an object or array) is still rejected, but with a message that names the key.
/// </summary>
public sealed class ScalarStringMapJsonConverter : JsonConverter<IReadOnlyDictionary<string, string>>
{
    public override IReadOnlyDictionary<string, string> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException($"Expected an object of key/value pairs but got '{reader.TokenType}'.");
        }

        var result = new Dictionary<string, string>();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                return result;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException($"Expected a property name but got '{reader.TokenType}'.");
            }

            var key = reader.GetString()!;
            reader.Read();
            result[key] = ReadScalar(ref reader, key);
        }

        throw new JsonException("Unexpected end of JSON while reading a key/value map.");
    }

    /// <summary>Converts a scalar to its textual form; anything else is an error naming the key.</summary>
    internal static string ReadScalar(ref Utf8JsonReader reader, string key)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.String:
                return reader.GetString() ?? string.Empty;

            case JsonTokenType.Number:
                return reader.TryGetInt64(out var whole)
                    ? whole.ToString(CultureInfo.InvariantCulture)
                    : reader.GetDouble().ToString(CultureInfo.InvariantCulture);

            case JsonTokenType.True:
            case JsonTokenType.False:
                return reader.GetBoolean() ? "true" : "false";

            case JsonTokenType.Null:
                return string.Empty;

            default:
                throw new JsonException(
                    $"Value of '{key}' must be a string, number or boolean; got '{reader.TokenType}'. " +
                    "Objects and arrays have no unambiguous text form here.");
        }
    }

    public override void Write(Utf8JsonWriter writer, IReadOnlyDictionary<string, string> value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        foreach (var (key, item) in value)
        {
            writer.WritePropertyName(key);
            writer.WriteStringValue(item);
        }
        writer.WriteEndObject();
    }
}
