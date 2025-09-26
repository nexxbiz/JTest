# JTest Documentation

Welcome to the comprehensive JTest documentation. This guide will help you learn JTest from basic concepts to advanced usage patterns.

## Documentation Index

### Core Guide (Read in order)
1. **[Getting Started](01-getting-started.md)** - Your first test in minutes
2. **[Test Structure](02-test-structure.md)** - Understanding the JSON format
3. **[Context and Variables](03-context-and-variables.md)** - Managing data and state
4. **[Templates](04-templates.md)** - Creating reusable test components
5. **[Assertions](05-assertions.md)** - Comprehensive validation reference
6. **[Best Practices](06-best-practices.md)** - Proven patterns and guidelines

### Tools and Reference
7. **[CLI Usage](07-cli-usage.md)** - Command-line interface and options
8. **[Troubleshooting](08-troubleshooting.md)** - Debugging and problem-solving
9. **[Extensibility](09-extensibility.md)** - Creating custom step types
10. **[CI/CD Integration](10-ci-cd-integration.md)** - Continuous integration setup
11. **[File Structure Reference](11-file-structure-reference.md)** - Comprehensive structure documentation

### Step Types
- **[HTTP Step](steps/http-step.md)** - Making HTTP requests (most common)
- **[Assert Step](steps/assert-step.md)** - Standalone assertions and validations
- **[Use Step](steps/use-step.md)** - Executing reusable templates

## Quick Examples

### Simple Variable Test
```json
{
    "version": "1.0",
    "env": {
        "testValue": "hello-world",
        "timeout": 1000
    },
    "tests": [
        {
            "name": "Basic Variable Test",
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
                }
            ]
        }
    ]
}
```

## Learning Path

### Beginner (1-2 hours)
1. Read [Getting Started](01-getting-started.md)
2. Understand [Test Structure](02-test-structure.md)
3. Try [HTTP Step](steps/http-step.md) examples
4. Practice basic assertions

### Intermediate (3-5 hours)
1. Learn [Context and Variables](03-context-and-variables.md)
2. Master [Assertions](05-assertions.md)
3. Explore [Templates](04-templates.md)
4. Review [Best Practices](06-best-practices.md)

### Advanced (5+ hours)
1. Master [CLI Usage](07-cli-usage.md)
2. Study [Extensibility](09-extensibility.md)
3. Implement [CI/CD Integration](10-ci-cd-integration.md)
4. Create custom step types

## Getting Help

### Documentation
- Browse this documentation for comprehensive guides
- Check [Troubleshooting](08-troubleshooting.md) for common issues
- Review [Best Practices](06-best-practices.md) for proven patterns

### Community
- **GitHub Issues** - Report bugs and request features
- **Discussions** - Ask questions and share experiences

## What's Next?

Ready to start? Choose your path:

- **New to JTest?** Start with [Getting Started](01-getting-started.md)
- **Ready to build tests?** Jump to [HTTP Step](steps/http-step.md)
- **Want advanced features?** Explore [Templates](04-templates.md)
- **Setting up CI/CD?** Check [CI/CD Integration](10-ci-cd-integration.md)
- **Need to debug?** Visit [Troubleshooting](08-troubleshooting.md)

