using JTest.Core.Execution;
using JTest.Core.Templates;
using JTest.Core.Utilities;
using JTest.Core.Variables;
using Spectre.Console;

namespace JTest.Cli.Commands;

public sealed class DebugCommand(IAnsiConsole ansiConsole, IJTestSuiteExecutionResultProcessor resultsProcessor, IJTestSuiteExecutor testSuiteExecutor, IVariablesContext variablesContext, ITemplateContext templateContext, JsonSerializerOptionsAccessor jsonSerializerOptionsCache)
    : RunCommand(ansiConsole, resultsProcessor, testSuiteExecutor, variablesContext, templateContext, jsonSerializerOptionsCache)
{
    protected override bool IsDebug => true;
}
