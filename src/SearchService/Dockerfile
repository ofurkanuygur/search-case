# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files
COPY SearchCase.sln .
COPY src/SearchCase.Search.Contracts/SearchCase.Search.Contracts.csproj ./src/SearchCase.Search.Contracts/
COPY src/SearchService/SearchService.csproj ./src/SearchService/

# Restore dependencies
RUN dotnet restore src/SearchService/SearchService.csproj

# Copy source code
COPY src/SearchCase.Search.Contracts/ ./src/SearchCase.Search.Contracts/
COPY src/SearchService/ ./src/SearchService/

# Build and publish
WORKDIR /src/src/SearchService
RUN dotnet publish -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Create non-root user
RUN groupadd -r searchservice && useradd -r -g searchservice searchservice

# Copy published output
COPY --from=build /app/publish .

# Create logs directory
RUN mkdir -p /app/logs && chown -R searchservice:searchservice /app

# Switch to non-root user
USER searchservice

EXPOSE 8080

ENTRYPOINT ["dotnet", "SearchService.dll"]
