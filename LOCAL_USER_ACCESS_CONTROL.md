# LOCAL_USER_ACCESS_CONTROL.md

> Phase 2 — Architecture Design (Design Only)
> Generated: 2026-11-15

---

## 1. Local User Model

### 1.1 Local User vs Cloud User

| Attribute | Cloud User | Local User |
|-----------|-----------|------------|
| Identity | Email, Username | Username (local) |
| Authentication | Password (bcrypt) | Password (bcrypt) |
| Scope | Global (all tenants) | Single local DB |
| Permissions | Tenant membership | Local membership |
| Sync | Synced to cloud | Local only |
| Offline | Requires cloud auth | Works offline |

### 1.2 Local User Table

```sql
CREATE TABLE local_users (
    id SERIAL PRIMARY KEY,
    local_database_id UUID NOT NULL REFERENCES local_databases(id),
    cloud_user_id INT NULL REFERENCES users(id),
    username VARCHAR(50) NOT NULL,
    password_hash VARCHAR(255) NOT NULL,
    full_name VARCHAR(100) NOT NULL,
    role_id INT NOT NULL REFERENCES roles(id),
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    last_login TIMESTAMP NULL,
    UNIQUE (local_database_id, username)
);
```

### 1.3 Local Membership Table

```sql
CREATE TABLE local_memberships (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    local_user_id INT NOT NULL REFERENCES local_users(id),
    local_database_id UUID NOT NULL REFERENCES local_databases(id),
    role_id INT NOT NULL REFERENCES roles(id),
    permissions JSONB,
    status VARCHAR(20) NOT NULL DEFAULT 'active',
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    created_by INT REFERENCES local_users(id),
    UNIQUE (local_user_id, local_database_id)
);
```

---

## 2. Local User Operations

### 2.1 Create User

```
1. Verify creator has "users.edit" permission in this local DB
2. Verify username is unique within this local DB
3. Hash password (bcrypt)
4. Create local_user record
5. Create local_membership record
6. Log in Audit Trail
```

### 2.2 Edit User

```
1. Verify editor has "users.edit" permission
2. Verify target user exists in this local DB
3. Update user fields
4. If role changed: update membership
5. If password changed: hash new password
6. Log in Audit Trail
```

### 2.3 Deactivate User

```
1. Verify deactivator has "users.edit" permission
2. Verify target user is not the last admin
3. Set is_active = FALSE
4. Invalidate all sessions for this user
5. Log in Audit Trail
```

### 2.4 Delete User

```
1. Verify deleter has "users.edit" permission
2. Check if user has any records (invoices, payments, etc.)
3. If has records: soft delete (set is_active=FALSE, keep for audit)
4. If no records: hard delete
5. Log in Audit Trail
```

### 2.5 Change Password

```
1. Verify user is authenticated (local session)
2. Verify current password
3. Hash new password
4. Update password_hash
5. Invalidate all sessions (force re-login)
6. Log in Audit Trail
```

### 2.6 Reset Password (Admin)

```
1. Verify admin has "users.edit" permission
2. Generate temporary password
3. Hash and store
4. Set must_change_password = TRUE
5. Log in Audit Trail
```

---

## 3. Role Management

### 3.1 Role Types

| Role Type | Scope | Description |
|-----------|-------|-------------|
| System Role | Global | Predefined (ADMIN, MANAGER, etc.) — cannot be deleted |
| Tenant Role | Tenant | Custom roles per tenant |
| Local Role | Local DB | Custom roles per local DB |

### 3.2 Role Operations

| Operation | Scope | Permission |
|-----------|-------|-----------|
| Create role | Local DB | users.roles |
| Edit role | Local DB | users.roles |
| Delete role | Local DB | users.roles (not system roles) |
| Assign role | Local DB | users.roles |
| View roles | Local DB | users.view |

### 3.3 Permission Matrix

| Permission | Description | Default Roles |
|------------|-------------|---------------|
| dashboard.view | View dashboard | All |
| parties.view/edit/delete | Party management | ADMIN, MANAGER, SALESPERSON |
| items.view/edit/delete | Item management | ADMIN, MANAGER, STOREKEEPER |
| invoices.view/edit/delete/cancel | Invoice management | ADMIN, MANAGER, SALESPERSON, CASHIER |
| payments.view/edit/delete | Payment management | ADMIN, MANAGER, CASHIER |
| subscriptions.view/edit/charge | Subscription management | ADMIN, MANAGER, ACCOUNTANT |
| cashboxes.view/edit/transfer/close | Cash box management | ADMIN, MANAGER, CASHIER |
| inventory.view/adjust/count/damaged/writeoff/transfer/audit | Inventory management | ADMIN, MANAGER, STOREKEEPER |
| warehouses.view/edit | Warehouse management | ADMIN, MANAGER, STOREKEEPER |
| accounts.edit/view | Account management | ADMIN, ACCOUNTANT |
| journals.view/edit/post | Journal management | ADMIN, ACCOUNTANT |
| periods.view/edit/close | Period management | ADMIN, ACCOUNTANT |
| budgets.view/edit | Budget management | ADMIN, ACCOUNTANT, MANAGER |
| loans.view/edit/repay | Loan management | ADMIN, MANAGER, ACCOUNTANT |
| pettycash.view/edit | Petty cash management | ADMIN, MANAGER |
| costs.view/edit | Cost management | ADMIN, MANAGER, ACCOUNTANT |
| assets.view/edit | Asset management | ADMIN, MANAGER, ACCOUNTANT |
| bankrec.view/edit | Bank reconciliation | ADMIN, ACCOUNTANT |
| baddebt.view/edit/approve | Bad debt management | ADMIN, MANAGER, ACCOUNTANT |
| discounts.view/edit | Discount management | ADMIN, MANAGER |
| fees.view/edit | Fee management | ADMIN, MANAGER |
| employees.view/edit/salary | Employee management | ADMIN, MANAGER |
| users.view/edit/permissions/roles | User management | ADMIN |
| settings.view/backup/export/import/restore/data | Settings | ADMIN |
| reports.view/inventory/profit/settings | Reports | ADMIN, MANAGER, ACCOUNTANT |
| revenue.view/manage | Revenue recognition | ADMIN, MANAGER, ACCOUNTANT |
| audit.view | Audit log | ADMIN, MANAGER |

---

## 4. Cloud User Link

### 4.1 Linking Cloud User to Local User

```
1. User logs in with cloud credentials
2. System finds cloud user (by email/username)
3. System checks if local user exists for this local DB
4. If not: prompt to create local user (link to cloud user)
5. If yes: verify local password (or use cloud password)
6. Create local session
```

### 4.2 Cloud User Permissions

- Cloud user permissions are **tenant-scoped** (from tenant_memberships)
- Local user permissions are **DB-scoped** (from local_memberships)
- Cloud permissions do NOT automatically grant local permissions
- Local permissions do NOT automatically grant cloud permissions

### 4.3 Multi-DB User

A cloud user can be linked to multiple local users (one per local DB):

```
Cloud User: ahmed@example.com
  ├── Local User A (DB A, role: Admin, permissions: full)
  ├── Local User B (DB B, role: Cashier, permissions: limited)
  └── Local User C (DB C, role: Viewer, permissions: read-only)
```

Each local user has independent permissions. No cross-DB access.

---

## 5. Permission Enforcement

### 5.1 Enforcement Points

| Layer | Enforcement | Description |
|-------|-------------|-------------|
| UI (Desktop) | CanExecute (RelayCommand) | Button enabled/disabled |
| UI (Mobile) | Conditional rendering | Button visible/hidden |
| API | [Authorize] + PermissionHandler | 403 if no permission |
| Service | HasPermission check | Defense in depth |
| Database | RLS policies | Row-level filtering |

### 5.2 Permission Check Flow

```
1. User action (click button)
2. UI: CanExecute checks HasPermission(key)
3. If false: button disabled, no action
4. If true: action proceeds
5. Service: HasPermission check (defense in depth)
6. API: PermissionHandler checks (if API call)
7. DB: RLS filters rows (if applicable)
```

### 5.3 Permission Resolution Order

```
1. System role permissions (from RoleCatalog)
2. Tenant role permissions (from tenant_memberships)
3. Local role permissions (from local_memberships)
4. Per-user overrides (grant/deny)
5. Effective permissions = union of all, minus denies
```

---

## 6. Audit Trail

### 6.1 Audit Log Structure

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

### 6.2 Audited Actions

| Action | Description | Audited |
|--------|-------------|---------|
| LOGIN | User login | ✅ |
| LOGOUT | User logout | ✅ |
| CREATE | Record created | ✅ |
| UPDATE | Record updated | ✅ |
| DELETE | Record deleted | ✅ |
| CANCEL | Record cancelled | ✅ |
| APPROVE | Record approved | ✅ |
| EXPORT | Data exported | ✅ |
| IMPORT | Data imported | ✅ |
| SYNC | Sync operation | ✅ |
| BACKUP | Backup created | ✅ |
| RESTORE | Backup restored | ✅ |
| DEVICE_REGISTER | Device registered | ✅ |
| DEVICE_REVOKE | Device revoked | ✅ |
| PERMISSION_CHANGE | Permission changed | ✅ |
| ROLE_CHANGE | Role changed | ✅ |
| USER_REVOKE | User revoked | ✅ |
| EMERGENCY_ACCESS | Emergency access used | ✅ |

### 6.3 Audit Protection

- No user can edit or delete audit records
- Audit records are append-only
- Audit records include tenant context
- Audit records are synced to cloud
- Audit records are retained per retention policy

---

## 7. خلاصة

### مبادئ أساسية

1. **Local user ≠ Cloud user** — independent permissions per DB
2. **No automatic access** — explicit membership required
3. **No master password/backdoor** — all access through proper auth
4. **Audit everything** — all actions logged
5. **Defense in depth** — UI + Service + API + DB

### التوافق مع النظام الحالي

- Existing `users` table → becomes `cloud_users` (or stays as-is with tenant context)
- Existing `roles` / `role_permissions` → become tenant-scoped
- Existing `user_permissions` → become local overrides
- New tables: `local_users`, `local_memberships`, `local_databases`

---
*Generated by OpenCode — Phase 2 Architecture Design — 2026-11-15*
