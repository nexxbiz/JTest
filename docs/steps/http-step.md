# HTTP Step

The HTTP step is the most commonly used step type in JTest. It makes HTTP requests and captures the response for use in assertions and subsequent steps.

## Basic Usage

```json
{
    "type": "http",
    "method": "GET",
    "url": "https://api.example.com/users/123"
}
```

## Required Properties

### `type`
Must be `"http"` to identify this as an HTTP step.

### `method`
HTTP method to use. Supported methods:
- `GET` - Retrieve data
- `POST` - Create or submit data
- `PUT` - Update/replace data
- `PATCH` - Partially update data
- `DELETE` - Remove data
- `HEAD` - Get headers only
- `OPTIONS` - Get allowed methods

### `url`
The endpoint URL to request. Can include variables:

```json
{
    "type": "http",
    "method": "GET",
    "url": "{{$.env.baseUrl}}/users/{{$.globals.userId}}"
}
```

## Optional Properties

### `headers`
HTTP headers to include with the request:

```json
{
    "type": "http",
    "method": "POST",
    "url": "/api/users",
    "headers": {
        "Content-Type": "application/json",
        "Authorization": "Bearer {{$.globals.authToken}}",
        "X-Request-ID": "{{$.globals.requestId}}"
    }
}
```

### `body`
Request body for POST, PUT, and PATCH requests:

#### JSON Body
```json
{
    "type": "http",
    "method": "POST",
    "url": "/api/users",
    "headers": {
        "Content-Type": "application/json"
    },
    "body": {
        "name": "{{$.globals.testUser.name}}",
        "email": "{{$.globals.testUser.email}}",
        "age": 25
    }
}
```

#### String Body
```json
{
    "type": "http",
    "method": "POST",
    "url": "/api/data",
    "headers": {
        "Content-Type": "text/plain"
    },
    "body": "Raw text data here"
}
```

#### Form Data
```json
{
    "type": "http",
    "method": "POST",
    "url": "/api/upload",
    "headers": {
        "Content-Type": "application/x-www-form-urlencoded"
    },
    "body": "name=John&email=john@example.com&age=30"
}
```

### `query`
Query parameters to append to the URL:

```json
{
    "type": "http",
    "method": "GET",
    "url": "/api/users",
    "query": {
        "page": "{{$.globals.currentPage}}",
        "limit": 10,
        "sort": "name",
        "filter": "active"
    }
}
```
This becomes: `/api/users?page=1&limit=10&sort=name&filter=active`

### `timeout`
Request timeout in milliseconds (default: 30000):

```json
{
    "type": "http",
    "method": "GET",
    "url": "/api/slow-endpoint",
    "timeout": 60000
}
```

## Response Data

HTTP steps automatically populate the context with response information:

```json
{
    "statusCode": 200,
    "headers": {
        "content-type": "application/json",
        "x-response-time": "150ms"
    },
    "body": {
        // Parsed JSON response (if Content-Type is application/json)
    },
    "request": {
        "url": "https://api.example.com/users/123",
        "method": "POST", 
        "headers": [
            {"name": "Authorization", "value": "Bearer token"},
            {"name": "Content-Type", "value": "application/json"}
        ],
        "body": "{\"name\":\"John\",\"email\":\"john@example.com\"}"
    }
}
```

### Accessing Response Data

```json
// Status code
"{{$.this.statusCode}}"

// Response headers
"{{$.this.headers['content-type']}}"

// Response body (JSON)
"{{$.this.body.user.id}}"
"{{$.this.body.data[0].name}}"

// Request details (NEW)
"{{$.this.request.url}}"
"{{$.this.request.method}}"
"{{$.this.request.headers[0].name}}"

// From a named step
"{{$.loginStep.body.token}}"
```

### HTTP Request Details in Reports

When using markdown output format, HTTP steps now display detailed request information in a convenient table format:

**HTTP Request:**

| Field   | Value |
|---------|-------|
| URL     | https://api.example.com/users/123 |
| Method  | POST |
| Headers | Authorization: masked<br/>Content-Type: application/json |
| Body    | <details><summary>show JSON</summary>...</details> |

*Note: Sensitive headers like Authorization are automatically masked for security.*

## Common HTTP Patterns

### GET Request with Parameters

```json
{
    "type": "http",
    "id": "getUsers",
    "description": "Fetch paginated user list",
    "method": "GET",
    "url": "{{$.env.apiUrl}}/users",
    "query": {
        "page": 1,
        "limit": 20,
        "sort": "created_at",
        "order": "desc"
    },
    "headers": {
        "Authorization": "Bearer {{$.globals.authToken}}"
    },
    "assert": [
        {
            "op": "equals",
            "actualValue": "{{$.this.statusCode}}",
            "expectedValue": 200
        },
        {
            "op": "exists",
            "actualValue": "{{$.this.body.users}}"
        }
    ]
}
```

### POST Request with JSON Body

```json
{
    "type": "http",
    "id": "createUser",
    "description": "Create a new user account",
    "method": "POST",
    "url": "{{$.env.apiUrl}}/users",
    "headers": {
        "Content-Type": "application/json",
        "Authorization": "Bearer {{$.globals.adminToken}}"
    },
    "body": {
        "name": "{{$.globals.newUser.name}}",
        "email": "{{$.globals.newUser.email}}",
        "role": "user",
        "preferences": {
            "newsletter": true,
            "notifications": false
        }
    },
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
    ],
    "save": {
        "$.globals.createdUserId": "{{$.this.body.id}}"
    }
}
```

### PUT Request for Updates

```json
{
    "type": "http",
    "description": "Update user profile",
    "method": "PUT",
    "url": "{{$.env.apiUrl}}/users/{{$.globals.userId}}",
    "headers": {
        "Content-Type": "application/json",
        "Authorization": "Bearer {{$.globals.userToken}}"
    },
    "body": {
        "name": "Updated Name",
        "email": "updated@example.com"
    },
    "assert": [
        {
            "op": "equals",
            "actualValue": "{{$.this.statusCode}}",
            "expectedValue": 200
        },
        {
            "op": "equals",
            "actualValue": "{{$.this.body.name}}",
            "expectedValue": "Updated Name"
        }
    ]
}
```

### DELETE Request

```json
{
    "type": "http",
    "description": "Delete user account",
    "method": "DELETE",
    "url": "{{$.env.apiUrl}}/users/{{$.globals.userId}}",
    "headers": {
        "Authorization": "Bearer {{$.globals.adminToken}}"
    },
    "assert": [
        {
            "op": "equals",
            "actualValue": "{{$.this.statusCode}}",
            "expectedValue": 204
        }
    ]
}
```

## Authentication Patterns

### Bearer Token Authentication

```json
{
    "type": "http",
    "method": "GET",
    "url": "/api/protected-resource",
    "headers": {
        "Authorization": "Bearer {{$.globals.accessToken}}"
    }
}
```

### API Key Authentication

```json
{
    "type": "http",
    "method": "GET",
    "url": "/api/data",
    "headers": {
        "X-API-Key": "{{$.env.apiKey}}"
    }
}
```

### Basic Authentication

```json
{
    "type": "http",
    "method": "GET",
    "url": "/api/secure",
    "headers": {
        "Authorization": "Basic {{$.env.basicAuthToken}}"
    }
}
```

## Error Handling

### Expected Error Responses

```json
{
    "type": "http",
    "description": "Test validation error handling",
    "method": "POST",
    "url": "/api/users",
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
            "actualValue": "{{$.this.body.error.message}}",
            "expectedValue": "Invalid email format"
        }
    ]
}
```

### Handling Different Status Codes

```json
{
    "type": "http",
    "method": "GET",
    "url": "/api/users/{{$.globals.userId}}",
    "assert": [
        {
            "op": "in",
            "actualValue": "{{$.this.statusCode}}",
            "expectedValue": [200, 404]
        }
    ]
}
```

## Advanced Features

### File Upload Simulation

```json
{
    "type": "http",
    "method": "POST",
    "url": "/api/upload",
    "headers": {
        "Content-Type": "multipart/form-data"
    },
    "body": {
        "file": "@/path/to/file.pdf",
        "description": "Test file upload"
    }
}
```

### Custom User Agent

```json
{
    "type": "http",
    "method": "GET",
    "url": "/api/data",
    "headers": {
        "User-Agent": "JTest/1.0 (API Testing Tool)"
    }
}
```

### Request with Cookies

```json
{
    "type": "http",
    "method": "GET",
    "url": "/api/session-data",
    "headers": {
        "Cookie": "sessionId={{$.globals.sessionId}}; userId={{$.globals.userId}}"
    }
}
```

## Best Practices

### URL Construction
```json
// Good: Use variables for base URLs
{
    "url": "{{$.env.baseUrl}}/users/{{$.globals.userId}}"
}

// Avoid: Hardcoded URLs
{
    "url": "http://localhost:8080/users/123"
}
```

### Header Management
```json
// Good: Use consistent header structure
{
    "headers": {
        "Authorization": "Bearer {{$.globals.authToken}}",
        "Content-Type": "application/json",
        "Accept": "application/json"
    }
}
```

### Body Structure
```json
// Good: Use variables for dynamic content
{
    "body": {
        "user": {
            "name": "{{$.globals.testUser.name}}",
            "email": "{{$.globals.testUser.email}}"
        },
        "metadata": {
            "source": "api-test",
            "timestamp": "{{$.globals.currentTimestamp}}"
        }
    }
}
```

### Error Checking
```json
// Always check status codes
{
    "assert": [
        {
            "op": "equals",
            "actualValue": "{{$.this.statusCode}}",
            "expectedValue": 200
        }
    ]
}
```

## Troubleshooting

### Common Issues

**Connection Errors:**
- Check URL formatting and accessibility
- Verify network connectivity
- Check timeout settings

**Authentication Failures:**
- Verify token/API key is valid
- Check header format
- Ensure proper Authorization header structure

**JSON Parsing Errors:**
- Verify Content-Type header is set correctly
- Check response is valid JSON
- Use string body for non-JSON responses

**Variable Resolution:**
- Ensure variables exist in context
- Check variable scope and naming
- Verify JSONPath expressions are correct

### Debugging Tips

1. **Add debug assertions** to check intermediate values:
```json
{
    "assert": [
        {
            "op": "debug",
            "actualValue": "{{$.this.body}}"
        }
    ]
}
```

2. **Use step IDs** for complex flows to reference specific results

3. **Test with simple requests** first, then add complexity

4. **Check response structure** before writing assertions

## Next Steps

- [Assertions](../05-assertions.md) - Learn how to validate HTTP responses
- [Use Step](use-step.md) - Learn about template reuse
- [Best Practices](../06-best-practices.md) - HTTP testing best practices