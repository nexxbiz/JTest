# Assertion Processing Implementation

This implementation adds comprehensive assertion processing to the JTest framework, allowing steps to validate their results after execution.

## Key Features Implemented

### 1. IAssertionProcessor Interface
```csharp
public interface IAssertionProcessor
{
    Task<List<AssertionResult>> ProcessAssertionsAsync(JsonElement assertArray, IExecutionContext context);
}
```

### 2. Enhanced AssertionResult
```csharp
public class AssertionResult
{
    public bool Success { get; set; }
    public string Operation { get; set; } = "";
    public string Description { get; set; } = "";
    public object? ActualValue { get; set; }
    public object? ExpectedValue { get; set; }
    public string ErrorMessage { get; set; } = "";
}
```

### 3. Step Integration
Steps now automatically process assertions after execution:

```csharp
// In BaseStep.cs
protected async Task<List<AssertionResult>> ProcessAssertionsAsync(IExecutionContext context)
{
    if (Configuration.ValueKind == JsonValueKind.Undefined || !Configuration.TryGetProperty("assert", out var assertElement))
    {
        return new List<AssertionResult>();
    }

    var processor = new DefaultAssertionProcessor();
    return await processor.ProcessAssertionsAsync(assertElement, context);
}
```

### 4. Usage Example

```json
{
  "type": "wait",
  "ms": 100,
  "assert": [
    {
      "op": "exists",
      "actualValue": "{{$.this.ms}}"
    },
    {
      "op": "equals", 
      "actualValue": "{{$.this.ms}}",
      "expectedValue": 100
    }
  ]
}
```

## Supported Assertion Operations

- `equals`: Compare actualValue with expectedValue
- `exists`: Check if actualValue is not null/empty
- `greater-than`: Numeric comparison (actualValue > expectedValue)
- `less-than`: Numeric comparison (actualValue < expectedValue)
- `contains`: String contains check
- And many more...

## Token Resolution

Assertions support JSONPath expressions for dynamic value resolution:
- `{{$.this.property}}` - Access step result data
- `{{$.stepId.property}}` - Access named step data
- `{{$.env.variable}}` - Access environment variables

## StepResult Enhancement

Step results now include assertion results:

```csharp
public class StepResult
{
    // ... existing properties
    public List<AssertionResult> AssertionResults { get; set; } = new();
}
```

This implementation provides a robust, extensible assertion framework that integrates seamlessly with the existing JTest architecture.