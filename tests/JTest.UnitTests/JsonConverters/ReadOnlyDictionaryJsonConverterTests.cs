using JTest.Core.JsonConverters;
using System.Text.Json;

namespace JTest.UnitTests.JsonConverters;

public sealed class ReadOnlyDictionaryJsonConverterTests
{
    private readonly JsonSerializerOptions options = new();

    public ReadOnlyDictionaryJsonConverterTests()
    {
        options.Converters.Add(
            new ReadOnlyDictionaryJsonConverter<string>()
        );
        options.Converters.Add(
            new ReadOnlyDictionaryJsonConverter<object>()
        );
    }

    [Fact]
    public void When_ReadStringDictionary_Then_Deserializes()
    {
        // Arrange
        const string json = "{\"key1\": \"value1\" }";

        // Act
        var result = JsonSerializer.Deserialize<IReadOnlyDictionary<string, string>>(json, options);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.ContainsKey("key1"));
        Assert.Equal("value1", result["key1"]);
    }

    [Fact]
    public void When_ReadObjectDictionary_Then_Deserializes()
    {
        // Arrange
        const string json = "{\"key1\": \"value1\", \"key2\": 12.23 }";

        // Act
        var result = JsonSerializer.Deserialize<IReadOnlyDictionary<string, object?>>(json, options);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.ContainsKey("key1"));
        Assert.Equal("value1", $"{result["key1"]}");
        Assert.True(result.ContainsKey("key2"));
        var numericValue = ((JsonElement)result["key2"]!).GetDouble();
        Assert.Equal(12.23, numericValue);
    }
}
