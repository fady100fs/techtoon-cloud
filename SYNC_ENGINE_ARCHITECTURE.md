# SYNC_ENGINE_ARCHITECTURE.md

> Phase 2 — Architecture Design (Design Only)
> Generated: 2026-11-15

---

## 1. Sync Architecture Overview

### 1.1 Sync Paths

```
Desktop (Local DB) ←→ API (Fly.io) ←→ Neon (Cloud DB) ←→ Mobile (PWA)
```

### 1.2 Sync Direction

- **Bidirectional**: Desktop ↔ Cloud ↔ Mobile
- **Push**: Local changes → Cloud
- **Pull**: Cloud changes → Local
- **Conflict Resolution**: Manual review for financial records

---

## 2. Outbox Pattern

### 2.1 Outbox Table

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
    sync_status VARCHAR(20) NOT NULL DEFAULT 'pending', -- pending, sent, acknowledged, failed
    retry_count INT NOT NULL DEFAULT 0,
    last_error TEXT,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    sent_at TIMESTAMP NULL,
    acknowledged_at TIMESTAMP NULL,
    UNIQUE (tenant_id, entity_type, entity_id, version)
);
```

### 2.2 Outbox Flow

```
1. Local DB change (insert/update/delete)
2. Trigger creates outbox event
3. Event queued in sync_outbox (status: pending)
4. Sync worker picks up pending events
5. Worker sends event to API (push)
6. API validates and applies to Neon
7. API returns acknowledgement
8. Worker marks event as acknowledged
9. On failure: retry with exponential backoff
```

### 2.3 Idempotency

- Each event has unique EventId
- API checks EventId before processing
- Duplicate EventId → return existing result (no new record)
- Uses `idempotency_keys` table (existing)

---

## 3. Versioning

### 3.1 Entity Version

```sql
ALTER TABLE invoices ADD COLUMN version INT NOT NULL DEFAULT 1;
ALTER TABLE payments ADD COLUMN version INT NOT NULL DEFAULT 1;
-- ... all data tables
```

### 3.2 Version Increment

- On create: `Version = 1`
- On update: `Version = Version + 1`
- On delete (soft): `Version = Version + 1`, `IsDeleted = true`

### 3.3 Sync Version Check

```
1. Local sends event with Version V
2. Cloud checks: current version in Neon
3. If V == cloud version: apply (no conflict)
4. If V > cloud version: apply (local is newer)
5. If V < cloud version: conflict (cloud is newer)
6. If V == cloud version but different data: conflict (concurrent edit)
```

---

## 4. Pull/Push Sync

### 4.1 Push (Local → Cloud)

```
1. Sync worker picks up pending outbox events
2. Batch events (max 100 per request)
3. Send to API: POST /api/sync/push
4. API validates each event:
   - TenantId matches
   - DeviceId authorized
   - Version check
   - Idempotency check
5. API applies changes to Neon
6. API returns acknowledgement (event IDs + results)
7. Worker marks events as acknowledged
8. On failure: retry with exponential backoff
```

### 4.2 Pull (Cloud → Local)

```
1. Sync worker requests changes from cloud
2. Send to API: GET /api/sync/pull?since=<timestamp>&tenantId=<id>
3. API returns changes since last sync
4. Worker applies changes to local DB
5. Worker updates last sync timestamp
6. On conflict: queue for manual review
```

### 4.3 Sync Frequency

| Mode | Frequency | Description |
|------|-----------|-------------|
| Real-time | On change | Immediate sync (when online) |
| Periodic | Every 5 minutes | Background sync |
| Manual | On demand | User-initiated sync |
| Offline | Queued | Sync when online |

---

## 5. Conflict Resolution

### 5.1 Conflict Types

| Type | Description | Strategy |
|------|-------------|----------|
| Concurrent Edit | Both sides edited same record | Manual review |
| Concurrent Delete | One deleted, one edited | Manual review |
| Version Mismatch | Versions don't match | Manual review |
| Schema Mismatch | Different schema versions | Block sync, require migration |

### 5.2 Conflict Resolution Rules

| Entity Type | Strategy | Description |
|-------------|----------|-------------|
| Invoice | Manual review | Show both versions, admin decides |
| Payment | Manual review | Show both versions, admin decides |
| Customer | Last write wins (with audit) | Log the overwrite |
| Product | Last write wins (with audit) | Log the overwrite |
| Stock level | Sum (additive) | Add both changes |
| Settings | Last write wins | Log the overwrite |
| User permissions | Manual review | Admin decides |
| Accounting period | Manual review | Admin decides |

### 5.3 Conflict Resolution Flow

```
1. Conflict detected during sync
2. System creates conflict record (sync_conflicts table)
3. System pauses sync for conflicting entity
4. Admin reviews conflict (shows both versions)
5. Admin chooses: keep local, keep cloud, or merge
6. System applies decision
7. System resumes sync
8. System logs decision in Audit Trail
```

### 5.4 Conflict Record Table

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
    conflict_type VARCHAR(30) NOT NULL, -- concurrent_edit, concurrent_delete, version_mismatch
    status VARCHAR(20) NOT NULL DEFAULT 'pending', -- pending, resolved, ignored
    resolution VARCHAR(20), -- keep_local, keep_cloud, merge
    resolved_by INT REFERENCES users(id),
    resolved_at TIMESTAMP NULL,
    created_at TIMESTAMP NOT NULL DEFAULT NOW()
);
```

---

## 6. Soft Delete

### 6.1 Soft Delete Columns

```sql
ALTER TABLE invoices ADD COLUMN is_deleted BOOLEAN NOT NULL DEFAULT FALSE;
ALTER TABLE invoices ADD COLUMN deleted_at TIMESTAMP NULL;
ALTER TABLE invoices ADD COLUMN deleted_by INT REFERENCES users(id);
-- ... all data tables
```

### 6.2 Soft Delete Rules

1. **No hard delete** for financial records (invoices, payments, journal entries)
2. **Soft delete** for reference data (customers, products, etc.)
3. **Sync propagates** soft delete
4. **Restore possible** (with audit)
5. **Hard delete** only for test data (with approval)

### 6.3 Soft Delete Sync

```
1. Local soft delete → outbox event (operation: Delete)
2. Cloud applies soft delete (IsDeleted = true)
3. Pull sync propagates soft delete to other devices
4. All devices show record as deleted
5. Restore possible (admin action)
```

---

## 7. Event Ordering

### 7.1 Ordering Rules

1. **By CreatedAt** (timestamp) — primary
2. **By Version** (entity version) — secondary
3. **By EventId** (GUID) — tiebreaker

### 7.2 Ordering Implementation

```
1. Sync worker sorts events by (CreatedAt, Version, EventId)
2. Events processed in order
3. If event fails: retry later (don't block subsequent events)
4. If dependency: wait for dependency (e.g., invoice before payment)
```

### 7.3 Dependency Handling

| Entity | Depends On |
|--------|-----------|
| InvoiceLine | Invoice |
| Payment | Invoice (optional) |
| JournalLine | JournalEntry |
| StockLevel | Item, Warehouse |
| LoanInstallment | Loan |
| SubscriptionPeriod | Subscription |

---

## 8. Duplicate Prevention

### 8.1 Idempotency Keys

- Each sync event has unique EventId
- API checks EventId before processing
- Duplicate EventId → return existing result
- Uses `idempotency_keys` table (existing)

### 8.2 Natural Key Checks

- Invoice: (TenantId, InvoiceNumber) — unique
- Payment: (TenantId, ReferenceNumber) — unique
- Party: (TenantId, Name) — unique (or (TenantId, Name, Type))
- Item: (TenantId, Barcode) — unique (if barcode present)

### 8.3 Duplicate Detection Flow

```
1. Sync event received
2. Check EventId in idempotency_keys
3. If found: return existing result (no new record)
4. If not found: process event
5. Store EventId in idempotency_keys
6. Return result
```

---

## 9. Sync Monitoring

### 9.1 Sync Status Table

```sql
CREATE TABLE sync_log (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL REFERENCES tenants(id),
    local_database_id UUID NOT NULL REFERENCES local_databases(id),
    device_id UUID NOT NULL REFERENCES devices(id),
    sync_type VARCHAR(20) NOT NULL, -- push, pull, full
    status VARCHAR(20) NOT NULL, -- success, partial, failed
    events_sent INT NOT NULL DEFAULT 0,
    events_received INT NOT NULL DEFAULT 0,
    conflicts INT NOT NULL DEFAULT 0,
    errors INT NOT NULL DEFAULT 0,
    started_at TIMESTAMP NOT NULL DEFAULT NOW(),
    completed_at TIMESTAMP NULL,
    error_message TEXT
);
```

### 9.2 Sync Status UI

| Field | Description |
|-------|-------------|
| Last Sync | Timestamp of last successful sync |
| Pending Events | Number of events in outbox (pending) |
| Failed Events | Number of events in outbox (failed) |
| Conflicts | Number of unresolved conflicts |
| API Status | Connected / Disconnected |
| Neon Status | Connected / Disconnected |
| Connection Status | Online / Offline |
| Retry Queue | Number of events in retry queue |

### 9.3 Sync Alerts

- Alert when sync fails 3 consecutive times
- Alert when conflicts exist
- Alert when offline for > 24 hours
- Alert when pending events > 1000

---

## 10. Transaction Boundaries

### 10.1 Local Transaction

```
1. Begin transaction
2. Apply local change
3. Create outbox event
4. Commit transaction
5. On failure: rollback (no change, no event)
```

### 10.2 Cloud Transaction

```
1. Begin transaction
2. Apply change to Neon
3. Update sync status
4. Commit transaction
5. On failure: rollback (no change)
```

### 10.3 Sync Transaction

```
1. Begin transaction
2. Process batch of events
3. For each event:
   a. Validate
   b. Apply (or conflict)
   c. Update outbox status
4. Commit transaction
5. On failure: rollback (retry later)
```

---

## 11. خلاصة

### Sync Architecture

```
Local Change → Outbox → Push → API → Neon → Pull → Local
                ↓
           Idempotency (EventId)
                ↓
           Versioning (Version column)
                ↓
           Conflict Detection (version mismatch)
                ↓
           Conflict Resolution (manual/automatic)
                ↓
           Audit Trail (all operations logged)
```

### Key Principles

1. **Outbox Pattern** — all changes queued before sync
2. **Idempotency** — no duplicate records
3. **Versioning** — track changes over time
4. **Conflict Resolution** — manual for financial, automatic for reference
5. **Soft Delete** — no hard delete for financial records
6. **Event Ordering** — by timestamp, version, event ID
7. **Duplicate Prevention** — EventId + natural key checks
8. **Transaction Boundaries** — atomic operations
9. **Sync Monitoring** — status, alerts, retry
10. **Audit Everything** — all sync operations logged

---
*Generated by OpenCode — Phase 2 Architecture Design — 2026-11-15*
