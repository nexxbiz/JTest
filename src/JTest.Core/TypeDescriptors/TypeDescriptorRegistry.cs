using System.Reflection;

namespace JTest.Core.TypeDescriptors;

public sealed class TypeDescriptorRegistry<TInterfaceMarker> : ITypeDescriptorRegistry
{
    private readonly Lazy<Dictionary<string, TypeDescriptor>> getDescriptors;
    private readonly IServiceProvider serviceProvider;
    private readonly ITypeDescriptorIdentification descriptorIdentification;
    private Dictionary<string, TypeDescriptor>? descriptors;

    public Type InterfaceMarkerType => typeof(TInterfaceMarker);

    public ITypeDescriptorIdentification Identification => descriptorIdentification;

    public TypeDescriptorRegistry(IServiceProvider serviceProvider, ITypeDescriptorIdentification descriptorIdentification)
        : this(typeof(TypeDescriptorRegistry<>).Assembly, serviceProvider, descriptorIdentification)
    {
    }

    public TypeDescriptorRegistry(Assembly assembly, IServiceProvider serviceProvider, ITypeDescriptorIdentification descriptorIdentification)
        : this(assembly.GetTypes(), serviceProvider, descriptorIdentification)
    {
    }

    public TypeDescriptorRegistry(Type[] types, IServiceProvider serviceProvider, ITypeDescriptorIdentification descriptorIdentification)
    {
        this.serviceProvider = serviceProvider;
        this.descriptorIdentification = descriptorIdentification;
        getDescriptors = new(() => GetTypeDescriptors(types));
    }

    public void RegisterTypes(params Type[] types)
    {
        var registeredDescriptors = GetDescriptors();
        var mergedDescriptors = new Dictionary<string, TypeDescriptor>(registeredDescriptors, StringComparer.OrdinalIgnoreCase);

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

        var typeIdentifier = descriptorIdentification.Identify(type);

        return new TypeDescriptor(
            CreateConstructor(constructor),
            typeIdentifier,
            type,
            constructor.GetParameters().Select(x => new TypeDescriptorConstructorParameter(x.Name!, x.ParameterType))
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

    private object?[] GetArguments(ConstructorInfo constructor, IEnumerable<TypeDescriptorConstructorArgument> arguments)
    {
        var parameters = constructor.GetParameters();
        var args = new object?[parameters.Length];

        for (var i = 0; i < args.Length; i++)
        {
            var param = parameters[i];
            var arg = arguments.FirstOrDefault(x => x.Name == param.Name)?.Value;
            arg ??= serviceProvider.GetService(param.ParameterType);

            args[i] = arg;
        }

        return args;
    }
}
