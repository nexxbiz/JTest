namespace JTest.Core.TypeDescriptorRegistries
{
    public interface ITypeDescriptorRegistryProvider
    {
        ITypeDescriptorRegistry StepTypeRegistry { get; }
        ITypeDescriptorRegistry AssertionTypeRegistry { get; }
    }
}