# دليل التثبيت — Techtoon Accounting

## 1) تثبيت PostgreSQL

1. حمّل PostgreSQL من: https://www.postgresql.org/download/windows/
2. ثبّته واختر كلمة مرور لمستخدم `postgres` (احفظها).
3. اترك المنفذ الافتراضي `5432`.

## 2) إنشاء قاعدة البيانات

افتح **SQL Shell (psql)** أو **pgAdmin** ونفّذ:

```sql
CREATE DATABASE techtoon_accounting;
```

## 3) إنشاء الجداول

### الطريقة أ — سكربت جاهز (موصى بها)

```powershell
psql -d techtoon_accounting -f deploy\migrations\schema_full.sql
```

السكربت ينشئ كل الجداول (85) والعروض (5) والفهارس والعلاقات — بدون بيانات.

### الطريقة ب — التطبيق ينشئ البيانات المرجعية تلقائيًا

عند أول تشغيل على قاعدة فارغة، يملأ البرنامج تلقائيًا:
دليل الحسابات الأساسي، العملات (EGP/USD/SAR)، والخزائن (نقدية + بنكية).

## 4) ضبط اتصال البرنامج

### الطريقة أ — متغير بيئة (موصى بها)

اضبط المتغير قبل تشغيل البرنامج:

```powershell
$env:TECHTOON_PG_CONNECTION = "Host=localhost;Port=5432;Database=techtoon_accounting;Username=postgres;Password=كلمة_المرور"
.\Application\Techtoon.UI.exe
```

### الطريقة ب — محفوظة داخل البرنامج

بعد أول تشغيل ناجح، تُحفظ سلسلة الاتصال مشفّرة (DPAPI) في:

```
%APPDATA%\TechtoonAccounting\connection.enc
```

> **مهم:** الاتصال المحفوظ مربوط بمستخدم Windows الحالي. عند نقله لجهاز آخر
> اضبط `TECHTOON_PG_CONNECTION` من جديد.

## 5) التشغيل

انقر مرتين على `Application\Techtoon.UI.exe`.

### حساب المدير (الأمان)

لا يوجد كلمة مرور افتراضية ثابتة. لإنشاء أول حساب مدير:

```powershell
$env:TECHTOON_BOOTSTRAP_ADMIN_PASSWORD = "كلمة_مرور_قوية_12_حرفًا_على_الأقل"
.\Application\Techtoon.UI.exe
```

تُنشأ الحساب مرة واحدة، ثم احذف المتغير من بيئة النظام.

> **لا تستخدم كلمات مرور افتراضية في التوزيع.**

## 6) تفعيل البرنامج

`الإعدادات` ← تبويب **«تفعيل البرنامج»** ← **«استيراد ملف الترخيص»**.

(راجع `README.md` للتفاصيل الكاملة.)

---

## التحقق من سلامة التثبيت

```powershell
# 1) البرنامج يعمل
.\Application\Techtoon.UI.exe

# 2) الاتصال بقاعدة البيانات سليم
psql -d techtoon_accounting -c "SELECT COUNT(*) FROM accounts;"

# 3) حالة الترخيص
.\Tools\Techtoon.LicenseTool.exe status
```

---

© 2026 Techtoon Accounting
