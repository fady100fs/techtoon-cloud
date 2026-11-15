# PHASE_2_REVIEW_CHECKLIST.md

> Phase 2 — Architecture Design (Design Only)
> Generated: 2026-11-15

---

## 1. Architecture Consistency Check

### 1.1 Tenant vs Local Database

- [ ] TenantId and LocalDatabaseId are distinct concepts
- [ ] TenantId identifies the company
- [ ] LocalDatabaseId identifies the local DB
- [ ] No confusion between the two
- [ ] All data tables have TenantId
- [ ] Local tables have LocalDatabaseId
- [ ] No cross-tenant data access
- [ ] No cross-DB data access

### 1.2 Local User vs Cloud User

- [ ] Local user is independent from cloud user
- [ ] Local user has local permissions only
- [ ] Cloud user has tenant permissions only
- [ ] No automatic permission inheritance
- [ ] Same email in multiple DBs = different users
- [ ] Local user can exist without cloud user

### 1.3 Admin vs Owner

- [ ] System Owner ≠ Tenant Owner ≠ Local DB Admin
- [ ] No automatic cross-tenant access
- [ ] No automatic cross-DB access
- [ ] Emergency access requires audit
- [ ] No master password/backdoor

### 1.4 Backup vs Active Replica

- [ ] Backup ≠ Device (no sync, no login)
- [ ] Backup ≠ Active Replica (separate states)
- [ ] Restore ≠ Activate (explicit activation)
- [ ] Activation creates new identity
- [ ] No duplicate ReplicaId
- [ ] No auto-sync after restore

### 1.5 Offline Authorization

- [ ] Offline duration is configurable
- [ ] Warning before expiry
- [ ] Re-verification on reconnect
- [ ] User revocation handled
- [ ] Device revocation handled
- [ ] Permission changes handled
- [ ] Pending transactions preserved

### 1.6 Sync Engine

- [ ] Outbox pattern implemented
- [ ] Idempotency enforced
- [ ] Versioning tracked
- [ ] Conflict detection
- [ ] Conflict resolution (manual for financial)
- [ ] Soft delete for financial records
- [ ] Event ordering
- [ ] Duplicate prevention
- [ ] Transaction boundaries
- [ ] Sync monitoring

### 1.7 Security

- [ ] Tenant validation middleware
- [ ] RLS policies
- [ ] Session isolation
- [ ] Token management
- [ ] Device revocation
- [ ] Encryption
- [ ] Secret management
- [ ] Audit trail
- [ ] Backup protection

### 1.8 Data Safety

- [ ] No data deletion (additive only)
- [ ] No production migration without approval
- [ ] No user/password changes
- [ ] No data loss
- [ ] Rollback plan documented
- [ ] Backup before changes

---

## 2. Design Consistency Check

### 2.1 No Conflicts Between TenantId and LocalDatabaseId

- [ ] TenantId identifies the company
- [ ] LocalDatabaseId identifies the local DB
- [ ] No confusion between the two
- [ ] All data tables have TenantId
- [ ] Local tables have LocalDatabaseId
- [ ] Composite keys where needed

### 2.2 No Conflicts Between Local User and Cloud User

- [ ] Local user is independent from cloud user
- [ ] Local user has local permissions only
- [ ] Cloud user has tenant permissions only
- [ ] No automatic permission inheritance
- [ ] Same email in multiple DBs = different users

### 2.3 No Admin Overreach

- [ ] No automatic cross-tenant access
- [ ] No automatic cross-DB access
- [ ] Emergency access requires audit
- [ ] No master password/backdoor
- [ ] All access through proper auth

### 2.4 No Backup as Active Replica

- [ ] Backup ≠ Device (no sync, no login)
- [ ] Backup ≠ Active Replica (separate states)
- [ ] Restore ≠ Activate (explicit activation)
- [ ] Activation creates new identity
- [ ] No duplicate ReplicaId
- [ ] No auto-sync after restore

### 2.5 No Automatic Sync for Restored DB

- [ ] Sync disabled until activation
- [ ] Activation required for sync
- [ ] Conflict resolution for old changes
- [ ] Audit trail for activation

### 2.6 Clear Offline Policy

- [ ] Offline duration configurable
- [ ] Warning before expiry
- [ ] Re-verification on reconnect
- [ ] User revocation handled
- [ ] Device revocation handled
- [ ] Permission changes handled
- [ ] Pending transactions preserved

### 2.7 No Duplicate Events

- [ ] Idempotency keys enforced
- [ ] Natural key checks
- [ ] Duplicate detection flow
- [ ] No duplicate records

### 2.8 Safe Conflict Handling

- [ ] Manual review for financial records
- [ ] LWW for reference data
- [ ] Sum for stock levels
- [ ] Conflict resolution UI
- [ ] Audit trail for decisions

### 2.9 Data Preservation

- [ ] No data deletion (additive only)
- [ ] No production migration without approval
- [ ] No user/password changes
- [ ] No data loss
- [ ] Rollback plan documented
- [ ] Backup before changes

### 2.10 No Component Duplication

- [ ] No duplicate tables
- [ ] No duplicate services
- [ ] No duplicate pages
- [ ] No duplicate tests

---

## 3. Review Questions

### 3.1 Architecture Questions

1. Is the tenant isolation model clear and complete?
2. Is the local DB isolation model sound?
3. Is the backup/replica separation correct?
4. Is the offline authorization policy reasonable?
5. Is the sync engine design sound?
6. Is the security architecture comprehensive?
7. Are the ADRs well-documented?
8. Are there any conflicts or contradictions?

### 3.2 Implementation Questions

1. Can Phase 3 (Database Design) proceed?
2. Are the migration steps clear?
3. Is the rollback plan adequate?
4. Are the test plans sufficient?
5. Are there any blocking issues?

### 3.3 Approval Questions

1. Approve ADR-1 (TenantId + RLS)?
2. Approve ADR-2 (LocalDatabaseId + Fingerprint)?
3. Approve ADR-3 (Backup ≠ Replica)?
4. Approve ADR-5 (Outbox + Idempotency + Versioning)?
5. Approve ADR-6 (Manual for financial, LWW for reference)?
6. Approve ADR-7 (Offline auth: 72h default)?
7. Approve ADR-8 (Soft delete for financial)?
8. Approve ADR-11 (SQLite + SQLCipher for local DB)?
9. Approve ADR-12 (Tenant selection on login)?

---

## 4. Review Result

### PASS Criteria

- All architecture documents created
- No conflicts or contradictions
- All ADRs documented
- All risks identified
- All approval points identified

### FAIL Criteria

- Missing architecture documents
- Conflicts or contradictions found
- Missing ADRs
- Unidentified risks
- Missing approval points

### NOT VERIFIED Criteria

- Design not yet reviewed
- Design not yet approved
- Implementation not yet started

---

## 5. Next Steps

1. **Review** all 11 architecture documents
2. **Approve** ADRs (mark as APPROVED)
3. **Request changes** if needed
4. **Approve Phase 3** (Database Design) when ready
5. **Do not start Phase 3** until explicit approval

---

## 6. Approval Sign-off

| Role | Name | Date | Signature |
|------|------|------|-----------|
| System Owner | | | |
| Tenant Owner | | | |
| Technical Lead | | | |
| Security Reviewer | | | |

---
*Generated by OpenCode — Phase 2 Architecture Design — 2026-11-15*
*Status: Review Pending · Awaiting Approval*
