# نتائج الاختبارات الفعلية

> جولة 2026-11-13 · **كل الاختبارات نُفِّذت فعليًا** — لا «PASS» بدون تنفيذ.
> قاعدة البيانات: `techtoon_test` (اختبار) · الإنتاج `neondb`: **لم يُكتب عليه شيء**.

---

## 1) الحزمة الآلية (`dotnet test`)

| المجموعة | العدد | النتيجة |
|----------|-------|---------|
| كل اختبارات المشروع | **135** | ✅ **135/135 ناجحة** |

### الاختبارات الجديدة في هذه الجولة

| الاختبار | ما يُثبته | النتيجة |
|----------|----------|---------|
| `PermissionsTests.Every_Role_Permission_Is_A_Real_Catalog_Key` | لا مفتاح صلاحية مطبوع خطأ في أي دور | ✅ |
| `PermissionsTests.Every_Page_Has_A_Permission_And_It_Exists` | كل صفحة (41) مرتبطة بصلاحية موجودة | ✅ |
| `PermissionsTests.Permission_Keys_Are_Unique_And_Well_Formed` | لا تكرار في المفاتيح | ✅ |
| `PermissionsTests.Roles_Are_Defined_For_The_Whole_Catalog` | 7 أدوار + ADMIN يملك كل شيء + VIEWER بلا تعديل | ✅ |
| `PermissionsTests.Different_Roles_Expose_Different_Pages` | الأدوار ترى صفحات مختلفة | ✅ |
| `PermissionsTests.Legacy_Permission_Keys_Map_To_Modern_Ones` | التوافق مع المفاتيح القديمة | ✅ |
| `PermissionsTests.Schema_Is_Created_And_System_Roles_Are_Seeded` | الجدول والأدوار تُزرع | ✅ |
| `PermissionsTests.User_Inherits_The_Permissions_Of_Its_Role` | الوراثة | ✅ |
| `PermissionsTests.User_Override_Grants_A_Permission_Above_The_Role` | استثناء «منح» | ✅ |
| `PermissionsTests.User_Override_Denies_A_Permission_From_The_Role` | استثناء «منع» | ✅ |
| `PermissionsTests.Admin_Role_Resolves_To_Every_Permission` | ADMIN = كل شيء | ✅ |
| `PermissionsTests.Login_Resolves_Permissions_From_The_Role_And_Overrides` | الدخول يحمّل الدور | ✅ |
| `PermissionsTests.Setting_A_Role_For_A_User_Changes_The_Effective_Permissions` | تغيير الدور يغيّر الصلاحيات | ✅ |
| `PermissionsTests.Custom_Role_Can_Be_Created_And_Deleted` | أدوار مخصصة | ✅ |
| `PermissionsTests.System_Roles_Cannot_Be_Deleted` | الأدوار النظامية محمية | ✅ |
| `BundleSubscriptionsTests.Subscriptions_Bundle_Contains_Subscriptions_And_Their_Periods` | المجمّع يحوي الاشتراكات وفتراتها | ✅ |
| `BundleSubscriptionsTests.Subscriptions_Export_Uses_Portable_Keys_Not_Database_Ids` | مفاتيح طبيعية | ✅ |
| `BundleSubscriptionsTests.Subscriptions_Reimport_Changes_Nothing` | إعادة الاستيراد لا تغيّر شيئًا | ✅ |
| `BundleSubscriptionsTests.Party_Without_Account_Uses_The_Default_Account` | الحساب الافتراضي | ✅ |
| `BundleSubscriptionsTests.Party_Without_Account_Gives_A_Clear_Arabic_Error...` | رسالة عربية لا خطأ PostgreSQL | ✅ |
| `BundleSubscriptionsTests.Analyze_Reports_The_Problem_Before_Any_Write` | المعاينة تكتشف المشكلة | ✅ |
| `PaymentEditTests.Updating_A_Payment_Changes_The_Row_And_Does_Not_Duplicate_It` | التعديل لا يُكرّر السند ولا البند | ✅ |
| `PaymentEditTests.Payment_Edit_Window_Loads_The_Existing_Payment...` | تحميل السند القائم | ✅ |

### اختبارات سابقة (من جولات سابقة) ما زالت ناجحة

النسخ الاحتياطي والاستعادة · استيراد/تصدير Excel · الترقيم الذكي والإيراد المؤجّل · كشوف البنك ·
حزمة الأدوار/الصلاحيات · اختبارات الدورات (B1–B8) · الترخيص وتوقيعه · توحيد البيانات.

---

## 2) اختبار الـAPI حيًّا (خادم حقيقي على PostgreSQL)

شغّلنا `Techtoon.Api` فعليًا على `http://127.0.0.1:5211` متصلًا بـ`techtoon_test`، ونفّذنا 29 فحصًا:

| # | الفحص | النتيجة |
|---|-------|---------|
| 1 | `POST /api/auth/login` (admin) | ✅ 200 |
| 2 | `POST /api/auth/login` (مستخدم بدور CASHIER) | ✅ 200 |
| 3 | الو.Role يحتوي 8 مطالبات صلاحيات | ✅ 8 |
| 4 | `GET /api/invoices` (بمفتاح قديم `sales`) | ✅ 200 (كان 403) |
| 5 | `GET /api/items` | ✅ 200 |
| 6 | `POST /api/parties` | ✅ 201 (كان 500) |
| 7 | `GET /api/parties/{id}` | ✅ 200 (كان 42703) |
| 8 | `PUT /api/parties/{id}` | ✅ 200 |
| 9 | تعديل الطرف انعكس في القاعدة | ✅ |
| 10 | `POST /api/expenses` | ✅ 201 (كان 23502) |
| 11 | `PUT /api/expenses/{id}` (المسار لم يكن موجودًا) | ✅ 200 |
| 12 | تعديل المصروف انعكس (20.00) | ✅ |
| 13–26 | 14 نقطة قراءة (خزائن، مخازن، موظفين، مرتبات، قروض، كيت، تحويلات، قيود دورية، عهد، تسوية بنكية، ديون، مخزون، مصروفات، مدفوعات) | ✅ كلها 200 |
| 27 | `DELETE /api/expenses/{id}` | ✅ 204 |
| 28 | `DELETE /api/parties/{id}` | ✅ 204 |
| 29 | `GET /health` | ✅ Healthy (فحص Neon حقيقي) |

**إجمالي: 29/29 ناجح.**

أُضيف بعدها فحص مُصفَّح كان يفشل 400:
`GET /api/payments?type=receipt` ✅ · `GET /api/invoices?type=sales` ✅ · `GET /api/invoices?type=purchase` ✅ · `GET /api/parties?type=customer` ✅ · `GET /api/items?search=Test` ✅

---

## 3) فحص دخان Streamlit (على قاعدة حقيقية)

`Techtoon.Streamlit/smoke_test.py` — 34 فحصًا: **34/34 ناجح**
(دخول صحيح ✔ / دخول خاطئ مرفوض ✔ / 16 استعلام صفحة نُفِّذت ✔ / 14 وحدة استُوردت ✔)

---

## 4) اختبار الواجهة (UIAutomation على البرنامج الحقيقي)

| الفحص | النتيجة |
|-------|---------|
| تشغيل `Techtoon.UI.exe` من حزمة التوزيع | ✅ يفتح |
| تسجيل دخول `admin` | ✅ «admin (مدير النظام)» |
| القائمة الجانبية للمدير | ✅ **22 زرًا** (كل الشاشات) |
| القائمة الجانبية للكاشير | ✅ **8 أزرار** فقط |
| شاشة «الأدوار والصلاحيات» | ✅ تبويبان + **7 أدوار** + **88 صلاحية** |
| شاشة «الاستيراد/التصدير» | ✅ وصف مجمّع الاشتراكات يظهر |

---

## 5) فحص التوزيع والتثبيت

| الفحص | النتيجة |
|-------|---------|
| `deploy\verify-distribution.ps1` | ✅ **29/29** |
| بناء حزمة التثبيت `TechtoonSetup.exe` | ✅ 73.3 م.ب |
| تثبيت صامت فعلي + تشغيل + إلغاء تثبيت | ✅exit code 0 / البرنامج يعمل / نُظّف كل شيء |

---

## 6) فحص المحاسبة والمخزون (استعلامات مباشرة)

11 فحصًا — كلها ناجحة (تفاصيلها في `ACCOUNTING_AND_INVENTORY_VERIFICATION.md`).

---

## 7) ما لم يُختبر (NOT VERIFIED)

| البند | السبب |
|-------|-------|
| بناء/تشغيل حزمة Flutter | لا Flutter SDK على هذه الآلة |
| `flutter test` | نفس السبب + ملف الاختبار قالب افتراضي |
| الطباعة الحرارية (58/80mm) | تحتاج معاينة طباعة فعلية |
| الأرقام التسلسلية للمخزون | غير منفَّذة أصلًا في المخطط |
