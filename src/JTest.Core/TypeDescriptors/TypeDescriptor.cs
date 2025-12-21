namespace JTest.Core.TypeDescriptors;

public sealed record TypeDescriptor(
    Func<IEnumerable<TypeDescriptorConstructorArgument>, object> Constructor, 
    string TypeIdentifier, 
    Type Type,
    IEnumerable<TypeDescriptorConstructorParameter> ConstructorParameters
);
