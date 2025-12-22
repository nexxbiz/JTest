using JTest.Core.Execution;
using JTest.Core.Utilities;
using JTest.Core.Variables;
using Spectre.Console;

namespace JTest.Cli.Commands;

public sealed class DebugCommand(IAnsiConsole ansiConsole, IJTestSuiteExecutionResultProcessor resultsProcessor, IJTestSuiteExecutor testSuiteExecutor, IVariablesContext variablesContext, JsonSerializerOptionsAccessor jsonSerializerOptionsCache)
    : RunCommand(ansiConsole, resultsProcessor, testSuiteExecutor, variablesContext, jsonSerializerOptionsCache)
{
    protected override bool IsDebug => true;
}
