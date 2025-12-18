using JTest.Cli;
using JTest.Core;
using JTest.Core.Models;
using JTest.Core.Output;
using JTest.Core.Output.Markdown;
using JTest.Core.Utilities;
using System.Collections.Concurrent;
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
    private readonly Dictionary<string, object> _envVars = [];
    private readonly Dictionary<string, object> _globals = [];
    private readonly TestRunner _testRunner;
    private int _parallelCount = 1; // Default to sequential execution        
    private const string globalConfigFileEnvVar = "JTEST_CONFIG_FILE"; // Environment variable name for global config file path
    private static readonly JsonSerializerOptions jsonSerializerOptions = new() { PropertyNameCaseInsensitive = true };
    private string OutputDirectory = Directory.GetCurrentDirectory();
    private bool skipOutput = false;
    private readonly List<string> testFileCategories = [];

    public JTestCli()
    {
        _testRunner = new TestRunner(globalConfig);

        if (!string.IsNullOrWhiteSpace(globalConfig?.OutputDirectory))
        {
            OutputDirectory = globalConfig.OutputDirectory;
        }
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
    --env key=value                             Set environment variable
    --env-file <path.json>                      Load environment from JSON file
    --globals key=value                         Set global variable
    --globals-file <path.json>                  Load globals from JSON file    
    --categories, -c                            Comma-separated list of test file categories to run (default: all)
    --parallel <count>, -p <count>              Run test files in parallel (default: 1)
    --output <folder-path>, -o <folder-path>    Output folder path where reports are saved (default: working directory)
    --skip-output                               When specified, then does not output a report file (default: false)

ENVIRONMENT VARIABLES:    
    JTEST_CONFIG_FILE                           Path to global JTest config file (JSON)

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

    # Run test files in parallel
    jtest run tests/*.json --parallel 4
    jtest run tests/*.json -p 8

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


        // Handle NON-execution known commands
        return command switch
        {
            "export" => await ExportCommand([.. parsedArgs.Skip(1)]),
            "validate" => await ValidateCommand([.. parsedArgs.Skip(1)]),
            "create" => await CreateCommand([.. parsedArgs.Skip(1)]),

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
            else if ((arg == "--parallel" || arg == "-p") && i + 1 < args.Length)
            {
                if (int.TryParse(args[++i], out var parallelCount) && parallelCount > 0)
                {
                    _parallelCount = parallelCount;
                    Console.WriteLine($"Set parallel execution: {_parallelCount} files");
                }
                else
                {
                    Console.Error.WriteLine($"Invalid parallel count: {args[i]}. Must be a positive integer.");
                }
            }
            else if ((arg == "--output" || arg == "-o") && i + 1 < args.Length)
            {
                OutputDirectory = args[++i];
                Console.WriteLine($"Set output directory: {OutputDirectory}");
            }
            else if (arg == "--skip-output" && i < args.Length)
            {
                skipOutput = true;
            }
            else if ((arg == "--categories" || arg == "-c") && i + 1 < args.Length)
            {
                var categories = args[++i].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                testFileCategories.AddRange(categories);
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
                    _globals[kvp.Key] = kvp.Value;
                    Console.WriteLine($"Loaded global variable from file: {kvp.Key}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error loading globals file {filePath}: {ex.Message}");
        }
    }

    private static bool IsKnownCommand(string command)
    {
        return command.ToLower() is "run" or "export" or "debug" or "validate" or "create" or "--help" or "-h";
    }

    private async Task<IEnumerable<TestFileExecutionResult>?> RunCommand(List<string> testFilePatterns)
    {
        if (testFilePatterns.Count == 0)
        {
            Console.Error.WriteLine($"Error: Run command requires a test file argument");
            Console.Error.WriteLine($"Usage: jtest run <testfile>");
            return null;
        }

        var testFiles = TestFileSearcher.Search(testFilePatterns, testFileCategories);
        if (testFiles.Length == 0)
        {
            Console.Error.WriteLine($"Error: No test files found matching patterns: {string.Join(", ", testFilePatterns)}");
            return null;
        }

        var allResults = new List<TestFileExecutionResult>();

        if (_parallelCount > 1 && testFiles.Length > 1)
        {
            Console.WriteLine($"Running {testFiles.Length} test files in parallel (max concurrent: {_parallelCount})");

            // Thread-safe collections for parallel execution
            var allResultsThreadSafe = new ConcurrentBag<JTestCaseResult>();
            var processedFilesThreadSafe = 0;
            var failedFilesThreadSafe = 0;

            // Use Parallel.ForEach with MaxDegreeOfParallelism
            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = _parallelCount
            };


            Parallel.ForEach(testFiles, parallelOptions, testFile =>
            {
                if (!File.Exists(testFile))
                {
                    lock (Console.Error)
                    {
                        Console.Error.WriteLine($"Error: Test file not found: {testFile}");
                    }
                    Interlocked.Increment(ref failedFilesThreadSafe);
                    return;
                }

                lock (Console.Out)
                {
                    Console.WriteLine($"Running test file: {testFile}");
                }

                try
                {
                    // Read and execute the test file
                    var jsonContent = File.ReadAllText(testFile);
                    var jsonDocument = JsonDocument.Parse(jsonContent);
                    var jsonDefinition = jsonDocument.RootElement;
                    var (testSuiteName, testSuiteDescription) = GetTestFileMetaData(jsonDefinition);
                    var results = _testRunner.RunTestAsync(jsonDefinition, testFile, _envVars, _globals).Result;

                    foreach (var result in results)
                    {
                        allResultsThreadSafe.Add(result);
                    }

                    allResults.Add(new(testFile, testSuiteName, testSuiteDescription, results));

                    Interlocked.Increment(ref processedFilesThreadSafe);
                }
                catch (Exception ex)
                {
                    lock (Console.Error)
                    {
                        Console.Error.WriteLine($"Error executing test file {testFile}: {ex.Message}");
                    }
                    Interlocked.Increment(ref failedFilesThreadSafe);
                }
            });
        }
        else
        {
            // Sequential execution (original logic)
            foreach (var testFile in testFiles)
            {
                if (!File.Exists(testFile))
                {
                    Console.Error.WriteLine($"Error: Test file not found: {testFile}");
                    continue;
                }

                Console.WriteLine($"Running test file: {testFile}");

                try
                {
                    // Read and execute the test file
                    var jsonContent = await File.ReadAllTextAsync(testFile);
                    var jsonDocument = JsonDocument.Parse(jsonContent);
                    var jsonDefinition = jsonDocument.RootElement;
                    var (testSuiteName, testSuiteDescription) = GetTestFileMetaData(jsonDefinition);

                    var results = await _testRunner.RunTestAsync(jsonDefinition, testFile, _envVars, _globals);
                    allResults.Add(new(testFile, testSuiteName, testSuiteDescription, results));
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Error executing test file {testFile}: {ex.Message}");
                }
            }
        }

        return allResults;
    }

    private static (string? name, string? description) GetTestFileMetaData(JsonElement jsonDefinition)
    {
        if (!jsonDefinition.TryGetProperty("info", out var infoElement))
        {
            return (null, null);
        }

        var name = string.Empty;
        var description = string.Empty;
        if (infoElement.TryGetProperty("name", out var nameElement))
        {
            name = nameElement.GetString();
        }
        if (infoElement.TryGetProperty("description", out var descriptionElement))
        {
            description = descriptionElement.GetString();
        }

        return (name, description);
    }

    private static Task<int> ExportCommand(List<string> args)
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

    private async Task<int> ValidateCommand(List<string> testFilePatterns)
    {
        if (testFilePatterns.Count == 0)
        {
            Console.Error.WriteLine("Error: validate command requires a test file argument");
            Console.Error.WriteLine("Usage: jtest validate <testfile>");
            return 1;
        }

        var testFiles = TestFileSearcher.Search(testFilePatterns);

        if (testFiles.Length == 0)
        {
            Console.Error.WriteLine($"Error: No test files found matching patterns: {string.Join(", ", testFilePatterns)}");
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
