# DATABASE_TENANT_CLASSIFICATION.md

> Phase 2 — Architecture Design (Design Only)
> Generated: 2026-11-15
> Based on: TECHTOON_DISCOVERY_REPORT.md + schema analysis of 86 base tables

---

## 1. Classification Categories

| # | Category | Description | Examples |
|---|----------|-------------|----------|
| 1 | **Tenant-Owned Data** | Data belonging to a specific tenant (company) | invoices, payments, parties, items |
| 2 | **Global Reference Data** | Shared reference data (currencies, categories) | currencies, categories, stock_reasons |
| 3 | **System Configuration** | App-wide settings, roles, permissions | roles, role_permissions, app_settings |
| 4 | **Local Device Data** | Data tied to a specific local DB/device | local_users, local_memberships, device_tenant_links |
| 5 | **Synchronization Data** | Sync metadata, outbox, conflicts, logs | sync_outbox, sync_conflicts, sync_log |
| 6 | **Audit Data** | Audit trail, logs, history | audit_log, app_audit_logs, login_attempts |

---

## 2. Table Classification (86 tables)

### 2.1 Tenant-Owned Data (60 tables)

These tables contain business data that belongs to a specific tenant. Each row must have `TenantId`.

| # | Table | Current Purpose | TenantId | LocalDatabaseId | Sync | FK Risk | Notes |
|---|-------|----------------|----------|-----------------|------|--------|-------|
| 1 | `invoices` | Sales/purchase invoices | ✅ Required | ✅ Required | ✅ Yes | High | Core financial record |
| 2 | `invoice_lines` | Invoice line items | ✅ Required | ✅ Required | ✅ Yes | High | Child of invoices |
| 3 | `invoice_payments` | Invoice payment records | ✅ Required | ✅ Required | ✅ Yes | High | Child of invoices |
| 4 | `invoice_discount_applications` | Discount applications | ✅ Required | ✅ Required | ✅ Yes | Medium | Child of invoices |
| 5 | `invoice_late_fees` | Late fee records | ✅ Required | ✅ Required | ✅ Yes | Medium | Child of invoices |
| 6 | `payments` | Receipt/disbursement vouchers | ✅ Required | ✅ Required | ✅ Yes | High | Financial record |
| 7 | `parties` | Customers/suppliers | ✅ Required | ✅ Required | ✅ Yes | High | Core entity |
| 8 | `items` | Products/items | ✅ Required | ✅ Required | ✅ Yes | High | Core entity |
| 9 | `accounts` | Chart of accounts | ✅ Required | ✅ Required | ✅ Yes | High | Core entity |
| 10 | `expenses` | Expense records | ✅ Required | ✅ Required | ✅ Yes | Medium | Financial record |
| 11 | `expense_categories` | Expense categories | ✅ Required | ✅ Required | ✅ Yes | Low | Reference per tenant |
| 12 | `cash_boxes` | Cash boxes/banks | ✅ Required | ✅ Required | ✅ Yes | High | Financial entity |
| 13 | `cash_transfers` | Cash box transfers | ✅ Required | ✅ Required | ✅ Yes | Medium | Financial record |
| 14 | `cash_transfer_log` | Transfer audit log | ✅ Required | ✅ Required | ✅ Yes | Low | Child of transfers |
| 15 | `journal_entries` | Accounting journal entries | ✅ Required | ✅ Required | ✅ Yes | High | Core financial |
| 16 | `journal_lines` | Journal entry lines | ✅ Required | ✅ Required | ✅ Yes | High | Child of entries |
| 17 | `cash_recognition_log` | Cash recognition log | ✅ Required | ✅ Required | ✅ Yes | Medium | Child of entries |
| 18 | `subscriptions` | Subscriptions | ✅ Required | ✅ Required | ✅ Yes | Medium | Financial record |
| 19 | `subscription_periods` | Subscription periods | ✅ Required | ✅ Required | ✅ Yes | Medium | Child of subscriptions |
| 20 | `loans` | Loans | ✅ Required | ✅ Required | ✅ Yes | High | Financial record |
| 21 | `loan_installments` | Loan installments | ✅ Required | ✅ Required | ✅ Yes | Medium | Child of loans |
| 22 | `loan_payments` | Loan payments | ✅ Required | ✅ Required | ✅ Yes | Medium | Child of loans |
| 23 | `loan_rescheduling_history` | Loan rescheduling | ✅ Required | ✅ Required | ✅ Yes | Low | Child of loans |
| 24 | `overdraft_transactions` | Overdraft transactions | ✅ Required | ✅ Required | ✅ Yes | Medium | Child of loans |
| 25 | `employees` | Employees | ✅ Required | ✅ Required | ✅ Yes | Medium | HR entity |
| 26 | `salary_records` | Salary records | ✅ Required | ✅ Required | ✅ Yes | Medium | Financial record |
| 27 | `petty_cash_custodies` | Petty cash custodies | ✅ Required | ✅ Required | ✅ Yes | Medium | Financial record |
| 28 | `petty_cash_expenses` | Petty cash expenses | ✅ Required | ✅ Required | ✅ Yes | Medium | Child of custodies |
| 29 | `petty_cash_settlements` | Petty cash settlements | ✅ Required | ✅ Required | ✅ Yes | Medium | Child of custodies |
| 30 | `custody_increase_requests` | Custody increase requests | ✅ Required | ✅ Required | ✅ Yes | Low | Child of custodies |
| 31 | `fixed_assets` | Fixed assets | ✅ Required | ✅ Required | ✅ Yes | Medium | Asset entity |
| 32 | `depreciation_records` | Depreciation records | ✅ Required | ✅ Required | ✅ Yes | Medium | Child of assets |
| 33 | `bad_debt_writeoffs` | Bad debt write-offs | ✅ Required | ✅ Required | ✅ Yes | Medium | Financial record |
| 34 | `bad_debt_reversals` | Bad debt reversals | ✅ Required | ✅ Required | ✅ Yes | Low | Child of writeoffs |
| 35 | `bad_debt_attachments` | Bad debt attachments | ✅ Required | ✅ Required | ✅ Yes | Low | Child of writeoffs |
| 36 | `recurring_invoice_templates` | Recurring invoice templates | ✅ Required | ✅ Required | ✅ Yes | Medium | Template entity |
| 37 | `recurring_invoice_lines` | Recurring invoice lines | ✅ Required | ✅ Required | ✅ Yes | Low | Child of templates |
| 38 | `recurring_journal_templates` | Recurring journal templates | ✅ Required | ✅ Required | ✅ Yes | Medium | Template entity |
| 39 | `recurring_journal_lines` | Recurring journal lines | ✅ Required | ✅ Required | ✅ Yes | Low | Child of templates |
| 40 | `recurring_journal_executions` | Recurring journal executions | ✅ Required | ✅ Required | ✅ Yes | Low | Child of templates |
| 41 | `warehouses` | Warehouses | ✅ Required | ✅ Required | ✅ Yes | Medium | Entity |
| 42 | `warehouse_transfers` | Warehouse transfers | ✅ Required | ✅ Required | ✅ Yes | Medium | Financial record |
| 43 | `warehouse_transfer_lines` | Transfer lines | ✅ Required | ✅ Required | ✅ Yes | Low | Child of transfers |
| 44 | `warehouse_transfer_log` | Transfer audit log | ✅ Required | ✅ Required | ✅ Yes | Low | Child of transfers |
| 45 | `stock_levels` | Stock levels per warehouse | ✅ Required | ✅ Required | ✅ Yes | Medium | Inventory record |
| 46 | `stock_adjustments` | Stock adjustments | ✅ Required | ✅ Required | ✅ Yes | Medium | Inventory record |
| 47 | `stock_count_sessions` | Stock count sessions | ✅ Required | ✅ Required | ✅ Yes | Medium | Inventory record |
| 48 | `stock_counts` | Stock count lines | ✅ Required | ✅ Required | ✅ Yes | Low | Child of sessions |
| 49 | `damaged_items` | Damaged items | ✅ Required | ✅ Required | ✅ Yes | Medium | Inventory record |
| 50 | `damaged_item_inspection_log` | Inspection log | ✅ Required | ✅ Required | ✅ Yes | Low | Child of damaged items |
| 51 | `writeoff_requests` | Write-off requests | ✅ Required | ✅ Required | ✅ Yes | Medium | Inventory record |
| 52 | `custody_increase_requests` | Custody increase requests | ✅ Required | ✅ Required | ✅ Yes | Low | Child of custodies |
| 53 | `cost_centers` | Cost centers | ✅ Required | ✅ Required | ✅ Yes | Medium | Entity |
| 54 | `cost_allocations` | Cost allocations | ✅ Required | ✅ Required | ✅ Yes | Low | Child of centers |
| 55 | `cost_center_transactions` | Cost center transactions | ✅ Required | ✅ Required | ✅ Yes | Low | Child of centers |
| 56 | `cost_history` | Cost history | ✅ Required | ✅ Required | ✅ Yes | Low | Child of items |
| 57 | `kit_components` | Kit components | ✅ Required | ✅ Required | ✅ Yes | Low | Child of items |
| 58 | `kit_component_history` | Kit component history | ✅ Required | ✅ Required | ✅ Yes | Low | Child of items |
| 59 | `accounting_periods` | Accounting periods | ✅ Required | ✅ Required | ✅ Yes | Medium | Entity |
| 60 | `budgets` | Budgets | ✅ Required | ✅ Required | ✅ Yes | Low | Child of periods |

### 2.2 Global Reference Data (8 tables)

Shared reference data that may be tenant-specific or global.

| # | Table | Current Purpose | TenantId | LocalDatabaseId | Sync | Notes |
|---|-------|----------------|----------|-----------------|------|-------|
| 61 | `currencies` | Currencies | ⚠️ Optional | ❌ No | ⚠️ Optional | May be global or per-tenant |
| 62 | `categories` | Item categories | ✅ Required | ❌ No | ✅ Yes | Per-tenant categories |
| 63 | `expense_categories` | Expense categories | ✅ Required | ❌ No | ✅ Yes | Per-tenant categories |
| 64 | `stock_reasons` | Stock adjustment reasons | ✅ Required | ❌ No | ✅ Yes | Per-tenant reasons |
| 65 | `late_fee_policies` | Late fee policies | ✅ Required | ❌ No | ✅ Yes | Per-tenant policies |
| 66 | `early_payment_discount_policies` | Early payment discount policies | ✅ Required | ❌ No | ✅ Yes | Per-tenant policies |
| 67 | `party_discount_policies` | Party discount policies | ✅ Required | ❌ No | ✅ Yes | Per-tenant policies |
| 68 | `party_late_fee_exceptions` | Party late fee exceptions | ✅ Required | ❌ No | ✅ Yes | Per-tenant exceptions |

### 2.3 System Configuration (6 tables)

App-wide settings, roles, permissions.

| # | Table | Current Purpose | TenantId | LocalDatabaseId | Sync | Notes |
|---|-------|----------------|----------|-----------------|------|-------|
| 69 | `roles` | Roles | ❌ No (global) | ❌ No | ❌ No | System roles (7) |
| 70 | `role_permissions` | Role permissions | ❌ No (global) | ❌ No | ❌ No | System permissions (88) |
| 71 | `users` | Cloud users | ❌ No (global) | ❌ No | ❌ No | Global identity |
| 72 | `user_permissions` | User permission overrides | ❌ No (global) | ❌ No | ❌ No | Global overrides |
| 73 | `app_settings` | App settings | ⚠️ Optional | ❌ No | ⚠️ Optional | May be per-tenant |
| 74 | `reporting_settings` | Reporting settings | ⚠️ Optional | ❌ No | ⚠️ Optional | May be per-tenant |

### 2.4 Local Device Data (4 tables)

Data tied to a specific local DB/device.

| # | Table | Current Purpose | TenantId | LocalDatabaseId | Sync | Notes |
|---|-------|----------------|----------|-----------------|------|-------|
| 75 | `user_devices` | Device tracking (refresh tokens) | ❌ No | ✅ Required | ❌ No | Local device only |
| 76 | `local_users` | Local users (NEW) | ❌ No | ✅ Required | ❌ No | Per-DB users |
| 77 | `local_memberships` | Local memberships (NEW) | ❌ No | ✅ Required | ❌ No | Per-DB memberships |
| 78 | `local_sessions` | Local sessions (NEW) | ❌ No | ✅ Required | ❌ No | Per-DB sessions |

### 2.5 Synchronization Data (4 tables — NEW)

Sync metadata, outbox, conflicts, logs.

| # | Table | Current Purpose | TenantId | LocalDatabaseId | Sync | Notes |
|---|-------|----------------|----------|-----------------|------|-------|
| 79 | `sync_outbox` | Outbox events (NEW) | ✅ Required | ✅ Required | ✅ Yes | Sync queue |
| 80 | `sync_conflicts` | Conflict records (NEW) | ✅ Required | ✅ Required | ✅ Yes | Conflict tracking |
| 81 | `sync_log` | Sync history (NEW) | ✅ Required | ✅ Required | ✅ Yes | Sync monitoring |
| 82 | `idempotency_keys` | Idempotency keys | ✅ Required | ✅ Required | ✅ Yes | Prevent duplicates |

### 2.6 Audit Data (4 tables)

Audit trail, logs, history.

| # | Table | Current Purpose | TenantId | LocalDatabaseId | Sync | Notes |
|---|-------|----------------|----------|-----------------|------|-------|
| 83 | `audit_log` | Audit log (legacy) | ✅ Required | ✅ Required | ✅ Yes | Per-tenant audit |
| 84 | `app_audit_logs` | App audit logs | ✅ Required | ✅ Required | ✅ Yes | Per-tenant audit |
| 85 | `login_attempts` | Login attempts | ✅ Required | ✅ Required | ✅ Yes | Per-tenant attempts |
| 86 | `audit_trail` | Enhanced audit (NEW) | ✅ Required | ✅ Required | ✅ Yes | Full audit trail |

---

## 3. Summary

| Category | Count | TenantId | LocalDatabaseId | Sync |
|----------|-------|----------|-----------------|------|
| Tenant-Owned Data | 60 | ✅ Yes | ✅ Yes | ✅ Yes |
| Global Reference Data | 8 | ⚠️ Optional | ❌ No | ⚠️ Optional |
| System Configuration | 6 | ❌ No | ❌ No | ❌ No |
| Local Device Data | 4 | ❌ No | ✅ Yes | ❌ No |
| Synchronization Data | 4 | ✅ Yes | ✅ Yes | ✅ Yes |
| Audit Data | 4 | ✅ Yes | ✅ Yes | ✅ Yes |
| **Total** | **86** | | | |

### Key Observations

1. **60 of 86 tables** (70%) are tenant-owned — require TenantId
2. **8 tables** are global reference data — TenantId optional (may be per-tenant or shared)
3. **6 tables** are system configuration — no TenantId (global)
4. **4 tables** are local device data — LocalDatabaseId only
5. **4 tables** are sync data — TenantId + LocalDatabaseId
6. **4 tables** are audit data — TenantId + LocalDatabaseId

### Risk Assessment

| Risk | Level | Mitigation |
|------|-------|-----------|
| 60 tables need TenantId | High | Additive migration, backfill default tenant |
| 8 tables may be shared/global | Medium | Decide per-table (global vs per-tenant) |
| 5 FK constraints only | High | Add FK constraints for TenantId |
| App-level integrity | High | Add DB-level constraints |
| No RLS policies | High | Implement RLS on all tenant-owned tables |

---
*Generated by OpenCode — Phase 2 Architecture Design — 2026-11-15*
