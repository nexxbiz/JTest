#!/usr/bin/env pwsh
# Quick setup script for JTest - builds and installs locally (Windows/PowerShell)

$ErrorActionPreference = "Stop"

Write-Host "🚀 JTest Quick Setup" -ForegroundColor Green
Write-Host "====================" -ForegroundColor Green

# Check if .NET is installed
try {
    $dotnetVersion = dotnet --version
    Write-Host "✅ .NET SDK version: $dotnetVersion" -ForegroundColor Green
} catch {
    Write-Host "❌ .NET SDK is not installed. Please install .NET 10.0 SDK first:" -ForegroundColor Red
    Write-Host "   https://dotnet.microsoft.com/download" -ForegroundColor Yellow
    exit 1
}

# Get repo root directory
$repoRoot = Split-Path -Parent $PSScriptRoot
Push-Location $repoRoot

try {
    Write-Host "📁 Working directory: $repoRoot" -ForegroundColor Yellow

    # Build the packages
    Write-Host "🔧 Building JTest packages..." -ForegroundColor Yellow
    & "$repoRoot/scripts/build-packages.ps1" -Clean

    # Check if jtest is already installed
    try {
        $jtestVersion = jtest --version 2>$null
        if ($jtestVersion) {
            Write-Host "⚠️  JTest CLI tool is already installed. Uninstalling old version..." -ForegroundColor Yellow
            try {
                dotnet tool uninstall --global JTest.Cli
            } catch {
                # Ignore uninstall errors
            }
        }
    } catch {
        # jtest not installed, continue
    }

    # Install the CLI tool locally
    Write-Host "📦 Installing JTest CLI tool..." -ForegroundColor Yellow
    dotnet tool install --global --add-source "./packages" JTest.Cli

    # Verify installation
    Write-Host "🧪 Verifying installation..." -ForegroundColor Yellow
    try {
        $testVersion = jtest --version 2>$null
        if ($testVersion) {
            Write-Host "✅ JTest CLI tool installed successfully!" -ForegroundColor Green
            Write-Host ""
            Write-Host "🎉 Setup complete! You can now use 'jtest' command globally." -ForegroundColor Green
            Write-Host ""
            Write-Host "📖 Quick start:" -ForegroundColor Yellow
            Write-Host "   jtest --help                    # Show help" -ForegroundColor White
            Write-Host "   jtest create `"My First Test`"    # Create a new test" -ForegroundColor White
            Write-Host "   jtest run my-test.json          # Run a test file" -ForegroundColor White
            Write-Host ""
            Write-Host "📚 Documentation: https://github.com/nexxbiz/JTest/blob/main/docs/README.md" -ForegroundColor Cyan
        } else {
            throw "JTest command not found after installation"
        }
    } catch {
        Write-Host "❌ Installation verification failed. JTest command not found." -ForegroundColor Red
        Write-Host "   Make sure dotnet tools are in your PATH" -ForegroundColor Yellow
        Write-Host "   You may need to restart your terminal or add the tools directory to your PATH" -ForegroundColor Yellow
        exit 1
    }
} catch {
    Write-Host "❌ Setup failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
} finally {
    Pop-Location
}