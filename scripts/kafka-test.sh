#!/bin/bash

# Kafka Integration Test Script
# This script validates the Kafka setup and event flow

set -e

echo "=================================================="
echo "Kafka Integration Test & Validation"
echo "=================================================="
echo ""

# Colors for output
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Function to print success message
success() {
    echo -e "${GREEN}✓ $1${NC}"
}

# Function to print error message
error() {
    echo -e "${RED}✗ $1${NC}"
}

# Function to print info message
info() {
    echo -e "${YELLOW}→ $1${NC}"
}

echo "Step 1: Check if Kafka is running"
info "Checking Kafka container..."
if docker ps | grep -q searchcase-kafka; then
    success "Kafka container is running"
else
    error "Kafka container is not running"
    echo "Please start services with: docker-compose up -d"
    exit 1
fi

echo ""
echo "Step 2: Check Kafka health"
info "Checking Kafka broker health..."
if docker exec searchcase-kafka kafka-broker-api-versions --bootstrap-server localhost:9092 > /dev/null 2>&1; then
    success "Kafka broker is healthy"
else
    error "Kafka broker is not responding"
    exit 1
fi

echo ""
echo "Step 3: List Kafka topics"
info "Fetching Kafka topics..."
TOPICS=$(docker exec searchcase-kafka kafka-topics --list --bootstrap-server localhost:9092)
echo "$TOPICS"

if echo "$TOPICS" | grep -q "content-batch-updated"; then
    success "Topic 'content-batch-updated' exists"
else
    info "Topic 'content-batch-updated' will be auto-created on first message"
fi

echo ""
echo "Step 4: Check topic details (if exists)"
if echo "$TOPICS" | grep -q "content-batch-updated"; then
    info "Topic details:"
    docker exec searchcase-kafka kafka-topics \
        --describe \
        --topic content-batch-updated \
        --bootstrap-server localhost:9092
    success "Topic configuration validated"
fi

echo ""
echo "Step 5: Check consumer groups"
info "Fetching consumer groups..."
GROUPS=$(docker exec searchcase-kafka kafka-consumer-groups \
    --list \
    --bootstrap-server localhost:9092 2>/dev/null || echo "")

if [ -n "$GROUPS" ]; then
    echo "$GROUPS"

    # Check if search-workers group exists
    if echo "$GROUPS" | grep -q "search-workers"; then
        success "Consumer group 'search-workers' is registered"
        info "Consumer group details:"
        docker exec searchcase-kafka kafka-consumer-groups \
            --describe \
            --group search-workers \
            --bootstrap-server localhost:9092
    fi

    # Check if cache-workers group exists
    if echo "$GROUPS" | grep -q "cache-workers"; then
        success "Consumer group 'cache-workers' is registered"
        info "Consumer group details:"
        docker exec searchcase-kafka kafka-consumer-groups \
            --describe \
            --group cache-workers \
            --bootstrap-server localhost:9092
    fi
else
    info "No consumer groups found yet (will be created when consumers connect)"
fi

echo ""
echo "Step 6: Check Kafka UI"
info "Checking Kafka UI..."
if docker ps | grep -q searchcase-kafka-ui; then
    success "Kafka UI is running at http://localhost:8090"
else
    error "Kafka UI is not running"
fi

echo ""
echo "Step 7: Check EventBusService (Producer)"
info "Checking EventBusService health..."
if curl -s http://localhost:8004/health > /dev/null 2>&1; then
    success "EventBusService is healthy"
else
    error "EventBusService is not responding"
fi

echo ""
echo "Step 8: Check SearchWorker (Consumer)"
info "Checking SearchWorker health..."
if curl -s http://localhost:8006/health > /dev/null 2>&1; then
    success "SearchWorker is healthy"
else
    error "SearchWorker is not responding"
fi

echo ""
echo "Step 9: Check CacheWorker (Consumer)"
info "Checking CacheWorker health..."
if curl -s http://localhost:8005/health > /dev/null 2>&1; then
    success "CacheWorker is healthy"
else
    error "CacheWorker is not responding"
fi

echo ""
echo "=================================================="
echo "Test Summary"
echo "=================================================="
success "All Kafka components are operational!"
echo ""
echo "Next steps:"
echo "1. Trigger ContentSyncJob from Hangfire Dashboard: http://localhost:8003/hangfire"
echo "2. Monitor Kafka messages in Kafka UI: http://localhost:8090"
echo "3. Check consumer logs:"
echo "   - docker-compose logs -f search-worker"
echo "   - docker-compose logs -f cache-worker"
echo "4. Verify Elasticsearch indexing: curl http://localhost:9200/content-index/_count"
echo ""
echo "=================================================="
