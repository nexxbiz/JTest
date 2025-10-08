# CI/CD Integration (Future Enhancement)

> **Note**: This functionality requires JTest to be published as a NuGet package, which is not yet available. 
> Currently, JTest must be built from source and run using the binary directly.

JTest is designed to work seamlessly in continuous integration and delivery pipelines once packaged. For now, you can integrate it by:

1. **Building from source** in your CI pipeline
2. **Running the binary directly** 
3. **Using standard exit codes** to determine success/failure

## Current Integration Example

For GitHub Actions, you could use:

```yaml
name: API Tests
on: [push, pull_request]

jobs:
  api-tests:
    runs-on: ubuntu-latest
    
    steps:
    - name: Checkout code
      uses: actions/checkout@v4
      
    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: '8.0.x'
        
    - name: Build JTest
      run: |
        git clone https://github.com/nexxbiz/JTest.git jtest-source
        cd jtest-source
        dotnet build src/JTest.Cli
        
    - name: Run Tests
      run: |
        cd jtest-source
        ./src/JTest.Cli/bin/Debug/net8.0/JTest run ../tests/*.json
```

## Standard Exit Codes

JTest uses standard exit codes for CI integration:

- `0` - All tests passed
- `1` - One or more tests failed  
- `2` - Invalid command line arguments
- `3` - Configuration error
- `4` - File not found error
- `5` - Runtime error

## Future Enhancements

Once JTest is published as a NuGet package, full CI/CD integration will be available with simpler installation and usage patterns.
        
    - name: Install JTest
      run: dotnet tool install -g JTest.Cli
      
    - name: Run API Tests
      run: jtest run tests/ --output junit --environment ci
      env:
        API_BASE_URL: ${{ secrets.CI_API_URL }}
        API_KEY: ${{ secrets.CI_API_KEY }}
        
    - name: Publish Test Results
      uses: dorny/test-reporter@v1
      if: always()
      with:
        name: JTest Results
        path: junit-results.xml
        reporter: java-junit
```

### Advanced Workflow with Multiple Environments

```yaml
name: Comprehensive API Tests

on:
  push:
    branches: [ main ]
  pull_request:
    branches: [ main ]

jobs:
  test-matrix:
    strategy:
      matrix:
        environment: [staging, production]
        include:
          - environment: staging
            api_url: ${{ secrets.STAGING_API_URL }}
            api_key: ${{ secrets.STAGING_API_KEY }}
          - environment: production
            api_url: ${{ secrets.PROD_API_URL }}
            api_key: ${{ secrets.PROD_API_KEY }}
    
    runs-on: ubuntu-latest
    name: Test ${{ matrix.environment }}
    
    steps:
    - uses: actions/checkout@v4
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: '8.0.x'
        
    - name: Install JTest
      run: dotnet tool install -g JTest.Cli
      
    - name: Create environment config
      run: |
        cat > env-${{ matrix.environment }}.json << EOF
        {
          "baseUrl": "${{ matrix.api_url }}",
          "apiKey": "${{ matrix.api_key }}",
          "timeout": 30000,
          "retryCount": 3
        }
        EOF
        
    - name: Run Tests
      run: |
        jtest run tests/ \
          --environment env-${{ matrix.environment }}.json \
          --output junit \
          --parallel 4 \
          --timeout 300
          
    - name: Upload Test Results
      uses: actions/upload-artifact@v4
      if: always()
      with:
        name: test-results-${{ matrix.environment }}
        path: junit-results.xml
        
    - name: Publish Test Report
      uses: dorny/test-reporter@v1
      if: always()
      with:
        name: JTest Results (${{ matrix.environment }})
        path: junit-results.xml
        reporter: java-junit
```

### Conditional Test Execution

```yaml
name: Smart API Testing

on: [push, pull_request]

jobs:
  detect-changes:
    runs-on: ubuntu-latest
    outputs:
      api-changed: ${{ steps.changes.outputs.api }}
      tests-changed: ${{ steps.changes.outputs.tests }}
    steps:
    - uses: actions/checkout@v4
    - uses: dorny/paths-filter@v2
      id: changes
      with:
        filters: |
          api:
            - 'src/api/**'
            - 'api-schema/**'
          tests:
            - 'tests/**'
            - 'templates/**'

  api-tests:
    needs: detect-changes
    if: needs.detect-changes.outputs.api == 'true' || needs.detect-changes.outputs.tests == 'true'
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v4
    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: '8.0.x'
        
    - name: Install JTest
      run: dotnet tool install -g JTest.Cli
      
    - name: Run Changed Tests
      run: |
        if [ "${{ needs.detect-changes.outputs.api }}" == "true" ]; then
          # Run all tests if API changed
          jtest run tests/ --output junit
        else
          # Run only modified test files
          jtest run $(git diff --name-only HEAD~1 tests/*.json) --output junit
        fi
```

## GitLab CI

### Basic Pipeline

```yaml
stages:
  - test
  - report

variables:
  DOTNET_VERSION: "8.0.x"

before_script:
  - apt-get update -qy
  - apt-get install -y curl
  - curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --version $DOTNET_VERSION
  - export PATH="$PATH:$HOME/.dotnet:$HOME/.dotnet/tools"

api-tests:
  stage: test
  image: mcr.microsoft.com/dotnet/sdk:8.0
  script:
    - dotnet tool install -g JTest.Cli
    - export PATH="$PATH:$HOME/.dotnet/tools"
    - jtest run tests/ --output junit --environment ci
  variables:
    API_BASE_URL: $CI_API_BASE_URL
    API_KEY: $CI_API_KEY
  artifacts:
    reports:
      junit: junit-results.xml
    paths:
      - junit-results.xml
    expire_in: 1 week
    when: always

generate-report:
  stage: report
  image: mcr.microsoft.com/dotnet/sdk:8.0
  dependencies:
    - api-tests
  script:
    - dotnet tool install -g JTest.Cli
    - export PATH="$PATH:$HOME/.dotnet/tools"
    - jtest run tests/ --output markdown --environment ci > test-report.md
  artifacts:
    paths:
      - test-report.md
    expire_in: 1 week
  when: always
```

### Environment-Specific Pipeline

```yaml
stages:
  - test-staging
  - test-production

.test-template: &test-template
  image: mcr.microsoft.com/dotnet/sdk:8.0
  before_script:
    - dotnet tool install -g JTest.Cli
    - export PATH="$PATH:$HOME/.dotnet/tools"
  script:
    - jtest run tests/ --output junit --environment $ENVIRONMENT --parallel 8
  artifacts:
    reports:
      junit: junit-results.xml
    when: always

test-staging:
  <<: *test-template
  stage: test-staging
  variables:
    ENVIRONMENT: staging
    API_BASE_URL: $STAGING_API_URL
    API_KEY: $STAGING_API_KEY
  only:
    - merge_requests
    - develop

test-production:
  <<: *test-template
  stage: test-production
  variables:
    ENVIRONMENT: production
    API_BASE_URL: $PROD_API_URL
    API_KEY: $PROD_API_KEY
  only:
    - main
  when: manual
```

## Jenkins

### Declarative Pipeline

```groovy
pipeline {
    agent any
    
    environment {
        DOTNET_CLI_TELEMETRY_OPTOUT = 'true'
        DOTNET_SKIP_FIRST_TIME_EXPERIENCE = 'true'
    }
    
    parameters {
        choice(
            name: 'ENVIRONMENT',
            choices: ['staging', 'production'],
            description: 'Environment to test against'
        )
        booleanParam(
            name: 'PARALLEL_EXECUTION',
            defaultValue: true,
            description: 'Enable parallel test execution'
        )
    }
    
    stages {
        stage('Setup') {
            steps {
                // Install .NET SDK
                sh '''
                    curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --version 8.0.100
                    export PATH="$PATH:$HOME/.dotnet"
                    dotnet --version
                '''
            }
        }
        
        stage('Install JTest') {
            steps {
                sh '''
                    export PATH="$PATH:$HOME/.dotnet:$HOME/.dotnet/tools"
                    dotnet tool install -g JTest.Cli
                    jtest --version
                '''
            }
        }
        
        stage('Run API Tests') {
            environment {
                API_BASE_URL = credentials("${params.ENVIRONMENT}-api-url")
                API_KEY = credentials("${params.ENVIRONMENT}-api-key")
            }
            steps {
                script {
                    def parallelFlag = params.PARALLEL_EXECUTION ? '--parallel 4' : ''
                    sh """
                        export PATH="\$PATH:\$HOME/.dotnet/tools"
                        jtest run tests/ \
                            --output junit \
                            --environment ${params.ENVIRONMENT} \
                            ${parallelFlag} \
                            --timeout 300
                    """
                }
            }
            post {
                always {
                    junit 'junit-results.xml'
                    archiveArtifacts artifacts: 'junit-results.xml', allowEmptyArchive: true
                }
            }
        }
        
        stage('Generate Report') {
            when {
                anyOf {
                    branch 'main'
                    changeRequest()
                }
            }
            steps {
                sh '''
                    export PATH="$PATH:$HOME/.dotnet/tools"
                    jtest run tests/ --output markdown --environment ${ENVIRONMENT} > api-test-report.md
                '''
                publishHTML([
                    allowMissing: false,
                    alwaysLinkToLastBuild: true,
                    keepAll: true,
                    reportDir: '.',
                    reportFiles: 'api-test-report.md',
                    reportName: 'API Test Report'
                ])
            }
        }
    }
    
    post {
        always {
            cleanWs()
        }
        failure {
            emailext(
                subject: "API Tests Failed: ${env.JOB_NAME} - ${env.BUILD_NUMBER}",
                body: "API tests failed for ${params.ENVIRONMENT} environment. Check Jenkins for details.",
                to: "${env.CHANGE_AUTHOR_EMAIL ?: 'team@company.com'}"
            )
        }
    }
}
```

### Multi-branch Pipeline

```groovy
pipeline {
    agent any
    
    stages {
        stage('Determine Test Strategy') {
            steps {
                script {
                    if (env.BRANCH_NAME == 'main') {
                        env.TEST_ENVIRONMENT = 'production'
                        env.TEST_SCOPE = 'full'
                    } else if (env.BRANCH_NAME == 'develop') {
                        env.TEST_ENVIRONMENT = 'staging'
                        env.TEST_SCOPE = 'full'
                    } else {
                        env.TEST_ENVIRONMENT = 'staging'
                        env.TEST_SCOPE = 'smoke'
                    }
                }
            }
        }
        
        stage('API Tests') {
            parallel {
                stage('Smoke Tests') {
                    when {
                        environment name: 'TEST_SCOPE', value: 'smoke'
                    }
                    steps {
                        sh '''
                            export PATH="$PATH:$HOME/.dotnet/tools"
                            jtest run tests/ \
                                --filter "*smoke*" \
                                --environment ${TEST_ENVIRONMENT} \
                                --output junit
                        '''
                    }
                }
                
                stage('Full Test Suite') {
                    when {
                        environment name: 'TEST_SCOPE', value: 'full'
                    }
                    steps {
                        sh '''
                            export PATH="$PATH:$HOME/.dotnet/tools"
                            jtest run tests/ \
                                --environment ${TEST_ENVIRONMENT} \
                                --output junit \
                                --parallel 6
                        '''
                    }
                }
            }
        }
    }
}
```

## Azure DevOps

### Basic Pipeline

```yaml
trigger:
  branches:
    include:
    - main
    - develop

variables:
  dotnetVersion: '8.0.x'

stages:
- stage: Test
  displayName: 'API Tests'
  jobs:
  - job: RunTests
    displayName: 'Run JTest Suite'
    pool:
      vmImage: 'ubuntu-latest'
    
    steps:
    - task: UseDotNet@2
      displayName: 'Setup .NET SDK'
      inputs:
        packageType: 'sdk'
        version: $(dotnetVersion)
        
    - script: |
        dotnet tool install -g JTest.Cli
        echo "##vso[task.prependpath]$HOME/.dotnet/tools"
      displayName: 'Install JTest'
      
    - script: |
        jtest run tests/ \
          --output junit \
          --environment ci \
          --parallel 4
      displayName: 'Run API Tests'
      env:
        API_BASE_URL: $(ApiBaseUrl)
        API_KEY: $(ApiKey)
        
    - task: PublishTestResults@2
      displayName: 'Publish Test Results'
      inputs:
        testResultsFormat: 'JUnit'
        testResultsFiles: 'junit-results.xml'
        testRunTitle: 'JTest API Tests'
      condition: always()
```

### Multi-Environment Pipeline

```yaml
parameters:
- name: environments
  type: object
  default:
  - name: staging
    displayName: 'Staging Environment'
    apiUrl: $(StagingApiUrl)
    apiKey: $(StagingApiKey)
  - name: production
    displayName: 'Production Environment'
    apiUrl: $(ProductionApiUrl)
    apiKey: $(ProductionApiKey)

stages:
- ${{ each env in parameters.environments }}:
  - stage: Test_${{ env.name }}
    displayName: 'Test ${{ env.displayName }}'
    jobs:
    - job: RunTests
      displayName: 'Run Tests - ${{ env.name }}'
      pool:
        vmImage: 'ubuntu-latest'
      
      steps:
      - task: UseDotNet@2
        inputs:
          packageType: 'sdk'
          version: '8.0.x'
          
      - script: dotnet tool install -g JTest.Cli
        displayName: 'Install JTest'
        
      - script: |
          jtest run tests/ \
            --environment ${{ env.name }} \
            --output junit \
            --parallel 8 \
            --timeout 300
        displayName: 'Run Tests'
        env:
          API_BASE_URL: ${{ env.apiUrl }}
          API_KEY: ${{ env.apiKey }}
          
      - task: PublishTestResults@2
        inputs:
          testResultsFormat: 'JUnit'
          testResultsFiles: 'junit-results.xml'
          testRunTitle: 'API Tests - ${{ env.displayName }}'
        condition: always()
```

## Docker Integration

### Test Container

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0

WORKDIR /app

# Install JTest
RUN dotnet tool install -g JTest.Cli
ENV PATH="$PATH:/root/.dotnet/tools"

# Copy test files
COPY tests/ ./tests/
COPY templates/ ./templates/
COPY environments/ ./environments/

# Default command
CMD ["jtest", "run", "tests/", "--output", "junit"]
```

### Docker Compose for Integration Testing

```yaml
version: '3.8'

services:
  api:
    image: myapi:latest
    ports:
      - "8080:8080"
    environment:
      - DATABASE_URL=postgresql://postgres:password@db:5432/testdb
    depends_on:
      - db
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
      interval: 30s
      timeout: 10s
      retries: 3

  db:
    image: postgres:15
    environment:
      - POSTGRES_DB=testdb
      - POSTGRES_USER=postgres
      - POSTGRES_PASSWORD=password
    ports:
      - "5432:5432"

  api-tests:
    build:
      context: .
      dockerfile: Dockerfile.tests
    environment:
      - API_BASE_URL=http://api:8080
      - DATABASE_URL=postgresql://postgres:password@db:5432/testdb
    depends_on:
      api:
        condition: service_healthy
    volumes:
      - ./test-results:/app/results
    command: >
      sh -c "
        jtest run tests/ 
          --output junit 
          --environment docker 
          --parallel 4 &&
        cp junit-results.xml /app/results/
      "
```

### Kubernetes Job

```yaml
apiVersion: batch/v1
kind: Job
metadata:
  name: api-tests
spec:
  template:
    spec:
      containers:
      - name: jtest
        image: mycompany/jtest:latest
        env:
        - name: API_BASE_URL
          valueFrom:
            configMapKeyRef:
              name: test-config
              key: api-url
        - name: API_KEY
          valueFrom:
            secretKeyRef:
              name: test-secrets
              key: api-key
        command:
        - jtest
        - run
        - tests/
        - --output
        - junit
        - --environment
        - kubernetes
        volumeMounts:
        - name: test-results
          mountPath: /app/results
      volumes:
      - name: test-results
        persistentVolumeClaim:
          claimName: test-results-pvc
      restartPolicy: Never
  backoffLimit: 2
```

## Environment Configuration Strategies

### Environment-Specific Files

**Structure:**
```
environments/
├── ci.json
├── staging.json
├── production.json
└── local.json
```

**ci.json:**
```json
{
  "baseUrl": "${API_BASE_URL}",
  "timeout": 30000,
  "retryCount": 3,
  "parallelTests": 8,
  "debugMode": false,
  "credentials": {
    "apiKey": "${API_KEY}",
    "adminUser": "${ADMIN_USER}",
    "adminPassword": "${ADMIN_PASSWORD}"
  }
}
```

### Dynamic Configuration

```bash
# Generate environment config in CI
cat > ci-env.json << EOF
{
  "baseUrl": "${API_BASE_URL}",
  "apiKey": "${API_KEY}",
  "buildNumber": "${BUILD_NUMBER}",
  "gitCommit": "${GIT_COMMIT}",
  "testTimestamp": "$(date -u +%Y-%m-%dT%H:%M:%SZ)"
}
EOF

jtest run tests/ --environment ci-env.json
```

### Secret Management

#### GitHub Actions
```yaml
- name: Run Tests
  env:
    API_KEY: ${{ secrets.API_KEY }}
    DB_PASSWORD: ${{ secrets.DB_PASSWORD }}
  run: jtest run tests/ --environment ci
```

#### GitLab CI
```yaml
variables:
  API_KEY: $CI_API_KEY
  DB_PASSWORD: $CI_DB_PASSWORD
```

#### Jenkins
```groovy
environment {
    API_KEY = credentials('api-key')
    DB_PASSWORD = credentials('db-password')
}
```

#### Azure DevOps
```yaml
variables:
- group: api-test-secrets

steps:
- script: jtest run tests/ --environment ci
  env:
    API_KEY: $(api-key)
    DB_PASSWORD: $(db-password)
```

## Test Data Management

### Test Data Isolation

```json
{
  "env": {
    "testRunId": "${BUILD_NUMBER:-$(date +%s)}",
    "baseUrl": "${API_BASE_URL}"
  },
  "globals": {
    "testUser": {
      "email": "test-${testRunId}@example.com",
      "username": "testuser-${testRunId}"
    }
  }
}
```

### Data Cleanup

```json
{
  "tests": [
    {
      "name": "User Management Test",
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
          "description": "Cleanup: Remove test user",
          "method": "DELETE",
          "url": "/api/users/{{$.createUser.body.id}}"
        }
      ]
    }
  ]
}
```

## Reporting and Notifications

### Slack Notifications

```bash
# In CI script
if ! jtest run tests/ --output json > results.json; then
  curl -X POST -H 'Content-type: application/json' \
    --data '{"text":"API tests failed in '"$ENVIRONMENT"' environment"}' \
    $SLACK_WEBHOOK_URL
fi
```

### Teams Notifications

```bash
# PowerShell in Azure DevOps
if ($LASTEXITCODE -ne 0) {
  $body = @{
    text = "API tests failed in $env:ENVIRONMENT environment"
    title = "Test Failure Alert"
  } | ConvertTo-Json
  
  Invoke-RestMethod -Uri $env:TEAMS_WEBHOOK -Method Post -Body $body -ContentType 'application/json'
}
```

### Custom Reporting

```bash
# Generate and upload custom report
jtest run tests/ --output json > results.json

# Process results and generate custom report
node generate-report.js results.json

# Upload to S3, Azure Blob, etc.
aws s3 cp test-report.html s3://reports-bucket/api-tests/
```

## Performance Optimization

### Parallel Execution

```bash
# Optimize based on environment
if [ "$CI" = "true" ]; then
  # CI environment - use more parallel workers
  PARALLEL_COUNT=8
else
  # Local development - conservative
  PARALLEL_COUNT=2
fi

jtest run tests/ --parallel $PARALLEL_COUNT
```

### Caching

```yaml
# GitHub Actions - Cache .NET tools
- name: Cache .NET tools
  uses: actions/cache@v4
  with:
    path: ~/.dotnet/tools
    key: ${{ runner.os }}-dotnet-tools-${{ hashFiles('**/*.csproj') }}
    
- name: Install JTest
  run: |
    if ! command -v jtest &> /dev/null; then
      dotnet tool install -g JTest.Cli
    fi
```

### Test Filtering

```bash
# Run different test suites based on trigger
case "$GITHUB_EVENT_NAME" in
  "push")
    if [ "$GITHUB_REF" = "refs/heads/main" ]; then
      # Full test suite for main branch
      jtest run tests/
    else
      # Smoke tests for feature branches  
      jtest run tests/ --filter "*smoke*"
    fi
    ;;
  "pull_request")
    # Integration tests for PRs
    jtest run tests/ --filter "*integration*"
    ;;
esac
```

## Best Practices

### CI/CD Integration Checklist

- [ ] **Environment Isolation** - Use separate environments for different stages
- [ ] **Secret Management** - Store sensitive data in CI/CD secret stores
- [ ] **Test Data Isolation** - Use unique test data per run
- [ ] **Parallel Execution** - Optimize test execution time
- [ ] **Proper Error Handling** - Fail builds on test failures
- [ ] **Result Publishing** - Make test results visible in CI/CD interface
- [ ] **Cleanup** - Remove test data after runs
- [ ] **Monitoring** - Track test execution metrics
- [ ] **Notifications** - Alert teams on failures
- [ ] **Documentation** - Document CI/CD setup and troubleshooting

### Common Pitfalls to Avoid

1. **Hardcoded URLs** - Use environment variables
2. **Shared Test Data** - Can cause flaky tests
3. **No Cleanup** - Leaves test data in systems
4. **Missing Timeouts** - Tests can hang indefinitely
5. **Inadequate Error Handling** - Failures are hard to diagnose
6. **No Test Result Publishing** - Results aren't visible to team
7. **Overly Complex Pipelines** - Hard to maintain and debug

## Next Steps

- [CLI Usage](cli-usage.md) - Advanced CLI options for CI/CD
- [Troubleshooting](troubleshooting.md) - Debugging CI/CD issues
- [Best Practices](06-best-practices.md) - General testing best practices