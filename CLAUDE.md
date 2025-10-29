# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

SearchCase is a microservices-based content aggregation system that fetches data from multiple external providers, transforms it into a canonical format, performs change detection, and provides full-text search capabilities with event-driven architecture. The system consists of:

### Microservices Layer
1. **JsonProviderMicroservice** - JSON provider integration (.NET 9.0, Port 8001)
2. **XmlProviderMicroservice** - XML provider integration (.NET 9.0, Port 8002)
3. **WriteService** - Content synchronization with change detection (ASP.NET Core 9.0, Port 8003)
4. **EventBusService** - REST API wrapper for RabbitMQ (ASP.NET Core 8.0, Port 8004)
5. **SearchWorker** - RabbitMQ consumer and Elasticsearch indexer (ASP.NET Core 8.0, Port 8005)
6. **CacheWorker** - Redis caching service (ASP.NET Core 8.0, Port 8006)
7. **SearchService** - Search API with Elasticsearch/Redis (ASP.NET Core 8.0, Port 8007)
8. **Dashboard** - Web UI for search (ASP.NET Core Razor Pages, Port 8008)
9. **HangfireWorker** - Legacy background job scheduler (ASP.NET Core 8.0, Port 5100) - Being deprecated

### Shared Libraries
- **SearchCase.Contracts** - Canonical data models and provider contracts (.NET 9.0)
- **SearchCase.Search.Contracts** - Search-specific DTOs and validators (.NET 8.0)

### Infrastructure
- **PostgreSQL** (Port 5433) - Primary database (hangfire, searchcase schemas)
- **RabbitMQ** (Ports 5672/15672) - Message broker
- **Elasticsearch** (Port 9200) - Full-text search engine
- **Kibana** (Port 5601) - Elasticsearch visualization
- **Redis** (Port 6379) - Caching layer

## Architecture

### Canonical Schema Pattern

The system implements a **canonical schema transformation** pattern where external provider data is normalized into shared contracts:

- **CanonicalContent** (abstract base class): Common fields (Id, Title, PublishedAt, Categories, SourceProvider)
  - **CanonicalVideoContent**: Video-specific metrics (Views, Likes, Duration)
  - **CanonicalArticleContent**: Article-specific metrics (ReadingTimeMinutes, Reactions, Comments)
- Uses JSON polymorphism with discriminated unions (`"type": "video"` or `"type": "article"`)
- All transformations go through `IContentMapper<TSource>` interface
- FluentValidation validates canonical content before returning to clients

### Provider Microservices

Each provider microservice follows this flow:
1. **HttpClient with Polly** - Fetch from external API with retry/circuit breaker patterns
2. **Mapper** - Transform provider-specific DTOs to canonical format
3. **Validator** - Validate using FluentValidation
4. **ProviderService** - Orchestrate the fetch-transform-validate pipeline
5. **Controller** - Expose REST endpoint `GET /api/provider/data`

Provider microservices are **stateless** and expose:
- `GET /api/provider/data` - Fetch and transform data
- `/health` - Health checks
- `/swagger` - OpenAPI documentation (always enabled)

### Hangfire Worker & WriteService

The system has two Hangfire-based services:

#### Original HangfireWorker (Being Deprecated)
- **FrequentJob**: Runs every 5 minutes, calls JsonProviderMicroservice
- **DailyJob**: Runs daily at 02:00 UTC, calls XmlProviderMicroservice (configurable)
- Uses PostgreSQL for job persistence with schema `hangfire.*`
- Dashboard available at `http://localhost:5100/hangfire` (when running)

#### New WriteService (Active)
WriteService is the primary content synchronization service that orchestrates the entire data flow pipeline.

**How It's Triggered:**
1. **On Startup**: Runs `ContentSyncJob` once immediately when the service starts (for initial data load)
2. **Scheduled Recurring Job**: Automatically runs every 5 minutes via Hangfire's `RecurringJob.AddOrUpdate`
3. **Configurable Schedule**: Cron expression defined in `appsettings.json` → `Hangfire:SyncJobCronExpression` (default: `*/5 * * * *`)

**Jobs:**
- **ContentSyncJob** (Every 5 minutes):
  1. Fetches content from all provider microservices via `IProviderClient`
  2. Converts canonical content to domain entities
  3. Uses `ContentSyncOrchestrator` for synchronized operations:
     - Hash-based change detection (compares `ContentHash`)
     - Inserts new content or updates changed content
     - Logs changes to `ContentChangeLogs` audit table
  4. Calculates freshness scores only for changed items (performance optimization)
  5. **Publishes events** to EventBus via `IEventBusClient.PublishContentChangedAsync`
     - Sends batch events with created/updated content IDs
     - EventBus forwards to RabbitMQ → SearchWorker → Elasticsearch

- **FreshnessScoreUpdateJob** (Daily at 02:00 UTC):
  - Recalculates freshness scores for all content
  - Uses decay algorithm based on `PublishedAt` timestamp
  - Maintains search relevance over time

**Key Features:**
- Hash-based change detection (avoids unnecessary updates)
- Event-driven architecture (publishes to RabbitMQ via EventBusService)
- Audit logging (all changes tracked in `ContentChangeLogs`)
- Circuit breaker pattern for EventBus communication (Polly)
- Dashboard available at `http://localhost:8003/hangfire`
- Uses Entity Framework Core for content persistence

**Database Tables:**
- `contents` - Main content storage
- `content_change_logs` - Audit trail of all changes
- `sync_batches` - Tracks synchronization operations

### Event-Driven Architecture

#### EventBusService (Port 8004)
REST API wrapper for RabbitMQ event publishing:
- **POST /api/events/content-changed**: Publishes batch content change events
- Circuit breaker pattern with Polly for resilience
- Publishes to RabbitMQ exchange: `content-events`
- Routing key: `content.changed`

#### SearchWorker (Port 8005)
Background worker that consumes RabbitMQ messages and indexes to Elasticsearch:
- Consumes from queue: `content-changed-queue`
- Batch indexing for performance (configurable batch size)
- Error handling with dead letter queue (DLQ)
- Automatic index creation and mapping
- Health checks for RabbitMQ and Elasticsearch connectivity

**Event Flow:**
```
WriteService → EventBusService → RabbitMQ → SearchWorker → Elasticsearch
```

#### CacheWorker (Port 8006)
Redis caching service for hot data:
- Consumes `content-batch-updated` events from RabbitMQ
- Updates Redis cache with latest content
- Key schema: `content:{contentId}`
- TTL-based expiration
- Cache invalidation on updates

### Search Architecture

#### SearchService (Port 8007)
Unified search API with multiple strategies:

**Search Strategies (Strategy Pattern):**
1. **ElasticsearchSearchStrategy**: Full-text search with advanced queries
2. **RedisSearchStrategy**: Fast cache lookups for hot content
3. **HybridSearchStrategy**: Combines both (checks Redis first, falls back to Elasticsearch)

**Endpoints:**
- `GET /api/search`: Search with filters (query, type, categories, sort, pagination)
- Supports sorting by relevance, date, or score
- Pagination with page/pageSize
- Returns: SearchResult with items, pagination, and metadata

**Key Features:**
- Strategy pattern for flexible search implementations
- FluentValidation for request validation
- Circuit breaker for Elasticsearch
- Response caching

#### SearchCase.Search.Contracts
Shared library for search operations:
- **DTOs**: `SearchRequest`, `SearchResult`, `ContentDto`
- **Enums**: `ContentType`, `SortBy`
- **Validators**: `SearchRequestValidator` with FluentValidation
- Used by both SearchService and Dashboard

#### Dashboard (Port 8008)
ASP.NET Core Razor Pages web UI:
- Search interface with filters
- Bootstrap responsive design
- Real-time search via SearchService API
- Displays results with pagination
- Categories and content type filtering

**Pages:**
- `/` - Homepage
- `/Search` - Search interface
- Uses `ISearchServiceClient` to communicate with SearchService

## Data Flow Overview

```
1. Providers (JSON/XML) → Fetch external data
2. WriteService → Sync, detect changes, save to PostgreSQL
3. WriteService → Publish events to EventBusService
4. EventBusService → Forward to RabbitMQ
5. SearchWorker → Consume events, index to Elasticsearch
6. CacheWorker → Consume events, update Redis cache
7. SearchService → Query Elasticsearch/Redis via strategies
8. Dashboard → Display search results to users
```

## Common Commands

### Build & Run

```bash
# Build entire solution
dotnet build SearchCase.sln

# Restore dependencies
dotnet restore SearchCase.sln

# Run specific project (local development)
dotnet run --project src/HangfireWorker
dotnet run --project src/JsonProviderMicroservice
dotnet run --project src/XmlProviderMicroservice

# Clean build artifacts
make dotnet-clean
# or
dotnet clean SearchCase.sln
```

### Docker Operations

```bash
# Start all services (recommended)
make up
# or
docker-compose up -d

# View logs
make logs-worker          # Hangfire worker logs
make logs-db             # PostgreSQL logs
docker-compose logs -f   # All services

# Stop services
make down

# Restart services
make restart

# Check health
make health
curl http://localhost:5100/health  # Hangfire worker
curl http://localhost:8001/health  # JSON provider
curl http://localhost:8002/health  # XML provider

# Clean everything (removes volumes!)
make clean
```

### Database Operations

```bash
# Connect to PostgreSQL shell
make db-shell
# or
docker exec -it search-db psql -U postgres -d hangfire

# List databases
make db-list

# List Hangfire tables
make db-tables

# Verify database setup
make verify-db
```

### Development Workflow

```bash
# 1. Start only the database for local .NET development
docker-compose up -d search-db

# 2. Run provider microservices locally
cd src/JsonProviderMicroservice && dotnet run
cd src/XmlProviderMicroservice && dotnet run

# 3. Run Hangfire worker locally
cd src/HangfireWorker && dotnet run
```

## Service Endpoints

| Service | Port | Key Endpoints | Description |
|---------|------|---------------|-------------|
| **JSON Provider** | 8001 | `/api/provider/data`, `/swagger`, `/health` | Fetches and transforms JSON data |
| **XML Provider** | 8002 | `/api/provider/data`, `/swagger`, `/health` | Fetches and transforms XML data |
| **WriteService** | 8003 | `/hangfire`, `/health`, `/api/content`, `/api/test` | Content sync with Hangfire |
| **EventBusService** | 8004 | `/api/events/content-changed`, `/health` | RabbitMQ event publisher |
| **SearchWorker** | 8005 | `/health`, background consumer | RabbitMQ → Elasticsearch indexer |
| **CacheWorker** | 8006 | `/health`, background consumer | Redis caching service |
| **SearchService** | 8007 | `/api/search`, `/health`, `/swagger` | Search API (Elasticsearch/Redis) |
| **Dashboard** | 8008 | `/`, `/Search` | Web UI for search |
| **Hangfire Worker** | 5100 | `/hangfire`, `/health` | Legacy (not running by default) |
| **PostgreSQL** | 5433 | Databases: `hangfire`, `searchcase` | Primary database |
| **RabbitMQ** | 5672 | AMQP protocol | Message broker |
| **RabbitMQ Management** | 15672 | Web UI (guest/guest) | Queue management |
| **Elasticsearch** | 9200 | REST API | Search engine |
| **Kibana** | 5601 | Web UI | Elasticsearch visualization |
| **Redis** | 6379 | Redis protocol | Cache store |
| **pgAdmin** | 5050 | Web UI (admin@searchcase.local/admin123) | Database management (profile: tools) |

**Note:** Port 5433 is used for PostgreSQL to avoid conflicts with local installations (default 5432).

## Key Configuration Patterns

### Connection Strings
- HangfireWorker uses `ConnectionStrings:HangfireDb` from appsettings.json
- Container connection: `Host=search-db;Port=5432;Database=hangfire`
- Local connection: `Host=localhost;Port=5433;Database=hangfire`

### Microservice Settings
Each provider configures external API settings via `ExternalApiSettings`:
- BaseUrl, TimeoutSeconds, RetryCount, RetryDelaySeconds
- CircuitBreakerThreshold, CircuitBreakerDurationSeconds
- Configured in appsettings.json or environment variables

### Hangfire Configuration
Located in `appsettings.json` under `Hangfire` section:
- WorkerCount (default: 5)
- Queues (default: ["critical", "default"])
- DashboardEnabled, DashboardPath
- Microservices:SyncJobService:BaseUrl and DailyJobService:BaseUrl for job targets

## Important Code Patterns

### Adding a New Provider Microservice

1. **Create project** referencing `SearchCase.Contracts`
2. **Define provider-specific DTOs** (e.g., `Models/YourProviderResponse.cs`)
3. **Implement mapper** implementing `IContentMapper<TProviderDto>`
   - Map to `CanonicalVideoContent` or `CanonicalArticleContent`
   - Use `MappingResult<CanonicalContent>` for return type
4. **Register services** in `Extensions/ServiceCollectionExtensions.cs`
5. **Configure HttpClient** with Polly resilience policies
6. **Add validation** using FluentValidation validators from Contracts
7. **Return ProviderResponse** with Items, Pagination, Provider metadata

### Adding a New Hangfire Job

1. **Inherit from BaseJob** (implements ExecuteAsync pattern)
2. **Override ExecuteJobAsync** with your job logic
3. **Register in DI** in `HangfireExtensions.AddHangfireConfiguration()`
4. **Schedule in UseHangfireJobs** with cron expression
5. Jobs should call provider microservices via `IMicroserviceClient`

### JSON Polymorphism Pattern

When working with canonical content:
```csharp
// Base class with polymorphic attributes
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(CanonicalVideoContent), "video")]
[JsonDerivedType(typeof(CanonicalArticleContent), "article")]
public abstract class CanonicalContent { ... }

// Add custom converter for ISO 8601 durations
options.JsonSerializerOptions.Converters.Add(new Iso8601DurationConverter());
```

### Validation Pattern

All canonical content must be validated before returning:
```csharp
var validationResult = _validator.Validate(canonical);
if (!validationResult.IsValid)
{
    return MappingResult<CanonicalContent>.Fail(validationResult.Errors);
}
```

## Project Dependencies

### HangfireWorker (.NET 8.0)
- Hangfire.AspNetCore, Hangfire.PostgreSql
- Polly, Polly.Extensions.Http
- Serilog.AspNetCore
- AspNetCore.HealthChecks.NpgSql

### Provider Microservices (.NET 9.0)
- SearchCase.Contracts (project reference)
- Microsoft.Extensions.Http.Polly
- Serilog.AspNetCore
- Swashbuckle.AspNetCore (Swagger)
- AspNetCore.HealthChecks.Uris

### SearchCase.Contracts (.NET 9.0)
- FluentValidation
- TreatWarningsAsErrors enabled

## Logging

All projects use **Serilog** with structured logging:
- Console sink: `[HH:mm:ss LEVEL] Message`
- File sink: `logs/{service}-.txt` with daily rolling
- Request logging middleware enabled via `UseSerilogRequestLogging()`
- Configure levels in appsettings.json under `Serilog` section

## Health Check Strategy

- `/health` - Critical dependencies only (database, required APIs)
- `/health/ready` - Kubernetes readiness probe
- `/health/live` - Kubernetes liveness probe (always healthy if running)
- `/health/external` - Optional external services (may be degraded)

## Testing Approach

When creating tests:
- Test mappers thoroughly (various inputs, validation failures)
- Mock `IExternalApiClient` for provider service tests
- Test Polly policies separately (retry, circuit breaker)
- Integration tests should use TestContainers for PostgreSQL
- Hangfire jobs should be testable without actual HTTP calls

## Environment Variables

Key variables in `.env` and `docker-compose.yml`:
- `POSTGRES_PASSWORD` - Database password
- `HANGFIRE_WORKER_COUNT` - Number of Hangfire workers
- `ENVIRONMENT` - Development/Production
- `ExternalApi__BaseUrl` - Override external API URLs
- Provider microservices support all standard ASP.NET Core env vars

## Database Schema

PostgreSQL contains two databases:

### `hangfire` Database
- Schema `hangfire.*` - All Hangfire tables auto-created
- Connection pooling configured: MinPoolSize=2, MaxPoolSize=20
- Initialization script: `scripts/init-db.sql`

### `searchcase` Database (WriteService)
- **Contents** table - Stores canonical content with hash-based change tracking
- **ContentChangeLogs** - Audit trail of all content changes
- **SyncBatches** - Tracks synchronization operations
- Initialization script: `scripts/init-write-db.sql`

## C# Concepts and Naming Conventions

### Key Terms
- **DTO (Data Transfer Object)**: Objects for transferring data between layers (e.g., `JsonProviderResponse`, `CanonicalContent`)
- **Interface (I prefix)**: Contract defining what methods a class must implement (e.g., `IContentMapper`, `IChangeDetectionService`)
- **DI (Dependency Injection)**: Pattern for providing dependencies to classes via constructor parameters
- **async/await**: Non-blocking asynchronous programming pattern
- **Generic Types <T>**: Type parameters allowing code reuse with different types

### Naming Conventions
```csharp
// Interfaces start with 'I'
public interface IContentService { }

// Private fields start with underscore
private readonly ILogger _logger;

// Async methods end with 'Async'
public async Task<Result> GetDataAsync() { }

// Properties use PascalCase
public string ContentTitle { get; set; }

// Local variables use camelCase
var contentItems = new List<Content>();
```

### Common Patterns in This Project

#### Interface-Based Design
```csharp
// Define contract
public interface IChangeDetectionStrategy
{
    ChangeDetectionResult DetectChanges(ContentEntity newContent, ContentEntity? existing);
}

// Multiple implementations
public class HashBasedChangeDetectionStrategy : IChangeDetectionStrategy { }
public class TimestampBasedChangeDetectionStrategy : IChangeDetectionStrategy { }

// Easy switching via DI
services.AddScoped<IChangeDetectionStrategy, HashBasedChangeDetectionStrategy>();
```

#### Result Pattern
```csharp
public class MappingResult<T>
{
    public bool IsSuccess { get; set; }
    public T Data { get; set; }
    public List<string> Errors { get; set; }

    public static MappingResult<T> Success(T data) => new() { IsSuccess = true, Data = data };
    public static MappingResult<T> Fail(List<string> errors) => new() { IsSuccess = false, Errors = errors };
}
```

## Testing Guidance

### Unit Testing with Interfaces

Interfaces enable easy mocking for unit tests:

```csharp
// Example test using Moq
[Test]
public async Task Should_Detect_New_Content()
{
    // Arrange - Create mocks
    var mockStrategy = new Mock<IChangeDetectionStrategy>();
    var mockRepository = new Mock<IContentRepository>();

    // Setup mock behavior
    mockStrategy.Setup(x => x.DetectChanges(It.IsAny<ContentEntity>(), null))
                .Returns(ChangeDetectionResult.NewContent(new ContentEntity()));

    // Act - Test the service
    var orchestrator = new ContentSyncOrchestrator(
        mockStrategy.Object,
        mockRepository.Object,
        logger);

    var result = await orchestrator.SynchronizeContentAsync(contents);

    // Assert
    Assert.AreEqual(1, result.Created);
    mockStrategy.Verify(x => x.DetectChanges(It.IsAny<ContentEntity>(), null), Times.Once);
}
```

### Testing Different Implementations

```csharp
// Test with different strategies without changing test code
[TestCase(typeof(HashBasedChangeDetectionStrategy))]
[TestCase(typeof(TimestampBasedChangeDetectionStrategy))]
public void Should_Detect_Changes_With_Strategy(Type strategyType)
{
    var strategy = (IChangeDetectionStrategy)Activator.CreateInstance(strategyType, logger);
    // Test logic remains the same
}
```

### Integration Testing

```csharp
// Use TestContainers for database tests
public class ContentRepositoryTests
{
    private PostgreSqlContainer _postgres;

    [SetUp]
    public async Task Setup()
    {
        _postgres = new PostgreSqlBuilder()
            .WithDatabase("testdb")
            .Build();
        await _postgres.StartAsync();
    }

    [Test]
    public async Task Should_Save_Content()
    {
        // Use real database in container
        var repository = new ContentRepository(GetDbContext());
        // Test with actual database operations
    }
}
```

## Architecture Benefits

### Why Use Interfaces?

1. **Testability**: Mock implementations for unit tests
2. **Flexibility**: Switch implementations without changing consuming code
3. **Team Development**: Define contracts early, implement in parallel
4. **Open/Closed Principle**: Open for extension, closed for modification

### Example: Adding a New Change Detection Strategy

```csharp
// 1. Create new implementation
public class AIBasedChangeDetectionStrategy : IChangeDetectionStrategy
{
    public ChangeDetectionResult DetectChanges(ContentEntity newContent, ContentEntity? existing)
    {
        // AI-powered change detection logic
    }
}

// 2. Update DI registration (single line change)
services.AddScoped<IChangeDetectionStrategy, AIBasedChangeDetectionStrategy>();

// 3. No changes needed in ContentSyncOrchestrator or any consuming code!
```

### Strategy Pattern in Action

The WriteService uses Strategy Pattern for change detection:
- **Current**: `HashBasedChangeDetectionStrategy` - Compares content hashes
- **Future Options**: Timestamp-based, AI-based, or hybrid approaches
- **Switching**: One-line change in DI configuration

## Common Operations

### Switching Between Services

```bash
# Use original Hangfire Worker
docker-compose up -d hangfire-worker
docker-compose stop write-service

# Use new WriteService (default)
docker-compose up -d write-service
docker-compose stop hangfire-worker
```

### Checking Active Hangfire Dashboard

```bash
# Check which service is running
docker ps | grep -E "hangfire|write"

# Access the active dashboard
# If write-service: http://localhost:8003/hangfire
# If hangfire-worker: http://localhost:5100/hangfire
```
