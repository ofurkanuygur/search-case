# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy solution and project files for better caching
COPY ["src/EventBusService/EventBusService.csproj", "src/EventBusService/"]
COPY ["src/EventBusContracts/EventBusContracts.csproj", "src/EventBusContracts/"]
COPY ["SearchCase.sln", "./"]

# Restore dependencies
RUN dotnet restore "src/EventBusService/EventBusService.csproj"

# Copy source code
COPY ["src/EventBusService/", "src/EventBusService/"]
COPY ["src/EventBusContracts/", "src/EventBusContracts/"]

# Build and publish
WORKDIR "/src/src/EventBusService"
RUN dotnet build "EventBusService.csproj" -c $BUILD_CONFIGURATION -o /app/build
RUN dotnet publish "EventBusService.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Create non-root user
RUN groupadd -r appuser && useradd -r -g appuser appuser

WORKDIR /app
COPY --from=build /app/publish .

# Create logs directory and set permissions
RUN mkdir -p /app/logs && \
    chown -R appuser:appuser /app

USER appuser

EXPOSE 8080
ENTRYPOINT ["dotnet", "EventBusService.dll"]