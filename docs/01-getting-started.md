# Getting Started with JTest

JTest is a JSON-based API testing framework that lets you write tests using simple JSON files. This guide will help you create your first test in minutes.

## Installation

JTest is a .NET tool that can be installed globally:

```bash
dotnet tool install -g JTest.Cli
```

## Your First Test

Let's start with the simplest possible test - making an HTTP request and checking the response.

### 1. Create a Test File

Create a file called `my-first-test.json`:

```json
{
    "version": "1.0",
    "info": {
        "name": "My First API Test",
        "description": "A simple test to get started with JTest"
    },
    "tests": [
        {
            "name": "Test JSONPlaceholder API",
            "description": "Make a GET request to a public API",
            "steps": [
                {
                    "type": "http",
                    "method": "GET",
                    "url": "https://jsonplaceholder.typicode.com/posts/1",
                    "assert": [
                        {
                            "op": "exists",
                            "actualValue": "{{$.this.body.title}}"
                        },
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

### 2. Run Your Test

```bash
jtest run my-first-test.json
```

You should see output showing your test passed!

## What Just Happened?

Let's break down what this test does:

1. **Test Structure**: Every JTest file has a `version`, optional `info`, and a `tests` array
2. **HTTP Step**: The `http` step makes a GET request to the JSONPlaceholder API
3. **Assertions**: We check that:
   - The response has a `title` field (exists assertion)
   - The status code is 200 (equals assertion)

## Key Concepts

### Variables and Context

JTest automatically stores the HTTP response in a variable called `this`. You can access:
- `{{$.this.statusCode}}` - HTTP status code
- `{{$.this.body}}` - Response body (parsed as JSON)
- `{{$.this.headers}}` - Response headers

### JSONPath Expressions

The `{{$.this.body.title}}` syntax is a JSONPath expression that extracts the `title` field from the response body.

## Next Steps

Now that you've run your first test, learn about:
- [Test Structure](02-test-structure.md) - Understanding the JSON format
- [HTTP Steps](steps/http-step.md) - Making different types of HTTP requests
- [Assertions](assertions.md) - Validating responses in detail

## Common First Test Patterns

### Testing Your Own API

```json
{
    "version": "1.0",
    "info": {
        "name": "My API Test"
    },
    "env": {
        "baseUrl": "http://localhost:8080"
    },
    "tests": [
        {
            "name": "Health Check",
            "steps": [
                {
                    "type": "http",
                    "method": "GET",
                    "url": "{{$.env.baseUrl}}/health",
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

### Testing with Authentication

```json
{
    "version": "1.0",
    "info": {
        "name": "Authenticated API Test"
    },
    "tests": [
        {
            "name": "Get User Profile",
            "steps": [
                {
                    "type": "http",
                    "method": "GET",
                    "url": "https://api.example.com/profile",
                    "headers": {
                        "Authorization": "Bearer your-token-here"
                    },
                    "assert": [
                        {
                            "op": "exists",
                            "actualValue": "{{$.this.body.user.id}}"
                        }
                    ]
                }
            ]
        }
    ]
}
```

## Tips for Success

1. **Start Simple**: Begin with basic GET requests before adding complexity
2. **Use Public APIs**: JSONPlaceholder, httpbin.org, and similar services are great for learning
3. **Check Status Codes**: Always verify the HTTP status code in your assertions
4. **Use Environment Variables**: Store URLs and credentials in the `env` section
5. **Run Often**: Execute tests frequently as you build them to catch issues early

Ready to learn more? Continue with [Test Structure](02-test-structure.md) to understand the JSON format in detail.