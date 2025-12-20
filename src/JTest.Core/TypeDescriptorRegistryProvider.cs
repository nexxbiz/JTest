using JTest.Core.Assertions;
using JTest.Core.Steps;

namespace JTest.Core;

public sealed class TypeDescriptorRegistryProvider(IServiceProvider serviceProvider)
{
    private readonly Lazy<ITypeDescriptorRegistry> stepTypeDescriptorRegistry = new(() => new TypeDescriptorRegistry<IStep>(serviceProvider, nameof(IStep.Type)));
    private readonly Lazy<ITypeDescriptorRegistry> assertionTypeDescriptorRegistry = new(() => new TypeDescriptorRegistry<IAssertionOperation>(serviceProvider, nameof(IAssertionOperation.OperationType)));

    public ITypeDescriptorRegistry StepTypeRegistry => stepTypeDescriptorRegistry.Value;
    public ITypeDescriptorRegistry AssertionTypeRegistry => assertionTypeDescriptorRegistry.Value;
}
