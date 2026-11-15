# MIGRATION_STRATEGY.md

> Phase 2 — Architecture Design (Design Only)
> Generated: 2026-11-15

---

## 1. Migration Approach

### 1.1 Principles

1. **Additive only** — add columns/tables, never drop
2. **Test on isolated DB first** — verify data integrity
3. **Backup before migration** — full dump of production
4. **Rollback plan** — documented for each migration
5. **No production migration without approval** — explicit sign-off required

### 1.2 Migration Phases

| Phase | Description | Risk | Approval |
|-------|-------------|------|----------|
| M1 | Create new tables (tenants, local_databases, devices, etc.) | Low | Required |
| M2 | Add TenantId column (nullable) to all 86 tables | Medium | Required |
| M3 | Backfill TenantId = default tenant | Medium | Required |
| M4 | Make TenantId NOT NULL | High | Required |
| M5 | Add LocalDatabaseId column (nullable) | Medium | Required |
| M6 | Backfill LocalDatabaseId = default local DB | Medium | Required |
| M7 | Make LocalDatabaseId NOT NULL | High | Required |
| M8 | Add Version column (default 1) | Low | Required |
| M9 | Add IsDeleted column (default false) | Low | Required |
| M10 | Add CreatedAt/UpdatedAt where missing | Low | Required |
| M11 | Add FK constraints | High | Required |
| M12 | Add indexes | Low | Required |
| M13 | Add RLS policies | High | Required |
| M14 | Verify data integrity | Low | Required |
| M15 | Test rollback | Low | Required |

---

## 2. Default Tenant Creation

### 2.1 Create Default Tenant

```sql
INSERT INTO tenants (id, name, slug, status, created_at, schema_version)
VALUES (gen_random_uuid(), 'Default Tenant', 'default', 'active', NOW(), 1);
```

### 2.2 Backfill TenantId

```sql
-- For all 86 tables
UPDATE invoices SET tenant_id = (SELECT id FROM tenants WHERE slug = 'default') WHERE tenant_id IS NULL;
UPDATE payments SET tenant_id = (SELECT id FROM tenants WHERE slug = 'default') WHERE tenant_id IS NULL;
-- ... (repeat for all tables)
```

### 2.3 Create Default Local Database

```sql
INSERT INTO local_databases (id, tenant_id, replica_id, installation_id, database_fingerprint, database_status, schema_version, created_at)
VALUES (gen_random_uuid(), (SELECT id FROM tenants WHERE slug = 'default'), gen_random_uuid(), gen_random_uuid(), 'default-fingerprint', 'active', 1, NOW());
```

### 2.4 Backfill LocalDatabaseId

```sql
-- For all local tables
UPDATE invoices SET local_database_id = (SELECT id FROM local_databases LIMIT 1) WHERE local_database_id IS NULL;
-- ... (repeat for all local tables)
```

---

## 3. Data Integrity Verification

### 3.1 Pre-Migration Checks

```sql
-- Verify no NULL tenant_id after backfill
SELECT COUNT(*) FROM invoices WHERE tenant_id IS NULL;
-- Expected: 0

-- Verify no orphaned records
SELECT COUNT(*) FROM invoices i LEFT JOIN parties p ON p.id = i.party_id WHERE p.id IS NULL;
-- Expected: 0

-- Verify balance integrity
SELECT SUM(debit) - SUM(credit) FROM journal_lines;
-- Expected: 0
```

### 3.2 Post-Migration Checks

```sql
-- Verify all tables have tenant_id
SELECT table_name FROM information_schema.columns WHERE column_name = 'tenant_id' AND table_schema = 'public';
-- Expected: 60 tables

-- Verify FK constraints
SELECT COUNT(*) FROM information_schema.table_constraints WHERE constraint_type = 'FOREIGN KEY' AND table_schema = 'public';
-- Expected: > 5 (new constraints added)

-- Verify RLS policies
SELECT COUNT(*) FROM pg_policies WHERE schemaname = 'public';
-- Expected: > 0
```

---

## 4. Rollback Plan

### 4.1 Rollback Steps

```sql
-- 1. Drop RLS policies
DROP POLICY IF EXISTS tenant_isolation ON invoices;
-- ... (repeat for all tables)

-- 2. Drop indexes
DROP INDEX IF EXISTS idx_invoices_tenant;
-- ... (repeat for all indexes)

-- 3. Drop FK constraints
ALTER TABLE invoices DROP CONSTRAINT IF EXISTS invoices_tenant_id_fkey;
-- ... (repeat for all constraints)

-- 4. Drop new columns
ALTER TABLE invoices DROP COLUMN IF EXISTS tenant_id;
ALTER TABLE invoices DROP COLUMN IF EXISTS local_database_id;
ALTER TABLE invoices DROP COLUMN IF EXISTS version;
ALTER TABLE invoices DROP COLUMN IF EXISTS is_deleted;
ALTER TABLE invoices DROP COLUMN IF EXISTS deleted_at;
ALTER TABLE invoices DROP COLUMN IF EXISTS deleted_by;
-- ... (repeat for all tables)

-- 5. Drop new tables
DROP TABLE IF EXISTS sync_conflicts;
DROP TABLE IF EXISTS sync_outbox;
DROP TABLE IF EXISTS sync_log;
DROP TABLE IF EXISTS backup_metadata;
DROP TABLE IF EXISTS audit_trail;
DROP TABLE IF EXISTS local_sessions;
DROP TABLE IF EXISTS local_memberships;
DROP TABLE IF EXISTS local_users;
DROP TABLE IF EXISTS local_databases;
DROP TABLE IF EXISTS device_tenant_links;
DROP TABLE IF EXISTS devices;
DROP TABLE IF EXISTS tenant_memberships;
DROP TABLE IF EXISTS tenants;
```

### 4.2 Rollback Verification

```sql
-- Verify all new tables dropped
SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = 'public' AND table_name IN ('tenants', 'local_databases', 'devices');
-- Expected: 0

-- Verify all new columns dropped
SELECT COUNT(*) FROM information_schema.columns WHERE table_schema = 'public' AND column_name = 'tenant_id';
-- Expected: 0

-- Verify data integrity (counts unchanged)
SELECT COUNT(*) FROM invoices;
-- Expected: same as before migration
```

---

## 5. Testing Strategy

### 5.1 Test on Isolated DB

1. Create isolated DB: `techtoon_test_ci` (copy of `techtoon_test`)
2. Run migration scripts on `techtoon_test_ci`
3. Verify data integrity
4. Run all tests (156 + new)
5. Test rollback
6. Verify data integrity after rollback

### 5.2 Test Cases

| Test | Description | Expected |
|------|-------------|----------|
| Migration success | Run all migration scripts | Success, no errors |
| Data integrity | Verify counts/relationships/balances | All match |
| Tenant filtering | Query with TenantId filter | Only tenant's data returned |
| RLS policy | Query without TenantId | No data returned (or error) |
| Rollback | Run rollback scripts | Success, data restored |
| Performance | Query performance after migration | No degradation |

---

## 6. خلاصة

### Migration Strategy Summary

| Phase | Description | Risk | Approval |
|-------|-------------|------|----------|
| M1 | Create new tables | Low | Required |
| M2-M4 | Add TenantId + backfill + NOT NULL | High | Required |
| M5-M7 | Add LocalDatabaseId + backfill + NOT NULL | High | Required |
| M8-M10 | Add Version, IsDeleted, timestamps | Low | Required |
| M11 | Add FK constraints | High | Required |
| M12 | Add indexes | Low | Required |
| M13 | Add RLS policies | High | Required |
| M14-M15 | Verify + test rollback | Low | Required |

### Key Principles

1. **Additive only** — no data deletion
2. **Test on isolated DB first** — verify data integrity
3. **Backup before migration** — full dump
4. **Rollback plan** — documented for each migration
5. **No production migration without approval** — explicit sign-off

---
*Generated by OpenCode — Phase 2 Architecture Design — 2026-11-15*
