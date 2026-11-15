# OFFLINE_AUTHORIZATION_POLICY.md

> Phase 2 — Architecture Design (Design Only)
> Generated: 2026-11-15

---

## 1. Policy Overview

### 1.1 Purpose

Define how user authorization works when the device is offline (no internet connection).

### 1.2 Key Principle

**Offline access is a privilege, not a right.** It must be:
- Time-limited
- Configurable
- Revocable
- Auditable
- Secure

---

## 2. Policy Parameters

| Parameter | Default | Configurable | Description |
|-----------|---------|-------------|-------------|
| OfflineValidityHours | 72 | Yes (per tenant) | Hours of offline access |
| WarningThresholdHours | 12 | Yes (per tenant) | Hours before expiry to warn |
| MaxOfflineTransactions | 1000 | Yes (per tenant) | Max transactions while offline |
| ReverificationRequired | Yes | Yes | Require cloud re-verification on reconnect |
| OfflineGracePeriodMinutes | 30 | Yes | Grace period after expiry for re-verification |
| AutoLockOnExpiry | Yes | Yes | Lock local session on expiry |

---

## 3. Offline Flow

### 3.1 Normal Offline Flow

```
1. User logs in (cloud + local) → last verification timestamp stored
2. Internet lost → switch to offline mode
3. Local auth continues (within validity window)
4. Transactions queued in outbox
5. Warning shown when approaching expiry (12h before)
6. Internet restored → re-verify with cloud
7. If user active → sync queued transactions
8. If conflict → resolve via conflict resolution
```

### 3.2 Offline Expiry Flow

```
1. Offline validity expires (72h)
2. Warning shown: "Offline verification expired"
3. Local session locked (no new transactions)
4. User must connect to internet
5. Re-verification with cloud
6. If user active → unlock session, sync transactions
7. If user revoked → reject, lock permanently
```

### 3.3 User Revocation During Offline

```
1. Admin revokes user on Device B (online)
2. Revocation recorded in cloud
3. Device A (offline) cannot know immediately
4. Device A continues within offline window (policy allows)
5. Internet restored on Device A
6. Re-verification fails (user revoked)
7. Local session locked
8. Pending transactions reviewed (may be rejected)
9. Audit trail records the revocation event
10. Admin notified of pending transactions
```

### 3.4 Device Revocation During Offline

```
1. Admin revokes device on cloud
2. Device A (offline) cannot know immediately
3. Device A continues within offline window
4. Internet restored on Device A
5. Re-verification fails (device revoked)
6. Local session locked
7. Pending transactions reviewed
8. Audit trail records the revocation event
```

### 3.5 Permission Change During Offline

```
1. Admin changes user permissions on cloud
2. Device A (offline) cannot know immediately
3. Device A continues with old permissions (within validity window)
4. Internet restored on Device A
5. Re-verification: permissions updated
6. If permission removed → restrict access
7. If permission added → grant access
8. Audit trail records the change
```

---

## 4. Offline Session States

| State | Description | Login | Transactions | Warning |
|-------|-------------|-------|--------------|---------|
| Online | Connected | ✅ | ✅ | None |
| Offline (valid) | No connection, within window | ✅ | ✅ | None |
| Offline (expiring) | No connection, <12h remaining | ✅ | ✅ | "Expires in X hours" |
| Offline (expired) | No connection, past validity | ❌ | ❌ | "Verification expired" |
| Revoked | User/device revoked | ❌ | ❌ | "Account revoked" |
| Suspended | Tenant suspended | ❌ | ❌ | "Tenant suspended" |

---

## 5. Warning UI

### 5.1 Banners

- **Offline mode**: "أنت تعمل دون اتصال — ستنتهي صلاحية التحقق خلال X ساعة"
- **Offline expiring**: "⚠️ صلاحية التحقق دون اتصال تنتهي خلال X ساعة — يرجى الاتصال بالإنترنت"
- **Offline expired**: "❌ صلاحية التحقق دون اتصال انتهت — يرجى الاتصال بالإنترنت"
- **Revoked**: "❌ تم إلغاء حسابك — يرجى مراجعة مدير النظام"
- **Suspended**: "❌ تم تعليق شركتك — يرجى مراجعة مدير النظام"

### 5.2 Dialogs

- **Offline expired**: "صلاحية التحقق دون اتصال انتهت. يرجى الاتصال بالإنترنت لتجديد صلاحية الوصول."
- **Revoked**: "تم إلغاء حسابك. يرجى مراجعة مدير النظام."
- **Pending transactions**: "لديك X عمليات معلقة. يرجى الاتصال بالإنترنت لمزامنتها."

---

## 6. Pending Transactions Handling

### 6.1 Transaction Queue

- All transactions created offline are queued in outbox
- Outbox persists to local DB
- Transactions include: invoices, payments, inventory movements, etc.
- Each transaction has: EventId, TenantId, LocalDatabaseId, EntityType, EntityId, OperationType, Version, Payload, CreatedAt

### 6.2 On Reconnect

```
1. Internet restored
2. Re-verify user (cloud auth)
3. If user active:
   a. Sync queued transactions (push to cloud)
   b. Pull cloud changes (pull from cloud)
   c. Resolve conflicts (if any)
   d. Update local DB
4. If user revoked:
   a. Reject all pending transactions
   b. Lock local session
   c. Alert admin
   d. Log in Audit Trail
```

### 6.3 Transaction Rejection

- If user revoked: all pending transactions rejected
- If permission removed: transactions requiring that permission rejected
- If conflict: transaction held for manual review
- Rejected transactions logged in Audit Trail

---

## 7. Testing Offline Scenarios

### 7.1 Test Cases

| # | Scenario | Expected |
|---|----------|----------|
| 1 | Normal offline (within validity) | ✅ Transactions queued, sync on reconnect |
| 2 | Offline expiry | ❌ Session locked, no new transactions |
| 3 | User revoked during offline | ❌ Session locked on reconnect, pending transactions rejected |
| 4 | Device revoked during offline | ❌ Session locked on reconnect |
| 5 | Permission changed during offline | ⚠️ Old permissions used offline, updated on reconnect |
| 6 | Tenant suspended during offline | ❌ Session locked on reconnect |
| 7 | Max offline transactions exceeded | ❌ No new transactions, warning shown |
| 8 | Re-verification on reconnect | ✅ Success if user active |

### 7.2 Test Procedure

```
1. Login on Device A (online)
2. Disconnect internet
3. Create transactions (within validity window)
4. Verify transactions queued
5. Verify warning shown when approaching expiry
6. Reconnect internet
7. Verify re-verification successful
8. Verify queued transactions synced
9. Verify no duplicates
```

---

## 8. خلاصة

### Offline Authorization Policy

- **Default validity**: 72 hours (configurable per tenant)
- **Warning threshold**: 12 hours before expiry
- **Max offline transactions**: 1000 (configurable)
- **Re-verification**: Required on reconnect
- **User revocation**: Applied on reconnect (not immediate)
- **Pending transactions**: Queued, synced on reconnect
- **Audit trail**: All offline events logged

### Key Principles

1. **Offline is a privilege** — time-limited and configurable
2. **No immediate revocation** — offline devices continue within validity window
3. **Re-verification on reconnect** — all changes validated
4. **Pending transactions preserved** — no data loss
5. **Audit everything** — all offline events logged
6. **Clear warnings** — user informed of expiry

---
*Generated by OpenCode — Phase 2 Architecture Design — 2026-11-15*
