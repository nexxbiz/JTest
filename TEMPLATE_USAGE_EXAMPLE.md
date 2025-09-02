# Template Step Implementation - Example Usage

This document demonstrates the template step functionality that has been implemented.

## Basic Template Usage

### 1. Create a Template File (`auth-template.json`)

```json
{
    "version": "1.0",
    "components": {
        "templates": [
            {
                "name": "authenticate",
                "description": "Generate authentication token",
                "params": {
                    "username": { "type": "string", "required": true },
                    "password": { "type": "string", "required": true }
                },
                "steps": [],
                "output": {
                    "token": "{{$.username}}-{{$.password}}-token",
                    "authHeader": "Bearer {{$.username}}-{{$.password}}-token"
                }
            }
        ]
    }
}
```

### 2. Use Template in a Test (`test-with-template.json`)

```json
{
    "name": "API test with authentication template",
    "flow": [
        {
            "type": "use",
            "template": "authenticate",
            "with": {
                "username": "{{$.env.username}}",
                "password": "{{$.env.password}}"
            },
            "save": {
                "$.globals.authToken": "{{$.this.token}}"
            }
        }
    ]
}
```

### 3. Run with C# Code

```csharp
using JTest.Core;

var testRunner = new TestRunner();

// Load templates
var templateJson = File.ReadAllText("auth-template.json");
testRunner.LoadTemplates(templateJson);

// Run test
var testJson = File.ReadAllText("test-with-template.json");
var environment = new Dictionary<string, object>
{
    ["username"] = "testuser",
    ["password"] = "secret123"
};

var results = await testRunner.RunTestAsync(testJson, environment);
```

## Key Features

### Context Isolation
- Templates execute in their own isolated context
- Template parameters don't leak to parent context
- Only template outputs are exposed to parent context

### Parameter Validation
- Required parameters are validated
- Default values supported for optional parameters
- Type checking as specified in template definition

### Output Mapping
- Template outputs available via consistent access: `{{$.this.outputKey}}`
- Supports complex output expressions with variable interpolation
- Clean isolation ensures only explicitly defined outputs are exposed

### Debug Support
- MarkdownDebugLogger shows template step execution
- Template context changes logged separately from parent context
- Full visibility into template parameter resolution

### Integration
- Works seamlessly with existing JTest step system
- Compatible with all existing features (save, assertions, etc.)
- Can be used with datasets and all other JTest functionality

## Template Context Behavior

When a template executes:

1. **Isolated Context**: Creates completely separate execution context
2. **Parameter Access**: Template parameters available as `{{$.paramName}}`
3. **Step Execution**: Template steps run in isolated context
4. **Output Mapping**: Only defined outputs are mapped to parent context via `{{$.this.outputKey}}`
5. **Parent Preservation**: Parent context unchanged except for outputs

This ensures clean separation between template logic and test logic while providing a powerful reusable component system.