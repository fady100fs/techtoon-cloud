# P5 — الاشتراكات + التراخيص + مثبّت EXE (2026-11-10)

> **المعيار:** `ENGINEERING_RULE.md` — كل ميزة مُختبَرة فعليًا مع دليل.

---

## 1. نظام الاشتراكات (تبويب داخل صفحة الفواتير)

**المتطلبات المنفَّذة:**
- تبويب **«💳 الاشتراكات»** داخل صفحة الفواتير (جنبًا إلى «🧾 الفواتير»).
- العميل يشتري **خدمة** بمبلغ دوري: **شهري** أو **سنوي**.
- عند إنشاء الاشتراك تُولَّد **الفترات** (الاستحقاق = نهاية كل شهر/سنة).
- عند الاستحقاق: إنشاء **سند دفع** (حركة خزينة حقيقية) ⟵ الفترة تُعتبر **مسددة**.
- إن لم يُدفع: **إنذار في لوحة التحكم** (عدد الفترات المتأخرة + المبلغ المتبقي).
- الحذف ممنوع إن وُجدت فترات مسددة (يُقترح الإيقاف).

**البنية:**
| الملف | الدور |
|---|---|
| `deploy/migrations/P5_subscriptions.sql` | جدولان: `subscriptions` + `subscription_periods` (إضافي فقط) |
| `Techtoon.UI/ViewModels/SubscriptionsViewModel.cs` | التحميل/الفلترة/الحذف/الإيقاف/السداد |
| `Techtoon.UI/Views/SubscriptionsView.xaml` | واجهة التبويب (جدول اشتراكات + جدول فترات) |
| `Techtoon.UI/Views/SubscriptionWindows.cs` | نافذة إنشاء/تعديل الاشتراك + نافذة سداد الفترة (سند دفع) |
| `Techtoon.UI/Views/InvoicesView.xaml` | تحويل الصفحة إلى TabControl بتبويبين |
| `Techtoon.UI/ViewModels/DashboardViewModel.cs` + `DashboardView.xaml` | بانر الإنذار |

**الحلقة المغلقة (مُثبتة):**
```
اشتراك (عميل + خدمة + 500 شهر��) → 12 فترة مستحقة
   ↓ استحقاق وبلا سداد
لوحة التحكم: "⚠ تنبيه: 3 فترة اشتراك متأخرة لم يتم سدد ... المتبقي: 900.00"
   ↓ سداد فترة (زر «سداد الفترة المحددة»)
payments: 300 receipt ref='SUB:5:42' cash_box=2 currency=1  ← سند دفع حقيقي
subscription_periods: paid_amount=300 status='paid' payment_id=5 paid_at=...
cash_boxes: movements=+300 (دخلت الخزينة)
```

**أدلة الاختبار (قاعدة معزولة `techtoon_test`):**
| # | الاختبار | الدليل | الحالة |
|---|----------|--------|--------|
| 1 | إنشاء اشتراك من الواجهة | `subscriptions: party=1, amount=500, frequency=monthly` + 12 فترة | **PASS** |
| 2 | ظهوره في الجدول | صف: `Test Customer | اشتراك صيانة شهرية | 500.00 | شهري | 2026-11-10 | 2026-12-09` | **PASS** |
| 3 | إنذار لوحة التحكم | `SUBSCRIPTION ALERT VISIBLE = True` — «3 فترة اشتراك متأخرة… المتبقي 900.00» | **PASS** |
| 4 | سداد فترة | `payments: 300 receipt SUB:5:42` + الفترة `paid` + الخزينة +300 | **PASS** |
| 5 | الإنتاج | `subscriptions=0, subscription_periods=0, payments=0` (لم يُمَسّ) | **PASS** |

---

## 2. نظام التراخيص

**المتطلبات:** رخصة دائمة + سنوية + شهرية + **فترة تجريبية 10 أيام افتراضيًا** عند إصدار الـ EXE.

**التنفيذ:**
| المكوّن | الملف |
|---|---|
| منطق الترخيص + التوقيع + هوية الجهاز | `Techtoon.Core/Licensing/LicenseService.cs` |
| أداة إصدار التراخيص (بديل Python) | `Techtoon.LicenseTool/` (`machine-id`, `generate`, `verify`) |
| الفحص عند الإقلاع | `Techtoon.UI/App.xaml.cs` (يمنع التشغيل عند انتهاء الرخصة ويعرض معرّف الجهاز) |
| ملف الترخيص | `license.key` بجانب الـ EXE — **نفس صيغة `license_manager.py`** (JSON + HMAC-SHA256) |

**التوافق مع أداة Python (مُثبت):**
```
PYTHON ID = A21C7D18E7CE123E  →  مقبول ضمن المعرّفات المقبولة في C# ✓
```
(لأن Python在没有 WMI يستخدم احتياطي `NODE|MAC` — أضفنا كل الصيغ: hex/عشرية/معكوسة/حالة الاسم.)

**أدلة:**
| # | الاختبار | الدليل | الحالة |
|---|----------|--------|--------|
| 1 | افتراضي: 10 أيام تجريبية | `LICENSE: state=Trial daysLeft=10` | **PASS** |
| 2 | انتهاء التجربة → منع التشغيل + عرض معرّف الجهاز | نافذة «البرنامج غير مرخّص» + معرّف الجهاز | **PASS** |
| 3 | إصدار رخصة سنوية | `generate --type yearly` → ينتهي 2027-11-10 | **PASS** |
| 4 | تفعيل الرخصة | `LICENSE: state=Valid type=yearly daysLeft=365` | **PASS** |
| 5 | النسخة المثبتة تعمل | `RUNNING title='Techtoon Accounting'` + سجل: `CanConnect: True` | **PASS** |

---

## 3. مثبّت EXE (Inno Setup)

**الملفات:** `deploy/installer/Techtoon.iss` + `deploy/installer/build-installer.ps1`

**ما يفعله السكربت:**
1. `dotnet publish` بملف واحد (win-x64, self-contained, single-file) ⟵ **لا يحتاج .NET مثبّت**.
2. ينسخ `logo.ico` و`fonts` و`LICENSE.txt`.
3. يشغّل `ISCC.exe` (Inno Setup) ⟵ ينتج `dist\TechtoonSetup.exe`.

**أدلة (Inno Setup 7 موجود على الجهاز):**
| # | الاختبار | النتيجة | الحالة |
|---|----------|---------|--------|
| 1 | النشر | `Techtoon.UI.exe` **70.7 MB** (ملف واحد) | **PASS** |
| 2 | تشغيل الـ EXE المستقل | `RUNNING title='Techtoon Accounting'` | **PASS** |
| 3 | بناء المثبّت | `dist\TechtoonSetup.exe` **68.9 MB** — «Successful compile» | **PASS** |
| 4 | تثبيت فعلي (`/VERYSILENT /DIR=...`) | الملفات كلها في مكانها + `unins000.exe` | **PASS** |
| 5 | تشغيل النسخة المثبتة | تعمل + `LICENSE state=Trial` + `CanConnect: True` + نسخ احتياطي تلقائي | **PASS** |

**ملاحظات:**
- المثبّت يعرض رسالة عن الفترة التجريبية (10 أيام) بعد التثبيت.
- لتفعيل برنامج عميل: ضع `license.key` بجانب الـ EXE المثبّت (أو في `dist\` قبل البناء ليدخل مع الحزمة).
- لإصدار رخصة: `Techtoon.LicenseTool.exe machine-id` ثم `generate --type yearly --customer "اسم العميل" --machine <ID>`.

---

## 4. إصلاح نظامي: فخ `DateOnly` في Npgsql 10

**الاكتشاف (عبر اختبار الواجهة):** خطأ «Unable to cast object of type 'System.DateOnly' to type 'System.DateTime'» ⟵ **أعمدة النوع `date` يرجعها Npgsql 10 بصيغة `DateOnly`**، وأي `GetDateTime()` عليها يفشل.

**الأثر:** صفحات كانت تفشل بصمت عند التحميل: **العهد النثرية**، **الديون المعدومة**، **التسوية البنكية**، **القيود الدورية**، **الرواتب**، **القروض**، + الاشتراكات.

**الإصلاح:** `Techtoon.Core/Data/DbValue.cs` (`AsDateTime` / `AsNullableDateTime`) + تحديث كل مواضع القراءة المتأثرة (11 ملفًا).

**قاعدة دائمة:** لا تستخدم `GetDateTime()` على عمود نوعه `date` — استخدم `DbValue.AsDateTime(r.GetValue(i))`.
