# Context and Variables

JTest's variable system is the foundation for creating dynamic, data-driven tests. Understanding how context works is essential for writing effective tests.

## Variable Scopes

JTest organizes variables into different scopes that determine where data is available and how long it persists.

### Environment Variables (`$.env`)

Environment variables are configuration values that typically change between environments (dev, staging, production).

```json
{
    "env": {
        "baseUrl": "https://api.example.com",
        "apiKey": "your-api-key",
        "timeout": 30000,
        "retryCount": 3
    }
}
```

**Access Pattern:** `{{$.env.variableName}}`

**Examples:**
```json
{
    "type": "http",
    "url": "{{$.env.baseUrl}}/users",
    "headers": {
        "Authorization": "Bearer {{$.env.apiKey}}"
    },
    "timeout": "{{$.env.timeout}}"
}
```

**Best Practices:**
- Store URLs, API keys, and environment-specific settings
- Use meaningful names like `baseUrl` instead of `url`
- Group related values with objects

### Global Variables (`$.globals`)

Global variables are shared across all tests in a test suite and persist throughout execution.

```json
{
    "globals": {
        "testUser": {
            "username": "testuser@example.com",
            "password": "testpass123"
        },
        "defaultHeaders": {
            "Content-Type": "application/json",
            "Accept": "application/json"
        }
    }
}
```

**Access Pattern:** `{{$.globals.variableName}}`

**Examples:**
```json
{
    "type": "http",
    "method": "POST",
    "url": "/auth/login",
    "body": {
        "username": "{{$.globals.testUser.username}}",
        "password": "{{$.globals.testUser.password}}"
    }
}
```

### Step Results (`$.this` and `$.stepId`)

Every step stores its results in the context, accessible in subsequent steps.

#### Current Step Results (`$.this`)
The most recently executed step's results:

```json
{
    "type": "http",
    "method": "GET",
    "url": "/api/user/profile",
    "assert": [
        {
            "op": "exists",
            "actualValue": "{{$.this.body.user.id}}"
        },
        {
            "op": "equals",
            "actualValue": "{{$.this.statusCode}}",
            "expectedValue": 200
        }
    ]
}
```

#### Named Step Results (`$.stepId`)
Reference specific steps by their ID:

```json
{
    "type": "http",
    "id": "login",
    "method": "POST",
    "url": "/auth/login",
    "body": {
        "username": "user@example.com",
        "password": "password123"
    }
},
{
    "type": "http",
    "method": "GET",
    "url": "/user/profile",
    "headers": {
        "Authorization": "Bearer {{$.login.body.token}}"
    }
}
```

## HTTP Step Context

HTTP steps automatically populate the context with response data:

```json
{
    "statusCode": 200,
    "headers": {
        "content-type": "application/json",
        "x-request-id": "12345"
    },
    "body": {
        "user": {
            "id": "user-123",
            "name": "John Doe",
            "email": "john@example.com"
        },
        "permissions": ["read", "write"]
    }
}
```

**Available Properties:**
- `{{$.this.statusCode}}` - HTTP status code (200, 404, etc.)
- `{{$.this.headers}}` - Response headers object
- `{{$.this.body}}` - Parsed response body (JSON object/array)
- `{{$.this.body.user.id}}` - Nested properties using dot notation

## Saving Variables

Use the `save` property to store values from step results into the global context:

### Basic Save Operations

```json
{
    "type": "http",
    "method": "POST",
    "url": "/auth/login",
    "body": {
        "username": "user@example.com",
        "password": "password123"
    },
    "save": {
        "$.globals.authToken": "{{$.this.body.token}}",
        "$.globals.userId": "{{$.this.body.user.id}}",
        "$.globals.loginTime": "{{$.this.body.timestamp}}"
    }
}
```

### Complex Save Operations

```json
{
    "type": "http",
    "method": "GET",
    "url": "/orders",
    "save": {
        "$.globals.orders": "{{$.this.body.orders}}",
        "$.globals.orderCount": "{{$.this.body.totalCount}}",
        "$.globals.firstOrderId": "{{$.this.body.orders[0].id}}",
        "$.globals.orderSummary": {
            "count": "{{$.this.body.totalCount}}",
            "lastUpdated": "{{$.this.body.lastModified}}"
        }
    }
}
```

## Variable Precedence

When variables have the same name in different scopes, JTest follows this precedence order:

1. **Step Results** (`$.this`, `$.stepId`) - Highest precedence
2. **Global Variables** (`$.globals`)
3. **Environment Variables** (`$.env`) - Lowest precedence

### Example of Precedence

```json
{
    "env": {
        "userId": "env-user-123"
    },
    "globals": {
        "userId": "global-user-456"
    },
    "tests": [
        {
            "name": "Variable Precedence Test",
            "steps": [
                {
                    "type": "http",
                    "id": "getUser",
                    "method": "GET",
                    "url": "/users/current",
                    "save": {
                        "$.globals.userId": "{{$.this.body.id}}"
                    }
                },
                {
                    "type": "http",
                    "method": "GET",
                    "url": "/users/{{$.globals.userId}}/profile"
                    // This will use the value saved from the previous step
                }
            ]
        }
    ]
}
```

## JSONPath Expressions

JTest uses JSONPath to navigate and extract data from JSON structures.

### Basic JSONPath Examples

```json
{
    "body": {
        "user": {
            "id": "123",
            "name": "John Doe",
            "addresses": [
                {
                    "type": "home",
                    "street": "123 Main St"
                },
                {
                    "type": "work",
                    "street": "456 Office Blvd"
                }
            ]
        },
        "orders": [
            {"id": "order-1", "total": 25.99},
            {"id": "order-2", "total": 15.50}
        ]
    }
}
```

**Extracting Values:**
- `{{$.this.body.user.id}}` → `"123"`
- `{{$.this.body.user.name}}` → `"John Doe"`
- `{{$.this.body.user.addresses[0].street}}` → `"123 Main St"`
- `{{$.this.body.orders[1].total}}` → `15.50`

### Advanced JSONPath

```json
// Get all order IDs
"{{$.this.body.orders[*].id}}"

// Get first order total
"{{$.this.body.orders[0].total}}"

// Get home address
"{{$.this.body.user.addresses[?(@.type=='home')].street}}"
```

## Dynamic Test Data

Use variables to create dynamic, reusable tests:

### Parameterized Tests

```json
{
    "env": {
        "testUsers": [
            {"username": "user1@test.com", "role": "admin"},
            {"username": "user2@test.com", "role": "user"}
        ]
    },
    "tests": [
        {
            "name": "Test User Login",
            "steps": [
                {
                    "type": "http",
                    "method": "POST",
                    "url": "/auth/login",
                    "body": {
                        "username": "{{$.env.testUsers[0].username}}",
                        "password": "defaultPassword"
                    },
                    "assert": [
                        {
                            "op": "equals",
                            "actualValue": "{{$.this.body.user.role}}",
                            "expectedValue": "{{$.env.testUsers[0].role}}"
                        }
                    ]
                }
            ]
        }
    ]
}
```

### Chaining Requests

```json
{
    "tests": [
        {
            "name": "Create Order Workflow",
            "steps": [
                {
                    "type": "http",
                    "id": "createUser",
                    "method": "POST",
                    "url": "/users",
                    "body": {
                        "name": "Test User",
                        "email": "test@example.com"
                    }
                },
                {
                    "type": "http",
                    "id": "createOrder",
                    "method": "POST",
                    "url": "/orders",
                    "body": {
                        "userId": "{{$.createUser.body.id}}",
                        "items": [
                            {"sku": "ITEM-1", "quantity": 2}
                        ]
                    }
                },
                {
                    "type": "http",
                    "method": "GET",
                    "url": "/orders/{{$.createOrder.body.id}}",
                    "assert": [
                        {
                            "op": "equals",
                            "actualValue": "{{$.this.body.userId}}",
                            "expectedValue": "{{$.createUser.body.id}}"
                        }
                    ]
                }
            ]
        }
    ]
}
```

## Common Patterns

### Authentication Flow

```json
{
    "tests": [
        {
            "name": "Authenticated API Access",
            "steps": [
                {
                    "type": "http",
                    "id": "login",
                    "description": "Get authentication token",
                    "method": "POST",
                    "url": "{{$.env.baseUrl}}/auth/login",
                    "body": {
                        "username": "{{$.env.username}}",
                        "password": "{{$.env.password}}"
                    },
                    "save": {
                        "$.globals.authToken": "{{$.this.body.accessToken}}"
                    }
                },
                {
                    "type": "http",
                    "description": "Access protected resource",
                    "method": "GET",
                    "url": "{{$.env.baseUrl}}/user/profile",
                    "headers": {
                        "Authorization": "Bearer {{$.globals.authToken}}"
                    }
                }
            ]
        }
    ]
}
```

### Error Handling

```json
{
    "type": "http",
    "method": "POST",
    "url": "/users",
    "body": {
        "email": "invalid-email"
    },
    "assert": [
        {
            "op": "equals",
            "actualValue": "{{$.this.statusCode}}",
            "expectedValue": 400
        },
        {
            "op": "contains",
            "actualValue": "{{$.this.body.errors[0].message}}",
            "expectedValue": "Invalid email format"
        }
    ]
}
```

## Best Practices

### Variable Naming
- Use descriptive names: `authToken` not `token`
- Use camelCase: `userId` not `user_id`
- Group related data in objects

### Data Organization
- Store configuration in `env`
- Store test data in `globals`
- Use step IDs for important intermediate results

### Performance
- Avoid deeply nested JSONPath expressions
- Cache frequently accessed values in globals
- Use specific paths instead of wildcard searches

## Troubleshooting

### Common Issues

**Variable Not Found:**
```
Error: Variable $.this.body.user.id not found
```
- Check if the HTTP request succeeded
- Verify the response structure matches your expectations
- Use assertions to validate response structure first

**Incorrect JSONPath:**
```json
// Wrong: Missing array index
"{{$.this.body.users.id}}"

// Correct: Include array index
"{{$.this.body.users[0].id}}"
```

**Variable Scope Issues:**
- Remember that step results are only available after the step executes
- Global variables persist across tests, environment variables are read-only

## Next Steps

Now that you understand variables and context:
- [HTTP Steps](steps/http-step.md) - Learn about making HTTP requests
- [Assertions](assertions.md) - Validate your data effectively  
- [Templates](templates.md) - Create reusable test patterns