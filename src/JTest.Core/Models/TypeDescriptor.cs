namespace JTest.Core.Models;

public sealed record TypeDescriptor(Func<IEnumerable<TypeDescriptorConstructorArgument>, object> Constructor, string TypeIdentifier, Type Type);
