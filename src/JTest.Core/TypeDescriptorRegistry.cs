using JTest.Core.Models;
using System.Reflection;

namespace JTest.Core;

public sealed class TypeDescriptorRegistry<TInterfaceMarker> : ITypeDescriptorRegistry
{
    private readonly Lazy<Dictionary<string, TypeDescriptor>> getDescriptors;
    private readonly IServiceProvider serviceProvider;
    private readonly string typeIdentifierPropertyName;
    private Dictionary<string, TypeDescriptor>? descriptors;

    public Type InterfaceMarkerType => typeof(TInterfaceMarker);

    public TypeDescriptorRegistry(IServiceProvider serviceProvider, string typeIdentifierPropertyName)
        : this(typeof(TypeDescriptorRegistry<>).Assembly, serviceProvider, typeIdentifierPropertyName)
    {
    }

    public TypeDescriptorRegistry(Assembly assembly, IServiceProvider serviceProvider, string typeIdentifierPropertyName)
        : this(assembly.GetTypes(), serviceProvider, typeIdentifierPropertyName)
    {
    }

    public TypeDescriptorRegistry(Type[] types, IServiceProvider serviceProvider, string typeIdentifierPropertyName)
    {
        this.serviceProvider = serviceProvider;
        this.typeIdentifierPropertyName = typeIdentifierPropertyName;
        getDescriptors = new(() => GetTypeDescriptors(types));
    }

    public void RegisterTypes(params Type[] types)
    {
        var registeredDescriptors = GetDescriptors();
        var mergedDescriptors = new Dictionary<string, TypeDescriptor>(registeredDescriptors);

        var descriptorsToAdd = GetTypeDescriptors(types);

        foreach (var descriptor in descriptorsToAdd)
        {
            mergedDescriptors.Add(descriptor.Key, descriptor.Value);
        }

        descriptors = mergedDescriptors;
    }

    public IReadOnlyDictionary<string, TypeDescriptor> GetDescriptors()
    {
        descriptors ??= getDescriptors.Value;
        return descriptors.AsReadOnly();
    }

    public TypeDescriptor GetDescriptor(string typeIdentifier)
    {
        var dictionary = GetDescriptors();
        if (dictionary.TryGetValue(typeIdentifier, out var result))
        {
            return result;
        }

        throw new InvalidOperationException($"Type with identifier '{typeIdentifier}' is not registered");
    }

    private Dictionary<string, TypeDescriptor> GetTypeDescriptors(Type[] types)
    {
        var markerInterfaceName = typeof(TInterfaceMarker).Name;
        var descriptors = types
            .Where(type => !type.IsAbstract && type.GetInterface(markerInterfaceName) != null)
            .Select(CreateDescriptor);

        return descriptors.ToDictionary(x => x.TypeIdentifier, x => x, StringComparer.OrdinalIgnoreCase);
    }

    private TypeDescriptor CreateDescriptor(Type type)
    {
        var constructors = type
            .GetConstructors(BindingFlags.Public | BindingFlags.Instance);
        var constructor = constructors
            .Where(x => x.GetParameters().Length > 0)
            .FirstOrDefault()
            ?? throw new InvalidOperationException($"No constructor found with parameters on type '{type.FullName}'. Please make sure the type has 1 constructor defined with parameters");

        var instance = Instantiate(constructor);
        var typeIdentifier = GetTypeIdentifier(instance);

        return new TypeDescriptor(
            CreateConstructor(constructor),
            typeIdentifier,
            type
        );
    }

    private Func<IEnumerable<TypeDescriptorConstructorArgument>, object> CreateConstructor(ConstructorInfo constructor)
    {
        return (arguments) =>
        {
            var args = GetArguments(constructor, arguments);
            return constructor.Invoke(args);
        };
    }

    private object[] GetArguments(ConstructorInfo constructor, IEnumerable<TypeDescriptorConstructorArgument> arguments)
    {
        var parameters = constructor.GetParameters();
        var args = new object[parameters.Length];

        for (var i = 0; i < args.Length; i++)
        {
            var param = parameters[i];
            var arg = arguments.FirstOrDefault(x => x.Name == param.Name)?.Value;
            arg ??= serviceProvider.GetService(param.ParameterType);

            if (arg is null)
            {
                throw new InvalidOperationException($"Cannot construct assertion; constructor argument '{param.Name}' cannot be resolved.");
            }

            args[i] = arg;
        }

        return args;
    }

    private string GetTypeIdentifier(object instance)
    {
        var typeIdentifierProperty = instance
            .GetType()
            .GetProperty(typeIdentifierPropertyName, BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException($"Could not find type identifier property '{typeIdentifierPropertyName}' on type '{instance.GetType().FullName}'");

        var result = typeIdentifierProperty.GetValue(instance);
        if (result is not string stringResult)
        {
            throw new InvalidOperationException($"Property '{typeIdentifierPropertyName}' on type '{instance.GetType().FullName}' is not of type string");
        }

        return stringResult;
    }

    private static object Instantiate(ConstructorInfo constructor)
    {
        var parameters = Enumerable
            .Range(0, constructor.GetParameters().Length)
            .Select(x => default(object))
            .ToArray();

        return constructor.Invoke(parameters);
    }
}
