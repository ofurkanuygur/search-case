#!/bin/bash

# Database Verification Script
# Verifies PostgreSQL database configuration

echo "======================================"
echo "SearchCase Database Verification"
echo "======================================"
echo ""

GREEN='\033[0;32m'
RED='\033[0;31m'
NC='\033[0m'

# Check container
echo "1. Checking search-db container..."
if docker ps | grep -q search-db; then
    echo -e "${GREEN}✓${NC} Container is running"
else
    echo -e "${RED}✗${NC} Container is not running"
    echo "   Run: docker-compose up -d"
    exit 1
fi
echo ""

# Check PostgreSQL
echo "2. Checking PostgreSQL..."
if docker exec search-db pg_isready -U postgres > /dev/null 2>&1; then
    echo -e "${GREEN}✓${NC} PostgreSQL is ready"
else
    echo -e "${RED}✗${NC} PostgreSQL is not ready"
    exit 1
fi
echo ""

# List databases
echo "3. Listing databases..."
docker exec search-db psql -U postgres -c "\l" | grep -E "Name|hangfire|postgres"
echo ""

# Check hangfire database
echo "4. Checking hangfire database..."
if docker exec search-db psql -U postgres -c "\l" | grep -q "hangfire"; then
    echo -e "${GREEN}✓${NC} Hangfire database exists"
else
    echo -e "${RED}✗${NC} Hangfire database does NOT exist"
    exit 1
fi
echo ""

# Check tables
echo "5. Checking Hangfire tables..."
TABLE_COUNT=$(docker exec search-db psql -U postgres -d hangfire -t -c "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = 'hangfire';" | xargs)
echo "   Found $TABLE_COUNT tables in hangfire schema"
if [ "$TABLE_COUNT" -gt 0 ]; then
    echo -e "${GREEN}✓${NC} Hangfire tables exist"
    echo ""
    docker exec search-db psql -U postgres -d hangfire -c "\dt hangfire.*" | head -15
else
    echo "   No tables yet (will be created when worker starts)"
fi
echo ""

echo "======================================"
echo "Database Connection Details"
echo "======================================"
echo ""
echo "Host:     localhost"
echo "Port:     5433"
echo "Database: hangfire"
echo "User:     postgres"
echo "Password: postgres"
echo ""
echo "Connection String:"
echo "Host=localhost;Port=5433;Database=hangfire;Username=postgres;Password=postgres"
echo ""
echo "======================================"
echo "Verification Complete!"
echo "======================================"
echo ""
echo -e "${GREEN}✓${NC} Database is ready!"
echo ""
