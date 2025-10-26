# SearchCase - Hangfire Worker Service

Production-ready Hangfire Worker Service for scheduling and executing background jobs that trigger microservices.

## Architecture

```
┌─────────────────────────┐
│   Hangfire Worker       │
│   - Job Scheduler       │
│   - Job Processor       │
│   - Dashboard           │
└──────────┬──────────────┘
           │
           ├─> PostgreSQL (Job Storage)
           │
           ├─> Microservice A (Every 5 min)
           └─> Microservice B (Daily)
```

## Features

- ✅ Production-ready Docker setup
- ✅ PostgreSQL with persistent storage
- ✅ Retry with exponential backoff
- ✅ Circuit breaker pattern
- ✅ Health checks
- ✅ Structured logging (Serilog)
- ✅ Hangfire Dashboard

## Quick Start

### 1. Start Services

```bash
docker-compose up -d
```

### 2. Check Status

```bash
docker-compose ps
```

### 3. Access Dashboard

```
http://localhost:5100/hangfire
```

### 4. Check Health

```bash
curl http://localhost:5100/health
```

## Project Structure

```
SearchCase/
├── src/HangfireWorker/      # Worker Service
│   ├── Jobs/                 # Job implementations
│   ├── Services/             # HTTP clients
│   └── Configuration/        # Settings
├── scripts/                  # Database init
├── data/                     # PostgreSQL data (gitignored)
├── docker-compose.yml
└── README.md
```

## Configuration

### Environment Variables (.env)

```env
# PostgreSQL
POSTGRES_PASSWORD=postgres

# Hangfire
HANGFIRE_WORKER_COUNT=5

# Microservices
SERVICE_A_URL=http://host.docker.internal:8001
SERVICE_B_URL=http://host.docker.internal:8002
```

### Database Connection

**Container:** `search-db`
**Port:** `5433` (to avoid conflict with system PostgreSQL)
**User:** `postgres`
**Password:** `postgres` (from .env)
**Databases:** `postgres` (default), `hangfire` (jobs)

**Connection String:**
```
Host=localhost;Port=5433;Database=hangfire;Username=postgres;Password=postgres
```

**DataGrip/IDE:**
```
Host:     localhost
Port:     5433
Database: hangfire
User:     postgres
Password: postgres
```

## Scheduled Jobs

| Job | Schedule | Target | Endpoint |
|-----|----------|--------|----------|
| **FrequentJob** | Every 5 minutes | Microservice A | `/api/process` |
| **DailyJob** | Daily 02:00 UTC | Microservice B | `/api/process` |

## Endpoints

### Hangfire Dashboard
- **URL:** http://localhost:5100/hangfire
- **Features:** Job monitoring, manual execution, retry

### Health Checks
- `/health` - Critical dependencies only (PostgreSQL)
- `/health/ready` - Readiness probe (same as /health)
- `/health/live` - Liveness probe (always healthy if app is running)
- `/health/external` - Optional external services (microservices - may be degraded)

## Docker Commands

```bash
# Start services
docker-compose up -d

# View logs
docker-compose logs -f hangfire-worker
docker-compose logs -f search-db

# Stop services
docker-compose down

# Stop and remove data
docker-compose down -v
```

## Makefile Commands

```bash
make up            # Start all services
make down          # Stop all services
make logs-worker   # View worker logs
make logs-db       # View database logs
make health        # Check health
make dashboard     # Open dashboard
make db-shell      # PostgreSQL shell
make verify-db     # Verify database
make clean         # Clean everything
```

## Database Management

### PostgreSQL Shell

```bash
# Via make
make db-shell

# Via docker
docker exec -it search-db psql -U postgres -d hangfire
```

### Common Queries

```sql
-- List all databases
\l

-- List tables
\dt hangfire.*

-- View jobs
SELECT id, statename, createdat
FROM hangfire.job
ORDER BY createdat DESC
LIMIT 10;
```

## Development

### Local Development (Without Docker)

1. **Start PostgreSQL:**
   ```bash
   docker-compose up -d search-db
   ```

2. **Run worker:**
   ```bash
   cd src/HangfireWorker
   dotnet run
   ```

### Build Solution

```bash
dotnet build SearchCase.sln
```

## Data Persistence

PostgreSQL data is stored in `./data/postgres/` (gitignored).

**Backup:**
```bash
docker exec search-db pg_dump -U postgres hangfire > backup.sql
```

**Restore:**
```bash
docker exec -i search-db psql -U postgres hangfire < backup.sql
```

## Troubleshooting

### Worker Not Starting

```bash
# Check logs
docker-compose logs hangfire-worker

# Restart worker
docker-compose restart hangfire-worker
```

### Database Connection Error

```bash
# Check if DB is running
docker-compose ps

# Check DB health
docker exec search-db pg_isready -U postgres

# View DB logs
docker-compose logs search-db
```

### Jobs Not Executing

1. Check Hangfire Dashboard for errors
2. Verify microservice URLs in `.env`
3. Check worker logs

## Security Notes

**Development Configuration:**
- User: `postgres`
- Password: `postgres`
- ⚠️ For development only!

**Production Recommendations:**
- Use strong, unique passwords
- Store credentials in secrets manager
- Enable SSL/TLS
- Use read-only replicas for reporting
- Implement IP whitelisting

## Tech Stack

- .NET 8.0
- Hangfire (Background jobs)
- PostgreSQL 16 (Storage)
- Serilog (Logging)
- Polly (Resilience)
- Docker & Docker Compose

## License

This project is proprietary to SearchCase.
