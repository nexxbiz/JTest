using JTest.Core.Assertions;
using JTest.Core.Steps;

namespace JTest.Core.TypeDescriptors;

public sealed class TypeDescriptorRegistryProvider(IServiceProvider serviceProvider) : ITypeDescriptorRegistryProvider
{
    private readonly Lazy<ITypeDescriptorRegistry> stepTypeDescriptorRegistry =
        new(() => new TypeDescriptorRegistry<IStep>(serviceProvider, new StepTypeDescriptorIdentification()));

    private readonly Lazy<ITypeDescriptorRegistry> assertionTypeDescriptorRegistry =
        new(() => new TypeDescriptorRegistry<IAssertionOperation>(serviceProvider, new AssertionTypeDescriptorIdentification()));

    public ITypeDescriptorRegistry StepTypeRegistry => stepTypeDescriptorRegistry.Value;
    public ITypeDescriptorRegistry AssertionTypeRegistry => assertionTypeDescriptorRegistry.Value;
}
