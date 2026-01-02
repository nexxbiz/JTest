namespace JTest.Core.Exceptions;

public sealed class JsonPathValueNotFoundException(string path) : Exception($"Could not find value at path '{path}'")
{
    public string Path { get; } = path;
}
