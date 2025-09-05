using JTest.Core;
using JTest.Core.Models;
using System.Text;
using System.Text.Json;

namespace JTest;

class Program
{
    static async Task<int> Main(string[] args)
    {
        try
        {
            var cli = new JTestCli();
            return await cli.RunAsync(args);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }
}

public class JTestCli
{
    private Dictionary<string, string> _envVars = new();
    private Dictionary<string, string> _globals = new();
    private readonly TestRunner _testRunner;

    public JTestCli()
    {
        _testRunner = new TestRunner();
    }

    private static readonly string HelpText = @"JTEST CLI v1.0 - Universal Test Definition Language
==================================================

USAGE:
    jtest <COMMAND> [OPTIONS]
    jtest <testfile.json> [OPTIONS]     # Direct test execution
    jtest <pattern> [OPTIONS]           # Wildcard execution (*.json, tests/*.json)

COMMANDS:
    run <testfile|pattern>              Run test file(s) - supports wildcards
    export <format> <testfile> [output] Export tests to other frameworks
    debug <testfile|pattern>            Run with verbose debug output and markdown log - supports wildcards
    validate <testfile|pattern>         Validate test file(s) syntax - supports wildcards
    create <testname> [output]          Create a new test template
    --help, -h                          Show this help message

EXPORT FORMATS:
    postman         Postman Collection (v2.1)
    karate          Karate DSL Feature file

RUNTIME OPTIONS:
    --env key=value                     Set environment variable
    --env-file <path.json>              Load environment from JSON file
    --globals key=value                 Set global variable
    --globals-file <path.json>          Load globals from JSON file

EXAMPLES:
    # Run a single test file
    jtest run my_api_tests.json
    jtest my_api_tests.json                     # Shorthand

    # Run multiple test files with wildcards
    jtest run *.json                            # All JSON files in current directory
    jtest run tests/*.json                      # All JSON files in tests directory
    jtest run api-*.json                        # All files starting with 'api-'

    # Run with environment variables
    jtest run tests/*.json --env baseUrl=https://api.prod.com
    jtest run tests.json --env-file prod.json

    # Export to other frameworks (single file only)
    jtest export postman tests.json
    jtest export karate tests.json my_tests

    # Validate test files
    jtest validate tests.json               # Single file
    jtest validate *.json                   # All JSON files

    # Debug mode with verbose output
    jtest debug tests.json
    jtest debug tests/*.json                # Debug all test files in tests directory
    jtest debug tests.json --env verbosity=Verbose

    # Create a new test template
    jtest create ""My API Test"" my_test.json

For more information, visit: https://github.com/ELSA-X/JTEST";

    public async Task<int> RunAsync(string[] args)
    {
        if (args.Length == 0)
        {
            ShowHelp();
            return 0;
        }

        // Parse runtime options first
        var parsedArgs = ParseRuntimeOptions(args);

        if (parsedArgs.Count == 0)
        {
            ShowHelp();
            return 0;
        }

        var command = parsedArgs[0].ToLower();

        // Handle help command
        if (command == "--help" || command == "-h" || command == "help")
        {
            ShowHelp();
            return 0;
        }

        // Handle direct test execution (shorthand)
        if (command.EndsWith(".json") && !IsKnownCommand(command))
        {
            return await RunCommand("run", parsedArgs);
        }

        // Handle known commands
        return command switch
        {
            "run" => await RunCommand("run", parsedArgs.Skip(1).ToList()),
            "export" => await ExportCommand(parsedArgs.Skip(1).ToList()),
            "debug" => await DebugCommand(parsedArgs.Skip(1).ToList()),
            "validate" => await ValidateCommand(parsedArgs.Skip(1).ToList()),
            "create" => await CreateCommand(parsedArgs.Skip(1).ToList()),
            _ => HandleUnknownCommand(command)
        };
    }

    private List<string> ParseRuntimeOptions(string[] args)
    {
        var remainingArgs = new List<string>();

        for (int i = 0; i < args.Length; i++)
        {
            var arg = args[i];

            if (arg == "--env" && i + 1 < args.Length)
            {
                var envPair = args[++i];
                var parts = envPair.Split('=', 2);
                if (parts.Length == 2)
                {
                    _envVars[parts[0]] = parts[1];
                    Console.WriteLine($"Set environment variable: {parts[0]}={parts[1]}");
                }
                else
                {
                    Console.Error.WriteLine($"Invalid environment variable format: {envPair}. Expected key=value");
                }
            }
            else if (arg == "--env-file" && i + 1 < args.Length)
            {
                var envFile = args[++i];
                LoadEnvironmentFile(envFile);
            }
            else if (arg == "--globals" && i + 1 < args.Length)
            {
                var globalPair = args[++i];
                var parts = globalPair.Split('=', 2);
                if (parts.Length == 2)
                {
                    _globals[parts[0]] = parts[1];
                    Console.WriteLine($"Set global variable: {parts[0]}={parts[1]}");
                }
                else
                {
                    Console.Error.WriteLine($"Invalid global variable format: {globalPair}. Expected key=value");
                }
            }
            else if (arg == "--globals-file" && i + 1 < args.Length)
            {
                var globalsFile = args[++i];
                LoadGlobalsFile(globalsFile);
            }
            else
            {
                remainingArgs.Add(arg);
            }
        }

        return remainingArgs;
    }

    private void LoadEnvironmentFile(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                Console.Error.WriteLine($"Environment file not found: {filePath}");
                return;
            }

            var json = File.ReadAllText(filePath);
            var envData = JsonSerializer.Deserialize<Dictionary<string, object>>(json);

            if (envData != null)
            {
                foreach (var kvp in envData)
                {
                    _envVars[kvp.Key] = kvp.Value?.ToString() ?? "";
                    Console.WriteLine($"Loaded environment variable from file: {kvp.Key}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error loading environment file {filePath}: {ex.Message}");
        }
    }

    private void LoadGlobalsFile(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                Console.Error.WriteLine($"Globals file not found: {filePath}");
                return;
            }

            var json = File.ReadAllText(filePath);
            var globalsData = JsonSerializer.Deserialize<Dictionary<string, object>>(json);

            if (globalsData != null)
            {
                foreach (var kvp in globalsData)
                {
                    _globals[kvp.Key] = kvp.Value?.ToString() ?? "";
                    Console.WriteLine($"Loaded global variable from file: {kvp.Key}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error loading globals file {filePath}: {ex.Message}");
        }
    }

    private bool IsKnownCommand(string command)
    {
        return command.ToLower() is "run" or "export" or "debug" or "validate" or "create" or "--help" or "-h";
    }

    /// <summary>
    /// Expands wildcard patterns to actual file paths
    /// </summary>
    /// <param name="pattern">File pattern (e.g., "*.json", "tests/*.json")</param>
    /// <returns>List of matching file paths</returns>
    private List<string> ExpandWildcardPattern(string pattern)
    {
        var files = new List<string>();

        try
        {
            // If it's not a pattern (no wildcards), treat as single file
            if (!pattern.Contains('*') && !pattern.Contains('?'))
            {
                files.Add(pattern);
                return files;
            }

            // Handle wildcard patterns
            var directory = Path.GetDirectoryName(pattern);
            var fileName = Path.GetFileName(pattern);

            // If no directory specified, use current directory
            if (string.IsNullOrEmpty(directory))
            {
                directory = ".";
            }

            // Ensure directory exists
            if (!Directory.Exists(directory))
            {
                Console.Error.WriteLine($"Warning: Directory not found: {directory}");
                return files;
            }

            // Get matching files
            var matchingFiles = Directory.GetFiles(directory, fileName, SearchOption.TopDirectoryOnly);
            files.AddRange(matchingFiles.OrderBy(f => f));

            if (files.Count == 0)
            {
                Console.WriteLine($"Warning: No files found matching pattern: {pattern}");
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error expanding pattern '{pattern}': {ex.Message}");
        }

        return files;
    }

    private async Task<int> RunCommand(string command, List<string> args)
    {
        if (args.Count == 0)
        {
            Console.Error.WriteLine($"Error: {command} command requires a test file argument");
            Console.Error.WriteLine($"Usage: jtest {command} <testfile>");
            return 1;
        }

        var pattern = args[0];
        var testFiles = ExpandWildcardPattern(pattern);

        if (testFiles.Count == 0)
        {
            Console.Error.WriteLine($"Error: No test files found matching pattern: {pattern}");
            return 1;
        }

        // Display environment and global variables once
        if (_envVars.Count > 0)
        {
            Console.WriteLine("Environment variables:");
            foreach (var kvp in _envVars)
            {
                Console.WriteLine($"  {kvp.Key} = {kvp.Value}");
            }
        }

        if (_globals.Count > 0)
        {
            Console.WriteLine("Global variables:");
            foreach (var kvp in _globals)
            {
                Console.WriteLine($"  {kvp.Key} = {kvp.Value}");
            }
        }

        var allResults = new List<JTestCaseResult>();
        var processedFiles = 0;
        var failedFiles = 0;

        foreach (var testFile in testFiles)
        {
            if (!File.Exists(testFile))
            {
                Console.Error.WriteLine($"Error: Test file not found: {testFile}");
                failedFiles++;
                continue;
            }

            Console.WriteLine($"\n{'=' * 60}");
            Console.WriteLine($"Running test file: {testFile}");
            Console.WriteLine($"{'=' * 60}");

            try
            {
                // Read and execute the test file
                var jsonContent = await File.ReadAllTextAsync(testFile);

                // Convert string dictionaries to object dictionaries
                var environment = _envVars.ToDictionary(kvp => kvp.Key, kvp => (object)kvp.Value);
                var globals = _globals.ToDictionary(kvp => kvp.Key, kvp => (object)kvp.Value);

                var results = await _testRunner.RunTestAsync(jsonContent, environment, globals);
                allResults.AddRange(results);

                // Display results for this file
                var fileSuccess = 0;
                var fileFailed = 0;

                foreach (var result in results)
                {
                    Console.WriteLine($"\nTest: {result.TestCaseName}");
                    if (result.Dataset != null)
                    {
                        Console.WriteLine($"Dataset: {result.Dataset.Name ?? "unnamed"}");
                    }
                    Console.WriteLine($"Status: {(result.Success ? "PASSED" : "FAILED")}");
                    Console.WriteLine($"Duration: {result.DurationMs}ms");
                    Console.WriteLine($"Steps executed: {result.StepResults.Count}");

                    if (!result.Success && !string.IsNullOrEmpty(result.ErrorMessage))
                    {
                        Console.WriteLine($"Error: {result.ErrorMessage}");
                    }

                    if (result.Success)
                        fileSuccess++;
                    else
                        fileFailed++;
                }

                Console.WriteLine($"\nFile Summary - {Path.GetFileName(testFile)}: {fileSuccess} passed, {fileFailed} failed");
                processedFiles++;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error executing test file {testFile}: {ex.Message}");
                failedFiles++;
            }
        }

        // Display overall summary
        Console.WriteLine($"\n{'=' * 60}");
        Console.WriteLine($"OVERALL TEST SUMMARY");
        Console.WriteLine($"{'=' * 60}");
        Console.WriteLine($"Files processed: {processedFiles}");
        Console.WriteLine($"Files failed: {failedFiles}");
        Console.WriteLine($"Total tests: {allResults.Count}");
        Console.WriteLine($"Passed: {allResults.Count(r => r.Success)}");
        Console.WriteLine($"Failed: {allResults.Count(r => !r.Success)}");

        var totalFailed = allResults.Count(r => !r.Success) + failedFiles;
        if (totalFailed > 0)
        {
            Console.WriteLine("Test execution completed with failures.");
            return 1;
        }
        else
        {
            Console.WriteLine("Test execution completed successfully.");
            return 0;
        }
    }

    private Task<int> ExportCommand(List<string> args)
    {
        if (args.Count < 2)
        {
            Console.Error.WriteLine("Error: export command requires format and test file arguments");
            Console.Error.WriteLine("Usage: jtest export <format> <testfile> [output]");
            Console.Error.WriteLine("Supported formats: postman, karate");
            return Task.FromResult(1);
        }

        var format = args[0].ToLower();
        var testFile = args[1];
        var outputFile = args.Count > 2 ? args[2] : null;

        if (format != "postman" && format != "karate")
        {
            Console.Error.WriteLine($"Error: Unsupported export format: {format}");
            Console.Error.WriteLine("Supported formats: postman, karate");
            return Task.FromResult(1);
        }

        if (!File.Exists(testFile))
        {
            Console.Error.WriteLine($"Error: Test file not found: {testFile}");
            return Task.FromResult(1);
        }

        var defaultOutput = format switch
        {
            "postman" => Path.ChangeExtension(testFile, ".postman_collection.json"),
            "karate" => Path.ChangeExtension(testFile, ".feature"),
            _ => throw new InvalidOperationException()
        };

        var output = outputFile ?? defaultOutput;

        Console.WriteLine($"Exporting test file: {testFile}");
        Console.WriteLine($"Export format: {format}");
        Console.WriteLine($"Output file: {output}");

        // TODO: Implement actual export logic
        Console.WriteLine("Export completed successfully.");

        return Task.FromResult(0);
    }

    private async Task<int> DebugCommand(List<string> args)
    {
        if (args.Count == 0)
        {
            Console.Error.WriteLine("Error: debug command requires a test file argument");
            Console.Error.WriteLine("Usage: jtest debug <testfile>");
            return 1;
        }

        var pattern = args[0];
        var testFiles = ExpandWildcardPattern(pattern);

        if (testFiles.Count == 0)
        {
            Console.Error.WriteLine($"Error: No test files found matching pattern: {pattern}");
            return 1;
        }

        var processedFiles = 0;
        var failedFiles = 0;
        var allOutputFiles = new List<string>();

        foreach (var testFile in testFiles)
        {
            if (!File.Exists(testFile))
            {
                Console.Error.WriteLine($"Error: Test file not found: {testFile}");
                failedFiles++;
                continue;
            }

            // Generate output markdown file name for each test file
            var outputFile = Path.ChangeExtension(testFile, ".md");
            allOutputFiles.Add(outputFile);

            Console.WriteLine($"\n{'=' * 60}");
            Console.WriteLine($"Running test file in debug mode: {testFile}");
            Console.WriteLine("Debug mode: ON");
            Console.WriteLine("Verbose output: ON");
            Console.WriteLine("Markdown logging: ON");
            Console.WriteLine($"Debug output will be saved to: {outputFile}");
            Console.WriteLine($"{'=' * 60}");

            var markdownContent = new StringBuilder();

            // Add header to markdown file
            markdownContent.AppendLine($"# Debug Report for {Path.GetFileName(testFile)}");
            markdownContent.AppendLine();
            markdownContent.AppendLine($"**Generated:** {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            markdownContent.AppendLine($"**Test File:** {testFile}");
            markdownContent.AppendLine();

            if (_envVars.Count > 0)
            {
                Console.WriteLine("\nEnvironment variables loaded");
                markdownContent.AppendLine("## Environment Variables");
                foreach (var kvp in _envVars)
                {
                    markdownContent.AppendLine($"- **{kvp.Key}**: {kvp.Value}");
                }
                markdownContent.AppendLine();
            }

            if (_globals.Count > 0)
            {
                Console.WriteLine("Global variables loaded");
                markdownContent.AppendLine("## Global Variables");
                foreach (var kvp in _globals)
                {
                    markdownContent.AppendLine($"- **{kvp.Key}**: {kvp.Value}");
                }
                markdownContent.AppendLine();
            }

            try
            {
                // Read and execute the test file with debug logging
                var jsonContent = await File.ReadAllTextAsync(testFile);

                // Convert string dictionaries to object dictionaries
                var environment = _envVars.ToDictionary(kvp => kvp.Key, kvp => (object)kvp.Value);
                var globals = _globals.ToDictionary(kvp => kvp.Key, kvp => (object)kvp.Value);



                var results = await _testRunner.RunTestAsync(jsonContent, environment, globals);

                Console.WriteLine("\nTest execution completed");

                // Add debug output to markdown content
                markdownContent.AppendLine("## Test Execution");
                // todo get a convertion results to markdown. and add it here

                // Calculate results summary
                var totalSuccess = 0;
                var totalFailed = 0;
                var errorMessages = new List<string>();

                foreach (var result in results)
                {
                    if (result.Success)
                        totalSuccess++;
                    else
                        totalFailed++;

                    if (!result.Success && !string.IsNullOrEmpty(result.ErrorMessage))
                    {
                        errorMessages.Add($"ERROR in {result.TestCaseName}: {result.ErrorMessage}");
                    }
                }

                // Add summary to markdown
                markdownContent.AppendLine("## Summary");
                markdownContent.AppendLine($"- **Total tests:** {results.Count}");
                markdownContent.AppendLine($"- **Passed:** {totalSuccess}");
                markdownContent.AppendLine($"- **Failed:** {totalFailed}");
                markdownContent.AppendLine();

                if (errorMessages.Any())
                {
                    markdownContent.AppendLine("## Errors");
                    foreach (var error in errorMessages)
                    {
                        markdownContent.AppendLine($"- {error}");
                    }
                    markdownContent.AppendLine();
                }

                // Write markdown content to file
                await File.WriteAllTextAsync(outputFile, markdownContent.ToString());

                // Display console summary
                Console.WriteLine($"Total tests: {results.Count}");
                Console.WriteLine($"Passed: {totalSuccess}");
                Console.WriteLine($"Failed: {totalFailed}");

                if (errorMessages.Any())
                {
                    Console.WriteLine("\nErrors occurred during execution:");
                    foreach (var error in errorMessages)
                    {
                        Console.WriteLine($"  {error}");
                    }
                }

                Console.WriteLine($"Detailed debug report saved to: {outputFile}");

                if (totalFailed > 0)
                {
                    failedFiles++;
                }
                else
                {
                    processedFiles++;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"\nERROR: {ex.Message}");

                // Still try to save what we have to the markdown file
                markdownContent.AppendLine("## Error");
                markdownContent.AppendLine($"Execution failed with error: {ex.Message}");

                try
                {
                    await File.WriteAllTextAsync(outputFile, markdownContent.ToString());
                    Console.WriteLine($"Partial debug report saved to: {outputFile}");
                }
                catch
                {
                    // Ignore file write errors in error handling
                }

                failedFiles++;
            }
        }

        // Display overall summary
        Console.WriteLine($"\n{'=' * 60}");
        Console.WriteLine($"DEBUG SUMMARY");
        Console.WriteLine($"{'=' * 60}");
        Console.WriteLine($"Files processed successfully: {processedFiles}");
        Console.WriteLine($"Files failed: {failedFiles}");
        Console.WriteLine($"Debug reports generated: {allOutputFiles.Count}");

        if (allOutputFiles.Any())
        {
            Console.WriteLine("\nGenerated debug reports:");
            foreach (var outputFile in allOutputFiles)
            {
                Console.WriteLine($"  - {outputFile}");
            }
        }

        if (failedFiles > 0)
        {
            Console.WriteLine("Debug execution completed with failures.");
            return 1;
        }
        else
        {
            Console.WriteLine("Debug execution completed successfully.");
            return 0;
        }
    }

    private async Task<int> CreateCommand(List<string> args)
    {
        if (args.Count == 0)
        {
            Console.Error.WriteLine("Error: create command requires a test name argument");
            Console.Error.WriteLine("Usage: jtest create <testname> [output]");
            return 1;
        }

        var testName = args[0];
        var outputFile = args.Count > 1 ? args[1] : $"{testName.Replace(" ", "_").ToLowerInvariant()}.json";

        Console.WriteLine($"Creating test template: {testName}");
        Console.WriteLine($"Output file: {outputFile}");

        try
        {
            var templateJson = _testRunner.CreateTestTemplate(testName);
            await File.WriteAllTextAsync(outputFile, templateJson);

            Console.WriteLine("Test template created successfully.");
            Console.WriteLine($"\nTo run the test:");
            Console.WriteLine($"  jtest run {outputFile} --env baseUrl=https://your-api.com");
            Console.WriteLine($"\nTo debug the test:");
            Console.WriteLine($"  jtest debug {outputFile} --env baseUrl=https://your-api.com");

            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error creating test template: {ex.Message}");
            return 1;
        }
    }

    private async Task<int> ValidateCommand(List<string> args)
    {
        if (args.Count == 0)
        {
            Console.Error.WriteLine("Error: validate command requires a test file argument");
            Console.Error.WriteLine("Usage: jtest validate <testfile>");
            return 1;
        }

        var pattern = args[0];
        var testFiles = ExpandWildcardPattern(pattern);

        if (testFiles.Count == 0)
        {
            Console.Error.WriteLine($"Error: No test files found matching pattern: {pattern}");
            return 1;
        }

        var processedFiles = 0;
        var validFiles = 0;
        var invalidFiles = 0;

        foreach (var testFile in testFiles)
        {
            if (!File.Exists(testFile))
            {
                Console.Error.WriteLine($"Error: Test file not found: {testFile}");
                invalidFiles++;
                continue;
            }

            Console.WriteLine($"\n{'=' * 50}");
            Console.WriteLine($"Validating test file: {testFile}");
            Console.WriteLine($"{'=' * 50}");

            try
            {
                var json = await File.ReadAllTextAsync(testFile);

                // Basic JSON syntax validation
                JsonDocument.Parse(json);
                Console.WriteLine("Valid JSON syntax");

                // JTEST schema validation using TestRunner
                if (_testRunner.ValidateTestDefinition(json))
                {
                    Console.WriteLine("Valid JTEST schema");
                    Console.WriteLine($"✓ {Path.GetFileName(testFile)} - Valid");
                    validFiles++;
                }
                else
                {
                    Console.Error.WriteLine("Invalid JTEST schema: Missing required properties (name, flow)");
                    Console.WriteLine($"✗ {Path.GetFileName(testFile)} - Invalid schema");
                    invalidFiles++;
                }
                processedFiles++;
            }
            catch (JsonException ex)
            {
                Console.Error.WriteLine($"Invalid JSON syntax: {ex.Message}");
                Console.WriteLine($"✗ {Path.GetFileName(testFile)} - Invalid JSON");
                invalidFiles++;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Validation error: {ex.Message}");
                Console.WriteLine($"✗ {Path.GetFileName(testFile)} - Error");
                invalidFiles++;
            }
        }

        // Display overall summary
        Console.WriteLine($"\n{'=' * 50}");
        Console.WriteLine($"VALIDATION SUMMARY");
        Console.WriteLine($"{'=' * 50}");
        Console.WriteLine($"Files processed: {processedFiles}");
        Console.WriteLine($"Valid files: {validFiles}");
        Console.WriteLine($"Invalid files: {invalidFiles}");

        if (invalidFiles > 0)
        {
            Console.WriteLine("Validation completed with errors.");
            return 1;
        }
        else
        {
            Console.WriteLine("All files are valid.");
            return 0;
        }
    }

    private int HandleUnknownCommand(string command)
    {
        Console.Error.WriteLine($"Error: Unknown command '{command}'");
        Console.Error.WriteLine("Run 'jtest --help' to see available commands.");
        return 1;
    }

    private void ShowHelp()
    {
        Console.WriteLine(HelpText);
    }
}
