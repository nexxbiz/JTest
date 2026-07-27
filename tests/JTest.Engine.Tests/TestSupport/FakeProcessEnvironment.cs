using JTest.Engine.Ports;

namespace JTest.Engine.Tests.TestSupport;

/// <summary>Scripted process environment.</summary>
internal sealed class FakeProcessEnvironment : IProcessEnvironment
{
    private readonly Dictionary<string, string> values = new(StringComparer.Ordinal);

    internal FakeProcessEnvironment With(string name, string value)
    {
        values[name] = value;
        return this;
    }

    public string? GetValue(string name) => values.GetValueOrDefault(name);
}
