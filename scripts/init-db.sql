-- SearchCase PostgreSQL Database Initialization Script
-- This script runs automatically when the PostgreSQL container starts for the first time

\echo '========================================='
\echo 'SearchCase Database Initialization'
\echo '========================================='

-- Create Hangfire Database
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

\echo 'Enabled extensions'

-- Create hangfire schema
CREATE SCHEMA IF NOT EXISTS hangfire;

\echo 'Schema created'

-- Log successful initialization
DO $$
BEGIN
    RAISE NOTICE '========================================';
    RAISE NOTICE 'Database initialization completed!';
    RAISE NOTICE 'Database: hangfire';
    RAISE NOTICE 'Owner: postgres';
    RAISE NOTICE 'Timestamp: %', NOW();
    RAISE NOTICE '========================================';
END $$;
