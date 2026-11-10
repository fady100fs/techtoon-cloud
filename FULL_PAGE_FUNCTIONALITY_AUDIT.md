# FULL PAGE FUNCTIONALITY AUDIT

> **2026-11-10** — مبني على الفحص الآلي + **اختبار فعلي من الواجهة** على قاعدة معزولة.
> المعيار: `ENGINEERING_RULE.md`. التفاصيل والأدلة: `REALITY_AUDIT.md`، `CRUD_TEST_RESULTS.md`.

## A. WPF — الصفحات التشغيلية (تم فحصها جميعًا)

| # | الصفحة | Create | List | Edit | Delete/Cancel | بحث | حفظ فعلي في DB | الحالة |
|---|--------|--------|------|------|---------------|------|----------------|--------|
| 1 | Accounts | ✓ | ✓ | ✓ | ✓ | — | ✓ | PASS (من الواجهة: غير مُختبَر) |
| 2 | AuditLog | — | ✓ | — | — | ✓ | — | PASS (قراءة فقط) |
| 3 | BadDebts | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | NOT VERIFIED (واجهة) |
| 4 | BankReconciliation | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | NOT VERIFIED (واجهة) |
| 5 | **CashBoxes** | ✓ | ✓ | ✓ | ✓ | — | ✓ | **PASS (من الواجهة)** — أُصلح: code + account_id + سجل تدقيق |
| 6 | CostCenters | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | NOT VERIFIED (واجهة) |
| 7 | DamagedItems | ✓ | ✓ | ✓ | ✓ | ◐ | ✓ | NOT VERIFIED (واجهة) |
| 8 | Dashboard | — | ✓ | — | — | — | — | **PASS** — أُصلح التكرار (حارس تحميل) |
| 9 | DataManagement | — | ◐ | — | ✓ (admin) | — | ✓ | NOT VERIFIED (واجهة) |
| 10 | EarlyPaymentDiscount | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | NOT VERIFIED (واجهة) |
| 11 | Employees | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | NOT VERIFIED (واجهة) |
| 12 | ExpenseCategories | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | NOT VERIFIED (واجهة) |
| 13 | Invoices | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | NOT VERIFIED (واجهة) |
| 14 | Items | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | NOT VERIFIED (واجهة) |
| 15 | LatePaymentFees | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | NOT VERIFIED (واجهة) |
| 16 | **Loans — سلف الموظفين** | ✓ | ✓ | ✓ | ✓ (استرداد) | ✓ | ✓ | **PASS (من الواجهة)** |
| 17 | **Loans — قروض المنشأة** | ✓ | ✓ | ✓ | ✓ (إقفال) | ✓ | ✓ | **PASS (من الواجهة)** + جدول أقساط وتسديد |
| 18 | Parties | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | NOT VERIFIED (واجهة) |
| 19 | Payments | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | NOT VERIFIED (واجهة) |
| 20 | **PettyCash (العهد)** | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | **PASS (من الواجهة)** — أُصلح: القوائم + التحقق + رقم العهدة |
| 21 | RecurringInvoices | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | **NOT IMPLEMENTED** (stub) |
| 22 | RecurringJournals | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | NOT VERIFIED (واجهة) |
| 23 | Reports | — | ✓ | — | — | ◐ | — | PASS (تحليلي — بلا CRUD) |
| 24 | Salaries | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | NOT VERIFIED (واجهة) |
| 25 | Settings | — | — | — | — | — | ✓ (backup) | NOT VERIFIED (واجهة) |
| 26 | StockAdjustments | ✓ | ✓ | ✓ | ✓ | ◐ | ✓ | NOT VERIFIED (واجهة) |
| 27 | StockCountSessions | ✓ | ✓ | ✓ | ✓ | ◐ | ✓ | NOT VERIFIED (واجهة) |
| 28 | StockLevels | — | ✓ | — | — | ✓ | — | PASS (قراءة فقط) |
| 29 | Users/Permissions | ✓ | ✓ | ✓ | ◐ (soft) | ✓ | ✓ | NOT VERIFIED (واجهة) |
| 30 | Warehouses | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | NOT VERIFIED (واجهة) |
| 31 | WarehouseTransfers | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | NOT VERIFIED (واجهة) |
| 32 | WriteoffRequests | ✓ | ✓ | ✓ | ✓ | ◐ | ✓ | NOT VERIFIED (واجهة) |

> **NOT VERIFIED (واجهة)** = الكود متصل بالـ DB وINSERT/UPDATE/DELETE موجودة وجردتها ومطابقتها مع أعمدة NOT NULL — لكن لم يُنفَّذ الاختبار بالنقر الفعلي بعد (وقت الجولة).

## B. Mobile / PWA

| الشاشة | CRUD | الحالة |
|--------|------|--------|
| Products · Parties · Payments | كامل (مع API) | NOT VERIFIED (Flutter SDK غير متاح) |
| Dashboard · CashBoxes · Inventory · Loans · Generic ×8 | قراءة | NOT VERIFIED |
| **Reports** | **لا شيء** | **NOT IMPLEMENTED** (stub بلا بيانات وهمية) |
| Sales (تحويل) / Settings | stub | NOT IMPLEMENTED |

> قرار المالك: Flutter/PWA = link-only — لا أولوية تطوير.

## C. API (18 controller)

| الحالة | التفاصيل |
|--------|----------|
| **PASS** | `/api/employees` (كان 500 → 200)، `/api/loans`، `/api/payments` CRUD + PUT + 400 واضح، `/health`، عقد problem+json |
| **NOT IMPLEMENTED** | `Expenses PUT` (Mobile 405)، `RecurringInvoices` |
| **NOT VERIFIED** | بقية الـ endpoints (مختبرة smoke فقط: 200) |

## D. قواعد الفواتير/السندات/القروض

| القاعدة | الحالة |
|---------|--------|
| الفاتورة تُحفظ ذرّيًا (فاتورة+سطور+حركات+مدفوعات في Transaction) | PASS (موثّق في P0) |
| السند يكتب `invoice_payments` ويُصلح رصيد الفاتورة (إنشاء/تعديل/حذف) | PASS (موثّق في P1) |
| المرتجع يُنشئ حركة عكسية للمخزون | PASS (موثّق في P1) |
| **القرض: استلام ← جدول أقساط ← تسديد ← رصيد الخزينة** | **PASS (من الواجهة)** |
| **الجاري المدين: فائدة فقط، الأصل يبقى للبنك** | **PASS (من الواجهة)** |
| الحذف يُمنع عند وجود حركات/تسديدات | PASS (منطق) / NOT VERIFIED (من الواجهة) |
| لا سجلات يتيمة بعد الحذف | PASS (24/24 DB test) |
