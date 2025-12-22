namespace JTest.Core.TypeDescriptors;

public interface ITypeDescriptorRegistryProvider
{
    ITypeDescriptorRegistry StepTypeRegistry { get; }
    ITypeDescriptorRegistry AssertionTypeRegistry { get; }
}