#!/bin/bash
# JTest Version Manager - Query and download different versions

set -e

GITHUB_REPO="nexxbiz/JTest"
GITHUB_API="https://api.github.com/repos/$GITHUB_REPO"

show_help() {
    echo "JTest Version Manager"
    echo "===================="
    echo ""
    echo "Usage: $0 <command> [options]"
    echo ""
    echo "Commands:"
    echo "  list                    List all available versions"
    echo "  latest                  Show latest stable version"
    echo "  dev                     Show latest development version"
    echo "  download <version>      Download specific version"
    echo "  install <version>       Install specific version globally"
    echo "  info <version>          Show version information"
    echo ""
    echo "Examples:"
    echo "  $0 list                        # List all versions"
    echo "  $0 latest                      # Show latest stable release"
    echo "  $0 dev                         # Show latest development build"
    echo "  $0 download v1.0.0             # Download v1.0.0"
    echo "  $0 download development        # Download latest dev build"
    echo "  $0 install v1.0.0              # Install v1.0.0 globally"
    echo "  $0 info development            # Show development build info"
}

list_versions() {
    echo "📦 Available JTest Versions:"
    echo ""
    
    # Get stable releases
    echo "🚀 Stable Releases:"
    curl -s "$GITHUB_API/releases" | \
        jq -r '.[] | select(.prerelease == false) | "  \(.tag_name) - \(.name) (\(.published_at[:10]))"' | \
        head -10
    
    echo ""
    echo "🚧 Development Builds:"
    curl -s "$GITHUB_API/releases" | \
        jq -r '.[] | select(.prerelease == true) | "  \(.tag_name) - \(.name) (\(.published_at[:10]))"' | \
        head -5
    
    echo ""
    echo "Use '$0 info <version>' for detailed information about a specific version."
}

get_latest_stable() {
    curl -s "$GITHUB_API/releases/latest" | jq -r '.tag_name'
}

get_latest_dev() {
    curl -s "$GITHUB_API/releases" | \
        jq -r '.[] | select(.prerelease == true) | .tag_name' | \
        head -1
}

get_version_info() {
    local version="$1"
    
    if [ "$version" = "latest" ]; then
        version=$(get_latest_stable)
    elif [ "$version" = "development" ] || [ "$version" = "dev" ]; then
        version="development"
    fi
    
    echo "📋 Version Information: $version"
    echo ""
    
    local release_info=$(curl -s "$GITHUB_API/releases/tags/$version")
    
    if echo "$release_info" | jq -e '.message' >/dev/null 2>&1; then
        echo "❌ Version $version not found"
        return 1
    fi
    
    echo "Name: $(echo "$release_info" | jq -r '.name')"
    echo "Tag: $(echo "$release_info" | jq -r '.tag_name')"
    echo "Published: $(echo "$release_info" | jq -r '.published_at')"
    echo "Prerelease: $(echo "$release_info" | jq -r '.prerelease')"
    echo ""
    
    echo "📥 Available Downloads:"
    echo "$release_info" | jq -r '.assets[] | "  \(.name) (\(.size) bytes) - \(.download_url)"'
    
    echo ""
    echo "📖 Release Notes:"
    echo "$release_info" | jq -r '.body' | head -20
}

download_version() {
    local version="$1"
    local download_dir="${2:-./jtest-$version}"
    
    if [ "$version" = "latest" ]; then
        version=$(get_latest_stable)
    elif [ "$version" = "development" ] || [ "$version" = "dev" ]; then
        version="development"
    fi
    
    echo "📥 Downloading JTest $version..."
    
    local release_info=$(curl -s "$GITHUB_API/releases/tags/$version")
    
    if echo "$release_info" | jq -e '.message' >/dev/null 2>&1; then
        echo "❌ Version $version not found"
        return 1
    fi
    
    mkdir -p "$download_dir"
    cd "$download_dir"
    
    # Download the main distribution package
    local main_package=$(echo "$release_info" | jq -r '.assets[] | select(.name | test("jtest-.*\\.zip$")) | .browser_download_url' | head -1)
    
    if [ -n "$main_package" ] && [ "$main_package" != "null" ]; then
        echo "Downloading main package..."
        curl -L -o "jtest-$version.zip" "$main_package"
        unzip -q "jtest-$version.zip"
        rm "jtest-$version.zip"
    fi
    
    # Download version info if available
    local version_info=$(echo "$release_info" | jq -r '.assets[] | select(.name == "version.json") | .browser_download_url')
    if [ -n "$version_info" ] && [ "$version_info" != "null" ]; then
        curl -L -o "version.json" "$version_info"
    fi
    
    cd - > /dev/null
    
    echo "✅ Downloaded to: $download_dir"
    echo ""
    echo "🚀 To install globally:"
    echo "  dotnet tool install --global --add-source $download_dir JTest.Cli"
}

install_version() {
    local version="$1"
    local temp_dir="/tmp/jtest-install-$version"
    
    echo "🔧 Installing JTest $version globally..."
    
    # Download to temporary directory
    download_version "$version" "$temp_dir"
    
    # Install the CLI tool
    if [ -f "$temp_dir"/JTest.Cli.*.nupkg ]; then
        echo "Installing CLI tool..."
        dotnet tool install --global --add-source "$temp_dir" JTest.Cli --prerelease || \
        dotnet tool update --global --add-source "$temp_dir" JTest.Cli --prerelease
        
        echo "✅ JTest $version installed successfully!"
        echo ""
        echo "Verify installation:"
        jtest --help
    else
        echo "❌ Could not find JTest.Cli package in download"
        return 1
    fi
    
    # Clean up
    rm -rf "$temp_dir"
}

# Main script logic
case "${1:-}" in
    list|ls)
        list_versions
        ;;
    latest)
        echo "Latest stable version: $(get_latest_stable)"
        ;;
    dev|development)
        echo "Latest development version: $(get_latest_dev)"
        ;;
    info)
        if [ -z "$2" ]; then
            echo "Usage: $0 info <version>"
            exit 1
        fi
        get_version_info "$2"
        ;;
    download|dl)
        if [ -z "$2" ]; then
            echo "Usage: $0 download <version> [directory]"
            exit 1
        fi
        download_version "$2" "$3"
        ;;
    install)
        if [ -z "$2" ]; then
            echo "Usage: $0 install <version>"
            exit 1
        fi
        install_version "$2"
        ;;
    help|--help|-h|"")
        show_help
        ;;
    *)
        echo "Unknown command: $1"
        echo ""
        show_help
        exit 1
        ;;
esac