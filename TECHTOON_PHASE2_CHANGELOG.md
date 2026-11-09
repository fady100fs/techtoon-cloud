# سجل تغييرات المرحلة الثانية — Techtoon Accounting

> **التاريخ:** 2026-11-09
> **النوع:** مُودعة محليًا في `cd8733d` + `a78c686` + إصلاح النشر (بدون push).
> **المرجع:** معالجة نتائج التدقيق C2–C10 / H1–H10 / M1–M10.
> **ملاحظة:** المستودع يحتوي أيضًا على تعديلات محلية سابقة للمرحلة الثانية (مثل `Techtoon.Core/Models/*`، `Techtoon.Data/Context/TechtoonDbContext.cs`، `models.py`، `services.py`، `MainWindow.xaml`، ملفات `.webview_data`، وحذف أرشيفات ZIP). **لم تُعدَّل هذه الملفات ضمن المرحلة الثانية**، وهي مذكورة هنا فقط للتمييز.

---

## 1. ملفات أُنشئت

| الملف | الغرض | البند |
|---|---|---|
| `Techtoon.Core/Inventory/InventorySignConvention.cs` | مصدر وحيد لتفسير إشارة `inventory_movements.quantity` (SQL + C#) دون migration إنتاجي | C5 |
| `Techtoon.Api/Security/PermissionPolicyProvider.cs` | سياسات `perm:<key>` الديناميكية + `HasPermissionAttribute` | C8 |
| `TECHTOON_PHASE2_PROGRESS.md` | تقرير التقدم والحالات | — |
| `TECHTOON_PHASE2_CHANGELOG.md` | هذا الملف | — |
| `TECHTOON_PHASE2_TEST_REPORT.md` | تقرير البناء والاختبارات | — |
| `TECHTOON_PENDING_DECISIONS.md` | القرارات المعمارية المعلّقة | D1–D5 |

---

## 2. ملفات عُدِّلت

### 2.1 الأمان والصلاحيات

| الملف | التغيير | البند |
|---|---|---|
| `Techtoon.Api/Extensions/ServiceCollectionExtensions.cs` | تسجيل `PermissionPolicyProvider`، وإلغاء fallback CORS المفتوح | C8, H6 |
| `Techtoon.Api/Controllers/InvoicesController.cs` | `[HasPermission]` على نقاط الكتابة | C8 |
| `Techtoon.Api/Controllers/PaymentsController.cs` | `[HasPermission]` على نقاط الكتابة | C8 |
| `Techtoon.Api/Controllers/ItemsController.cs` | `[HasPermission]` على نقاط الكتابة | C8 |
| `Techtoon.Api/Controllers/PartiesController.cs` | `[HasPermission]` على نقاط الكتابة | C8 |
| `Techtoon.Api/Controllers/CashBoxesController.cs` | `[HasPermission]` على نقاط الكتابة | C8 |
| `Techtoon.Api/Controllers/ExpensesController.cs` | `[HasPermission]` على نقاط الكتابة | C8 |
| `Techtoon.Api/Controllers/WarehouseTransfersController.cs` | `[HasPermission]` على نقاط الكتابة | C8 |
| `Techtoon.Api/Controllers/KitsController.cs` | `[HasPermission]` على نقاط الكتابة | C8 |
| `Techtoon.Api/Controllers/BankReconciliationController.cs` | `[HasPermission]` على نقاط الكتابة | C8 |
| `Techtoon.Api/Controllers/BadDebtController.cs` | `[HasPermission]` على نقاط الكتابة | C8 |
| `Techtoon.Api/Controllers/PettyCashController.cs` | `[HasPermission]` على نقاط الكتابة | C8 |
| `Techtoon.Api/Controllers/RecurringJournalController.cs` | `[HasPermission]` على نقاط الكتابة | C8 |
| `Techtoon.Api/Services/AuthAppService.cs` | فشل مغلق للهاش الفارغ/التالف، تسجيل فشل المحاولات/التدقيق، انتهاء توكن التحديث | C2, H5, H9 |
| `Techtoon.UI/Services/AuthService.cs` | فشل مغلق للهاش، إزالة `admin/admin123`، تسجيل أخطاء الصلاحيات/التدقيق | C2, M1, M4 |

### 2.2 النقد والدفعات والفاتورة

| الملف | التغيير | البند |
|---|---|---|
| `Techtoon.Api/Services/PaymentAppService.cs` | `reference_number` بدل `voucher_no`، `InvoiceId`، كتابة/حذف `invoice_payments`، إعادة حساب `paid_amount` والحالة، كشف أعمدة وقت التشغيل | H3, H10 |
| `Techtoon.Api/DTOs/Payments/PaymentDto.cs` | إضافة `InvoiceId` إلى DTO الإنشاء والقراءة | H10 |
| `Techtoon.Api/Services/InvoiceAppService.cs` | قفل advisory لترقيم الفواتير، احتساب `MAX+1`، تخزين `cost_price` الحقيقي، قراءة الإشارة الموحدة للمخزون | C5, H8, H10 |
| `Techtoon.UI/ViewModels/PaymentsViewModel.cs` | حذف الدفعة داخل معاملة مع تسوية `paid_amount`، وتحويل المبلغ إلى `decimal` | H3, M5 |
| `Techtoon.UI/Views/PaymentEditWindow.xaml.cs` | إزالة تحويل المبلغ إلى `double` | M5 |
| `Techtoon.UI/Views/InvoiceEditViewModel.cs` | حفظ ذرّي (معاملة + rollback)، إزالة `currency_id` المكرر، تنفيذ الحفظ خارج خيط الواجهة | C3, H1, H4 |
| `Techtoon.UI/Services/FinancialService.cs` | `payment_type`، رمي بدل الصفر الصامت، قفل الصندوقين بترتيب المعرّف، إغلاق الوردية على العجز فقط، تسجيل أخطاء | C6, H2, M4 |
| `Techtoon.UI/Services/PrintService.cs` | الأعمدة القياسية للدفعات/الكشف، وإزالة `MessageBox` من طبقة الخدمة | C6, M9 |

### 2.3 المخزون

| الملف | التغيير | البند |
|---|---|---|
| `Techtoon.Api/Services/ItemAppService.cs` | استخدام `InventorySignConvention` في قراءات الحركة/الرصيد | C5 |
| `Techtoon.UI/Services/InventoryAdjustmentService.cs` | تطبيق `ABS` حسب نوع الحركة | C5 |
| `Techtoon.UI/Services/InventoryCountService.cs` | تطبيق `ABS` حسب نوع الحركة | C5 |
| `Techtoon.UI/ViewModels/DashboardViewModel.cs` | تطبيق `ABS` في تجميعات المخزون | C5 |
| `Techtoon.UI/ViewModels/ItemsViewModel.cs` | تطبيق `ABS` في حساب الرصيد | C5 |
| `Techtoon.UI/ViewModels/StockLevelsViewModel.cs` | تطبيق `ABS` في حساب الرصيد | C5 |
| `Techtoon.UI/Views/StockAdjustmentEditWindow.xaml.cs` | تطبيق `ABS` عند قراءة الحركة | C5 |
| `Techtoon.UI/Views/ReturnInvoiceWindow.xaml.cs` | إزالة N+1 (استعلام مُجمَّع)، حركات كاملة الحقول، تحديث `stock_levels` ذرّيًا | M8 |

### 2.4 المنصة والتشغيل والأخطاء

| الملف | التغيير | البند |
|---|---|---|
| `Techtoon.Api/Program.cs` | `ForwardedHeaders` (hop واحد) وإعادة ترتيب pipeline ليعمل Idempotency بعد المصادقة | H5, H7 |
| `Techtoon.Api/Middleware/IdempotencyMiddleware.cs` | حجز ذرّي، مقارنة hash/endpoint، `user_id`، تحرير المفاتيح عند الخطأ، منع التخزين المؤقت للأخطاء | H7 |
| `Techtoon.UI/App.xaml.cs` | استدعاء `ErrorGuard.Report` في `DispatcherUnhandledException`، تسجيل فشل النسخ الاحتياطي | M3, M4 |
| `Techtoon.UI/ErrorGuard.cs` | إزالة `Environment.Exit` بعد 3 أخطاء، تسجيل فشل التقرير | M3 |
| `Techtoon.sln` | إزالة مشروع `Techtoon.Mobile` و12 سطر إعداد | C7 |
| `fly.toml` | إضافة `[[vm]]` صحيح | C9 |
| `.gitignore` | منع تتبع `.sessions.json` و`.webview_data/` وملفات الجلسات/الكاش | M2 |
| `BUILD.md` | إزالة علامات تعارض الدمج | M10 |
| `Techtoon.Mobile/lib/core/services/api_service.dart` | `ApiException`، مهلات، توكنات، تحديث/إعادة محاولة عند 401، رسائل عربية | C10 |
| `Techtoon.Mobile/lib/features/auth/login_screen.dart` | إزالة بيانات الدخول الافتراضية وفحوص `mounted` | C10 |

---

## 3. ملفات لم تُعدَّل (مقصودة)

| الملف/المكوّن | السبب |
|---|---|
| `Techtoon.Data/Context/TechtoonDbContext.cs` | يحتوي بيانات اتصال — C1 مستبعد |
| `Techtoon.UI/Services/SecureConnectionManager.cs` | بيانات اتصال — C1 |
| ثوابت الاتصال داخل `InvoiceEditViewModel.cs` / `PaymentsViewModel.cs` / `ReturnInvoiceWindow.xaml.cs` | بيانات اتصال — C1. الملفات نفسها عُدِّلت لأسباب أخرى (C3, H3, M8) لكن **نص الاتصال لم يُلمس**. |
| `apply_security_migration.py` | بيانات اتصال — C1 |
| ملفات Python / Streamlit / Backup / الأرشيفات | لا حذف قبل موافقة (D4) |
| مخطط قاعدة الإنتاج وبياناتها | لا كتابة/لا migration إنتاجي |

---

## 4. مخرجات مساندة (خارج المستودع)

| المسار | الوصف |
|---|---|
| `D:\PROG\Accounting_Prog\_backups\pre-phase2-tracked.patch` | نسخة تراجع لتعديلات الملفات المتعقّبة قبل المرحلة الثانية |
| `D:\PROG\Accounting_Prog\_backups\pre-phase2-untracked\` | نسخة من 133 ملفًا غير متعقّب قبل المرحلة الثانية |
| `C:\Users\fady1\AppData\Local\Temp\opencode\phase2_api_test.ps1` | سكربت اختبارات تكامل API (25 فحصًا) |
| `D:\PROG\Accounting_Prog\_testenv\` | PostgreSQL 17.6 المحلي وقاعدة `techtoon_test` |

---

## 5. ملاحظات التوافق

- كل تغييرات المخطط كانت على **قاعدة الاختبار المحلية فقط** لمواءمة أعمدة الإنتاج (`invoices.total/paid_amount/...`, `payments.invoice_id`, `invoice_payments`, `user_permissions`). لا migrations إنتاجية.
- الشيفرة تعتمد كشف الأعمدة وقت التشغيل (`information_schema`) حيث يختلف مخطط الإنتاج عن `models.py`، لذلك لا تنكسر عند غياب عمود اختياري.
- لم تُحذف أي وظيفة أو تكامل قائم؛ الإضافات الجديدة متوافقة للخلف.

---

## 6. إصلاح نشر Fly.io (2026-11-09)

### 6.1 ملفات أُنشئت

| الملف | الغرض |
|---|---|
| `deploy/mobile/Dockerfile` | صورة مدمجة: بناء `Techtoon.Api` + Nginx يقدّم حزمة PWA |
| `deploy/mobile/nginx.conf` | تقديم الملفات الثابتة وتمرير `/api/*` و`/health` إلى `127.0.0.1:8081` |
| `deploy/mobile/start.sh` | تشغيل Nginx والـAPI معًا؛ خروج الحاوية عند موت أي منهما |
| `deploy/mobile/fly.toml` | إعداد Fly لتطبيق `techtoon-mobile` (health check على `/health`) |
| `deploy/mobile/prepare-context.ps1` | تجميع سياق بناء صغير (26.3 MB) بدل رفع المستودع كاملًا |
| `deploy/mobile/mirror-pwa.ps1` | عكس حزمة PWA المنشورة وإعادة كتابة أصل الـAPI إلى نفس النطاق |
| `deploy/.dockerignore` | استبعاد `bin/obj/.vs` من سياق البناء |

### 6.2 ملفات عُدِّلت

| الملف | التغيير | البند |
|---|---|---|
| `Techtoon.Api/Program.cs` | تسجيل `AddDbContext<TechtoonDbContext>` مع `UseNpgsql(connStr)` — ستة Controllers كانت ستفشل في DI | — |
| `fly.toml` | إضافة أصل PWA إلى `Cors__AllowedOrigins__0` | H6 |
| `Techtoon.Mobile/lib/core/services/api_service.dart` | تحويل 401 أثناء الدخول إلى رسالة «بيانات غير صحيحة» بدل «انتهت الجلسة» | C10 |

### 6.3 خارج Git

| المسار | الوصف |
|---|---|
| `Techtoon.Mobile/build/web/` | حزمة PWA المنشورة (27 ملفًا، 26.3 MB) مع أصل API مُعاد كتابته. مستبعدة من Git عبر `Techtoon.Mobile/.gitignore`. |
| `C:\Users\fady1\AppData\Local\Temp\opencode\techtoon-deploy-context\` | سياق البناء المُجمَّع المستخدم في النشر |

### 6.4 أسرار Fly

| المفتاح | الحالة |
|---|---|
| `ConnectionStrings__DefaultConnection` | مضبوط (القيمة لم تُطبع) |
| `Jwt__Secret` | مضبوط بمفتاح جديد 64 حرفًا (القيمة لم تُطبع) |

> تدوير `Jwt__Secret` يُبطل أي توكنات قديمة؛ لا توجد توكنات إنتاجية سابقة لأن الـAPI لم يكن منشورًا.
