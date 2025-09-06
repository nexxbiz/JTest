# JTest Structure

Understanding the JSON structure is key to writing effective JTest files. This guide explains each part of a test file and how they work together.

## Basic File Structure

Every JTest file follows this pattern:

```json
{
    "version": "1.0",
    "info": {
        "name": "Test Suite Name",
        "description": "Optional description"
    },
    "using": [
        "./path/to/templates.json"
    ],
    "env": {
        "baseUrl": "https://api.example.com",
        "apiKey": "your-api-key"
    },
    "globals": {
        "timeout": 30000,
        "defaultHeaders": {
            "Content-Type": "application/json"
        }
    },
    "tests": [
        // Your test cases go here
    ]
}
```

## Root Properties

### `version` (Required)
Specifies the JTest schema version. Currently always `"1.0"`.

```json
{
    "version": "1.0"
}
```

### `info` (Optional)
Metadata about your test suite:

```json
{
    "info": {
        "name": "User API Tests",
        "description": "Tests for user management endpoints",
        "author": "QA Team",
        "version": "1.2.0"
    }
}
```

### `using` (Optional)
Import template files to reuse common test patterns:

```json
{
    "using": [
        "./templates/auth-templates.json",
        "./templates/user-templates.json"
    ]
}
```

### `env` (Optional)
Environment variables available throughout your tests as `{{$.env.variableName}}`:

```json
{
    "env": {
        "baseUrl": "https://api.staging.com",
        "apiKey": "staging-key-123",
        "timeout": 5000
    }
}
```

### `globals` (Optional)
Global variables shared across all tests as `{{$.globals.variableName}}`:

```json
{
    "globals": {
        "defaultUser": {
            "username": "testuser",
            "email": "test@example.com"
        },
        "commonHeaders": {
            "Accept": "application/json",
            "User-Agent": "JTest/1.0"
        }
    }
}
```

### `tests` (Required)
Array of test cases to execute:

```json
{
    "tests": [
        {
            "name": "Test Case 1",
            "steps": [...]
        },
        {
            "name": "Test Case 2", 
            "steps": [...]
        }
    ]
}
```

## Test Case Structure

Each test case contains:

```json
{
    "name": "Create and Verify User",
    "description": "Creates a new user and verifies the response",
    "enabled": true,
    "steps": [
        // Test steps go here
    ]
}
```

### Test Case Properties

- **`name`** (Required): Human-readable test name
- **`description`** (Optional): Detailed explanation of what the test does
- **`enabled`** (Optional): Whether to run this test (default: `true`)
- **`steps`** (Required): Array of steps to execute

## Step Structure

All steps share these common properties:

```json
{
    "type": "http",
    "id": "create-user",
    "description": "Create a new user account",
    "enabled": true,
    
    // Step-specific properties...
    "method": "POST",
    "url": "{{$.env.baseUrl}}/users",
    
    // Common optional properties
    "assert": [...],
    "save": {...}
}
```

### Common Step Properties

- **`type`** (Required): Step type (`http`, `use`, `assert`, etc.)
- **`id`** (Optional): Unique identifier for referencing step results
- **`description`** (Optional): Human-readable description
- **`enabled`** (Optional): Whether to execute this step (default: `true`)
- **`assert`** (Optional): Array of assertions to validate
- **`save`** (Optional): Variables to save from step results

## Variable Scopes

JTest has different variable scopes that determine where data is available:

### Environment Variables (`$.env`)
Set in the `env` section, available everywhere:

```json
{
    "env": {
        "apiUrl": "https://api.example.com"
    }
}
```
Access with: `{{$.env.apiUrl}}`

### Global Variables (`$.globals`) 
Set in the `globals` section, available everywhere:

```json
{
    "globals": {
        "userId": "12345"
    }
}
```
Access with: `{{$.globals.userId}}`

### Step Results (`$.this` or `$.stepId`)
Data from the current or previous steps:

```json
{
    "type": "http",
    "id": "login",
    "method": "POST",
    "url": "/auth/login"
}
```
Access current step: `{{$.this.body.token}}`
Access by ID: `{{$.login.body.token}}`

## Real-World Example

Here's a complete test file that demonstrates all concepts:

```json
{
    "version": "1.0",
    "info": {
        "name": "E-commerce API Tests",
        "description": "Tests for the shopping cart and order management"
    },
    "env": {
        "baseUrl": "https://api.shop.com",
        "adminApiKey": "admin-key-123"
    },
    "globals": {
        "testProduct": {
            "name": "Test Widget",
            "price": 29.99,
            "sku": "TEST-001"
        }
    },
    "tests": [
        {
            "name": "Create Product and Add to Cart",
            "description": "Full flow test: create product, add to cart, verify total",
            "steps": [
                {
                    "type": "http",
                    "id": "create-product",
                    "description": "Create a test product",
                    "method": "POST",
                    "url": "{{$.env.baseUrl}}/products",
                    "headers": {
                        "Authorization": "Bearer {{$.env.adminApiKey}}"
                    },
                    "body": "{{$.globals.testProduct}}",
                    "assert": [
                        {
                            "op": "equals",
                            "actualValue": "{{$.this.statusCode}}",
                            "expectedValue": 201
                        }
                    ],
                    "save": {
                        "$.globals.productId": "{{$.this.body.id}}"
                    }
                },
                {
                    "type": "http",
                    "description": "Add product to cart",
                    "method": "POST",
                    "url": "{{$.env.baseUrl}}/cart/items",
                    "body": {
                        "productId": "{{$.globals.productId}}",
                        "quantity": 2
                    },
                    "assert": [
                        {
                            "op": "equals",
                            "actualValue": "{{$.this.body.total}}",
                            "expectedValue": 59.98
                        }
                    ]
                }
            ]
        }
    ]
}
```

## Best Practices

### Structure Organization
1. **Group related tests** in the same file
2. **Use descriptive names** for tests and steps
3. **Add descriptions** for complex test logic
4. **Keep files focused** - one feature area per file

### Variable Management
1. **Use env for configuration** that changes between environments
2. **Use globals for test data** that's shared across tests
3. **Use step IDs** when you need to reference specific step results
4. **Choose meaningful variable names**

### File Organization
```
tests/
├── auth/
│   ├── login-tests.json
│   └── registration-tests.json
├── users/
│   ├── user-crud-tests.json
│   └── user-permissions-tests.json
└── templates/
    ├── auth-templates.json
    └── common-templates.json
```

## Next Steps

Now that you understand the structure, learn about:
- [Context and Variables](03-context-and-variables.md) - Deep dive into variable management
- [HTTP Steps](steps/http-step.md) - Making HTTP requests
- [Templates](04-templates.md) - Creating reusable test components