-- SearchCase PostgreSQL Database Initialization Script
-- This script runs automatically when the PostgreSQL container starts for the first time

\echo '========================================='
\echo 'SearchCase Database Initialization'
\echo '========================================='

-- ============================================================================
-- Create Hangfire Database
-- ============================================================================
CREATE DATABASE hangfire
    WITH
    OWNER = postgres
    ENCODING = 'UTF8'
    LC_COLLATE = 'en_US.utf8'
    LC_CTYPE = 'en_US.utf8'
    TEMPLATE = template0;

\echo 'Created database: hangfire'

-- Connect to hangfire database and setup
\c hangfire

-- Enable required extensions
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

\echo 'Enabled extensions for hangfire'

-- Create hangfire schema
CREATE SCHEMA IF NOT EXISTS hangfire;

\echo 'Hangfire schema created'

-- ============================================================================
-- Create SearchCase Database (for Write Service)
-- ============================================================================
\c postgres

CREATE DATABASE searchcase
    WITH
    OWNER = postgres
    ENCODING = 'UTF8'
    LC_COLLATE = 'en_US.utf8'
    LC_CTYPE = 'en_US.utf8'
    TEMPLATE = template0;

\echo 'Created database: searchcase'

-- ============================================================================
-- Initialize SearchCase Database Schema
-- ============================================================================
-- The schema setup is handled by init-write-db.sql
\i /docker-entrypoint-initdb.d/init-write-db.sql

-- ============================================================================
-- Log successful initialization
-- ============================================================================
\c postgres

DO $$
BEGIN
    RAISE NOTICE '========================================';
    RAISE NOTICE 'Database initialization completed!';
    RAISE NOTICE 'Databases created:';
    RAISE NOTICE '  - hangfire (Hangfire job storage)';
    RAISE NOTICE '  - searchcase (Write Service content storage)';
    RAISE NOTICE 'Owner: postgres';
    RAISE NOTICE 'Timestamp: %', NOW();
    RAISE NOTICE '========================================';
END $$;
