# JTest File Structure Reference

This document provides a comprehensive explanation of JTest file structures, including test suites, templates, and all supported configurations. This serves as the definitive reference for understanding how JTest organizes and executes tests.

## Table of Contents

- [Test Suite Structure](#test-suite-structure)
- [Template Structure](#template-structure)
- [Step Types](#step-types)
- [Variable Context and Scope](#variable-context-and-scope)
- [Dataset Structure](#dataset-structure)
- [Complete Configuration Reference](#complete-configuration-reference)
- [Implementation Details](#implementation-details)

## Test Suite Structure

A JTest suite is the main container for your tests. It follows this structure:

```json
{
    "version": "1.0",
    "info": {
        "name": "Test Suite Name",
        "description": "Description of what this suite tests"
    },
    "using": [
        "./path/to/templates.json",
        "./other/templates.json"
    ],
    "env": {
        "baseUrl": "https://api.example.com",
        "apiKey": "your-api-key",
        "username": "test-user"
    },
    "globals": {
        "authToken": null,
        "sharedData": "initial-value"
    },
    "tests": [
        // Individual test cases go here
    ]
}
```

### Test Suite Properties

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `version` | string | Yes | Version of the JTest format (e.g., "1.0") |
| `info` | object | No | Metadata about the test suite |
| `info.name` | string | No | Human-readable name for the suite |
| `info.description` | string | No | Description of the test suite purpose |
| `using` | array[string] | No | List of template files to include |
| `env` | object | No | Environment variables available to all tests |
| `globals` | object | No | Global variables shared across all tests |
| `tests` | array[JTestCase] | Yes | List of test cases to execute |

### Intent and Usage

- **Test Suites** are designed to group related tests that share common setup, environment variables, or templates
- **Environment variables** (`env`) are intended for configuration that might change between environments (dev/staging/prod)
- **Global variables** (`globals`) are for runtime data that accumulates across tests (tokens, shared IDs, etc.)
- **Template imports** (`using`) allow code reuse and modular test design

## Test Case Structure

Individual test cases within a suite follow this structure:

```json
{
    "name": "Test Case Name",
    "description": "What this test validates",
    "steps": [
        // Steps to execute
    ],
    "datasets": [
        // Optional: data-driven test variations
    ]
}
```

### Test Case Properties

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `name` | string | Yes | Test case identifier (appears in reports) |
| `description` | string | No | Human-readable description |
| `steps` | array[Step] | Yes | Sequence of steps to execute |
| `datasets` | array[JTestDataset] | No | Data variations for data-driven testing |

### Intent and Usage

- **Names** should be descriptive as they appear in markdown reports
- **Descriptions** provide context for test maintainers and in reports
- **Steps** execute sequentially, each can access results from previous steps
- **Datasets** run the same test logic with different data, perfect for boundary testing

## Template Structure

Templates are reusable test components that run in isolated contexts:

```json
{
    "version": "1.0",
    "components": {
        "templates": [
            {
                "name": "authenticate",
                "description": "Authenticate user and return tokens",
                "params": {
                    "username": {
                        "type": "string",
                        "required": true,
                        "description": "Username for authentication"
                    },
                    "password": {
                        "type": "string",
                        "required": true,
                        "description": "Password for authentication"
                    },
                    "tokenUrl": {
                        "type": "string", 
                        "required": true,
                        "description": "URL for token endpoint"
                    }
                },
                "steps": [
                    {
                        "type": "http",
                        "method": "POST",
                        "url": "{{$.params.tokenUrl}}",
                        "json": {
                            "username": "{{$.params.username}}",
                            "password": "{{$.params.password}}"
                        },
                        "id": "auth_response",
                        "assert": [
                            {
                                "op": "equals",
                                "actualValue": "{{$.auth_response.status}}",
                                "expectedValue": 200
                            }
                        ]
                    }
                ],
                "output": {
                    "token": "{{$.auth_response.body.accessToken}}",
                    "authHeader": "Bearer {{$.auth_response.body.accessToken}}"
                }
            }
        ]
    }
}
```

### Template Properties

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `name` | string | Yes | Template identifier for `use` steps |
| `description` | string | No | Template purpose documentation |
| `params` | object | No | Parameter definitions with validation |
| `steps` | array[Step] | Yes | Steps to execute within template |
| `output` | object | No | Values to expose to parent context |

### Template Parameter Properties

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `type` | string | No | Parameter type (string, number, boolean, object) |
| `required` | boolean | No | Whether parameter is mandatory |
| `description` | string | No | Parameter documentation |
| `default` | any | No | Default value if not provided |

### Template Context Isolation

Templates function like **static methods** with these key principles:

- **Isolated Context**: Templates run in their own variable context
- **Input Only**: Only parameters passed via `with` are available inside
- **Explicit Output**: Only values defined in `output` are accessible to parent
- **No Side Effects**: Templates cannot modify parent context directly
- **Auto-Save**: Steps with `id` save results automatically within template context

## Step Types

JTest supports several step types for different testing scenarios:

### HTTP Step

Makes HTTP requests and captures responses:

```json
{
    "type": "http",
    "method": "GET|POST|PUT|DELETE|PATCH",
    "url": "https://api.example.com/endpoint",
    "headers": {
        "Authorization": "Bearer {{$.globals.token}}",
        "Content-Type": "application/json"
    },
    "json": {
        "key": "value"
    },
    "id": "response_data",
    "assert": [
        // Inline assertions
    ]
}
```

**Intent**: Primary step for API testing, capturing responses for validation and data extraction.

### Assert Step

Standalone assertions for validation:

```json
{
    "type": "assert",
    "assertions": [
        {
            "op": "equals",
            "actualValue": "{{$.response_data.status}}",
            "expectedValue": 200,
            "description": "Should return success status"
        }
    ]
}
```

**Intent**: Dedicated validation step when assertions need to be separate from HTTP requests.

### Use Step

Executes templates with parameters:

```json
{
    "type": "use",
    "template": "authenticate",
    "with": {
        "username": "{{$.env.username}}",
        "password": "{{$.env.password}}",
        "tokenUrl": "{{$.env.tokenUrl}}"
    },
    "save": {
        "$.globals.token": "{{$.this.token}}",
        "$.globals.authHeader": "{{$.this.authHeader}}"
    },
    "assert": [
        // Assertions on template output
    ]
}
```

**Intent**: Code reuse through template execution, promoting DRY principles and maintainable test suites.

### Wait Step

Introduces delays in test execution:

```json
{
    "type": "wait",
    "ms": 50
}
```

**Intent**: Handle asynchronous operations, rate limiting, or timing-dependent scenarios.

## Variable Context and Scope

JTest uses a hierarchical variable context system:

### Context Hierarchy

1. **Environment** (`$.env.*`) - Configuration variables
2. **Globals** (`$.globals.*`) - Shared runtime data  
3. **Case** (`$.case.*`) - Dataset-specific variables
4. **Step Results** (`$.step_id.*`) - Auto-saved step outputs
5. **Template Parameters** (`$.params.*`) - Template input (template context only)
6. **Template Output** (`$.this.*`) - Template results (calling context only)

### Variable Types and Usage

```json
{
    "env": {
        "baseUrl": "https://api.example.com",    // string
        "timeout": 30,                           // number
        "debug": true,                          // boolean
        "headers": {                            // object
            "User-Agent": "JTest/1.0"
        }
    },
    "globals": {
        "token": null,                          // initially null, set at runtime
        "userCount": 0,                         // accumulator
        "sessionData": {}                       // object for complex data
    }
}
```

### Auto-Save with Step IDs

Steps with `id` properties automatically save their results:

```json
{
    "type": "http",
    "method": "GET",
    "url": "{{$.env.baseUrl}}/users",
    "id": "user_list",     // Auto-saves response as $.user_list
    "assert": [
        {
            "op": "greaterthan",
            "actualValue": "{{$.user_list.body.length}}",  // Access saved data
            "expectedValue": 0
        }
    ]
}
```

**Saved Structure**:
- `$.user_list.status` - HTTP status code
- `$.user_list.headers` - Response headers
- `$.user_list.body` - Response body (parsed JSON if applicable)
- `$.user_list.duration` - Request duration in milliseconds

## Dataset Structure

Datasets enable data-driven testing by running the same test with different inputs:

```json
{
    "name": "User Registration Tests",
    "steps": [
        {
            "type": "http",
            "method": "POST", 
            "url": "{{$.env.baseUrl}}/register",
            "json": {
                "username": "{{$.case.username}}",
                "email": "{{$.case.email}}",
                "password": "{{$.case.password}}"
            },
            "assert": [
                {
                    "op": "equals",
                    "actualValue": "{{$.this.status}}",
                    "expectedValue": "{{$.case.expectedStatus}}",
                    "description": "{{$.case.description}}"
                }
            ]
        }
    ],
    "datasets": [
        {
            "name": "valid-user",
            "case": {
                "username": "john_doe",
                "email": "john@example.com", 
                "password": "SecurePass123!",
                "expectedStatus": 201,
                "description": "Valid user registration should succeed"
            }
        },
        {
            "name": "invalid-email",
            "case": {
                "username": "jane_doe",
                "email": "invalid-email",
                "password": "SecurePass123!",
                "expectedStatus": 400,
                "description": "Invalid email should be rejected"
            }
        }
    ]
}
```

### Dataset Properties

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `name` | string | Yes | Dataset identifier (appears in reports) |
| `case` | object | Yes | Variables available as `$.case.*` |

### Intent and Usage

- **Data-driven testing** for boundary conditions, edge cases, and input validation
- **Same logic, different data** - perfect for testing various scenarios
- **Clear reporting** - each dataset run appears separately in results
- **Parameterized descriptions** - use `{{$.case.*}}` in assertion descriptions

## Complete Configuration Reference

### Assertion Operators

All supported assertion operators (implementation-verified):

| Operator | Description | Example |
|----------|-------------|---------|
| `equals` | Exact equality | `"actual": "hello", "expected": "hello"` |
| `notequals` | Not equal | `"actual": "hello", "expected": "world"` |
| `exists` | Value exists and not null | `"actual": "{{$.response.id}}"` |
| `notexists` | Value is null/undefined | `"actual": "{{$.response.optional}}"` |
| `contains` | String/array contains value | `"actual": "hello world", "expected": "world"` |
| `notcontains` | String/array doesn't contain | `"actual": "hello", "expected": "world"` |
| `startswith` | String starts with value | `"actual": "hello world", "expected": "hello"` |
| `endswith` | String ends with value | `"actual": "hello world", "expected": "world"` |
| `matches` | Regex pattern match | `"actual": "test123", "expected": "^test\\d+$"` |
| `greaterthan` | Numeric greater than | `"actual": 10, "expected": 5` |
| `greaterorequal` | Numeric greater or equal | `"actual": 10, "expected": 10` |
| `lessthan` | Numeric less than | `"actual": 5, "expected": 10` |
| `lessorequal` | Numeric less or equal | `"actual": 5, "expected": 5` |
| `between` | Numeric between range | `"actual": 5, "expected": [1, 10]` |
| `length` | Array/string length | `"actual": [1,2,3], "expected": 3` |
| `empty` | Empty string/array/object | `"actual": ""` or `"actual": []` |
| `notempty` | Not empty | `"actual": "hello"` |
| `in` | Value in array | `"actual": "red", "expected": ["red","blue"]` |
| `type` | Type checking | `"actual": 123, "expected": "number"` |

### Variable Interpolation

JTest uses `{{$.path}}` syntax for variable interpolation:

```json
{
    "baseUrl": "{{$.env.baseUrl}}",                    // Environment variable
    "token": "{{$.globals.authToken}}",               // Global variable  
    "userId": "{{$.case.userId}}",                    // Dataset variable
    "previousResponse": "{{$.step1.body.id}}",        // Step result
    "templateParam": "{{$.params.username}}",         // Template parameter
    "templateOutput": "{{$.this.token}}",             // Template output
    "computed": "User: {{$.case.username}} ({{$.case.email}})"  // String interpolation
}
```

### File Organization Patterns

**Test Suite Files** (`.json`):
- Single suite per file
- Name should reflect the feature/component being tested
- Include environment-specific configurations

**Template Files** (`.json`):
- Group related templates by domain/functionality
- Use descriptive template names
- Document parameters thoroughly

**Recommended Structure**:
```
tests/
├── auth-tests.json              # Authentication test suite
├── user-management-tests.json   # User CRUD operations
├── templates/
│   ├── auth-templates.json      # Authentication templates
│   ├── user-templates.json      # User management templates
│   └── common-templates.json    # Shared utilities
└── environments/
    ├── dev.env.json            # Development environment
    ├── staging.env.json        # Staging environment  
    └── prod.env.json           # Production environment
```

## Implementation Details

### Step Execution Order

1. **Variable Resolution** - All `{{$.path}}` expressions are resolved
2. **Step Execution** - HTTP request, template call, or assertion
3. **Auto-Save** - If `id` is present, results saved to context
4. **Manual Save** - If `save` is present, specified values stored
5. **Assertions** - If `assert` is present, validations run
6. **Context Update** - Results available for subsequent steps

### Template Execution Context

Templates maintain complete isolation:

```json
// Parent context before template call
{
    "env": { "baseUrl": "https://api.com" },
    "globals": { "token": null },
    "user_data": { "id": 123 }
}

// Template receives only parameters
{
    "params": { 
        "username": "john",
        "password": "secret" 
    }
    // No access to parent env, globals, or other variables
}

// Template output merged back to parent
{
    "env": { "baseUrl": "https://api.com" },
    "globals": { "token": null },
    "user_data": { "id": 123 },
    "this": {                    // Template output
        "token": "abc123",
        "authHeader": "Bearer abc123"
    }
}
```

### Error Handling and Debugging

- **Assertion Failures** - Test continues unless critical step fails
- **HTTP Errors** - 4xx/5xx responses can still be asserted against
- **Template Failures** - Bubble up to calling context with detailed error info
- **Variable Resolution Errors** - Clear error messages for missing variables
- **Markdown Reports** - Detailed execution logs with timing and variable states

### Performance Considerations

- **Template Context Isolation** - Prevents variable collisions but requires explicit output
- **Auto-Save Optimization** - Only saves when `id` is specified to minimize memory usage
- **Parallel Execution** - Datasets run in parallel when possible
- **HTTP Connection Reuse** - Optimized for API testing scenarios

This reference covers the complete JTest structure and implementation. For getting started, see the numbered guide files (01-getting-started.md, etc.). For specific features, refer to the focused documentation in the `steps/` directory.