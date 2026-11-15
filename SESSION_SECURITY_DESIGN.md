# SESSION_SECURITY_DESIGN.md

> Phase 2 — Architecture Design (Design Only)
> Generated: 2026-11-15

---

## 1. Session Model

### 1.1 Session Entity

```sql
CREATE TABLE sessions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    local_database_id UUID NOT NULL REFERENCES local_databases(id),
    local_user_id INT NOT NULL REFERENCES local_users(id),
    device_id UUID NOT NULL REFERENCES devices(id),
    tenant_id UUID NOT NULL REFERENCES tenants(id),
    token VARCHAR(500) NOT NULL,
    refresh_token VARCHAR(500),
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    expires_at TIMESTAMP NOT NULL,
    last_activity_at TIMESTAMP NOT NULL DEFAULT NOW(),
    is_offline BOOLEAN NOT NULL DEFAULT FALSE,
    offline_expires_at TIMESTAMP NULL,
    is_revoked BOOLEAN NOT NULL DEFAULT FALSE,
    revoked_at TIMESTAMP NULL,
    revoked_reason VARCHAR(100)
);
```

### 1.2 Session Types

| Type | Duration | Refresh | Offline |
|------|----------|---------|---------|
| Cloud Session | 15 min (access) + 7 days (refresh) | Yes | No |
| Local Session | 8 hours | No | Yes (72h) |
| Offline Session | 72 hours | No | Yes |

---

## 2. Session Lifecycle

### 2.1 Creation

```
1. User authenticates (cloud or local)
2. System verifies credentials
3. System verifies tenant membership
4. System verifies local DB membership
5. System verifies device authorization
6. System creates session (isolated per local DB)
7. System generates session token (JWT)
8. System stores session in DB
9. System returns token to client
```

### 2.2 Validation

```
1. Client sends request with session token
2. System verifies token signature
3. System verifies token not expired
4. System verifies session exists in DB
5. System verifies session not revoked
6. System verifies LocalDatabaseId matches
7. System verifies DeviceId matches
8. System verifies UserId matches
9. System updates last_activity_at
10. System processes request
```

### 2.3 Invalidation Triggers

| Trigger | Action | Scope |
|---------|--------|-------|
| User logout | Invalidate session | Single session |
| DB switch | Invalidate all sessions for old DB | All sessions for DB |
| User revoked | Invalidate all user sessions | All sessions for user |
| Device revoked | Invalidate all device sessions | All sessions for device |
| Tenant suspended | Invalidate all tenant sessions | All sessions for tenant |
| Local DB suspended | Invalidate all DB sessions | All sessions for DB |
| Session expired | Auto-invalidate | Single session |
| Password changed | Invalidate all user sessions | All sessions for user |
| Permissions changed | Invalidate all user sessions | All sessions for user |
| Offline expired | Invalidate offline session | Single session |

### 2.4 Refresh Token Rotation

```
1. Client sends refresh token
2. System verifies refresh token
3. System verifies refresh token not expired
4. System verifies session exists
5. System generates new access token
6. System generates new refresh token
7. System invalidates old refresh token
8. System stores new refresh token
9. System returns new tokens
```

**Rules:**
- Refresh token is single-use (rotated on each refresh)
- Old refresh token is invalidated immediately
- If old refresh token is reused → revoke all sessions (token theft detection)

---

## 3. Session Isolation

### 3.1 Isolation Rules

1. **One session per local DB** — switching DBs creates new session
2. **No session reuse** — old session invalidated on DB switch
3. **Session tied to LocalDatabaseId** — token includes LocalDatabaseId
4. **No cross-DB session sharing** — even for same user
5. **Session invalidated on DB switch** — all old sessions closed

### 3.2 Session Switching Flow

```
1. User selects "Switch Database"
2. System shows confirmation (unsaved changes?)
3. System closes current session (invalidate token)
4. System clears current data context
5. System clears current permissions
6. System opens new DB (verify fingerprint, status, etc.)
7. System prompts for local login (if required)
8. System verifies local user + membership
9. System verifies device authorization
10. System creates new session
11. System loads new permissions
12. System refreshes all data (UI reload)
13. System updates app title (show DB name)
```

### 3.3 Session Token Structure

```json
{
  "sub": "local_user_id",
  "tid": "tenant_id",
  "ldb": "local_database_id",
  "dev": "device_id",
  "role": "role_name",
  "perms": ["perm1", "perm2"],
  "iat": 1234567890,
  "exp": 1234571490,
  "offline_exp": 1234829490
}
```

---

## 4. Offline Session Management

### 4.1 Offline Session Flow

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

### 4.2 Offline Expiry

| Parameter | Default | Configurable | Description |
|-----------|---------|-------------|-------------|
| OfflineValidityHours | 72 | Yes | Hours of offline access |
| WarningThresholdHours | 12 | Yes | Hours before expiry to warn |
| MaxOfflineTransactions | 1000 | Yes | Max transactions while offline |

### 4.3 Offline Session States

| State | Description | Login Allowed | Sync Allowed |
|-------|-------------|---------------|--------------|
| Online | Connected to cloud | ✅ Yes | ✅ Yes |
| Offline (valid) | No connection, within validity window | ✅ Yes | ⏸️ Queued |
| Offline (expiring) | No connection, approaching expiry | ✅ Yes | ⏸️ Queued + Warning |
| Offline (expired) | No connection, past validity | ❌ No | ❌ No |
| Revoked | User/device revoked | ❌ No | ❌ No |

---

## 5. Session Security

### 5.1 Token Security

- JWT signed with HMAC-SHA256
- Token includes LocalDatabaseId (prevents cross-DB use)
- Token includes DeviceId (prevents device spoofing)
- Token includes TenantId (prevents cross-tenant access)
- Short access token expiry (15 minutes)
- Long refresh token expiry (7 days)
- Refresh token rotation (single-use)

### 5.2 Token Theft Detection

```
1. Client uses refresh token
2. System verifies refresh token
3. System generates new refresh token
4. System invalidates old refresh token
5. If old refresh token is reused → revoke ALL sessions (token theft)
6. Alert admin
7. Log in Audit Trail
```

### 5.3 Session Hijacking Prevention

- Device fingerprint verification
- IP address tracking (logged, not enforced)
- User-Agent tracking (logged, not enforced)
- Session tied to device (cannot transfer)
- Token bound to device fingerprint

---

## 6. Session Monitoring

### 6.1 Active Sessions View

| Field | Description |
|-------|-------------|
| Session ID | Unique session ID |
| User | Username |
| Local DB | Local database name |
| Device | Device name |
| Created | Session creation time |
| Last Activity | Last activity time |
| Expires | Session expiry time |
| Status | Active / Expired / Revoked |
| Offline | Yes/No |

### 6.2 Session Actions

- View active sessions
- Revoke session
- Revoke all sessions for user
- Revoke all sessions for device
- Revoke all sessions for DB

---

## 7. خلاصة

### Session Lifecycle

```
Login → Create Session → Validate → Refresh → Invalidate
  ↑                                              ↓
  └──────── Re-login (if invalidated) ──────────┘
```

### Key Principles

1. **One session per local DB** — no reuse
2. **Session tied to LocalDatabaseId** — prevents cross-DB access
3. **Refresh token rotation** — single-use, theft detection
4. **Offline expiry** — configurable, with warnings
5. **Immediate revocation** — on user/device/tenant changes
6. **Audit everything** — all session events logged

---
*Generated by OpenCode — Phase 2 Architecture Design — 2026-11-15*
