using System.Text.Json;
using System.Text.Json.Nodes;

namespace JTest.Engine.Expressions;

/// <summary>Static conversion from immutable elements to mutable nodes.</summary>
public static class JsonElementNodes
{
    /// <summary>Converts one element into an unparented node tree.</summary>
    /// <param name="element">The element to convert.</param>
    public static JsonNode? ToNode(JsonElement element) =>
        element.ValueKind == JsonValueKind.Null ? null : JsonNode.Parse(element.GetRawText());
}
