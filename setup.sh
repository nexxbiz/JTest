#!/bin/bash
# Quick setup script for JTest - builds and installs locally

set -e

echo "🚀 JTest Quick Setup"
echo "===================="

# Check if .NET is installed
if ! command -v dotnet &> /dev/null; then
    echo "❌ .NET SDK is not installed. Please install .NET 8.0 SDK first:"
    echo "   https://dotnet.microsoft.com/download"
    exit 1
fi

# Check .NET version
DOTNET_VERSION=$(dotnet --version)
echo "✅ .NET SDK version: $DOTNET_VERSION"

# Get repo root directory
REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$REPO_ROOT"

echo "📁 Working directory: $REPO_ROOT"

# Build the packages
echo "🔧 Building JTest packages..."
"$REPO_ROOT/scripts/build-packages.sh" --clean

# Check if jtest is already installed
if command -v jtest &> /dev/null; then
    echo "⚠️  JTest CLI tool is already installed. Uninstalling old version..."
    dotnet tool uninstall --global JTest.Cli || true
fi

# Install the CLI tool locally
echo "📦 Installing JTest CLI tool..."
dotnet tool install --global --add-source ./packages JTest.Cli

# Verify installation
echo "🧪 Verifying installation..."
if command -v jtest &> /dev/null; then
    echo "✅ JTest CLI tool installed successfully!"
    echo ""
    echo "🎉 Setup complete! You can now use 'jtest' command globally."
    echo ""
    echo "📖 Quick start:"
    echo "   jtest --help                    # Show help"
    echo "   jtest create \"My First Test\"    # Create a new test"
    echo "   jtest run my-test.json          # Run a test file"
    echo ""
    echo "📚 Documentation: https://github.com/nexxbiz/JTest/blob/main/docs/README.md"
else
    echo "❌ Installation verification failed. JTest command not found."
    echo "   Make sure ~/.dotnet/tools is in your PATH"
    echo "   You may need to restart your terminal or run:"
    echo "   export PATH=\"\$PATH:\$HOME/.dotnet/tools\""
    exit 1
fi