# Assert Step

The assert step allows you to perform standalone assertions without making HTTP requests. It's useful for validating data from previous steps or checking computed values.

## Basic Usage

```json
{
    "type": "assert",
    "assert": [
        {
            "op": "equals",
            "actualValue": "{{$.globals.userId}}",
            "expectedValue": "user-123"
        }
    ]
}
```

## Required Properties

### `type`
Must be `"assert"` to identify this as an assertion step.

### `assert`
Array of assertion operations to perform. Each assertion has:
- `op` - The assertion operation (equals, exists, contains, etc.)
- `actualValue` - The value to test (usually a variable expression)
- `expectedValue` - The expected value (for comparison operations)

## Assertion Operations

### Basic Comparisons

#### `equals`
Exact equality check:

```json
{
    "type": "assert",
    "assert": [
        {
            "op": "equals",
            "actualValue": "{{$.this.statusCode}}",
            "expectedValue": 200
        },
        {
            "op": "equals",
            "actualValue": "{{$.this.body.user.name}}",
            "expectedValue": "John Doe"
        }
    ]
}
```

#### `notequals`
Inequality check:

```json
{
    "type": "assert",
    "assert": [
        {
            "op": "notequals",
            "actualValue": "{{$.this.body.status}}",
            "expectedValue": "error"
        }
    ]
}
```

### Existence Checks

#### `exists`
Verify a value exists (is not null/undefined):

```json
{
    "type": "assert",
    "assert": [
        {
            "op": "exists",
            "actualValue": "{{$.this.body.user.id}}"
        },
        {
            "op": "exists",
            "actualValue": "{{$.globals.authToken}}"
        }
    ]
}
```

#### `notexists`
Verify a value does not exist:

```json
{
    "type": "assert",
    "assert": [
        {
            "op": "notexists",
            "actualValue": "{{$.this.body.error}}"
        }
    ]
}
```

### Numeric Comparisons

#### `greaterthan`
Numeric greater than comparison:

```json
{
    "type": "assert",
    "assert": [
        {
            "op": "greaterthan",
            "actualValue": "{{$.this.body.count}}",
            "expectedValue": 0
        },
        {
            "op": "greaterthan",
            "actualValue": "{{$.this.body.price}}",
            "expectedValue": 10.50
        }
    ]
}
```

#### `lessthan`
Numeric less than comparison:

```json
{
    "type": "assert",
    "assert": [
        {
            "op": "lessthan",
            "actualValue": "{{$.this.body.responseTime}}",
            "expectedValue": 1000
        }
    ]
}
```

#### `greaterorequal`
Numeric greater than or equal comparison:

```json
{
    "type": "assert",
    "assert": [
        {
            "op": "greaterorequal",
            "actualValue": "{{$.this.body.items.length}}",
            "expectedValue": 1
        }
    ]
}
```

#### `lessorequal`
Numeric less than or equal comparison:

```json
{
    "type": "assert",
    "assert": [
        {
            "op": "lessorequal",
            "actualValue": "{{$.this.body.age}}",
            "expectedValue": 120
        }
    ]
}
```

### String Operations

#### `contains`
Check if a string contains a substring:

```json
{
    "type": "assert",
    "assert": [
        {
            "op": "contains",
            "actualValue": "{{$.this.body.message}}",
            "expectedValue": "success"
        },
        {
            "op": "contains",
            "actualValue": "{{$.this.body.email}}",
            "expectedValue": "@example.com"
        }
    ]
}
```

#### `notcontains`
Check if a string does not contain a substring:

```json
{
    "type": "assert",
    "assert": [
        {
            "op": "notcontains",
            "actualValue": "{{$.this.body.message}}",
            "expectedValue": "error"
        }
    ]
}
```

#### `startswith`
Check if a string starts with a prefix:

```json
{
    "type": "assert",
    "assert": [
        {
            "op": "startswith",
            "actualValue": "{{$.this.body.id}}",
            "expectedValue": "user_"
        }
    ]
}
```

#### `endswith`
Check if a string ends with a suffix:

```json
{
    "type": "assert",
    "assert": [
        {
            "op": "endswith",
            "actualValue": "{{$.this.body.filename}}",
            "expectedValue": ".json"
        }
    ]
}
```

#### `matches`
Regular expression matching:

```json
{
    "type": "assert",
    "assert": [
        {
            "op": "matches",
            "actualValue": "{{$.this.body.email}}",
            "expectedValue": "^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\\.[a-zA-Z]{2,}$"
        },
        {
            "op": "matches",
            "actualValue": "{{$.this.body.phone}}",
            "expectedValue": "^\\+?[1-9]\\d{1,14}$"
        }
    ]
}
```

### Collection Operations

#### `in`
Check if a value exists in an array:

```json
{
    "type": "assert",
    "assert": [
        {
            "op": "in",
            "actualValue": "{{$.this.statusCode}}",
            "expectedValue": [200, 201, 202]
        },
        {
            "op": "in",
            "actualValue": "{{$.this.body.status}}",
            "expectedValue": ["active", "pending"]
        }
    ]
}
```

### Type Checks

#### `type`
Check the type of a value:

```json
{
    "type": "assert",
    "assert": [
        {
            "op": "type",
            "actualValue": "{{$.this.body.count}}",
            "expectedValue": "number"
        },
        {
            "op": "type",
            "actualValue": "{{$.this.body.user}}",
            "expectedValue": "object"
        },
        {
            "op": "type",
            "actualValue": "{{$.this.body.tags}}",
            "expectedValue": "array"
        }
    ]
}
```

Supported types: `string`, `number`, `boolean`, `object`, `array`, `null`

## Common Assert Step Patterns

### Data Validation After HTTP Request

```json
{
    "tests": [
        {
            "name": "User Profile Validation",
            "steps": [
                {
                    "type": "http",
                    "id": "getProfile",
                    "method": "GET",
                    "url": "/api/user/profile",
                    "headers": {
                        "Authorization": "Bearer {{$.globals.authToken}}"
                    }
                },
                {
                    "type": "assert",
                    "description": "Validate profile data structure",
                    "assert": [
                        {
                            "op": "exists",
                            "actualValue": "{{$.getProfile.body.user.id}}"
                        },
                        {
                            "op": "type",
                            "actualValue": "{{$.getProfile.body.user.id}}",
                            "expectedValue": "string"
                        },
                        {
                            "op": "matches",
                            "actualValue": "{{$.getProfile.body.user.email}}",
                            "expectedValue": "^[^@]+@[^@]+\\.[^@]+$"
                        },
                        {
                            "op": "in",
                            "actualValue": "{{$.getProfile.body.user.status}}",
                            "expectedValue": ["active", "pending", "suspended"]
                        }
                    ]
                }
            ]
        }
    ]
}
```

### Cross-Step Data Validation

```json
{
    "tests": [
        {
            "name": "Order Creation Validation",
            "steps": [
                {
                    "type": "http",
                    "id": "createOrder",
                    "method": "POST",
                    "url": "/api/orders",
                    "body": {
                        "userId": "{{$.globals.userId}}",
                        "items": [{"sku": "ITEM-1", "qty": 2}]
                    }
                },
                {
                    "type": "http",
                    "id": "getOrder",
                    "method": "GET",
                    "url": "/api/orders/{{$.createOrder.body.id}}"
                },
                {
                    "type": "assert",
                    "description": "Validate order consistency",
                    "assert": [
                        {
                            "op": "equals",
                            "actualValue": "{{$.getOrder.body.userId}}",
                            "expectedValue": "{{$.createOrder.body.userId}}"
                        },
                        {
                            "op": "equals",
                            "actualValue": "{{$.getOrder.body.id}}",
                            "expectedValue": "{{$.createOrder.body.id}}"
                        },
                        {
                            "op": "equals",
                            "actualValue": "{{$.getOrder.body.items.length}}",
                            "expectedValue": 1
                        }
                    ]
                }
            ]
        }
    ]
}
```

### Computed Value Validation

```json
{
    "tests": [
        {
            "name": "Price Calculation Validation",
            "steps": [
                {
                    "type": "http",
                    "id": "getCart",
                    "method": "GET",
                    "url": "/api/cart"
                },
                {
                    "type": "assert",
                    "description": "Validate cart totals",
                    "assert": [
                        {
                            "op": "greaterthan",
                            "actualValue": "{{$.getCart.body.subtotal}}",
                            "expectedValue": 0
                        },
                        {
                            "op": "greaterorequal",
                            "actualValue": "{{$.getCart.body.total}}",
                            "expectedValue": "{{$.getCart.body.subtotal}}"
                        },
                        {
                            "op": "equals",
                            "actualValue": "{{$.getCart.body.tax}}",
                            "expectedValue": "{{$.getCart.body.subtotal * 0.08}}"
                        }
                    ]
                }
            ]
        }
    ]
}
```

### Array and Object Validation

```json
{
    "type": "assert",
    "description": "Validate collection properties",
    "assert": [
        {
            "op": "type",
            "actualValue": "{{$.this.body.users}}",
            "expectedValue": "array"
        },
        {
            "op": "greaterthan",
            "actualValue": "{{$.this.body.users.length}}",
            "expectedValue": 0
        },
        {
            "op": "exists",
            "actualValue": "{{$.this.body.users[0].id}}"
        },
        {
            "op": "type",
            "actualValue": "{{$.this.body.users[0].id}}",
            "expectedValue": "string"
        }
    ]
}
```

## Complex Assertion Scenarios

### Conditional Assertions

```json
{
    "type": "assert",
    "description": "Conditional validation based on user type",
    "assert": [
        {
            "op": "exists",
            "actualValue": "{{$.this.body.user.type}}"
        },
        {
            "op": "in",
            "actualValue": "{{$.this.body.user.type}}",
            "expectedValue": ["admin", "user", "guest"]
        }
    ]
}
```

### Multi-Field Validation

```json
{
    "type": "assert",
    "description": "Comprehensive user validation",
    "assert": [
        {
            "op": "exists",
            "actualValue": "{{$.this.body.user.id}}"
        },
        {
            "op": "matches",
            "actualValue": "{{$.this.body.user.id}}",
            "expectedValue": "^user_[a-zA-Z0-9]{8,}$"
        },
        {
            "op": "exists",
            "actualValue": "{{$.this.body.user.email}}"
        },
        {
            "op": "contains",
            "actualValue": "{{$.this.body.user.email}}",
            "expectedValue": "@"
        },
        {
            "op": "exists",
            "actualValue": "{{$.this.body.user.createdAt}}"
        },
        {
            "op": "matches",
            "actualValue": "{{$.this.body.user.createdAt}}",
            "expectedValue": "^\\d{4}-\\d{2}-\\d{2}T\\d{2}:\\d{2}:\\d{2}"
        }
    ]
}
```

## Step-Level Properties

Assert steps support all common step properties:

### With ID for Reference

```json
{
    "type": "assert",
    "id": "userValidation",
    "description": "Validate user data structure",
    "assert": [
        {
            "op": "exists",
            "actualValue": "{{$.this.body.user.id}}"
        }
    ]
}
```

### With Save Operations

```json
{
    "type": "assert",
    "description": "Validate and save validation results",
    "assert": [
        {
            "op": "exists",
            "actualValue": "{{$.this.body.user.id}}"
        }
    ],
    "save": {
        "$.globals.validationPassed": true,
        "$.globals.validatedUserId": "{{$.this.body.user.id}}"
    }
}
```

## Error Handling

Assert steps fail if any assertion fails:

```json
{
    "type": "assert",
    "description": "This step will fail if status is not 200",
    "assert": [
        {
            "op": "equals",
            "actualValue": "{{$.this.statusCode}}",
            "expectedValue": 200
        }
    ]
}
```

When an assertion fails:
- The step is marked as failed
- Subsequent steps in the test may be skipped (depending on configuration)
- Error details are included in test results

## Best Practices

### Meaningful Descriptions
```json
// Good: Clear description of what's being validated
{
    "type": "assert",
    "description": "Validate user profile contains required fields",
    "assert": [...]
}

// Avoid: Generic or missing descriptions
{
    "type": "assert",
    "assert": [...]
}
```

### Logical Grouping
```json
// Good: Group related assertions in one step
{
    "type": "assert",
    "description": "Validate order response structure",
    "assert": [
        {"op": "exists", "actualValue": "{{$.this.body.orderId}}"},
        {"op": "exists", "actualValue": "{{$.this.body.status}}"},
        {"op": "exists", "actualValue": "{{$.this.body.total}}"}
    ]
}

// Consider: Separate unrelated validations
{
    "type": "assert",
    "description": "Validate authentication state",
    "assert": [
        {"op": "exists", "actualValue": "{{$.globals.authToken}}"}
    ]
}
```

### Specific Error Messages
```json
// Good: Use specific expected values
{
    "op": "equals",
    "actualValue": "{{$.this.body.status}}",
    "expectedValue": "active"
}

// Avoid: Vague validations without context
{
    "op": "exists",
    "actualValue": "{{$.this.body.something}}"
}
```

### Type Safety
```json
// Good: Check types before operations
{
    "assert": [
        {
            "op": "type",
            "actualValue": "{{$.this.body.count}}",
            "expectedValue": "number"
        },
        {
            "op": "greaterthan",
            "actualValue": "{{$.this.body.count}}",
            "expectedValue": 0
        }
    ]
}
```

## Troubleshooting

### Common Issues

**Variable Not Found:**
```
Error: Variable $.this.body.user.id not found
```
- Check if previous step succeeded
- Verify JSONPath expression is correct
- Ensure the field exists in the response

**Type Mismatch:**
```
Error: Cannot compare string with number
```
- Use appropriate assertion operations for data types
- Check that actualValue is the expected type
- Consider using `type` assertion first

**Regular Expression Errors:**
```
Error: Invalid regular expression
```
- Escape special characters properly
- Test regex patterns separately
- Use online regex testers for complex patterns

### Debugging Tips

1. **Add debug assertions** to see actual values:
```json
{
    "op": "debug",
    "actualValue": "{{$.this.body}}"
}
```

2. **Check data types** before comparisons:
```json
{
    "op": "type",
    "actualValue": "{{$.this.body.value}}",
    "expectedValue": "number"
}
```

3. **Use simpler assertions** first, then add complexity

4. **Verify variable scope** and timing

## Next Steps

- [HTTP Step](http-step.md) - Learn about making HTTP requests
- [Assertions](../05-assertions.md) - Comprehensive assertion reference
- [Context and Variables](../03-context-and-variables.md) - Understanding variable access