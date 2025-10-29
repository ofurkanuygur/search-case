# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files
COPY SearchCase.sln .
COPY src/SearchCase.Search.Contracts/SearchCase.Search.Contracts.csproj ./src/SearchCase.Search.Contracts/
COPY src/Dashboard/Dashboard.csproj ./src/Dashboard/

# Restore dependencies
RUN dotnet restore src/Dashboard/Dashboard.csproj

# Copy source code
COPY src/SearchCase.Search.Contracts/ ./src/SearchCase.Search.Contracts/
COPY src/Dashboard/ ./src/Dashboard/

# Build and publish
WORKDIR /src/src/Dashboard
RUN dotnet publish -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Create non-root user
RUN groupadd -r dashboard && useradd -r -g dashboard dashboard

# Copy published output
COPY --from=build /app/publish .

# Switch to non-root user
USER dashboard

EXPOSE 8080

ENTRYPOINT ["dotnet", "Dashboard.dll"]
