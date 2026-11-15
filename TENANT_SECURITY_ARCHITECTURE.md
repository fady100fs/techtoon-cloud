# TENANT_SECURITY_ARCHITECTURE.md

> Phase 2 — Architecture Design (Design Only)
> Generated: 2026-11-15

---

## 1. Threat Model

### 1.1 Threats

| # | Threat | Impact | Likelihood | Mitigation |
|---|--------|--------|------------|------------|
| 1 | Cross-tenant data access | Critical | Medium | TenantId filter + RLS + API validation |
| 2 | Cross-DB data access | Critical | Medium | LocalDatabaseId filter + session isolation |
| 3 | Session hijacking | High | Low | JWT + device fingerprint + token rotation |
| 4 | Credential stuffing | High | Medium | Rate limiting + account lockout |
| 5 | Brute force | High | Medium | Rate limiting + CAPTCHA (future) |
| 6 | Data tampering | High | Low | Audit trail + integrity checks |
| 7 | Backup theft | High | Low | Encryption + access control |
| 8 | API abuse | Medium | Medium | Rate limiting + CORS + input validation |
| 9 | Secret leakage | Critical | Low | No secrets in client, env vars for server |
| 10 | Offline data theft | Medium | Low | Local DB encryption + device fingerprint |
| 11 | Privilege escalation | Critical | Low | Permission checks at all layers |
| 12 | SQL injection | Critical | Low | Parameterized queries + input validation |
| 13 | XSS/CSRF | Medium | Low | CORS + CSP + input sanitization |
| 14 | Man-in-the-middle | High | Low | HTTPS/TLS + certificate pinning (future) |

---

## 2. Defense in Depth

### 2.1 Layer 1: API Level

| Control | Description | Status |
|---------|-------------|--------|
| Tenant Validation Middleware | Verify TenantId on every request | ❌ To implement |
| Permission Handler | Check permissions per endpoint | ✅ Implemented |
| JWT Authentication | Verify token signature + expiry | ✅ Implemented |
| Rate Limiting | Limit requests per IP/user | ✅ Configured |
| CORS | Specific origins only | ✅ Configured |
| Input Validation | Validate all inputs | ✅ Implemented |
| Error Handling | No sensitive info in errors | ✅ Implemented |

### 2.2 Layer 2: Database Level

| Control | Description | Status |
|---------|-------------|--------|
| Row Level Security (RLS) | Filter rows by TenantId | ❌ To implement |
| Foreign Key Constraints | Enforce referential integrity | ⚠️ Partial (5 FKs) |
| Unique Constraints | Prevent duplicates | ✅ Implemented |
| Indexes | Performance + uniqueness | ✅ Implemented |
| Encryption at rest | Encrypt DB files | ❌ To implement |

### 2.3 Layer 3: Application Level

| Control | Description | Status |
|---------|-------------|--------|
| Service-layer tenant filtering | All queries filtered by TenantId | ❌ To implement |
| Permission checks | HasPermission on all actions | ✅ Implemented |
| Input sanitization | Sanitize all user inputs | ✅ Implemented |
| Audit logging | Log all operations | ✅ Implemented |

### 2.4 Layer 4: UI Level

| Control | Description | Status |
|---------|-------------|--------|
| Page-level gating | Show/hide pages by permission | ✅ Implemented |
| Action-level gating | Enable/disable buttons by permission | ✅ Implemented |
| Data filtering | Show only authorized data | ⚠️ Partial |

### 2.5 Layer 5: Device Level

| Control | Description | Status |
|---------|-------------|--------|
| Device fingerprint | Verify device identity | ❌ To implement |
| Session isolation | One session per local DB | ❌ To implement |
| Local DB encryption | Encrypt local DB files | ❌ To implement |
| Secure storage | Store secrets in OS keychain | ⚠️ Partial (DPAPI for connection string) |

---

## 3. Tenant Isolation (Multi-Layer)

### 3.1 API Level

```
Middleware: TenantValidationMiddleware
1. Extract UserId from JWT
2. Extract TenantId from request (header or body)
3. Verify user is member of tenant (tenant_memberships)
4. Verify user has required role
5. Set TenantId in HttpContext
6. All subsequent queries filtered by TenantId
```

### 3.2 Service Level

```
All service methods:
1. Get TenantId from HttpContext (or session)
2. Filter all queries by TenantId
3. Verify entity belongs to tenant before update/delete
4. Log tenant context in audit trail
```

### 3.3 Database Level

```sql
-- Row Level Security (PostgreSQL)
ALTER TABLE invoices ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation ON invoices
    USING (tenant_id = current_setting('app.current_tenant_id')::uuid);
```

### 3.4 UI Level

```
1. User selects tenant on login
2. All data filtered by selected tenant
3. No cross-tenant data visible
4. Tenant context shown in UI (company name in title bar)
```

---

## 4. Local Database Isolation

### 4.1 Verification Flow

```
1. Read LocalDatabaseId from DB file
2. Read TenantId from DB file
3. Compute DatabaseFingerprint (SHA-256)
4. Compare with stored fingerprint → reject if mismatch
5. Verify DatabaseStatus == Active
6. Verify InstallationId matches
7. Read LocalUser (from local_users table)
8. Verify password hash
9. Verify LocalMembership (user has role in this DB)
10. Verify Device (device is registered and active)
11. Verify DeviceTenantLink (device authorized for this tenant)
12. Create new Session (isolated)
13. Load permissions (tenant + local)
```

### 4.2 Prevention of Isolation Bypass

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

---

## 5. Secret Management

### 5.1 Secret Storage

| Secret | Storage | Access |
|--------|---------|--------|
| Neon connection string | Environment variable (server) | API only |
| JWT secret | Environment variable (server) | API only |
| Fly API key | Environment variable (CI/CD) | Deployment only |
| Desktop DB password | DPAPI-encrypted file | Desktop only |
| Mobile tokens | Secure storage (OS keychain) | Mobile only |
| Encryption keys | OS keychain | Desktop/Mobile only |

### 5.2 Secret Rules

1. No secrets in source code
2. No secrets in client apps
3. No secrets in logs
4. Environment variables for server secrets
5. DPAPI/Keychain for client secrets
6. Secret rotation supported (with audit)

### 5.3 Exemption C1 (Out of Scope)

- Exposed Neon password in 13 files: **NOT addressed** (per agreement)
- No password rotation
- No connection string changes

---

## 6. Audit Trail Security

### 6.1 Audit Trail Protection

- No user can edit or delete audit records
- Audit records are append-only
- Audit records include tenant context
- Audit records are synced to cloud
- Audit records are retained per retention policy

### 6.2 Audit Trail Structure

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
    source VARCHAR(20) NOT NULL, -- Desktop, Mobile, API
    created_at TIMESTAMP NOT NULL DEFAULT NOW()
);
```

### 6.3 Audit Retention

- Default: 90 days
- Configurable per tenant
- Archived to cold storage after retention
- No deletion (append-only)

---

## 7. خلاصة

### Security Layers

```
Layer 1: API (Tenant validation, JWT, rate limiting, CORS)
  ↓
Layer 2: Database (RLS, FK constraints, indexes)
  ↓
Layer 3: Application (Service-layer filtering, permission checks)
  ↓
Layer 4: UI (Page-level and action-level gating)
  ↓
Layer 5: Device (Fingerprint, session isolation, encryption)
```

### Key Principles

1. **Zero-Trust** — no automatic access, verify everything
2. **Defense in Depth** — multiple layers of security
3. **Tenant Isolation** — strict separation between tenants
4. **Local DB Isolation** — strict separation between local DBs
5. **No Master Password/Backdoor** — all access through proper auth
6. **Audit Everything** — all operations logged
7. **Secret Management** — no secrets in client, env vars for server
8. **Encryption** — at rest and in transit

---
*Generated by OpenCode — Phase 2 Architecture Design — 2026-11-15*
