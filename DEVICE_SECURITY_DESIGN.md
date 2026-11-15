# DEVICE_SECURITY_DESIGN.md

> Phase 2 — Architecture Design (Design Only)
> Generated: 2026-11-15

---

## 1. Device Model

### 1.1 Device Entity

```sql
CREATE TABLE devices (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    device_type VARCHAR(20) NOT NULL, -- Desktop, Mobile, Web
    installation_id UUID NOT NULL,
    tenant_id UUID NOT NULL REFERENCES tenants(id),
    local_database_id UUID REFERENCES local_databases(id),
    device_status VARCHAR(20) NOT NULL DEFAULT 'pending', -- pending, active, suspended, revoked
    device_fingerprint VARCHAR(255) NOT NULL,
    registered_at TIMESTAMP NOT NULL DEFAULT NOW(),
    last_seen_at TIMESTAMP NULL,
    revoked_at TIMESTAMP NULL,
    app_version VARCHAR(50),
    os_info VARCHAR(100),
    UNIQUE (installation_id, tenant_id)
);
```

### 1.2 Device Statuses

| Status | Description | Login Allowed | Sync Allowed |
|--------|-------------|---------------|--------------|
| pending | Awaiting approval | ❌ No | ❌ No |
| active | Approved and active | ✅ Yes | ✅ Yes |
| suspended | Temporarily suspended | ❌ No | ❌ No |
| revoked | Permanently revoked | ❌ No | ❌ No |

### 1.3 Device Fingerprint

```
Fingerprint = SHA-256(
    MachineName +
    OSVersion +
    ProcessorCount +
    MAC Address (optional) +
    InstallationId
)
```

**Verification:**
- On device registration: compute and store fingerprint
- On login: compute and compare with stored
- On sync: verify fingerprint matches
- Mismatch: reject access, alert admin

---

## 2. Device Registration Flow

### 2.1 Desktop Registration

```
1. App starts (first run)
2. Generate InstallationId (GUID)
3. Compute DeviceFingerprint
4. Prompt user for login (cloud credentials)
5. Cloud auth: verify credentials
6. Cloud auth: verify tenant membership
7. Check if device already registered
   - If yes: verify fingerprint matches
   - If no: register new device (status: pending)
8. Admin approves device (status: active)
9. Link device to tenant
10. Store device info locally
11. Enable sync
```

### 2.2 Mobile Registration

```
1. Mobile app starts
2. Generate InstallationId (GUID)
3. Compute DeviceFingerprint (browser/device info)
4. User scans QR code from Desktop OR enters activation link
5. Cloud auth: verify QR token (single-use, short-lived)
6. Cloud auth: verify tenant membership
7. Register device (status: pending)
8. Admin approves device (status: active)
9. Store device info locally
10. Enable sync
```

### 2.3 QR Pairing Flow

```
1. Desktop generates QR code containing:
   - PairingToken (random, 32 bytes)
   - TenantId
   - Expiry (5 minutes)
   - Single-use flag
2. Mobile scans QR code
3. Mobile sends PairingToken to API
4. API validates token (not expired, not used)
5. API creates device (status: pending)
6. API marks token as used
7. Admin approves device
8. Mobile receives approval notification
9. Mobile enables sync
```

### 2.4 Activation Link Flow

```
1. Admin generates activation link from Desktop
2. Link contains: TenantId, ActivationCode (random, 16 chars), Expiry (24 hours)
3. Mobile opens link (deep link)
4. Mobile sends ActivationCode to API
5. API validates code (not expired, not used)
6. API creates device (status: pending)
7. API marks code as used
8. Admin approves device
9. Mobile enables sync
```

---

## 3. Device Management UI

### 3.1 Desktop: Device Management Page

**Columns:**
- Device Name
- Device Type (Desktop/Mobile/Web)
- Status (Active/Suspended/Revoked/Pending)
- Tenant
- Local DB
- Last Seen
- Last Sync
- App Version
- Registered By
- Registered At

**Actions:**
- View details
- Approve (for pending)
- Suspend
- Revoke
- Reactivate
- View activity log
- View sync status

### 3.2 Mobile: Device Info

**Display:**
- Device name
- Device type
- Status
- Last sync
- App version

**Actions:**
- View sync status
- Request re-pairing

---

## 4. Device Revocation

### 4.1 Revocation Flow

```
1. Admin revokes device
2. System sets DeviceStatus = Revoked
3. System invalidates all sessions for this device
4. System sets RevokedAt = NOW()
5. System logs in Audit Trail
6. On next sync attempt: device rejected
7. On next login attempt: device rejected
8. Local data remains (read-only until re-pairing)
```

### 4.2 Revocation Effects

- Device cannot login
- Device cannot sync
- Device cannot access API
- Local data remains (but cannot be modified)
- Audit trail records revocation

### 4.3 Re-activation

```
1. Admin re-activates device
2. System sets DeviceStatus = Active
3. System clears RevokedAt
4. System logs in Audit Trail
5. Device can login and sync again
```

---

## 5. Device-Tenant Link

### 5.1 Link Structure

```sql
CREATE TABLE device_tenant_links (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    device_id UUID NOT NULL REFERENCES devices(id),
    tenant_id UUID NOT NULL REFERENCES tenants(id),
    status VARCHAR(20) NOT NULL DEFAULT 'active',
    permissions JSONB,
    linked_at TIMESTAMP NOT NULL DEFAULT NOW(),
    linked_by INT REFERENCES users(id),
    revoked_at TIMESTAMP NULL,
    UNIQUE (device_id, tenant_id)
);
```

### 5.2 Link Rules

1. A device can be linked to multiple tenants
2. Each link has independent permissions
3. Revoking a link does not affect other links
4. Device must be active to have links
5. Link permissions override device permissions

---

## 6. Security Considerations

### 6.1 Threat Model

| Threat | Mitigation |
|--------|-----------|
| Device spoofing | Device fingerprint verification |
| QR code interception | Single-use, short-lived token |
| Device theft | Remote revocation, session invalidation |
| Unauthorized pairing | Admin approval required |
| Replay attack | Token single-use, timestamp validation |
| Device cloning | Fingerprint mismatch detection |

### 6.2 Rate Limiting

- Device registration: 5 per hour per IP
- QR pairing: 10 per hour per device
- Login: 5 per minute per device
- Sync: 60 per minute per device

### 6.3 Audit

- All device operations logged
- All pairing attempts logged
- All revocation events logged
- All fingerprint mismatches logged

---

## 7. خلاصة

### Device Management Flow

```
Register Device → Admin Approve → Link to Tenant → Active → Sync
                                                              ↓
                                                         Revoke (if needed)
                                                              ↓
                                                         Revoked (no access)
```

### Key Principles

1. **No auto-approval** — admin must approve
2. **No auto-linking** — explicit tenant link required
3. **No cross-tenant access** — device scoped to tenant
4. **Fingerprint verification** — prevents device spoofing
5. **Audit everything** — all operations logged

---
*Generated by OpenCode — Phase 2 Architecture Design — 2026-11-15*
