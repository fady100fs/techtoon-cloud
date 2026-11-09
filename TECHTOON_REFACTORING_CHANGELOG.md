# TECHTOON — REFACTORING CHANGELOG

> **التاريخ:** 2026-11-09
> **الحالة:** Phase 1 (Audit) + Phase 2 P0/P1 — **مكتمل ومنشور**

---

## 0. ملاحظة

هذا الملف يوثّق **تغييرات Refactoring**. Phase 2 remediation في `TECHTOON_PHASE2_CHANGELOG.md`.

---

## Phase 1 — Audit (لا تغييرات)

- لم يتم تعديل أي ملف كود أو قاعدة بيانات.

---

## Phase 2 — التنفيذ (2026-11-09)

### [P0] — secrets removal (C1/P0-1)
- **المشكلة:** 41 ملف يحتوي `GetCS()` مع Neon connection string + password.
- **الحل:** استبدال بـ `SecureConnectionManager` (DPAPI)؛ حذف Fallbacks.
- **الملفات:** 41 C# + `Techtoon.Data` ×2 + `Techtoon.Streamlit\database.py`.
- **الاختبارات:** `Search: Host=ep-bitter in C# = 0`.
- **المخاطر/التراجع:** `flyctl releases rollback` + revert.

### [P0] — TLS fix (P0-2)
- **المشكلة:** `Trust Server Certificate=true`.
- **الحل:** إزالة من كل C# + Python generators.
- **الملفات:** 41 C# + 4 Python.

### [P0] — fallback removal (P0-5)
- **المشكلة:** Plaintext fallback في SecureManager + DbContexts.
- **الحل:** Env `TECHTOON_PG_CONNECTION` فقط.
- **الملفات:** `Techtoon.Data\TechtoonDbContext.cs`, `Context\TechtoonDbContext.cs`, `SecureConnectionManager.cs`.

### [P0] — decimal migration (P0-3)
- **المشكلة:** Money/quantity كـ `double`.
- **الحل:** Core (29) + API (6) + UI (7) `double`→`decimal`؛ SQL `deploy/migrations/P0-3_*`؛ backup `techtoon-prod-pre-p03-20261109.dump`؛ prod migration (0 double) + 5 views recreated.
- **الملفات:** 42 files + 3 migration SQL.
- **الاختبارات:** rehearsal (86→2)؛ prod verify (0).
- **المخاطر/التراجع:** restore backup dump / Neon branch.

### [P0] — data-access unification (P0-4 B1-2)
- **الحل:** `Techtoon.Core\Finance\BalanceCalculator`؛ `FinancialService` CloseShift داخل المعاملة؛ `CashBoxAppService` transfers.
- **الملفات:** `BalanceCalculator.cs`, `FinancialService.cs`, `CashBoxAppService.cs`.

### [P0] — performance (P0-4 B3-5)
- **الحل:** DataGrid virtualization عالمي (App.xaml)؛ N+1 batch (Items + BankRecon)؛ NotificationService leak.
- **الملفات:** `App.xaml`, `ItemsViewModel.cs`, `BankReconController.cs`, `NotificationService.cs`.

### [P0] — async (P0-4 B6-14)
- **الحل:** Items, Invoices, Payments, Dashboard, StockLevels, Reports, Financial (Balance/Transfer/Close/CashFlow).
- **الملفات:** 7 ViewModels + FinancialService.

### [P0] — pagination (P0-4 B10, B13)
- **الحل:** Items, Invoices, Payments, StockLevels → `page/pageSize=50` + `COUNT + LIMIT/OFFSET`.
- **الملفات:** 4 ViewModels.

### [P1] — accounting (P1.1-1.5)
- **الحل:** Party opening (API + Calculator)؛ PaymentEdit invoice_payments+reconcile؛ Return decimal+link؛ Stock in/out canonical؛ Avg cost weighted.
- **الملفات:** `PartyAppService.cs`, `BalanceCalculator.cs`, `PaymentEditWindow.xaml.cs`, `PaymentsViewModel.cs`, `ReturnInvoiceWindow.xaml.cs`, `InventoryService.cs`, `InvoiceAppService.cs`.

### [Deploy] — Fly v14
- **النتيجة:** health 200، login 200، items 200 (112)، parties 200 (3)، invoices 200.

---

> **Phase 2 مكتمل (P0 + P1 محدد). ما بقي P2/P3 + ~54 sync files + 106 in-progress.**
