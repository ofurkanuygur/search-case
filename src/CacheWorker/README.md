# CacheWorker Service

## Overview
CacheWorker is a Redis cache consumer service that implements the **"Single Source of Truth"** pattern. It consumes events from RabbitMQ, fetches pre-calculated content from the database, and updates the Redis cache WITHOUT recalculating scores.

## Key Features

### Single Source of Truth Pattern
- **NO score recalculation** happens in CacheWorker
- Fetches content with **pre-calculated scores** from WriteService database
- Trusts the database as the authoritative source
- Eliminates redundant calculations across services

### Event-Driven Architecture
- Consumes `ContentBatchUpdatedEvent` from RabbitMQ
- Events contain only content IDs (optimized payload)
- Fetches full content data from database on demand

### Resilience Patterns
- **Circuit Breaker** for RabbitMQ connections
- **Exponential Backoff** retry policy (3 retries, 5-30 seconds)
- **Health Checks** for PostgreSQL, Redis, and RabbitMQ
- **Graceful Degradation** when services are unavailable

### Cache Management
- Stores content in Redis with configurable expiration (default: 24 hours)
- Uses Redis sorted sets for score-based queries
- Supports batch updates for efficiency
- Provides cache statistics and monitoring

## API Endpoints

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/cache/{id}` | GET | Get cached content by ID |
| `/api/cache/batch` | POST | Get multiple contents by IDs |
| `/api/cache/statistics` | GET | Get cache and database statistics |
| `/api/cache/refresh` | POST | Manually refresh cache for specific IDs |
| `/api/cache/{id}` | DELETE | Remove specific content from cache |
| `/api/cache/all` | DELETE | Clear all cache (use with caution) |
| `/health` | GET | Health check endpoint |
| `/swagger` | GET | API documentation |

## Architecture Flow

```
1. WriteService calculates scores and saves to DB
2. WriteService publishes ContentBatchUpdatedEvent (IDs only)
3. CacheWorker consumes event from RabbitMQ
4. CacheWorker fetches full content from DB (with scores)
5. CacheWorker updates Redis cache (no recalculation)
```

## Configuration

### appsettings.json
```json
{
  "ConnectionStrings": {
    "WriteServiceDb": "Host=localhost;Port=5433;Database=searchcase;Username=postgres;Password=postgres",
    "Redis": "localhost:6379"
  },
  "RabbitMQ": {
    "Host": "localhost",
    "Username": "guest",
    "Password": "guest"
  },
  "Cache": {
    "ExpirationHours": 24
  }
}
```

## Docker Deployment

The service includes a production-ready Dockerfile with:
- Multi-stage build for optimization
- Non-root user for security
- Health checks
- Resource limits

## Redis Data Structure

### Keys
- `content:{id}` - Individual content JSON
- `content:by_score` - Sorted set of all content by score
- `content:by_score:{type}` - Sorted set by content type
- `content:by_score:{provider}` - Sorted set by provider
- `statistics:cache` - Hash with cache statistics
- `metadata:last_updated` - Last update timestamp

### Cache Policy
- LRU eviction when memory limit reached (512MB)
- Append-only file for persistence
- 24-hour expiration for content entries

## Dependencies

- **.NET 9.0**
- **MassTransit.RabbitMQ** - Message broker integration
- **StackExchange.Redis** - Redis client
- **Npgsql.EntityFrameworkCore.PostgreSQL** - Database access
- **Dapper** - Micro ORM for queries
- **Serilog** - Structured logging
- **Polly** - Resilience patterns
- **Swashbuckle** - API documentation

## Running Locally

```bash
# Start dependencies
docker-compose up -d search-db redis rabbitmq

# Run the service
dotnet run

# Access endpoints
curl http://localhost:8080/health
curl http://localhost:8080/api/cache/statistics
```

## Running with Docker Compose

```bash
# Start all services
docker-compose up -d

# Check logs
docker-compose logs cache-worker

# Access the service
curl http://localhost:8005/health
```

## Performance Characteristics

- **Batch Processing**: Handles up to 16 messages concurrently
- **Connection Pooling**: PostgreSQL (2-20 connections)
- **Redis Pipeline**: Batch operations for efficiency
- **Memory Limit**: 512MB container limit
- **CPU Limit**: 1 CPU (0.25 reserved)

## Monitoring

The service provides:
- Health checks at `/health`
- Cache statistics API
- Structured logging with Serilog
- RabbitMQ management UI at `http://localhost:15672`
- Redis monitoring via `redis-cli`

## Security

- Non-root container user
- Connection string encryption in production
- No sensitive data in logs
- Resource limits to prevent DoS

## Future Enhancements

- [ ] Implement cache warming on startup
- [ ] Add cache hit/miss ratio tracking
- [ ] Implement cache invalidation patterns
- [ ] Add distributed tracing
- [ ] Implement cache preloading for hot data