# Troubleshooting

This guide helps you diagnose and resolve common issues when working with JTest.

## Common Issues and Solutions

### Test Execution Issues

#### Test File Not Found

**Error:**
```
Error: Could not find test file 'my-tests.json'
```

**Solutions:**
1. Check file path and spelling:
```bash
# Use absolute path
./src/JTest.Cli/bin/Debug/net8.0/JTest run /full/path/to/tests.json

# Use relative path from current directory
./src/JTest.Cli/bin/Debug/net8.0/JTest run ./tests/my-tests.json

# List files to verify
ls -la tests/
```

2. Verify file permissions:
```bash
# Check file permissions
ls -la my-tests.json

# Fix permissions if needed
chmod 644 my-tests.json
```

#### JSON Syntax Errors

**Error:**
```
Error: JSON parse error at line 15: Unexpected character ','
```

**Solutions:**
1. Use JSON validator:
```bash
# Online tools: jsonlint.com, json.parser.online.fr
# Command line tools:
cat tests.json | jq .  # jq will show syntax errors
```

2. Common JSON issues:
```json
// Wrong: Trailing comma
{
    "name": "test",
    "steps": [],
}

// Correct: No trailing comma
{
    "name": "test", 
    "steps": []
}

// Wrong: Unescaped quotes
{
    "description": "Test "quoted" text"
}

// Correct: Escaped quotes
{
    "description": "Test \"quoted\" text"
}
```

### Variable Resolution Issues

#### Variable Not Found

**Error:**
```
Error: Variable '$.this.body.user.id' not found
```

**Debugging Steps:**

1. **Check if previous step succeeded:**
```json
{
    "type": "http",
    "method": "GET",
    "url": "/api/users/123",
    "assert": [
        {
            "op": "equals",
            "actualValue": "{{$.this.statusCode}}",
            "expectedValue": 200
        }
    ]
}
```

2. **Add debug output to see actual response:**
```json
{
    "type": "assert",
    "description": "Debug: Check response structure",
    "assert": [
        {
            "op": "debug",
            "actualValue": "{{$.this}}",
            "message": "Full response data"
        }
    ]
}
```

3. **Verify JSONPath expression:**
```json
// If response is:
{
    "data": {
        "user": {
            "id": "123"
        }
    }
}

// Use correct path:
"{{$.this.body.data.user.id}}"  // Correct
"{{$.this.body.user.id}}"       // Wrong - missing 'data' level
```

#### Scope Issues

**Error:**
```
Error: Cannot access variable from different scope
```

**Solutions:**

1. **Use proper variable scopes:**
```json
// env: Environment configuration
{
    "env": {
        "apiUrl": "https://api.example.com"
    }
}

// globals: Shared across tests
{
    "globals": {
        "userId": "user-123"
    }
}

// Step results: From previous steps
{
    "save": {
        "$.globals.authToken": "{{$.this.body.token}}"
    }
}
```

2. **Check variable timing:**
```json
{
    "tests": [
        {
            "steps": [
                {
                    "type": "http",
                    "id": "login",
                    "method": "POST",
                    "url": "/auth/login"
                },
                {
                    "type": "http",
                    "method": "GET",
                    "url": "/profile",
                    "headers": {
                        "Authorization": "Bearer {{$.login.body.token}}"
                    }
                }
            ]
        }
    ]
}
```

### HTTP Request Issues

#### Connection Refused

**Error:**
```
Error: Connection refused to https://api.example.com
```

**Solutions:**

1. **Check URL and connectivity:**
```bash
# Test with curl
curl https://api.example.com/health

# Check DNS resolution
nslookup api.example.com

# Test with ping
ping api.example.com
```

2. **Verify environment configuration:**
```json
{
    "env": {
        "baseUrl": "https://api-staging.example.com",  // Check correct environment
        "timeout": 30000
    }
}
```

3. **Check firewall and network settings:**
```bash
# Check if proxy is needed
export HTTP_PROXY=http://proxy.company.com:8080
export HTTPS_PROXY=http://proxy.company.com:8080
```

#### Timeout Issues

**Error:**
```
Error: Request timeout after 30000ms
```

**Solutions:**

1. **Increase timeout:**
```json
{
    "type": "http",
    "method": "GET",
    "url": "/api/slow-endpoint",
    "timeout": 60000
}
```

2. **Set global timeout:**
```bash
./src/JTest.Cli/bin/Debug/net8.0/JTest run tests.json --timeout 120
```

3. **Check API performance:**
```bash
# Measure response time
time curl https://api.example.com/endpoint
```

#### Authentication Failures

**Error:**
```
Error: HTTP 401 Unauthorized
```

**Solutions:**

1. **Verify credentials:**
```json
{
    "env": {
        "apiKey": "${API_KEY}",           // From environment variable
        "username": "user@example.com",
        "password": "${USER_PASSWORD}"
    }
}
```

2. **Check token format:**
```json
{
    "headers": {
        "Authorization": "Bearer {{$.globals.authToken}}",  // Bearer prefix
        "X-API-Key": "{{$.env.apiKey}}"                    // Or API key header
    }
}
```

3. **Debug authentication flow:**
```json
{
    "tests": [
        {
            "name": "Debug Authentication",
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
                    "assert": [
                        {
                            "op": "debug",
                            "actualValue": "{{$.this}}",
                            "message": "Login response"
                        }
                    ]
                }
            ]
        }
    ]
}
```

### Assertion Failures

#### Type Mismatch

**Error:**
```
Error: Cannot compare string with number
```

**Solutions:**

1. **Check data types first:**
```json
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

2. **Use appropriate assertions for data types:**
```json
// For strings
{
    "op": "contains",
    "actualValue": "{{$.this.body.message}}",
    "expectedValue": "success"
}

// For numbers
{
    "op": "greaterthan",
    "actualValue": "{{$.this.body.count}}",
    "expectedValue": 0
}

// For booleans
{
    "op": "equals",
    "actualValue": "{{$.this.body.isActive}}",
    "expectedValue": true
}
```

#### Assertion Logic Errors

**Error:**
```
Assertion failed: Expected 'active', got 'inactive'
```

**Debugging:**

1. **Add intermediate assertions:**
```json
{
    "assert": [
        {
            "op": "exists",
            "actualValue": "{{$.this.body.status}}"
        },
        {
            "op": "debug",
            "actualValue": "{{$.this.body.status}}",
            "message": "Current status value"
        },
        {
            "op": "equals",
            "actualValue": "{{$.this.body.status}}",
            "expectedValue": "active"
        }
    ]
}
```

2. **Check data flow:**
```json
{
    "tests": [
        {
            "name": "Debug Data Flow",
            "steps": [
                {
                    "type": "http",
                    "id": "createUser",
                    "method": "POST",
                    "url": "/api/users",
                    "body": {
                        "name": "Test User",
                        "status": "active"
                    },
                    "assert": [
                        {
                            "op": "debug",
                            "actualValue": "{{$.this.body}}",
                            "message": "User creation response"
                        }
                    ]
                },
                {
                    "type": "http",
                    "method": "GET",
                    "url": "/api/users/{{$.createUser.body.id}}",
                    "assert": [
                        {
                            "op": "debug",
                            "actualValue": "{{$.this.body}}",
                            "message": "User retrieval response"
                        }
                    ]
                }
            ]
        }
    ]
}
```

### Template Issues

#### Template Not Found

**Error:**
```
Error: Template 'authenticate' not found
```

**Solutions:**

1. **Check template name:**
```json
{
    "components": {
        "templates": [
            {
                "name": "authenticate",  // Must match exactly
                "params": {...},
                "steps": [...]
            }
        ]
    }
}
```

2. **Verify template imports:**
```json
{
    "using": [
        "./templates/auth-templates.json"  // Check file path
    ]
}
```

3. **Check file structure:**
```bash
# Verify template file exists
ls -la templates/auth-templates.json

# Check template file content
cat templates/auth-templates.json | jq '.components.templates[].name'
```

#### Parameter Validation Errors

**Error:**
```
Error: Required parameter 'username' not provided
```

**Solutions:**

1. **Check parameter definition:**
```json
{
    "name": "authenticate",
    "params": {
        "username": { "type": "string", "required": true },
        "password": { "type": "string", "required": true }
    }
}
```

2. **Verify parameter usage:**
```json
{
    "type": "use",
    "template": "authenticate",
    "params": {
        "username": "{{$.env.testUser}}",     // Must provide required params
        "password": "{{$.env.testPassword}}"
    }
}
```

### Environment and Configuration Issues

#### Environment Variable Not Set

**Error:**
```
Error: Environment variable 'API_KEY' not found
```

**Solutions:**

1. **Set environment variables:**
```bash
# Linux/macOS
export API_KEY=your-api-key
export BASE_URL=https://api.example.com

# Windows
set API_KEY=your-api-key
set BASE_URL=https://api.example.com
```

2. **Use .env file:**
```
# .env file
API_KEY=your-api-key
BASE_URL=https://api.example.com
TIMEOUT=30000
```

3. **Provide defaults:**
```json
{
    "env": {
        "baseUrl": "${BASE_URL:-http://localhost:8080}",
        "timeout": "${TIMEOUT:-30000}"
    }
}
```

#### Configuration File Issues

**Error:**
```
Error: Configuration file 'jtest.config.json' is invalid
```

**Solutions:**

1. **Validate configuration syntax:**
```bash
# Check JSON syntax
cat jtest.config.json | jq .
```

2. **Example valid configuration:**
```json
{
    "defaultTimeout": 30000,
    "maxParallelTests": 4,
    "environments": {
        "dev": "./environments/development.json",
        "staging": "./environments/staging.json"
    }
}
```

## Debugging Techniques

### Enable Debug Logging

1. **CLI debug flags:**
```bash
# Verbose output
./src/JTest.Cli/bin/Debug/net8.0/JTest run tests.json --verbose

# Debug mode
./src/JTest.Cli/bin/Debug/net8.0/JTest run tests.json --debug

# Maximum detail
./src/JTest.Cli/bin/Debug/net8.0/JTest run tests.json --debug --verbose
```

2. **Environment variable:**
```bash
export JTEST_LOG_LEVEL=debug
./src/JTest.Cli/bin/Debug/net8.0/JTest run tests.json
```

### Debug Output Strategies

1. **Add debug assertions:**
```json
{
    "type": "assert",
    "description": "Debug checkpoint",
    "assert": [
        {
            "op": "debug",
            "actualValue": "{{$.this}}",
            "message": "Current step result"
        },
        {
            "op": "debug",
            "actualValue": "{{$.globals}}",
            "message": "Global variables"
        }
    ]
}
```

2. **Save intermediate values:**
```json
{
    "save": {
        "$.globals.debug_step1": "{{$.this}}",
        "$.globals.debug_timestamp": "{{$.now}}"
    }
}
```

3. **Use step descriptions:**
```json
{
    "type": "http",
    "description": "Get user profile - expecting user object with id, name, email",
    "method": "GET",
    "url": "/api/profile"
}
```

### Isolate Issues

1. **Test individual steps:**
```json
{
    "tests": [
        {
            "name": "Isolated Step Test",
            "steps": [
                {
                    "type": "http",
                    "method": "GET",
                    "url": "{{$.env.baseUrl}}/health",
                    "assert": [
                        {
                            "op": "equals",
                            "actualValue": "{{$.this.statusCode}}",
                            "expectedValue": 200
                        }
                    ]
                }
            ]
        }
    ]
}
```

2. **Simplify complex tests:**
```bash
# Run single test file
./src/JTest.Cli/bin/Debug/net8.0/JTest run single-test.json --debug

# Filter specific test
./src/JTest.Cli/bin/Debug/net8.0/JTest run tests.json --filter "specific test name" --debug
```

3. **Use minimal configuration:**
```json
{
    "version": "1.0",
    "env": {
        "baseUrl": "http://localhost:8080"
    },
    "tests": [
        {
            "name": "Minimal Test",
            "steps": [
                {
                    "type": "http",
                    "method": "GET",
                    "url": "{{$.env.baseUrl}}/ping"
                }
            ]
        }
    ]
}
```

## Performance Issues

### Slow Test Execution

**Symptoms:**
- Tests take longer than expected
- Individual steps have high response times
- Parallel execution doesn't improve performance

**Diagnosis:**

1. **Check step timing:**
```bash
./src/JTest.Cli/bin/Debug/net8.0/JTest run tests.json --verbose | grep "Duration:"
```

2. **Profile API endpoints:**
```bash
# Test individual endpoints
time curl https://api.example.com/endpoint
```

3. **Monitor system resources:**
```bash
# Check CPU and memory usage
top
htop

# Check network
netstat -an | grep ESTABLISHED
```

**Solutions:**

1. **Optimize parallel execution:**
```bash
# Adjust parallel count based on system
./src/JTest.Cli/bin/Debug/net8.0/JTest run tests.json --parallel 4

# For I/O bound tests, use higher count
./src/JTest.Cli/bin/Debug/net8.0/JTest run tests.json --parallel 16
```

2. **Reduce timeout for development:**
```json
{
    "env": {
        "timeout": 10000  // Reduce for dev environment
    }
}
```

3. **Cache authentication tokens:**
```json
{
    "globals": {
        "authToken": null  // Will be populated once and reused
    },
    "tests": [
        {
            "name": "Shared Authentication",
            "steps": [
                {
                    "type": "use",
                    "template": "authenticate-if-needed",
                    "params": {
                        "currentToken": "{{$.globals.authToken}}"
                    }
                }
            ]
        }
    ]
}
```

### Memory Issues

**Error:**
```
Error: Out of memory exception
```

**Solutions:**

1. **Reduce parallel execution:**
```bash
./src/JTest.Cli/bin/Debug/net8.0/JTest run tests.json --parallel 1
```

2. **Break large test suites:**
```bash
# Run tests in batches
./src/JTest.Cli/bin/Debug/net8.0/JTest run auth-tests.json
./src/JTest.Cli/bin/Debug/net8.0/JTest run user-tests.json
./src/JTest.Cli/bin/Debug/net8.0/JTest run order-tests.json
```

3. **Optimize test data:**
```json
// Avoid large request/response bodies in save operations
{
    "save": {
        "$.globals.userId": "{{$.this.body.id}}",        // Good: Save only needed data
        "$.globals.fullResponse": "{{$.this.body}}"      // Avoid: Save entire response
    }
}
```

## Platform-Specific Issues

### Windows Issues

**Path Separators:**
```json
// Use forward slashes (works on all platforms)
{
    "using": ["./templates/auth.json"]
}

// Avoid backslashes
{
    "using": [".\\templates\\auth.json"]  // May cause issues
}
```

**PowerShell Execution Policy:**
```powershell
# Check execution policy
Get-ExecutionPolicy

# Set execution policy if needed
Set-ExecutionPolicy RemoteSigned -Scope CurrentUser
```

### macOS Issues

**Certificate Issues:**
```bash
# Update certificates
brew install ca-certificates

# Or bypass SSL verification (not recommended for production)
export NODE_TLS_REJECT_UNAUTHORIZED=0
```

### Linux Issues

**Permission Issues:**
```bash
# Fix tool permissions
chmod +x ~/.dotnet/tools/jtest

# Check .NET installation
dotnet --info
```

## Integration Issues

### CI/CD Pipeline Problems

**Build Failures:**

1. **Missing .NET SDK:**
```yaml
# GitHub Actions
- name: Setup .NET
  uses: actions/setup-dotnet@v3
  with:
    dotnet-version: '8.0.x'
```

2. **Tool installation issues:**
```bash
# Ensure tool path is in PATH
export PATH="$PATH:$HOME/.dotnet/tools"

# Verify installation
which jtest
./src/JTest.Cli/bin/Debug/net8.0/JTest --version
```

**Environment Configuration:**

1. **Missing secrets:**
```yaml
# GitHub Actions
env:
  API_KEY: ${{ secrets.API_KEY }}
  BASE_URL: ${{ secrets.BASE_URL }}
```

2. **Network restrictions:**
```bash
# Check if proxy configuration is needed
export HTTP_PROXY=http://proxy.company.com:8080
export HTTPS_PROXY=http://proxy.company.com:8080
export NO_PROXY=localhost,127.0.0.1,.company.com
```

## Getting Help

### Log Collection

When reporting issues, collect relevant logs:

```bash
# Run with maximum debugging
./src/JTest.Cli/bin/Debug/net8.0/JTest run tests.json --debug --verbose > debug.log 2>&1

# Include system information
dotnet --info > system-info.log
./src/JTest.Cli/bin/Debug/net8.0/JTest --version >> system-info.log
```

### Minimal Reproduction

Create minimal test case that reproduces the issue:

```json
{
    "version": "1.0",
    "env": {
        "baseUrl": "https://httpbin.org"
    },
    "tests": [
        {
            "name": "Minimal reproduction case",
            "steps": [
                {
                    "type": "http",
                    "method": "GET",
                    "url": "{{$.env.baseUrl}}/get",
                    "assert": [
                        {
                            "op": "equals",
                            "actualValue": "{{$.this.statusCode}}",
                            "expectedValue": 200
                        }
                    ]
                }
            ]
        }
    ]
}
```

### Community Resources

- **GitHub Issues** - Report bugs and request features
- **Documentation** - Check latest documentation
- **Examples** - Review example test suites
- **Stack Overflow** - Community Q&A (tag: jtest)

## Next Steps

- [CLI Usage](cli-usage.md) - Advanced CLI debugging techniques
- [Best Practices](06-best-practices.md) - Prevent common issues
- [Extensibility](extensibility.md) - Debug custom extensions