# MULTI_TENANT_ARCHITECTURE.md

> Phase 2 — Architecture Design (Design Only, No Code Changes)
> Generated: 2026-11-15 · OpenCode (GLM 5.3)
> Based on: TECHTOON_DISCOVERY_REPORT.md

---

## 1. المعمارية العامة (Overall Architecture)

### 1.1 نمط المعمارية (Architecture Pattern)

**Multi-Tenant SaaS with Local-First Database Isolation**

```
┌─────────────────────────────────────────────────────────────┐
│                    CLOUD LAYER (Neon PostgreSQL)            │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐                  │
│  │ Tenant A │  │ Tenant B │  │ Tenant C │  (Row-Level)     │
│  │ (TenantId│  │ (TenantId│  │ (TenantId│                  │
│  │ filtered)│  │ filtered)│  │ filtered)│                  │
│  └────┬─────┘  └────┬─────┘  └────┬─────┘                  │
│       │              │              │                        │
│  ┌────┴──────────────┴──────────────┴────┐                  │
│  │         ASP.NET Core API (Fly.io)     │                  │
│  │  - Tenant Validation Middleware       │                  │
│  │  - JWT + Refresh Tokens               │                  │
│  │  - Permission-based Authorization     │                  │
│  │  - Sync Endpoints (push/pull)         │                  │
│  │  - Conflict Resolution                │                  │
│  └────┬──────────────┬──────────────┬────┘                  │
└───────┼──────────────┼──────────────┼───────────────────────┘
        │              │              │
   ┌────┴────┐    ┌────┴────┐    ┌────┴────┐
   │ Desktop │    │ Desktop │    │ Mobile  │
   │   A     │    │   B     │    │  (PWA)  │
   │ LocalDB │    │ LocalDB │    │  (API)  │
   │   A     │    │   B     │    │         │
   └─────────┘    └─────────┘    └─────────┘
```

### 1.2 مبادئ التصميم (Design Principles)

1. **Tenant Isolation First**: كل query يُفلتر بـ TenantId (app-level + DB-level RLS).
2. **Local-First**: Desktop يعمل محليًا، sync لاحقًا.
3. **Zero-Trust**: لا ثقة تلقائية — كل طلب يُتحقق من هويته وصلاحياته.
4. **Additive Changes**: فقط إضافة columns/tables — لا حذف أو تعديل existing.
5. **Idempotency**: كل sync event قابل لإعادة الإرسال دون تكرار.
6. **Conflict Detection**: لا Last Write Wins للمستندات المحاسبية.

---

## 2. المكونات (Components)

### 2.1 Tenant

| Attribute | Type | Description |
|-----------|------|-------------|
| TenantId | Guid (PK) | Unique identifier |
| Name | string | Company name |
| Slug | string (unique) | URL-friendly name |
| Status | enum | Active, Suspended, Revoked |
| CreatedAt | DateTime | Creation timestamp |
| CreatedBy | int (FK → users) | Creator |
| Settings | JSON | Tenant-specific settings |
| SchemaVersion | int | Schema version for migration |

**Relationships:**
- 1 Tenant → N TenantMemberships
- 1 Tenant → N LocalDatabases
- 1 Tenant → N Devices
- 1 Tenant → N (all data tables with TenantId)

### 2.2 Cloud User

| Attribute | Type | Description |
|-----------|------|-------------|
| UserId | int (PK) | Unique identifier |
| Username | string (unique) | Login name |
| Email | string (unique) | Email address |
| PasswordHash | string | bcrypt hash |
| FullName | string | Display name |
| Status | enum | Active, Suspended, Revoked |
| CreatedAt | DateTime | Creation timestamp |
| LastLogin | DateTime? | Last login timestamp |

**Relationships:**
- 1 User → N TenantMemberships
- 1 User → N LocalUserAccounts (per local DB)
- 1 User → N Devices (registered)

### 2.3 Tenant Membership

| Attribute | Type | Description |
|-----------|------|-------------|
| MembershipId | Guid (PK) | Unique identifier |
| UserId | int (FK → users) | User |
| TenantId | Guid (FK → tenants) | Tenant |
| RoleId | int (FK → roles) | Role within tenant |
| Status | enum | Active, Suspended, Revoked, Pending |
| Permissions | JSON | Per-user overrides |
| CreatedAt | DateTime | Membership creation |
| ApprovedBy | int? (FK → users) | Approver |
| RevokedAt | DateTime? | Revocation timestamp |

**Constraints:**
- Unique: (UserId, TenantId) — one membership per user per tenant
- FK: UserId → users(id), TenantId → tenants(id), RoleId → roles(id)

### 2.4 Local Database

| Attribute | Type | Description |
|-----------|------|-------------|
| LocalDatabaseId | Guid (PK) | Unique identifier |
| TenantId | Guid (FK → tenants) | Owner tenant |
| ReplicaId | Guid (unique) | Replica identifier (for sync) |
| InstallationId | Guid | Installation identifier |
| DatabaseFingerprint | string | Hash of DB file/content |
| DatabaseStatus | enum | Active, Offline, Backup, RestoredPendingActivation, Suspended, Revoked, RecoveryMode |
| SchemaVersion | int | Current schema version |
| EncryptionStatus | enum | None, Encrypted, Pending |
| CreatedAt | DateTime | Creation timestamp |
| LastSuccessfulSync | DateTime? | Last sync timestamp |
| LastSeenAt | DateTime? | Last heartbeat |

**Relationships:**
- 1 LocalDatabase → 1 Tenant
- 1 LocalDatabase → N LocalUsers
- 1 LocalDatabase → N Devices (registered to this DB)
- 1 LocalDatabase → N SyncOutbox events

### 2.5 Device

| Attribute | Type | Description |
|-----------|------|-------------|
| DeviceId | Guid (PK) | Unique identifier |
| DeviceType | enum | Desktop, Mobile, Web |
| InstallationId | Guid | Installation identifier |
| TenantId | Guid (FK → tenants) | Owner tenant |
| LocalDatabaseId | Guid? (FK → local_databases) | Associated local DB |
| DeviceStatus | enum | Active, Suspended, Revoked, Pending |
| DeviceFingerprint | string | Hash of device hardware |
| RegisteredAt | DateTime | Registration timestamp |
| LastSeenAt | DateTime? | Last heartbeat |
| RevokedAt | DateTime? | Revocation timestamp |
| AppVersion | string | Application version |

**Relationships:**
- 1 Device → 1 Tenant
- 1 Device → 0..1 LocalDatabase
- 1 Device → N DeviceTenantLinks

### 2.6 Device-Tenant Link

| Attribute | Type | Description |
|-----------|------|-------------|
| LinkId | Guid (PK) | Unique identifier |
| DeviceId | Guid (FK → devices) | Device |
| TenantId | Guid (FK → tenants) | Tenant |
| Status | enum | Active, Suspended, Revoked |
| Permissions | JSON | Device-specific permissions |
| LinkedAt | DateTime | Link creation |
| LinkedBy | int (FK → users) | Linker |
| RevokedAt | DateTime? | Revocation timestamp |

### 2.7 Local User (per Local Database)

| Attribute | Type | Description |
|-----------|------|-------------|
| LocalUserId | int (PK) | Unique within local DB |
| LocalDatabaseId | Guid (FK → local_databases) | Owner local DB |
| CloudUserId | int? (FK → users) | Linked cloud user (optional) |
| Username | string | Login name (local) |
| PasswordHash | string | bcrypt hash |
| FullName | string | Display name |
| RoleId | int (FK → roles) | Local role |
| IsActive | bool | Active status |
| CreatedAt | DateTime | Creation timestamp |
| LastLogin | DateTime? | Last login |

**Key principle:** Local user is **independent** from cloud user. Same email in two local DBs = two independent users with independent permissions.

### 2.8 Local Database Membership

| Attribute | Type | Description |
|-----------|------|-------------|
| MembershipId | Guid (PK) | Unique identifier |
| LocalUserId | int (FK → local_users) | User |
| LocalDatabaseId | Guid (FK → local_databases) | Local DB |
| RoleId | int (FK → roles) | Local role |
| Permissions | JSON | Per-user overrides |
| Status | enum | Active, Suspended, Revoked |
| CreatedAt | DateTime | Membership creation |
| CreatedBy | int (FK → local_users) | Creator |

---

## 3. الفصل بين Tenant وLocal Database

### 3.1 Tenant Isolation (Company Level)

```
Company A (TenantId=A)          Company B (TenantId=B)
├── Desktop A1 (LocalDB A1)     ├── Desktop B1 (LocalDB B1)
├── Desktop A2 (LocalDB A2)     ├── Desktop B2 (LocalDB B2)
├── Mobile A1 (Device)          └── Mobile B1 (Device)
└── Mobile A2 (Device)
```

- Tenant A's data is filtered by `TenantId = A` in ALL queries
- Tenant B's data is filtered by `TenantId = B` in ALL queries
- No cross-tenant access at any layer (API, DB, Service, UI)

### 3.2 Local Database Isolation (Device Level)

```
Device A (LocalDatabaseId=A)    Device B (LocalDatabaseId=B)
├── LocalUser: Ahmed            ├── LocalUser: Ahmed (same email, different user)
├── LocalUser: Mohamed          ├── LocalUser: Sara
├── Session: A (isolated)       ├── Session: B (isolated)
└── Permissions: A-specific     └── Permissions: B-specific
```

- Local user Ahmed in DB A ≠ Local user Ahmed in DB B
- Permissions in DB A ≠ Permissions in DB B
- Session in DB A ≠ Session in DB B
- Switching DBs = close session + re-authenticate + re-verify permissions

### 3.3 Verification Flow (Opening a Local Database)

```
1. Read LocalDatabaseId from DB file
2. Read TenantId from DB file
3. Verify DatabaseFingerprint (hash of DB file)
4. Verify DatabaseStatus (Active only)
5. Verify LocalUser (username + password hash)
6. Verify LocalMembership (user has role in this DB)
7. Verify Device (device is registered and active)
8. Verify DeviceTenantLink (device authorized for this tenant)
9. Create new Session (isolated, no reuse of previous session)
10. Load permissions for this user in this DB
```

### 3.4 Prevention of Isolation Bypass

| Attack | Prevention |
|--------|-----------|
| Rename DB file | DatabaseFingerprint mismatch → reject |
| Copy DB to another machine | InstallationId mismatch → reject |
| Modify TenantId manually | DatabaseFingerprint mismatch → reject |
| Modify LocalDatabaseId manually | DatabaseFingerprint mismatch → reject |
| Replace DB with another | DatabaseFingerprint mismatch → reject |
| Use old session | Session tied to LocalDatabaseId → reject |
| Use token from another DB | Token tied to LocalDatabaseId → reject |
| Direct API access | API validates TenantId + Membership + Device |

---

## 4. Backup vs Active Replica

### 4.1 States

| State | Description | Sync Allowed | Login Allowed |
|-------|-------------|-------------|---------------|
| Active | Production database in use | ✅ Yes | ✅ Yes |
| Offline | No internet connection | ⏸️ Queued | ✅ Yes (within offline window) |
| Backup | Backup copy (not active) | ❌ No | ❌ No |
| RestoredPendingActivation | Restored backup awaiting activation | ❌ No | ⚠️ Read-only |
| Suspended | Temporarily suspended | ❌ No | ❌ No |
| Revoked | Permanently revoked | ❌ No | ❌ No |
| RecoveryMode | In recovery process | ⚠️ Limited | ⚠️ Limited |

### 4.2 Backup Lifecycle

```
Active DB → Create Backup → Backup File (with metadata)
                              ├── BackupId
                              ├── TenantId
                              ├── LocalDatabaseId (original)
                              ├── SchemaVersion
                              ├── CreatedAt
                              ├── Encrypted (yes/no)
                              └── Fingerprint

Restore Backup → RestoredPendingActivation DB
                              ├── New LocalDatabaseId (on activation)
                              ├── New ReplicaId (on activation)
                              ├── New InstallationId (on activation)
                              └── Requires explicit activation

Activate → Active DB (new identity)
```

### 4.3 Rules

1. **Backup is NOT a device** — no DeviceId, no sync, no login
2. **Backup is NOT an active replica** — cannot sync until activated
3. **Restored backup requires explicit activation** — admin must approve
4. **Activation creates new identity** — new LocalDatabaseId, ReplicaId, InstallationId
5. **No duplicate ReplicaId** — enforced by unique constraint
6. **No auto-sync after restore** — requires activation first
7. **Old changes from restored backup** — conflict resolution applies

### 4.4 Restore Flow

```
1. Upload backup file
2. Verify backup metadata (TenantId, SchemaVersion, Fingerprint)
3. Create restored DB (state: RestoredPendingActivation)
4. Verify data integrity (counts, relationships, balances)
5. Admin reviews and approves activation
6. Create new LocalDatabaseId, ReplicaId, InstallationId
7. Set state to Active
8. Enable sync (with conflict resolution for old changes)
```

---

## 5. Owner/Admin Scope

### 5.1 Role Hierarchy

```
System Owner (platform level)
  └── Tenant Owner (company level)
       └── Tenant Admin (company management)
            └── Local Database Admin (device/DB level)
                 └── Local User (standard user)
```

### 5.2 Scope Matrix

| Role | Scope | Can Access Other Tenants? | Can Access Other Local DBs? | Can Grant Permissions? |
|------|-------|--------------------------|----------------------------|------------------------|
| System Owner | Platform | ✅ All (with audit) | ✅ All (with audit) | ✅ Yes |
| Tenant Owner | Own tenant only | ❌ No | ❌ No | ✅ Own tenant only |
| Tenant Admin | Own tenant only | ❌ No | ❌ No | ✅ Own tenant only |
| Local DB Admin | Own local DB only | ❌ No | ❌ No | ✅ Own local DB only |
| Local User | Own permissions | ❌ No | ❌ No | ❌ No |

### 5.3 Emergency Access

- System Owner can access any tenant/DB **only with explicit audit**
- Requires: reason, duration, approval, audit log
- All actions logged in Audit Trail
- Time-limited (max 24 hours)
- Cannot modify data without explicit permission

### 5.4 No Master Password / Backdoor

- No universal password
- No bypass of authentication
- No direct DB access without verification
- All access through API with proper auth

---

## 6. Authentication & Authorization

### 6.1 Authentication Levels

```
Level 1: Cloud Authentication (API)
  - Username + Password → JWT (access + refresh)
  - Device registration
  - Tenant membership verification

Level 2: Local Authentication (Desktop)
  - Local username + password → Local session
  - Local database verification
  - Device fingerprint verification

Level 3: Tenant Authorization
  - Verify user is member of tenant
  - Verify user has required role
  - Verify user has required permissions

Level 4: Local Database Authorization
  - Verify user exists in local DB
  - Verify user has local role
  - Verify user has local permissions
  - Verify local DB belongs to tenant

Level 5: Device Authorization
  - Verify device is registered
  - Verify device is active
  - Verify device belongs to tenant
  - Verify device fingerprint
```

### 6.2 Login Flow

```
1. User enters username + password
2. Cloud Auth: verify credentials → JWT
3. Cloud Auth: verify tenant membership → tenant list
4. User selects tenant
5. Cloud Auth: verify device registration
6. Local Auth: verify local user (if offline) or cloud user
7. Local Auth: verify local DB membership
8. Local Auth: verify device fingerprint
9. Create session (isolated per local DB)
10. Load permissions (tenant + local)
```

### 6.3 Session Isolation

- Each local DB has its own session
- Switching DBs = close session + re-authenticate
- No session reuse across DBs
- Session tied to LocalDatabaseId + DeviceId + UserId

### 6.4 Offline Authentication

- Local password hash stored (bcrypt)
- Offline session valid for configurable duration (default 72h)
- Last cloud verification timestamp stored
- Warning when approaching expiry
- Re-verification required on reconnect
- If user revoked during offline → reject on reconnect

---

## 7. Offline Authorization Policy

### 7.1 Policy Parameters

| Parameter | Default | Configurable | Description |
|-----------|---------|-------------|-------------|
| OfflineValidityHours | 72 | Yes | Hours of offline access |
| WarningThresholdHours | 12 | Yes | Hours before expiry to warn |
| MaxOfflineTransactions | 1000 | Yes | Max transactions while offline |
| ReverificationRequired | Yes | Yes | Require cloud re-verification on reconnect |

### 7.2 Offline Flow

```
1. User logs in (cloud + local)
2. Cloud verification successful → store last verification timestamp
3. Internet lost → switch to offline mode
4. Local auth continues (within validity window)
5. Transactions queued in outbox
6. Warning shown when approaching expiry
7. Internet restored → re-verify with cloud
8. If user revoked → reject, lock local session
9. If user active → sync queued transactions
10. If conflict → resolve via conflict resolution
```

### 7.3 User Revocation During Offline

```
1. Admin revokes user on Device B (online)
2. Revocation recorded in cloud
3. Device A (offline) cannot know immediately
4. Device A continues within offline window (policy)
5. Internet restored on Device A
6. Re-verification fails (user revoked)
7. Local session locked
8. Pending transactions reviewed (may be rejected)
9. Audit trail records the revocation event
```

### 7.4 Warning UI

- Banner: "Offline mode — verification expires in X hours"
- Banner: "Offline mode — X transactions queued"
- Dialog: "Offline verification expired — please connect to internet"
- Dialog: "Your account has been revoked — contact administrator"

---

## 8. Sync Architecture

### 8.1 Outbox Pattern

```
Local DB Change → Outbox Event → Sync Queue → API → Neon
```

**Outbox Event Structure:**
| Field | Type | Description |
|-------|------|-------------|
| EventId | Guid (PK) | Unique event ID |
| TenantId | Guid (FK) | Owner tenant |
| LocalDatabaseId | Guid (FK) | Source local DB |
| ReplicaId | Guid | Source replica |
| DeviceId | Guid (FK) | Source device |
| EntityType | string | Entity type (invoice, payment, etc.) |
| EntityId | string | Entity ID |
| OperationType | enum | Create, Update, Delete, Cancel |
| Version | int | Entity version |
| CreatedAt | DateTime | Event creation |
| Payload | JSON | Serialized entity |
| SyncStatus | enum | Pending, Sent, Acknowledged, Failed |
| RetryCount | int | Number of retries |
| LastError | string? | Last error message |

### 8.2 Idempotency

- Each event has unique EventId
- API checks EventId before processing
- Duplicate EventId → return existing result (no new record)
- Uses `idempotency_keys` table (existing)

### 8.3 Versioning

- Each entity has `Version` column (int, auto-increment)
- On update: `Version = Version + 1`
- Sync checks: if cloud version > local version → conflict
- Sync checks: if local version > cloud version → apply local change

### 8.4 Conflict Resolution

| Entity Type | Conflict Strategy | Description |
|-------------|-------------------|-------------|
| Invoice | Manual review | Show both versions, admin decides |
| Payment | Manual review | Show both versions, admin decides |
| Customer | Last write wins (with audit) | Log the overwrite |
| Product | Last write wins (with audit) | Log the overwrite |
| Stock level | Sum (additive) | Add both changes |
| Settings | Last write wins | Log the overwrite |
| User permissions | Manual review | Admin decides |

### 8.5 Soft Delete

- No hard delete for financial records
- `IsDeleted = true` + `DeletedAt` + `DeletedBy`
- Sync propagates soft delete
- Restore possible (with audit)

### 8.6 Event Ordering

- Events ordered by `CreatedAt` (timestamp)
- Events ordered by `Version` (entity version)
- Events ordered by `EventId` (tiebreaker)
- Sync processes events in order

### 8.7 Sync Flow

```
Desktop Change → Outbox Event → Sync Queue
                                    ↓
                              API Push Endpoint
                                    ↓
                              Neon (validate + apply)
                                    ↓
                              Sync Acknowledgement
                                    ↓
                              Mobile Pull (on next sync)
```

### 8.8 Sync Monitoring

- Last successful sync timestamp
- Pending events count
- Failed events count
- Conflict count
- API status
- Neon status
- Connection status
- Retry queue

---

## 9. Migration Strategy

### 9.1 Approach: Additive Only

1. **No data deletion** — only add columns/tables
2. **No production migration** without approval
3. **Test on isolated DB first** — verify data integrity
4. **Rollback plan** — documented for each migration

### 9.2 Migration Steps

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

### 9.3 Default Tenant

- Create default tenant for existing data
- TenantId = first tenant created
- All existing data assigned to default tenant
- No data loss

### 9.4 Rollback

- Drop new columns (TenantId, LocalDatabaseId, Version, IsDeleted)
- Drop new tables
- Restore from backup
- Verify data integrity

---

## 10. Security Architecture

### 10.1 Threat Model

| Threat | Mitigation |
|--------|-----------|
| Cross-tenant data access | TenantId filter on all queries + RLS |
| Cross-DB data access | LocalDatabaseId filter + session isolation |
| Session hijacking | JWT + refresh token rotation + device fingerprint |
| Credential stuffing | Rate limiting + account lockout |
| Brute force | Rate limiting + CAPTCHA (future) |
| Data tampering | Audit trail + integrity checks |
| Backup theft | Encryption + access control |
| API abuse | Rate limiting + CORS + input validation |
| Secret leakage | No secrets in client, env vars for server |
| Offline data theft | Local DB encryption + device fingerprint |

### 10.2 Defense in Depth

1. **API Level**: Tenant validation middleware, permission checks
2. **Database Level**: RLS policies, FK constraints, indexes
3. **Application Level**: Service-layer tenant filtering, permission checks
4. **UI Level**: Page-level and action-level permission gating
5. **Device Level**: Device fingerprint, session isolation

### 10.3 Secret Management

- No Neon connection string in client
- No Fly keys in client
- No admin passwords in client
- API secrets in environment variables
- Desktop secrets in DPAPI-encrypted files
- Mobile secrets in secure storage

---

## 11. الأداء (Performance Considerations)

### 11.1 Tenant Filtering

- Index on TenantId for all data tables
- Composite index (TenantId, Id) for PK lookups
- Query optimization (avoid N+1)
- Connection pooling (existing)

### 11.2 Sync Performance

- Batch sync (multiple events per request)
- Incremental sync (only changed records)
- Compression (gzip for large payloads)
- Retry with exponential backoff
- Rate limiting on sync endpoints

### 11.3 Local DB Performance

- SQLite for local DB (faster than PostgreSQL for single-user)
- Or keep PostgreSQL (consistent with cloud)
- Index on LocalDatabaseId for all local tables
- Query optimization

---

## 12. خلاصة Phase 2

### الملفات المطلوبة (11 files)

1. ✅ MULTI_TENANT_ARCHITECTURE.md (this file)
2. ⏳ LOCAL_DATABASE_ISOLATION.md
3. ⏳ LOCAL_USER_ACCESS_CONTROL.md
4. ⏳ DEVICE_SECURITY_DESIGN.md
5. ⏳ SESSION_SECURITY_DESIGN.md
6. ⏳ BACKUP_AND_REPLICA_POLICY.md
7. ⏳ OFFLINE_AUTHORIZATION_POLICY.md
8. ⏳ SYNC_ENGINE_ARCHITECTURE.md
9. ⏳ TENANT_SECURITY_ARCHITECTURE.md
10. ⏳ ARCHITECTURE_DECISIONS.md
11. ⏳ PHASE_2_REVIEW_CHECKLIST.md

### Architecture Decision Records (ADRs)

| # | Decision | Status |
|---|----------|--------|
| ADR-1 | Tenant isolation via TenantId column + RLS | NEEDS APPROVAL |
| ADR-2 | Local DB isolation via LocalDatabaseId + fingerprint | NEEDS APPROVAL |
| ADR-3 | Backup vs Replica: separate states, explicit activation | NEEDS APPROVAL |
| ADR-4 | No master password/backdoor | APPROVED |
| ADR-5 | Sync: Outbox + Idempotency + Versioning | NEEDS APPROVAL |
| ADR-6 | Conflict resolution: manual for financial, LWW for reference | NEEDS APPROVAL |
| ADR-7 | Offline auth: 72h default, configurable | NEEDS APPROVAL |
| ADR-8 | Soft delete for financial records | NEEDS APPROVAL |
| ADR-9 | Additive migrations only | APPROVED |
| ADR-10 | No production migration without approval | APPROVED |

### Points Needing Approval

1. **TenantId on all 86 tables** — requires production migration
2. **RLS policies** — requires DB admin privileges
3. **Sync engine** — new endpoints, new tables
4. **Offline auth policy** — 72h default, configurable
5. **Conflict resolution** — manual review for financial records
6. **Backup encryption** — requires key management
7. **Device fingerprint** — requires hardware access
8. **Local DB encryption** — requires key management

### Next Phase

**Phase 3 (Database Design)** — requires approval to proceed.

Phase 3 will create:
- `TENANT_DATABASE_DESIGN.md` — detailed schema design
- Migration scripts (on test DB only)
- Rollback plan

No code changes, no production migration, no deployment.

---
*Generated by OpenCode — Phase 2 Architecture Design — 2026-11-15*
*Status: Design Complete · Awaiting Review & Approval*
