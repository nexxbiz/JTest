using JTest.Core.Assertions;
using JTest.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace JTest.Core.JsonConverters;

public sealed class AssertionOperationJsonConverter(IServiceProvider serviceProvider) : JsonConverter<IAssertionOperation>
{
    public override void Write(Utf8JsonWriter writer, IAssertionOperation value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        writer.WritePropertyName("op");
        writer.WriteStringValue(value.OperationType);
        writer.WritePropertyName("actualValue");
        writer.WriteRawValue(
            JsonSerializer.SerializeToElement(value.ActualValue).GetRawText()
        );
        writer.WritePropertyName("expectedValue");
        writer.WriteRawValue(
            JsonSerializer.SerializeToElement(value.ExpectedValue).GetRawText()
        );
        writer.WritePropertyName("description");
        writer.WriteStringValue(value.Description);
        writer.WritePropertyName("mask");
        writer.WriteBooleanValue(value.Mask == true);

        writer.WriteEndObject();
    }

    public override IAssertionOperation? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (!JsonDocument.TryParseValue(ref reader, out var doc))
        {
            throw new JsonException("Failed to parse JsonDocument");
        }

        if (!doc.RootElement.TryGetProperty("op", out var operationType) || operationType.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(operationType.GetString()))
        {
            throw new JsonException("Failed to extract operation type property");
        }

        var typeRegistry = serviceProvider
            .GetRequiredService<TypeDescriptorRegistryProvider>()
            .AssertionTypeRegistry;

        var descriptor = typeRegistry.GetDescriptor(operationType.GetString()!);
        var arguments = CreateArguments(doc.RootElement);
        var result = descriptor.Constructor.Invoke(arguments);

        if (result is not IAssertionOperation assertionOperation)
        {
            throw new JsonException($"Assertion operation for type {operationType} cannot be constructed");
        }

        return assertionOperation;
    }

    private static IEnumerable<TypeDescriptorConstructorArgument> CreateArguments(JsonElement root)
    {
        foreach (var property in root.EnumerateObject())
        {
            yield return new(
                property.Name,
                GetArgumentValue(property.Name, property.Value)
            );
        }
    }

    private static object? GetArgumentValue(string name, JsonElement value)
    {
        if (name.Equals("description", StringComparison.OrdinalIgnoreCase))
        {
            if (value.ValueKind == JsonValueKind.String)
                return value.GetString() ?? string.Empty;

            return string.Empty;
        }

        if (name.Equals("mask", StringComparison.OrdinalIgnoreCase))
        {
            if (value.ValueKind is JsonValueKind.True or JsonValueKind.False)
                return value.GetBoolean();
            if (value.ValueKind == JsonValueKind.String && bool.TryParse(value.GetString()!, out var mask))
                return mask;

            return default(bool?);
        }

        return value;
    }
}
