using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;

namespace JTest.Cli.DI;

public sealed class TypeRegistrar(IServiceCollection builder) : ITypeRegistrar
{
    public ITypeResolver Build()
    {
        return new TypeResolver(builder.BuildServiceProvider());
    }

    public void Register(Type service, Type implementation)
    {
        builder.AddSingleton(service, implementation);
    }

    internal TypeRegistrar Register<TService, TImplementation>()
    {
        Register(typeof(TService), typeof(TImplementation));
        return this;
    }

    internal TypeRegistrar Register<TImplementation>()
    {
        Register(typeof(TImplementation), typeof(TImplementation));
        return this;
    }

    public void RegisterInstance(Type service, object implementation)
    {
        RegisterInstance(service, implementation, factory: null);
    }

    internal TypeRegistrar RegisterInstance<TService>(Func<IServiceProvider, object> factory)
    {
        RegisterInstance(typeof(TService), implementation: null, factory);
        return this;
    }

    private void RegisterInstance(Type service, object? implementation, Func<IServiceProvider, object>? factory)
    {
        if (implementation is not null)
            builder.AddSingleton(service, implementation);
        else if (factory is not null)
            builder.AddSingleton(service, factory);
    }

    internal TypeRegistrar RegisterInstance<TService>(object implemmentation)
    {
        RegisterInstance(typeof(TService), implemmentation);
        return this;
    }

    public void RegisterLazy(Type service, Func<object> func)
    {
        ArgumentNullException.ThrowIfNull(func);

        builder.AddSingleton(service, (provider) => func());
    }
}
