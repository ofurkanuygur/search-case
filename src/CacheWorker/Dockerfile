# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["src/CacheWorker/CacheWorker.csproj", "src/CacheWorker/"]
COPY ["src/EventBusContracts/EventBusContracts.csproj", "src/EventBusContracts/"]
RUN dotnet restore "src/CacheWorker/CacheWorker.csproj"

# Copy everything else and build
COPY ["src/CacheWorker/", "src/CacheWorker/"]
COPY ["src/EventBusContracts/", "src/EventBusContracts/"]
WORKDIR "/src/src/CacheWorker"
RUN dotnet build "CacheWorker.csproj" -c Release -o /app/build
RUN dotnet publish "CacheWorker.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Create non-root user
RUN addgroup --system appgroup && adduser --system appuser --ingroup appgroup

# Copy published output
COPY --from=build --chown=appuser:appgroup /app/publish .

# Switch to non-root user
USER appuser

# Expose port
EXPOSE 8080

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1

# Run the application
ENTRYPOINT ["dotnet", "CacheWorker.dll"]