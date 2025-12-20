using JTest.Core.Assertions;
using JTest.Core.Models;
using JTest.Core.Steps;
using System.Reflection;

namespace JTest.Core;

public interface ITypeDescriptorRegistry
{
    void RegisterTypesFromAssembly(Assembly assembly);

    IReadOnlyDictionary<string, TypeDescriptor> GetDescriptors();

    TypeDescriptor GetDescriptor(string typeIdentifier);

    Type InterfaceMarkerType { get; }

    static ITypeDescriptorRegistry CreateStepRegistry(IServiceProvider serviceProvider) => new TypeDescriptorRegistry<IStep>(serviceProvider, nameof(IStep.Type));
    static ITypeDescriptorRegistry CreateAssertionRegistry(IServiceProvider serviceProvider) => new TypeDescriptorRegistry<IAssertionOperation>(serviceProvider, nameof(IAssertionOperation.OperationType));
}
