# LOCAL_DATABASE_ISOLATION.md

> Phase 2 — Architecture Design (Design Only)
> Generated: 2026-11-15

---

## 1. Local Database Identity

### 1.1 LocalDatabaseId

- **Type**: Guid (UUID)
- **Generated**: On first database creation
- **Stored**: In `local_databases` table + DB metadata
- **Immutable**: Cannot be changed after creation
- **Verification**: DatabaseFingerprint (hash of DB file/content)

### 1.2 DatabaseFingerprint

- **Algorithm**: SHA-256 hash of (file size + first/last 1MB + creation timestamp)
- **Stored**: In `local_databases.DatabaseFingerprint`
- **Verified**: On every DB open
- **Mismatch**: Reject access (DB may have been tampered/copied)

### 1.3 InstallationId

- **Type**: Guid (UUID)
- **Generated**: On first app installation
- **Stored**: In app config (not in DB)
- **Purpose**: Identify the installation (not the DB)
- **Verification**: Compared with DB's InstallationId

### 1.4 ReplicaId

- **Type**: Guid (UUID)
- **Generated**: On first sync registration
- **Stored**: In `local_databases.ReplicaId`
- **Unique**: Globally unique (enforced by constraint)
- **Purpose**: Identify this DB as a sync replica

---

## 2. Local Database Isolation Model

### 2.1 Isolation Layers

```
Layer 1: File System
  - DB file location (configurable)
  - File permissions (OS-level)
  - File encryption (optional)

Layer 2: Database Identity
  - LocalDatabaseId (GUID)
  - DatabaseFingerprint (SHA-256)
  - InstallationId (GUID)
  - TenantId (FK → tenants)

Layer 3: User Isolation
  - Local users (per DB)
  - Local memberships (per DB)
  - Local permissions (per DB)
  - No cross-DB user access

Layer 4: Session Isolation
  - Session tied to LocalDatabaseId
  - No session reuse across DBs
  - Session invalidated on DB switch

Layer 5: API Isolation
  - All API calls include LocalDatabaseId
  - API validates LocalDatabaseId + TenantId
  - No cross-DB API access
```

### 2.2 Opening a Local Database

```
1. Read DB file path from config
2. Open DB connection
3. Read LocalDatabaseId from DB metadata
4. Read TenantId from DB metadata
5. Compute DatabaseFingerprint (hash of DB file)
6. Compare with stored fingerprint → reject if mismatch
7. Verify DatabaseStatus == Active
8. Verify InstallationId matches (or register new)
9. Read LocalUser (from local_users table)
10. Verify password hash
11. Verify LocalMembership (user has role in this DB)
12. Verify Device (device is registered and active)
13. Verify DeviceTenantLink (device authorized for this tenant)
14. Create new Session (isolated)
15. Load permissions (tenant + local)
16. Set CurrentLocalDatabaseId, CurrentTenantId, CurrentSessionId
```

### 2.3 Switching Databases

```
1. Close current session
2. Invalidate current session token
3. Clear current permissions
4. Clear current data context
5. Open new DB (follow "Opening a Local Database" flow)
6. Create new session
7. Load new permissions
8. Update UI (refresh all data)
```

### 2.4 Preventing Isolation Bypass

| Attack | Prevention |
|--------|-----------|
| Rename DB file | DatabaseFingerprint mismatch → reject |
| Copy DB to another machine | InstallationId mismatch → reject |
| Modify TenantId manually | DatabaseFingerprint mismatch → reject |
| Modify LocalDatabaseId manually | DatabaseFingerprint mismatch → reject |
| Replace DB with another | DatabaseFingerprint mismatch → reject |
| Use old session | Session tied to LocalDatabaseId → reject |
| Use token from another DB | Token tied to LocalDatabaseId → reject |
| Direct API access | API validates LocalDatabaseId + TenantId + DeviceId |
| SQL injection | Parameterized queries + input validation |

### 2.5 Database Fingerprint Algorithm

```
Fingerprint = SHA-256(
    FileSize +
    First 1MB of file +
    Last 1MB of file +
    CreationTimestamp +
    LocalDatabaseId +
    TenantId
)
```

**Verification:**
- On DB open: compute fingerprint, compare with stored
- On sync: verify fingerprint matches cloud record
- On backup: store fingerprint in backup metadata

---

## 3. Local User Management

### 3.1 Local User Table

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

### 3.2 Local Membership Table

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

### 3.3 User Operations

| Operation | Scope | Audit |
|-----------|-------|-------|
| Create user | Local DB only | ✅ |
| Edit user | Local DB only | ✅ |
| Deactivate user | Local DB only | ✅ |
| Delete user | Soft delete (if has records) | ✅ |
| Change password | Local DB only | ✅ |
| Reset password | Local DB only (admin) | ✅ |
| Assign role | Local DB only | ✅ |
| Grant/deny permission | Local DB only | ✅ |

### 3.4 Cloud User Link

- `cloud_user_id` is optional (local-only users have NULL)
- Cloud user provides identity (email, name) but NOT permissions
- Permissions come from local membership, not cloud membership
- Cloud user can be linked to multiple local users (one per DB)

---

## 4. Session Management

### 4.1 Session Structure

| Field | Type | Description |
|-------|------|-------------|
| SessionId | Guid (PK) | Unique session ID |
| LocalDatabaseId | Guid (FK) | Owner local DB |
| LocalUserId | int (FK) | Logged-in user |
| DeviceId | Guid (FK) | Device |
| TenantId | Guid (FK) | Tenant |
| CreatedAt | DateTime | Session creation |
| ExpiresAt | DateTime | Session expiry |
| LastActivityAt | DateTime | Last activity |
| IsOffline | bool | Offline mode flag |
| OfflineExpiresAt | DateTime? | Offline expiry |
| Token | string | Session token (JWT) |

### 4.2 Session Rules

1. **One session per local DB** — switching DBs creates new session
2. **No session reuse** — old session invalidated on DB switch
3. **Session tied to LocalDatabaseId** — token includes LocalDatabaseId
4. **Session expiry** — configurable (default 8 hours)
5. **Offline expiry** — configurable (default 72 hours)
6. **Revocation** — immediate (on next API call or sync)

### 4.3 Session Invalidation Triggers

- User logout
- DB switch
- User revoked
- Device revoked
- Tenant suspended
- Local DB suspended/revoked
- Session expired
- Password changed
- Permissions changed

---

## 5. Database Switching Flow

```
User selects "Switch Database" →
  1. Confirm dialog (unsaved changes?)
  2. Close current session (invalidate token)
  3. Clear current data context (DbContext dispose)
  4. Clear current permissions
  5. Open new DB file
  6. Verify fingerprint
  7. Verify status (Active)
  8. Prompt for local login (if required)
  9. Verify local user + membership
  10. Verify device authorization
  11. Create new session
  12. Load new permissions
  13. Refresh all data (UI reload)
  14. Update app title (show DB name)
```

### 5.1 UI Indicators

- App title bar: "Techtoon Accounting — [DB Name] — [Tenant Name]"
- Status bar: "Connected to: [LocalDatabaseId short]"
- Warning: "You are now working on [DB Name]. Data from [previous DB] is no longer visible."

---

## 6. Backup vs Active Replica

### 6.1 State Machine

```
                    ┌──────────┐
                    │  Active  │
                    └────┬─────┘
                         │ Create Backup
                         ▼
                    ┌──────────┐
                    │  Backup  │ (not a device, no sync, no login)
                    └────┬─────┘
                         │ Restore
                         ▼
                    ┌──────────────────────┐
                    │ RestoredPending      │
                    │ Activation           │ (read-only, no sync)
                    └────┬─────────────────┘
                         │ Admin Approves
                         ▼
                    ┌──────────┐
                    │  Active  │ (new LocalDatabaseId, ReplicaId, InstallationId)
                    └──────────┘
```

### 6.2 Backup Metadata

| Field | Type | Description |
|-------|------|-------------|
| BackupId | Guid (PK) | Unique backup ID |
| TenantId | Guid (FK) | Owner tenant |
| LocalDatabaseId | Guid (FK) | Original local DB |
| SchemaVersion | int | Schema version at backup time |
| CreatedAt | DateTime | Backup creation |
| CreatedBy | int (FK → local_users) | Creator |
| FileSize | long | Backup file size |
| Fingerprint | string | SHA-256 of backup file |
| Encrypted | bool | Encryption status |
| Status | enum | Valid, Corrupted, Expired |

### 6.3 Activation Flow

```
1. Admin uploads backup file
2. System verifies backup metadata (TenantId, SchemaVersion, Fingerprint)
3. System creates restored DB (state: RestoredPendingActivation)
4. System verifies data integrity (counts, relationships, balances)
5. Admin reviews and approves activation
6. System creates new LocalDatabaseId, ReplicaId, InstallationId
7. System sets state to Active
8. System enables sync (with conflict resolution for old changes)
9. System logs activation in Audit Trail
```

### 6.4 Preventing Duplicate ReplicaId

- Unique constraint on `local_databases.ReplicaId`
- On activation: generate new ReplicaId (GUID)
- Cloud validates: no two active local DBs with same ReplicaId
- If duplicate detected: reject sync, alert admin

---

## 7. Local Database Encryption

### 7.1 Encryption Options

| Option | Technology | Pros | Cons |
|--------|-----------|------|------|
| SQLite + SQLCipher | SQLCipher | Built-in, transparent | Requires SQLCipher library |
| PostgreSQL + pgcrypto | pgcrypto | PostgreSQL native | Complex key management |
| File-level (BitLocker) | OS-level | Transparent | Requires BitLocker |
| Application-level | AES-256 | Full control | Complex implementation |

### 7.2 Recommendation

- **Desktop**: Use SQLCipher (SQLite) for local DB — transparent encryption, no performance impact
- **Mobile**: Use SQLCipher (SQLite) — same as desktop
- **Cloud**: Neon PostgreSQL with TLS (already encrypted in transit)

### 7.3 Key Management

- Encryption key stored in OS keychain (Windows Credential Manager / macOS Keychain)
- Key derived from user password + device fingerprint
- Key never stored in source code or config files
- Key rotation supported (re-encrypt on password change)

### 7.4 Backup Encryption

- All backups encrypted with AES-256
- Encryption key stored separately from backup
- Key stored in OS keychain
- Backup verification includes decryption test

---

## 8. Migration Strategy

### 8.1 Approach: Additive Only

1. **No data deletion** — only add columns/tables
2. **No production migration** without approval
3. **Test on isolated DB first** — verify data integrity
4. **Rollback plan** — documented for each migration

### 8.2 Migration Steps

1. Create new tables (tenants, local_databases, devices, etc.)
2. Add TenantId column to all 86 tables (nullable initially)
3. Backfill: set TenantId = default tenant for existing data
4. Make TenantId NOT NULL (after backfill)
5. Add LocalDatabaseId column (nullable initially)
6. Backfill: set LocalDatabaseId = default local DB
7. Make LocalDatabaseId NOT NULL (after backfill)
8. Add Version column (default 1)
9. Add IsDeleted column (default false)
10. Add CreatedAt/UpdatedAt where missing
11. Add FK constraints
12. Add indexes
13. Add RLS policies
14. Verify data integrity
15. Test rollback

### 8.3 Default Tenant

- Create default tenant for existing data
- TenantId = first tenant created
- All existing data assigned to default tenant
- No data loss

### 8.4 Rollback

- Drop new columns (TenantId, LocalDatabaseId, Version, IsDeleted)
- Drop new tables
- Restore from backup
- Verify data integrity

---

## 9. Performance Considerations

### 9.1 Tenant Filtering

- Index on TenantId for all data tables
- Composite index (TenantId, Id) for PK lookups
- Query optimization (avoid N+1)
- Connection pooling (existing)

### 9.2 Sync Performance

- Batch sync (multiple events per request)
- Incremental sync (only changed records)
- Compression (gzip for large payloads)
- Retry with exponential backoff
- Rate limiting on sync endpoints

### 9.3 Local DB Performance

- SQLite for local DB (faster than PostgreSQL for single-user)
- Or keep PostgreSQL (consistent with cloud)
- Index on LocalDatabaseId for all local tables
- Query optimization

---

## 10. خلاصة

### الملفات المطلوبة (11 files)

1. ✅ MULTI_TENANT_ARCHITECTURE.md
2. ✅ LOCAL_DATABASE_ISOLATION.md (this file)
3. ⏳ LOCAL_USER_ACCESS_CONTROL.md
4. ⏳ DEVICE_SECURITY_DESIGN.md
5. ⏳ SESSION_SECURITY_DESIGN.md
6. ⏳ BACKUP_AND_REPLICA_POLICY.md
7. ⏳ OFFLINE_AUTHORIZATION_POLICY.md
8. ⏳ SYNC_ENGINE_ARCHITECTURE.md
9. ⏳ TENANT_SECURITY_ARCHITECTURE.md
10. ⏳ ARCHITECTURE_DECISIONS.md
11. ⏳ PHASE_2_REVIEW_CHECKLIST.md

---
*Generated by OpenCode — Phase 2 Architecture Design — 2026-11-15*
