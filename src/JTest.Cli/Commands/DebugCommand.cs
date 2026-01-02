using JTest.Cli.Services;
using JTest.Core.Execution;
using JTest.Core.Templates;
using JTest.Core.Utilities;
using JTest.Core.Variables;
using Spectre.Console;

namespace JTest.Cli.Commands;

public sealed class DebugCommand(
    IAnsiConsole ansiConsole,
    IErrorHandlingService errorHandlingService,
    IJTestSuiteExecutionResultProcessor resultsProcessor,
    IJTestSuiteExecutor testSuiteExecutor,
    IVariablesContext variablesContext,
    ITemplateContext templateContext,
    JsonSerializerOptionsAccessor jsonSerializerOptionsCache)
    : RunCommand(ansiConsole, errorHandlingService, resultsProcessor, testSuiteExecutor, variablesContext,
        templateContext, jsonSerializerOptionsCache)
{
    protected override bool IsDebug => true;
}
