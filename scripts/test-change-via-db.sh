#!/bin/bash

# Database-level Change Detection Test
# This script modifies data directly in the database to test change detection

GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${BLUE}🗄️  Database-Level Change Detection Test${NC}"
echo "=========================================="

# Step 1: Show current state
echo -e "\n${YELLOW}Step 1: Current database state${NC}"
docker exec search-db psql -U postgres -d searchcase -c "
SELECT
    id,
    LEFT(title, 30) as title,
    LEFT(content_hash, 16) as hash_prefix,
    version,
    updated_at
FROM contents
WHERE id IN ('provider1_v1', 'provider1_v2')
ORDER BY id;"

# Step 2: Save original hash
echo -e "\n${YELLOW}Step 2: Saving original hash values${NC}"
ORIGINAL_HASH=$(docker exec search-db psql -U postgres -d searchcase -t -c "
SELECT content_hash FROM contents WHERE id = 'provider1_v1';" | xargs)
echo "Original hash for provider1_v1: ${ORIGINAL_HASH:0:32}..."

# Step 3: Modify content directly in DB
echo -e "\n${YELLOW}Step 3: Modifying content in database${NC}"
docker exec search-db psql -U postgres -d searchcase -c "
UPDATE contents
SET
    title = title || ' - MODIFIED',
    score = score + 10,
    version = version + 1,
    updated_at = NOW()
WHERE id = 'provider1_v1'
RETURNING id, title, score, version;"

# Step 4: Show the change
echo -e "\n${YELLOW}Step 4: Showing modified content${NC}"
docker exec search-db psql -U postgres -d searchcase -c "
SELECT
    id,
    title,
    score,
    LEFT(content_hash, 16) as hash_prefix,
    version
FROM contents
WHERE id = 'provider1_v1';"

# Step 5: Monitor next sync
echo -e "\n${YELLOW}Step 5: Next sync behavior${NC}"
echo -e "${BLUE}When the next sync runs (every 5 minutes), it will:${NC}"
echo "1. Fetch fresh data from provider"
echo "2. Compute new hash from fresh data"
echo "3. Compare with DB hash (currently still old hash: ${ORIGINAL_HASH:0:16}...)"
echo "4. Detect change and update with fresh data"
echo "5. Log: 'Change detection complete: 0 created, 1 updated, 7 unchanged'"
echo ""
echo -e "${GREEN}Watch the logs with:${NC} docker logs -f searchcase-write-service | grep -E 'Change detection|Updated'"

# Step 6: Create monitoring command
echo -e "\n${YELLOW}Step 6: Live monitoring command${NC}"
cat << 'EOF' > /tmp/monitor-changes.sh
#!/bin/bash
watch -n 2 "docker exec search-db psql -U postgres -d searchcase -c \"
SELECT
    id,
    LEFT(title, 35) as title,
    LEFT(content_hash, 12) || '...' as hash,
    version,
    TO_CHAR(updated_at, 'HH24:MI:SS') as last_update
FROM contents
ORDER BY updated_at DESC
LIMIT 5;\""
EOF

chmod +x /tmp/monitor-changes.sh
echo -e "${GREEN}Run this to monitor changes:${NC} /tmp/monitor-changes.sh"

echo -e "\n${GREEN}✅ Test setup complete!${NC}"
echo "The next sync will restore the original data from the provider."