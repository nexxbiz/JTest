using JTest.Core.JsonConverters;
using System.Text.Json;

namespace JTest.Core.Utilities;

public sealed class JsonSerializerOptionsCache
{
    public static readonly JsonSerializerOptions Default = GetDefaultOptions();

    private readonly Lazy<JsonSerializerOptions> options;
    private readonly IServiceProvider serviceProvider;

    public JsonSerializerOptionsCache(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
        options = new(Build);
    }

    public JsonSerializerOptions Options => options.Value;


    private static JsonSerializerOptions GetDefaultOptions()
    {
        var result = new JsonSerializerOptions()
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };

        result.Converters.Add(
            new ReadOnlyDictionaryJsonConverter<string>()
        );
        result.Converters.Add(
            new ReadOnlyDictionaryJsonConverter<object>()
        );

        return result;
    }

    private JsonSerializerOptions Build()
    {
        var result = GetDefaultOptions();

        result.Converters.Add(
            new StepJsonConverter(serviceProvider)
        );
        result.Converters.Add(
            new AssertionOperationJsonConverter(serviceProvider)
        );

        return result;
    }
}
