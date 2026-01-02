# Extensibility

JTest is designed from the ground up to be extensible. You can add custom step types, assertion operations, and functionality without modifying the core framework.

## Extension Architecture

### Core Components

1. **Step Registry** - Central registry that manages all available step types
2. **Step Interface** - Common contract that all step implementations must follow  
3. **Step Base Class** - Provides common functionality and reduces boilerplate
4. **Execution Engine** - Orchestrates step execution without knowing specific implementations
5. **Context System** - Unified context management that all steps can use

### Extensibility Philosophy

- **Plugin Architecture** - New step types are implemented as plugins that register with the core engine
- **Interface-Driven** - All extensible components implement well-defined interfaces
- **Isolation** - Extensions operate in isolation and cannot affect core system stability
- **Convention over Configuration** - Extensions follow simple naming and structural conventions
- **JSON-First** - All extensions are defined and configured through JSON

## Creating Custom Step Types

### Step Interface Requirements

All custom steps must implement the `IStep` interface:

```csharp
public interface IStep
{
    string Type { get; }
    string? Id { get; set; }
    
    void SetConfiguration(JsonElement configuration);
    bool ValidateConfiguration(JsonElement configuration);
    Task<StepResult> ExecuteAsync(IExecutionContext context);
}
```

### Using BaseStep

Inherit from `BaseStep` to get common functionality:

```csharp
using JTest.Core.Steps;
using JTest.Core.Execution;
using System.Text.Json;

public class CustomStep : BaseStep
{
    public override string Type => "custom";

    public override bool ValidateConfiguration(JsonElement configuration)
    {
        // Validate required properties
        return configuration.TryGetProperty("requiredProperty", out _);
    }

    public override async Task<StepResult> ExecuteAsync(IExecutionContext context)
    {
        var contextBefore = CloneContext(context);
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            // Your custom logic here
            var result = await PerformCustomOperation(context);
            
            stopwatch.Stop();
            
            // Use common step completion logic
            return await ProcessStepCompletionAsync(context, contextBefore, stopwatch, result);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return StepResult.CreateFailure(this, ex.Message, stopwatch.ElapsedMilliseconds);
        }
    }

    private async Task<object> PerformCustomOperation(IExecutionContext context)
    {
        // Implement your custom step logic
        // Access configuration via Configuration property
        // Use context for variable resolution
        
        return new { success = true, message = "Custom operation completed" };
    }
}
```

## Example Custom Step Types

### File System Step

```csharp
public class FileSystemStep : BaseStep
{
    public override string Type => "file";

    public override bool ValidateConfiguration(JsonElement configuration)
    {
        return configuration.TryGetProperty("operation", out _) && 
               configuration.TryGetProperty("path", out _);
    }

    public override async Task<StepResult> ExecuteAsync(IExecutionContext context)
    {
        var contextBefore = CloneContext(context);
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var operation = ResolveVariable(Configuration.GetProperty("operation").GetString(), context);
            var path = ResolveVariable(Configuration.GetProperty("path").GetString(), context);
            
            object result = operation.ToLower() switch
            {
                "read" => await ReadFileAsync(path),
                "write" => await WriteFileAsync(path, context),
                "exists" => File.Exists(path),
                "delete" => DeleteFile(path),
                _ => throw new InvalidOperationException($"Unknown file operation: {operation}")
            };
            
            stopwatch.Stop();
            return await ProcessStepCompletionAsync(context, contextBefore, stopwatch, result);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return StepResult.CreateFailure(this, ex.Message, stopwatch.ElapsedMilliseconds);
        }
    }

    private async Task<object> ReadFileAsync(string path)
    {
        var content = await File.ReadAllTextAsync(path);
        return new { path, content, exists = true };
    }

    private async Task<object> WriteFileAsync(string path, IExecutionContext context)
    {
        var content = ResolveVariable(
            Configuration.GetProperty("content").GetString(), 
            context
        );
        
        await File.WriteAllTextAsync(path, content);
        return new { path, written = true, size = content.Length };
    }

    private object DeleteFile(string path)
    {
        var existed = File.Exists(path);
        if (existed)
        {
            File.Delete(path);
        }
        return new { path, deleted = existed };
    }
}
```

**Usage:**
```json
{
    "type": "file",
    "operation": "read",
    "path": "{{$.env.configPath}}/settings.json",
    "assert": [
        {
            "op": "exists",
            "actualValue": "{{$.this.content}}"
        }
    ],
    "save": {
        "$.globals.configData": "{{$.this.content}}"
    }
}
```

### Database Step

```csharp
public class DatabaseStep : BaseStep
{
    private readonly IDbConnectionFactory _connectionFactory;

    public DatabaseStep(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public override string Type => "database";

    public override bool ValidateConfiguration(JsonElement configuration)
    {
        return configuration.TryGetProperty("query", out _);
    }

    public override async Task<StepResult> ExecuteAsync(IExecutionContext context)
    {
        var contextBefore = CloneContext(context);
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var connectionString = ResolveVariable(
                Configuration.GetProperty("connectionString").GetString(), 
                context
            );
            
            var query = ResolveVariable(
                Configuration.GetProperty("query").GetString(), 
                context
            );
            
            using var connection = _connectionFactory.CreateConnection(connectionString);
            await connection.OpenAsync();
            
            using var command = connection.CreateCommand();
            command.CommandText = query;
            
            var result = await ExecuteQueryAsync(command);
            
            stopwatch.Stop();
            return await ProcessStepCompletionAsync(context, contextBefore, stopwatch, result);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return StepResult.CreateFailure(this, ex.Message, stopwatch.ElapsedMilliseconds);
        }
    }

    private async Task<object> ExecuteQueryAsync(IDbCommand command)
    {
        var results = new List<Dictionary<string, object?>>();
        
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var row = new Dictionary<string, object?>();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                row[reader.GetName(i)] = reader.GetValue(i);
            }
            results.Add(row);
        }
        
        return new { rows = results, count = results.Count };
    }
}
```

**Usage:**
```json
{
    "type": "database",
    "connectionString": "{{$.env.dbConnectionString}}",
    "query": "SELECT * FROM users WHERE email = '{{$.globals.testUserEmail}}'",
    "assert": [
        {
            "op": "equals",
            "actualValue": "{{$.this.count}}",
            "expectedValue": 1
        }
    ],
    "save": {
        "$.globals.userId": "{{$.this.rows[0].id}}"
    }
}
```

### Wait/Delay Step

```csharp
public class WaitStep : BaseStep
{
    public override string Type => "wait";

    public override bool ValidateConfiguration(JsonElement configuration)
    {
        return configuration.TryGetProperty("duration", out _) ||
               configuration.TryGetProperty("condition", out _);
    }

    public override async Task<StepResult> ExecuteAsync(IExecutionContext context)
    {
        var contextBefore = CloneContext(context);
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            object result;
            
            if (Configuration.TryGetProperty("duration", out var durationElement))
            {
                result = await WaitForDuration(durationElement, context);
            }
            else if (Configuration.TryGetProperty("condition", out var conditionElement))
            {
                result = await WaitForCondition(conditionElement, context);
            }
            else
            {
                throw new InvalidOperationException("Wait step requires either 'duration' or 'condition'");
            }
            
            stopwatch.Stop();
            return await ProcessStepCompletionAsync(context, contextBefore, stopwatch, result);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return StepResult.CreateFailure(this, ex.Message, stopwatch.ElapsedMilliseconds);
        }
    }

    private async Task<object> WaitForDuration(JsonElement durationElement, IExecutionContext context)
    {
        var durationMs = int.Parse(ResolveVariable(durationElement.GetString(), context));
        await Task.Delay(durationMs);
        
        return new { 
            waitType = "duration", 
            durationMs, 
            completedAt = DateTime.UtcNow 
        };
    }

    private async Task<object> WaitForCondition(JsonElement conditionElement, IExecutionContext context)
    {
        var condition = ResolveVariable(conditionElement.GetString(), context);
        var maxWaitMs = 30000; // Default 30 second timeout
        var intervalMs = 1000;  // Check every second
        
        if (Configuration.TryGetProperty("maxWait", out var maxWaitElement))
        {
            maxWaitMs = int.Parse(ResolveVariable(maxWaitElement.GetString(), context));
        }
        
        if (Configuration.TryGetProperty("interval", out var intervalElement))
        {
            intervalMs = int.Parse(ResolveVariable(intervalElement.GetString(), context));
        }
        
        var startTime = DateTime.UtcNow;
        var attempts = 0;
        
        while ((DateTime.UtcNow - startTime).TotalMilliseconds < maxWaitMs)
        {
            attempts++;
            
            if (EvaluateCondition(condition, context))
            {
                return new {
                    waitType = "condition",
                    condition,
                    satisfied = true,
                    attempts,
                    waitedMs = (DateTime.UtcNow - startTime).TotalMilliseconds
                };
            }
            
            await Task.Delay(intervalMs);
        }
        
        throw new TimeoutException($"Condition '{condition}' was not satisfied within {maxWaitMs}ms");
    }

    private bool EvaluateCondition(string condition, IExecutionContext context)
    {
        // Simple condition evaluation - could be enhanced with expression parser
        return condition.ToLower() == "true";
    }
}
```

**Usage:**
```json
{
    "type": "wait",
    "duration": "5000",
    "description": "Wait 5 seconds for system to process"
}
```

```json
{
    "type": "wait",
    "condition": "{{$.globals.processComplete}}",
    "maxWait": "30000",
    "interval": "2000",
    "description": "Wait for background process to complete"
}
```

### Script Execution Step

```csharp
public class ScriptStep : BaseStep
{
    public override string Type => "script";

    public override bool ValidateConfiguration(JsonElement configuration)
    {
        return configuration.TryGetProperty("language", out _) && 
               configuration.TryGetProperty("code", out _);
    }

    public override async Task<StepResult> ExecuteAsync(IExecutionContext context)
    {
        var contextBefore = CloneContext(context);
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var language = Configuration.GetProperty("language").GetString();
            var code = ResolveVariable(Configuration.GetProperty("code").GetString(), context);
            
            object result = language.ToLower() switch
            {
                "javascript" => await ExecuteJavaScript(code, context),
                "python" => await ExecutePython(code, context),
                "powershell" => await ExecutePowerShell(code, context),
                _ => throw new NotSupportedException($"Script language '{language}' is not supported")
            };
            
            stopwatch.Stop();
            return await ProcessStepCompletionAsync(context, contextBefore, stopwatch, result);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return StepResult.CreateFailure(this, ex.Message, stopwatch.ElapsedMilliseconds);
        }
    }

    private async Task<object> ExecuteJavaScript(string code, IExecutionContext context)
    {
        // Implementation using JavaScript engine (e.g., Jint)
        var engine = new Jint.Engine();
        
        // Inject context variables
        engine.SetValue("context", context.Variables);
        
        var result = engine.Evaluate(code);
        
        return new { 
            language = "javascript", 
            result = result?.ToString(),
            success = true 
        };
    }

    private async Task<object> ExecutePython(string code, IExecutionContext context)
    {
        // Implementation using Python.NET or process execution
        using var process = new Process();
        process.StartInfo.FileName = "python";
        process.StartInfo.Arguments = "-c \"" + code.Replace("\"", "\\\"") + "\"";
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.UseShellExecute = false;
        
        process.Start();
        var output = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();
        
        return new {
            language = "python",
            output,
            exitCode = process.ExitCode,
            success = process.ExitCode == 0
        };
    }

    private async Task<object> ExecutePowerShell(string code, IExecutionContext context)
    {
        // Implementation using PowerShell SDK
        using var ps = PowerShell.Create();
        ps.AddScript(code);
        
        var results = await Task.Run(() => ps.Invoke());
        var errors = ps.Streams.Error.Select(e => e.ToString()).ToList();
        
        return new {
            language = "powershell",
            results = results.Select(r => r.ToString()).ToList(),
            errors,
            success = !errors.Any()
        };
    }
}
```

**Usage:**
```json
{
    "type": "script",
    "language": "javascript",
    "code": "Math.random() * 1000",
    "save": {
        "$.globals.randomNumber": "{{$.this.result}}"
    }
}
```

## Custom Assertion Operations

### Creating Custom Assertions

```csharp
public class CustomAssertionOperation : IAssertionOperation
{
    public string Name => "custom_operation";

    public Task<AssertionResult> EvaluateAsync(
        JsonElement assertion, 
        IExecutionContext context)
    {
        var actualValue = GetActualValue(assertion, context);
        var expectedValue = GetExpectedValue(assertion, context);
        
        // Implement custom assertion logic
        var success = PerformCustomAssertion(actualValue, expectedValue);
        
        return Task.FromResult(new AssertionResult
        {
            Success = success,
            Operation = Name,
            ActualValue = actualValue,
            ExpectedValue = expectedValue,
            Message = success ? "Assertion passed" : $"Custom assertion failed: {actualValue} vs {expectedValue}"
        });
    }

    private bool PerformCustomAssertion(object actual, object expected)
    {
        // Implement your custom assertion logic
        return actual?.ToString().Length == expected?.ToString().Length;
    }
}
```

### Registering Custom Assertions

```csharp
public class CustomAssertionProcessor : DefaultAssertionProcessor
{
    protected override void RegisterOperations()
    {
        base.RegisterOperations();
        RegisterOperation(new CustomAssertionOperation());
    }
}
```

## Step Registration

### Automatic Registration

Steps are automatically discovered and registered using reflection:

```csharp
[StepType("custom")]
public class CustomStep : BaseStep
{
    // Implementation
}
```

### Manual Registration

Register steps programmatically:

```csharp
public class CustomStepRegistry : IStepRegistry
{
    public void RegisterSteps(IStepFactory factory)
    {
        factory.RegisterStep<CustomStep>("custom");
        factory.RegisterStep<FileSystemStep>("file");
        factory.RegisterStep<DatabaseStep>("database");
        factory.RegisterStep<WaitStep>("wait");
        factory.RegisterStep<ScriptStep>("script");
    }
}
```

### Dependency Injection

Configure dependencies for custom steps:

```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // Register core services
        services.AddJTestCore();
        
        // Register custom step dependencies
        services.AddTransient<IDbConnectionFactory, SqlConnectionFactory>();
        services.AddTransient<FileSystemStep>();
        services.AddTransient<DatabaseStep>();
        
        // Register custom step registry
        services.AddTransient<IStepRegistry, CustomStepRegistry>();
    }
}
```

## JSON Schema Integration

### Step Schema Definition

Define JSON schemas for your custom steps:

**custom-step.schema.json:**
```json
{
    "$schema": "http://json-schema.org/draft-07/schema#",
    "type": "object",
    "title": "Custom Step",
    "description": "Custom step for specialized operations",
    "properties": {
        "type": {
            "const": "custom",
            "description": "Step type identifier"
        },
        "operation": {
            "type": "string",
            "enum": ["read", "write", "process"],
            "description": "Operation to perform"
        },
        "parameters": {
            "type": "object",
            "description": "Operation-specific parameters",
            "additionalProperties": true
        }
    },
    "required": ["type", "operation"],
    "additionalProperties": true
}
```

### Schema Validation

Integrate schema validation in your step:

```csharp
public class CustomStep : BaseStep
{
    private static readonly JsonSchema Schema = JsonSchema.FromFile("custom-step.schema.json");

    public override bool ValidateConfiguration(JsonElement configuration)
    {
        var document = JsonDocument.Parse(configuration.GetRawText());
        var validation = Schema.Evaluate(document);
        
        if (!validation.IsValid)
        {
            var errors = validation.GetAllErrors().Select(e => e.Message);
            throw new InvalidOperationException($"Configuration validation failed: {string.Join(", ", errors)}");
        }
        
        return true;
    }
}
```

## Extension Development Guidelines

### Best Practices

1. **Single Responsibility** - Each step type should have a clear, focused purpose
2. **Consistent Naming** - Use descriptive, consistent names for step types and properties
3. **Error Handling** - Provide clear, actionable error messages
4. **Documentation** - Include comprehensive documentation and examples
5. **Testing** - Thoroughly test step implementations in isolation and integration
6. **Performance** - Consider performance implications, especially for frequently used steps

### Configuration Patterns

```csharp
// Good: Clear, validated configuration
public override bool ValidateConfiguration(JsonElement configuration)
{
    var required = new[] { "operation", "target" };
    foreach (var prop in required)
    {
        if (!configuration.TryGetProperty(prop, out _))
        {
            throw new InvalidOperationException($"Required property '{prop}' is missing");
        }
    }
    return true;
}

// Avoid: Unvalidated or unclear configuration access
public override async Task<StepResult> ExecuteAsync(IExecutionContext context)
{
    var operation = Configuration.GetProperty("op").GetString(); // Unclear property name
    // No validation of required properties
}
```

### Context Usage Guidelines

```csharp
// Good: Proper context interaction
public override async Task<StepResult> ExecuteAsync(IExecutionContext context)
{
    var contextBefore = CloneContext(context);
    var stopwatch = Stopwatch.StartNew();
    
    try
    {
        // Resolve variables with proper error handling
        var target = ResolveVariable(
            Configuration.GetProperty("target").GetString(), 
            context
        );
        
        // Perform operation
        var result = await PerformOperation(target);
        
        stopwatch.Stop();
        
        // Use standard completion processing
        return await ProcessStepCompletionAsync(context, contextBefore, stopwatch, result);
    }
    catch (Exception ex)
    {
        stopwatch.Stop();
        context.Log.Add($"Custom step failed: {ex.Message}");
        return StepResult.CreateFailure(this, ex.Message, stopwatch.ElapsedMilliseconds);
    }
}
```

### Error Handling Patterns

```csharp
// Good: Comprehensive error handling
try
{
    var result = await PerformOperation();
    return await ProcessStepCompletionAsync(context, contextBefore, stopwatch, result);
}
catch (FileNotFoundException ex)
{
    return StepResult.CreateFailure(this, $"File not found: {ex.FileName}", stopwatch.ElapsedMilliseconds);
}
catch (UnauthorizedAccessException ex)
{
    return StepResult.CreateFailure(this, $"Access denied: {ex.Message}", stopwatch.ElapsedMilliseconds);
}
catch (Exception ex)
{
    context.Log.Add($"Unexpected error in {Type} step: {ex}");
    return StepResult.CreateFailure(this, ex.Message, stopwatch.ElapsedMilliseconds);
}
```

## Future Extensibility Considerations

The architecture supports future enhancements:

- **Event System** - Pre/post execution hooks for cross-cutting concerns
- **Middleware Pipeline** - Request/response transformation pipeline  
- **Custom Exporters** - Additional export formats beyond Postman and Karate
- **Remote Step Execution** - Steps that execute on remote systems
- **Parallel Execution** - Steps that can execute concurrently
- **Conditional Logic** - Built-in conditional execution and loops
- **Data Providers** - External data sources for test parameterization

## Extension Examples

### Plugin Package Structure

```
MyJTestExtensions/
├── src/
│   ├── Steps/
│   │   ├── CustomStep.cs
│   │   ├── FileSystemStep.cs
│   │   └── DatabaseStep.cs
│   ├── Assertions/
│   │   └── CustomAssertions.cs
│   ├── Schemas/
│   │   ├── custom-step.schema.json
│   │   └── file-step.schema.json
│   └── MyJTestExtensions.csproj
├── tests/
│   ├── CustomStepTests.cs
│   └── test-examples/
└── docs/
    ├── README.md
    └── step-documentation.md
```

### NuGet Package Creation

**MyJTestExtensions.csproj:**
```xml
<Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
        <TargetFramework>net8.0</TargetFramework>
        <PackageId>MyCompany.JTest.Extensions</PackageId>
        <PackageVersion>1.0.0</PackageVersion>
        <Authors>Your Name</Authors>
        <Description>Custom JTest extensions for specialized testing scenarios</Description>
        <PackageTags>jtest;testing;api;extensions</PackageTags>
    </PropertyGroup>
    
    <ItemGroup>
        <PackageReference Include="JTest.Core" Version="1.0.0" />
    </ItemGroup>
    
    <ItemGroup>
        <Content Include="Schemas\*.json">
            <Pack>true</Pack>
            <PackagePath>schemas\</PackagePath>
        </Content>
    </ItemGroup>
</Project>
```

This extensible foundation ensures that JTest can evolve to meet new testing requirements while maintaining backward compatibility and ease of use.

## Next Steps

- [Best Practices](06-best-practices.md) - Extension development patterns
- [CLI Usage](cli-usage.md) - Using custom steps via CLI
- [Troubleshooting](troubleshooting.md) - Debugging custom steps