# Use Step (Template Usage)

The use step allows you to execute reusable templates with parameters. Templates are powerful for creating common test patterns that can be shared across multiple tests.

## Basic Usage

```json
{
    "type": "use",
    "template": "authenticate",
    "params": {
        "username": "{{$.env.testUser}}",
        "password": "{{$.env.testPassword}}"
    }
}
```

## Required Properties

### `type`
Must be `"use"` to identify this as a template usage step.

### `template`
The name of the template to execute. This must match a template defined in:
- The current test file's `components.templates` section
- An imported template file via the `using` property

```json
{
    "type": "use",
    "template": "create-order-workflow"
}
```

## Optional Properties

### `params`
Parameters to pass to the template. These become available within the template as variables:

```json
{
    "type": "use",
    "template": "authenticate",
    "params": {
        "username": "{{$.globals.currentUser.email}}",
        "password": "{{$.globals.currentUser.password}}",
        "loginUrl": "{{$.env.baseUrl}}/auth/login"
    }
}
```

## Template Definition

Templates are defined in the `components.templates` section:

```json
{
    "version": "1.0",
    "components": {
        "templates": [
            {
                "name": "authenticate",
                "description": "Login and get authentication token",
                "params": {
                    "username": { "type": "string", "required": true },
                    "password": { "type": "string", "required": true },
                    "loginUrl": { "type": "string", "required": false, "default": "/auth/login" }
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
                    "token": "{{$.this.body.accessToken}}",
                    "userId": "{{$.this.body.user.id}}",
                    "authHeader": "Bearer {{$.this.body.accessToken}}"
                }
            }
        ]
    }
}
```

## Template Context Isolation

Templates execute in their own isolated context:

1. **Isolated Execution**: Template creates its own context separate from the parent test
2. **Parameter Access**: Template parameters are available as `{{paramName}}` within the template
3. **Step Execution**: Template steps run in the isolated context
4. **Output Mapping**: Only defined outputs are exposed to the parent context
5. **Parent Preservation**: Parent context remains unchanged except for outputs

### Example of Context Isolation

```json
{
    "version": "1.0",
    "globals": {
        "parentVariable": "parent-value"
    },
    "components": {
        "templates": [
            {
                "name": "isolated-template",
                "params": {
                    "inputValue": { "type": "string", "required": true }
                },
                "steps": [
                    {
                        "type": "http",
                        "method": "GET",
                        "url": "/api/data",
                        "save": {
                            "$.templateVariable": "{{inputValue}}-processed"
                        }
                    }
                ],
                "output": {
                    "result": "{{$.templateVariable}}"
                }
            }
        ]
    },
    "tests": [
        {
            "name": "Context Isolation Test",
            "steps": [
                {
                    "type": "use",
                    "template": "isolated-template",
                    "params": {
                        "inputValue": "test-data"
                    }
                }
            ]
        }
    ]
}
```

After execution:
- Parent context still has `{{$.globals.parentVariable}}` = "parent-value"
- Parent context does NOT have `{{$.globals.templateVariable}}`
- Parent context has `{{$.this.result}}` = "test-data-processed"

## Accessing Template Outputs

Template outputs are available via the standard `{{$.this.outputKey}}` pattern:

```json
{
    "tests": [
        {
            "name": "Authentication Flow",
            "steps": [
                {
                    "type": "use",
                    "id": "login",
                    "template": "authenticate",
                    "params": {
                        "username": "{{$.env.username}}",
                        "password": "{{$.env.password}}"
                    }
                },
                {
                    "type": "http",
                    "method": "GET",
                    "url": "/api/profile",
                    "headers": {
                        "Authorization": "{{$.login.authHeader}}"
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
    "name": "oauth-login",
    "description": "OAuth 2.0 authentication flow",
    "params": {
        "clientId": { "type": "string", "required": true },
        "clientSecret": { "type": "string", "required": true },
        "username": { "type": "string", "required": true },
        "password": { "type": "string", "required": true },
        "tokenUrl": { "type": "string", "required": true }
    },
    "steps": [
        {
            "type": "http",
            "method": "POST",
            "url": "{{tokenUrl}}",
            "headers": {
                "Content-Type": "application/x-www-form-urlencoded"
            },
            "body": "grant_type=password&client_id={{clientId}}&client_secret={{clientSecret}}&username={{username}}&password={{password}}",
            "assert": [
                {
                    "op": "equals",
                    "actualValue": "{{$.this.statusCode}}",
                    "expectedValue": 200
                }
            ]
        }
    ],
    "output": {
        "accessToken": "{{$.this.body.access_token}}",
        "refreshToken": "{{$.this.body.refresh_token}}",
        "tokenType": "{{$.this.body.token_type}}",
        "authHeader": "{{$.this.body.token_type}} {{$.this.body.access_token}}"
    }
}
```

### CRUD Operation Template

```json
{
    "name": "create-and-verify-user",
    "description": "Create a user and verify the creation",
    "params": {
        "userData": { "type": "object", "required": true },
        "authToken": { "type": "string", "required": true }
    },
    "steps": [
        {
            "type": "http",
            "id": "createUser",
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
                }
            ]
        },
        {
            "type": "http",
            "method": "GET",
            "url": "/api/users/{{$.createUser.body.id}}",
            "headers": {
                "Authorization": "Bearer {{authToken}}"
            },
            "assert": [
                {
                    "op": "equals",
                    "actualValue": "{{$.this.body.name}}",
                    "expectedValue": "{{userData.name}}"
                }
            ]
        }
    ],
    "output": {
        "userId": "{{$.createUser.body.id}}",
        "createdUser": "{{$.createUser.body}}",
        "verificationResult": "{{$.this.body}}"
    }
}
```

### Data Setup Template

```json
{
    "name": "setup-test-data",
    "description": "Create necessary test data for complex scenarios",
    "params": {
        "organizationName": { "type": "string", "required": true },
        "adminToken": { "type": "string", "required": true }
    },
    "steps": [
        {
            "type": "http",
            "id": "createOrg",
            "method": "POST",
            "url": "/api/organizations",
            "headers": {
                "Authorization": "Bearer {{adminToken}}"
            },
            "body": {
                "name": "{{organizationName}}",
                "type": "test"
            }
        },
        {
            "type": "http",
            "id": "createUser",
            "method": "POST",
            "url": "/api/users",
            "headers": {
                "Authorization": "Bearer {{adminToken}}"
            },
            "body": {
                "email": "test@{{organizationName}}.com",
                "name": "Test User",
                "organizationId": "{{$.createOrg.body.id}}"
            }
        },
        {
            "type": "http",
            "id": "createProject",
            "method": "POST",
            "url": "/api/projects",
            "headers": {
                "Authorization": "Bearer {{adminToken}}"
            },
            "body": {
                "name": "Test Project",
                "organizationId": "{{$.createOrg.body.id}}",
                "ownerId": "{{$.createUser.body.id}}"
            }
        }
    ],
    "output": {
        "organizationId": "{{$.createOrg.body.id}}",
        "userId": "{{$.createUser.body.id}}",
        "projectId": "{{$.createProject.body.id}}",
        "setupComplete": true
    }
}
```

## Using Templates from External Files

### Template File Structure

Create a separate file for templates (e.g., `auth-templates.json`):

```json
{
    "version": "1.0",
    "components": {
        "templates": [
            {
                "name": "basic-auth",
                "params": {
                    "username": { "type": "string", "required": true },
                    "password": { "type": "string", "required": true }
                },
                "steps": [...],
                "output": {...}
            },
            {
                "name": "token-refresh",
                "params": {
                    "refreshToken": { "type": "string", "required": true }
                },
                "steps": [...],
                "output": {...}
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
        "./templates/user-templates.json"
    ],
    "tests": [
        {
            "name": "Test with External Templates",
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

## Error Handling in Templates

### Template-Level Assertions

```json
{
    "name": "robust-api-call",
    "params": {
        "endpoint": { "type": "string", "required": true },
        "authToken": { "type": "string", "required": true }
    },
    "steps": [
        {
            "type": "http",
            "method": "GET",
            "url": "{{endpoint}}",
            "headers": {
                "Authorization": "Bearer {{authToken}}"
            },
            "assert": [
                {
                    "op": "in",
                    "actualValue": "{{$.this.statusCode}}",
                    "expectedValue": [200, 201, 204]
                }
            ]
        }
    ],
    "output": {
        "success": true,
        "data": "{{$.this.body}}",
        "statusCode": "{{$.this.statusCode}}"
    }
}
```

### Conditional Output

```json
{
    "name": "safe-user-lookup",
    "params": {
        "userId": { "type": "string", "required": true }
    },
    "steps": [
        {
            "type": "http",
            "method": "GET",
            "url": "/api/users/{{userId}}",
            "assert": [
                {
                    "op": "in",
                    "actualValue": "{{$.this.statusCode}}",
                    "expectedValue": [200, 404]
                }
            ]
        }
    ],
    "output": {
        "found": "{{$.this.statusCode == 200}}",
        "user": "{{$.this.statusCode == 200 ? $.this.body : null}}",
        "statusCode": "{{$.this.statusCode}}"
    }
}
```

## Best Practices

### Template Design
1. **Single Responsibility**: Each template should have one clear purpose
2. **Parameter Validation**: Define required parameters and types
3. **Clear Outputs**: Only expose necessary data via output mapping
4. **Documentation**: Include descriptive names and descriptions

### Parameter Management
```json
// Good: Clear parameter definition
{
    "params": {
        "apiEndpoint": { "type": "string", "required": true },
        "timeout": { "type": "number", "required": false, "default": 30000 },
        "retryCount": { "type": "number", "required": false, "default": 3 }
    }
}

// Avoid: Unclear or missing parameter definitions
{
    "params": {
        "data": { "type": "object" }
    }
}
```

### Output Organization
```json
// Good: Structured, meaningful outputs
{
    "output": {
        "authToken": "{{$.login.body.token}}",
        "userId": "{{$.login.body.user.id}}",
        "expiresAt": "{{$.login.body.expiresAt}}",
        "loginSuccess": true
    }
}

// Avoid: Exposing internal details
{
    "output": {
        "rawResponse": "{{$.login}}"
    }
}
```

### File Organization
```
templates/
├── auth/
│   ├── oauth-templates.json
│   └── basic-auth-templates.json
├── crud/
│   ├── user-templates.json
│   └── order-templates.json
└── utilities/
    ├── data-validation-templates.json
    └── cleanup-templates.json
```

## Troubleshooting

### Common Issues

**Template Not Found:**
```
Error: Template 'authenticate' not found
```
- Check template name spelling
- Verify template is defined or properly imported
- Check `using` array for correct file paths

**Parameter Missing:**
```
Error: Required parameter 'username' not provided
```
- Ensure all required parameters are passed
- Check parameter names match template definition
- Verify parameter values are available in context

**Context Access Issues:**
- Remember template parameters use `{{paramName}}` not `{{$.paramName}}`
- Step results within templates use standard `{{$.this}}` pattern
- Template outputs use `{{$.this.outputKey}}` in parent context

### Debugging Templates

1. **Add debug outputs** to see intermediate values:
```json
{
    "output": {
        "debugData": "{{$.this.body}}",
        "actualResult": "{{$.processedData}}"
    }
}
```

2. **Test templates individually** before using in complex flows

3. **Use meaningful step IDs** within templates for easier debugging

## Next Steps

- [Templates](../templates.md) - Deep dive into template system
- [HTTP Step](http-step.md) - Learn about HTTP requests within templates
- [Best Practices](../best-practices.md) - Template design patterns