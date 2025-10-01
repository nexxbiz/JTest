#!/usr/bin/env pwsh
# Build script for creating JTest NuGet packages

param(
    [Parameter(Mandatory=$false)]
    [string]$Configuration = "Release",
    
    [Parameter(Mandatory=$false)]
    [string]$OutputPath = "./packages",
    
    [Parameter(Mandatory=$false)]
    [switch]$Clean = $false
)

$ErrorActionPreference = "Stop"

Write-Host "🔧 Building JTest Packages" -ForegroundColor Green
Write-Host "Configuration: $Configuration" -ForegroundColor Yellow
Write-Host "Output Path: $OutputPath" -ForegroundColor Yellow

# Change to repo root
$repoRoot = Split-Path -Parent $PSScriptRoot
Push-Location $repoRoot

try {
    # Clean if requested
    if ($Clean) {
        Write-Host "🧹 Cleaning previous builds..." -ForegroundColor Yellow
        dotnet clean --configuration $Configuration
        if (Test-Path $OutputPath) {
            Remove-Item $OutputPath -Recurse -Force
        }
    }

    # Create output directory
    New-Item -ItemType Directory -Path $OutputPath -Force | Out-Null

    # Restore dependencies
    Write-Host "📦 Restoring dependencies..." -ForegroundColor Yellow
    dotnet restore

    # Build solution first
    Write-Host "🔨 Building solution..." -ForegroundColor Yellow
    dotnet build --configuration $Configuration --no-restore

    # Build Core package
    Write-Host "🏗️  Building JTest.Core package..." -ForegroundColor Yellow
    dotnet pack src/JTest.Core/JTest.Core.csproj `
        --configuration $Configuration `
        --output $OutputPath `
        --no-build

    # Build CLI tool package
    Write-Host "🏗️  Building JTest.Cli tool package..." -ForegroundColor Yellow
    dotnet pack src/JTest.Cli/JTest.Cli.csproj `
        --configuration $Configuration `
        --output $OutputPath `
        --no-build

    Write-Host "✅ Package build completed successfully!" -ForegroundColor Green
    Write-Host "📦 Packages created in: $OutputPath" -ForegroundColor Green
    
    # List created packages
    Get-ChildItem $OutputPath -Filter "*.nupkg" | ForEach-Object {
        Write-Host "   • $($_.Name)" -ForegroundColor Cyan
    }
    
    Write-Host ""
    Write-Host "🚀 To install the CLI tool locally:" -ForegroundColor Yellow
    Write-Host "   dotnet tool install --global --add-source $OutputPath JTest.Cli" -ForegroundColor White
    Write-Host ""
    Write-Host "🧪 To test the package before publishing:" -ForegroundColor Yellow
    Write-Host "   dotnet tool install --global --add-source $OutputPath JTest.Cli --version 1.0.0" -ForegroundColor White
    
} catch {
    Write-Host "❌ Build failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
} finally {
    Pop-Location
}