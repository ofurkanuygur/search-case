-- ============================================================================
-- SearchCase Write Service Database Schema
-- ============================================================================
-- Description: Database schema for storing normalized content from providers
--              with score calculations and change detection support
-- Version: 1.0
-- ============================================================================

-- Create database if not exists (handled by docker-entrypoint-initdb.d)
-- This script assumes 'searchcase' database already exists

\c searchcase;

-- ============================================================================
-- Enable Required Extensions
-- ============================================================================

CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pg_trgm"; -- For text search optimization

-- ============================================================================
-- Custom Types
-- ============================================================================

-- Content type enum
CREATE TYPE content_type AS ENUM ('video', 'article');

-- ============================================================================
-- Contents Table
-- ============================================================================
-- Stores all content items from both JSON and XML providers in canonical format
-- Uses discriminated union pattern with type field

CREATE TABLE IF NOT EXISTS contents (
    -- Primary Key
    id TEXT PRIMARY KEY,

    -- Content Metadata
    type content_type NOT NULL,
    title TEXT NOT NULL,
    published_at TIMESTAMPTZ NOT NULL,

    -- Categories stored as JSONB array for flexibility
    categories JSONB NOT NULL DEFAULT '[]'::jsonb,

    -- Source Information
    source_provider TEXT NOT NULL, -- 'json-provider' or 'xml-provider'

    -- Metrics stored as JSONB (flexible for video vs article differences)
    -- Video: { "views": 1000, "likes": 50, "duration": "PT22M45S" }
    -- Article: { "readingTimeMinutes": 5, "reactions": 100, "comments": 20 }
    metrics JSONB NOT NULL,

    -- Calculated Score
    score DECIMAL(10, 2) NOT NULL DEFAULT 0.0,

    -- Change Detection
    content_hash TEXT NOT NULL, -- SHA256 hash of canonical data

    -- Optimistic Locking
    version BIGINT NOT NULL DEFAULT 1,

    -- Audit Fields
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    -- Constraints
    CONSTRAINT chk_title_length CHECK (char_length(title) <= 1000),
    CONSTRAINT chk_source_provider CHECK (source_provider IN ('json-provider', 'xml-provider')),
    CONSTRAINT chk_score_positive CHECK (score >= 0),
    CONSTRAINT chk_version_positive CHECK (version > 0)
);

-- ============================================================================
-- Indexes for Performance
-- ============================================================================

-- Composite index for leaderboard queries (type + score descending)
CREATE INDEX idx_contents_type_score ON contents(type, score DESC);

-- Index for time-based queries (recency calculation)
CREATE INDEX idx_contents_published_at ON contents(published_at DESC);

-- Index for change detection lookups
CREATE INDEX idx_contents_content_hash ON contents(content_hash);

-- Index for time service queries (find stale content)
CREATE INDEX idx_contents_updated_at ON contents(updated_at);

-- Index for source provider filtering (monitoring, debugging)
CREATE INDEX idx_contents_source_provider ON contents(source_provider);

-- GIN index for categories array searching
CREATE INDEX idx_contents_categories ON contents USING GIN(categories jsonb_path_ops);

-- GIN index for full-text search on title
CREATE INDEX idx_contents_title_trgm ON contents USING GIN(title gin_trgm_ops);

-- ============================================================================
-- Functions and Triggers
-- ============================================================================

-- Function: Update updated_at timestamp automatically
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    NEW.version = OLD.version + 1;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Trigger: Auto-update updated_at and version on UPDATE
CREATE TRIGGER trigger_contents_updated_at
    BEFORE UPDATE ON contents
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

-- ============================================================================
-- Statistics and Maintenance
-- ============================================================================

-- Create statistics for better query planning
CREATE STATISTICS IF NOT EXISTS contents_type_score_stats (dependencies)
    ON type, score FROM contents;

-- ============================================================================
-- Sample Queries (Commented for reference)
-- ============================================================================

-- Query 1: Get top 10 videos by score
-- SELECT id, title, score, published_at
-- FROM contents
-- WHERE type = 'video'
-- ORDER BY score DESC
-- LIMIT 10;

-- Query 2: Find changed content by hash
-- SELECT id, title, content_hash
-- FROM contents
-- WHERE id = ANY($1) -- Array of IDs
--   AND content_hash != ANY($2); -- Array of new hashes

-- Query 3: Bulk upsert pattern
-- INSERT INTO contents (id, type, title, published_at, categories, source_provider, metrics, score, content_hash)
-- VALUES ($1, $2, $3, $4, $5, $6, $7, $8, $9)
-- ON CONFLICT (id) DO UPDATE SET
--     title = EXCLUDED.title,
--     published_at = EXCLUDED.published_at,
--     categories = EXCLUDED.categories,
--     metrics = EXCLUDED.metrics,
--     score = EXCLUDED.score,
--     content_hash = EXCLUDED.content_hash;

-- Query 4: Find stale content for time service (6+ hours old)
-- SELECT id, type, published_at, score
-- FROM contents
-- WHERE updated_at < NOW() - INTERVAL '6 hours'
-- ORDER BY updated_at ASC
-- LIMIT 1000;

-- ============================================================================
-- Permissions (if using specific service user)
-- ============================================================================

-- Grant permissions to application user
GRANT SELECT, INSERT, UPDATE ON contents TO postgres;
GRANT USAGE ON SCHEMA public TO postgres;

-- ============================================================================
-- Initial Data / Seed (Optional)
-- ============================================================================

-- No seed data for production
-- For development, you can add sample data here

-- ============================================================================
-- End of Schema
-- ============================================================================

-- Display table info
\d+ contents;

-- Display indexes
\di+ idx_contents_*;
