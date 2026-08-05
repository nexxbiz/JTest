using JTest.Cli.Services;
using JTest.Core;
using JTest.Core.Assertions;
using JTest.Core.Execution;
using JTest.Core.Steps;
using JTest.Core.Templates;
using JTest.Core.TypeDescriptors;
using JTest.Core.Utilities;
using JTest.Core.Variables;
using Spectre.Console;

namespace JTest.Cli.DI;

/// <summary>
/// Handles dependency injection container configuration for the CLI application.
/// </summary>
internal static class DependencyRegistration
{
    public static void ConfigureServices(TypeRegistrar typeRegistrar)
    {
        RegisterCoreServices(typeRegistrar);
        RegisterExecutionServices(typeRegistrar);
    }


    private static void RegisterCoreServices(TypeRegistrar typeRegistrar)
    {
        typeRegistrar
            .Register<JsonSerializerOptionsAccessor>()
            .Register<IGlobalConfigurationAccessor, GlobalConfigurationAccessor>()
            .Register<ITemplateContext, TemplateContext>()
            .Register<IVariablesContext, VariablesContext>()
            .Register<ITypeDescriptorRegistryProvider, TypeDescriptorRegistryProvider>()
            .Register<IJTestSuiteValidator, JTestSuiteValidator>();
    }

    private static void RegisterExecutionServices(TypeRegistrar typeRegistrar)
    {
        typeRegistrar
            .Register<IAssertionProcessor, AssertionProcessor>()
            .Register<IStepProcessor, StepProcessor>()
            .Register<IJTestCaseExecutor, JTestCaseExecutor>()
            .Register<IJTestSuiteExecutor, JTestSuiteExecutor>()
            .Register<IJTestSuiteExecutionResultProcessor, JTestSuiteExecutionResultProcessor>();
    }
}
