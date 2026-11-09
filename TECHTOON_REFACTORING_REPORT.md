# TECHTOON — REFACTORING REPORT (Phase 1: Audit)

> **المشروع:** Techtoon Accounting
> **المسار:** `D:\PROG\Accounting_Prog\Techtoon_Accounting_C+`
> **التاريخ:** 2026-11-09
> **المرحلة:** Phase 1 — Audit (بدون تعديلات)
> **الحالة:** تقرير تدقيقي (Audit) فقط. **لم يتم تنفيذ أي Refactoring** — في انتظار موافقتك على الخطة (المرحلة 2).

---

## 1. الحالة الأصلية للمشروع (As-Is)

### 1.1 المكونات
- **Techtoon.Core** (net10.0): ~38 ملف، ~2,086 LOC — Entities/Enums/Inventory helpers.
- **Techtoon.Data** (net10.0): 2 ملف، ~193 LOC — EF Core DbContext (two parallel contexts).
- **Techtoon.Api** (net10.0): 54 ملف، ~5,675 LOC — ASP.NET Core API (controllers, services, middleware).
- **Techtoon.UI** (net10.0-windows, WPF): 199 ملف، ~22,855 LOC — Desktop app.
- **Techtoon.Mobile** (Flutter 3.13): 21 ملف، ~2,491 LOC — PWA/Mobile (default API base URL `https://techtoon-api.fly.dev`).
- **Techtoon.Streamlit** (Python): 16 ملف، ~1,269 LOC — legacy web app.
- **Root scripts**: 139 Python files (~40,659 LOC) + PS1 + migrations.
- **Backup/Old_Projects**: historical copies (excluded from main audit).

### 1.2 التبعيات (بدون دورات)
`Core ← Data ← Api/UI`; `Mobile` و `Streamlit` مستقلان (يتصلان بـ Neon مباشرة/عبر API).

### 1.3 الحالة (Git)
- 4 commits pushed: `cd8733d` (Phase 2), `a78c686` (login fix), `3300637` (deploy), `b7aaf1b` (cache-bust), `d1d8f24` (cleanup), `34387bc` (.gitignore).
- 106 ملف in-progress (WPF features) محليًا (skip-worktree/.gitignore).
- Production: Fly `techtoon-mobile` v10 (PWA + API), Neon DB (86 جدول).

### 1.4 الاختبارات
- **لا توجد C# test projects** (xUnit/NUnit/MSTest). Mobile has stale boilerplate `widget_test.dart`.
- 12 Python test scripts (`test_*.py`) + `phase4_accounting_tests.py` + `phase2_api_test.ps1` (25 checks, passed).
- Build: `dotnet build Techtoon.sln` → 0 errors (11 warnings nullable).

---

## 2. أكبر مشكلات تنظيم الكود (Architecture)

### 2.1 طبقتان متوازيتان لوصول البيانات (Critical)
- **EF Core** (API controllers + 6 controllers) و **Raw Npgsql** (API services + ~60 UI files).
- `Techtoon.Data\TechtoonDbContext.cs` (root stub, localhost fallback) و `Techtoon.Data\Context\TechtoonDbContext.cs` (Neon hardcoded) — تضارب.
- 40 ملف UI يحتوي `GetCS()` hardcoded Neon connection string مع password.

### 2.2 مسؤوليات متعددة (God classes)
- `Techtoon.UI\Views\InvoiceEditViewModel.cs` (584 سطر): SQL + business logic + UI state.
- `Techtoon.UI\Services\AuthService.cs` (584 سطر): auth + permissions + audit + admin seeding.
- `Techtoon.UI\Services\PrintService.cs` (473), `InventoryCountService.cs` (472), `ReturnInvoiceWindow.xaml.cs` (422).
- Code-behind (22 ملف) يحتوي SQL + business logic (edit windows, PartyStatement, ReturnInvoice, InvoiceEditViewModel).

### 2.3 تكرار (Duplicate Code)
- `GetCS()` hardcoded connection string في ~40 ملف.
- SQL sign mapping for inventory (in/out/adjustment) مكرر في API `InventorySignConvention`, UI `ReturnInvoiceWindow.GetCurrentStock`, `InventoryService`.
- Balance formulas مكررة/متباينة: UI `FinancialService.GetCashBoxBalance` (opening+receipts−payments−expenses−transfers) vs API `CashBoxAppService.BuildBalanceExpr` (opening+receipts−payments−expenses, **missing transfers**).
- `Party.CurrentBalance` (API) = purchases only; UI may differ.

### 2.4 تبعيات دائرية
- لا توجد دورات csproj (graph نظيف). ✅

### 2.5 مسؤوليات غير مفصولة
- `Techtoon.Core` يحتوي Models (entities) + Inventory helper — OK.
- `Techtoon.Data` لا يحتوي repositories/services — EF context فقط.
- Business logic موزعة بين Core (rules) و UI (SQL) و API (services).

---

## 3. التكرار (Duplication) — مفصل

| التكرار | المواقع | التأثير |
|---|---|---|
| Hardcoded Neon conn string + password | 41 ملف (1 Data + 40 UI) | C1 secret leak; drift |
| Inventory sign mapping | `InventorySignConvention.cs`, `ReturnInvoiceWindow.xaml.cs`, `InventoryService.cs` | stock mismatch |
| Cash box balance formula | `FinancialService.cs`, `CashBoxAppService.cs`, `CashBoxesViewModel.cs` | discrepancy |
| Paid-amount reconciliation | `PaymentAppService.ReconcileInvoiceAsync`, `PaymentsViewModel.ReconcileInvoicePaid` | drift |
| Payment DTOs / validation | API DTOs + controller-nested DTOs | inconsistent validation |
| Error handling middleware vs idempotency | `ErrorHandlingMiddleware`, `IdempotencyMiddleware.WriteProblemAsync` | response shape mismatch |
| Connection manager | `SecureConnectionManager` (DPAPI) + hardcoded default | fallback leaks secret |

---

## 4. نقاط ضعف الأداء

### 4.1 قاعدة البيانات
- **WPF 100% synchronous DB** (215 `.Open()`, 149 `ExecuteReader()`, 151 `ExecuteNonQuery()`, 59 `ExecuteScalar()`, 0 async) ⇒ UI thread blocked on every query.
- **N+1 queries**: `ItemsViewModel` deletes/updates per item in loop; `InvoicesViewModel` deletes per invoice; `InvoiceEditViewModel` inserts per line/payment; `BankReconciliationController.Approve` calls `SaveChangesAsync` inside foreach.
- **Pagination**: UI uses `LIMIT` only (no OFFSET); API uses `LIMIT/OFFSET`/`Skip/Take`. No consistent pagination contract.
- **No indexes beyond 11 EF-defined + ~109 SQL migration indexes** (some duplicates across migration files).
- **No FK delete-rule consistency**: mix of CASCADE/RESTRICT/NO ACTION/SET NULL (19/37/59/5).

### 4.2 WPF UI
- **No DataGrid virtualization**: 33 files, 322 DataGrid tags, 0 `VirtualizingStackPanel`/`EnableRowVirtualization` ⇒ large collections render fully.
- **Memory leaks**: 379 event `+=` vs 4 `-=`; `NotificationService` holds static UI elements; `AuthService._permissions` static (stale across user switch); 0 `IDisposable`.
- **No in-memory caching**; repeated identical queries on every refresh.
- **God classes** + code-behind SQL.

### 4.3 PWA/API
- Mobile calls endpoints that **don't exist** in API: `api/employees`, `api/loans`, `api/salaries`, `api/warehouses` (404 in production). API has `api/kits` which mobile doesn't call.
- `main.dart.js` hardcoded default base URL `https://techtoon-api.fly.dev` (now patched in deploy).
- Mobile tokens stored in memory only (no secure storage).
- No offline handling / sync.

---

## 5. الأخطاء والمشكلات التقنية

### 5.1 Build / Warnings
- Build succeeds: 0 errors, 11 warnings (nullable dereferences in `BankReconciliationController`, unused fields in `PaymentEditWindow`).

### 5.2 Error handling
- `ErrorHandlingMiddleware` returns `{ status, message, traceId }` (not RFC 7807 ProblemDetails). `ErrorResponseDto` unused. Idempotency returns `{ message }` only ⇒ shape mismatch.
- Controllers mix `BadRequest(new { message })`, `NotFound()` (empty body), `BadRequest(ex.Message)` (leaks exception text).
- Empty `catch { }` in some UI paths (rollback best-effort acceptable; stock-error swallowing in older code).

### 5.3 Async misuse
- `async void` in `App.xaml.cs:26`, `InvoiceEditWindow.xaml.cs:26`.
- 0 `ConfigureAwait`; `MainViewModel.LoginAsync` calls sync `AuthService.Login` with no await ("fake async").
- No `Task.Run` around heavy work except 2 spots.

### 5.4 Concurrency
- No `rowversion`/optimistic concurrency tokens anywhere.
- Only cash transfers + invoice numbering are locked; payments/invoices/stock levels unprotected.

### 5.5 Idempotency
- API only (`IdempotencyMiddleware` + `idempotency_keys` table, 24h TTL). No idempotency in WPF.

---

## 6. المخاطر المحاسبية والمخزنية (الأهم)

### 6.1 Money as `double`
- All monetary fields in `Techtoon.Core/Models/*.cs` are `double` (mapped to PG `double precision`). Reads cast to `::numeric`/`decimal`, but storage loses precision. Risk: rounding drift in reports.

### 6.2 Party balance definition
- API `PartyAppService` computes `current_balance` = SUM(purchase invoices) only; receivables (sales) not netted. OpeningBalance stored separately. UI may differ.

### 6.3 Cash box balance
- UI: opening + receipts − payments − expenses − transfers.
- API: opening + receipts − payments − expenses (**omits transfers**).
- `CashBoxesViewModel`: opening + receipts − payments (omits expenses + transfers).
- ⇒ Three different formulas for the same concept.

### 6.4 Return invoices
- UI-only (no API endpoint). Updates stock_levels + movements, but **does not** update original invoice `paid_amount`, does not reverse payments, does not handle cancel of a return.

### 6.5 Payment editing
- `PaymentEditWindow.xaml.cs` does not update `invoice_payments` or `invoices.paid_amount` and does not reconcile ⇒ edited payments leave stale balances.

### 6.6 Payment deletion (UI)
- `PaymentsViewModel` deletes from `payments` but does **not** delete matching `invoice_payments` row ⇒ reconciliation (prefers `invoice_payments`) leaves paid amount unchanged.

### 6.7 Cost/COGS
- `avg_cost_price` is set once (`ItemEditWindow`) and never recalculated on purchases. `total_purchased_qty` not maintained. COGS relies on last-set cost ⇒ inaccurate gross profit.

### 6.8 Journal entries
- Created only in API advanced controllers (BadDebt, BankReconciliation, PettyCash, RecurringJournal). Invoice/payment paths do not generate journal entries. `BadDebtController.Reverse` creates reversal; `InvoiceAppService.CancelInvoiceAsync` does not (invoices don't generate entries anyway). Deleted invoices leave no trail.

### 6.9 Cancelled/deleted invoices
- `CancelInvoiceAsync`: flips movement types (`out`↔`in`), normalizes via `ABS`, sets status cancelled. `DeleteInvoiceAsync`: deletes movements + invoice. No reversal journal. Risk: audit trail gaps.

### 6.10 Stock movement type mismatch
- API uses `out`/`in`; UI `InventoryService` uses `sale_out`/`purchase_in` (not recognized by `InventorySignConvention` ⇒ stock effect = 0 in API calculations).

### 6.11 Concurrency
- No rowversion/optimistic locking; concurrent edits overwrite silently.

---

## 7. مخاطر الأمان

### 7.1 Secrets in source (Critical — C1)
- 41 WPF `.cs` files + `Techtoon.Data\Context\TechtoonDbContext.cs` + Streamlit `database.py` + root Python scripts contain **hardcoded Neon connection string with password** (`npg_K9nHVkY5OjRC`).
- `Techtoon.UI\Services\SecureConnectionManager.cs` has plaintext default fallback.
- Streamlit `database.py` hardcodes `CONN_STRING`.

### 7.2 TLS
- `SSL Mode=Require; Trust Server Certificate=true` across WPF/Streamlit/Python (disables cert validation).
- `E1-MAUI-Setup.ps1`: `ServerCertificateCustomValidationCallback => true` (disables TLS entirely).

### 7.3 Auth
- Streamlit: plaintext password fallback on bcrypt exception; empty-hash "first login" bypass; no authorization (any authed user = full access).
- WPF: 4-char minimum password; no session expiry/timeout.
- Mobile: tokens in memory only (no secure storage); no expiry handling beyond 401.
- API: solid (bcrypt, rate limit 5/5min, refresh rotation, device revocation). JWT min 32 chars enforced.

### 7.4 SQL injection
- Mostly parameterized. Risky spots: `DataManagementService.GetCount` (`$"SELECT COUNT(*) FROM {table} {where}"`), `BackupService` (string-escaped values in exported SQL), `cash_boxes.py` (f-string identifiers). Medium-low risk (internal callers).

### 7.5 CORS
- Deny-by-default when no origins; `fly.toml` sets PWA origin. ✅

### 7.6 Tenant isolation
- None (single-tenant design). Not a bug per se, but limits multi-org use.

### 7.7 File upload
- No upload endpoints in API. WPF uses OpenFileDialog (local). Streamlit no uploader.

---

## 8. التكامل (WPF ↔ PWA ↔ API ↔ DB)

### 8.1 Mobile vs API mismatch
- Mobile calls `api/employees`, `api/loans`, `api/salaries`, `api/warehouses` — **no controllers** (404).
- API has `api/kits` — mobile doesn't call.
- Mobile uses sub-paths (`api/petty-cash/custodies`, etc.) that do match.

### 8.2 Contracts
- Error shape inconsistent: `{ status, message, traceId }` (middleware) vs `{ message }` (idempotency) vs `NotFound()` (empty). Mobile reads `message`/`title`/`detail` ⇒ variable behavior.
- DTOs have **no** DataAnnotations; validation only in 6 controllers' nested DTOs. Inconsistent.
- Swagger enabled only in Development.

### 8.3 Health
- `/health` (mapped) + `HealthController` `/api/health`. Fly checks `/health`.

---

## 9. الاختبارات

- No C# unit/integration tests in repo.
- Python test scripts (12) + `phase4_accounting_tests.py`.
- `phase2_api_test.ps1` (25 checks) — passed on local test DB.
- No automated accounting/inventory regression tests.

---

## 10. خطة Refactoring مرتبة حسب الأولوية (Phase 2 proposal)

### Critical (P0)
1. **C1 — Remove hardcoded secrets** from 41 WPF files + Data context + Streamlit + Python scripts; use DPAPI/config/env only. Rotate Neon password (requires approval). Add startup check rejecting disabled TLS in production.
2. **M6 — Fix TLS** (`Trust Server Certificate=true` removal; cert validation).
3. **Money as decimal** — migrate `double` → `decimal` in Core models + DB columns (requires migration approval).
4. **Unify data access** — decide EF Core vs raw Npgsql; remove parallel stack; create repositories/services in Data layer.

### High (P1)
5. **Unify balance formulas** (cash box, party balance) across UI/API.
6. **Reconcile payment edits/deletes** in UI (update `invoice_payments` + `paid_amount`).
7. **Return invoices** — reverse payments + update original invoice; add API endpoint.
8. **Fix stock movement type mismatch** (`sale_out`/`purchase_in` vs `out`/`in`).
9. **Maintain avg cost** on purchases (weighted average).
10. **Add rowversion/optimistic concurrency** to invoices, payments, stock_levels.
11. **Journal entries** — create on invoice/payment/expense; reverse on cancel/delete.
12. **Async DB in WPF** — convert sync `Open()`/`ExecuteReader()` to async; use `Task.Run` for heavy queries; avoid UI thread blocking.
13. **DataGrid virtualization** — enable row/column virtualization.
14. **Memory leaks** — unsubscribe events; remove static UI elements in `NotificationService`; clear `AuthService._permissions` on logout.

### Medium (P2)
15. **N+1 queries** — batch deletes/updates; `SaveChangesAsync` outside loops.
16. **Pagination contract** — consistent `page`/`pageSize`/`total` across UI/API.
17. **Error contract** — adopt RFC 7807 ProblemDetails; unify middleware + idempotency shapes.
18. **Validation** — DataAnnotations on DTOs; consistent 400 responses.
19. **Mobile/API alignment** — add missing controllers (employees, loans, salaries, warehouses) or remove mobile calls; add `api/kits` to mobile.
20. **Mobile secure storage** — persist tokens in `flutter_secure_storage`.
21. **Streamlit authorization** — permission checks; remove plaintext fallback.
22. **Code-behind → MVVM** — move SQL/business logic out of `.xaml.cs`.

### Low (P3)
23. **God class split** — `InvoiceEditViewModel`, `AuthService`, `PrintService`.
24. **Duplicate code extraction** — shared connection manager, inventory sign (already started), balance formulas.
25. **Tests** — add xUnit project; accounting/inventory regression tests; integration tests for API.
26. **Documentation** — update `BUILD.md`, remove merge markers (done M10).

### Phase 2 prerequisites (need your approval)
- Approve P0 (secrets rotation, TLS, decimal migration, data-access unification).
- Approve P1 (accounting rule changes: avg cost, journal entries, return-invoice reversal).
- Confirm whether to keep Streamlit or deprecate (D4).
- Confirm mobile scope (employees/loans/salaries/warehouses endpoints).
- Test DB must be refreshed from production schema (read-only copy) before accounting tests.

---

## 11. الملفات التي ستحتاج إلى تعديل (تقدير)

| الفئة | عدد الملفات (تقدير) |
|---|---|
| Core models (double→decimal) | ~15 |
| Data context (unify) | 2 |
| API services (async/transactions/balance) | ~9 |
| API controllers (validation/ProblemDetails) | 14 |
| UI services (async/conn strings/balance) | ~15 |
| UI ViewModels (N+1/virtualization) | ~22 |
| UI Views (code-behind SQL) | ~22 |
| Mobile (endpoints/secure storage) | ~5 |
| Streamlit (auth/secrets) | ~12 |
| Tests (new) | new project |

**إجمالي تقدير:** ~115 ملف.

---

## 12. الاختبارات التي يجب تجهيزها

- Unit tests: money arithmetic, inventory sign convention, balance formulas, idempotency hash comparison.
- Integration tests: invoice create/edit/cancel/delete → paid_amount, movements, status; payment create/delete → reconcile; return invoice → stock reversal.
- API tests: auth, CRUD, idempotency, concurrency (pg_advisory), rate limit.
- DB tests: FK integrity, index usage (EXPLAIN), transaction rollback.
- Accounting regression: trial balance, GL totals, COGS, gross profit.
- Inventory regression: stock_levels vs movements sum, kit cost recalculation.
- Integration: WPF ↔ API ↔ PWA ↔ Neon (shared DB counts).
- Performance: DataGrid load time, query latency, memory profile.

---

## 13. المشكلات المتبقية (بعد Phase 1 Audit)

- لا توجد C# tests (must be created in Phase 2).
- Mobile calls 4 non-existent API endpoints.
- Streamlit has no authorization and plaintext password fallback.
- 41 secret leaks (C1) — requires approval for rotation.
- Money as `double` (precision risk) — requires migration approval.
- No optimistic concurrency — requires schema change approval.

---

## 14. المخاطر التي تحتاج تدخلًا يدويًا

1. **C1 secret rotation** — requires approval + coordinated deploy.
2. **Decimal migration** — schema change on production (requires backup + approval).
3. **Data-access unification** — large refactor; risk of regression; needs phased rollout.
4. **Streamlit deprecation (D4)** — decision needed.
5. **Mobile endpoint alignment** — decision on whether to implement missing controllers or remove mobile calls.
6. **WPF async conversion** — wide-scope; may introduce deadlocks if not careful.

---

## 15. توصيات المرحلة التالية (Phase 2)

**لا أبدأ Phase 2 بدون موافقتك.** عند الموافقة، سأنفذ:
1. P0 (secrets + TLS) — requires approval.
2. P1 (accounting fixes) — requires approval per item.
3. P2/P3 (performance, error contract, tests) — can proceed incrementally.

**الملفات (للمرحلة 2، عند الموافقة):** ~115 ملف، 8-12 commits مُصغَّرة.

---

> **Phase 1 (Audit) مكتمل.** **لم يتم تعديل أي كود أو قاعدة بيانات في هذه المرحلة.**
> في انتظار موافقتك على خطة Phase 2 (P0/P1/P2/P3) قبل البدء.
