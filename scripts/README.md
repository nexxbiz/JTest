# JTest Scripts and CI/CD Reference

This directory contains build scripts and CI/CD configurations for the JTest project.

## 🚀 Quick Start Scripts

### Setup Scripts (Recommended)

**`setup.sh` / `setup.ps1`** - One-command installation
- Builds packages locally
- Installs JTest CLI tool globally
- Verifies installation
- Ready to use in seconds

```bash
# Linux/macOS
./setup.sh

# Windows
.\setup.ps1
```

### Build Scripts

**`scripts/build-packages.sh` / `scripts/build-packages.ps1`** - Create NuGet packages
- Builds solution in Release configuration
- Creates Core and CLI packages
- Outputs to `./packages/` directory

```bash
# Linux/macOS
./scripts/build-packages.sh [--clean] [--configuration Release]

# Windows
.\scripts\build-packages.ps1 [-Clean] [-Configuration Release]
```

### Docker Scripts

**`docker.sh`** - Docker helper commands
- Build JTest Docker image
- Run JTest in containers
- Interactive debugging shell

```bash
./docker.sh build                    # Build image
./docker.sh run --help              # Show help
./docker.sh test my-test.json       # Run test file
./docker.sh shell                   # Interactive shell
```

## 📦 Package Structure

After building, packages are created in `./packages/`:

```
packages/
├── JTest.Core.1.0.0.nupkg          # Core library
├── JTest.Core.1.0.0.snupkg         # Core symbols
├── JTest.Cli.1.0.0.nupkg           # CLI tool
└── JTest.Cli.1.0.0.snupkg          # CLI symbols
```

## 🔧 CI/CD Configurations

### GitHub Actions (`.github/workflows/`)

**`build-and-test.yml`** - Main build pipeline
- Triggers on push/PR to main branches
- Builds, tests, and creates packages
- Tests CLI installation and Docker build
- Uploads artifacts

**`release.yml`** - Release pipeline  
- Triggers on version tags (v1.0.0, etc.)
- Creates GitHub releases with packages
- Updates package versions
- Ready for NuGet publishing

### CI/CD Examples (`ci-examples/`)

**`gitlab-ci.yml`** - GitLab CI configuration
- Multi-stage pipeline with caching
- Environment-specific testing
- Package creation and testing
- Docker build verification

**`Jenkinsfile`** - Jenkins pipeline
- Parameterized builds
- Parallel test execution
- Integration testing
- Release automation

**`azure-pipelines.yml`** - Azure DevOps pipeline
- Multi-stage YAML pipeline
- Code coverage reporting
- Environment deployments
- Artifact management

## 🛠️ Local Development

### Prerequisites

- .NET 8.0 SDK
- Git
- Docker (optional, for containerized builds)

### Development Workflow

1. **Quick setup:**
   ```bash
   git clone https://github.com/nexxbiz/JTest.git
   cd JTest
   ./setup.sh
   ```

2. **Development build:**
   ```bash
   dotnet build
   ./src/JTest.Cli/bin/Debug/net8.0/JTest --help
   ```

3. **Create packages:**
   ```bash
   ./scripts/build-packages.sh --clean
   ```

4. **Test installation:**
   ```bash
   dotnet tool install --global --add-source ./packages JTest.Cli
   jtest --help
   ```

## 🐳 Docker Usage

### Building and Running

```bash
# Build image
./docker.sh build

# Run JTest commands
./docker.sh run --help
./docker.sh run run tests/*.json

# Interactive debugging
./docker.sh shell
```

### Docker Compose (for complex scenarios)

```yaml
version: '3.8'
services:
  jtest:
    build: .
    volumes:
      - ./tests:/app/workspace/tests:ro
      - ./results:/app/workspace/results
    environment:
      - API_BASE_URL=https://api.staging.com
      - API_KEY=${API_KEY}
    command: jtest run tests/ --output junit
```

## 🔄 CI/CD Integration

### Quick Integration

For immediate CI/CD integration, use the build scripts:

```yaml
# Any CI system
steps:
  - name: Setup JTest
    run: |
      git clone https://github.com/nexxbiz/JTest.git jtest-repo
      cd jtest-repo
      ./scripts/build-packages.sh
      dotnet tool install --global --add-source ./packages JTest.Cli
      
  - name: Run Tests  
    run: jtest run tests/ --parallel 4
```

### Environment Variables

Common environment variables for CI/CD:

```bash
# API Configuration
API_BASE_URL=https://api.example.com
API_KEY=your-api-key

# JTest Configuration  
JTEST_PARALLEL=4
JTEST_TIMEOUT=300
JTEST_OUTPUT_FORMAT=junit

# Build Configuration
DOTNET_VERSION=8.0.x
BUILD_CONFIGURATION=Release
```

### Artifacts and Caching

**Build Artifacts:**
- `packages/*.nupkg` - NuGet packages
- `junit-results.xml` - Test results
- `src/*/bin/Release/` - Build outputs

**Cache Paths:**
- `.nuget/` - NuGet package cache
- `obj/` - Build intermediate files
- `bin/` - Build outputs

## 🐛 Troubleshooting

### Common Issues

**Build Failures:**
```bash
# Clean and rebuild
./scripts/build-packages.sh --clean

# Check .NET version
dotnet --version
```

**Installation Issues:**
```bash
# Check tool installation
dotnet tool list --global

# Verify PATH
echo $PATH | grep -q dotnet && echo "✓ dotnet tools in PATH" || echo "✗ Add ~/.dotnet/tools to PATH"

# Reinstall
dotnet tool uninstall --global JTest.Cli
./setup.sh
```

**Docker Issues:**
```bash
# Clean Docker resources
./docker.sh clean

# Rebuild with no cache
docker build --no-cache -t jtest .
```

### Debug Mode

Enable verbose logging:

```bash
# Local debugging
jtest debug my-test.json --env verbosity=Verbose

# CI debugging
export JTEST_DEBUG=true
jtest run tests/
```

## 📚 Additional Resources

- **[Installation Guide](../INSTALLATION.md)** - Complete installation options
- **[Documentation](../docs/README.md)** - Full JTest documentation
- **[Examples](../docs/examples/)** - Test examples and patterns
- **[Contributing](../CONTRIBUTING.md)** - Development guidelines