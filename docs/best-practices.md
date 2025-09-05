# Best Practices

This guide outlines proven patterns and practices for writing effective, maintainable, and reliable JTest suites.

## Test Organization

### File Structure

Organize tests by feature or domain:

```
tests/
├── auth/
│   ├── login-tests.json
│   ├── logout-tests.json
│   └── password-reset-tests.json
├── users/
│   ├── user-crud-tests.json
│   ├── user-permissions-tests.json
│   └── user-profiles-tests.json
├── orders/
│   ├── order-creation-tests.json
│   ├── order-management-tests.json
│   └── order-payment-tests.json
└── integration/
    ├── end-to-end-workflows.json
    └── cross-service-tests.json
```

### Template Organization

Keep templates organized by purpose:

```
templates/
├── auth/
│   ├── oauth-templates.json
│   ├── basic-auth-templates.json
│   └── token-management-templates.json
├── crud/
│   ├── user-crud-templates.json
│   ├── order-crud-templates.json
│   └── generic-crud-templates.json
├── validation/
│   ├── response-validation-templates.json
│   ├── error-handling-templates.json
│   └── data-format-templates.json
└── utilities/
    ├── setup-templates.json
    ├── cleanup-templates.json
    └── data-generation-templates.json
```

### Naming Conventions

#### Test Files
```json
// Good: Descriptive, kebab-case
"user-registration-tests.json"
"order-payment-workflows.json"
"api-error-handling-tests.json"

// Avoid: Vague or unclear names
"tests.json"
"user-stuff.json"
"misc-tests.json"
```

#### Test Cases
```json
{
    "name": "Create user with valid data",
    "description": "Should successfully create a new user with all required fields"
}

{
    "name": "Reject user creation with invalid email",
    "description": "Should return 400 error when email format is invalid"
}
```

#### Step IDs
```json
// Good: Clear, descriptive IDs
{
    "id": "createUser",
    "id": "authenticateAdmin", 
    "id": "validateResponse"
}

// Avoid: Generic or confusing IDs
{
    "id": "step1",
    "id": "request",
    "id": "test"
}
```

## Environment Management

### Environment Variables

Use environment variables for configuration that changes between environments:

```json
{
    "env": {
        "baseUrl": "https://api.staging.example.com",
        "adminUsername": "admin@example.com",
        "adminPassword": "staging-password",
        "timeout": 30000,
        "retryCount": 3,
        "database": {
            "host": "staging-db.example.com",
            "port": 5432
        }
    }
}
```

### Global Variables

Use globals for test data and shared state:

```json
{
    "globals": {
        "testUser": {
            "name": "Test User",
            "email": "testuser@example.com",
            "role": "user"
        },
        "defaultHeaders": {
            "Content-Type": "application/json",
            "Accept": "application/json",
            "User-Agent": "JTest/1.0"
        },
        "testData": {
            "validProduct": {
                "name": "Test Product",
                "price": 29.99,
                "category": "electronics"
            }
        }
    }
}
```

### Environment-Specific Files

Create separate files for different environments:

```
environments/
├── development.json
├── staging.json
├── production.json
└── local.json
```

**development.json:**
```json
{
    "env": {
        "baseUrl": "http://localhost:8080",
        "timeout": 10000,
        "debugMode": true
    }
}
```

**production.json:**
```json
{
    "env": {
        "baseUrl": "https://api.example.com",
        "timeout": 30000,
        "debugMode": false
    }
}
```

## Data Management

### Test Data Isolation

Create isolated test data for each test:

```json
{
    "tests": [
        {
            "name": "User registration test",
            "steps": [
                {
                    "type": "http",
                    "method": "POST",
                    "url": "/api/users",
                    "body": {
                        "email": "user-{{$.env.testRunId}}@example.com",
                        "name": "Test User {{$.env.testRunId}}"
                    }
                }
            ]
        }
    ]
}
```

### Data Cleanup

Always clean up test data:

```json
{
    "tests": [
        {
            "name": "Complete user lifecycle test",
            "steps": [
                {
                    "type": "http",
                    "id": "createUser",
                    "method": "POST",
                    "url": "/api/users",
                    "body": "{{$.globals.testUser}}"
                },
                {
                    "type": "http",
                    "method": "GET",
                    "url": "/api/users/{{$.createUser.body.id}}"
                },
                {
                    "type": "http",
                    "description": "Cleanup: Delete test user",
                    "method": "DELETE",
                    "url": "/api/users/{{$.createUser.body.id}}"
                }
            ]
        }
    ]
}
```

### Dynamic Test Data

Generate unique test data to avoid conflicts:

```json
{
    "globals": {
        "timestamp": "{{$.now}}",
        "uniqueId": "{{$.uuid}}",
        "testUser": {
            "email": "test-{{$.timestamp}}@example.com",
            "username": "testuser-{{$.uniqueId}}"
        }
    }
}
```

## Error Handling

### Comprehensive Status Code Validation

```json
{
    "assert": [
        {
            "op": "in",
            "actualValue": "{{$.this.statusCode}}",
            "expectedValue": [200, 201, 202, 204],
            "message": "Expected successful status code"
        }
    ]
}
```

### Error Response Testing

Always test error scenarios:

```json
{
    "tests": [
        {
            "name": "Invalid email format validation",
            "steps": [
                {
                    "type": "http",
                    "method": "POST",
                    "url": "/api/users",
                    "body": {
                        "email": "invalid-email",
                        "name": "Test User"
                    },
                    "assert": [
                        {
                            "op": "equals",
                            "actualValue": "{{$.this.statusCode}}",
                            "expectedValue": 400
                        },
                        {
                            "op": "exists",
                            "actualValue": "{{$.this.body.error}}"
                        },
                        {
                            "op": "contains",
                            "actualValue": "{{$.this.body.error.message}}",
                            "expectedValue": "email"
                        }
                    ]
                }
            ]
        }
    ]
}
```

### Graceful Degradation

Handle optional dependencies gracefully:

```json
{
    "tests": [
        {
            "name": "User profile with optional avatar",
            "steps": [
                {
                    "type": "http",
                    "method": "GET",
                    "url": "/api/users/{{$.globals.userId}}/profile",
                    "assert": [
                        {
                            "op": "equals",
                            "actualValue": "{{$.this.statusCode}}",
                            "expectedValue": 200
                        },
                        {
                            "op": "exists",
                            "actualValue": "{{$.this.body.user.name}}"
                        },
                        {
                            "op": "exists",
                            "actualValue": "{{$.this.body.user.email}}"
                        }
                    ]
                },
                {
                    "type": "assert",
                    "description": "Avatar is optional but if present must be valid URL",
                    "assert": [
                        {
                            "op": "matches",
                            "actualValue": "{{$.this.body.user.avatar || ''}}",
                            "expectedValue": "^(|https?://.*)"
                        }
                    ]
                }
            ]
        }
    ]
}
```

## Authentication Patterns

### Token-Based Authentication

```json
{
    "tests": [
        {
            "name": "Authenticated API access pattern",
            "steps": [
                {
                    "type": "use",
                    "id": "auth",
                    "template": "oauth-authentication",
                    "params": {
                        "clientId": "{{$.env.clientId}}",
                        "clientSecret": "{{$.env.clientSecret}}"
                    }
                },
                {
                    "type": "http",
                    "method": "GET",
                    "url": "/api/protected-resource",
                    "headers": {
                        "Authorization": "{{$.auth.authHeader}}"
                    }
                }
            ]
        }
    ]
}
```

### Session-Based Authentication

```json
{
    "tests": [
        {
            "name": "Session-based authentication flow",
            "steps": [
                {
                    "type": "http",
                    "id": "login",
                    "method": "POST",
                    "url": "/auth/login",
                    "body": {
                        "username": "{{$.env.username}}",
                        "password": "{{$.env.password}}"
                    },
                    "save": {
                        "$.globals.sessionCookie": "{{$.this.headers['set-cookie']}}"
                    }
                },
                {
                    "type": "http",
                    "method": "GET",
                    "url": "/api/profile",
                    "headers": {
                        "Cookie": "{{$.globals.sessionCookie}}"
                    }
                }
            ]
        }
    ]
}
```

## Performance Testing

### Response Time Validation

```json
{
    "assert": [
        {
            "op": "less_than",
            "actualValue": "{{$.this.responseTime}}",
            "expectedValue": 2000,
            "message": "Response time should be under 2 seconds"
        }
    ]
}
```

### Load Testing Patterns

```json
{
    "tests": [
        {
            "name": "API load test simulation",
            "description": "Simulate multiple concurrent users",
            "steps": [
                {
                    "type": "use",
                    "template": "create-multiple-users",
                    "params": {
                        "userCount": 10,
                        "authToken": "{{$.globals.adminToken}}"
                    }
                },
                {
                    "type": "assert",
                    "assert": [
                        {
                            "op": "less_than",
                            "actualValue": "{{$.this.totalTime}}",
                            "expectedValue": 5000
                        }
                    ]
                }
            ]
        }
    ]
}
```

## Assertion Best Practices

### Assertion Hierarchy

Follow this order for assertions:

1. **Status Code** - Always check first
2. **Response Structure** - Ensure expected format
3. **Required Fields** - Check existence
4. **Data Types** - Validate types
5. **Content Validation** - Check specific values

```json
{
    "assert": [
        // 1. Status code
        {
            "op": "equals",
            "actualValue": "{{$.this.statusCode}}",
            "expectedValue": 200
        },
        // 2. Response structure
        {
            "op": "exists",
            "actualValue": "{{$.this.body}}"
        },
        {
            "op": "is_type",
            "actualValue": "{{$.this.body}}",
            "expectedValue": "object"
        },
        // 3. Required fields
        {
            "op": "exists",
            "actualValue": "{{$.this.body.user}}"
        },
        {
            "op": "exists",
            "actualValue": "{{$.this.body.user.id}}"
        },
        // 4. Data types
        {
            "op": "is_type",
            "actualValue": "{{$.this.body.user.id}}",
            "expectedValue": "string"
        },
        // 5. Content validation
        {
            "op": "matches",
            "actualValue": "{{$.this.body.user.id}}",
            "expectedValue": "^user_[0-9a-f]{8}$"
        }
    ]
}
```

### Specific vs Generic Assertions

```json
// Good: Specific validation
{
    "op": "matches",
    "actualValue": "{{$.this.body.email}}",
    "expectedValue": "^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\\.[a-zA-Z]{2,}$"
}

// Avoid: Too generic
{
    "op": "contains",
    "actualValue": "{{$.this.body.email}}",
    "expectedValue": "@"
}
```

### Meaningful Error Messages

```json
{
    "op": "equals",
    "actualValue": "{{$.this.statusCode}}",
    "expectedValue": 201,
    "message": "User creation should return 201 Created status"
}
```

## Template Design

### Single Responsibility

```json
// Good: Focused template
{
    "name": "authenticate-user",
    "description": "Performs user authentication and returns token",
    "params": {
        "username": {"type": "string", "required": true},
        "password": {"type": "string", "required": true}
    }
}

// Avoid: Multi-purpose template
{
    "name": "user-everything",
    "description": "Authenticates, creates user, sends email, etc."
}
```

### Clear Parameter Contracts

```json
{
    "params": {
        "userEmail": {
            "type": "string",
            "required": true,
            "description": "Valid email address for the user",
            "pattern": "^[^@]+@[^@]+\\.[^@]+$"
        },
        "timeout": {
            "type": "number",
            "required": false,
            "default": 30000,
            "minimum": 1000,
            "description": "Request timeout in milliseconds"
        }
    }
}
```

### Meaningful Output Mapping

```json
// Good: Clear, specific outputs
{
    "output": {
        "authToken": "{{$.login.body.access_token}}",
        "userId": "{{$.login.body.user.id}}",
        "tokenExpiry": "{{$.login.body.expires_at}}",
        "loginSuccessful": true
    }
}

// Avoid: Raw or unclear outputs
{
    "output": {
        "result": "{{$.login.body}}"
    }
}
```

## Variable Management

### Naming Conventions

```json
// Good: Descriptive, consistent naming
{
    "globals": {
        "currentUserId": "user-123",
        "authToken": "bearer-token-xyz",
        "testRunTimestamp": "2024-01-15T10:30:00Z"
    }
}

// Avoid: Unclear or inconsistent naming
{
    "globals": {
        "id": "user-123",
        "token": "bearer-token-xyz",
        "time": "2024-01-15T10:30:00Z"
    }
}
```

### Variable Scope Usage

```json
// env: Configuration and environment-specific values
{
    "env": {
        "apiUrl": "https://api.example.com",
        "timeout": 30000,
        "debugMode": true
    }
}

// globals: Shared test data and state
{
    "globals": {
        "testUser": {...},
        "authToken": "...",
        "createdResourceIds": []
    }
}

// Step-level save: Temporary or step-specific data
{
    "save": {
        "$.responseTime": "{{$.this.responseTime}}",
        "$.validationResult": true
    }
}
```

## Security Considerations

### Credential Management

```json
// Good: Use environment variables for sensitive data
{
    "env": {
        "apiKey": "{{env.API_KEY}}",
        "adminPassword": "{{env.ADMIN_PASSWORD}}"
    }
}

// Avoid: Hardcoded credentials
{
    "env": {
        "apiKey": "secret-key-123",
        "adminPassword": "admin123"
    }
}
```

### Token Validation

```json
{
    "assert": [
        {
            "op": "exists",
            "actualValue": "{{$.this.body.token}}"
        },
        {
            "op": "is_type",
            "actualValue": "{{$.this.body.token}}",
            "expectedValue": "string"
        },
        {
            "op": "greater_than",
            "actualValue": "{{$.this.body.token.length}}",
            "expectedValue": 10
        },
        {
            "op": "not_contains",
            "actualValue": "{{$.this.body.token}}",
            "expectedValue": "password"
        }
    ]
}
```

## Documentation Practices

### Test Documentation

```json
{
    "version": "1.0",
    "info": {
        "name": "User Management API Tests",
        "description": "Comprehensive test suite for user management endpoints",
        "version": "2.1.0",
        "author": "QA Team",
        "lastUpdated": "2024-01-15"
    },
    "tests": [
        {
            "name": "User Registration Happy Path",
            "description": "Tests successful user registration with valid data including email verification",
            "steps": [...]
        }
    ]
}
```

### Step Documentation

```json
{
    "type": "http",
    "id": "createUser",
    "description": "Create new user account with registration data",
    "method": "POST",
    "url": "/api/users",
    "body": "{{$.globals.newUserData}}"
}
```

### Template Documentation

```json
{
    "name": "oauth-authentication",
    "description": "OAuth 2.0 client credentials flow for service-to-service authentication",
    "version": "1.2.0",
    "author": "Platform Team",
    "params": {
        "clientId": {
            "type": "string",
            "required": true,
            "description": "OAuth client identifier provided during app registration"
        }
    }
}
```

## Maintenance and Refactoring

### Regular Review Checklist

1. **Remove unused variables** and templates
2. **Update environment configurations** for new environments  
3. **Refactor common patterns** into templates
4. **Update assertions** for API changes
5. **Review error handling** for new error cases
6. **Optimize performance** by reducing redundant calls

### Template Extraction

When you see repeated patterns, extract them:

```json
// Before: Repeated authentication in multiple tests
{
    "steps": [
        {
            "type": "http",
            "method": "POST", 
            "url": "/auth/login",
            "body": {"username": "...", "password": "..."}
        }
    ]
}

// After: Extract to template
{
    "steps": [
        {
            "type": "use",
            "template": "authenticate",
            "params": {"username": "...", "password": "..."}
        }
    ]
}
```

### Version Control Best Practices

1. **Commit frequently** with descriptive messages
2. **Use branches** for major test changes
3. **Tag releases** when updating test suites
4. **Document changes** in commit messages
5. **Review test changes** like code changes

## Debugging and Troubleshooting

### Debug Outputs

Add temporary debug assertions:

```json
{
    "type": "assert",
    "description": "Debug: Check response structure",
    "assert": [
        {
            "op": "debug",
            "actualValue": "{{$.this.body}}",
            "message": "Full response for debugging"
        }
    ]
}
```

### Incremental Testing

Build tests incrementally:

```json
// Step 1: Basic connectivity
{
    "type": "http",
    "method": "GET", 
    "url": "/api/health"
}

// Step 2: Add authentication
{
    "type": "http",
    "method": "GET",
    "url": "/api/health",
    "headers": {"Authorization": "Bearer {{$.globals.token}}"}
}

// Step 3: Add assertions
{
    "assert": [
        {"op": "equals", "actualValue": "{{$.this.statusCode}}", "expectedValue": 200}
    ]
}
```

### Common Pitfalls to Avoid

1. **Hardcoded values** instead of variables
2. **Missing error handling** for edge cases
3. **Overly complex assertions** that are hard to debug
4. **Test dependencies** that make tests fragile
5. **Poor variable naming** that makes tests unclear
6. **Missing cleanup** that leaves test data behind
7. **Inconsistent patterns** across test files

## Next Steps

- [CLI Usage](cli-usage.md) - Running tests effectively
- [Troubleshooting](troubleshooting.md) - Debugging test issues
- [CI/CD Integration](ci-cd-integration.md) - Automated test execution