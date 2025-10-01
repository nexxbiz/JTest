# JTest

A powerful JSON-based API testing framework that lets you write comprehensive tests using simple JSON configuration files.

## Quick Start

Create your first test file `my-test.json`:

```json
{
    "version": "1.0",
    "info": {
        "name": "My First Test"
    },
    "env": {
        "testValue": "hello-world"
    },
    "tests": [
        {
            "name": "Basic Variable Test",
            "steps": [
                {
                    "type": "wait",
                    "ms": 100,
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
                    "assert": [
                        {
                            "op": "equals",
                            "actualValue": "{{$.env.testValue}}",
                            "expectedValue": "hello-world"
                        }
                    ]
                }
            ]
        }
    ]
}
```

Build and run it:
```bash
# Clone and build the project
git clone https://github.com/nexxbiz/JTest.git
cd JTest
dotnet build src/JTest.Cli

# Run your test
./src/JTest.Cli/bin/Debug/net8.0/JTest run my-test.json
```

## Documentation

📚 **[Complete Documentation](docs/README.md)** - Start here for comprehensive guides

### Quick Links

- **[Getting Started](docs/01-getting-started.md)** - Your first test in minutes
- **[Test Structure](docs/02-test-structure.md)** - Understanding the JSON format  
- **[HTTP Step](docs/steps/http-step.md)** - Making HTTP requests
- **[Assertions](docs/05-assertions.md)** - Validating responses
- **[Templates](docs/04-templates.md)** - Reusable test components
- **[CLI Usage](docs/07-cli-usage.md)** - Command-line options
- **[Best Practices](docs/06-best-practices.md)** - Proven patterns
- **[Troubleshooting](docs/08-troubleshooting.md)** - Debugging help

## Key Features

- **JSON-Based** - Write tests using familiar JSON syntax
- **Powerful Assertions** - Comprehensive validation with JSONPath expressions
- **Template System** - Create reusable test components
- **Variable System** - Environment and global variables with JSONPath access
- **Multiple Step Types** - HTTP requests, assertions, wait steps, and templates
- **CLI Interface** - Command-line tool for running and debugging tests
- **Extensible** - Add custom step types and functionality

## Installation

### 🚀 Quick Setup (Recommended)

**One-command installation:**

```bash
# Linux/macOS
git clone https://github.com/nexxbiz/JTest.git && cd JTest && ./setup.sh

# Windows (PowerShell)  
git clone https://github.com/nexxbiz/JTest.git; cd JTest; .\setup.ps1
```

This installs the `jtest` CLI tool globally. After setup:

```bash
jtest --help                    # Show help
jtest create "My First Test"    # Create a new test
jtest run my-test.json         # Run tests
```

### 📦 Other Installation Methods

- **[Complete Installation Guide](INSTALLATION.md)** - All installation options
- **Docker:** `./docker.sh build && ./docker.sh run --help`
- **From Source:** `dotnet build && ./src/JTest.Cli/bin/Debug/net8.0/JTest --help`
- **CI/CD Integration:** See [Installation Guide](INSTALLATION.md#cicd-integration)

## Usage Examples

### Basic Variable and Assertion Test
```json
{
    "version": "1.0",
    "env": {
        "testValue": "hello-world",
        "timeout": 1000
    },
    "globals": {
        "expectedResult": "success"
    },
    "tests": [
        {
            "name": "Variable Test",
            "steps": [
                {
                    "type": "wait",
                    "ms": "{{$.env.timeout}}",
                    "assert": [
                        {
                            "op": "greaterthan",
                            "actualValue": "{{$.this.ms}}",
                            "expectedValue": 500
                        }
                    ]
                },
                {
                    "type": "assert",
                    "assert": [
                        {
                            "op": "equals",
                            "actualValue": "{{$.env.testValue}}",
                            "expectedValue": "hello-world"
                        }
                    ]
                }
            ]
        }
    ]
}
```

### Using Templates
Templates allow you to reuse common patterns. First, create `auth-template.json`:

```json
{
    "version": "1.0",
    "components": {
        "templates": [
            {
                "name": "authenticate",
                "description": "Generate test authentication token",
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

Then use it in your test:

```json
{
    "version": "1.0",
    "using": ["./auth-template.json"],
    "tests": [
        {
            "name": "Template Usage Example",
            "steps": [
                {
                    "type": "use",
                    "template": "authenticate",
                    "with": {
                        "username": "testuser",
                        "password": "secret123"
                    },
                    "save": {
                        "$.globals.authToken": "{{$.this.token}}"
                    }
                },
                {
                    "type": "assert",
                    "assert": [
                        {
                            "op": "equals",
                            "actualValue": "{{$.globals.authToken}}",
                            "expectedValue": "testuser-secret123-token"
                        }
                    ]
                }
            ]
        }
    ]
}
```

## CLI Commands

```bash
# Run tests (using globally installed tool)
jtest run tests.json

# Run multiple test files with wildcards
jtest run tests/*.json

# Run with environment variables
jtest run tests.json --env baseUrl=https://api.example.com

# Validate test files
jtest validate tests.json

# Debug mode with verbose output
jtest debug tests.json
```

## Project Structure

```
JTest/
├── src/                 # Source code
│   ├── JTest.Core/      # Core framework library
│   ├── JTest.Cli/       # Command-line interface
│   └── JTest.UnitTests/ # Unit tests
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
