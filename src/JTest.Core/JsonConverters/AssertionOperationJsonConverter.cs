using JTest.Core.Assertions;
using JTest.Core.TypeDescriptors;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace JTest.Core.JsonConverters;

public sealed class AssertionOperationJsonConverter(IServiceProvider serviceProvider) : JsonConverter<IAssertionOperation>
{
    private const string operationTypePropertyName = "op";

    public override void Write(Utf8JsonWriter writer, IAssertionOperation value, JsonSerializerOptions options)
    {
        var typeRegistry = serviceProvider
            .GetRequiredService<ITypeDescriptorRegistryProvider>()
            .AssertionTypeRegistry;

        writer.WriteStartObject();

        var typeIdentifier = typeRegistry.Identification.Identify(value.GetType());
        writer.WritePropertyName(operationTypePropertyName);
        writer.WriteStringValue(typeIdentifier);

        if (value.ActualValue is not null)
        {
            writer.WritePropertyName(GetPropertyName(nameof(value.ActualValue)));
            writer.WriteRawValue(
                JsonSerializer.SerializeToElement(value.ActualValue, options).GetRawText()
            );
        }

        if (value.ExpectedValue is not null)
        {
            writer.WritePropertyName(GetPropertyName(nameof(value.ExpectedValue)));
            writer.WriteRawValue(
                JsonSerializer.SerializeToElement(value.ExpectedValue, options).GetRawText()
            );
        }

        if (!string.IsNullOrWhiteSpace(value.Description))
        {
            writer.WritePropertyName(GetPropertyName(nameof(value.Description)));
            writer.WriteStringValue(value.Description);
        }

        if (value.Mask.HasValue)
        {
            writer.WritePropertyName(GetPropertyName(nameof(value.Mask)));
            writer.WriteBooleanValue(value.Mask == true);
        }

        writer.WriteEndObject();
    }

    public override IAssertionOperation? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (!JsonDocument.TryParseValue(ref reader, out var doc))
        {
            throw new JsonException("Failed to parse JsonDocument");
        }
        if (doc.RootElement.ValueKind != JsonValueKind.Object)
        {
            throw new JsonException($"Expected json object but got '{doc.RootElement.ValueKind}'");
        }

        var operationType = GetOperationType(doc.RootElement);

        var typeRegistry = serviceProvider
            .GetRequiredService<ITypeDescriptorRegistryProvider>()
            .AssertionTypeRegistry;

        var descriptor = typeRegistry.GetDescriptor(operationType);
        var arguments = CreateArguments(doc.RootElement);
        var result = descriptor.Constructor.Invoke(arguments);

        if (result is not IAssertionOperation assertionOperation)
        {
            throw new InvalidOperationException($"Assertion operation for type {operationType} cannot be constructed");
        }

        return assertionOperation;
    }

    private static string GetOperationType(JsonElement json)
    {
        var operationTypeProperty = json
            .EnumerateObject()
            .FirstOrDefault(x => x.Name.Equals(operationTypePropertyName, StringComparison.OrdinalIgnoreCase));

        if (operationTypeProperty.Value.ValueKind != JsonValueKind.String)
        {
            throw new JsonException($"Assertion is missing required string property '{operationTypePropertyName}'");
        }

        var result = operationTypeProperty.Value.GetString();
        if (string.IsNullOrWhiteSpace(result))
        {
            throw new JsonException($"Required property '{operationTypePropertyName}' is null or empty");
        }

        return result;
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
        if (name.Equals(nameof(IAssertionOperation.Description), StringComparison.OrdinalIgnoreCase))
        {
            if (value.ValueKind == JsonValueKind.String)
                return value.GetString() ?? string.Empty;

            return string.Empty;
        }

        if (name.Equals(nameof(IAssertionOperation.Mask), StringComparison.OrdinalIgnoreCase))
        {
            if (value.ValueKind is JsonValueKind.True or JsonValueKind.False)
                return value.GetBoolean();
            if (value.ValueKind == JsonValueKind.String && bool.TryParse(value.GetString()!, out var mask))
                return mask;

            return default(bool?);
        }

        return value;
    }

    private static string GetPropertyName(string name)
    {
        var result = name.ToCharArray();
        result[0] = char.ToLower(result[0]);
        return new(result);
    }
}
