# JTest comprehensive guide

## Table of contents

1. [Introduction](#introduction)
2. [Core concepts](#core-concepts)
3. [Getting started](#getting-started)
4. [Test structure](#test-structure)
5. [Step types](#step-types)
6. [Context and variables](#context-and-variables)
7. [Templates](#templates)
8. [Assertions](#assertions)
9. [CLI usage](#cli-usage)
10. [Debugging and troubleshooting](#debugging-and-troubleshooting)
11. [CI/CD integration](#cicd-integration)
12. [Advanced topics](#advanced-topics)
13. [Extensibility](#extensibility)

## Introduction

JTest is a universal test definition language designed for API testing using JSON notation and JSONPath expressions. It allows testers and QA engineers to write tests in a technology-agnostic way, making it easy to create, maintain, and execute API tests across different environments.

### Key features

- **JSON-based test definitions**: Write tests using familiar JSON syntax
- **JSONPath expressions**: Use JSONPath for data extraction and assertions
- **Template system**: Create reusable test components for common workflows
- **Context management**: Powerful variable system for data sharing between test steps
- **CLI tool**: Command-line interface for running, debugging, and exporting tests
- **Export capabilities**: Convert tests to other frameworks like Postman and Karate
- **CI/CD ready**: Designed to run in continuous integration pipelines

### Design philosophy

JTest follows these core principles:

- **Technology agnostic**: Tests are defined independently of execution framework
- **Extensible**: Easy to add new step types without modifying core code
- **Context-driven**: Rich context system for variable management and data flow
- **Developer friendly**: JSON structure with clear separation of concerns

## Core concepts

### Test execution context

The execution context is the runtime environment where tests run. It consists of several variable scopes:

- **env**: Environment variables (configuration, URLs, credentials)
- **globals**: Global variables shared across all tests
- **case**: Test case-specific variables
- **ctx**: Step-specific context variables
- **this**: Current step's response data (status, headers, body)
- **now**: Current timestamp information
- **random**: Random values (UUIDs, etc.)

### JSONPath expressions

JTest uses JSONPath to reference data within the context. JSONPath expressions are enclosed in double curly braces:

```json
"{{$.env.baseUrl}}"
"{{$.this.status}}"
"{{$.execute-workflow.body.workflowInstanceId}}"
```

### Step-based execution

Tests are composed of steps that execute sequentially. Each step can:

- Perform an action (HTTP request, template usage)
- Make assertions on data
- Save data to context for later use
- Modify the execution context

## Getting started

### Installation

1. Build the JTest CLI tool from source
2. Add the executable to your system PATH
3. Verify installation by running `jtest --help`

### Your first test

Create a simple test file called `hello-world.json`:

```json
{
    "version": "1.0",
    "info": {
        "name": "Hello world test"
    },
    "env": {
        "baseUrl": "https://jsonplaceholder.typicode.com"
    },
    "tests": [
        {
            "name": "Get first post",
            "steps": [
                {
                    "type": "http",
                    "id": "get-post",
                    "method": "GET",
                    "url": "{{$.env.baseUrl}}/posts/1",
                    "assert": [
                        {
                            "op": "equals",
                            "actualValue": "{{$.this.status}}",
                            "expectedValue": 200
                        },
                        {
                            "op": "exists",
                            "actualValue": "{{$.this.body.id}}"
                        }
                    ]
                }
            ]
        }
    ]
}
```

Run the test:

```bash
jtest hello-world.json
```

## Test structure

### Root structure

Every JTest file has this basic structure:

```json
{
    "version": "1.0",
    "info": {
        "name": "Test suite name",
        "description": "Optional description"
    },
    "using": [
        "./path/to/templates.json"
    ],
    "env": {
        "key": "value"
    },
    "globals": {
        "key": "value"
    },
    "tests": [
        // Test cases
    ]
}
```

#### Properties explained

- **version**: JTest schema version (currently "1.0")
- **info**: Metadata about the test suite
- **using**: Array of template files to import
- **env**: Environment variables available as `{{$.env.key}}`
- **globals**: Global variables available as `{{$.globals.key}}`
- **tests**: Array of test cases to execute

### Test case structure

Each test case contains:

```json
{
    "name": "Test case name",
    "description": "Optional description",
    "enabled": true,
    "steps": [
        // Test steps
    ]
}
```

### Step structure

All steps share common properties:

```json
{
    "type": "step-type",
    "id": "optional-step-id",
    "enabled": true,
    "description": "Optional description"
}
```

- **type**: The step type (http, use, etc.)
- **id**: Optional identifier for referencing step results
- **enabled**: Whether to execute this step (default: true)
- **description**: Human-readable description

## Step types

### HTTP step

The HTTP step performs HTTP requests and is the primary way to test APIs.

```json
{
    "type": "http",
    "id": "api-call",
    "method": "POST",
    "url": "{{$.env.baseUrl}}/api/endpoint",
    "headers": [
        {
            "name": "Content-Type",
            "value": "application/json"
        },
        {
            "name": "Authorization",
            "value": "Bearer {{$.globals.token}}"
        }
    ],
    "body": {
        "key": "value",
        "dynamic": "{{$.previous-step.body.id}}"
    },
    "assert": [
        {
            "op": "equals",
            "actualValue": "{{$.this.status}}",
            "expectedValue": 200
        }
    ],
    "save": {
        "$.ctx.responseId": "{{$.this.body.id}}",
        "$.globals.lastApiCall": "{{$.now.iso}}"
    }
}
```

#### HTTP step properties

- **method**: HTTP method (GET, POST, PUT, DELETE, etc.)
- **url**: Request URL with variable substitution
- **headers**: Array of header objects with name/value pairs
- **body**: Request body (object for JSON, string for other types)
- **assert**: Array of assertion objects
- **save**: Object mapping explicit JSONPath targets to source expressions

### Use step

The use step executes a template with provided parameters.

```json
{
    "type": "use",
    "template": "template-name",
    "with": {
        "param1": "value1",
        "param2": "{{$.env.configValue}}"
    }
}
```

#### Use step properties

- **template**: Name of the template to execute
- **with**: Object containing parameter values for the template

## Context and variables

### Understanding context

Context is the runtime state that persists throughout test execution. Each step can read from and write to the context.

### Context scopes

#### Environment variables (env)

Environment variables are set at the test suite level and remain constant, but can be set from command line or modified during test execution. afterthat change is not possible:

```json
{
    "env": {
        "baseUrl": "https://api.example.com",
        "apiVersion": "v1",
        "timeout": 30000
    }
}
```

Access with: `{{$.env.baseUrl}}`

#### Global variables (globals)

Global variables can be set from command line or modified during test execution:

```json
{
    "globals": {
        "userId": null,
        "sessionToken": null
    }
}
```

Access with: `{{$.globals.userId}}`

#### Step response (this)

The `this` object contains the current step's response:

```json
{
    "status": 200,
    "headers": [
        {
            "name": "Content-Type",
            "value": "application/json"
        }
    ],
    "body": {
        "id": 123,
        "name": "Example"
    }
}
```

Access with: `{{$.this.status}}`, `{{$.this.body.id}}`

#### Step results by ID

When a step has an ID, its results are saved in context:

```json
{
    "type": "http",
    "id": "login",
    "method": "POST",
    "url": "{{$.env.baseUrl}}/auth/login"
}
```

Later steps can access: `{{$.login.body.token}}`

#### Built-in variables

- **now**: Current timestamp (`{{$.now.iso}}`)
- **random**: Random values (`{{$.random.uuid}}`)

### Variable precedence

Variables are resolved in this order:

1. Step-specific context
2. Step results by ID
3. Global variables
4. Environment variables
5. Built-in variables

## Templates

Templates are reusable test components that can be parameterized and shared across test suites.

### Template definition

Templates are defined in separate JSON files:

```json
{
    "version": "1.0",
    "components": {
        "templates": [
            {
                "name": "authenticate",
                "description": "Authenticate user and obtain access token",
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
                        "description": "Token endpoint URL"
                    }
                },
                "steps": [
                    {
                        "type": "http",
                        "method": "POST",
                        "url": "{{tokenUrl}}",
                        "body": {
                            "username": "{{username}}",
                            "password": "{{password}}",
                            "grant_type": "password"
                        },
                        "assert": [
                            {
                                "op": "equals",
                                "actualValue": "{{$.this.status}}",
                                "expectedValue": 200
                            }
                        ],
                        "save": {
                            "accessToken": "{{$.this.body.access_token}}",
                            "tokenType": "{{$.this.body.token_type}}"
                        }
                    }
                ],
                "output": {
                    "token": "{{$.accessToken}}",
                    "authHeader": "{{$.tokenType}} {{$.accessToken}}"
                }
            }
        ]
    }
}
```

### Template usage

Import templates using the `using` array:

```json
{
    "version": "1.0",
    "using": [
        "./auth-templates.json"
    ],
    "tests": [
        {
            "name": "Test with authentication",
            "steps": [
                {
                    "type": "use",
                    "template": "authenticate",
                    "with": {
                        "username": "{{$.env.username}}",
                        "password": "{{$.env.password}}",
                        "tokenUrl": "{{$.env.tokenUrl}}"
                    }
                },
                {
                    "type": "http",
                    "method": "GET",
                    "url": "{{$.env.baseUrl}}/protected-endpoint",
                    "headers": [
                        {
                            "name": "Authorization",
                            "value": "{{$.output.authHeader}}"
                        }
                    ]
                }
            ]
        }
    ]
}
```

### Template parameters

Templates can define typed parameters:

- **type**: Parameter data type (string, number, boolean, object, array)
- **required**: Whether the parameter is mandatory
- **description**: Human-readable parameter description
- **default**: Default value if not provided

### Template context

Templates execute in their own isolated scope with no access to the parent test's context. They communicate with the parent context through a well-defined interface:

- **params**: Define input parameters that the template accepts
- **steps**: Execute within the template's private context
- **save** (step-level): Individual steps can save data to the template's internal context
- **output**: Defines which values from the template's context are exposed to the parent context

#### Template scope isolation

When a template executes:

1. It creates its own isolated execution context
2. Template parameters are available as variables within the template scope
3. Steps within the template can save data to the template's internal context using step-level `save`
4. The template's `output` property maps internal values to variables in the parent context
5. The parent test context remains unchanged except for the explicitly defined outputs

#### Template output mapping

The template's `output` property defines what gets exposed to the parent context:

```json
{
    "name": "authenticate",
    "params": {
        "username": { "type": "string", "required": true },
        "password": { "type": "string", "required": true },
        "tokenUrl": { "type": "string", "required": true }
    },
    "steps": [
        {
            "type": "http",
            "method": "POST",
            "url": "{{tokenUrl}}",
            "body": {
                "username": "{{username}}",
                "password": "{{password}}",
                "grant_type": "password"
            },
            "save": {
                "accessToken": "{{$.this.body.access_token}}",
                "tokenType": "{{$.this.body.token_type}}"
            }
        }
    ],
    "output": {
        "token": "{{$.accessToken}}",
        "authHeader": "{{$.tokenType}} {{$.accessToken}}"
    }
}
```

After this template executes, the parent context will have:

- `{{$.output.token}}` - containing the access token value
- `{{$.output.authHeader}}` - containing the formatted authorization header

#### Accessing template outputs

Parent tests can access template outputs using the defined output variable names. Here's a complete example showing how the `authenticate` template's outputs are used:

```json
{
    "version": "1.0",
    "using": [
        "./elsa-test-templates.json"
    ],
    "env": {
        "baseUrl": "https://api.example.com",
        "tokenUrl": "https://api.example.com/token",
        "username": "testuser",
        "password": "testpass"
    },
    "tests": [
        {
            "name": "Test with authentication",
            "steps": [
                {
                    "type": "use",
                    "template": "authenticate",
                    "with": {
                        "username": "{{$.env.username}}",
                        "password": "{{$.env.password}}",
                        "tokenUrl": "{{$.env.tokenUrl}}"
                    },
                    "save": {
                        "$.globals.authToken": "{{$.output.token}}"
                    }
                },
                {
                    "type": "http",
                    "method": "GET",
                    "url": "{{$.env.baseUrl}}/protected-endpoint",
                    "headers": [
                        {
                            "name": "Authorization",
                            "value": "{{$.output.authHeader}}"
                        }
                    ],
                    "assert": [
                        {
                            "op": "equals",
                            "actualValue": "{{$.this.status}}",
                            "expectedValue": 200
                        }
                    ]
                }
            ]
        }
    ]
}
```

In this example:
- The `authenticate` template executes and exposes `token` and `authHeader` through its `output` property
- The first step saves the token to globals using `"$.globals.authToken": "{{$.output.token}}"`
- The second step uses the template's `authHeader` output directly with `{{$.output.authHeader}}`

#### Alternative output access pattern

You can also access template outputs using direct variable names without the `$.output.` prefix:

```json
{
    "name": "Authorization",
    "value": "{{$.authHeader}}"
}
```

Both access patterns (`{{$.output.authHeader}}` and `{{$.authHeader}}`) work identically - the `$.output.` prefix makes it explicit that you're accessing template output values.

## Assertions

Assertions validate that responses meet expected criteria. They use JSONPath expressions to extract actual values.

### Assertion structure

```json
{
    "op": "operation-name",
    "actualValue": "{{JSONPath expression}}",
    "expectedValue": "expected value",
    "description": "Optional assertion description"
}
```

### Assertion operations

#### equals

Tests exact equality:

```json
{
    "op": "equals",
    "actualValue": "{{$.this.status}}",
    "expectedValue": 200
}
```

#### exists

Tests that a value exists and is not null:

```json
{
    "op": "exists",
    "actualValue": "{{$.this.body.id}}"
}
```

#### contains

Tests that a string contains a substring:

```json
{
    "op": "contains",
    "actualValue": "{{$.this.body.message}}",
    "expectedValue": "success"
}
```

#### greater-than / less-than

Numeric comparisons:

```json
{
    "op": "greater-than",
    "actualValue": "{{$.this.body.count}}",
    "expectedValue": 0
}
```

#### regex

Pattern matching:

```json
{
    "op": "regex",
    "actualValue": "{{$.this.body.email}}",
    "expectedValue": "^[\\w\\.-]+@[\\w\\.-]+\\.[a-zA-Z]{2,}$"
}
```

### Common assertion patterns

#### HTTP status validation

```json
{
    "op": "equals",
    "actualValue": "{{$.this.status}}",
    "expectedValue": 200,
    "description": "Should return HTTP 200 OK"
}
```

#### Response structure validation

```json
{
    "op": "exists",
    "actualValue": "{{$.this.body.data.id}}",
    "description": "Response should contain data.id field"
}
```

#### Response time validation

```json
{
    "op": "less-than",
    "actualValue": "{{$.this.duration}}",
    "expectedValue": 5000,
    "description": "Response should be under 5 seconds"
}
```

## CLI usage

The JTest CLI provides commands for running, debugging, and managing tests.

### Basic commands

#### Run tests

```bash
# Run a test file
jtest run tests.json

# Shorthand syntax
jtest tests.json
```

#### Debug tests

```bash
# Run with verbose debug output
jtest debug tests.json
```

#### Validate tests

```bash
# Validate test file syntax
jtest validate tests.json
```

#### Export tests

```bash
# Export to Postman collection
jtest export postman tests.json

# Export to Karate feature file
jtest export karate tests.json output.feature
```

### Runtime options

#### Environment variables

```bash
# Set environment variables
jtest run tests.json --env baseUrl=https://api.prod.com
jtest run tests.json --env username=admin --env password=secret

# Load environment from file
jtest run tests.json --env-file production.json
```

#### Global variables

```bash
# Set global variables
jtest run tests.json --globals userId=123
jtest run tests.json --globals-file globals.json
```

### Environment and globals files

Environment and globals files are JSON objects:

```json
{
    "baseUrl": "https://api.production.com",
    "timeout": 30000,
    "retries": 3
}
```

## Debugging and troubleshooting

### Debug mode

Debug mode provides detailed execution information:

```bash
jtest debug tests.json
```

Debug output is a markdown that includes:

- Step-by-step execution details
- Context changes between steps
- Available JSONPath expressions
- Full runtime context
- Timing information

### Common troubleshooting scenarios

#### Response body inspection

**Problem**: Need to see what the API returned

**Solution**: Use debug mode to inspect `$.this.body`

```bash
jtest debug tests.json
```

#### Call success verification

**Problem**: API call might be failing

**Solution**: Check the status code assertion and debug output

```json
{
    "op": "equals",
    "actualValue": "{{$.this.status}}",
    "expectedValue": 200,
    "description": "Verify API call succeeded"
}
```

#### Assertion placement

**Problem**: Don't know where to put assertions

**Solution**: Use JSONPath expressions based on response structure

```json
// For status code
"{{$.this.status}}"

// For response headers
"{{$.this.headers[?(@.name=='Content-Type')].value}}"

// For response body fields
"{{$.this.body.fieldName}}"

// For nested objects
"{{$.this.body.data.user.id}}"

// For arrays
"{{$.this.body.items[0].name}}"
```

#### Variable reference errors

**Problem**: Variables not resolving correctly

**Solution**: Check variable scope and JSONPath syntax

```json
// Environment variables
"{{$.env.variableName}}"

// Global variables  
"{{$.globals.variableName}}"

// Previous step results
"{{$.stepId.body.fieldName}}"

// Current step response
"{{$.this.body.fieldName}}"
```

### Error messages

Common error patterns and solutions:

- **"Variable not found"**: Check JSONPath syntax and variable scope
- **"Assertion failed"**: Verify expected vs actual values in debug output
- **"Template not found"**: Ensure template file is in `using` array
- **"Invalid JSON"**: Validate JSON syntax in test file

## CI/CD integration

JTest is designed to run in continuous integration pipelines.

### Basic CI integration

```yaml
# Example GitHub Actions workflow
name: API Tests
on: [push, pull_request]
jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v2
      - name: Setup .NET
        uses: actions/setup-dotnet@v1
        with:
          dotnet-version: 6.0.x
      - name: Build JTest
        run: dotnet build
      - name: Run API Tests
        run: |
          jtest tests.json --env baseUrl=${{ secrets.API_BASE_URL }}
```

### Environment-specific testing

```bash
# Development environment
jtest tests.json --env-file environments/dev.json

# Staging environment
jtest tests.json --env-file environments/staging.json

# Production environment
jtest tests.json --env-file environments/prod.json
```

### Exit codes

JTest returns standard exit codes for CI integration:

- **0**: All tests passed
- **1**: Test failures or execution errors

### Test reporting

Use debug mode for detailed test reports:

```bash
jtest debug tests.json > test-report.md
```

## Extensibility

JTest is designed from the ground up to be easily extensible. The architecture facilitates adding new step types, assertion operations, and functionality without modifying the core engine code.

### Extensibility Philosophy

The extensibility model follows these key principles:

- **Plugin Architecture**: New step types are implemented as plugins that register with the core engine
- **Interface-Driven**: All extensible components implement well-defined interfaces
- **Isolation**: Extensions operate in isolation and cannot affect the core system stability
- **Convention over Configuration**: Extensions follow simple naming and structural conventions
- **JSON-First**: All extensions are defined and configured through JSON, maintaining consistency

### Step Type Extension Architecture

The step execution system is built around a pluggable architecture that makes adding new step types straightforward.

#### Core Components

1. **Step Registry**: Central registry that manages all available step types
2. **Step Interface**: Common contract that all step implementations must follow
3. **Step Base Class**: Provides common functionality and reduces boilerplate code
4. **Execution Engine**: Orchestrates step execution without knowing specific step implementations
5. **Context System**: Unified context management that all steps can use

#### How Step Extension Works

When JTest encounters a step in a test definition:

1. **Type Resolution**: The execution engine looks up the step type in the registry
2. **Factory Creation**: A factory function creates an instance of the step implementation
3. **Validation**: The step validates its configuration from the JSON definition
4. **Execution**: The step executes within the provided context
5. **Result Handling**: Common result processing (assertions, saves, context updates)

This design ensures that:
- New step types integrate seamlessly with existing functionality
- All steps benefit from common features (assertions, variable saving, context access)
- The core engine remains unchanged when adding new step types
- Step implementations are isolated and cannot interfere with each other

### Implementing Custom Step Types

#### Step Implementation Requirements

Every custom step type must:

1. **Implement the Step Interface**: Define the step's execution behavior
2. **Provide Type Identification**: Specify the JSON `type` value that identifies the step
3. **Handle JSON Configuration**: Parse and validate step-specific properties from JSON
4. **Integrate with Context**: Use the execution context for variable resolution and data storage
5. **Return Structured Results**: Provide results in a format the engine can process

#### Step Lifecycle

Custom steps participate in a well-defined lifecycle:

1. **Registration**: Step type is registered with the engine at startup
2. **Validation**: Step validates its JSON configuration before execution
3. **Pre-execution**: Common properties (id, enabled, description) are processed
4. **Execution**: Step-specific logic runs with access to execution context
5. **Post-execution**: Results are processed for assertions and variable saving
6. **Context Update**: Step results are saved to context if an ID is provided

#### Built-in Functionality for Custom Steps

All custom steps automatically receive:

- **Variable Resolution**: Access to environment, global, and context variables through JSONPath
- **Assertion Processing**: Automatic handling of `assert` arrays in step definitions
- **Variable Saving**: Automatic processing of `save` objects for context updates
- **Error Handling**: Consistent error handling and reporting
- **Performance Metrics**: Automatic timing and performance measurement
- **Context Integration**: Full access to the execution context and variable scopes

### Example Custom Step Types

#### Database Step
```json
{
    "type": "database",
    "id": "user-lookup",
    "connectionString": "{{$.env.dbConnection}}",
    "query": "SELECT * FROM users WHERE email = @email",
    "parameters": {
        "email": "{{$.previous-step.body.email}}"
    },
    "assert": [
        {
            "op": "greater-than",
            "actualValue": "{{$.this.rowCount}}",
            "expectedValue": 0
        }
    ],
    "save": {
        "$.ctx.userId": "{{$.this.results[0].id}}"
    }
}
```

#### File System Step
```json
{
    "type": "file",
    "id": "read-config",
    "operation": "read",
    "path": "{{$.env.configPath}}/settings.json",
    "encoding": "utf8",
    "assert": [
        {
            "op": "exists",
            "actualValue": "{{$.this.content}}"
        }
    ],
    "save": {
        "$.globals.appSettings": "{{$.this.content}}"
    }
}
```

#### Message Queue Step
```json
{
    "type": "queue",
    "id": "send-message",
    "operation": "publish",
    "queueName": "{{$.env.queueName}}",
    "message": {
        "userId": "{{$.globals.userId}}",
        "action": "user_created",
        "timestamp": "{{$.now.iso}}"
    },
    "timeout": 5000,
    "assert": [
        {
            "op": "equals",
            "actualValue": "{{$.this.success}}",
            "expectedValue": true
        }
    ]
}
```

#### Wait/Delay Step
```json
{
    "type": "wait",
    "duration": 2000,
    "reason": "Allow time for async processing"
}
```

#### Script Execution Step
```json
{
    "type": "script",
    "id": "data-transform",
    "language": "javascript",
    "script": "return { transformed: context.previousStep.body.data.map(x => x.id) }",
    "save": {
        "$.ctx.transformedIds": "{{$.this.result.transformed}}"
    }
}
```

### Custom Assertion Operations

The assertion system is also extensible, allowing for domain-specific validation logic:

#### Custom Assertion Examples
```json
{
    "op": "json-schema",
    "actualValue": "{{$.this.body}}",
    "expectedValue": {
        "type": "object",
        "required": ["id", "name"]
    }
}
```

```json
{
    "op": "date-range",
    "actualValue": "{{$.this.body.createdAt}}",
    "expectedValue": {
        "after": "2024-01-01",
        "before": "{{$.now.iso}}"
    }
}
```

### Extension Registration

#### Programmatic Registration
Extensions are typically registered at application startup:

```csharp
// Register custom step types
engine.RegisterStep<DatabaseStep>("database");
engine.RegisterStep<FileStep>("file");
engine.RegisterStep<QueueStep>("queue");
engine.RegisterStep<WaitStep>("wait");
engine.RegisterStep<ScriptStep>("script");

// Register custom assertion operations
engine.RegisterAssertion<JsonSchemaAssertion>("json-schema");
engine.RegisterAssertion<DateRangeAssertion>("date-range");
```

#### Plugin Discovery
JTest can also discover extensions automatically:

- **Assembly Scanning**: Scan assemblies for types implementing step interfaces
- **Configuration-Based**: Load extensions specified in configuration files
- **Convention-Based**: Discover extensions following naming conventions

### Extension Development Guidelines

#### Best Practices

1. **Single Responsibility**: Each step type should have a clear, focused purpose
2. **Consistent Naming**: Use descriptive, consistent names for step types and properties
3. **Error Handling**: Provide clear, actionable error messages
4. **Documentation**: Include comprehensive documentation and examples
5. **Testing**: Thoroughly test step implementations in isolation and integration
6. **Performance**: Consider performance implications, especially for frequently used steps

#### JSON Schema Integration

Custom steps should provide JSON schema definitions for:
- **Validation**: Ensure step configurations are valid before execution
- **IntelliSense**: Enable IDE support for step configuration
- **Documentation**: Provide self-documenting step definitions

#### Context Usage Guidelines

When working with the execution context:
- **Read-Only Access**: Prefer reading from context over modifying it directly
- **Scoped Variables**: Use appropriate scope (globals, case, ctx) for saved variables
- **Variable Naming**: Use descriptive variable names that won't conflict
- **Cleanup**: Clean up temporary variables when appropriate

### Template Extensions

The template system also supports extensibility through:

#### Custom Template Functions
```json
{
    "name": "advanced-auth",
    "params": {
        "authType": { "type": "string", "enum": ["oauth2", "jwt", "api-key"] }
    },
    "steps": [
        {
            "type": "conditional",
            "condition": "{{authType}} === 'oauth2'",
            "then": [
                { "type": "oauth2", "..." }
            ],
            "else": [
                { "type": "api-key", "..." }
            ]
        }
    ]
}
```

#### Template Composition
Templates can compose other templates, enabling hierarchical reuse:

```json
{
    "name": "full-api-test",
    "steps": [
        { "type": "use", "template": "authenticate" },
        { "type": "use", "template": "setup-test-data" },
        { "type": "use", "template": "execute-business-logic" },
        { "type": "use", "template": "verify-results" },
        { "type": "use", "template": "cleanup-test-data" }
    ]
}
```

### Future Extensibility Considerations

The architecture is designed to support future enhancements:

- **Event System**: Pre/post execution hooks for cross-cutting concerns
- **Middleware Pipeline**: Request/response transformation pipeline
- **Custom Exporters**: Additional export formats beyond Postman and Karate
- **Remote Step Execution**: Steps that execute on remote systems
- **Parallel Execution**: Steps that can execute concurrently
- **Conditional Logic**: Built-in conditional execution and loops
- **Data Providers**: External data sources for test parameterization

This extensible foundation ensures that JTest can evolve to meet new testing requirements while maintaining backward compatibility and ease of use.

## Advanced topics

### Context manipulation

Understanding how context flows between steps using the explicit save syntax for targeting specific scopes:

```json
{
    "type": "http",
    "id": "create-user",
    "method": "POST",
    "url": "{{$.env.baseUrl}}/users",
    "body": {
        "name": "Test User",
        "email": "test@example.com"
    },
    "save": {
        "$.globals.currentUserId": "{{$.this.body.id}}",        // Save to global scope
        "$.case.userEmail": "{{$.this.body.email}}",            // Save to test case scope
        "$.ctx.responseTime": "{{$.this.duration}}"             // Save to step context scope
    }
}
```

#### Save target scopes

The `save` property uses explicit JSONPath targeting to specify where variables are stored:

- **$.globals.variableName**: Saves to global scope, accessible across all test cases
- **$.case.variableName**: Saves to test case scope, accessible within the current test
- **$.ctx.variableName**: Saves to step context scope, accessible to subsequent steps

#### Protected system variables

Certain variable scopes are read-only and cannot be modified through save operations:

- **$.this.***: Current step response data (status, headers, body, duration)
- **$.env.***: Environment variables (read-only after initialization)
- **$.now.***: System timestamp information
- **$.random.***: System-generated random values
- **$.stepId.***: Previous step results (where stepId is a step's ID)

Attempting to save to protected scopes will result in a validation error.

#### Save syntax examples

```json
// ? Valid save operations
"save": {
    "$.globals.authToken": "{{$.this.body.access_token}}",
    "$.case.userId": "{{$.this.body.user.id}}",
    "$.ctx.processingTime": "{{$.this.duration}}"
}

// ? Invalid save operations (will cause errors)
"save": {
    "$.this.status": "200",                    // Cannot modify current response
    "$.env.baseUrl": "{{$.this.body.newUrl}}", // Cannot modify environment
    "$.login.body": "{{$.this.body}}"          // Cannot modify step results
}
```

#### Cross-test data sharing example

Here's a complete example showing how to share data across test cases using globals:

```json
{
    "version": "1.0",
    "globals": {
        "authToken": null,
        "createdUserId": null
    },
    "tests": [
        {
            "name": "Setup authentication",
            "steps": [
                {
                    "type": "use",
                    "template": "authenticate",
                    "with": {
                        "username": "{{$.env.username}}",
                        "password": "{{$.env.password}}"
                    },
                    "save": {
                        "$.globals.authToken": "{{$.output.token}}"
                    }
                }
            ]
        },
        {
            "name": "Create and verify user",
            "steps": [
                {
                    "type": "http",
                    "method": "POST",
                    "url": "{{$.env.baseUrl}}/users",
                    "headers": [
                        {
                            "name": "Authorization",
                            "value": "Bearer {{$.globals.authToken}}"
                        }
                    ],
                    "body": {
                        "name": "New User"
                    },
                    "save": {
                        "$.globals.createdUserId": "{{$.this.body.id}}",
                        "$.case.userName": "{{$.this.body.name}}"
                    }
                },
                {
                    "type": "http",
                    "method": "GET",
                    "url": "{{$.env.baseUrl}}/users/{{$.globals.createdUserId}}",
                    "headers": [
                        {
                            "name": "Authorization",
                            "value": "Bearer {{$.globals.authToken}}"
                        }
                    ],
                    "assert": [
                        {
                            "op": "equals",
                            "actualValue": "{{$.this.body.name}}",
                            "expectedValue": "{{$.case.userName}}"
                        }
                    ]
                }
            ]
        }
    ]
}