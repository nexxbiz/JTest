using JTest.Core.Models;
using JTest.Core.Steps;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace JTest.Core.JsonConverters;

public sealed class StepJsonConverter(IServiceProvider serviceProvider) : JsonConverter<IStep>
{
    public override IStep? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (!JsonDocument.TryParseValue(ref reader, out var doc))
        {
            throw new JsonException("Failed to parse JsonDocument");
        }

        if (!doc.RootElement.TryGetProperty("type", out var stepType) || stepType.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(stepType.GetString()))
        {
            throw new JsonException("Failed to extract step type property");
        }

        var typeRegistry = serviceProvider
            .GetRequiredService<IEnumerable<ITypeDescriptorRegistry>>()
            .FirstOrDefault(r => r.InterfaceMarkerType == typeof(IStep))
            ?? throw new JsonException("Failed to create step; type registry is not configured");

        var descriptor = typeRegistry.GetDescriptor(stepType.GetString()!);

        var configurationType = GetConfigurationType(descriptor.Type);
        var configurationJson = JsonSerializer.Serialize(doc.RootElement, options);
        var configuration = JsonSerializer.Deserialize(configurationJson, JsonTypeInfo.CreateJsonTypeInfo(configurationType, options));

        var constructorArguments = new TypeDescriptorConstructorArgument[]
        {
            new("configuration", configuration)
        };
        var result = descriptor.Constructor.Invoke(constructorArguments);

        return (IStep)result;
    }

    private static Type GetConfigurationType(Type type)
    {
        if (type.BaseType is null)
        {
            throw new InvalidProgramException($"Base type of '{type.FullName}' is null");
        }

        if (type.BaseType.IsGenericType && type.BaseType.GetGenericTypeDefinition() == typeof(BaseStep<>))
        {
            return type.BaseType.GetGenericArguments()[0];
        }

        return GetConfigurationType(type.BaseType);
    }

    public override void Write(Utf8JsonWriter writer, IStep value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        writer.WritePropertyName("type");
        writer.WriteStringValue(value.Type);

        var properties = value.Configuration?
            .GetType()
            .GetProperties(BindingFlags.Public)
            ?? [];

        foreach (var property in properties)
        {
            writer.WritePropertyName(
                GetPropertyName(property.Name)
            );
            var propertyValue = property.GetValue(value.Configuration);
            writer.WriteRawValue(
                JsonSerializer.SerializeToElement(propertyValue).GetRawText()
            );
        }

        writer.WriteEndObject();
    }

    private static string GetPropertyName(string name)
    {
        var result = name.ToCharArray();
        result[0] = char.ToLower(result[0]);
        return new(result);
    }
}
