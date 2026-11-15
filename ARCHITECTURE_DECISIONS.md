# ARCHITECTURE_DECISIONS.md

> Phase 2 — Architecture Design (Design Only)
> Generated: 2026-11-15

---

## ADR-1: Tenant Isolation via TenantId Column + RLS

**Problem:** Need to isolate data between tenants in a shared database.

**Options:**
1. Separate database per tenant
2. Shared database with TenantId column + RLS
3. Shared database with TenantId column only (app-level filtering)

**Decision:** Option 2 (Shared database with TenantId column + RLS)

**Reasons:**
- Cost-effective (single Neon instance)
- Easier to manage (single schema)
- RLS provides DB-level enforcement
- App-level filtering provides additional layer

**Pros:**
- Lower cost (single DB)
- Easier migrations (single schema)
- RLS provides defense in depth

**Cons:**
- RLS overhead on queries
- Single point of failure (one DB)
- Harder to scale per-tenant

**Risks:**
- RLS misconfiguration could expose data
- Performance impact on large datasets

**Rejected Alternatives:**
- Option 1: Too expensive for small tenants
- Option 3: No DB-level enforcement

**Status:** NEEDS APPROVAL

---

## ADR-2: Local DB Isolation via LocalDatabaseId + Fingerprint

**Problem:** Need to isolate local databases on the same machine.

**Options:**
1. File-level isolation (different directories)
2. Database-level isolation (LocalDatabaseId + fingerprint)
3. Both (file + database level)

**Decision:** Option 3 (Both)

**Reasons:**
- File-level: OS permissions
- Database-level: fingerprint verification
- Defense in depth

**Pros:**
- Strong isolation
- Tamper detection
- No cross-DB access

**Cons:**
- Complex implementation
- Fingerprint computation overhead

**Risks:**
- Fingerprint algorithm may have collisions
- Performance impact on DB open

**Rejected Alternatives:**
- Option 1: Not sufficient (file can be copied)
- Option 2: Not sufficient (no OS-level protection)

**Status:** NEEDS APPROVAL

---

## ADR-3: Backup vs Replica — Separate States, Explicit Activation

**Problem:** Need to distinguish between backup copies and active replicas.

**Options:**
1. Backup = Replica (same state)
2. Backup ≠ Replica (separate states, explicit activation)
3. Backup = Replica with auto-activation

**Decision:** Option 2 (Separate states, explicit activation)

**Reasons:**
- Prevents backup from syncing automatically
- Prevents duplicate ReplicaId
- Requires admin approval for activation
- Creates new identity on activation

**Pros:**
- Strong isolation
- No accidental sync
- Audit trail for activation

**Cons:**
- More complex restore flow
- Requires admin intervention

**Risks:**
- Admin may forget to activate
- Delayed recovery

**Rejected Alternatives:**
- Option 1: Backup could sync automatically (dangerous)
- Option 3: Auto-activation could cause conflicts

**Status:** NEEDS APPROVAL

---

## ADR-4: No Master Password / Backdoor

**Problem:** Should there be a universal password for emergency access?

**Options:**
1. Master password (universal access)
2. Emergency access with audit (time-limited, reason required)
3. No emergency access

**Decision:** Option 2 (Emergency access with audit)

**Reasons:**
- No backdoor (security)
- Audit trail for all emergency access
- Time-limited (max 24 hours)
- Reason required

**Pros:**
- Secure
- Auditable
- Time-limited

**Cons:**
- Slower recovery in emergencies
- Requires admin approval

**Risks:**
- Admin may be unavailable
- Delayed recovery

**Rejected Alternatives:**
- Option 1: Security risk (backdoor)
- Option 3: No recovery path

**Status:** APPROVED

---

## ADR-5: Sync — Outbox + Idempotency + Versioning

**Problem:** How to sync data between Desktop and Cloud?

**Options:**
1. Direct sync (no queue)
2. Outbox pattern + idempotency + versioning
3. Event sourcing

**Decision:** Option 2 (Outbox + Idempotency + Versioning)

**Reasons:**
- Outbox: reliable (no data loss)
- Idempotency: no duplicates
- Versioning: conflict detection
- Simpler than event sourcing

**Pros:**
- Reliable
- No duplicates
- Conflict detection
- Audit trail

**Cons:**
- More complex than direct sync
- Requires outbox table

**Risks:**
- Outbox table growth (needs cleanup)
- Performance impact on writes

**Rejected Alternatives:**
- Option 1: Unreliable (data loss on failure)
- Option 3: Too complex for current needs

**Status:** NEEDS APPROVAL

---

## ADR-6: Conflict Resolution — Manual for Financial, LWW for Reference

**Problem:** How to resolve sync conflicts?

**Options:**
1. Last Write Wins (LWW) for all
2. Manual review for all
3. Manual for financial, LWW for reference

**Decision:** Option 3 (Manual for financial, LWW for reference)

**Reasons:**
- Financial records (invoices, payments) require human review
- Reference data (customers, products) can use LWW
- Stock levels use sum (additive)
- Settings use LWW

**Pros:**
- Safe for financial data
- Automatic for reference data
- Audit trail for all

**Cons:**
- Manual review required for financial conflicts
- More complex than LWW

**Risks:**
- Admin may not review conflicts promptly
- Data inconsistency if not resolved

**Rejected Alternatives:**
- Option 1: LWW for financial records is dangerous
- Option 2: Manual review for all is too slow

**Status:** NEEDS APPROVAL

---

## ADR-7: Offline Auth — 72h Default, Configurable

**Problem:** How long should offline access be valid?

**Options:**
1. No offline access (always online)
2. Fixed duration (e.g., 72 hours)
3. Configurable per tenant

**Decision:** Option 3 (Configurable per tenant, default 72h)

**Reasons:**
- Different tenants have different needs
- 72h is a reasonable default
- Configurable for flexibility

**Pros:**
- Flexible
- Reasonable default
- Warning before expiry

**Cons:**
- Complex configuration
- Security risk if too long

**Risks:**
- User may be offline longer than validity
- Revocation not immediate

**Rejected Alternatives:**
- Option 1: Too restrictive
- Option 2: Not flexible enough

**Status:** NEEDS APPROVAL

---

## ADR-8: Soft Delete for Financial Records

**Problem:** How to handle deletion of financial records?

**Options:**
1. Hard delete (permanent)
2. Soft delete (IsDeleted flag)
3. Archive (move to archive table)

**Decision:** Option 2 (Soft delete)

**Reasons:**
- Preserves audit trail
- Allows restore
- Sync propagates soft delete
- No data loss

**Pros:**
- Audit trail preserved
- Restore possible
- Sync-safe

**Cons:**
- Data grows over time
- Queries must filter IsDeleted

**Risks:**
- Storage growth
- Query complexity

**Rejected Alternatives:**
- Option 1: Data loss, audit trail broken
- Option 3: Too complex for current needs

**Status:** NEEDS APPROVAL

---

## ADR-9: Additive Migrations Only

**Problem:** How to migrate the database?

**Options:**
1. Destructive migration (drop/recreate)
2. Additive migration (add columns/tables only)
3. Blue-green migration (parallel databases)

**Decision:** Option 2 (Additive only)

**Reasons:**
- No data loss
- Rollback possible
- Backward compatible
- Safer for production

**Pros:**
- No data loss
- Rollback possible
- Safer

**Cons:**
- Schema may have unused columns
- More complex over time

**Risks:**
- Schema bloat
- Migration complexity

**Rejected Alternatives:**
- Option 1: Data loss risk
- Option 3: Too complex

**Status:** APPROVED

---

## ADR-10: No Production Migration Without Approval

**Problem:** Should migrations run automatically on production?

**Options:**
1. Auto-migrate on startup
2. Manual migration with approval
3. Migration tool with approval

**Decision:** Option 2 (Manual migration with approval)

**Reasons:**
- Safety first
- Human review required
- No unexpected changes

**Pros:**
- Safe
- Controlled
- Auditable

**Cons:**
- Slower deployment
- Requires manual step

**Risks:**
- Forgotten migration
- Delayed deployment

**Rejected Alternatives:**
- Option 1: Dangerous (auto-migrate could break production)
- Option 3: Overkill for current needs

**Status:** APPROVED

---

## ADR-11: Local Database Technology

**Problem:** What database technology for local storage?

**Options:**
1. PostgreSQL (same as cloud)
2. SQLite (lighter, faster for single-user)
3. SQL Server LocalDB

**Decision:** Option 2 (SQLite with SQLCipher for encryption)

**Reasons:**
- Lighter (no server process)
- Faster for single-user
- SQLCipher provides transparent encryption
- Consistent API (Npgsql → Microsoft.Data.Sqlite)

**Pros:**
- Lightweight
- Fast
- Encrypted
- No server needed

**Cons:**
- Different SQL dialect
- Migration from PostgreSQL
- Less features than PostgreSQL

**Risks:**
- SQL dialect differences
- Migration complexity

**Rejected Alternatives:**
- Option 1: Heavy for single-user
- Option 3: Windows-only

**Status:** NEEDS APPROVAL

---

## ADR-12: Tenant Selection on Login

**Problem:** How does a user select a tenant on login?

**Options:**
1. Single tenant per user (no selection)
2. Tenant selection dropdown on login
3. Tenant selection after login

**Decision:** Option 2 (Tenant selection dropdown on login)

**Reasons:**
- User may belong to multiple tenants
- Clear tenant context from start
- Prevents accidental cross-tenant access

**Pros:**
- Clear tenant context
- Prevents mistakes
- User-friendly

**Cons:**
- Extra step on login
- UI complexity

**Risks:**
- User may select wrong tenant

**Rejected Alternatives:**
- Option 1: Not flexible enough
- Option 3: Confusing UX

**Status:** NEEDS APPROVAL

---

## Summary of Decisions

| ADR | Decision | Status |
|-----|----------|--------|
| ADR-1 | TenantId + RLS | NEEDS APPROVAL |
| ADR-2 | LocalDatabaseId + Fingerprint | NEEDS APPROVAL |
| ADR-3 | Backup ≠ Replica, explicit activation | NEEDS APPROVAL |
| ADR-4 | No master password, emergency access with audit | APPROVED |
| ADR-5 | Outbox + Idempotency + Versioning | NEEDS APPROVAL |
| ADR-6 | Manual for financial, LWW for reference | NEEDS APPROVAL |
| ADR-7 | Offline auth: 72h default, configurable | NEEDS APPROVAL |
| ADR-8 | Soft delete for financial records | NEEDS APPROVAL |
| ADR-9 | Additive migrations only | APPROVED |
| ADR-10 | No production migration without approval | APPROVED |
| ADR-11 | SQLite + SQLCipher for local DB | NEEDS APPROVAL |
| ADR-12 | Tenant selection on login | NEEDS APPROVAL |

---
*Generated by OpenCode — Phase 2 Architecture Design — 2026-11-15*
