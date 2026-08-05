using JTest.Core;
using JTest.Core.Assertions;
using JTest.Core.Execution;
using JTest.Core.Steps;
using JTest.Core.Templates;
using JTest.Core.TypeDescriptors;
using JTest.Core.Utilities;
using JTest.Core.Variables;
using Microsoft.Extensions.DependencyInjection;

namespace JTest.Cli.DI;

/// <summary>
/// Helper class to register JTest.Core services using Microsoft.Extensions.DependencyInjection.
/// </summary>
internal static class DependencyRegistrationHelper
{
    public static void RegisterCoreServices(IServiceCollection services)
    {
        // Core utilities
        services.AddSingleton<JsonSerializerOptionsAccessor>();
        services.AddSingleton<IGlobalConfigurationAccessor, GlobalConfigurationAccessor>();
        
        // Template and variable contexts
        services.AddSingleton<ITemplateContext, TemplateContext>();
        services.AddSingleton<IVariablesContext, VariablesContext>();
        
        // Type descriptors
        services.AddSingleton<ITypeDescriptorRegistryProvider, TypeDescriptorRegistryProvider>();
        
        // Validation
        services.AddScoped<IJTestSuiteValidator, JTestSuiteValidator>();
        
        // Execution services
        services.AddScoped<IAssertionProcessor, AssertionProcessor>();
        services.AddScoped<IStepProcessor, StepProcessor>();
        services.AddScoped<IJTestCaseExecutor, JTestCaseExecutor>();
        services.AddScoped<IJTestSuiteExecutor, JTestSuiteExecutor>();
        services.AddScoped<IJTestSuiteExecutionResultProcessor, JTestSuiteExecutionResultProcessor>();
    }
}
