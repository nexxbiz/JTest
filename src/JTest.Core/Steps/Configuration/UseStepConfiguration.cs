using JTest.Core.Assertions;
using JTest.Core.Execution;
using JTest.Core.Templates;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json.Serialization;

namespace JTest.Core.Steps.Configuration;

public sealed record UseStepConfiguration(
    [property: JsonRequired] string Template, 
    IReadOnlyDictionary<string, object?>? With, 
    string? Id = null, 
    string? Name = null, 
    string? Description = null, 
    IEnumerable<IAssertionOperation>? Assert = null, 
    IReadOnlyDictionary<string, object?>? Save = null
)

    : StepConfigurationBase;
