# Multi-stage build for SearchWorker

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["src/SearchWorker/SearchWorker.csproj", "src/SearchWorker/"]
COPY ["src/EventBusContracts/EventBusContracts.csproj", "src/EventBusContracts/"]
RUN dotnet restore "src/SearchWorker/SearchWorker.csproj"

# Copy everything and build
COPY ["src/SearchWorker/", "src/SearchWorker/"]
COPY ["src/EventBusContracts/", "src/EventBusContracts/"]
WORKDIR "/src/src/SearchWorker"
RUN dotnet build "SearchWorker.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "SearchWorker.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Create non-root user
RUN groupadd -r appuser && useradd -r -g appuser appuser
USER appuser

# Copy published app
COPY --from=publish /app/publish .

# Environment variables
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
ENV ASPNETCORE_URLS=http://+:8080

# Expose port
EXPOSE 8080

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=30s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Entry point
ENTRYPOINT ["dotnet", "SearchWorker.dll"]