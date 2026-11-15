# TECHTOON — FINAL FIXES

> جولة 2026-11-15 · OpenCode (GLM 5.3)

---

## 1. إصلاحات P0 (حرجة — أُصلحت)

### 1.1 API دخول (42P10)
- **الملف:** `Techtoon.Api/Services/AuthAppService.cs`
- **الخلل:** `ON CONFLICT (user_id, device_id)` بدون قيد فريد ⇒ كل دخول يفشل
- **الإصلاح:** UPDATE ثم INSERT (لا حاجة لتعديل المخطط)
- **الحالة:** ✅ PASS

### 1.2 API صلاحيات (403 على ~28 نقطة)
- **الملفات:** `Techtoon.Api/Security/PermissionHandler.cs`, `AuthAppService.cs`
- **الخلل:** مطابقة تامة للمفتاح + `LoadPermissionsAsync` لا يقرأ `role_permissions`
- **الإصلاح:** حلّ التطابق عبر `LegacyAliases`/`ReverseAliases` + تحميل الدور + الاستثناءات
- **الحالة:** ✅ PASS

### 1.3 Streamlit (SyntaxError — الصفحة لا تُستورد)
- **الملفات:** `Techtoon.Streamlit/app.py`, `utils/math_eval.py`, `database.py`
- **الإصلاح:** `@@st.cache_data` → `@st.cache_data`, إغلاق نص، إصلاح بايت، bcrypt only, DSN بصيغتين
- **الحالة:** ✅ PASS (34/34 checks)

### 1.4 Flutter (8 شاشات تُعرض نصًا حرفيًا)
- **الملفات:** 8 شاشات Dart (`\$` → `${}`), `user.dart`, `expense_service.dart`, `loans_service.dart`, `bank_reconciliation_service.dart`
- **الإصلاح:** استبدال حقيقي + cashBoxId + expense PUT + field names + error surfacing
- **الحالة:** ⚠️ NOT VERIFIED (no Flutter SDK)

### 1.5 WPF (زر تعديل الفواتير، ترقيم، etc.)
- **الملفات:** `InvoicesView.xaml`, `ItemsView.xaml`, `PaymentsView.xaml`, `StockLevelsView.xaml`, `AuditLogView.xaml`, `InvoiceEditViewModel.cs`
- **الإصلاح:** زر «تعديل» + شريط ترقيم (5 صفحات)
- **الحالة:** ✅ PASS

### 1.6 Payment edit (كان يُنشئ تكرارًا)
- **الملفات:** `PaymentEditWindow.xaml.cs`, `PaymentsViewModel.cs`
- **الإصلاح:** `LoadForEdit` + UPDATE path + `invoice_payments` update (no duplicate)
- **الحالة:** ✅ PASS (2/2 tests)

### 1.7 Bundle import (NOT NULL FKs)
- **الملف:** `Techtoon.UI/Services/MasterDataBundleService.cs`
- **الإصلاح:** `DEFAULT-PARTY` account (ASSET) + first currency fallback + file-locked read (4 retries)
- **الحالة:** ✅ PASS (real-file test: 364 parties, 0 errors)

### 1.8 Data Management (حذف كامل/جزئي)
- **الملفات:** `DataManagementService.cs` (rewritten), `DataManagementView.xaml`, `DataManagementViewModel.cs`, `ErrorMessages.cs`
- **الإصلاح:** 25 groups covering 86 tables + partial scopes + transactional + integrity sweep
- **الحالة:** ✅ PASS (9/9 tests)

### 1.9 Permissions enforcement (delete/edit buttons)
- **الملفات:** 29 ViewModels (`SubscriptionsViewModel.cs`, `PartiesViewModel.cs`, ...)
- **الإصلاح:** `DeleteCommand.CanExecute` gated by `AuthService.HasPermission("<module>.delete/edit")`
- **الحالة:** ✅ PASS (build + manual verification)

### 1.10 Delete-party error (FK violation)
- **الملف:** `PartiesViewModel.cs`
- **الإصلاح:** `CountReferencesAsync` + `ErrorMessages.Describe` (Arabic message)
- **الحالة:** ✅ PASS

---

## 2. إصلاحات P1 (متوسطة — أُصلحت)

| # | المشكلة | الملف | الحالة |
|---|---------|-------|--------|
| 1 | `InventoryService` "sale" → "sales" | `InventoryService.cs` | ✅ |
| 2 | `cashBoxId` binding | `PaymentsViewModel.cs` | ✅ |
| 3 | `loan_name` column | `loan_service.dart` | ✅ |
| 4 | `user.dart` parse error | `user.dart` | ✅ |
| 5 | Error surfacing (4 screens) | 4 ViewModels | ✅ |
| 6 | Reports screen (Flutter) | `reports_screen.dart` | ✅ |
| 7 | Settings health probe | `settings_screen.dart` | ✅ |
| 8 | `cash_recognition_log` nullability | `DataManagementService.cs` | ✅ |
| 9 | `cost_history.invoice_id` orphan | `DataManagementService.cs` | ✅ |
| 10 | `bank_statement_lines.matched_payment_id` orphan | `DataManagementService.cs` | ✅ |

---

## 3. إصلاحات P2 (تحسينات — أُصلحت)

| # | المشكلة | الملف | الحالة |
|---|---------|-------|--------|
| 1 | `AppPaths` writable fallback | `AppPaths.cs` | ✅ |
| 2 | Backup/restore (constraints, sequences, transaction) | `BackupService.cs` | ✅ |
| 3 | Auto-backup/log paths | `App.xaml.cs` | ✅ |
| 4 | Double VM init | `MainViewModel.cs` | ✅ |
| 5 | Salaries/parties search | `SalariesViewModel.cs`, `PartiesViewModel.cs` | ✅ |
| 6 | Dependency windows | `MainViewModel.cs` | ✅ |
| 7 | Loan close | `LoansViewModel.cs` | ✅ |
| 8 | License tab | `LicenseSettingsViewModel.cs` | ✅ |
| 9 | Math eval SyntaxError | `math_eval.py` | ✅ |
| 10 | Invalid UTF-8 | `app.py` | ✅ |
| 11 | Plaintext password fallback | `auth_service.py` | ✅ |
| 12 | DSN formats | `database.py` | ✅ |
| 13 | File-locked read (4 retries) | `MasterDataBundleService.cs` | ✅ |
| 14 | Error messages Arabic | `ErrorMessages.cs` + 23 VMs | ✅ |

---

## 4. إصلاحات قاعدة البيانات (DB)

| # | المشكلة | الإصلاح | الحالة |
|---|---------|---------|--------|
| 1 | `subscriptions.party_id` FK RESTRICT | Pre-check in `PartiesViewModel` + Data Management sweep | ✅ |
| 2 | `parties.account_id` NOT NULL | `DEFAULT-PARTY` account (ASSET) | ✅ |
| 3 | `payments.currency_id` NOT NULL | First currency fallback | ✅ |
| 4 | `expenses.category_id`/`cash_box_id` NOT NULL | Schema-aware nullability | ✅ |
| 5 | Sequences out of sync | Resync to `MAX(id)` (not `RESTART WITH 1`) | ✅ |
| 6 | `user_devices` unique constraint | UPDATE+INSERT (no migration) | ✅ |

---

## 5. Migration pending (needs approval)

| # | Migration | السبب |
|---|-----------|-------|
| 1 | `parties.email` / `tax_number` columns | Production migration |
| 2 | `user_devices(user_id, device_id)` unique constraint | Production migration |

---

## 6. ملفات معدلة (Modified files) — 38+

**Core:**
- `Techtoon.Core/Permissions/PermissionCatalog.cs`
- `Techtoon.Core/Permissions/RoleCatalog.cs`
- `Techtoon.Core/Bundles/BundleCatalog.cs`

**UI:**
- `Techtoon.UI/Services/DataManagementService.cs` (rewritten)
- `Techtoon.UI/Services/ErrorMessages.cs` (new)
- `Techtoon.UI/Services/MasterDataBundleService.cs`
- `Techtoon.UI/Services/RoleService.cs`
- `Techtoon.UI/Services/SecureConnectionManager.cs`
- `Techtoon.UI/ViewModels/DataManagementViewModel.cs` (rewritten)
- `Techtoon.UI/ViewModels/PartiesViewModel.cs`
- `Techtoon.UI/ViewModels/SubscriptionsViewModel.cs`
- `Techtoon.UI/ViewModels/*.cs` (23 files — error messages)
- `Techtoon.UI/Views/DataManagementView.xaml` (rewritten)

**Tests:**
- `Techtoon.Tests/DataManagementTests.cs` (new, 9 tests)
- `Techtoon.Tests/DataManagementViewModelTests.cs` (new, 4 tests)
- `Techtoon.Tests/BundleSubscriptionsTests.cs`
- `Techtoon.Tests/PaymentEditTests.cs`
- `Techtoon.Tests/DatabaseTestCleanupFixture.cs`

**API:**
- `Techtoon.Api/Security/PermissionHandler.cs`
- `Techtoon.Api/Services/AuthAppService.cs`
- `Techtoon.Api/Controllers/ExpensesController.cs`

**Streamlit:**
- `Techtoon.Streamlit/app.py`, `database.py`, `auth_service.py`, `utils/math_eval.py`

**Flutter:**
- 8 screens, 4 services, `user.dart`

---

## 7. Commits

| Commit | Description |
|--------|-------------|
| `e022b7a` | chore: remove debug test |
| `e48af38` | feat: gate every delete/edit/add command by permission |
| `d32eca1` | fix: enforce subscriptions.edit permission |
| `5a6037f` | feat: complete data-management feature + delete-party fix + error messages |
| `a1a9df2` | fix: read import file even when Excel/drive holds it |
| `f8f0a98` | fix: composite import/export verified against real production export |
| `529c8a3` | fix: writable log/backup paths, ship rebuilt installer |
| `2c94d0a` | feat: full-system verification cycle |
| `1ff1c96` | feat: permissions system |
| `52cd55e` | feat: real roles and permissions |

---
*Generated by OpenCode audit — 2026-11-15*
