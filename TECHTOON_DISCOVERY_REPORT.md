# TECHTOON_DISCOVERY_REPORT.md

> جولة 2026-11-15 · OpenCode (GLM 5.3) · Discovery Phase (Phase 1)
> الهدف: التحقق من البنية الفعلية قبل any changes

---

## 1. ملخص تنفيذي

| البند | الحالة |
|-------|--------|
| Multi-tenant architecture | ❌ **غير موجود** — النظام single-tenant (شركة واحدة per DB) |
| Local database isolation | ❌ **غير موجود** — قاعدة واحدة per installation |
| TenantId في الجداول | ❌ **0 references** في الكود (verified by grep) |
| Sync Engine | ❌ **غير موجود** — لا Outbox, لا Replica, لا Sync Log |
| Offline Authorization | ❌ **غير موجود** — لا سياسة offline |
| Device Management | ⚠️ **جزئي** — user_devices table exists (refresh tokens) but no full device management |
| Cloud DB (Neon) | ✅ موجود (PostgreSQL, single-tenant) |
| Fly.io hosting | ✅ موجود (mobile PWA + API) |
| Desktop WPF | ✅ .NET 10, self-contained |
| Mobile Flutter/PWA | ✅ Flutter Web (not built locally) |

**الخلاصة:** النظام الحالي single-tenant/single-DB. Multi-tenant architecture هي **ميزة جديدة** تتطلب تطويرًا جوهريًا، وليس "إصلاحًا" (fix). تتطلب Migration + Schema changes + API changes + Desktop UI + Mobile changes + Sync Engine.

---

## 2. البنية الحالية (Current Architecture)

### 2.1 المشاريع (Projects)

| المشروع | التقنية | الدور | Source files |
|---------|---------|-------|-------------|
| `Techtoon.Core` | .NET 10 | Models, Permissions, Bundles, Licensing | 55 |
| `Techtoon.Data` | EF Core + Npgsql | DbContext, Migrations | 8 |
| `Techtoon.UI` | WPF (.NET 10) | Desktop app (43 views, 50 ViewModels) | 1331 |
| `Techtoon.Api` | ASP.NET Core (.NET 10) | REST API (18 controllers) | 68 |
| `Techtoon.Tests` | xUnit | 156 tests | 36 |
| `Techtoon.Mobile` | Flutter/Dart | Mobile PWA (22 dart files) | 22 |
| `Techtoon.Streamlit` | Python | Dashboard (17 files) | 17 |
| `Techtoon.LicenseTool` | .NET 10 | License generator | 10 |

### 2.2 قاعدة البيانات (Database)

- **86 base tables + 5 views** in PostgreSQL (Neon)
- **5 FK constraints** (most integrity is app-level)
- **0 TenantId columns** in any table
- **0 sync/outbox/replica/device-management tables** (except `user_devices` for refresh tokens)
- **Neon connection**: single database `neondb` (production), `techtoon_test` (working/test)

### 2.3 API (ASP.NET Core)

- 18 controllers: Auth, Invoices, Items, Parties, Payments, Expenses, CashBoxes, Employees, Salaries, PettyCash, RecurringJournal, Loans, Warehouses, WarehouseTransfers, BankReconciliation, BadDebt, Health
- JWT authentication (15 min access, 7 day refresh)
- Permission-based authorization (PermissionHandler)
- CORS configured (specific origins)
- Health check (Neon PostgreSQL)
- **No tenant validation** (single-tenant)

### 2.4 Mobile (Flutter/PWA)

- Base URL: `https://techtoon-api.fly.dev` (configurable via `--dart-define`)
- Login via API (JWT)
- Screens: dashboard, invoices, payments, parties, products, inventory, cashboxes, expenses, loans, reports, settings
- **No tenant selection** (single company)
- **Not built locally** (no Flutter SDK)

### 2.5 Desktop (WPF)

- 43 views, 50 ViewModels
- Pages gated by `PermissionCatalog.PagePermission`
- Actions gated by `AuthService.HasPermission`
- Local auth via `SecureConnectionManager` (DPAPI-encrypted connection string)
- Auto-backup to `%LOCALAPPDATA%\TechtoonAccounting`
- **No multi-tenant support** (single DB connection)

---

## 3. what EXISTS (Verified)

### 3.1 Authentication & Authorization
- ✅ Login (username/password, bcrypt)
- ✅ JWT (access + refresh tokens)
- ✅ Device tracking (user_devices table)
- ✅ Permission system (88 permissions, 7 roles)
- ✅ Per-user overrides (grant/deny)
- ✅ Role seeding (RoleService.EnsureSchema)
- ❌ No tenant membership
- ❌ No local database users (single user table)
- ❌ No device management (only refresh token tracking)

### 3.2 Database
- ✅ 86 tables, 5 views
- ✅ EF Core DbContext
- ✅ Migrations (P0-P7)
- ❌ No TenantId columns
- ❌ No sync/outbox/replica tables
- ❌ No RLS (Row Level Security)
- ❌ No device isolation

### 3.3 Sync
- ❌ No sync engine
- ❌ No outbox pattern
- ❌ No conflict resolution
- ❌ No offline support
- ❌ No sync monitoring

### 3.4 Backup/Restore
- ✅ BackupService (CreateBackup, RestoreBackup, VerifyBackup)
- ✅ Constraints ordered, sequences, transaction
- ❌ No encryption
- ❌ No backup metadata (TenantId, LocalDatabaseId, SchemaVersion)
- ❌ No backup verification on restore

### 3.5 Security
- ✅ bcrypt password hashing
- ✅ JWT + refresh tokens
- ✅ CORS (specific origins)
- ✅ Rate limiting (configured)
- ✅ Input validation
- ❌ No tenant validation
- ❌ No device authorization
- ❌ No offline authorization
- ❌ Exposed Neon password in 13 files (Exemption C1 — out of scope)

---

## 4. what is MISSING (Required by Multi-Tenant)

### 4.1 Database Schema (New Tables Required)

| Table | Purpose | Status |
|-------|---------|--------|
| `tenants` | Company/organization entity | ❌ Missing |
| `tenant_memberships` | User ↔ Tenant membership | ❌ Missing |
| `local_databases` | Local DB identity (LocalDatabaseId, ReplicaId, Fingerprint) | ❌ Missing |
| `devices` | Device registration (DeviceId, DeviceType, Status) | ❌ Missing |
| `device_tenant_links` | Device ↔ Tenant authorization | ❌ Missing |
| `sync_outbox` | Outbox pattern (events pending sync) | ❌ Missing |
| `sync_conflicts` | Conflict records | ❌ Missing |
| `sync_log` | Sync history | ❌ Missing |
| `backup_metadata` | Backup metadata (TenantId, SchemaVersion, etc.) | ❌ Missing |
| `audit_trail` | Enhanced audit (TenantId, LocalDatabaseId, DeviceId, etc.) | ⚠️ Partial (audit_log exists but no tenant fields) |

### 4.2 Schema Modifications (Existing Tables)

| Change | Tables | Status |
|--------|--------|--------|
| Add `TenantId` column | 86 tables (all data tables) | ❌ Not done |
| Add `LocalDatabaseId` column | All data tables | ❌ Not done |
| Add `Version` column | All data tables (for sync) | ❌ Not done |
| Add `IsDeleted` (soft delete) | All data tables | ❌ Not done |
| Add `CreatedAt` / `UpdatedAt` | All data tables | ⚠️ Partial (some have created_at) |
| FK constraints (TenantId) | All data tables | ❌ Not done |
| RLS policies | All data tables | ❌ Not done |

### 4.3 API Changes

| Change | Status |
|--------|--------|
| Tenant validation middleware | ❌ Missing |
| Tenant-aware queries (repository layer) | ❌ Missing |
| Device authorization | ❌ Missing |
| Sync endpoints (push/pull) | ❌ Missing |
| Conflict resolution endpoints | ❌ Missing |
| Device management endpoints | ❌ Missing |
| Tenant management endpoints | ❌ Missing |

### 4.4 Desktop Changes

| Change | Status |
|--------|--------|
| Tenant selection on login | ❌ Missing |
| Local database selection/management | ❌ Missing |
| Device registration | ❌ Missing |
| Sync status page | ❌ Missing |
| Conflict resolution UI | ❌ Missing |
| Device management page | ❌ Missing |
| Tenant management page | ❌ Missing |
| Offline authorization | ❌ Missing |
| Backup/restore with metadata | ❌ Missing |

### 4.5 Mobile Changes

| Change | Status |
|--------|--------|
| Tenant selection on login | ❌ Missing |
| Device registration | ❌ Missing |
| Sync status | ❌ Missing |
| Conflict resolution | ❌ Missing |

---

## 5. مخاطر التعديل (Risk Assessment)

| # | المخاطرة | الأثر | التخفيف |
|---|----------|-------|---------|
| 1 | إضافة TenantId إلى 86 جدول | High | Migration on test DB first, verify data integrity |
| 2 | تغيير relationships (FK constraints) | High | Test on isolated DB, rollback plan |
| 3 | API changes (tenant validation) | Medium | Backward-compatible (default tenant) |
| 4 | Desktop UI changes (tenant selection) | Medium | Feature flag, gradual rollout |
| 5 | Mobile changes | Medium | API-compatible, no breaking changes |
| 6 | Sync engine (new) | High | Start with Desktop→Cloud only, then add Mobile |
| 7 | Offline authorization | Medium | Configurable timeout, clear warnings |
| 8 | Backup/restore changes | Low | Backup existing before changes |
| 9 | Data loss during migration | Critical | Test on isolated DB, backup production |
| 10 | User confusion (new UI) | Low | Gradual rollout, documentation |

---

## 6. خطة حماية البيانات (Data Safety Plan)

### قبل أي تغيير
1. **Backup production DB** (`neondb`) — full dump
2. **Backup working DB** (`techtoon_test`) — full dump
3. **Git commit** — commit current state (clean)
4. **Test on isolated DB** — `techtoon_test_ci` (copy of working DB)

### أثناء التغيير
1. **Migration on test DB first** — verify data integrity
2. **Rollback plan** — documented for each migration
3. **No production migration** without explicit approval
4. **No data deletion** — only additive changes (new columns, new tables)
5. **No user/password changes** — preserve all existing data

### بعد التغيير
1. **Verify data integrity** — counts, relationships, balances
2. **Verify all tests pass** — 156/156 + new tests
3. **Verify API works** — all endpoints
4. **Verify Desktop works** — all pages
5. **Verify Mobile works** — all screens

---

## 7. خطة Rollback

### لكل مرحلة
1. **Git revert** — revert commits if needed
2. **DB restore** — restore from backup
3. **Schema rollback** — drop new columns/tables (if additive)
4. **Config rollback** — restore appsettings

### Rollback triggers
- Data integrity failure
- Test failure (critical)
- User data loss
- Performance degradation
- Security vulnerability

---

## 8. الجداول التي تحتاج إلى تعديل (Tables Requiring Changes)

### 8.1 New Tables (10)
1. `tenants` — company entity
2. `tenant_memberships` — user ↔ tenant membership
3. `local_databases` — local DB identity
4. `devices` — device registration
5. `device_tenant_links` — device ↔ tenant authorization
6. `sync_outbox` — outbox pattern
7. `sync_conflicts` — conflict records
8. `sync_log` — sync history
9. `backup_metadata` — backup metadata
10. `audit_trail` — enhanced audit (or extend existing `audit_log`)

### 8.2 Modified Tables (86 — add TenantId + LocalDatabaseId + Version + IsDeleted)
All 86 base tables need:
- `TenantId` (FK to tenants)
- `LocalDatabaseId` (FK to local_databases) — for local DB tables
- `Version` (int) — for sync
- `IsDeleted` (bool) — soft delete
- `CreatedAt` / `UpdatedAt` (timestamp) — where missing

### 8.3 New Indexes
- `TenantId` on all data tables
- `LocalDatabaseId` on all local tables
- Composite: `(TenantId, Id)` for all data tables
- `Version` on all data tables (for sync)

### 8.4 New Constraints
- FK: `TenantId → tenants(id)` on all data tables
- FK: `LocalDatabaseId → local_databases(id)` on local tables
- Unique: `(TenantId, <natural key>)` where applicable

---

## 9. الوظائف التي تعتمد على الجداول المتأثرة

### 9.1 Directly Affected (86 tables)
All CRUD operations on all 86 tables will need tenant-aware queries.

### 9.2 Indirectly Affected
- Reports (all reports need tenant filtering)
- Dashboard (all metrics need tenant filtering)
- Audit (audit log needs tenant context)
- Backup/Restore (needs tenant metadata)
- Sync (new — depends on all tables)
- Permissions (needs tenant-scoped roles)

### 9.3 Not Affected
- Licensing (Techtoon.LicenseTool) — independent
- AppSettings (global, not tenant-scoped) — may need tenant scoping later

---

## 10. خطة التنفيذ (Implementation Plan)

### Phase 1 — Discovery ✅ (this report)
- [x] Examine project structure
- [x] Identify missing components
- [x] Document current state
- [x] Create discovery report

### Phase 2 — Architecture (NEXT)
- [ ] Design final architecture
- [ ] Define tenant isolation boundaries
- [ ] Define local DB isolation model
- [ ] Define permission scopes
- [ ] Define sync protocol
- [ ] Create MULTI_TENANT_ARCHITECTURE.md

### Phase 3 — Database Design
- [ ] Design new tables (10)
- [ ] Design schema modifications (86 tables)
- [ ] Create migration scripts
- [ ] Test on isolated DB

### Phase 4 — Tenant Isolation
- [ ] Add TenantId to all tables
- [ ] Implement tenant validation in API
- [ ] Implement tenant-aware queries
- [ ] Add RLS policies

### Phase 5 — Local Database Isolation
- [ ] Add LocalDatabaseId to all tables
- [ ] Implement local user management
- [ ] Implement device isolation
- [ ] Implement session isolation

### Phase 6 — Authentication
- [ ] Cloud authentication (existing JWT)
- [ ] Local authentication (new)
- [ ] Tenant authorization (new)
- [ ] Device authorization (new)

### Phase 7 — Authorization
- [ ] Tenant-scoped roles
- [ ] Tenant-scoped permissions
- [ ] Local DB permissions
- [ ] Device permissions

### Phase 8 — Device Management
- [ ] Device registration
- [ ] Device management UI
- [ ] Device pairing (QR)
- [ ] Device revocation

### Phase 9 — Backup and Recovery
- [ ] Backup metadata
- [ ] Backup encryption
- [ ] Restore with activation
- [ ] Replica prevention

### Phase 10 — Sync Engine
- [ ] Outbox pattern
- [ ] Sync endpoints (push/pull)
- [ ] Idempotency
- [ ] Conflict resolution
- [ ] Sync monitoring

### Phase 11 — Offline Authorization
- [ ] Offline timeout policy
- [ ] Re-verification on reconnect
- [ ] User revocation during offline
- [ ] Warning UI

### Phase 12 — Desktop UI
- [ ] Tenant selection
- [ ] Local DB management
- [ ] Device management
- [ ] Sync status
- [ ] Conflict resolution UI

### Phase 13 — Mobile Integration
- [ ] Tenant selection
- [ ] Device registration
- [ ] Sync status

### Phase 14 — Security and Accounting Tests
- [ ] Tenant isolation tests
- [ ] Local DB isolation tests
- [ ] Sync tests
- [ ] Offline tests
- [ ] Accounting verification

### Phase 15 — Final Audit
- [ ] Comprehensive audit
- [ ] Final reports

---

## 11. المهام المستقلة الآمنة (Safe Independent Tasks)

هذه المهام يمكن تنفيذها دون مخاطرة (additive only):

1. **Create discovery report** ✅ (this document)
2. **Create architecture document** (design only, no code changes)
3. **Create database design document** (design only, no migrations)
4. **Add TenantId column to one test table** (on isolated DB)
5. **Create sync_outbox table** (on isolated DB)
6. **Create tenants table** (on isolated DB)
7. **Create local_databases table** (on isolated DB)
8. **Create devices table** (on isolated DB)

### المهام التي تتطلب موافقة (Approval Required)

1. **Production migration** (add TenantId to 86 tables)
2. **API changes** (tenant validation middleware)
3. **Desktop UI changes** (tenant selection)
4. **Mobile changes** (tenant selection)
5. **Sync engine** (new endpoints)
6. **Offline authorization** (policy changes)
7. **Backup/restore changes** (encryption, metadata)
8. **Deployment** (Fly.io, Neon)

---

## 12. التكلفة والوقت (Estimated Effort)

| Phase | Duration | Risk | Dependencies |
|-------|----------|------|-------------|
| Phase 1 — Discovery | ✅ Done | Low | None |
| Phase 2 — Architecture | 1-2 days | Low | Phase 1 |
| Phase 3 — Database Design | 2-3 days | Medium | Phase 2 |
| Phase 4 — Tenant Isolation | 3-5 days | High | Phase 3 |
| Phase 5 — Local DB Isolation | 3-5 days | High | Phase 4 |
| Phase 6 — Authentication | 2-3 days | Medium | Phase 5 |
| Phase 7 — Authorization | 2-3 days | Medium | Phase 6 |
| Phase 8 — Device Management | 2-3 days | Medium | Phase 7 |
| Phase 9 — Backup/Recovery | 1-2 days | Low | Phase 8 |
| Phase 10 — Sync Engine | 5-7 days | High | Phase 9 |
| Phase 11 — Offline Auth | 2-3 days | Medium | Phase 10 |
| Phase 12 — Desktop UI | 3-5 days | Medium | Phase 11 |
| Phase 13 — Mobile Integration | 2-3 days | Medium | Phase 12 |
| Phase 14 — Security/Accounting Tests | 3-5 days | High | Phase 13 |
| Phase 15 — Final Audit | 1-2 days | Low | Phase 14 |

**Total estimated: 33-53 days** (depending on complexity and testing)

---

## 13. التوصية (Recommendation)

**لا تبدأ Phase 4 (Tenant Isolation) بدون موافقة صريحة.**

**ابدأ بـ Phase 2 (Architecture) — تصميم فقط، بدون تغييرات في الكود.**

Phase 2 output: `MULTI_TENANT_ARCHITECTURE.md` — تصميم مفصل للبنية المعمارية، without any code changes.

بعد الموافقة على Phase 2، انتقل إلى Phase 3 (Database Design) — تصميم المخطط (schema) والـ migrations، على بيئة اختبار (isolated DB).

لا تنفذ أي Migration على production (`neondb`) دون موافقة صريحة.

---

## 14. خلاصة Discovery

| البند | الحالة |
|-------|--------|
| Project structure | ✅ Understood |
| Database schema | ✅ 86 tables, 5 FKs, 0 TenantId columns |
| API | ✅ 18 controllers, JWT + permissions |
| Desktop | ✅ 43 views, 50 ViewModels, self-contained |
| Mobile | ✅ Flutter (not built locally) |
| Sync | ❌ Not implemented |
| Multi-tenant | ❌ Not implemented |
| Local DB isolation | ❌ Not implemented |
| Offline auth | ❌ Not implemented |
| Device management | ⚠️ Partial (user_devices only) |
| Backup/Restore | ✅ Implemented (no encryption/metadata) |

**الخلاصة:** النظام الحالي single-tenant/single-DB. Multi-tenant architecture هي ميزة جديدة تتطلب 33-53 يومًا من العمل، مقسمة على 15 مرحلة. Phase 1 (Discovery) مكتملة. Phase 2 (Architecture) هي التالية.

---
*Generated by OpenCode — Discovery Phase — 2026-11-15*
*Status: Discovery Complete · Architecture Pending Approval*
