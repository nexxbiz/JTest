using JTest.Core.Assertions;
using JTest.Core.Steps;
using JTest.Core.TypeDescriptorRegistries;
using JTest.Core.TypeDescriptors;
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
        if(doc.RootElement.ValueKind != JsonValueKind.Object)
        {
            throw new JsonException($"Expected json object but got '{doc.RootElement.ValueKind}'");
        }

        var stepType = GetStepType(doc.RootElement);

        var typeRegistry = serviceProvider
            .GetRequiredService<ITypeDescriptorRegistryProvider>()
            .StepTypeRegistry;

        var descriptor = typeRegistry.GetDescriptor(stepType);

        var configurationType = GetConfigurationType(descriptor.Type);
        var configurationJson = doc.RootElement.GetRawText();        
        var configuration = JsonSerializer.Deserialize(configurationJson, configurationType, options);

        var constructorArguments = new TypeDescriptorConstructorArgument[]
        {
            new("configuration", configuration)
        };
        var result = descriptor.Constructor.Invoke(constructorArguments);

        if (result is not IStep step)
        {
            throw new InvalidOperationException($"Step for type {stepType} cannot be constructed");
        }

        return step;
    }

    private static string GetStepType(JsonElement json)
    {
        var stepTypeProperty = json
            .EnumerateObject()
            .FirstOrDefault(x => x.Name.Equals("type", StringComparison.OrdinalIgnoreCase));

        if(stepTypeProperty.Value.ValueKind != JsonValueKind.String)
        {
            throw new JsonException("Step is missing required string property 'type'");
        }

        var result = stepTypeProperty.Value.GetString();
        if(string.IsNullOrWhiteSpace(result))
        {
            throw new JsonException("Required property 'type' is null or empty");
        }

        return result;
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

        var typeRegistry = serviceProvider
            .GetRequiredService<ITypeDescriptorRegistryProvider>()
            .StepTypeRegistry;

        var typeIdentifier = typeRegistry.Identification.Identify(value.GetType());
        writer.WritePropertyName("type");
        writer.WriteStringValue(typeIdentifier);

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
