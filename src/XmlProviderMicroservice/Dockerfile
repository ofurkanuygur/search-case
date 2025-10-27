# syntax=docker/dockerfile:1

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
WORKDIR /src

# Copy solution and project files
COPY src/SearchCase.Contracts/SearchCase.Contracts.csproj ./SearchCase.Contracts/
COPY src/XmlProviderMicroservice/XmlProviderMicroservice.csproj ./XmlProviderMicroservice/
RUN dotnet restore "./XmlProviderMicroservice/XmlProviderMicroservice.csproj"

# Copy source code
COPY src/SearchCase.Contracts/ ./SearchCase.Contracts/
COPY src/XmlProviderMicroservice/ ./XmlProviderMicroservice/

# Build and publish
WORKDIR /src/XmlProviderMicroservice
RUN dotnet publish "XmlProviderMicroservice.csproj" \
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
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Set entry point
ENTRYPOINT ["dotnet", "XmlProviderMicroservice.dll"]
