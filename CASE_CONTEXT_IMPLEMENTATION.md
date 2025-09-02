# Case Context and Datasets Implementation

This implementation provides complete support for case context ($.case.*) and datasets functionality as specified in the problem statement.

## Key Features Implemented

### 1. Case Context ($.case.*)
- Special runtime scope providing access to dataset variables during test execution
- Stored in context.Variables["case"] and accessible via JSONPath expressions
- Immutable during test execution - set once per dataset iteration
- Available in all step types: HTTP requests, assertions, assignments, templates

### 2. Dataset Support
- JTestCase class with optional `datasets` property
- JTestDataset class with `name` and `case` dictionary
- Data-driven testing where same test flow runs multiple times with different input data

### 3. Execution Flow
- Test without datasets → Single execution with no case context
- Test with datasets → Multiple executions via TestCaseExecutor, one per dataset
- Each dataset execution gets its own JTestCaseResult with dataset reference

## Usage Example

```json
{
  "name": "Order processing",
  "flow": [
    {
      "type": "http",
      "id": "createOrder", 
      "method": "POST",
      "url": "{{$.env.baseUrl}}/orders",
      "body": "{{$.case.orderPayload}}",
      "assert": [
        { "op": "equals", "actualValue": "$.this.status", "expectedValue": 201 }
      ],
      "save": { "orderId": "$.this.body.id" }
    },
    {
      "type": "http",
      "id": "getOrder",
      "method": "GET", 
      "url": "{{$.env.baseUrl}}/accounts/{{$.case.accountId}}/orders/{{$.orderId}}",
      "assert": [
        { "op": "equals", "actualValue": "$.this.body.total", "expectedValue": "$.case.expectedTotal" }
      ]
    }
  ],
  "datasets": [
    {
      "name": "basic",
      "case": {
        "accountId": "acct-1001",
        "orderPayload": { "sku": "SKU-1", "qty": 2 },
        "expectedTotal": 20
      }
    },
    {
      "name": "discounted",
      "case": {
        "accountId": "acct-1002",
        "orderPayload": { "sku": "SKU-2", "qty": 3, "discountPct": 10 },
        "expectedTotal": 27
      }
    }
  ]
}
```

## Code Usage

```csharp
// Parse test case from JSON
var testCase = JsonSerializer.Deserialize<JTestCase>(jsonContent);

// Set up execution context
var baseContext = new TestExecutionContext();
baseContext.Variables["env"] = new { baseUrl = "https://api.example.com" };

// Execute with datasets
var executor = new TestCaseExecutor();
var results = await executor.ExecuteAsync(testCase, baseContext);

// Results contains one JTestCaseResult per dataset
// Each result includes the dataset reference and execution details
```

## Implementation Details

### Models
- **JTestCase**: Test definition with optional datasets
- **JTestDataset**: Dataset with name and case variables
- **JTestCaseResult**: Execution result with dataset reference

### Execution
- **TestExecutionContext**: Runtime context with SetCase() method
- **TestCaseExecutor**: Handles dataset iteration and context management
- **VariableInterpolator**: Already supports $.case.* JSONPath expressions

### Variable Scopes
All variable scopes work together seamlessly:
- `$.env.*` - Environment variables 
- `$.globals.*` - Global variables shared across tests
- `$.case.*` - Dataset-specific variables (NEW)
- `$.ctx.*` - Step context variables
- `$.this.*` - Current step response data
- `$.stepId.*` - Previous step results

## Testing
Comprehensive test coverage with 139 tests including:
- Case context variable resolution
- Dataset execution flow
- Complex nested object support  
- Integration tests with real-world examples
- Variable scope interaction validation