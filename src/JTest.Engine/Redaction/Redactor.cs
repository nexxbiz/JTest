using System.Text.Json;
using System.Text.Json.Nodes;

namespace JTest.Engine.Redaction;

/// <summary>
/// Static capture-time redaction: secret values never enter the trace, so
/// no projection can leak them. Redaction applies to evidence only —
/// execution keeps real values in context.
/// </summary>
public static class Redactor
{
    /// <summary>Returns a redacted deep clone of one value.</summary>
    /// <param name="value">The value to clone and redact.</param>
    /// <param name="secrets">The run's secret set.</param>
    public static JsonNode? Redact(JsonNode? value, SecretSet secrets)
    {
        switch (value)
        {
            case null:
                return null;

            case JsonObject jsonObject:
                var redactedObject = new JsonObject();
                foreach (var property in jsonObject)
                {
                    redactedObject[property.Key] = Redact(property.Value, secrets);
                }

                return redactedObject;

            case JsonArray jsonArray:
                var redactedArray = new JsonArray();
                foreach (var item in jsonArray)
                {
                    redactedArray.Add(Redact(item, secrets));
                }

                return redactedArray;

            default:
                if (value.GetValueKind() == JsonValueKind.String)
                {
                    return JsonValue.Create(RedactText(value.GetValue<string>(), secrets));
                }

                return value.DeepClone();
        }
    }

    /// <summary>Replaces every secret occurrence inside one string.</summary>
    /// <param name="text">The text to filter.</param>
    /// <param name="secrets">The run's secret set.</param>
    public static string RedactText(string text, SecretSet secrets)
    {
        var result = text;
        foreach (var (secret, marker) in secrets.Ordered)
        {
            result = result.Replace(secret, marker, StringComparison.Ordinal);
        }

        return result;
    }

    /// <summary>Redacts one header value: credential headers are always fully masked.</summary>
    /// <param name="headerName">The header name.</param>
    /// <param name="headerValue">The header value.</param>
    /// <param name="secrets">The run's secret set.</param>
    public static string RedactHeader(string headerName, string headerValue, SecretSet secrets) =>
        SecretSet.CredentialHeaders.Contains(headerName)
            ? SecretSet.MarkerFor(headerValue)
            : RedactText(headerValue, secrets);
}
