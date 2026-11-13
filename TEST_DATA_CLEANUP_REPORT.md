# تقرير تنظيف بيانات الاختبار

> جولة 2026-11-13 · قاعدة العمل: `techtoon_test` · الإنتاج `neondb`: **لم يُمس**

---

## 1) بيانات الاختبار التي أنشأتها هذه الجولة

| # | السجل | المُنشئ | المعرّف | الحالة |
|---|-------|---------|---------|--------|
| 1 | مستخدم `apiverify_cashier` (دور CASHIER) | `dbtool mkuser` لاختبار صلاحيات الـAPI | `username LIKE 'apiverify%'` | ✅ **محذوف** |
| 2 | استثناءات صلاحيات المستخدم التجريبي | إنشاء المستخدم | `user_permissions` | ✅ **محذوف** |
| 3 | سجلات تدقيق المستخدم التجريبي | `POST /api/auth/login` | `audit_log WHERE username LIKE 'apiverify%'` | ✅ **محذوف** |
| 4 | محاولات دخول المستخدم التجريبي | `POST /api/auth/login` | `login_attempts WHERE username LIKE 'apiverify%'` | ✅ **محذوف** |
| 5 | سجلات أجهزة الـAPI (admin + المستخدم التجريبي) | كل عمليات دخول الاختبار | `device_id LIKE 'dbg%'/'APIVER%'/'verif%'/'api%'` | ✅ **محذوف** |
| 6 | طرف «UpdatedParty_*» | `POST /api/parties` | `name LIKE '%APIVER%'` | ✅ **محذوف** (اختبار الـAPI حذفه) |
| 7 | مصروف اختباري | `POST /api/expenses` | `notes LIKE 'APIVER%'` | ✅ **محذوف** (اختبار الـAPI حذفه) |
| 8 | فاتورة/سند اختباري | `PaymentEditTests` | `notes LIKE 'PAYEDIT%'` | ✅ **محذوف** (تنظيف الاختبار) |
| 9 | سجلات `app_audit_logs` للاختبارات | الاختبارات | `details LIKE '%APIVER%'` | ✅ **محذوف** |
| 10 | قاعدة `techtoon_test_ci` | مُثبّت الاختبارات (نسخة معزولة) | — | ✅ **محذوفة** |
| 11 | قواعد `techtoon_rt_manual` / `_manual2` / `techtoon_restore_check` / `techtoon_restoretest_*` | تجارب الاستعادة | — | ✅ **محذوفة** |
| 12 | حساب الأطراف الافتراضي `DEFAULT-PARTY` | اختبار الاستيراد | `accounts.code = 'DEFAULT-PARTY'` | ✅ **محذوف** |

## 2) التحقق بعد التنظيف

| الفحص | النتيجة |
|-------|---------|
| مستخدمون بأسماء اختبارية | **0** ✅ |
| أطراف باسم اختباري | **0** ✅ |
| مصروفات بوسم اختباري | **0** ✅ |
| سجلات أجهزة اختبارية | **0** ✅ |
| قواعد بيانات اختبارية باقية | **0** (بقيت `techtoon_test` فقط) ✅ |

## 3) بيانات المستخدم — لم تتغيّر

| الجدول | قبل الجولة | بعد التنظيف | مطابق |
|--------|-----------|-------------|--------|
| items | 6 | **6** | ✅ |
| parties | 1 | **1** | ✅ |
| accounts | 8 | **8** | ✅ |
| subscriptions | 2 | **2** | ✅ |
| subscription_periods | 24 | **24** | ✅ |
| payments | 4 | **4** | ✅ |
| inventory_movements | 2 | **2** | ✅ |
| cash_boxes | 2 | **2** | ✅ |
| currencies | 3 | **3** | ✅ |
| users | 1 (admin) | **1** (admin) | ✅ |

## 4) كيف حُذفت البيانات (بدون أوامر عامة)

كل الحذف كان **بشروط دقيقة** تُحدِّد سجلات هذه الجولة فقط:

```sql
DELETE FROM user_devices      WHERE device_id LIKE 'dbg%' OR device_id LIKE 'APIVER%' ...
DELETE FROM login_attempts    WHERE username LIKE 'apiverify%';
DELETE FROM audit_log          WHERE username LIKE 'apiverify%';
DELETE FROM user_permissions   WHERE user_id IN (SELECT id FROM users WHERE username LIKE 'apiverify%');
DELETE FROM users              WHERE username LIKE 'apiverify%';
DELETE FROM parties            WHERE name LIKE '%APIVER%';
DELETE FROM expenses           WHERE notes LIKE 'APIVER%' OR description LIKE 'APIVER%';
DELETE FROM payments           WHERE notes LIKE 'APIVER%' OR reference_number LIKE 'APIVER%';
DELETE FROM accounts           WHERE code = 'DEFAULT-PARTY';
```

**لم يُستخدم:** `TRUNCATE` · `DROP TABLE` · `DELETE FROM table` بلا شرط · Factory Reset · تهيئة قاعدة البيانات.

## 5) سجلات أنشأتها الاختبارات الآلية (تُنظَّف ذاتيًا)

| الأثر | كيف يُنظَّف |
|-------|-------------|
| مستخدمو/سجلات اختبارات `techtoon_test_ci` | القاعدة كلها تُحذف بعد الجولة (أُسقطت) |
| سجلات `app_audit_logs` للاختبارات | تُنظَّف في `DatabaseTestCleanupFixture` قبل/بعد المجموعة |
| الحساب الافتراضي `DEFAULT-PARTY` | يُنظَّف في مُثبّت الاختبارات + بعد الجولة |

## 6) بيانات الإنتاج

| الفحص | النتيجة |
|-------|---------|
| `neondb` — عدد الحسابات/الأطراف/المستخدمين | 31 / 3 / 3 — **كما كانت** (لم تُنفَّذ عليها أي كتابة) |
| عمليات كتابة على الإنتاج | **0** — كل الاختبارات استخدمت `techtoon_test` أو `techtoon_test_ci` |

## 7) التحقق بعد التنظيف: إعادة تشغيل التطبيقات

| التطبيق | النتيجة |
|---------|---------|
| WPF Desktop | ✅ يفتح، الدخول يعمل، لوحة التحكم تعرض بيانات المستخدم |
| ASP.NET Core API | ✅ `/health` = Healthy، والدخول يعمل |
| Streamlit | ✅ كل الوحدات تُستورد والاستعلامات تعمل |

## 8) ملفات مؤقتة أُنشئت خارج المشروع

| الملف/المجلد | الموقع | الحالة |
|--------------|--------|--------|
| أدوات الفحص (`dbtool`, `dumptool`, `uitest*`, `smoke_test.py`) | `C:\Users\fady1\AppData\Local\TechtoonTools\` | خارج المستودع — **لم تُحذف** (أدوات تطوير) |
| `Techtoon.Streamlit/smoke_test.py` | داخل المستودع | **مُضاف كدليل اختبار قابل لإعادة التشغيل** |
| `deploy/distribution/` + `dist/` | مخرجات بناء | مُتجاهَلة في git |
| `C:\Users\fady1\AppData\Local\TechtoonTools\installtest` | مجلد التثبيت الاختباري | حُذف بتثبيت/إلغاء التثبيت |
