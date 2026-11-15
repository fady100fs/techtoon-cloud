# ACCOUNTING_INTEGRITY_DESIGN.md

> Phase 2 — Architecture Design (Design Only)
> Generated: 2026-11-15

---

## 1. Accounting Integrity Principles

### 1.1 Core Principle

**Every financial operation must maintain the integrity of all related records.**

No partial updates. No orphaned records. No unbalanced entries. All operations are atomic (transactional).

### 1.2 Transactional Integrity

Every financial operation creates/updates multiple records in a single transaction:
- Invoice → invoice + lines + inventory movement + journal entry + party balance
- Payment → payment + invoice_payment + journal entry + party balance + cash box balance
- Return → reverse invoice + reverse inventory + reverse journal + reverse balances
- Transfer → update source + destination + journal entry

### 1.3 Multi-Tenant Impact

Adding `TenantId` to all tables must NOT:
- Change any accounting formula
- Duplicate any balance
- Create orphaned records
- Break any relationship
- Alter any calculation

The accounting logic remains identical — only the filtering changes (add `WHERE tenant_id = @tenantId`).

---

## 2. Affected Operations

### 2.1 Sales Invoices

| Record | Table | Effect | Tenant-Scoped |
|--------|-------|--------|---------------|
| Invoice | `invoices` | Created | ✅ Yes |
| Invoice lines | `invoice_lines` | Created | ✅ Yes |
| Inventory movement | `inventory_movements` | Decrease stock | ✅ Yes |
| Journal entry | `journal_entries` | Debit AR, Credit Revenue | ✅ Yes |
| Journal lines | `journal_lines` | Debit/Credit accounts | ✅ Yes |
| Party balance | `parties` | Increase receivable | ✅ Yes |
| Cash box (if paid) | `cash_boxes` | Increase balance | ✅ Yes |
| COGS entry | `journal_entries` | Debit COGS, Credit Inventory | ✅ Yes |

### 2.2 Purchase Invoices

| Record | Table | Effect | Tenant-Scoped |
|--------|-------|--------|---------------|
| Invoice | `invoices` | Created | ✅ Yes |
| Invoice lines | `invoice_lines` | Created | ✅ Yes |
| Inventory movement | `inventory_movements` | Increase stock | ✅ Yes |
| Journal entry | `journal_entries` | Debit Inventory, Credit AP | ✅ Yes |
| Party balance | `parties` | Increase payable | ✅ Yes |
| Cash box (if paid) | `cash_boxes` | Decrease balance | ✅ Yes |

### 2.3 Receipts (سند قبض)

| Record | Table | Effect | Tenant-Scoped |
|--------|-------|--------|---------------|
| Payment | `payments` | Created | ✅ Yes |
| Invoice payment | `invoice_payments` | Created | ✅ Yes |
| Party balance | `parties` | Decrease receivable | ✅ Yes |
| Cash box | `cash_boxes` | Increase balance | ✅ Yes |
| Journal entry | `journal_entries` | Debit Cash, Credit AR | ✅ Yes |

### 2.4 Disbursements (سند صرف)

| Record | Table | Effect | Tenant-Scoped |
|--------|-------|--------|---------------|
| Payment | `payments` | Created | ✅ Yes |
| Party balance | `parties` | Decrease payable | ✅ Yes |
| Cash box | `cash_boxes` | Decrease balance | ✅ Yes |
| Journal entry | `journal_entries` | Debit AP, Credit Cash | ✅ Yes |

### 2.5 Returns (مرتجعات)

| Record | Table | Effect | Tenant-Scoped |
|--------|-------|--------|---------------|
| Return invoice | `invoices` | Created (type=return) | ✅ Yes |
| Inventory movement | `inventory_movements` | Reverse original | ✅ Yes |
| Journal entry | `journal_entries` | Reverse original | ✅ Yes |
| Party balance | `parties` | Reverse original | ✅ Yes |
| Cash box | `cash_boxes` | Reverse original | ✅ Yes |
| COGS entry | `journal_entries` | Reverse original | ✅ Yes |

### 2.6 Transfers (تحويلات)

| Record | Table | Effect | Tenant-Scoped |
|--------|-------|--------|---------------|
| Cash transfer | `cash_transfers` | Created | ✅ Yes |
| Source cash box | `cash_boxes` | Decrease balance | ✅ Yes |
| Destination cash box | `cash_boxes` | Increase balance | ✅ Yes |
| Journal entry | `journal_entries` | Debit dest, Credit source | ✅ Yes |
| Warehouse transfer | `warehouse_transfers` | Created | ✅ Yes |
| Source warehouse | `stock_levels` | Decrease stock | ✅ Yes |
| Destination warehouse | `stock_levels` | Increase stock | ✅ Yes |

**Key rule:** Transfers are NOT recorded as revenue or expense.

### 2.7 Journal Entries

| Record | Table | Effect | Tenant-Scoped |
|--------|-------|--------|---------------|
| Journal entry | `journal_entries` | Created | ✅ Yes |
| Journal lines | `journal_lines` | Debit/Credit | ✅ Yes |
| Balance check | — | Debits = Credits | ✅ Yes |

---

## 3. Accounting Formulas (Unchanged)

```
PartyBalance = Opening + Receipts - Payments + Sales - Purchases
CashBoxBalance = Opening + Receipts - Payments - Expenses + TransfersIn - TransfersOut
InvoiceDue = Total - Paid (never negative)
```

These formulas remain **identical** after multi-tenant migration. Only the filtering changes (add `WHERE tenant_id = @tenantId`).

---

## 4. Data Integrity Rules

### 4.1 Balance Integrity

- Sum of debits = Sum of credits (per tenant)
- Party balance = sum of invoices + payments (per tenant)
- Cash box balance = sum of transactions (per tenant)
- Stock level = sum of movements (per tenant)

### 4.2 Cross-Tenant Integrity

- No invoice from tenant A references party from tenant B
- No payment from tenant A references invoice from tenant B
- No journal entry from tenant A references account from tenant B
- No stock movement from tenant A references item from tenant B

### 4.3 Sync Integrity

- No duplicate invoices after sync
- No duplicate payments after sync
- No orphaned records after sync
- No balance discrepancies after sync
- No stock level discrepancies after sync

---

## 5. Multi-Tenant Impact on Accounting

### 5.1 What Changes

| Aspect | Before (Single-Tenant) | After (Multi-Tenant) |
|--------|----------------------|---------------------|
| Query filtering | No tenant filter | `WHERE tenant_id = @tenantId` |
| Balance calculation | Global | Per-tenant |
| Stock levels | Global | Per-tenant |
| Invoice numbering | Global sequence | Per-tenant sequence (or global with tenant prefix) |
| Journal entries | Global | Per-tenant |
| Reports | Global | Per-tenant |
| Audit trail | Global | Per-tenant |

### 5.2 What Does NOT Change

| Aspect | Unchanged |
|--------|-----------|
| Accounting formulas | PartyBalance, CashBoxBalance, InvoiceDue |
| Transactional integrity | All operations in single transaction |
| Double-entry | Debits = Credits |
| Inventory sign convention | Sales negative, purchases positive |
| Return processing | Reverse original effects |
| Transfer processing | Source - destination, no double-count |
| COGS calculation | Based on inventory movements |
| Tax calculation | Per invoice, per tenant |

---

## 6. Sync Integrity

### 6.1 Financial Record Sync

| Record | Sync Strategy | Conflict Resolution |
|--------|--------------|---------------------|
| Invoice | Push/Pull | Manual review |
| Payment | Push/Pull | Manual review |
| Journal Entry | Push/Pull | Manual review |
| Party Balance | Derived (not synced directly) | Recalculated from invoices/payments |
| Cash Box Balance | Derived (not synced directly) | Recalculated from transactions |
| Stock Level | Push/Pull | Sum (additive) |

### 6.2 Conflict Detection

- If local version > cloud version: apply local change
- If cloud version > local version: apply cloud change
- If both changed: conflict → manual review
- Financial records: always manual review
- Reference data: LWW (with audit)

### 6.3 Preventing Data Loss

1. All changes versioned (Version column)
2. All changes in outbox (sync_outbox table)
3. Sync is incremental (only changed records)
4. Conflicts detected before applying
5. Manual review for financial records
6. Audit trail records all decisions

---

## 7. خلاصة

### Accounting Integrity Design

- **Formulas**: Unchanged (PartyBalance, CashBoxBalance, InvoiceDue)
- **Transactional integrity**: Unchanged (all operations in single transaction)
- **Double-entry**: Unchanged (debits = credits)
- **Inventory sign convention**: Unchanged (sales negative, purchases positive)
- **Multi-tenant impact**: Only filtering changes (add `WHERE tenant_id = @tenantId`)
- **Sync integrity**: Versioning + conflict resolution (manual for financial)
- **Data safety**: Additive only, no data loss, rollback plan

### Key Principle

> **Multi-tenant migration changes HOW data is filtered, not HOW it's calculated.**

All accounting formulas, transactional rules, and integrity checks remain identical. Only the `WHERE tenant_id = @tenantId` clause is added to queries.

---
*Generated by OpenCode — Phase 2 Architecture Design — 2026-11-15*
