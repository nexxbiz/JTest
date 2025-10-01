#!/bin/bash
# Docker helper script for JTest

set -e

IMAGE_NAME="jtest"
TAG="latest"
CONTAINER_NAME="jtest-container"

show_help() {
    echo "JTest Docker Helper"
    echo "=================="
    echo ""
    echo "Usage: $0 <command> [options]"
    echo ""
    echo "Commands:"
    echo "  build              Build the JTest Docker image"
    echo "  run <args>         Run JTest in a container with provided arguments"
    echo "  shell              Start an interactive shell in the container"
    echo "  test <test-file>   Run a specific test file (mounts current directory)"
    echo "  clean              Remove JTest Docker image and containers"
    echo ""
    echo "Examples:"
    echo "  $0 build                           # Build the Docker image"
    echo "  $0 run --help                     # Show JTest help"
    echo "  $0 test my-test.json              # Run a test file from current directory"
    echo "  $0 run run tests/*.json --parallel 4  # Run all tests in parallel"
    echo "  $0 shell                          # Interactive shell for debugging"
}

build_image() {
    echo "🔧 Building JTest Docker image..."
    docker build -t "$IMAGE_NAME:$TAG" .
    echo "✅ Docker image built successfully: $IMAGE_NAME:$TAG"
}

run_container() {
    local args="$*"
    echo "🚀 Running JTest in container..."
    docker run --rm \
        -v "$(pwd):/app/workspace" \
        -w /app/workspace \
        "$IMAGE_NAME:$TAG" \
        jtest $args
}

run_shell() {
    echo "🐚 Starting interactive shell..."
    docker run --rm -it \
        -v "$(pwd):/app/workspace" \
        -w /app/workspace \
        "$IMAGE_NAME:$TAG" \
        /bin/bash
}

test_file() {
    local test_file="$1"
    if [ ! -f "$test_file" ]; then
        echo "❌ Test file not found: $test_file"
        exit 1
    fi
    
    echo "🧪 Running test file: $test_file"
    docker run --rm \
        -v "$(pwd):/app/workspace" \
        -w /app/workspace \
        "$IMAGE_NAME:$TAG" \
        jtest run "$test_file"
}

clean_docker() {
    echo "🧹 Cleaning up Docker resources..."
    docker rm -f "$CONTAINER_NAME" 2>/dev/null || true
    docker rmi "$IMAGE_NAME:$TAG" 2>/dev/null || true
    echo "✅ Cleanup completed"
}

case "${1:-}" in
    build)
        build_image
        ;;
    run)
        shift
        run_container "$@"
        ;;
    shell)
        run_shell
        ;;
    test)
        if [ -z "$2" ]; then
            echo "❌ Please specify a test file"
            echo "Usage: $0 test <test-file>"
            exit 1
        fi
        test_file "$2"
        ;;
    clean)
        clean_docker
        ;;
    help|--help|-h|"")
        show_help
        ;;
    *)
        echo "❌ Unknown command: $1"
        echo ""
        show_help
        exit 1
        ;;
esac