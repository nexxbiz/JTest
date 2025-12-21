namespace JTest.Core.TypeDescriptors
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class TypeIdentifierAttribute(string id) : Attribute
    {
        public string Id { get; } = id;
    }
}
