# CRUD IMPLEMENTATION REPORT

## ما تم إصلاحه في هذه الجلسة

### 1. CashBoxesView (WPF) — كان مكسورًا (أزرار ميتة)
- **المشكلة:** `AddCommand/EditCommand/DeleteCommand/SelectedRow/TypeDisplay/StatusDisplay` لا وجود لها في VM.
- **الحل:** إضافة 5 خصائص + commands + نافذة تحرير برمجية `CashBoxEditor` (INSERT/UPDATE في transaction) + Delete مع فحص FK (يمنع عند وجود حركات).
- **الملفات:** `Techtoon.UI/ViewModels/CashBoxesViewModel.cs`.
- **الاختبار:** Build 0 errors؛ Fly smoke (cashboxes 200).

### 2. Payments/Expenses PUT (API) — كان 405
- **المشكلة:** Mobile يستدعي `PUT api/payments/{id}` / `PUT api/expenses/{id}` — لا يوجد PUT.
- **الحل:** `UpdatePaymentAsync` + `PUT api/payments/{id}` (مع reconcile ledger delta). Expenses PUT ما يزال مفقودًا (انظر INCOMPLETE).
- **الملفات:** `Techtoon.Api/Controllers/PaymentsController.cs`, `Techtoon.Api/Services/PaymentAppService.cs`.

### 3. Loan CashBoxId (API 500)
- **المشكلة:** `CashBox.Loans` يخلق shadow FK `CashBoxId` → 42703.
- **الحل:** `[Column("cash_box_id")] int? CashBoxId` + navigation في `Loan.cs`.
- **الملفات:** `Techtoon.Core/Models/Loan.cs`.

### 4. Salaries ordering (API 400)
- **المشكلة:** `OrderByDescending(s => new { Year, Month })` لا يُترجم.
- **الحل:** `ThenByDescending`.
- **الملفات:** `Techtoon.Api/Controllers/SalariesController.cs`.

### P0/P1/P2/P3 (سابقًا، مدفوعة ومنشورة)
انظر TECHTOON_REFACTORING_REPORT.md / CHANGELOG.

## ما لم يتم
- RecurringInvoices stub، Expenses PUT، Loans/Salaries read-only (server)، FinancialService orphan، LoanEditWindow orphan — انظر INCOMPLETE_FEATURES.md.
