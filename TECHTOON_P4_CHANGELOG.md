# سجل التغييرات — P4 (إصلاحات التشغيل الفعلي + القروض)

> **التاريخ:** 2026-11-10 | **السبب:** ملاحظات المستخدم من تشغيل فعلي للبرنامج (خطأ XAML، أزرار ميتة، قوائم فارغة، تكرار لوحة التحكم) + طلب وحدة قروض كاملة.

## P4-1 — إصلاح crash الـ XamlParseException ( obstructing )

- **السبب الجذري:** `Techtoon.UI/App.xaml` → `<Setter Property="VirtualizingStackPanel.VirtualizationMode" Value="Simple"/>` — القيمة `Simple` **غير موجودة** في WPF (المتاح: `Standard` / `Recycling` فقط، و`Simple` من UWP).
- **الأثر:** الـ Style مطبَّق على **كل DataGrid في البرنامج** → `Initialization of 'System.Windows.Setter' threw an exception` عند رسم أي جدول (تسبب في crash نافذة لوحة التحكم).
- **الإصلاح:** `Value="Recycling"`.
- **المصدر:** `Techtoon.UI\bin\Release\net10.0-windows\_logs\crash-20261110-092249-459.txt`.

## P4-2 — تكرار لوحة التحكم

- **السبب:** التحميل يُنفَّذ مرتين متزامنتين (من مُنشئ `DashboardViewModel` + من `UserControl_Loaded`) → سباق على `Metrics.Clear()` يعطي 5 بطاقات + 5 بطاقات.
- **الإصلاح:** حارس `Interlocked.Exchange(ref _loadBusy, 1)` + `finally { _loadBusy = 0; }`، وإزالة الاستدعاء من المُنشئ.

## P4-3 — أزرار الخزائن (مربعات فارغة)

- **السبب:** `CashBoxesView.xaml` الأزرار الثلاثة بلا `Content` (ولا code-behind يضبط النص) → مربعات ملونة صغيرة.
- **الإصلاح:** `Content="➕ إضافة"` / `✏️ تعديل` / `🗑️ حذف` + عنوان الصفحة.

## P4-4 — قوائم العهدة الفارغة

- **السبب 1:** `employees.status` يُخزَّن `ACTIVE` (كبير) والاستعلام يقارن `'active'` (صغير) → لا نتائج.
- **السبب 2:** `accounts.type` نص (`ASSET/EQUITY/...`) والاستعلام يقارن `type = 1` (رقم) → خطأ SQL مُبتلَع بـ `catch {}` → قائمة فارغة.
- **الإصلاح:** `COALESCE(status,'ACTIVE') ILIKE 'active%'` و `UPPER(COALESCE(type,''))='ASSET'` + إظهار أخطاء التحميل بدل ابتلاعها + تحويل `double`→`decimal` في المبلغ + تصحيح سجل التدقيق (`PettyCah` → `PettyCash`).

## P4-5 — API `/api/employees` كان 500

- **السبب:** `System.InvalidCastException: Column 'allowances' is null` — EF يقرأ الكيان كاملًا وأعمدة المال NULL.
- **الإصلاح:** (1) ترحيل `P4_loans_closed_loop.sql`: تصفير NULL + `DEFAULT 0` + `NOT NULL`; (2) المتحكم يبرمج SQL ويقرأ الأعمدة المطلوبة فقط.
- نفس المعالجة في `LoansController`.

## P4-6 — وحدة القروض والسلف (حلقة مغلقة)

- **فصل المفاهيم:** تبويبان — **سلف الموظفين** (مرتبطة بالعهدة) و**قروض المنشأة** (بنك/مؤسسة تمويل).
- **أنواع القروض:** فائدة ثابتة (قسط ثابت Annuity) / فائدة متغيرة / جارٍ مدين (فائدة فقط، الأصل يبقى للبنك) / بدون فائدة.
- **جدول الأقساط:** يُولَّد آليًا عند الإنشاء + نافذة مخصصة «جدول الأقساط» تُفتح تلقائيًا بعد إنشاء القرض.
- **الحلقة المغلقة:**
  | الحدث | الأثر المالي |
  |---|---|
  | صرف سلفة | `payments` نوع `payment` (خروج من الخزينة) |
  | استرداد سلفة | `payments` نوع `receipt` (دخول) + تحديث `paid/remaining` |
  | استلام قرض | `payments` نوع `receipt` (صافي بعد الرسوم) |
  | تسديد قسط | `payments` نوع `payment` + `loan_payments` + تحديث القسط والرصيد |
  | تسوية جارٍ مدين | سداد رأس المال المتبقي + الإقفال التلقائي |
  | حذف | يمنع إن وُجدت تسديدات، وإلا يحذف الجدول + الحركة المالية معًا |
- **منطق حسابي نقي في Core:** `Techtoon.Core/Finance/LoanScheduleCalculator.cs` (قابل للاختبار).
- **ترحيل DB:** `deploy/migrations/P4_loans_closed_loop.sql` (إضافي فقط: `loan_payments` + أعمدة الربط + تصفير NULL).

## P4-7 — ربط العهدة بالسلف

- زر «سلفة للموظف» في صفحة العهد يفتح `EmployeeAdvanceWindow` مع اختيار العهدة المفتوحة للموظف، وكل حركة صرف/استرداد تُرحَّل في الخزينة.

## P4-8 — تنظيف

- حذف `LoanEditWindow` (orphan) — استُبدل بـ `EmployeeAdvanceWindow`.

---

## نتائج التحقق

| البند | النتيجة |
|---|---|
| `dotnet build Techtoon.sln` | **0 أخطاء** |
| `dotnet test Techtoon.Tests` | **21/21 ناجح** (13 سابق + 8 جديدة لحاسبة الأقساط) |
| اختبار DB متكامل (قاعدة `techtoon_test`) | **24/24 ناجح** |
| ترحيل الإنتاج | `loan_payments` منشأ + أعمدة الربط + تصفير NULL — **0 سجلات إنتاجية أُنشئت** |
| بيانات الإنتاج بعد الاختبار | `loans=0, loan_installments=0, loan_payments=0, payments=0` (سليمة) |
