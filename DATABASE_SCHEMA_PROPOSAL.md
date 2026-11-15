# DATABASE_SCHEMA_PROPOSAL.md

> Phase 2 — Architecture Design (Design Only)
> Generated: 2026-11-15

---

## 1. New Tables (10)

### 1.1 tenants

```sql
CREATE TABLE tenants (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(200) NOT NULL,
    slug VARCHAR(50) UNIQUE NOT NULL,
    status VARCHAR(20) NOT NULL DEFAULT 'active', -- active, suspended, revoked
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    created_by INT REFERENCES users(id),
    settings JSONB,
    schema_version INT NOT NULL DEFAULT 1,
    is_active BOOLEAN NOT NULL DEFAULT TRUE
);
```

### 1.2 tenant_memberships

```sql
CREATE TABLE tenant_memberships (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id INT NOT NULL REFERENCES users(id),
    tenant_id UUID NOT NULL REFERENCES tenants(id),
    role_id INT NOT NULL REFERENCES roles(id),
    status VARCHAR(20) NOT NULL DEFAULT 'active',
    permissions JSONB,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    approved_by INT REFERENCES users(id),
    revoked_at TIMESTAMP NULL,
    UNIQUE (user_id, tenant_id)
);
```

### 1.3 local_databases

```sql
CREATE TABLE local_databases (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL REFERENCES tenants(id),
    replica_id UUID UNIQUE NOT NULL,
    installation_id UUID NOT NULL,
    database_fingerprint VARCHAR(64) NOT NULL,
    database_status VARCHAR(30) NOT NULL DEFAULT 'active',
    schema_version INT NOT NULL DEFAULT 1,
    encryption_status VARCHAR(20) NOT NULL DEFAULT 'none',
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    last_successful_sync TIMESTAMP NULL,
    last_seen_at TIMESTAMP NULL
);
```

### 1.4 devices

```sql
CREATE TABLE devices (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    device_type VARCHAR(20) NOT NULL, -- Desktop, Mobile, Web
    installation_id UUID NOT NULL,
    tenant_id UUID NOT NULL REFERENCES tenants(id),
    local_database_id UUID REFERENCES local_databases(id),
    device_status VARCHAR(20) NOT NULL DEFAULT 'pending',
    device_fingerprint VARCHAR(255) NOT NULL,
    registered_at TIMESTAMP NOT NULL DEFAULT NOW(),
    last_seen_at TIMESTAMP NULL,
    revoked_at TIMESTAMP NULL,
    app_version VARCHAR(50),
    os_info VARCHAR(100),
    UNIQUE (installation_id, tenant_id)
);
```

### 1.5 device_tenant_links

```sql
CREATE TABLE device_tenant_links (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    device_id UUID NOT NULL REFERENCES devices(id),
    tenant_id UUID NOT NULL REFERENCES tenants(id),
    status VARCHAR(20) NOT NULL DEFAULT 'active',
    permissions JSONB,
    linked_at TIMESTAMP NOT NULL DEFAULT NOW(),
    linked_by INT REFERENCES users(id),
    revoked_at TIMESTAMP NULL,
    UNIQUE (device_id, tenant_id)
);
```

### 1.6 sync_outbox

```sql
CREATE TABLE sync_outbox (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL REFERENCES tenants(id),
    local_database_id UUID NOT NULL REFERENCES local_databases(id),
    replica_id UUID NOT NULL,
    device_id UUID NOT NULL REFERENCES devices(id),
    entity_type VARCHAR(50) NOT NULL,
    entity_id VARCHAR(50) NOT NULL,
    operation_type VARCHAR(20) NOT NULL, -- Create, Update, Delete, Cancel
    version INT NOT NULL,
    payload JSONB NOT NULL,
    sync_status VARCHAR(20) NOT NULL DEFAULT 'pending',
    retry_count INT NOT NULL DEFAULT 0,
    last_error TEXT,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    sent_at TIMESTAMP NULL,
    acknowledged_at TIMESTAMP NULL,
    UNIQUE (tenant_id, entity_type, entity_id, version)
);
```

### 1.7 sync_conflicts

```sql
CREATE TABLE sync_conflicts (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL REFERENCES tenants(id),
    local_database_id UUID NOT NULL REFERENCES local_databases(id),
    entity_type VARCHAR(50) NOT NULL,
    entity_id VARCHAR(50) NOT NULL,
    local_version INT NOT NULL,
    cloud_version INT NOT NULL,
    local_payload JSONB NOT NULL,
    cloud_payload JSONB NOT NULL,
    conflict_type VARCHAR(30) NOT NULL,
    status VARCHAR(20) NOT NULL DEFAULT 'pending',
    resolution VARCHAR(20),
    resolved_by INT REFERENCES users(id),
    resolved_at TIMESTAMP NULL,
    created_at TIMESTAMP NOT NULL DEFAULT NOW()
);
```

### 1.8 sync_log

```sql
CREATE TABLE sync_log (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL REFERENCES tenants(id),
    local_database_id UUID NOT NULL REFERENCES local_databases(id),
    device_id UUID NOT NULL REFERENCES devices(id),
    sync_type VARCHAR(20) NOT NULL,
    status VARCHAR(20) NOT NULL,
    events_sent INT NOT NULL DEFAULT 0,
    events_received INT NOT NULL DEFAULT 0,
    conflicts INT NOT NULL DEFAULT 0,
    errors INT NOT NULL DEFAULT 0,
    started_at TIMESTAMP NOT NULL DEFAULT NOW(),
    completed_at TIMESTAMP NULL,
    error_message TEXT
);
```

### 1.9 backup_metadata

```sql
CREATE TABLE backup_metadata (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL REFERENCES tenants(id),
    local_database_id UUID NOT NULL REFERENCES local_databases(id),
    schema_version INT NOT NULL,
    backup_type VARCHAR(20) NOT NULL,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    created_by INT REFERENCES local_users(id),
    file_size BIGINT NOT NULL,
    fingerprint VARCHAR(64) NOT NULL,
    encrypted BOOLEAN NOT NULL DEFAULT FALSE,
    encryption_key_id VARCHAR(50),
    status VARCHAR(20) NOT NULL DEFAULT 'valid',
    expires_at TIMESTAMP,
    verified_at TIMESTAMP NULL,
    verified_by INT REFERENCES local_users(id)
);
```

### 1.10 local_users

```sql
CREATE TABLE local_users (
    id SERIAL PRIMARY KEY,
    local_database_id UUID NOT NULL REFERENCES local_databases(id),
    cloud_user_id INT NULL REFERENCES users(id),
    username VARCHAR(50) NOT NULL,
    password_hash VARCHAR(255) NOT NULL,
    full_name VARCHAR(100) NOT NULL,
    role_id INT NOT NULL REFERENCES roles(id),
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    last_login TIMESTAMP NULL,
    UNIQUE (local_database_id, username)
);
```

### 1.11 local_memberships

```sql
CREATE TABLE local_memberships (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    local_user_id INT NOT NULL REFERENCES local_users(id),
    local_database_id UUID NOT NULL REFERENCES local_databases(id),
    role_id INT NOT NULL REFERENCES roles(id),
    permissions JSONB,
    status VARCHAR(20) NOT NULL DEFAULT 'active',
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    created_by INT REFERENCES local_users(id),
    UNIQUE (local_user_id, local_database_id)
);
```

### 1.12 local_sessions

```sql
CREATE TABLE local_sessions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    local_database_id UUID NOT NULL REFERENCES local_databases(id),
    local_user_id INT NOT NULL REFERENCES local_users(id),
    device_id UUID NOT NULL REFERENCES devices(id),
    tenant_id UUID NOT NULL REFERENCES tenants(id),
    token VARCHAR(500) NOT NULL,
    refresh_token VARCHAR(500),
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    expires_at TIMESTAMP NOT NULL,
    last_activity_at TIMESTAMP NOT NULL DEFAULT NOW(),
    is_offline BOOLEAN NOT NULL DEFAULT FALSE,
    offline_expires_at TIMESTAMP NULL,
    is_revoked BOOLEAN NOT NULL DEFAULT FALSE,
    revoked_at TIMESTAMP NULL,
    revoked_reason VARCHAR(100)
);
```

### 1.13 audit_trail (Enhanced)

```sql
CREATE TABLE audit_trail (
    id SERIAL PRIMARY KEY,
    tenant_id UUID NOT NULL REFERENCES tenants(id),
    local_database_id UUID REFERENCES local_databases(id),
    device_id UUID REFERENCES devices(id),
    user_id INT REFERENCES users(id),
    local_user_id INT REFERENCES local_users(id),
    action VARCHAR(50) NOT NULL,
    entity_type VARCHAR(50) NOT NULL,
    entity_id VARCHAR(50),
    old_values JSONB,
    new_values JSONB,
    ip_address VARCHAR(45),
    user_agent TEXT,
    source VARCHAR(20) NOT NULL,
    created_at TIMESTAMP NOT NULL DEFAULT NOW()
);
```

---

## 2. Modifications to Existing Tables

### 2.1 Add TenantId + LocalDatabaseId + Version + IsDeleted

For all 60 tenant-owned tables:

```sql
-- Example for invoices table
ALTER TABLE invoices ADD COLUMN tenant_id UUID REFERENCES tenants(id);
ALTER TABLE invoices ADD COLUMN local_database_id UUID REFERENCES local_databases(id);
ALTER TABLE invoices ADD COLUMN version INT NOT NULL DEFAULT 1;
ALTER TABLE invoices ADD COLUMN is_deleted BOOLEAN NOT NULL DEFAULT FALSE;
ALTER TABLE invoices ADD COLUMN deleted_at TIMESTAMP NULL;
ALTER TABLE invoices ADD COLUMN deleted_by INT REFERENCES users(id);
ALTER TABLE invoices ADD COLUMN created_at TIMESTAMP NOT NULL DEFAULT NOW();
ALTER TABLE invoices ADD COLUMN updated_at TIMESTAMP NULL;
```

### 2.2 Add TenantId to Global Reference Tables (8)

```sql
-- For currencies, categories, expense_categories, stock_reasons, etc.
ALTER TABLE currencies ADD COLUMN tenant_id UUID REFERENCES tenants(id);
-- ... (may be NULL for global reference data)
```

### 2.3 Add LocalDatabaseId to Local Device Tables (4)

```sql
-- Already has local_database_id in new tables
-- user_devices needs local_database_id
ALTER TABLE user_devices ADD COLUMN local_database_id UUID REFERENCES local_databases(id);
```

### 2.4 Add Indexes

```sql
-- For all tenant-owned tables
CREATE INDEX idx_invoices_tenant ON invoices(tenant_id);
CREATE INDEX idx_invoices_tenant_id ON invoices(tenant_id, id);
CREATE INDEX idx_invoices_local_db ON invoices(local_database_id);
CREATE INDEX idx_invoices_version ON invoices(version);
CREATE INDEX idx_invoices_deleted ON invoices(is_deleted) WHERE is_deleted = FALSE;
```

### 2.5 Add RLS Policies

```sql
-- Row Level Security for tenant isolation
ALTER TABLE invoices ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation ON invoices
    USING (tenant_id = current_setting('app.current_tenant_id')::uuid);
```

---

## 3. Foreign Key Strategy

### 3.1 Current State

- Only 5 FK constraints exist (role_permissions, subscriptions_party, subscription_periods_*)
- Most integrity is app-level

### 3.2 Proposed Changes

| Table | FK Column | References | ON DELETE | ON UPDATE |
|-------|-----------|------------|-----------|-----------|
| All tenant-owned tables | `tenant_id` | `tenants(id)` | RESTRICT | NO ACTION |
| All local tables | `local_database_id` | `local_databases(id)` | RESTRICT | NO ACTION |
| All data tables | `created_by` | `users(id)` | SET NULL | NO ACTION |
| All data tables | `updated_by` | `users(id)` | SET NULL | NO ACTION |
| All data tables | `deleted_by` | `users(id)` | SET NULL | NO ACTION |

### 3.3 Composite Keys (where needed)

| Table | Composite Key | Purpose |
|-------|--------------|---------|
| `role_permissions` | `(role_id, permission_key)` | Unique permission per role |
| `user_permissions` | `(user_id, permission_key)` | Unique permission per user |
| `local_memberships` | `(local_user_id, local_database_id)` | One membership per user per DB |
| `device_tenant_links` | `(device_id, tenant_id)` | One link per device per tenant |

---

## 4. Index Strategy

### 4.1 Required Indexes (per tenant-owned table)

```sql
CREATE INDEX idx_{table}_tenant ON {table}(tenant_id);
CREATE INDEX idx_{table}_tenant_id ON {table}(tenant_id, id);
CREATE INDEX idx_{table}_local_db ON {table}(local_database_id);
CREATE INDEX idx_{table}_version ON {table}(version);
CREATE INDEX idx_{table}_deleted ON {table}(is_deleted) WHERE is_deleted = FALSE;
CREATE INDEX idx_{table}_created ON {table}(created_at);
```

### 4.2 Special Indexes

```sql
-- For sync queries
CREATE INDEX idx_sync_outbox_status ON sync_outbox(sync_status) WHERE sync_status = 'pending';
CREATE INDEX idx_sync_outbox_tenant ON sync_outbox(tenant_id, created_at);
CREATE INDEX idx_sync_conflicts_status ON sync_conflicts(status) WHERE status = 'pending';

-- For audit trail
CREATE INDEX idx_audit_trail_tenant ON audit_trail(tenant_id, created_at);
CREATE INDEX idx_audit_trail_entity ON audit_trail(entity_type, entity_id);

-- For local sessions
CREATE INDEX idx_local_sessions_db ON local_sessions(local_database_id, is_revoked);
CREATE INDEX idx_local_sessions_user ON local_sessions(local_user_id, is_revoked);
```

---

## 5. Data Types

### 5.1 ID Types

| Entity | ID Type | Reason |
|--------|---------|--------|
| Tenant | UUID | Global, distributed |
| Local Database | UUID | Global, distributed |
| Device | UUID | Global, distributed |
| User (Cloud) | INT (SERIAL) | Existing, local |
| User (Local) | INT (SERIAL) | Per-DB, local |
| All data tables | INT (SERIAL) | Existing, per-tenant |

### 5.2 Timestamp Types

| Column | Type | Reason |
|--------|------|--------|
| `created_at` | TIMESTAMP WITH TIME ZONE | UTC, consistent |
| `updated_at` | TIMESTAMP WITH TIME ZONE | UTC, consistent |
| `deleted_at` | TIMESTAMP WITH TIME ZONE | UTC, consistent |
| `last_seen_at` | TIMESTAMP WITH TIME ZONE | UTC, consistent |
| `expires_at` | TIMESTAMP WITH TIME ZONE | UTC, consistent |

### 5.3 JSON Columns

| Column | Type | Purpose |
|--------|------|---------|
| `settings` (tenants) | JSONB | Tenant-specific settings |
| `permissions` (memberships) | JSONB | Per-user overrides |
| `payload` (sync_outbox) | JSONB | Serialized entity |
| `old_values` (audit_trail) | JSONB | Before values |
| `new_values` (audit_trail) | JSONB | After values |

---

## 6. Schema Version

### 6.1 Version Tracking

- `tenants.schema_version` — current schema version for the tenant
- `local_databases.schema_version` — current schema version for the local DB
- Migration scripts increment schema version
- Sync checks schema version compatibility

### 6.2 Version Compatibility

- Cloud API accepts sync from local DBs with schema version >= minimum supported
- Local DBs with older schema version are prompted to update
- Schema version mismatch → sync blocked, update required

---

## 7. خلاصة

### Schema Changes Summary

| Change | Count | Impact |
|--------|-------|--------|
| New tables | 13 | Low (additive) |
| Modified tables (add columns) | 86 | Medium (additive) |
| New indexes | ~200 | Low (performance) |
| New FK constraints | ~100 | Medium (integrity) |
| New RLS policies | 60 | Medium (security) |
| Schema version | 1 → 2 | Low |

### Risk Assessment

| Risk | Level | Mitigation |
|------|-------|-----------|
| Migration failure | High | Test on isolated DB first |
| Data loss | Critical | Backup before migration, rollback plan |
| Performance degradation | Medium | Indexes, query optimization |
| FK constraint violations | Medium | Backfill before adding constraints |
| RLS policy errors | Medium | Test thoroughly, fallback to app-level |

---
*Generated by OpenCode — Phase 2 Architecture Design — 2026-11-15*
