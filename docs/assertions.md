# Assertions

Assertions are the core of test validation in JTest. They allow you to verify that your API responses and data meet your expectations. This comprehensive guide covers all assertion operations and patterns.

## Assertion Structure

All assertions follow this basic structure:

```json
{
    "op": "operation-name",
    "actualValue": "{{$.this.body.field}}",
    "expectedValue": "expected-result"
}
```

### Required Properties

- **`op`**: The assertion operation to perform
- **`actualValue`**: The value to test (usually from context variables)

### Optional Properties

- **`expectedValue`**: The expected value for comparison operations
- **`message`**: Custom error message for failed assertions

## Assertion Operations Reference

### Equality Operations

#### `equals`
Exact equality comparison:

```json
{
    "op": "equals",
    "actualValue": "{{$.this.statusCode}}",
    "expectedValue": 200
}
```

**Examples:**
```json
// String equality
{
    "op": "equals",
    "actualValue": "{{$.this.body.status}}",
    "expectedValue": "success"
}

// Number equality
{
    "op": "equals",
    "actualValue": "{{$.this.body.count}}",
    "expectedValue": 42
}

// Boolean equality
{
    "op": "equals",
    "actualValue": "{{$.this.body.isActive}}",
    "expectedValue": true
}

// Object equality
{
    "op": "equals",
    "actualValue": "{{$.this.body.user}}",
    "expectedValue": {
        "id": "123",
        "name": "John Doe"
    }
}
```

#### `not_equals`
Inequality comparison:

```json
{
    "op": "not_equals",
    "actualValue": "{{$.this.body.error}}",
    "expectedValue": null
}
```

### Existence Operations

#### `exists`
Verifies a value exists (is not null, undefined, or empty):

```json
{
    "op": "exists",
    "actualValue": "{{$.this.body.user.id}}"
}
```

**Use Cases:**
```json
// Check required field exists
{
    "op": "exists",
    "actualValue": "{{$.this.body.data.userId}}"
}

// Verify nested object property
{
    "op": "exists",
    "actualValue": "{{$.this.body.user.profile.avatar}}"
}

// Check array has elements
{
    "op": "exists",
    "actualValue": "{{$.this.body.items[0]}}"
}
```

#### `not_exists`
Verifies a value does not exist:

```json
{
    "op": "not_exists",
    "actualValue": "{{$.this.body.error}}"
}
```

### Numeric Comparisons

#### `greater_than`
Numeric greater than comparison:

```json
{
    "op": "greater_than",
    "actualValue": "{{$.this.body.price}}",
    "expectedValue": 0
}
```

#### `less_than`
Numeric less than comparison:

```json
{
    "op": "less_than",
    "actualValue": "{{$.this.body.responseTime}}",
    "expectedValue": 1000
}
```

#### `greater_than_or_equal`
Greater than or equal comparison:

```json
{
    "op": "greater_than_or_equal",
    "actualValue": "{{$.this.body.items.length}}",
    "expectedValue": 1
}
```

#### `less_than_or_equal`
Less than or equal comparison:

```json
{
    "op": "less_than_or_equal",
    "actualValue": "{{$.this.body.age}}",
    "expectedValue": 120
}
```

### String Operations

#### `contains`
String substring check:

```json
{
    "op": "contains",
    "actualValue": "{{$.this.body.message}}",
    "expectedValue": "success"
}
```

**Examples:**
```json
// Check error message contains specific text
{
    "op": "contains",
    "actualValue": "{{$.this.body.error.message}}",
    "expectedValue": "validation failed"
}

// Verify URL contains domain
{
    "op": "contains",
    "actualValue": "{{$.this.body.redirectUrl}}",
    "expectedValue": "example.com"
}

// Check email format
{
    "op": "contains",
    "actualValue": "{{$.this.body.user.email}}",
    "expectedValue": "@"
}
```

#### `not_contains`
String does not contain substring:

```json
{
    "op": "not_contains",
    "actualValue": "{{$.this.body.description}}",
    "expectedValue": "deprecated"
}
```

#### `starts_with`
String starts with prefix:

```json
{
    "op": "starts_with",
    "actualValue": "{{$.this.body.id}}",
    "expectedValue": "user_"
}
```

#### `ends_with`
String ends with suffix:

```json
{
    "op": "ends_with",
    "actualValue": "{{$.this.body.filename}}",
    "expectedValue": ".json"
}
```

#### `matches`
Regular expression matching:

```json
{
    "op": "matches",
    "actualValue": "{{$.this.body.email}}",
    "expectedValue": "^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\\.[a-zA-Z]{2,}$"
}
```

**Common Regex Patterns:**
```json
// Email validation
{
    "op": "matches",
    "actualValue": "{{$.this.body.email}}",
    "expectedValue": "^[^@]+@[^@]+\\.[^@]+$"
}

// Phone number (international format)
{
    "op": "matches",
    "actualValue": "{{$.this.body.phone}}",
    "expectedValue": "^\\+?[1-9]\\d{1,14}$"
}

// UUID format
{
    "op": "matches",
    "actualValue": "{{$.this.body.uuid}}",
    "expectedValue": "^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$"
}

// ISO date format
{
    "op": "matches",
    "actualValue": "{{$.this.body.createdAt}}",
    "expectedValue": "^\\d{4}-\\d{2}-\\d{2}T\\d{2}:\\d{2}:\\d{2}"
}
```

### Collection Operations

#### `in`
Value exists in array or string:

```json
{
    "op": "in",
    "actualValue": "{{$.this.statusCode}}",
    "expectedValue": [200, 201, 202]
}
```

**Examples:**
```json
// Status code in success range
{
    "op": "in",
    "actualValue": "{{$.this.statusCode}}",
    "expectedValue": [200, 201, 202, 204]
}

// User role is valid
{
    "op": "in",
    "actualValue": "{{$.this.body.user.role}}",
    "expectedValue": ["admin", "user", "moderator"]
}

// Check substring in string
{
    "op": "in",
    "actualValue": "admin",
    "expectedValue": "{{$.this.body.user.permissions}}"
}
```

#### `not_in`
Value does not exist in array:

```json
{
    "op": "not_in",
    "actualValue": "{{$.this.body.status}}",
    "expectedValue": ["error", "failed", "disabled"]
}
```

### Type Validation

#### `is_type`
Validates the data type:

```json
{
    "op": "is_type",
    "actualValue": "{{$.this.body.count}}",
    "expectedValue": "number"
}
```

**Supported Types:**
- `string`
- `number`
- `boolean`
- `object`
- `array`
- `null`

**Examples:**
```json
// Validate response structure types
{
    "assert": [
        {
            "op": "is_type",
            "actualValue": "{{$.this.body.user}}",
            "expectedValue": "object"
        },
        {
            "op": "is_type",
            "actualValue": "{{$.this.body.items}}",
            "expectedValue": "array"
        },
        {
            "op": "is_type",
            "actualValue": "{{$.this.body.count}}",
            "expectedValue": "number"
        },
        {
            "op": "is_type",
            "actualValue": "{{$.this.body.isActive}}",
            "expectedValue": "boolean"
        }
    ]
}
```

## Advanced Assertion Patterns

### Complex Object Validation

```json
{
    "assert": [
        {
            "op": "exists",
            "actualValue": "{{$.this.body.user}}"
        },
        {
            "op": "is_type",
            "actualValue": "{{$.this.body.user}}",
            "expectedValue": "object"
        },
        {
            "op": "exists",
            "actualValue": "{{$.this.body.user.id}}"
        },
        {
            "op": "is_type",
            "actualValue": "{{$.this.body.user.id}}",
            "expectedValue": "string"
        },
        {
            "op": "matches",
            "actualValue": "{{$.this.body.user.id}}",
            "expectedValue": "^user_[0-9a-f]{8}$"
        },
        {
            "op": "exists",
            "actualValue": "{{$.this.body.user.email}}"
        },
        {
            "op": "contains",
            "actualValue": "{{$.this.body.user.email}}",
            "expectedValue": "@"
        }
    ]
}
```

### Array Validation

```json
{
    "assert": [
        {
            "op": "exists",
            "actualValue": "{{$.this.body.items}}"
        },
        {
            "op": "is_type",
            "actualValue": "{{$.this.body.items}}",
            "expectedValue": "array"
        },
        {
            "op": "greater_than",
            "actualValue": "{{$.this.body.items.length}}",
            "expectedValue": 0
        },
        {
            "op": "exists",
            "actualValue": "{{$.this.body.items[0].id}}"
        },
        {
            "op": "is_type",
            "actualValue": "{{$.this.body.items[0].id}}",
            "expectedValue": "string"
        }
    ]
}
```

### Cross-Field Validation

```json
{
    "assert": [
        {
            "op": "exists",
            "actualValue": "{{$.this.body.startDate}}"
        },
        {
            "op": "exists",
            "actualValue": "{{$.this.body.endDate}}"
        },
        {
            "op": "greater_than",
            "actualValue": "{{$.this.body.endDate}}",
            "expectedValue": "{{$.this.body.startDate}}"
        }
    ]
}
```

### Conditional Assertions

```json
{
    "assert": [
        {
            "op": "exists",
            "actualValue": "{{$.this.body.user.type}}"
        },
        {
            "op": "in",
            "actualValue": "{{$.this.body.user.type}}",
            "expectedValue": ["premium", "basic"]
        }
    ]
}
```

## Error Response Validation

### Standard Error Format

```json
{
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
            "op": "exists",
            "actualValue": "{{$.this.body.error.message}}"
        },
        {
            "op": "is_type",
            "actualValue": "{{$.this.body.error.message}}",
            "expectedValue": "string"
        }
    ]
}
```

### Validation Error Details

```json
{
    "assert": [
        {
            "op": "equals",
            "actualValue": "{{$.this.statusCode}}",
            "expectedValue": 422
        },
        {
            "op": "exists",
            "actualValue": "{{$.this.body.errors}}"
        },
        {
            "op": "is_type",
            "actualValue": "{{$.this.body.errors}}",
            "expectedValue": "array"
        },
        {
            "op": "greater_than",
            "actualValue": "{{$.this.body.errors.length}}",
            "expectedValue": 0
        },
        {
            "op": "exists",
            "actualValue": "{{$.this.body.errors[0].field}}"
        },
        {
            "op": "exists",
            "actualValue": "{{$.this.body.errors[0].message}}"
        }
    ]
}
```

## Performance Assertions

### Response Time Validation

```json
{
    "assert": [
        {
            "op": "less_than",
            "actualValue": "{{$.this.responseTime}}",
            "expectedValue": 1000
        }
    ]
}
```

### Size Constraints

```json
{
    "assert": [
        {
            "op": "less_than_or_equal",
            "actualValue": "{{$.this.body.items.length}}",
            "expectedValue": 100
        },
        {
            "op": "greater_than",
            "actualValue": "{{$.this.body.data.length}}",
            "expectedValue": 0
        }
    ]
}
```

## Custom Error Messages

Add descriptive error messages to assertions:

```json
{
    "assert": [
        {
            "op": "equals",
            "actualValue": "{{$.this.statusCode}}",
            "expectedValue": 200,
            "message": "Expected successful response status code"
        },
        {
            "op": "exists",
            "actualValue": "{{$.this.body.user.id}}",
            "message": "User ID must be present in response"
        },
        {
            "op": "matches",
            "actualValue": "{{$.this.body.user.email}}",
            "expectedValue": "^[^@]+@[^@]+\\.[^@]+$",
            "message": "User email must be in valid email format"
        }
    ]
}
```

## Assertion Best Practices

### Start with Basic Validations

```json
// Always check status code first
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

### Validate Data Structure Before Content

```json
{
    "assert": [
        // First: Check structure exists
        {
            "op": "exists",
            "actualValue": "{{$.this.body.user}}"
        },
        {
            "op": "is_type",
            "actualValue": "{{$.this.body.user}}",
            "expectedValue": "object"
        },
        // Then: Check specific content
        {
            "op": "equals",
            "actualValue": "{{$.this.body.user.name}}",
            "expectedValue": "John Doe"
        }
    ]
}
```

### Use Specific Assertions

```json
// Good: Specific checks
{
    "assert": [
        {
            "op": "matches",
            "actualValue": "{{$.this.body.email}}",
            "expectedValue": "^[^@]+@[^@]+\\.[^@]+$"
        }
    ]
}

// Avoid: Vague checks
{
    "assert": [
        {
            "op": "contains",
            "actualValue": "{{$.this.body.email}}",
            "expectedValue": "@"
        }
    ]
}
```

### Group Related Assertions

```json
// Good: Logical grouping in one step
{
    "type": "assert",
    "description": "Validate user profile structure",
    "assert": [
        {"op": "exists", "actualValue": "{{$.this.body.user.id}}"},
        {"op": "exists", "actualValue": "{{$.this.body.user.name}}"},
        {"op": "exists", "actualValue": "{{$.this.body.user.email}}"}
    ]
}

// Separate: Authentication validation
{
    "type": "assert", 
    "description": "Validate authentication state",
    "assert": [
        {"op": "exists", "actualValue": "{{$.globals.authToken}}"}
    ]
}
```

### Assert Early and Often

```json
{
    "tests": [
        {
            "name": "Multi-step flow with validation",
            "steps": [
                {
                    "type": "http",
                    "method": "POST",
                    "url": "/api/users",
                    "assert": [
                        {"op": "equals", "actualValue": "{{$.this.statusCode}}", "expectedValue": 201}
                    ]
                },
                {
                    "type": "assert",
                    "description": "Validate created user",
                    "assert": [
                        {"op": "exists", "actualValue": "{{$.this.body.id}}"}
                    ]
                },
                {
                    "type": "http",
                    "method": "GET",
                    "url": "/api/users/{{$.this.body.id}}",
                    "assert": [
                        {"op": "equals", "actualValue": "{{$.this.statusCode}}", "expectedValue": 200}
                    ]
                }
            ]
        }
    ]
}
```

## Common Assertion Patterns

### API Response Validation

```json
{
    "assert": [
        {
            "op": "in",
            "actualValue": "{{$.this.statusCode}}",
            "expectedValue": [200, 201]
        },
        {
            "op": "exists",
            "actualValue": "{{$.this.headers['content-type']}}"
        },
        {
            "op": "contains",
            "actualValue": "{{$.this.headers['content-type']}}",
            "expectedValue": "application/json"
        },
        {
            "op": "exists",
            "actualValue": "{{$.this.body}}"
        },
        {
            "op": "is_type",
            "actualValue": "{{$.this.body}}",
            "expectedValue": "object"
        }
    ]
}
```

### Pagination Validation

```json
{
    "assert": [
        {
            "op": "exists",
            "actualValue": "{{$.this.body.data}}"
        },
        {
            "op": "is_type",
            "actualValue": "{{$.this.body.data}}",
            "expectedValue": "array"
        },
        {
            "op": "exists",
            "actualValue": "{{$.this.body.pagination}}"
        },
        {
            "op": "exists",
            "actualValue": "{{$.this.body.pagination.page}}"
        },
        {
            "op": "exists",
            "actualValue": "{{$.this.body.pagination.total}}"
        },
        {
            "op": "greater_than_or_equal",
            "actualValue": "{{$.this.body.pagination.total}}",
            "expectedValue": "{{$.this.body.data.length}}"
        }
    ]
}
```

### Business Logic Validation

```json
{
    "assert": [
        {
            "op": "exists",
            "actualValue": "{{$.this.body.order}}"
        },
        {
            "op": "exists",
            "actualValue": "{{$.this.body.order.items}}"
        },
        {
            "op": "greater_than",
            "actualValue": "{{$.this.body.order.items.length}}",
            "expectedValue": 0
        },
        {
            "op": "greater_than",
            "actualValue": "{{$.this.body.order.total}}",
            "expectedValue": 0
        },
        {
            "op": "equals",
            "actualValue": "{{$.this.body.order.currency}}",
            "expectedValue": "USD"
        }
    ]
}
```

## Troubleshooting Assertions

### Common Issues

**JSONPath Resolution Errors:**
```
Error: Cannot resolve path $.this.body.user.id
```
- Verify the HTTP request succeeded
- Check response structure with debug output
- Ensure field names match exactly (case-sensitive)

**Type Comparison Errors:**
```
Error: Cannot compare string with number
```
- Use appropriate assertion operations for data types
- Check actual data types with `is_type` first
- Convert types if necessary

**Regular Expression Errors:**
```
Error: Invalid regular expression
```
- Escape special characters properly
- Test regex patterns separately
- Use online regex validators

### Debugging Techniques

#### Add Debug Assertions

```json
{
    "assert": [
        {
            "op": "debug",
            "actualValue": "{{$.this.body}}",
            "message": "Full response body for debugging"
        },
        {
            "op": "debug", 
            "actualValue": "{{$.this.statusCode}}",
            "message": "Response status code"
        }
    ]
}
```

#### Incremental Validation

```json
// Start simple
{
    "assert": [
        {"op": "exists", "actualValue": "{{$.this.body}}"}
    ]
}

// Add structure checks
{
    "assert": [
        {"op": "exists", "actualValue": "{{$.this.body}}"},
        {"op": "is_type", "actualValue": "{{$.this.body}}", "expectedValue": "object"}
    ]
}

// Add specific field checks
{
    "assert": [
        {"op": "exists", "actualValue": "{{$.this.body}}"},
        {"op": "is_type", "actualValue": "{{$.this.body}}", "expectedValue": "object"},
        {"op": "exists", "actualValue": "{{$.this.body.user}}"}
    ]
}
```

## Next Steps

- [Assert Step](steps/assert-step.md) - Learn about standalone assertion steps
- [HTTP Step](steps/http-step.md) - Using assertions with HTTP requests
- [Best Practices](best-practices.md) - Testing and assertion patterns