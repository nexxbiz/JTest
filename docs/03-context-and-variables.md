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

## Step IDs and Automatic Storage

### The `id` Property

Every step can have an `id` property that serves two purposes:

1. **Automatic Storage**: Step results are automatically saved with the ID as the variable name
2. **Reference**: You can reference the step's results from other steps

```json
{
    "type": "http",
    "id": "userLogin",
    "method": "POST", 
    "url": "/auth/login",
    "body": {
        "username": "user@example.com",
        "password": "password123"
    }
}
```

After this step runs, the results are automatically available as `{{$.userLogin}}`:

```json
{
    "type": "http",
    "method": "GET",
    "url": "/user/profile",
    "headers": {
        "Authorization": "Bearer {{$.userLogin.body.token}}"
    }
}
```

### When to Use IDs

**✅ Use IDs when:**
- You need to reference step results later
- Multiple steps use the same response data  
- You want clear, named references instead of generic `$.this`

**❌ Don't use IDs when:**
- The step result is only used in the next step (just use `$.this`)
- You're doing simple sequential operations

### Auto-Save vs Manual Save

```json
// With ID (automatic storage)
{
    "type": "http",
    "id": "fetchUsers",
    "method": "GET",
    "url": "/users"
}
// Results automatically available as {{$.fetchUsers}}

// Without ID (manual save if needed)
{
    "type": "http", 
    "method": "GET",
    "url": "/users",
    "save": {
        "$.globals.users": "{{$.this.body}}"
    }
}
// Results manually saved to {{$.globals.users}}
```

**Best Practice**: Use `id` for temporary step-to-step data, use `save` for data that needs to persist across multiple tests.

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

## Datasets: Data-Driven Testing

Datasets allow you to run the same test multiple times with different data. This is perfect for testing various inputs, edge cases, or user scenarios.

### Basic Dataset Structure

```json
{
    "tests": [
        {
            "name": "User Registration Validation",
            "description": "Test user registration with different email formats",
            "steps": [
                {
                    "type": "http",
                    "method": "POST",
                    "url": "/auth/register",
                    "body": {
                        "email": "{{$.case.email}}",
                        "password": "TestPass123"
                    },
                    "assert": [
                        {
                            "op": "equals",
                            "actualValue": "{{$.this.statusCode}}",
                            "expectedValue": "{{$.case.expectedStatus}}"
                        }
                    ]
                }
            ],
            "datasets": [
                {
                    "name": "valid-email",
                    "case": {
                        "email": "user@example.com",
                        "expectedStatus": 201
                    }
                },
                {
                    "name": "invalid-email-format",
                    "case": {
                        "email": "invalid-email",
                        "expectedStatus": 400
                    }
                },
                {
                    "name": "missing-domain",
                    "case": {
                        "email": "user@",
                        "expectedStatus": 400
                    }
                }
            ]
        }
    ]
}
```

### How Datasets Work

1. **Test Execution**: JTest runs the test once for each dataset
2. **Variable Access**: Use `{{$.case.variableName}}` to access dataset values
3. **Dataset Names**: Each dataset's `name` appears in test reports to identify which data caused failures
4. **Isolation**: Each dataset run is independent - variables don't carry over between runs

### Dataset Variables in Context

Within each dataset run, you can access:

- `{{$.case.fieldName}}` - Values from the current dataset
- `{{$.env.configValue}}` - Environment variables (same for all datasets)
- `{{$.globals.sharedData}}` - Global variables (shared across datasets)
- `{{$.this.responseData}}` - Current step results

### Advanced Dataset Example

```json
{
    "tests": [
        {
            "name": "E-commerce Product Search",
            "description": "Test product search with different criteria",
            "steps": [
                {
                    "type": "http",
                    "method": "GET",
                    "url": "/products/search",
                    "query": {
                        "category": "{{$.case.category}}",
                        "priceMin": "{{$.case.priceRange.min}}",
                        "priceMax": "{{$.case.priceRange.max}}"
                    },
                    "assert": [
                        {
                            "op": "equals",
                            "actualValue": "{{$.this.statusCode}}",
                            "expectedValue": 200,
                            "description": "Search should succeed for {{$.case.description}}"
                        },
                        {
                            "op": "greaterorequal",
                            "actualValue": "{{$.this.body.totalResults}}",
                            "expectedValue": "{{$.case.expectedMinResults}}",
                            "description": "Should find at least {{$.case.expectedMinResults}} products for {{$.case.description}}"
                        }
                    ]
                }
            ],
            "datasets": [
                {
                    "name": "electronics-budget",
                    "case": {
                        "category": "electronics",
                        "priceRange": {"min": 10, "max": 100},
                        "expectedMinResults": 5,
                        "description": "budget electronics"
                    }
                },
                {
                    "name": "electronics-premium", 
                    "case": {
                        "category": "electronics",
                        "priceRange": {"min": 500, "max": 2000},
                        "expectedMinResults": 2,
                        "description": "premium electronics"
                    }
                },
                {
                    "name": "books-any-price",
                    "case": {
                        "category": "books",
                        "priceRange": {"min": 0, "max": 999999},
                        "expectedMinResults": 10,
                        "description": "books at any price"
                    }
                }
            ]
        }
    ]
}
```

### Datasets in Test Reports

Dataset names and descriptions appear in markdown reports:

```markdown
### Test: User Registration Validation

**Status:** PASSED
**Duration:** 450ms

#### Dataset: valid-email
**Status:** PASSED
- Email validation should accept valid@example.com

#### Dataset: invalid-email-format  
**Status:** PASSED
- Email validation should reject invalid-email

#### Dataset: missing-domain
**Status:** PASSED
- Email validation should reject user@
```

### When to Use Datasets

**✅ Perfect for:**
- Input validation testing (different email formats, password requirements)
- Boundary value testing (min/max values, edge cases)
- Multiple user scenarios (admin, user, guest permissions)
- Different environment configurations
- Error condition testing

**❌ Avoid for:**
- Sequential workflows where steps depend on each other
- Tests that modify shared state (unless you clean up between runs)
- Large datasets that slow down test execution unnecessarily

### Dataset Best Practices

1. **Descriptive Names**: Use clear dataset names like `valid-email` not `test1`
2. **Clear Descriptions**: Add description fields explaining what each dataset tests
3. **Meaningful Assertions**: Include descriptive assertion messages using `{{$.case.description}}`
4. **Independent Data**: Ensure each dataset can run independently
5. **Focused Testing**: Keep datasets focused on one aspect at a time

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
- [Assertions](05-assertions.md) - Validate your data effectively  
- [Templates](04-templates.md) - Create reusable test patterns