# MULTI_TENANT_TEST_STRATEGY.md

> Phase 2 — Architecture Design (Design Only)
> Generated: 2026-11-15

---

## 1. Test Strategy Overview

### 1.1 Test Levels

| Level | Description | Scope |
|-------|-------------|-------|
| Unit | Isolated function tests | BalanceCalculator, LoanScheduleCalculator, etc. |
| Integration | Multi-component tests | DB + Service + API |
| E2E | Full workflow tests | Desktop → API → DB |
| Security | Permission/isolation tests | Tenant, Local DB, Device |
| Performance | Load/stress tests | Large datasets, concurrent users |

### 1.2 Test Environments

| Environment | Database | Purpose |
|-------------|----------|---------|
| Unit | In-memory / mock | Fast, isolated |
| Integration | `techtoon_test_ci` (isolated copy) | Real DB, isolated |
| E2E | `techtoon_test` (working) | Full workflow |
| Security | `techtoon_test_ci` (isolated copy) | Isolation verification |
| Performance | `techtoon_test_ci` (isolated copy) | Load testing |

---

## 2. Test Matrix

### 2.1 Tenant Isolation Tests

| # | Test | Expected | Priority |
|---|------|----------|----------|
| TI-1 | User A cannot read Tenant B data | Reject | Critical |
| TI-2 | User A cannot write Tenant B data | Reject | Critical |
| TI-3 | User A cannot delete Tenant B data | Reject | Critical |
| TI-4 | User A cannot access API with TenantId B | Reject (403) | Critical |
| TI-5 | User A cannot access Tenant B attachments | Reject | High |
| TI-6 | User A cannot view Tenant B reports | Reject | High |
| TI-7 | User A cannot access Tenant B sync logs | Reject | High |
| TI-8 | Admin A cannot manage Tenant B | Reject | Critical |
| TI-9 | RLS policy filters correctly | Only tenant's data | Critical |
| TI-10 | App-level filter works | Only tenant's data | Critical |

### 2.2 Local Database Isolation Tests

| # | Test | Expected | Priority |
|---|------|----------|----------|
| LDI-1 | User in DB A cannot login to DB B | Reject | Critical |
| LDI-2 | User in DB A cannot login to DB C | Reject | Critical |
| LDI-3 | Cloud Admin cannot login to local DB | Reject | Critical |
| LDI-4 | Tenant Owner cannot login to other DB | Reject | Critical |
| LDI-5 | Local Admin cannot login to other DB | Reject | Critical |
| LDI-6 | Rename DB file → fingerprint mismatch | Reject | High |
| LDI-7 | Copy DB to another machine → InstallationId mismatch | Reject | High |
| LDI-8 | Modify TenantId manually → fingerprint mismatch | Reject | Critical |
| LDI-9 | Modify LocalDatabaseId manually → fingerprint mismatch | Reject | Critical |
| LDI-10 | Replace DB with another → fingerprint mismatch | Reject | Critical |
| LDI-11 | Reuse old session → reject | Reject | High |
| LDI-12 | Reuse token from another DB → reject | Reject | High |
| LDI-13 | Direct API access → validate LocalDatabaseId + TenantId + DeviceId | Reject | Critical |
| LDI-14 | Same user in multiple DBs → independent permissions | Verify | High |

### 2.3 User Permission Tests

| # | Test | Expected | Priority |
|---|------|----------|----------|
| UP-1 | User without permission cannot view page | Button hidden | Medium |
| UP-2 | User without permission cannot edit | Button disabled | Medium |
| UP-3 | User without permission cannot delete | Button disabled | Medium |
| UP-4 | User without permission cannot create | Button disabled | Medium |
| UP-5 | Per-user override (grant) works | Permission granted | Medium |
| UP-6 | Per-user override (deny) works | Permission denied | Medium |
| UP-7 | Role change updates permissions | Permissions updated | Medium |
| UP-8 | Permission inheritance (role → user) | Correct resolution | Medium |

### 2.4 Session Isolation Tests

| # | Test | Expected | Priority |
|---|------|----------|----------|
| SI-1 | One session per local DB | New session on DB switch | High |
| SI-2 | No session reuse across DBs | Reject old session | High |
| SI-3 | Session tied to LocalDatabaseId | Token includes LocalDatabaseId | High |
| SI-4 | Session invalidated on DB switch | Old session closed | High |
| SI-5 | Session invalidated on user revoke | All user sessions closed | High |
| SI-6 | Session invalidated on device revoke | All device sessions closed | High |
| SI-7 | Session expiry | Auto-invalidate after expiry | Medium |
| SI-8 | Password change invalidates sessions | All user sessions closed | Medium |

### 2.5 Device Authentication Tests

| # | Test | Expected | Priority |
|---|------|----------|----------|
| DA-1 | Device registration | Device created (pending) | High |
| DA-2 | Device approval | Device activated | High |
| DA-3 | Device suspension | Device suspended, no login | High |
| DA-4 | Device revocation | Device revoked, no login/sync | High |
| DA-5 | Device re-activation | Device re-activated | Medium |
| DA-6 | Device fingerprint verification | Mismatch → reject | High |
| DA-7 | QR pairing | Token single-use, short-lived | High |
| DA-8 | Activation link | Code single-use, 24h expiry | Medium |
| DA-9 | Device-tenant link | Link created, permissions set | Medium |

### 2.6 Offline Authorization Tests

| # | Test | Expected | Priority |
|---|------|----------|----------|
| OA-1 | Normal offline (within validity) | Transactions queued, sync on reconnect | High |
| OA-2 | Offline expiry | Session locked, no new transactions | High |
| OA-3 | User revoked during offline | Session locked on reconnect | High |
| OA-4 | Device revoked during offline | Session locked on reconnect | High |
| OA-5 | Permission changed during offline | Old perms used, updated on reconnect | Medium |
| OA-6 | Tenant suspended during offline | Session locked on reconnect | High |
| OA-7 | Max offline transactions exceeded | No new transactions, warning shown | Medium |
| OA-8 | Re-verification on reconnect | Success if user active | High |

### 2.7 Backup Restore Tests

| # | Test | Expected | Priority |
|---|------|----------|----------|
| BR-1 | Backup creation | Success, metadata created | High |
| BR-2 | Backup verification | Success, fingerprint matches | High |
| BR-3 | Restore to new DB | Success, data intact | High |
| BR-4 | Data integrity after restore | Counts/relationships/balances match | Critical |
| BR-5 | Activation (new identity) | New LocalDatabaseId, ReplicaId, InstallationId | High |
| BR-6 | Sync after activation | Success, no duplicates | High |
| BR-7 | Old changes conflict | Conflict detected, manual review | High |
| BR-8 | Duplicate ReplicaId | Rejected | High |
| BR-9 | Backup ≠ Device (no sync, no login) | Verified | High |
| BR-10 | Restore ≠ Activate (explicit activation) | Verified | High |

### 2.8 Sync Idempotency Tests

| # | Test | Expected | Priority |
|---|------|----------|----------|
| SI-1 | Send same event twice | No duplicate record | Critical |
| SI-2 | Retry failed event | No duplicate record | Critical |
| SI-3 | Re-play event | No duplicate record | Critical |
| SI-4 | Duplicate ReplicaId sync | Rejected | High |
| SI-5 | Event ordering | Events processed in order | High |

### 2.9 Sync Conflict Tests

| # | Test | Expected | Priority |
|---|------|----------|----------|
| SC-1 | Concurrent edit (invoice) | Conflict detected, manual review | Critical |
| SC-2 | Concurrent edit (payment) | Conflict detected, manual review | Critical |
| SC-3 | Concurrent delete + edit | Conflict detected, manual review | High |
| SC-4 | Version mismatch | Conflict detected | High |
| SC-5 | Schema mismatch | Sync blocked, update required | Medium |
| SC-6 | Stock level (sum) | Additive aggregation | Medium |
| SC-7 | Reference data (LWW) | Last write wins (with audit) | Medium |

### 2.10 Accounting Integrity Tests

| # | Test | Expected | Priority |
|---|------|----------|----------|
| AI-1 | Invoice creates all related records | All records created | Critical |
| AI-2 | Payment updates party balance | Balance updated | Critical |
| AI-3 | Return reverses invoice | All effects reversed | Critical |
| AI-4 | Transfer updates source and destination | Both updated, no double-count | Critical |
| AI-5 | Journal entry balanced (debits = credits) | Balanced | Critical |
| AI-6 | Stock level = sum of movements | Match | Critical |
| AI-7 | Party balance = sum of invoices + payments | Match | Critical |
| AI-8 | Cash box balance = sum of transactions | Match | Critical |
| AI-9 | Invoice due = total - paid (never negative) | Correct | Critical |
| AI-10 | COGS calculation correct | Match | High |
| AI-11 | Tax calculation correct | Match | High |
| AI-12 | Multi-tenant: no cross-tenant data | Isolated | Critical |
| AI-13 | Sync: no duplicate records | No duplicates | Critical |
| AI-14 | Sync: no balance discrepancies | Match | Critical |
| AI-15 | Sync: no stock discrepancies | Match | Critical |

### 2.11 Data Migration Tests

| # | Test | Expected | Priority |
|---|------|----------|----------|
| DM-1 | Migration success (all scripts) | Success, no errors | Critical |
| DM-2 | Data integrity (counts/relationships/balances) | All match | Critical |
| DM-3 | Tenant filtering | Only tenant's data | Critical |
| DM-4 | RLS policy | No data without tenant | Critical |
| DM-5 | Rollback | Success, data restored | High |
| DM-6 | Performance | No degradation | Medium |

### 2.12 Recovery Tests

| # | Test | Expected | Priority |
|---|------|----------|----------|
| R-1 | Restore old backup | Conflict detected | High |
| R-2 | No data regression | Cloud changes preserved | Critical |
| R-3 | No duplicate records | No duplicates | Critical |
| R-4 | No lost modifications | Recent changes preserved | Critical |

---

## 3. Test Execution Order

1. **Unit tests** (no DB) — fast, isolated
2. **Integration tests** (DB) — slower, requires isolated DB
3. **Security tests** (isolation) — requires multi-tenant setup
4. **Sync tests** — requires multi-device setup
5. **Offline tests** — requires network simulation
6. **Performance tests** — requires large dataset
7. **E2E tests** — requires full stack

---

## 4. Test Environments

| Environment | Database | Purpose |
|-------------|----------|---------|
| Unit | In-memory / mock | Fast, isolated |
| Integration | `techtoon_test_ci` (isolated copy) | Real DB, isolated |
| Security | `techtoon_test_ci` (isolated copy) | Isolation verification |
| Sync | `techtoon_test_ci` (isolated copy) + mock cloud | Sync verification |
| Offline | `techtoon_test_ci` (isolated copy) | Offline verification |
| Performance | `techtoon_test_ci` (isolated copy) | Load testing |
| E2E | `techtoon_test` (working) | Full workflow |

---

## 5. Test Safety Rules

1. Never test on production DB (`neondb`)
2. Always use `techtoon_test` or `techtoon_test_ci`
3. Always clean up test data
4. Never use TRUNCATE or blanket DELETE
5. Never claim PASS without evidence
6. Always document test results
7. Use PASS/FAIL/NOT VERIFIED/BLOCKED

---

## 6. Test Coverage Target

| Category | Target | Current |
|----------|--------|---------|
| Unit tests | 90%+ | ~80% |
| Integration tests | 80%+ | ~70% |
| Security tests | 100% | 0% (new) |
| Sync tests | 100% | 0% (new) |
| Offline tests | 100% | 0% (new) |
| E2E tests | 70%+ | ~50% |
| **Overall** | **85%+** | **~60%** |

---

## 7. خلاصة

### Test Strategy Summary

- **12 test categories** with 100+ test cases
- **7 test environments** (unit, integration, security, sync, offline, performance, E2E)
- **Safety rules**: never production DB, always clean up, never TRUNCATE
- **Verification**: PASS/FAIL/NOT VERIFIED/BLOCKED
- **Coverage target**: 85%+ (current ~60%)

### Key Principles

1. **Evidence-based** — no PASS without evidence
2. **Isolated** — test on isolated DB, never production
3. **Clean** — always clean up test data
4. **Comprehensive** — test all paths (success, failure, edge cases)
5. **Security-focused** — test isolation, permissions, auth

---
*Generated by OpenCode — Phase 2 Architecture Design — 2026-11-15*
