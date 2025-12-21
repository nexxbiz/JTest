using JTest.Core.Assertions;
using JTest.Core.Steps;
using JTest.Core.Steps.Configuration;
using JTest.Core.TypeDescriptorRegistries;
using JTest.Core.TypeDescriptors;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace JTest.Core.JsonConverters;

public sealed class StepJsonConverter(IServiceProvider serviceProvider) : JsonConverter<IStep>
{
    private const string typePropertyName = "type";

    public override IStep? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (!JsonDocument.TryParseValue(ref reader, out var doc))
        {
            throw new JsonException("Failed to parse JsonDocument");
        }
        if (doc.RootElement.ValueKind != JsonValueKind.Object)
        {
            throw new JsonException($"Expected json object but got '{doc.RootElement.ValueKind}'");
        }

        var stepType = GetStepType(doc.RootElement);

        var typeRegistry = serviceProvider
            .GetRequiredService<ITypeDescriptorRegistryProvider>()
            .StepTypeRegistry;

        var descriptor = typeRegistry.GetDescriptor(stepType);
        var result = ConstructStepInstance(doc.RootElement, descriptor, options);

        if (result is not IStep step)
        {
            throw new InvalidOperationException("Could not construct instance of step");
        }

        return step;
    }

    private static object ConstructStepInstance(JsonElement root, TypeDescriptor descriptor, JsonSerializerOptions options)
    {
        var configurationTypeParameter = GetStepConfigurationParameter(descriptor);
        var configurationJson = root.GetRawText();
        var configuration = JsonSerializer.Deserialize(configurationJson, configurationTypeParameter.Type, options);

        return descriptor.Constructor.Invoke(
            [new(configurationTypeParameter.Name, configuration)]
        );
    }

    private static TypeDescriptorConstructorParameter GetStepConfigurationParameter(TypeDescriptor descriptor)
    {
        var errorMessage = $"Step does not have configuration constructor parameter. " +
            $"Please make sure you define a constructor with a parameter of type '{typeof(StepConfiguration).FullName}', " +
            $"or a parameter that derives from '{typeof(StepConfiguration).FullName}'";

        return descriptor.ConstructorParameters.FirstOrDefault(x => typeof(StepConfiguration).IsAssignableFrom(x.Type))
            ?? throw new InvalidOperationException(errorMessage);
    }

    private static string GetStepType(JsonElement json)
    {
        var stepTypeProperty = json
            .EnumerateObject()
            .FirstOrDefault(x => x.Name.Equals(typePropertyName, StringComparison.OrdinalIgnoreCase));

        if (stepTypeProperty.Value.ValueKind != JsonValueKind.String)
        {
            throw new JsonException($"Step is missing required string property '{typePropertyName}'");
        }

        var result = stepTypeProperty.Value.GetString();
        if (string.IsNullOrWhiteSpace(result))
        {
            throw new JsonException($"Required property '{typePropertyName}' is null or empty");
        }

        return result;
    }

    public override void Write(Utf8JsonWriter writer, IStep value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        var typeRegistry = serviceProvider
            .GetRequiredService<ITypeDescriptorRegistryProvider>()
            .StepTypeRegistry;

        var typeIdentifier = typeRegistry.Identification.Identify(value.GetType());
        writer.WritePropertyName(typePropertyName);
        writer.WriteStringValue(typeIdentifier);

        var properties = value.Configuration?
            .GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            ?? [];

        foreach (var property in properties)
        {
            writer.WritePropertyName(
                GetPropertyName(property.Name)
            );
            var propertyValue = property.GetValue(value.Configuration);
            writer.WriteRawValue(
                JsonSerializer.SerializeToElement(propertyValue, options).GetRawText()
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
