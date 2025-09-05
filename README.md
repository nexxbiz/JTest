# JTest

A powerful JSON-based API testing framework that lets you write comprehensive tests using simple JSON configuration files.

## Quick Start

Create your first test:

```json
{
    "version": "1.0",
    "info": {
        "name": "My First API Test"
    },
    "tests": [
        {
            "name": "Health Check",
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

Run it:
```bash
dotnet tool install -g JTest.Cli
jtest run my-test.json
```

## Documentation

📚 **[Complete Documentation](docs/README.md)** - Start here for comprehensive guides

### Quick Links

- **[Getting Started](docs/01-getting-started.md)** - Your first test in minutes
- **[Test Structure](docs/02-test-structure.md)** - Understanding the JSON format  
- **[HTTP Step](docs/steps/http-step.md)** - Making HTTP requests
- **[Assertions](docs/assertions.md)** - Validating responses
- **[Templates](docs/templates.md)** - Reusable test components
- **[CLI Usage](docs/cli-usage.md)** - Command-line options
- **[Best Practices](docs/best-practices.md)** - Proven patterns
- **[Troubleshooting](docs/troubleshooting.md)** - Debugging help

## Key Features

- **JSON-Based** - Write tests using familiar JSON syntax
- **Powerful Assertions** - Comprehensive validation with JSONPath expressions
- **Template System** - Create reusable test components
- **CLI Tool** - Command-line interface for running tests
- **Multiple Output Formats** - Console, JSON, JUnit XML, Markdown
- **CI/CD Ready** - Designed for continuous integration pipelines
- **Extensible** - Add custom step types and functionality

## Installation

Install as a global .NET tool:

```bash
dotnet tool install -g JTest.Cli
```

## Usage Examples

### Basic HTTP Test
```json
{
    "version": "1.0",
    "tests": [
        {
            "name": "Get User",
            "steps": [
                {
                    "type": "http",
                    "method": "GET", 
                    "url": "https://api.example.com/users/123",
                    "assert": [
                        {"op": "equals", "actualValue": "{{$.this.statusCode}}", "expectedValue": 200},
                        {"op": "exists", "actualValue": "{{$.this.body.user.id}}"}
                    ]
                }
            ]
        }
    ]
}
```

### Authentication Flow
```json
{
    "tests": [
        {
            "name": "Authenticated Request",
            "steps": [
                {
                    "type": "http",
                    "id": "login",
                    "method": "POST",
                    "url": "/auth/login",
                    "body": {"username": "user@example.com", "password": "password"}
                },
                {
                    "type": "http",
                    "method": "GET",
                    "url": "/user/profile",
                    "headers": {"Authorization": "Bearer {{$.login.body.token}}"}
                }
            ]
        }
    ]
}
```

### Using Templates
```json
{
    "using": ["./templates/auth.json"],
    "tests": [
        {
            "name": "Template Example",
            "steps": [
                {
                    "type": "use",
                    "template": "authenticate",
                    "params": {"username": "test@example.com", "password": "password"}
                }
            ]
        }
    ]
}
```

## CLI Commands

```bash
# Run tests
jtest run tests.json

# Run with specific environment
jtest run tests.json --environment staging

# Generate JUnit XML report
jtest run tests.json --output junit

# Run tests in parallel
jtest run tests.json --parallel 4

# Debug mode
jtest run tests.json --debug --verbose
```

## Project Structure

```
JTest/
├── JTest.Core/          # Core framework library
├── JTest.Cli/           # Command-line interface
├── JTest.UnitTests/     # Unit tests
└── docs/                # Documentation
    ├── README.md        # Documentation index
    ├── 01-getting-started.md
    ├── 02-test-structure.md
    ├── steps/           # Step type documentation
    ├── templates.md
    ├── assertions.md
    ├── best-practices.md
    ├── cli-usage.md
    ├── troubleshooting.md
    ├── ci-cd-integration.md
    └── extensibility.md
```

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests for new functionality
5. Submit a pull request

See [Contributing Guidelines](CONTRIBUTING.md) for more details.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
