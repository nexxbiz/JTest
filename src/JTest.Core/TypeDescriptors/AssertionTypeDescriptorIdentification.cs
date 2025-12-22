using System.Reflection;

namespace JTest.Core.TypeDescriptors;

public sealed class AssertionTypeDescriptorIdentification : ITypeDescriptorIdentification
{
    public string Identify(Type type)
    {
        if (HasIdentifierAttribute(type, out var id))
        {
            return id;
        }

        var result = type.Name
            .Replace("Assertion", string.Empty)
            .ToLowerInvariant();

        return result;
    }

    private static bool HasIdentifierAttribute(Type type, out string id)
    {
        var attribute = type.GetCustomAttribute<TypeIdentifierAttribute>();
        if (attribute is null)
        {
            id = string.Empty;
            return false;
        }

        id = attribute.Id;
        return true;
    }
}
