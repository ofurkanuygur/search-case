# syntax=docker/dockerfile:1

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
ARG BUILDCONFIG=Release
WORKDIR /src

# Copy csproj and restore dependencies (layer caching)
COPY ["HangfireWorker.csproj", "./"]
RUN dotnet restore "HangfireWorker.csproj"

# Copy source code
COPY . .

# Build and publish in one step
RUN dotnet publish "HangfireWorker.csproj" \
    -c $BUILDCONFIG \
    -o /app/publish \
    /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS final

# Install dependencies for health checks and ICU libraries
RUN apk add --no-cache curl icu-libs icu-data-full

# Create non-root user for security
RUN addgroup -g 1000 hangfire && \
    adduser -D -u 1000 -G hangfire hangfire

WORKDIR /app

# Copy published app from build stage
COPY --from=build /app/publish .

# Create logs directory with correct permissions
RUN mkdir -p /app/logs && \
    chown -R hangfire:hangfire /app

# Switch to non-root user
USER hangfire

# Expose port
EXPOSE 80

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
    CMD curl -f http://localhost:80/health || exit 1

# Set environment variables
ENV ASPNETCORE_URLS=http://+:80 \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

# Entry point
ENTRYPOINT ["dotnet", "SearchCase.HangfireWorker.dll"]
