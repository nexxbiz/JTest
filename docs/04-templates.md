# Templates

Templates are reusable test components that function like **static methods** - they run in their own isolated context and only interact with the calling test through input parameters and returned values. This isolation prevents variable collisions and makes templates predictable and safe to use.

## How Templates Work

### Template as Static Methods

Think of templates like static methods in programming:

```json
// Similar to: function authenticateUser(username, password) { ... }
{
    "name": "authenticate-user",
    "description": "Login with credentials and return auth token",
    "steps": [
        {
            "type": "http",
            "method": "POST",
            "url": "{{$.params.tokenUrl}}",
            "body": {
                "username": "{{$.params.username}}",
                "password": "{{$.params.password}}"
            }
        }
    ]
}
```

### Isolated Context

**Key Principle**: Templates run in complete isolation. They cannot access or modify variables from the calling test, except through:

1. **Input**: Parameters passed via `with`
2. **Output**: Values returned via the template result

```json
// ❌ Template CANNOT access these variables
{
    "globals": {
        "userId": "12345"
    },
    "tests": [
        {
            "steps": [
                {
                    "type": "use",
                    "template": "some-template"
                    // Template cannot see $.globals.userId
                }
            ]
        }
    ]
}
```

```json
// ✅ Template CAN access these parameters
{
    "tests": [
        {
            "steps": [
                {
                    "type": "use",
                    "template": "some-template",
                    "with": {
                        "userId": "{{$.globals.userId}}"
                    }
                    // Template sees this as $.params.userId
                }
            ]
        }
    ]
}
```

## Template Structure

Templates are defined in the `components.templates` section:

```json
{
    "version": "1.0",
    "components": {
        "templates": [
            {
                "name": "template-name",
                "description": "What this template does",
                "params": {
                    // Parameter definitions
                },
                "steps": [
                    // Template steps
                ],
                "output": {
                    // Output mappings
                }
            }
        ]
    }
}
```

### Template Properties

#### `name` (Required)
Unique identifier for the template:

```json
{
    "name": "authenticate-user"
}
```

#### `description` (Optional)
Human-readable description of the template's purpose:

```json
{
    "name": "authenticate-user",
    "description": "Performs user authentication and returns access token"
}
```

#### `params` (Optional)
Parameter definitions with types and validation:

```json
{
    "params": {
        "username": {
            "type": "string",
            "required": true,
            "description": "User's email or username"
        },
        "password": {
            "type": "string", 
            "required": true,
            "description": "User's password"
        },
        "rememberMe": {
            "type": "boolean",
            "required": false,
            "default": false,
            "description": "Whether to extend session duration"
        }
    }
}
```

#### `steps` (Required)
Array of steps to execute within the template:

```json
{
    "steps": [
        {
            "type": "http",
            "method": "POST",
            "url": "/auth/login",
            "body": {
                "username": "{{username}}",
                "password": "{{password}}"
            }
        }
    ]
}
```

#### `output` (Optional)
Defines what data to expose to the parent context:

```json
{
    "output": {
        "token": "{{$.this.body.accessToken}}",
        "userId": "{{$.this.body.user.id}}",
        "expiresAt": "{{$.this.body.expiresAt}}"
    }
}
```

## Parameter Types and Validation

### Supported Types

#### String Parameters
```json
{
    "username": {
        "type": "string",
        "required": true,
        "minLength": 3,
        "maxLength": 50
    }
}
```

#### Number Parameters
```json
{
    "timeout": {
        "type": "number",
        "required": false,
        "default": 30000,
        "minimum": 1000,
        "maximum": 300000
    }
}
```

#### Boolean Parameters
```json
{
    "enableLogging": {
        "type": "boolean",
        "required": false,
        "default": true
    }
}
```

#### Object Parameters
```json
{
    "userConfig": {
        "type": "object",
        "required": true,
        "properties": {
            "name": {"type": "string"},
            "email": {"type": "string"}
        }
    }
}
```

#### Array Parameters
```json
{
    "tags": {
        "type": "array",
        "required": false,
        "items": {"type": "string"},
        "minItems": 1,
        "maxItems": 10
    }
}
```

### Parameter Validation Properties

- **`required`**: Whether the parameter must be provided
- **`default`**: Default value if not provided
- **`description`**: Human-readable description
- **`minLength`/`maxLength`**: String length constraints
- **`minimum`/`maximum`**: Numeric range constraints
- **`minItems`/`maxItems`**: Array size constraints
- **`enum`**: Allowed values list

```json
{
    "priority": {
        "type": "string",
        "required": false,
        "default": "normal",
        "enum": ["low", "normal", "high", "critical"],
        "description": "Task priority level"
    }
}
```

## Template Context and Scope

### Isolated Execution Context

Templates run in their own isolated context:

```json
{
    "version": "1.0",
    "globals": {
        "parentVar": "parent-value"
    },
    "components": {
        "templates": [
            {
                "name": "isolated-template",
                "params": {
                    "inputData": {"type": "string", "required": true}
                },
                "steps": [
                    {
                        "type": "http",
                        "method": "GET",
                        "url": "/api/process",
                        "query": {
                            "data": "{{inputData}}"
                        },
                        "save": {
                            "$.templateVar": "{{$.this.body.result}}"
                        }
                    }
                ],
                "output": {
                    "processedData": "{{$.templateVar}}"
                }
            }
        ]
    }
}
```

### Parameter Access
Within templates, parameters are accessed directly:

```json
{
    "steps": [
        {
            "type": "http",
            "url": "/api/users/{{userId}}",  // Direct parameter access
            "headers": {
                "Authorization": "Bearer {{authToken}}"  // Direct parameter access
            }
        }
    ]
}
```

### Step Results Access
Template steps use standard context access:

```json
{
    "steps": [
        {
            "type": "http",
            "id": "createUser",
            "method": "POST",
            "url": "/api/users",
            "body": {"name": "{{userName}}"}
        },
        {
            "type": "http",
            "method": "GET",
            "url": "/api/users/{{$.createUser.body.id}}"  // Standard step reference
        }
    ]
}
```

## Using Templates

### Basic Template Usage

```json
{
    "tests": [
        {
            "name": "User Authentication Test",
            "steps": [
                {
                    "type": "use",
                    "template": "authenticate-user",
                    "params": {
                        "username": "{{$.env.testUser}}",
                        "password": "{{$.env.testPassword}}"
                    }
                }
            ]
        }
    ]
}
```

### Accessing Template Outputs

```json
{
    "tests": [
        {
            "name": "Authenticated API Test",
            "steps": [
                {
                    "type": "use",
                    "id": "auth",
                    "template": "authenticate-user",
                    "params": {
                        "username": "user@example.com",
                        "password": "password123"
                    }
                },
                {
                    "type": "http",
                    "method": "GET",
                    "url": "/api/profile",
                    "headers": {
                        "Authorization": "Bearer {{$.auth.token}}"
                    }
                }
            ]
        }
    ]
}
```

## Common Template Patterns

### Authentication Template

```json
{
    "name": "oauth2-authentication",
    "description": "OAuth 2.0 client credentials flow",
    "params": {
        "clientId": {"type": "string", "required": true},
        "clientSecret": {"type": "string", "required": true},
        "scope": {"type": "string", "required": false, "default": "read write"},
        "tokenEndpoint": {"type": "string", "required": true}
    },
    "steps": [
        {
            "type": "http",
            "method": "POST",
            "url": "{{tokenEndpoint}}",
            "headers": {
                "Content-Type": "application/x-www-form-urlencoded"
            },
            "body": "grant_type=client_credentials&client_id={{clientId}}&client_secret={{clientSecret}}&scope={{scope}}",
            "assert": [
                {
                    "op": "equals",
                    "actualValue": "{{$.this.statusCode}}",
                    "expectedValue": 200
                },
                {
                    "op": "exists",
                    "actualValue": "{{$.this.body.access_token}}"
                }
            ]
        }
    ],
    "output": {
        "accessToken": "{{$.this.body.access_token}}",
        "tokenType": "{{$.this.body.token_type}}",
        "expiresIn": "{{$.this.body.expires_in}}",
        "authHeader": "{{$.this.body.token_type}} {{$.this.body.access_token}}"
    }
}
```

### CRUD Operations Template

```json
{
    "name": "create-user-workflow",
    "description": "Complete user creation and verification workflow",
    "params": {
        "userData": {
            "type": "object",
            "required": true,
            "properties": {
                "name": {"type": "string"},
                "email": {"type": "string"},
                "role": {"type": "string"}
            }
        },
        "authToken": {"type": "string", "required": true}
    },
    "steps": [
        {
            "type": "http",
            "id": "createUser",
            "description": "Create new user",
            "method": "POST",
            "url": "/api/users",
            "headers": {
                "Authorization": "Bearer {{authToken}}",
                "Content-Type": "application/json"
            },
            "body": "{{userData}}",
            "assert": [
                {
                    "op": "equals",
                    "actualValue": "{{$.this.statusCode}}",
                    "expectedValue": 201
                },
                {
                    "op": "exists",
                    "actualValue": "{{$.this.body.id}}"
                }
            ]
        },
        {
            "type": "http",
            "id": "verifyUser",
            "description": "Verify user was created correctly",
            "method": "GET",
            "url": "/api/users/{{$.createUser.body.id}}",
            "headers": {
                "Authorization": "Bearer {{authToken}}"
            },
            "assert": [
                {
                    "op": "equals",
                    "actualValue": "{{$.this.statusCode}}",
                    "expectedValue": 200
                },
                {
                    "op": "equals",
                    "actualValue": "{{$.this.body.email}}",
                    "expectedValue": "{{userData.email}}"
                }
            ]
        }
    ],
    "output": {
        "userId": "{{$.createUser.body.id}}",
        "createdUser": "{{$.createUser.body}}",
        "verifiedUser": "{{$.verifyUser.body}}",
        "success": true
    }
}
```

### Data Validation Template

```json
{
    "name": "validate-api-response",
    "description": "Standard API response validation",
    "params": {
        "responseData": {"type": "object", "required": true},
        "expectedStatus": {"type": "number", "required": false, "default": 200},
        "requiredFields": {"type": "array", "required": false, "default": []}
    },
    "steps": [
        {
            "type": "assert",
            "description": "Validate response status and structure",
            "assert": [
                {
                    "op": "equals",
                    "actualValue": "{{responseData.statusCode}}",
                    "expectedValue": "{{expectedStatus}}"
                },
                {
                    "op": "exists",
                    "actualValue": "{{responseData.body}}"
                },
                {
                    "op": "type",
                    "actualValue": "{{responseData.body}}",
                    "expectedValue": "object"
                }
            ]
        }
    ],
    "output": {
        "validated": true,
        "statusCode": "{{responseData.statusCode}}",
        "bodyType": "object"
    }
}
```

## External Template Files

### Template File Structure

Create dedicated template files (`templates/auth-templates.json`):

```json
{
    "version": "1.0",
    "components": {
        "templates": [
            {
                "name": "basic-auth",
                "description": "Basic username/password authentication",
                "params": {
                    "username": {"type": "string", "required": true},
                    "password": {"type": "string", "required": true},
                    "loginUrl": {"type": "string", "required": false, "default": "/auth/login"}
                },
                "steps": [
                    {
                        "type": "http",
                        "method": "POST",
                        "url": "{{loginUrl}}",
                        "body": {
                            "username": "{{username}}",
                            "password": "{{password}}"
                        }
                    }
                ],
                "output": {
                    "token": "{{$.this.body.token}}",
                    "user": "{{$.this.body.user}}"
                }
            },
            {
                "name": "token-refresh",
                "description": "Refresh authentication token",
                "params": {
                    "refreshToken": {"type": "string", "required": true}
                },
                "steps": [
                    {
                        "type": "http",
                        "method": "POST",
                        "url": "/auth/refresh",
                        "body": {
                            "refreshToken": "{{refreshToken}}"
                        }
                    }
                ],
                "output": {
                    "newToken": "{{$.this.body.accessToken}}",
                    "newRefreshToken": "{{$.this.body.refreshToken}}"
                }
            }
        ]
    }
}
```

### Importing Templates

```json
{
    "version": "1.0",
    "using": [
        "./templates/auth-templates.json",
        "./templates/user-templates.json",
        "./templates/order-templates.json"
    ],
    "tests": [
        {
            "name": "Integration Test",
            "steps": [
                {
                    "type": "use",
                    "template": "basic-auth",
                    "params": {
                        "username": "{{$.env.username}}",
                        "password": "{{$.env.password}}"
                    }
                }
            ]
        }
    ]
}
```

### Template File Organization

```
project/
├── tests/
│   ├── user-tests.json
│   ├── order-tests.json
│   └── integration-tests.json
└── templates/
    ├── auth/
    │   ├── oauth-templates.json
    │   └── basic-auth-templates.json
    ├── crud/
    │   ├── user-crud-templates.json
    │   └── order-crud-templates.json
    └── utilities/
        ├── validation-templates.json
        └── setup-templates.json
```

## Advanced Template Features

### Nested Template Usage

Templates can use other templates:

```json
{
    "name": "full-user-workflow",
    "description": "Complete user lifecycle including authentication",
    "params": {
        "adminCredentials": {"type": "object", "required": true},
        "newUserData": {"type": "object", "required": true}
    },
    "steps": [
        {
            "type": "use",
            "id": "adminAuth",
            "template": "basic-auth",
            "params": {
                "username": "{{adminCredentials.username}}",
                "password": "{{adminCredentials.password}}"
            }
        },
        {
            "type": "use",
            "template": "create-user-workflow",
            "params": {
                "userData": "{{newUserData}}",
                "authToken": "{{$.adminAuth.token}}"
            }
        }
    ],
    "output": {
        "adminToken": "{{$.adminAuth.token}}",
        "userId": "{{$.this.userId}}",
        "userCreated": "{{$.this.success}}"
    }
}
```

### Conditional Logic in Templates

```json
{
    "name": "adaptive-auth",
    "description": "Choose authentication method based on environment",
    "params": {
        "authMethod": {"type": "string", "enum": ["basic", "oauth"], "required": true},
        "credentials": {"type": "object", "required": true}
    },
    "steps": [
        {
            "type": "use",
            "template": "{{authMethod}}-auth",
            "params": "{{credentials}}"
        }
    ],
    "output": {
        "token": "{{$.this.token}}",
        "method": "{{authMethod}}"
    }
}
```

## Best Practices

### Template Design Principles

#### Single Responsibility
```json
// Good: Focused template
{
    "name": "authenticate-user",
    "description": "Performs user authentication only"
}

// Avoid: Multi-purpose template
{
    "name": "user-operations",
    "description": "Authenticates, creates user, sends email, etc."
}
```

#### Clear Parameter Contracts
```json
// Good: Well-defined parameters
{
    "params": {
        "apiEndpoint": {
            "type": "string",
            "required": true,
            "description": "Full URL to the API endpoint",
            "pattern": "^https?://"
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

// Avoid: Vague or unvalidated parameters
{
    "params": {
        "config": {"type": "object"}
    }
}
```

#### Meaningful Output Mapping
```json
// Good: Specific, useful outputs
{
    "output": {
        "authToken": "{{$.login.body.access_token}}",
        "userId": "{{$.login.body.user.id}}",
        "tokenExpiry": "{{$.login.body.expires_at}}",
        "authenticated": true
    }
}

// Avoid: Raw or unclear outputs
{
    "output": {
        "data": "{{$.login.body}}"
    }
}
```

### Error Handling in Templates

```json
{
    "name": "robust-api-call",
    "description": "API call with comprehensive error handling",
    "params": {
        "endpoint": {"type": "string", "required": true},
        "method": {"type": "string", "enum": ["GET", "POST", "PUT", "DELETE"], "default": "GET"}
    },
    "steps": [
        {
            "type": "http",
            "method": "{{method}}",
            "url": "{{endpoint}}",
            "assert": [
                {
                    "op": "in",
                    "actualValue": "{{$.this.statusCode}}",
                    "expectedValue": [200, 201, 202, 204]
                }
            ]
        }
    ],
    "output": {
        "success": "{{$.this.statusCode < 400}}",
        "statusCode": "{{$.this.statusCode}}",
        "data": "{{$.this.body}}",
        "hasError": "{{$.this.statusCode >= 400}}"
    }
}
```

### Performance Considerations

1. **Minimize HTTP calls** in templates
2. **Cache authentication tokens** between template uses
3. **Use specific assertions** to fail fast
4. **Avoid deep nesting** of template calls

### Documentation and Maintenance

```json
{
    "name": "user-registration",
    "description": "Complete user registration workflow including validation and welcome email",
    "version": "2.1.0",
    "author": "QA Team",
    "lastModified": "2024-01-15",
    "params": {
        "userEmail": {
            "type": "string",
            "required": true,
            "description": "Valid email address for the new user",
            "pattern": "^[^@]+@[^@]+\\.[^@]+$"
        }
    },
    "examples": [
        {
            "description": "Basic user registration",
            "params": {
                "userEmail": "newuser@example.com"
            }
        }
    ]
}
```

## Moving Tests into Templates

One of the most powerful uses of templates is refactoring repeated test patterns. Here's a step-by-step guide for converting inline test logic into reusable templates.

### Step 1: Identify Repetitive Patterns

Look for test sequences that appear multiple times with slight variations:

```json
// Repeated pattern in multiple tests
{
    "name": "Test User Login - Admin",
    "steps": [
        {
            "type": "http",
            "method": "POST",
            "url": "https://api.example.com/auth/login",
            "body": {
                "username": "admin@company.com",
                "password": "adminpass123"
            }
        },
        {
            "type": "http", 
            "method": "GET",
            "url": "https://api.example.com/user/profile",
            "headers": {
                "Authorization": "Bearer {{$.this.body.token}}"
            }
        }
    ]
},
{
    "name": "Test User Login - Regular User",
    "steps": [
        {
            "type": "http",
            "method": "POST", 
            "url": "https://api.example.com/auth/login",
            "body": {
                "username": "user@company.com",
                "password": "userpass123"
            }
        },
        {
            "type": "http",
            "method": "GET", 
            "url": "https://api.example.com/user/profile",
            "headers": {
                "Authorization": "Bearer {{$.this.body.token}}"
            }
        }
    ]
}
```

### Step 2: Extract Variables

Identify what changes between the repetitions (these become template parameters):

- `username` varies: `admin@company.com` vs `user@company.com`
- `password` varies: `adminpass123` vs `userpass123`
- URLs are the same
- Logic flow is identical

### Step 3: Create the Template

Move the pattern to a template with parameters for the varying parts:

```json
{
    "components": {
        "templates": [
            {
                "name": "login-and-get-profile",
                "description": "Login with credentials and fetch user profile",
                "steps": [
                    {
                        "type": "http",
                        "method": "POST", 
                        "url": "https://api.example.com/auth/login",
                        "body": {
                            "username": "{{$.params.username}}",
                            "password": "{{$.params.password}}"
                        }
                    },
                    {
                        "type": "http",
                        "method": "GET",
                        "url": "https://api.example.com/user/profile", 
                        "headers": {
                            "Authorization": "Bearer {{$.this.body.token}}"
                        }
                    }
                ]
            }
        ]
    }
}
```

### Step 4: Replace with Template Usage

Replace the original tests with template calls:

```json
{
    "tests": [
        {
            "name": "Test Admin Login",
            "steps": [
                {
                    "type": "use",
                    "template": "login-and-get-profile",
                    "with": {
                        "username": "admin@company.com", 
                        "password": "adminpass123"
                    }
                }
            ]
        },
        {
            "name": "Test User Login",
            "steps": [
                {
                    "type": "use",
                    "template": "login-and-get-profile",
                    "with": {
                        "username": "user@company.com",
                        "password": "userpass123" 
                    }
                }
            ]
        }
    ]
}
```

### Step 5: Add Assertions and Save Results

Enhance the template to be more useful by adding validation and returning useful data:

```json
{
    "name": "login-and-get-profile",
    "description": "Login with credentials and fetch user profile",
    "steps": [
        {
            "type": "http",
            "id": "login",
            "method": "POST",
            "url": "https://api.example.com/auth/login",
            "body": {
                "username": "{{$.params.username}}",
                "password": "{{$.params.password}}"
            },
            "assert": [
                {
                    "op": "equals",
                    "actualValue": "{{$.this.statusCode}}",
                    "expectedValue": 200
                },
                {
                    "op": "exists", 
                    "actualValue": "{{$.this.body.token}}"
                }
            ]
        },
        {
            "type": "http",
            "id": "getProfile",
            "method": "GET",
            "url": "https://api.example.com/user/profile",
            "headers": {
                "Authorization": "Bearer {{$.login.body.token}}"
            },
            "assert": [
                {
                    "op": "equals",
                    "actualValue": "{{$.this.statusCode}}",
                    "expectedValue": 200
                },
                {
                    "op": "exists",
                    "actualValue": "{{$.this.body.user}}"
                }
            ]
        }
    ]
}
```

### Step 6: Save What You Need

Use the `save` property in the calling test to store only the data you actually need:

```json
{
    "type": "use",
    "template": "login-and-get-profile", 
    "with": {
        "username": "admin@company.com",
        "password": "adminpass123"
    },
    "save": {
        "$.globals.adminToken": "{{$.this.login.body.token}}",
        "$.globals.adminUserId": "{{$.this.getProfile.body.user.id}}"
    }
}
```

### Refactoring Best Practices

1. **Save only what you need**: Don't save entire responses if you only need specific fields
2. **Start simple**: Create basic templates first, add complexity gradually
3. **Clear boundaries**: Templates should have one clear responsibility
4. **Predictable interface**: Use consistent parameter names across templates
5. **Document well**: Add descriptions explaining what the template does and returns

## Templates in Templates

Templates can use other templates, creating powerful composition patterns:

```json
{
    "name": "setup-test-user",
    "description": "Create and authenticate a test user",
    "steps": [
        {
            "type": "use",
            "template": "create-user",
            "with": {
                "userData": "{{$.params.userData}}"
            }
        },
        {
            "type": "use", 
            "template": "login-user",
            "with": {
                "username": "{{$.this.user.email}}",
                "password": "{{$.params.userData.password}}"
            }
        }
    ]
}
```

This pattern allows you to build complex workflows from simple, focused templates.

## Troubleshooting Templates

### Common Issues

**Template Not Found:**
```
Error: Template 'user-auth' not found
```
- Check template name spelling
- Verify template file is imported via `using`
- Ensure template is defined in current file

**Parameter Validation Failed:**
```
Error: Required parameter 'username' not provided
```
- Check all required parameters are passed
- Verify parameter names match template definition
- Ensure parameter values exist in context

**Context Variable Not Found:**
```
Error: Variable 'authToken' not found in template
```
- Check parameter names (use direct names, not `$.paramName`)
- Verify step IDs within template
- Check output mapping syntax

### Debugging Templates

1. **Test templates in isolation** before complex workflows
2. **Add debug outputs** to see intermediate values
3. **Use meaningful step IDs** for easier debugging
4. **Validate parameters** at template boundaries

## Next Steps

- [Use Step](steps/use-step.md) - Learn how to use templates in tests
- [Best Practices](06-best-practices.md) - Template design patterns
- [Extensibility](extensibility.md) - Creating custom template functionality