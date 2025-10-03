# CLI Usage

The JTest CLI is the primary way to run tests, debug issues, and validate test files. This guide covers all command-line functionality.

## Getting the CLI

### 🚀 Quick Installation (Recommended)

Install JTest globally with our setup script:

```bash
# Linux/macOS
git clone https://github.com/nexxbiz/JTest.git && cd JTest && ./setup.sh

# Windows (PowerShell)
git clone https://github.com/nexxbiz/JTest.git; cd JTest; .\setup.ps1
```

After installation, use `jtest` command anywhere:

```bash
jtest --help
jtest run tests.json
```

### 📦 Manual Installation

```bash
# Clone and build packages
git clone https://github.com/nexxbiz/JTest.git
cd JTest
./scripts/build-packages.sh

# Install globally
dotnet tool install --global --add-source ./packages JTest.Cli
```

### 🔧 Development Build

For development or latest changes:

```bash
# Clone the repository
git clone https://github.com/nexxbiz/JTest.git
cd JTest

# Build the CLI
dotnet build src/JTest.Cli

# Use directly: ./src/JTest.Cli/bin/Debug/net8.0/JTest
# Or create an alias for convenience:
alias jtest='./src/JTest.Cli/bin/Debug/net8.0/JTest'
```

See **[INSTALLATION.md](../INSTALLATION.md)** for complete installation options including Docker and CI/CD integration.

## Basic Usage

### Running Tests

Run a single test file:

```bash
jtest run my-tests.json
```

Run multiple test files:

```bash
jtest run auth-tests.json user-tests.json order-tests.json
```

Run all tests in a directory:

```bash
jtest run tests/
```

Run tests with a pattern:

```bash
jtest run tests/*-integration.json
```

## Command Reference

### `run` Command

Execute test files:

```bash
jtest run [options] <test-files...>
```

#### Options

**`--environment` / `-e`**
Specify environment configuration:

```bash
./src/JTest.Cli/bin/Debug/net8.0/JTest run tests.json -e staging
./src/JTest.Cli/bin/Debug/net8.0/JTest run tests.json --environment production
```

**`--output` / `-o`**
Set output format:

```bash
./src/JTest.Cli/bin/Debug/net8.0/JTest run tests.json -o json
./src/JTest.Cli/bin/Debug/net8.0/JTest run tests.json --output markdown
./src/JTest.Cli/bin/Debug/net8.0/JTest run tests.json --output junit
```

Available formats:
- `console` (default) - Human-readable console output
- `json` - JSON test results
- `markdown` - Markdown report
- `junit` - JUnit XML format for CI/CD

**`--verbose` / `-v`**
Enable verbose output:

```bash
./src/JTest.Cli/bin/Debug/net8.0/JTest run tests.json -v
./src/JTest.Cli/bin/Debug/net8.0/JTest run tests.json --verbose
```

**`--debug` / `-d`**
Enable debug mode with detailed logging:

```bash
./src/JTest.Cli/bin/Debug/net8.0/JTest run tests.json -d
./src/JTest.Cli/bin/Debug/net8.0/JTest run tests.json --debug
```

**`--parallel` / `-p`**
Run tests in parallel:

```bash
./src/JTest.Cli/bin/Debug/net8.0/JTest run tests.json -p 4
./src/JTest.Cli/bin/Debug/net8.0/JTest run tests.json --parallel 8
```

**`--timeout` / `-t`**
Set global timeout (in seconds):

```bash
./src/JTest.Cli/bin/Debug/net8.0/JTest run tests.json -t 60
./src/JTest.Cli/bin/Debug/net8.0/JTest run tests.json --timeout 120
```

**`--filter` / `-f`**
Filter tests by name or pattern:

```bash
./src/JTest.Cli/bin/Debug/net8.0/JTest run tests.json -f "user creation"
./src/JTest.Cli/bin/Debug/net8.0/JTest run tests.json --filter "*auth*"
```

**`--skip-cleanup`**
Skip cleanup steps (useful for debugging):

```bash
./src/JTest.Cli/bin/Debug/net8.0/JTest run tests.json --skip-cleanup
```

**`--fail-fast`**
Stop on first test failure:

```bash
./src/JTest.Cli/bin/Debug/net8.0/JTest run tests.json --fail-fast
```

### Complete Example

```bash
./src/JTest.Cli/bin/Debug/net8.0/JTest run \
  --environment staging \
  --output junit \
  --verbose \
  --parallel 4 \
  --timeout 300 \
  --filter "*integration*" \
  tests/
```

## Configuration Files

### Environment Files

Create environment-specific configuration files:

**environments/staging.json:**
```json
{
    "baseUrl": "https://api-staging.example.com",
    "timeout": 30000,
    "credentials": {
        "adminUser": "admin@staging.example.com",
        "adminPassword": "${STAGING_ADMIN_PASSWORD}"
    }
}
```

**environments/production.json:**
```json
{
    "baseUrl": "https://api.example.com", 
    "timeout": 60000,
    "credentials": {
        "adminUser": "admin@example.com",
        "adminPassword": "${PROD_ADMIN_PASSWORD}"
    }
}
```

Use with:

```bash
./src/JTest.Cli/bin/Debug/net8.0/JTest run tests.json -e staging
```

### Global Configuration

Create a global config file `jtest.config.json`:

```json
{
    "defaultTimeout": 30000,
    "maxParallelTests": 4,
    "defaultOutputFormat": "console",
    "environments": {
        "dev": "./environments/development.json",
        "staging": "./environments/staging.json", 
        "prod": "./environments/production.json"
    },
    "templates": {
        "searchPaths": [
            "./templates/",
            "./shared-templates/"
        ]
    }
}
```

## Output Formats

### Console Output (Default)

Human-readable output for development:

```
Running JTest Suite: User API Tests
Environment: staging
Test Files: 3

✓ User Registration Tests
  ✓ Register with valid data (1.2s)
  ✗ Register with invalid email (0.8s)
    Expected status 400, got 500
  ✓ Register with existing email (0.5s)

✓ User Authentication Tests  
  ✓ Login with valid credentials (0.9s)
  ✓ Login with invalid password (0.4s)

Summary:
  Tests: 5
  Passed: 4
  Failed: 1
  Duration: 3.8s
```

### JSON Output

Machine-readable format for CI/CD:

```bash
./src/JTest.Cli/bin/Debug/net8.0/JTest run tests.json -o json > results.json
```

```json
{
    "summary": {
        "total": 5,
        "passed": 4,
        "failed": 1,
        "duration": 3.8
    },
    "testResults": [
        {
            "name": "Register with valid data",
            "status": "passed",
            "duration": 1.2,
            "steps": [...]
        }
    ]
}
```

### Markdown Output

Documentation-friendly format:

```bash
./src/JTest.Cli/bin/Debug/net8.0/JTest run tests.json -o markdown > test-report.md
```

```markdown
# Test Results

## Summary
- **Total Tests:** 5
- **Passed:** 4
- **Failed:** 1
- **Duration:** 3.8s

## Test Details

### ✓ User Registration Tests
#### ✓ Register with valid data (1.2s)
Steps executed successfully.

#### ✗ Register with invalid email (0.8s)
**Error:** Expected status 400, got 500
```

### JUnit XML Output

For CI/CD integration:

```bash
./src/JTest.Cli/bin/Debug/net8.0/JTest run tests.json -o junit > junit-results.xml
```

```xml
<?xml version="1.0" encoding="UTF-8"?>
<testsuites tests="5" failures="1" time="3.8">
    <testsuite name="User API Tests" tests="5" failures="1" time="3.8">
        <testcase name="Register with valid data" time="1.2"/>
        <testcase name="Register with invalid email" time="0.8">
            <failure message="Expected status 400, got 500"/>
        </testcase>
    </testsuite>
</testsuites>
```

## Environment Variables

JTest supports environment variable substitution:

### In Test Files

```json
{
    "env": {
        "baseUrl": "${API_BASE_URL}",
        "apiKey": "${API_KEY}",
        "timeout": "${TIMEOUT:-30000}"
    }
}
```

### Setting Variables

```bash
# Linux/macOS
export API_BASE_URL=https://api.example.com
export API_KEY=your-api-key
./src/JTest.Cli/bin/Debug/net8.0/JTest run tests.json

# Windows
set API_BASE_URL=https://api.example.com
set API_KEY=your-api-key
./src/JTest.Cli/bin/Debug/net8.0/JTest run tests.json

# Inline
API_BASE_URL=https://api.example.com ./src/JTest.Cli/bin/Debug/net8.0/JTest run tests.json
```

### .env File Support

Create a `.env` file in your project:

```
API_BASE_URL=https://api-staging.example.com
API_KEY=staging-api-key
ADMIN_EMAIL=admin@staging.example.com
ADMIN_PASSWORD=staging-password
TIMEOUT=30000
```

JTest automatically loads `.env` files from:
1. Current directory
2. Project root (if in subdirectory)
3. Home directory

## Debugging

### Verbose Mode

Get detailed execution information:

```bash
./src/JTest.Cli/bin/Debug/net8.0/JTest run tests.json --verbose
```

Output includes:
- Variable resolution details
- HTTP request/response details
- Step execution timing
- Context changes

### Debug Mode

Maximum debugging information:

```bash
./src/JTest.Cli/bin/Debug/net8.0/JTest run tests.json --debug
```

Includes everything from verbose mode plus:
- Internal engine logging
- Template resolution details
- Assertion evaluation steps
- Error stack traces

### Debug Specific Tests

Filter and debug specific tests:

```bash
./src/JTest.Cli/bin/Debug/net8.0/JTest run tests.json --filter "user creation" --debug --verbose
```

### Save Debug Output

Capture debug output to file:

```bash
./src/JTest.Cli/bin/Debug/net8.0/JTest run tests.json --debug --verbose > debug.log 2>&1
```

## Integration Examples

### CI/CD Pipeline Examples

#### GitHub Actions

```yaml
name: API Tests
on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'
          
      - name: Install JTest
        run: dotnet tool install -g JTest.Cli
        
      - name: Run Tests
        run: ./src/JTest.Cli/bin/Debug/net8.0/JTest run tests/ --output junit --environment staging
        env:
          API_BASE_URL: ${{ secrets.STAGING_API_URL }}
          API_KEY: ${{ secrets.STAGING_API_KEY }}
          
      - name: Publish Test Results
        uses: dorny/test-reporter@v1
        if: always()
        with:
          name: JTest Results
          path: junit-results.xml
          reporter: java-junit
```

#### GitLab CI

```yaml
stages:
  - test

api-tests:
  stage: test
  image: mcr.microsoft.com/dotnet/sdk:8.0
  script:
    - dotnet tool install -g JTest.Cli
    - ./src/JTest.Cli/bin/Debug/net8.0/JTest run tests/ --output junit --environment staging
  variables:
    API_BASE_URL: $STAGING_API_URL
    API_KEY: $STAGING_API_KEY
  artifacts:
    reports:
      junit: junit-results.xml
```

#### Jenkins Pipeline

```groovy
pipeline {
    agent any
    
    environment {
        API_BASE_URL = credentials('staging-api-url')
        API_KEY = credentials('staging-api-key')
    }
    
    stages {
        stage('Install JTest') {
            steps {
                sh 'dotnet tool install -g JTest.Cli'
            }
        }
        
        stage('Run API Tests') {
            steps {
                sh './src/JTest.Cli/bin/Debug/net8.0/JTest run tests/ --output junit --environment staging'
            }
            post {
                always {
                    junit 'junit-results.xml'
                }
            }
        }
    }
}
```

### Docker Integration

**Dockerfile:**
```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0

WORKDIR /tests

# Install JTest
RUN dotnet tool install -g JTest.Cli
ENV PATH="$PATH:/root/.dotnet/tools"

# Copy test files
COPY tests/ ./tests/
COPY environments/ ./environments/

# Run tests
CMD ["jtest", "run", "tests/", "--environment", "production"]
```

**Build and run:**
```bash
docker build -t api-tests .
docker run --env-file .env api-tests
```

### NPM Integration

**package.json:**
```json
{
    "scripts": {
        "test:api": "./src/JTest.Cli/bin/Debug/net8.0/JTest run tests/",
        "test:api:staging": "./src/JTest.Cli/bin/Debug/net8.0/JTest run tests/ -e staging",
        "test:api:ci": "./src/JTest.Cli/bin/Debug/net8.0/JTest run tests/ --output junit --fail-fast"
    }
}
```

```bash
npm run test:api
npm run test:api:staging
```

## Advanced Usage

### Custom Output Handlers

Create custom output processors:

```bash
./src/JTest.Cli/bin/Debug/net8.0/JTest run tests.json -o json | jq '.testResults[] | select(.status == "failed")'
```

### Test Report Generation

Generate comprehensive reports:

```bash
# Generate HTML report
./src/JTest.Cli/bin/Debug/net8.0/JTest run tests.json -o markdown | pandoc -f markdown -t html > report.html

# Generate PDF report  
./src/JTest.Cli/bin/Debug/net8.0/JTest run tests.json -o markdown | pandoc -f markdown -t pdf > report.pdf
```

### Parallel Execution Tuning

Optimize parallel execution:

```bash
# CPU-bound: Use CPU count
./src/JTest.Cli/bin/Debug/net8.0/JTest run tests.json --parallel $(nproc)

# I/O-bound: Use higher count
./src/JTest.Cli/bin/Debug/net8.0/JTest run tests.json --parallel 16

# Conservative: Use half CPU count
./src/JTest.Cli/bin/Debug/net8.0/JTest run tests.json --parallel $(($(nproc)/2))
```

### Conditional Test Execution

Run tests based on conditions:

```bash
# Only run integration tests in CI
if [ "$CI" = "true" ]; then
    ./src/JTest.Cli/bin/Debug/net8.0/JTest run tests/ --filter "*integration*"
else
    ./src/JTest.Cli/bin/Debug/net8.0/JTest run tests/ --filter "*unit*"
fi
```

## Performance Optimization

### Tips for Faster Test Execution

1. **Use parallel execution** for independent tests:
```bash
./src/JTest.Cli/bin/Debug/net8.0/JTest run tests.json --parallel 8
```

2. **Filter tests** during development:
```bash
./src/JTest.Cli/bin/Debug/net8.0/JTest run tests.json --filter "auth*"
```

3. **Optimize templates** to reduce HTTP calls

4. **Use environment-specific timeouts**:
```json
{
    "env": {
        "timeout": "${TEST_TIMEOUT:-10000}"
    }
}
```

5. **Skip cleanup** during debugging:
```bash
./src/JTest.Cli/bin/Debug/net8.0/JTest run tests.json --skip-cleanup
```

## Troubleshooting CLI Issues

### Common Issues

**Tool not found:**
```bash
# Check installation
dotnet tool list -g

# Reinstall if needed
dotnet tool uninstall -g JTest.Cli
dotnet tool install -g JTest.Cli
```

**Path issues:**
```bash
# Add to PATH (Linux/macOS)
export PATH="$PATH:$HOME/.dotnet/tools"

# Windows
set PATH=%PATH%;%USERPROFILE%\.dotnet\tools
```

**Permission issues:**
```bash
# Linux/macOS: Check file permissions
chmod +x ~/.dotnet/tools/jtest

# Windows: Run as administrator if needed
```

**Environment variable issues:**
```bash
# Debug environment variables
./src/JTest.Cli/bin/Debug/net8.0/JTest run tests.json --debug | grep -i "environment"

# Check variable substitution
echo $API_BASE_URL
```

### Debug CLI Issues

Enable maximum logging:

```bash
JTEST_LOG_LEVEL=debug ./src/JTest.Cli/bin/Debug/net8.0/JTest run tests.json --debug --verbose
```

Check configuration loading:

```bash
./src/JTest.Cli/bin/Debug/net8.0/JTest run tests.json --debug 2>&1 | grep -i "config"
```

## Exit Codes

JTest uses standard exit codes:

- `0` - All tests passed
- `1` - One or more tests failed
- `2` - Invalid command line arguments
- `3` - Configuration error
- `4` - File not found error
- `5` - Runtime error

Use in scripts:

```bash
if ./src/JTest.Cli/bin/Debug/net8.0/JTest run tests.json; then
    echo "All tests passed!"
else
    echo "Tests failed with exit code $?"
    exit 1
fi
```

## Next Steps

- [Troubleshooting](troubleshooting.md) - Debug test and CLI issues
- [CI/CD Integration](ci-cd-integration.md) - Advanced pipeline integration
- [Best Practices](06-best-practices.md) - Effective CLI usage patterns