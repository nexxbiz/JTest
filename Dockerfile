# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files
COPY src/JTest.Core/JTest.Core.csproj src/JTest.Core/
COPY src/JTest.Cli/JTest.Cli.csproj src/JTest.Cli/

# Copy solution file
COPY JTest.sln ./

# Restore dependencies for specific projects
RUN dotnet restore src/JTest.Core/JTest.Core.csproj
RUN dotnet restore src/JTest.Cli/JTest.Cli.csproj

# Copy source code
COPY src/ src/

# Build and pack
RUN dotnet build --configuration Release --no-restore
RUN dotnet pack src/JTest.Cli/JTest.Cli.csproj \
    --configuration Release \
    --output /packages \
    --no-build

# Runtime stage
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS runtime
WORKDIR /app

# Install dotnet CLI tools support
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Copy the package and install tool
COPY --from=build /packages/*.nupkg /packages/
RUN dotnet tool install --global --add-source /packages JTest.Cli
ENV PATH="$PATH:/root/.dotnet/tools"

# Create directories for test files
RUN mkdir -p /app/tests /app/templates /app/environments /app/output

# Set working directory for tests
WORKDIR /app

# Default command
CMD ["jtest", "--help"]