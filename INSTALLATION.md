# Installation Guide

JTest provides multiple installation methods to fit different workflows and environments.

## Version Management

JTest provides an automated version system with different release channels:

### 🚀 **Stable Releases**
- Tagged releases (e.g., `v1.0.0`, `v1.1.0`)
- Manually created for major milestones
- Published to NuGet.org
- Available via GitHub Releases

### 🚧 **Development Builds**
- Automatically generated on every merge to main
- Version format: `1.0.0-dev.{build-number}+{commit-hash}`
- Pre-release packages available on GitHub
- Updated continuously with latest changes

### 📦 **Version Manager Tool**

Use the included version manager to easily work with different versions:

```bash
# List all available versions
./scripts/version-manager.sh list

# Get latest stable version
./scripts/version-manager.sh latest

# Get latest development version  
./scripts/version-manager.sh dev

# Download specific version
./scripts/version-manager.sh download v1.0.0
./scripts/version-manager.sh download development

# Install specific version globally
./scripts/version-manager.sh install v1.0.0
./scripts/version-manager.sh install development

# Get version information
./scripts/version-manager.sh info development
```

## Quick Setup (Recommended)

### 🚀 One-Command Setup

**Linux/macOS:**
```bash
# Clone and setup in one command
git clone https://github.com/nexxbiz/JTest.git && cd JTest && ./setup.sh
```

**Windows (PowerShell):**
```powershell
# Clone and setup in one command
git clone https://github.com/nexxbiz/JTest.git; cd JTest; .\setup.ps1
```

This will:
- Build the packages locally
- Install the `jtest` CLI tool globally
- Verify the installation

After setup, you can use `jtest` command anywhere:
```bash
jtest --help
jtest create "My First Test" my-test.json
jtest run my-test.json
```

## Manual Installation

### 📦 From Local Build

1. **Clone the repository:**
   ```bash
   git clone https://github.com/nexxbiz/JTest.git
   cd JTest
   ```

2. **Build packages:**
   ```bash
   # Linux/macOS
   ./scripts/build-packages.sh
   
   # Windows
   .\scripts\build-packages.ps1
   ```

3. **Install the CLI tool:**
   ```bash
   dotnet tool install --global --add-source ./packages JTest.Cli
   ```

### 🔧 From Source (Development)

For development or when you need the latest changes:

```bash
# Clone the repository
git clone https://github.com/nexxbiz/JTest.git
cd JTest

# Build the solution
dotnet build

# Run directly from source
./src/JTest.Cli/bin/Debug/net8.0/JTest --help

# Or create an alias for convenience
alias jtest='./src/JTest.Cli/bin/Debug/net8.0/JTest'
```

## Docker Installation

### 🐳 Using Docker

1. **Build the Docker image:**
   ```bash
   git clone https://github.com/nexxbiz/JTest.git
   cd JTest
   ./docker.sh build
   ```

2. **Run JTest in a container:**
   ```bash
   # Show help
   ./docker.sh run --help
   
   # Run a test file (mounts current directory)
   ./docker.sh test my-test.json
   
   # Interactive shell for debugging
   ./docker.sh shell
   ```

### Docker Examples

```bash
# Build the image
./docker.sh build

# Run tests from current directory
echo '{"version":"1.0","tests":[{"name":"Test","steps":[{"type":"wait","ms":100}]}]}' > test.json
./docker.sh test test.json

# Run with custom arguments
./docker.sh run run tests/*.json --parallel 4
```

## CI/CD Integration

### GitHub Actions

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
          
      - name: Setup JTest
        run: |
          git clone https://github.com/nexxbiz/JTest.git jtest-repo
          cd jtest-repo
          ./scripts/build-packages.sh
          dotnet tool install --global --add-source ./packages JTest.Cli
          
      - name: Run Tests
        run: jtest run tests/*.json --parallel 4
        env:
          API_BASE_URL: ${{ secrets.API_BASE_URL }}
          API_KEY: ${{ secrets.API_KEY }}
```

### GitLab CI

```yaml
stages:
  - test

api-tests:
  stage: test
  image: mcr.microsoft.com/dotnet/sdk:8.0
  before_script:
    - git clone https://github.com/nexxbiz/JTest.git jtest-repo
    - cd jtest-repo
    - ./scripts/build-packages.sh
    - dotnet tool install --global --add-source ./packages JTest.Cli
    - cd ..
  script:
    - jtest run tests/*.json --parallel 4
  variables:
    API_BASE_URL: $API_BASE_URL
    API_KEY: $API_KEY
```

### Jenkins Pipeline

```groovy
pipeline {
    agent any
    
    stages {
        stage('Setup JTest') {
            steps {
                sh '''
                    git clone https://github.com/nexxbiz/JTest.git jtest-repo
                    cd jtest-repo
                    ./scripts/build-packages.sh
                    dotnet tool install --global --add-source ./packages JTest.Cli
                '''
            }
        }
        
        stage('Run Tests') {
            steps {
                sh 'jtest run tests/*.json --parallel 4'
            }
        }
    }
}
```

## Verification

After installation, verify JTest is working:

```bash
# Check version and help
jtest --help

# Create a sample test
jtest create "My Test" sample.json

# Run the test
jtest run sample.json

# Debug mode for troubleshooting
jtest debug sample.json
```

## Troubleshooting

### Common Issues

**`jtest` command not found:**
- Make sure `~/.dotnet/tools` is in your PATH
- Restart your terminal after installation
- Run: `export PATH="$PATH:$HOME/.dotnet/tools"`

**Permission errors on Linux/macOS:**
- Make sure scripts are executable: `chmod +x setup.sh scripts/*.sh`
- Ensure you have write permissions in the installation directory

**Docker build issues:**
- Make sure Docker is running
- Try: `docker system prune` to clean up space
- Check Docker has sufficient memory allocated

### Getting Help

- **Documentation:** [docs/README.md](docs/README.md)
- **Issues:** [GitHub Issues](https://github.com/nexxbiz/JTest/issues)
- **Examples:** [docs/examples/](docs/examples/)

## Next Steps

After installation:

1. **📖 Read the [Getting Started Guide](docs/01-getting-started.md)**
2. **🧪 Try the [Quick Examples](docs/examples/)**
3. **📚 Explore the [Full Documentation](docs/README.md)**