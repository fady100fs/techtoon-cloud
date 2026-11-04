-- ============================================================================
-- Techtoon API B-2b: Auth Security & Idempotency Tables
-- SAFE: uses IF NOT EXISTS everywhere. No data loss, no column renames.
-- ============================================================================

-- 1. user_devices: manages logged-in devices (JWT refresh tokens)
CREATE TABLE IF NOT EXISTS user_devices (
    id SERIAL PRIMARY KEY,
    user_id INTEGER NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    device_id VARCHAR(100) NOT NULL,
    device_name VARCHAR(200),
    refresh_token_hash VARCHAR(500) NOT NULL,
    last_seen TIMESTAMP DEFAULT NOW(),
    is_revoked BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMP DEFAULT NOW(),
    UNIQUE(user_id, device_id)
);
CREATE INDEX IF NOT EXISTS idx_user_devices_user ON user_devices(user_id) WHERE is_revoked = FALSE;
CREATE INDEX IF NOT EXISTS idx_user_devices_refresh ON user_devices(refresh_token_hash);

-- 2. login_attempts: rate limiting + audit
CREATE TABLE IF NOT EXISTS login_attempts (
    id SERIAL PRIMARY KEY,
    username VARCHAR(100),
    ip_address VARCHAR(45),
    success BOOLEAN,
    failure_reason VARCHAR(200),
    attempted_at TIMESTAMP DEFAULT NOW()
);
CREATE INDEX IF NOT EXISTS idx_login_attempts_username ON login_attempts(username, attempted_at);
CREATE INDEX IF NOT EXISTS idx_login_attempts_ip ON login_attempts(ip_address, attempted_at);

-- 3. idempotency_keys: prevents duplicate financial operations
CREATE TABLE IF NOT EXISTS idempotency_keys (
    key VARCHAR(100) PRIMARY KEY,
    user_id INTEGER,
    endpoint VARCHAR(200),
    request_hash VARCHAR(64),
    response_status INTEGER,
    response_body TEXT,
    created_at TIMESTAMP DEFAULT NOW(),
    expires_at TIMESTAMP NOT NULL
);
CREATE INDEX IF NOT EXISTS idx_idempotency_expires ON idempotency_keys(expires_at);
CREATE INDEX IF NOT EXISTS idx_idempotency_user ON idempotency_keys(user_id);

-- 4. Ensure audit_log table exists (matches AuthService.cs usage)
CREATE TABLE IF NOT EXISTS audit_log (
    id SERIAL PRIMARY KEY,
    user_id INTEGER,
    username VARCHAR(100),
    action VARCHAR(50),
    entity_type VARCHAR(50),
    entity_id INTEGER,
    details TEXT,
    created_at TIMESTAMP DEFAULT NOW()
);
CREATE INDEX IF NOT EXISTS idx_audit_log_user ON audit_log(user_id);
CREATE INDEX IF NOT EXISTS idx_audit_log_created ON audit_log(created_at);