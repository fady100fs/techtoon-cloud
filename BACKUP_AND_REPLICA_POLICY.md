# BACKUP_AND_REPLICA_POLICY.md

> Phase 2 — Architecture Design (Design Only)
> Generated: 2026-11-15

---

## 1. Backup Lifecycle

### 1.1 Backup States

| State | Description | Sync Allowed | Login Allowed |
|-------|-------------|-------------|---------------|
| Active | Production database in use | ✅ Yes | ✅ Yes |
| Backup | Backup copy (not active) | ❌ No | ❌ No |
| RestoredPendingActivation | Restored backup awaiting activation | ❌ No | ⚠️ Read-only |
| Suspended | Temporarily suspended | ❌ No | ❌ No |
| Revoked | Permanently revoked | ❌ No | ❌ No |
| RecoveryMode | In recovery process | ⚠️ Limited | ⚠️ Limited |

### 1.2 Backup Types

| Type | Description | Frequency | Retention |
|------|-------------|-----------|-----------|
| Full Backup | Complete database dump | Daily | 30 days |
| Incremental Backup | Changed records only | Hourly | 7 days |
| Transaction Log Backup | WAL (Write-Ahead Log) | Continuous | 7 days |
| Manual Backup | User-initiated | On demand | Until deleted |

---

## 2. Backup Metadata

### 2.1 Backup Metadata Table

```sql
CREATE TABLE backup_metadata (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL REFERENCES tenants(id),
    local_database_id UUID NOT NULL REFERENCES local_databases(id),
    schema_version INT NOT NULL,
    backup_type VARCHAR(20) NOT NULL, -- full, incremental, transaction_log, manual
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    created_by INT REFERENCES local_users(id),
    file_size BIGINT NOT NULL,
    fingerprint VARCHAR(64) NOT NULL, -- SHA-256
    encrypted BOOLEAN NOT NULL DEFAULT FALSE,
    encryption_key_id VARCHAR(50),
    status VARCHAR(20) NOT NULL DEFAULT 'valid', -- valid, corrupted, expired
    expires_at TIMESTAMP,
    verified_at TIMESTAMP NULL,
    verified_by INT REFERENCES local_users(id)
);
```

### 2.2 Backup File Structure

```
backup_<tenant>_<localdb>_<timestamp>.sql.gz.enc
├── Header (JSON)
│   ├── BackupId
│   ├── TenantId
│   ├── LocalDatabaseId
│   ├── SchemaVersion
│   ├── CreatedAt
│   ├── CreatedBy
│   ├── FileSize
│   ├── Fingerprint
│   ├── Encrypted
│   └── EncryptionKeyId
├── Body (SQL dump, gzipped, encrypted)
└── Footer (checksum)
```

---

## 3. Backup Creation

### 3.1 Backup Flow

```
1. User initiates backup (manual or scheduled)
2. System verifies user has "settings.backup" permission
3. System creates backup (SQL dump)
4. System compresses (gzip)
5. System encrypts (AES-256, if enabled)
6. System computes fingerprint (SHA-256)
7. System stores backup file
8. System creates backup_metadata record
9. System logs in Audit Trail
10. System verifies backup (decryption test + integrity check)
```

### 3.2 Backup Verification

```
1. Read backup file
2. Verify fingerprint (SHA-256)
3. Decrypt (if encrypted)
4. Decompress (gzip)
5. Parse SQL (syntax check)
6. Verify table counts
7. Verify relationships
8. Mark as verified (verified_at, verified_by)
```

---

## 4. Restore Flow

### 4.1 Restore Steps

```
1. User uploads backup file
2. System verifies backup metadata (TenantId, SchemaVersion, Fingerprint)
3. System creates restored DB (state: RestoredPendingActivation)
4. System verifies data integrity (counts, relationships, balances)
5. Admin reviews and approves activation
6. System creates new LocalDatabaseId, ReplicaId, InstallationId
7. System sets state to Active
8. System enables sync (with conflict resolution for old changes)
9. System logs activation in Audit Trail
```

### 4.2 Restore Verification

```
1. Read backup file
2. Verify fingerprint
3. Decrypt (if encrypted)
4. Decompress (gzip)
5. Create new DB
6. Execute SQL (in transaction)
7. Verify table counts
8. Verify relationships (FK integrity)
9. Verify balances (sum of debits = sum of credits)
10. Verify stock levels (non-negative)
11. Mark as verified
```

### 4.3 Activation Flow

```
1. Admin reviews restored DB
2. Admin clicks "Activate"
3. System generates new LocalDatabaseId (GUID)
4. System generates new ReplicaId (GUID)
5. System generates new InstallationId (GUID)
6. System updates local_databases record
7. System sets state to Active
8. System enables sync
9. System logs activation in Audit Trail
```

---

## 5. Preventing Duplicate ReplicaId

### 5.1 Unique Constraint

```sql
ALTER TABLE local_databases ADD CONSTRAINT local_databases_replicaid_unique UNIQUE (replica_id);
```

### 5.2 Cloud Validation

```
1. Local DB sends sync request with ReplicaId
2. Cloud checks: is this ReplicaId already registered?
3. If yes: check if it's the same LocalDatabaseId
   - If same: allow sync
   - If different: reject sync, alert admin
4. If no: register new ReplicaId
```

### 5.3 Duplicate Detection

```
1. Cloud receives sync from ReplicaId X
2. Cloud checks: is X already active?
3. If X is active from another LocalDatabaseId:
   - Reject sync
   - Alert admin
   - Log in Audit Trail
4. If X is not active:
   - Allow sync
   - Mark X as active
```

---

## 6. Backup vs Active Replica

### 6.1 Key Differences

| Attribute | Backup | Active Replica |
|-----------|--------|----------------|
| State | Backup | Active |
| Sync Allowed | ❌ No | ✅ Yes |
| Login Allowed | ❌ No | ✅ Yes |
| Device ID | None | Required |
| Replica ID | Original (stored) | New (generated on activation) |
| Tenant ID | Original | Same (on activation) |
| Last Sync | Stored (historical) | Updated on sync |
| Can Send Changes | ❌ No | ✅ Yes |

### 6.2 Preventing Backup from Syncing

```
1. Backup file has no DeviceId
2. Backup file has no active session
3. Backup file has no sync token
4. On restore: state = RestoredPendingActivation
5. Sync disabled until activation
6. Activation creates new identity
```

### 6.3 Preventing Duplicate Active Replicas

```
1. Unique constraint on ReplicaId
2. Cloud validation on sync
3. Activation creates new ReplicaId
4. Old ReplicaId marked as inactive
5. No two active replicas with same ReplicaId
```

---

## 7. Old Changes After Restore

### 7.1 Conflict Resolution

When a restored backup (with old changes) is activated:

```
1. System compares local version with cloud version
2. For each entity:
   - If local version == cloud version: no conflict
   - If local version > cloud version: apply local change (with audit)
   - If local version < cloud version: apply cloud change (with audit)
   - If both changed: conflict → manual review
3. Financial records (invoices, payments): manual review required
4. Reference data (customers, products): last write wins (with audit)
5. Stock levels: sum (additive)
```

### 7.2 Conflict Resolution UI

```
1. Show conflict list (entity, local version, cloud version)
2. For each conflict: show both versions
3. Admin chooses: keep local, keep cloud, or merge
4. System applies decision
5. System logs decision in Audit Trail
```

### 7.3 Preventing Data Loss

```
1. All changes are versioned
2. All changes are in outbox
3. Sync is incremental (only changed records)
4. Conflicts are detected before applying
5. Manual review required for financial records
6. Audit trail records all decisions
```

---

## 8. Backup Verification Test

### 8.1 Test Procedure

```
1. Create backup from test DB
2. Verify backup metadata
3. Restore backup to new DB
4. Verify data integrity:
   - Table counts match
   - Relationships intact
   - Balances correct (debits = credits)
   - Stock levels non-negative
   - No orphaned records
5. Verify encryption (if encrypted)
6. Verify fingerprint
7. Test activation flow
8. Test sync after activation
```

### 8.2 Test Cases

| Test | Description | Expected |
|------|-------------|----------|
| Backup creation | Create backup from test DB | Success, metadata created |
| Backup verification | Verify backup file | Success, fingerprint matches |
| Restore | Restore to new DB | Success, data intact |
| Data integrity | Verify counts/relationships/balances | All match |
| Activation | Activate restored DB | New identity created |
| Sync after activation | Sync activated DB | Success, no duplicates |
| Old changes conflict | Restore old backup, sync | Conflict detected, manual review |
| Duplicate ReplicaId | Try to activate with existing ReplicaId | Rejected |

---

## 9. Backup Security

### 9.1 Encryption

- AES-256 encryption for backup files
- Encryption key stored in OS keychain
- Key never in source code or config
- Key rotation supported

### 9.2 Access Control

- Only admin can create/restore backups
- Backup files stored in secure location
- Backup metadata in database (tenant-scoped)
- Backup verification requires admin

### 9.3 Audit

- All backup operations logged
- All restore operations logged
- All activation events logged
- All conflict resolutions logged

---

## 10. خلاصة

### Backup Lifecycle

```
Active → Backup → RestoredPendingActivation → Active (new identity)
```

### Key Principles

1. **Backup ≠ Device** — no sync, no login
2. **Restore ≠ Activate** — explicit activation required
3. **Activation = New Identity** — new LocalDatabaseId, ReplicaId, InstallationId
4. **No Duplicate ReplicaId** — unique constraint + cloud validation
5. **No Auto-Sync After Restore** — requires activation first
6. **Conflict Resolution** — manual review for financial records
7. **Audit Everything** — all operations logged

---
*Generated by OpenCode — Phase 2 Architecture Design — 2026-11-15*
