using JTest.Core.Models;
using System.Reflection;

namespace JTest.Core;

public interface ITypeDescriptorRegistry
{
    void RegisterTypes(params Type[] types);

    IReadOnlyDictionary<string, TypeDescriptor> GetDescriptors();

    TypeDescriptor GetDescriptor(string typeIdentifier);

    Type InterfaceMarkerType { get; }    
}

public static class TypeDescriptorRegistryExtensions
{
    public static void RegisterTypesFromAssembly(this ITypeDescriptorRegistry registry, Assembly assembly)
    {
        registry.RegisterTypes(assembly.GetTypes());
    }
}
