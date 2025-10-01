#!/bin/bash
# Build script for creating JTest NuGet packages (Bash version)

set -e

CONFIGURATION="Release"
OUTPUT_PATH="./packages"
CLEAN=false

# Parse command line arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        -c|--configuration)
            CONFIGURATION="$2"
            shift 2
            ;;
        -o|--output)
            OUTPUT_PATH="$2"
            shift 2
            ;;
        --clean)
            CLEAN=true
            shift
            ;;
        -h|--help)
            echo "Usage: $0 [OPTIONS]"
            echo "Options:"
            echo "  -c, --configuration   Build configuration (default: Release)"
            echo "  -o, --output         Output path for packages (default: ./packages)"
            echo "  --clean              Clean before building"
            echo "  -h, --help           Show this help message"
            exit 0
            ;;
        *)
            echo "Unknown option: $1"
            exit 1
            ;;
    esac
done

echo "🔧 Building JTest Packages"
echo "Configuration: $CONFIGURATION"
echo "Output Path: $OUTPUT_PATH"

# Get repo root directory
REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$REPO_ROOT"

# Clean if requested
if [ "$CLEAN" = true ]; then
    echo "🧹 Cleaning previous builds..."
    dotnet clean --configuration "$CONFIGURATION"
    rm -rf "$OUTPUT_PATH"
fi

# Create output directory
mkdir -p "$OUTPUT_PATH"

# Restore dependencies
echo "📦 Restoring dependencies..."
dotnet restore

# Build solution first
echo "🔨 Building solution..."
dotnet build --configuration "$CONFIGURATION" --no-restore

# Build Core package
echo "🏗️  Building JTest.Core package..."
dotnet pack src/JTest.Core/JTest.Core.csproj \
    --configuration "$CONFIGURATION" \
    --output "$OUTPUT_PATH" \
    --no-build

# Build CLI tool package
echo "🏗️  Building JTest.Cli tool package..."
dotnet pack src/JTest.Cli/JTest.Cli.csproj \
    --configuration "$CONFIGURATION" \
    --output "$OUTPUT_PATH" \
    --no-build

echo "✅ Package build completed successfully!"
echo "📦 Packages created in: $OUTPUT_PATH"

# List created packages
echo "Created packages:"
for pkg in "$OUTPUT_PATH"/*.nupkg; do
    if [ -f "$pkg" ]; then
        echo "   • $(basename "$pkg")"
    fi
done

echo ""
echo "🚀 To install the CLI tool locally:"
echo "   dotnet tool install --global --add-source $OUTPUT_PATH JTest.Cli"
echo ""
echo "🧪 To test the package before publishing:"
echo "   dotnet tool install --global --add-source $OUTPUT_PATH JTest.Cli --version 1.0.0"