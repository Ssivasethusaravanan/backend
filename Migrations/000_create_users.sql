-- Migration: 000_create_users.sql
-- Creates the users table that maps Firebase Auth UIDs to application profile data.

CREATE TABLE IF NOT EXISTS users (
    id          UUID         PRIMARY KEY DEFAULT gen_random_uuid(),
    firebase_uid VARCHAR(128) NOT NULL UNIQUE,
    email        VARCHAR(255) NOT NULL UNIQUE,
    name         VARCHAR(255) NOT NULL DEFAULT '',
    role         VARCHAR(50)  NOT NULL DEFAULT 'customer',
    tenant_id    VARCHAR(128),
    avatar_url   TEXT,
    fcm_token    TEXT,
    created_at   TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    updated_at   TIMESTAMPTZ  NOT NULL DEFAULT NOW()
);

-- Partial index: only index rows that have an FCM token (avoids indexing NULLs)
CREATE INDEX IF NOT EXISTS idx_users_fcm_token
    ON users (fcm_token)
    WHERE fcm_token IS NOT NULL;

-- Auto-update updated_at on every row change
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER users_updated_at_trigger
    BEFORE UPDATE ON users
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
