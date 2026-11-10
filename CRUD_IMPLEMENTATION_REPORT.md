# CRUD IMPLEMENTATION REPORT

> آخر تحديث: **2026-11-10 (P4 + تدقيق واقعي)** — راجع `REALITY_AUDIT.md` للأدلة الكاملة.

##原则: لا يُكتب "تم" إلا مع دليل تنفيذ حقيقي.

## ما تم إصلاحه في هذه الجولة (مُثبت من واجهة المستخدم)

| # | المشكلة | السبب الجذري | الإصلاح | الدليل | الحالة |
|---|---------|--------------|---------|--------|--------|
| 1 | إضافة خزينة تفشل `23502 null value in column "code"` | `cash_boxes.code` NOT NULL بلا افتراضي، والكود كان يمرر NULL | توليد كود فريد تلقائيًا (`CB###`) | `cash_boxes id=3 code=CB003` | PASS |
| 2 | لا يمكن ربط الخزينة بحساب | `cash_boxes.account_id` NOT NULL + لا حقل في المحرر | حقل «الحساب المرتبط» (قائمة حسابات الأصول) + إلزامي | `account_id=2` | PASS |
| 3 | فشل دخول عند لصق كلمة المرور | `Password` كانت تُنقل بـ `KeyDown` فقط | ربط `PasswordChanged` | دخول آلي ناجح | PASS |
| 4 | قائمة الموظفين فارغة في العهدة | `status='ACTIVE'` ≠ `'active'` | `ILIKE 'active%'` | combo: 1 عنصر | PASS |
| 5 | **قائمة "حساب العهدة" فارغة** (شكاوى المالك) | `accounts.type` نص ≠ رقم `1` | `UPPER(type)='ASSET'` | combo: Bank + Cash | PASS |
| 6 | إنشاء قرض/سلفة يفشل 500 | `payments.currency_id` NOT NULL بلا افتراضي | `DEFAULT 1` + تمرير العملة من جدول `currencies` | 3 حركات بـ currency_id=1 | PASS |
| 7 | حفظ العهدة يفشل | 3 أعمدة NOT NULL تمرر NULL | تحقق إلزامي + رسائل عربية | `petty_cash_custodies id=1` | PASS |
| 8 | رقم العهدة فارغ | لم يكن يُولَّد | توليد `عهد-yyyyMMdd-####` | عمود `custody_number` | PASS |
| 9 | تسجيل التدقيق ناقص | لا استدعاء لـ `AuditService` | log لكل خزينة/عهدة/قرض | `app_audit_logs` | PASS |
| 10 | 5 Views من P0-3 غير موجودة في الإنتاج | التوثيق سبق التنفيذ | إنشاء `v_stock_balance` + 4 أخرى | `pg_views` = 5 | PASS |
| 11 | `/api/employees` يرجع 500 | EF يقرأ كيانًا فيه NULL | SQL projection + تصفير NULL | `/api/employees` = 200 | PASS |
| 12 | رابط تقسيم خاطئ في `LoanScheduleCalculator` | القسط الأخير لا يمتص التقريب | تصحيح + 8 اختبارات | 21/21 | PASS |

## البنية الجديدة — قروض وسلف (حلقة مغلقة)

| الملف | الدور |
|-------|-------|
| `Techtoon.Core/Finance/LoanScheduleCalculator.cs` | حساب جدول الأقساط (منطق خالص، قابل للاختبار) |
| `Techtoon.UI/Views/LoanWindows.cs` | نافذة سلفة الموظف + نافذة قرض المنشأة + مساعدات DB |
| `Techtoon.UI/Views/LoanScheduleWindows.cs` | نافذة جدول الأقساط + نافذة التسديد |
| `Techtoon.UI/ViewModels/LoansViewModel.cs` | تبويبان: سلف الموظفين / قروض المنشأة + أوامر CRUD |
| `Techtoon.UI/Views/LoansView.xaml` | واجهة التبويبين |
| `deploy/migrations/P4_loans_closed_loop.sql` | جدول `loan_payments` + أعمدة الربط + تصفير NULL |

**دورة المال المُثبتة:**
```
سلفة موظف   → صرف: payments(payment) ↓الخزينة  |  استرداد: payments(receipt) ↑الخزينة
قرض منشأة   → استلام: payments(receipt) ↑     |  تسديد قسط: payments(payment) ↓ + loan_payments
جارٍ مدين   → فائدة فقط (الأصل يبقى)          |  تسوية: سداد الأصل + إقفال تلقائي
```

## ما لم يتم (موثّق في `INCOMPLETE_FEATURES.md`)

- `RecurringInvoicesView` — stub (NOT IMPLEMENTED)
- `LoansService` + `LoanDetailsWindow` — orphan قديم (NOT IMPLEMENTED)
- `Expenses PUT` في الـ API (Mobile 405)
- بقية صفحات WPF — مُختبَرة على مستوى الكود والـ SQL، لكن ليس كلها من الواجهة (NOT VERIFIED)
