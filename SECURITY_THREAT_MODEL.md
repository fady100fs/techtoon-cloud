# SECURITY_THREAT_MODEL.md

> Phase 2 — Architecture Design (Design Only)
> Generated: 2026-11-15

---

## 1. Threat Categories

### 1.1 Tenant Isolation Threats

| # | Threat | Likelihood | Impact | Severity | Mitigation |
|---|--------|-----------|--------|----------|------------|
| T1 | Cross-tenant data access via API | Medium | Critical | **Critical** | Tenant validation middleware + RLS |
| T2 | Cross-tenant data access via DB | Low | Critical | **Critical** | RLS policies + app-level filtering |
| T3 | Tenant ID spoofing (client sends wrong TenantId) | Medium | Critical | **Critical** | Server-side tenant resolution from user membership |
| T4 | Cross-tenant data leak via reports | Medium | High | **High** | Tenant filtering on all report queries |
| T5 | Cross-tenant data leak via exports | Medium | High | **High** | Tenant filtering on all export queries |

### 1.2 Local Database Isolation Threats

| # | Threat | Likelihood | Impact | Severity | Mitigation |
|---|--------|-----------|--------|----------|------------|
| T6 | DB file copy to another machine | Medium | Critical | **Critical** | DatabaseFingerprint + InstallationId check |
| T7 | DB file rename | Low | High | **High** | DatabaseFingerprint mismatch → reject |
| T8 | Manual TenantId/LocalDatabaseId modification | Medium | Critical | **Critical** | DatabaseFingerprint mismatch → reject |
| T9 | DB file replacement | Medium | Critical | **Critical** | DatabaseFingerprint mismatch → reject |
| T10 | Session reuse across DBs | Low | High | **High** | Session tied to LocalDatabaseId |
| T11 | Token replay from another DB | Medium | High | **High** | Token tied to LocalDatabaseId + DeviceId |

### 1.3 Authentication Threats

| # | Threat | Likelihood | Impact | Severity | Mitigation |
|---|--------|-----------|--------|----------|------------|
| T12 | Brute force login | Medium | High | **High** | Rate limiting + account lockout |
| T13 | Credential stuffing | Medium | High | **High** | Rate limiting + CAPTCHA (future) |
| T14 | Session hijacking | Low | Critical | **Critical** | JWT + refresh token rotation + device fingerprint |
| T15 | Token theft | Medium | High | **High** | Refresh token rotation (single-use) + theft detection |
| T16 | Offline data theft | Low | Medium | **Medium** | Local DB encryption + device fingerprint |

### 1.4 Sync Threats

| # | Threat | Likelihood | Impact | Severity | Mitigation |
|---|--------|-----------|--------|----------|------------|
| T17 | Duplicate sync events | Medium | Medium | **Medium** | Idempotency keys |
| T18 | Stale data from restored backup | Medium | High | **High** | Conflict resolution + versioning |
| T19 | Sync loop (infinite retry) | Low | Medium | **Medium** | Retry limit + exponential backoff |
| T20 | Man-in-the-middle (sync) | Low | High | **High** | HTTPS/TLS + certificate pinning (future) |
| T21 | Unauthorized sync | Medium | Critical | **Critical** | Device authorization + tenant validation |

### 1.5 Device Threats

| # | Threat | Likelihood | Impact | Severity | Mitigation |
|---|--------|-----------|--------|----------|------------|
| T22 | Device spoofing | Medium | High | **High** | Device fingerprint verification |
| T23 | QR code interception | Low | Medium | **Medium** | Single-use, short-lived token |
| T24 | Device theft | Medium | High | **High** | Remote revocation + session invalidation |
| T25 | Unauthorized pairing | Low | Medium | **Medium** | Admin approval required |

### 1.6 Backup Threats

| # | Threat | Likelihood | Impact | Severity | Mitigation |
|---|--------|-----------|--------|----------|------------|
| T26 | Backup theft | Low | High | **High** | Encryption + access control |
| T27 | Backup tampering | Low | High | **High** | Fingerprint verification |
| T28 | Restore of old backup (data regression) | Medium | High | **High** | Conflict resolution + versioning |
| T29 | Duplicate ReplicaId | Low | Medium | **Medium** | Unique constraint + cloud validation |

### 1.7 Offline Threats

| # | Threat | Likelihood | Impact | Severity | Mitigation |
|---|--------|-----------|--------|----------|------------|
| T30 | User revoked during offline | Medium | Medium | **Medium** | Re-verification on reconnect |
| T31 | Device revoked during offline | Low | Medium | **Medium** | Re-verification on reconnect |
| T32 | Permission changed during offline | Medium | Medium | **Medium** | Re-verification on reconnect |
| T33 | Offline data theft | Low | Medium | **Medium** | Local DB encryption |

---

## 2. Threat Severity Matrix

| Severity | Count | Description |
|----------|-------|-------------|
| Critical | 5 | Cross-tenant access, cross-DB access, TenantId spoofing, session hijacking, unauthorized sync |
| High | 12 | Data leak via reports/exports, DB file copy/rename/modify/replace, token replay, brute force, credential stuffing, backup theft/tampering, data regression, device spoofing/theft |
| Medium | 10 | Sync duplicate/loop/MITM, QR interception, unauthorized pairing, offline data theft, user/device/permission revocation during offline |
| Low | 3 | — |

---

## 3. Security Controls

### 3.1 Implemented Controls

| Control | Status | Layer |
|---------|--------|-------|
| bcrypt password hashing | ✅ | Auth |
| JWT + refresh tokens | ✅ | Auth |
| Permission-based authorization | ✅ | AuthZ |
| CORS (specific origins) | ✅ | API |
| Input validation | ✅ | API |
| Error handling | ✅ | API |
| Audit logging | ✅ | App |
| Rate limiting | ✅ | API |
| DPAPI (connection string) | ✅ | Desktop |
| Parameterized SQL | ✅ | DB |

### 3.2 Planned Controls

| Control | Status | Layer | Phase |
|---------|--------|-------|-------|
| Tenant validation middleware | ❌ | API | Phase 4 |
| RLS policies | ❌ | DB | Phase 4 |
| Local DB fingerprint | ❌ | DB | Phase 5 |
| Session isolation | ❌ | App | Phase 5 |
| Device fingerprint | ❌ | Device | Phase 8 |
| Device registration/revocation | ❌ | Device | Phase 8 |
| Backup encryption | ❌ | Backup | Phase 9 |
| Offline authorization | ❌ | App | Phase 11 |
| Sync engine | ❌ | App/API | Phase 10 |
| Conflict resolution | ❌ | App/API | Phase 10 |

---

## 4. Defense in Depth

### 4.1 Layer 1: API
- Tenant validation middleware
- Permission handler
- JWT authentication
- Rate limiting
- CORS
- Input validation
- Error handling

### 4.2 Layer 2: Database
- RLS policies (tenant isolation)
- FK constraints (referential integrity)
- Unique constraints (duplicate prevention)
- Indexes (performance)
- Encryption at rest (future)

### 4.3 Layer 3: Application
- Service-layer tenant filtering
- Permission checks (defense in depth)
- Input sanitization
- Audit logging

### 4.4 Layer 4: UI
- Page-level permission gating
- Action-level permission gating
- Data filtering (show only authorized data)

### 4.5 Layer 5: Device
- Device fingerprint
- Session isolation
- Local DB encryption
- Secure storage (OS keychain)

---

## 5. Exemption C1 (Out of Scope)

- Exposed Neon password in 13 files: **NOT addressed**
- No password rotation
- No connection string changes
- No modification to any secret or credential

This exemption is explicitly excluded from this task per agreement.

---

## 6. خلاصة

### Threat Summary

| Severity | Count | Top Threats |
|----------|-------|-------------|
| Critical | 5 | Cross-tenant access, cross-DB access, TenantId spoofing, session hijacking, unauthorized sync |
| High | 12 | Data leak, DB file tampering, token replay, brute force, backup theft |
| Medium | 10 | Sync issues, QR interception, device theft, offline threats |
| Low | 3 | — |

### Security Controls

- **Implemented**: 10 controls (bcrypt, JWT, permissions, CORS, rate limiting, input validation, audit, DPAPI, parameterized SQL, error handling)
- **Planned**: 10 controls (tenant validation, RLS, fingerprint, session isolation, device registration, backup encryption, offline auth, sync engine, conflict resolution)
- **Out of scope**: 1 (Exemption C1 — exposed Neon password)

---
*Generated by OpenCode — Phase 2 Architecture Design — 2026-11-15*
