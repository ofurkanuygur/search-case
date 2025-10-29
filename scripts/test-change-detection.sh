#!/bin/bash

# Test Change Detection Script
# This script tests the hash-based change detection system

GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

WRITE_SERVICE_URL="http://localhost:8003"

echo -e "${YELLOW}🧪 Change Detection Test Script${NC}"
echo "================================"

# Function to print colored messages
print_success() {
    echo -e "${GREEN}✓ $1${NC}"
}

print_error() {
    echo -e "${RED}✗ $1${NC}"
}

print_info() {
    echo -e "${YELLOW}ℹ $1${NC}"
}

# 1. Get current content states
echo -e "\n${YELLOW}Step 1: Getting current content states${NC}"
curl -s "$WRITE_SERVICE_URL/api/test/content-states" | jq '.' || print_error "Failed to get content states"

# 2. Simulate a change
echo -e "\n${YELLOW}Step 2: Simulating content change${NC}"
CONTENT_ID="provider1_v1"
print_info "Changing title for $CONTENT_ID"

RESPONSE=$(curl -s -X POST "$WRITE_SERVICE_URL/api/test/simulate-change/$CONTENT_ID" \
  -H "Content-Type: application/json" \
  -d '{
    "newTitle": "Go Programming Tutorial - UPDATED FOR TEST",
    "newScore": 99.99
  }')

echo "$RESPONSE" | jq '.' || print_error "Failed to simulate change"

# 3. Wait for next sync
echo -e "\n${YELLOW}Step 3: Waiting for next sync (5 minutes)${NC}"
print_info "Next sync will occur at: $(date -d '+5 minutes' '+%H:%M:%S')"
print_info "Watch the logs to see change detection in action..."

# 4. Monitor sync logs
echo -e "\n${YELLOW}Step 4: Monitoring WriteService logs${NC}"
docker logs searchcase-write-service --tail 10 -f 2>&1 | grep -E "(Change detection|Updated|Unchanged|hash)" &

# 5. Test hash comparison
echo -e "\n${YELLOW}Step 5: Testing hash comparison${NC}"
curl -s -X POST "$WRITE_SERVICE_URL/api/test/compare-hash" \
  -H "Content-Type: application/json" \
  -d '{
    "data1": {"title": "Test 1", "views": 100},
    "data2": {"title": "Test 1", "views": 100}
  }' | jq '.areEqual' && print_success "Same data produces same hash"

curl -s -X POST "$WRITE_SERVICE_URL/api/test/compare-hash" \
  -H "Content-Type: application/json" \
  -d '{
    "data1": {"title": "Test 1", "views": 100},
    "data2": {"title": "Test 2", "views": 100}
  }' | jq '.areEqual' && print_error "Different data should not match" || print_success "Different data produces different hash"

echo -e "\n${GREEN}Test setup complete! Monitor the logs to see change detection in action.${NC}"