namespace JTest.Core.TypeDescriptors
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class IdentifierAttribute(string id) : Attribute
    {
        public string Id { get; } = id;
    }
}
