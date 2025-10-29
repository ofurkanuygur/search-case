# syntax=docker/dockerfile:1

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
WORKDIR /src

# Copy solution and project files
COPY src/EventBusContracts/EventBusContracts.csproj ./EventBusContracts/
COPY src/TimeService/TimeService.csproj ./TimeService/
RUN dotnet restore "./TimeService/TimeService.csproj"

# Copy source code
COPY src/EventBusContracts/ ./EventBusContracts/
COPY src/TimeService/ ./TimeService/

# Build and publish
WORKDIR /src/TimeService
RUN dotnet publish "TimeService.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS final

# Install dependencies for health checks
RUN apk add --no-cache curl

# Create non-root user
RUN addgroup -g 1000 appuser && \
    adduser -D -u 1000 -G appuser appuser

WORKDIR /app

# Copy published app
COPY --from=build /app/publish .

# Create logs directory and set permissions
RUN mkdir -p /app/logs && \
    chown -R appuser:appuser /app

# Switch to non-root user
USER appuser

# Expose port
EXPOSE 8080

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Set entry point
ENTRYPOINT ["dotnet", "TimeService.dll"]
