.PHONY: help build up down logs clean restart health dashboard clean-data clean-redis clean-elasticsearch clean-db

help: ## Show this help message
	@echo 'Usage: make [target]'
	@echo ''
	@echo 'Available targets:'
	@awk 'BEGIN {FS = ":.*?## "} /^[a-zA-Z_-]+:.*?## / {printf "  %-15s %s\n", $$1, $$2}' $(MAKEFILE_LIST)

build: ## Build Docker images
	docker-compose build

up: ## Start all services
	docker-compose up -d

down: ## Stop all services
	docker-compose down

logs: ## View logs (use logs-follow for real-time)
	docker-compose logs

logs-follow: ## Follow logs in real-time
	docker-compose logs -f

logs-worker: ## View Hangfire worker logs
	docker-compose logs -f hangfire-worker

logs-db: ## View PostgreSQL logs
	docker-compose logs -f search-db

verify-db: ## Verify database configuration and connectivity
	@./scripts/verify-database.sh

db-shell: ## Open PostgreSQL shell
	docker exec -it search-db psql -U postgres -d hangfire

db-list: ## List all databases
	@docker exec search-db psql -U postgres -c "\l"

db-tables: ## List all tables in hangfire database
	@docker exec search-db psql -U postgres -d hangfire -c "\dt hangfire.*"

clean: ## Stop and remove all containers, volumes, and networks
	docker-compose down -v --remove-orphans
	rm -rf logs/

restart: down up ## Restart all services

restart-worker: ## Restart only the Hangfire worker
	docker-compose restart hangfire-worker

health: ## Check health of all services
	@echo "Checking Hangfire Worker health..."
	@curl -f http://localhost:5100/health || echo "Worker unhealthy"
	@echo "\nChecking PostgreSQL..."
	@docker exec search-db pg_isready -U postgres || echo "PostgreSQL unhealthy"

dashboard: ## Open Hangfire dashboard in browser
	@echo "Opening Hangfire Dashboard..."
	@open http://localhost:5100/hangfire || xdg-open http://localhost:5100/hangfire || echo "Please open http://localhost:5100/hangfire manually"

pgadmin: ## Start PgAdmin for database management
	docker-compose --profile tools up -d pgadmin
	@echo "PgAdmin available at http://localhost:5050"

ps: ## Show running containers
	docker-compose ps

shell-worker: ## Open shell in Hangfire worker container
	docker-compose exec hangfire-worker sh

shell-db: ## Open PostgreSQL shell (alias for db-shell)
	docker exec -it search-db psql -U postgres -d hangfire

dotnet-restore: ## Restore .NET dependencies
	dotnet restore SearchCase.sln

dotnet-build: ## Build .NET solution
	dotnet build SearchCase.sln

dotnet-clean: ## Clean .NET build artifacts
	dotnet clean SearchCase.sln
	find . -type d -name "bin" -o -name "obj" | xargs rm -rf

## Data Cleaning Commands

clean-redis: ## Clear all Redis data
	@echo "Clearing Redis..."
	@docker exec searchcase-redis redis-cli FLUSHDB
	@echo "✅ Redis cleared"

clean-elasticsearch: ## Delete all Elasticsearch indices
	@echo "Deleting Elasticsearch indices..."
	@curl -X DELETE "http://localhost:9200/content-index" 2>/dev/null || true
	@echo ""
	@echo "✅ Elasticsearch indices deleted"

clean-db: ## Truncate all PostgreSQL tables (Hangfire jobs + Content data)
	@echo "⚠️  WARNING: This will delete all data from PostgreSQL!"
	@echo "Cleaning Hangfire jobs..."
	@docker exec search-db psql -U postgres -d hangfire -c "TRUNCATE TABLE hangfire.job, hangfire.state, hangfire.jobparameter, hangfire.jobqueue CASCADE;" 2>/dev/null || true
	@echo "Cleaning content data..."
	@docker exec search-db psql -U postgres -d searchcase -c "TRUNCATE TABLE contents CASCADE;" 2>/dev/null || true
	@echo "✅ PostgreSQL tables truncated"

clean-data: clean-redis clean-elasticsearch clean-db ## Clean all data (Redis + Elasticsearch + PostgreSQL)
	@echo ""
	@echo "✅ All data cleaned successfully!"
	@echo "   - Redis: Flushed"
	@echo "   - Elasticsearch: Indices deleted"
	@echo "   - PostgreSQL: Tables truncated"
