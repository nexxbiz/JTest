# JTest Documentation

Welcome to the comprehensive JTest documentation. This guide will help you learn JTest from basic concepts to advanced usage patterns.

## Getting Started

Start here if you're new to JTest:

- **[Getting Started](01-getting-started.md)** - Your first test in minutes
- **[Test Structure](02-test-structure.md)** - Understanding the JSON format
- **[Context and Variables](03-context-and-variables.md)** - Managing data and state

## Core Concepts

Essential knowledge for effective JTest usage:

### Step Types
- **[HTTP Step](steps/http-step.md)** - Making HTTP requests (most common)
- **[Assert Step](steps/assert-step.md)** - Standalone assertions and validations
- **[Use Step](steps/use-step.md)** - Executing reusable templates

### Advanced Features
- **[Templates](04-templates.md)** - Creating reusable test components
- **[Assertions](05-assertions.md)** - Comprehensive validation reference
- **[Best Practices](06-best-practices.md)** - Proven patterns and guidelines

## Tools and Integration

Practical guides for development and deployment:

- **[CLI Usage](cli-usage.md)** - Command-line interface and options
- **[Troubleshooting](troubleshooting.md)** - Debugging and problem-solving
- **[CI/CD Integration](ci-cd-integration.md)** - Continuous integration setup
- **[Extensibility](extensibility.md)** - Creating custom step types

## Quick Reference

- **[File Structure Reference](file-structure-reference.md)** - Comprehensive structure documentation

### Common Patterns

### Common Patterns

#### Simple Variable Test
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

#### Sequential Step Execution
```json
{
    "version": "1.0",
    "globals": {
        "counter": 0
    },
    "tests": [
        {
            "name": "Sequential Steps Example",
            "steps": [
                {
                    "type": "wait",
                    "id": "step1",
                    "ms": 50,
                    "save": {
                        "$.globals.step1Time": "{{$.this.ms}}"
                    }
                },
                {
                    "type": "wait",
                    "id": "step2", 
                    "ms": 100,
                    "assert": [
                        {
                            "op": "greaterthan",
                            "actualValue": "{{$.this.ms}}",
                            "expectedValue": "{{$.globals.step1Time}}"
                        }
                    ]
                },
                {
                    "type": "assert",
                    "assert": [
                        {
                            "op": "equals",
                            "actualValue": "{{$.step1.ms}}",
                            "expectedValue": 50
                        },
                        {
                            "op": "equals",
                            "actualValue": "{{$.step2.ms}}",
                            "expectedValue": 100
                        }
                    ]
                }
            ]
        }
    ]
}
```

#### Using Templates
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

### Assertion Examples

#### Basic Validations
```json
{
    "assert": [
        {
            "op": "equals",
            "actualValue": "{{$.this.statusCode}}",
            "expectedValue": 200
        },
        {
            "op": "exists",
            "actualValue": "{{$.this.body.user.id}}"
        },
        {
            "op": "contains",
            "actualValue": "{{$.this.body.message}}",
            "expectedValue": "success"
        }
    ]
}
```

#### Advanced Validations
```json
{
    "assert": [
        {
            "op": "matches",
            "actualValue": "{{$.this.body.email}}",
            "expectedValue": "^[^@]+@[^@]+\\.[^@]+$"
        },
        {
            "op": "in",
            "actualValue": "{{$.this.statusCode}}",
            "expectedValue": [200, 201, 202]
        },
        {
            "op": "greaterthan",
            "actualValue": "{{$.this.body.items.length}}",
            "expectedValue": 0
        }
    ]
}
```

## Learning Path

### Beginner (1-2 hours)
1. Read [Getting Started](01-getting-started.md)
2. Understand [Test Structure](02-test-structure.md)
3. Try [HTTP Step](steps/http-step.md) examples
4. Practice with public APIs (JSONPlaceholder, httpbin.org)

### Intermediate (3-5 hours)
1. Learn [Context and Variables](03-context-and-variables.md)
2. Master [Assertions](05-assertions.md)
3. Explore [Templates](04-templates.md)
4. Review [Best Practices](06-best-practices.md)

### Advanced (5+ hours)
1. Study [Extensibility](extensibility.md)
2. Implement [CI/CD Integration](ci-cd-integration.md)
3. Master [CLI Usage](cli-usage.md)
4. Create custom step types
5. Build comprehensive test suites

## Common Use Cases

### API Testing
- REST API validation
- GraphQL endpoint testing
- Authentication flows
- Error handling verification
- Performance benchmarking

### Integration Testing
- Microservice communication
- Database integration
- Third-party service integration
- End-to-end workflows
- Cross-platform compatibility

### CI/CD Testing
- Build pipeline validation
- Deployment verification
- Environment-specific testing
- Regression testing
- Performance monitoring

## Getting Help

### Documentation
- Browse this documentation for comprehensive guides
- Check [Troubleshooting](troubleshooting.md) for common issues
- Review [Best Practices](06-best-practices.md) for proven patterns

### Community
- **GitHub Issues** - Report bugs and request features
- **Discussions** - Ask questions and share experiences
- **Examples** - Browse real-world test examples

### Support
- Review existing issues before creating new ones
- Provide minimal reproduction cases when reporting bugs
- Include environment details and error logs

## Contributing

Help improve JTest documentation:

1. **Fix Issues** - Correct errors or unclear explanations
2. **Add Examples** - Contribute real-world usage examples
3. **Improve Clarity** - Enhance explanations and structure
4. **Extend Coverage** - Document missing features or edge cases

## What's Next?

Ready to start? Choose your path:

- **New to JTest?** Start with [Getting Started](01-getting-started.md)
- **Ready to build tests?** Jump to [HTTP Step](steps/http-step.md)
- **Want advanced features?** Explore [Templates](04-templates.md)
- **Setting up CI/CD?** Check [CI/CD Integration](ci-cd-integration.md)
- **Need to debug?** Visit [Troubleshooting](troubleshooting.md)

---

*Last updated: $(date +%Y-%m-%d)*