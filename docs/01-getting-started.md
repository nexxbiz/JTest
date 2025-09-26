# Getting Started with JTest

JTest is a JSON-based testing framework that lets you write tests using simple JSON files. This guide will help you create your first test in minutes.

## Getting JTest

Since JTest is currently a development project, you need to build it from source:

```bash
# Clone the repository
git clone https://github.com/nexxbiz/JTest.git
cd JTest

# Build the CLI
dotnet build src/JTest.Cli

# The executable will be at: ./src/JTest.Cli/bin/Debug/net8.0/JTest
```

## Your First Test

Let's start with a simple test that demonstrates the core concepts without requiring external APIs.

### 1. Create a Basic Test File

Create a file called `my-first-test.json`:

```json
{
    "version": "1.0",
    "info": {
        "name": "My First JTest",
        "description": "A simple test to get started with JTest"
    },
    "env": {
        "testMessage": "Hello JTest!",
        "waitTime": 100
    },
    "tests": [
        {
            "name": "Basic Variable and Wait Test",
            "description": "Test variables and wait functionality",
            "steps": [
                {
                    "type": "wait",
                    "id": "waitStep",
                    "ms": "{{$.env.waitTime}}",
                    "assert": [
                        {
                            "op": "equals",
                            "actualValue": "{{$.this.ms}}",
                            "expectedValue": 100
                        }
                    ]
                },
                {
                    "type": "assert",
                    "id": "checkVariables",
                    "assert": [
                        {
                            "op": "equals",
                            "actualValue": "{{$.env.testMessage}}",
                            "expectedValue": "Hello JTest!"
                        }
                    ]
                }
            ]
        }
    ]
}
```

### 2. Run Your Test

```bash
./src/JTest.Cli/bin/Debug/net8.0/JTest run my-first-test.json
```

You should see output showing your test passed!

## What Just Happened?

This test simply made a GET request to https://jsonplaceholder.typicode.com/posts/1. That's it. JTest:

1. **Made the HTTP request** using the `http` step type
2. **Checked that it succeeded** (didn't return an error)
3. **Reported the result** back to you

The key insight: **keep tests simple**. Start with basic requests before adding validation.

## Adding Simple Validation

Now let's add one assertion to check the status code:

```json
{
    "version": "1.0",
    "info": {
        "name": "My First API Test",
        "description": "A simple test with basic validation"
    },
    "tests": [
        {
            "name": "Test JSONPlaceholder API",
            "description": "Make a GET request and check status",
            "steps": [
                {
                    "type": "http",
                    "method": "GET",
                    "url": "https://jsonplaceholder.typicode.com/posts/1",
                    "assert": [
                        {
                            "op": "equals",
                            "actualValue": "{{$.this.statusCode}}",
                            "expectedValue": 200
                        }
                    ]
                }
            ]
        }
    ]
}
```

### What's New Here?

- **`assert` array**: Contains validation rules
- **`{{$.this.statusCode}}`**: Accesses the HTTP status code from the response
- **`op: "equals"`**: Checks for exact equality

The `$.this` refers to the result of the current step. JTest automatically puts the HTTP response there.

## Key Concepts

### Variables and Context

When a step runs, JTest stores its result in a variable called `this`. For HTTP steps, you can access:
- `{{$.this.statusCode}}` - HTTP status code (e.g., 200, 404, 500)
- `{{$.this.body}}` - Response body (parsed as JSON if applicable)
- `{{$.this.headers}}` - Response headers

### Variable Expressions

The `{{$.variable}}` syntax lets you access stored values:
- `{{$.this.body.title}}` - Get the `title` field from the response body
- `{{$.this.statusCode}}` - Get the HTTP status code
- `{{$.env.baseUrl}}` - Get environment variables (explained in [Test Structure](02-test-structure.md))

## Naming and Documentation

The `name` and `description` fields are not just metadata - they appear in test reports and markdown output:

```json
{
    "name": "User Registration Flow",
    "description": "Test complete user registration including validation"
}
```

When JTest generates markdown reports, these become:
- **Headings** in the report (from `name`)
- **Explanatory text** under each test (from `description`)

Good naming helps you understand what failed when tests break.

## Test Structure Rules

1. **Keep it simple**: Start with one request, add complexity gradually
2. **One concern per test**: Don't try to test everything in one test
3. **Clear names**: Use descriptive names that explain what you're testing
4. **Minimal assertions**: Only assert what matters for this specific test

## Next Steps

Now that you've mastered the basics, learn about:
- [Test Structure](02-test-structure.md) - JSON format and organization
- [Context and Variables](03-context-and-variables.md) - Managing data between steps
- [HTTP Steps](steps/http-step.md) - Making different types of requests

## Common Beginner Mistakes

### ❌ Testing Too Much at Once
```json
// Don't do this - too complex for a beginner
{
    "name": "Complete User Workflow",
    "steps": [
        { "type": "http", "method": "POST", "url": "/register" },
        { "type": "http", "method": "POST", "url": "/login" },
        { "type": "http", "method": "GET", "url": "/profile" },
        { "type": "http", "method": "PUT", "url": "/profile" }
    ]
}
```

### ✅ Start Simple
```json
// Do this - one thing at a time
{
    "name": "User Registration",
    "steps": [
        {
            "type": "http",
            "method": "POST",
            "url": "/register"
        }
    ]
}
```

### ❌ Too Many Assertions
```json
// Don't validate everything at once
"assert": [
    { "op": "equals", "actualValue": "{{$.this.statusCode}}", "expectedValue": 200 },
    { "op": "exists", "actualValue": "{{$.this.body.id}}" },
    { "op": "exists", "actualValue": "{{$.this.body.email}}" },
    { "op": "exists", "actualValue": "{{$.this.body.createdAt}}" },
    { "op": "equals", "actualValue": "{{$.this.body.status}}", "expectedValue": "active" }
]
```

### ✅ Assert What Matters
```json
// Start with the basics
"assert": [
    {
        "op": "equals",
        "actualValue": "{{$.this.statusCode}}",
        "expectedValue": 200
    }
]
```

Remember: **There's no need to create tests for your tests**. Keep them simple and focused.