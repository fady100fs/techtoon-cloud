# تقرير الاختبار والبناء — المرحلة الثانية

> **المشروع:** Techtoon Accounting
> **التاريخ:** 2026-11-09
> **النوع:** نتائج فعلية (وليست تقديرية) لبناء واختبارات نُفِّذت على الجهاز.
> **قاعدة الاختبار:** PostgreSQL 17.6 محلي معزول — `Host=127.0.0.1;Port=55432;Database=techtoon_test;Username=postgres;SSL Mode=Disable`
> **تحذير:** لم تُستخدم قاعدة الإنتاج Neon ولا بياناتها في أي اختبار كتابة.

---

## 1. بيئة التنفيذ

| العنصر | القيمة |
|---|---|
| نظام التشغيل | Windows (win32) |
| .NET SDK | 10.0.403 |
| PostgreSQL | 17.6 (نسخة محمولة داخل `D:\PROG\Accounting_Prog\_testenv\pg`) |
| منفذ قاعدة الاختبار | `55432` |
| قاعدة الاختبار | `techtoon_test` |
| Python | 3.11.9 (لـ bcrypt والتحقق من TOML) |
| Flutter SDK | **غير متوفر** |
| Docker | **غير متوفر** |
| API environment | `Development` |
| API URL أثناء الاختبار | `http://localhost:5190` |
| JWT test secret | قيمة اختبارية فقط (غير إنتاجية) |

---

## 2. نتائج البناء

### 2.1 الحل بالكامل

الأمر:

```powershell
dotnet build Techtoon.sln -v q --nologo
```

النتيجة الفعلية:

```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

### 2.2 بناء المشاريع المستهدفة منفردة

| المشروع | النتيجة |
|---|---|
| `Techtoon.Api/Techtoon.Api.csproj` | Build succeeded — 0 Error(s) |
| `Techtoon.UI/Techtoon.UI.csproj` | Build succeeded — 0 Error(s) |
| `Techtoon.Core/Techtoon.Core.csproj` | ضمن بناء الحل — ناجح |
| `Techtoon.Data/Techtoon.Data.csproj` | ضمن بناء الحل — ناجح |

### 2.3 التحقق من الإعدادات

| الفحص | النتيجة |
|---|---|
| `fly.toml` عبر `python -c "import tomllib..."` | `OK vm= [{'memory': '512mb', 'cpu_kind': 'shared', 'cpus': 1}]` |
| `Dockerfile` / `Techtoon.Api/Dockerfile` | `EXPOSE 8080` و`ASPNETCORE_URLS=http://+:8080` |
| `BUILD.md` | 256 سطرًا، 0 علامات تعارض |
| مراجع `Techtoon.Mobile` في `Techtoon.sln` | 0 |

---

## 3. اختبارات تكامل API

**السكربت:** `C:\Users\fady1\AppData\Local\Temp\opencode\phase2_api_test.ps1`
**التشغيل:** API محلي على `http://localhost:5190` + قاعدة `techtoon_test`.
**النتيجة النهائية:** **25 ناجح / 0 فاشل**.

| # | الفحص | المتوقع | النتيجة |
|---|---|---|---|
| 1 | `GET /health` | 200 | PASS |
| 2 | تسجيل دخول `testadmin` | 200 + accessToken | PASS |
| 3 | تسجيل دخول `testlimited` | 200 | PASS |
| 4 | `POST /api/items` بمستخدم محدود | 403 | PASS |
| 5 | `GET /api/items` بمستخدم محدود | 200 | PASS |
| 6 | دخول مستخدم `emptyhash` | 401 | PASS |
| 7 | دخول مستخدم `malformedhash` | 401 | PASS |
| 8 | إنشاء فاتورة بيع | 201 | PASS |
| 9 | `cost_price` لسطر الفاتورة = تكلفة الصنف | 10 | PASS |
| 10 | `paid_amount` الابتدائي | 10 | PASS |
| 11 | صيغة رقم الفاتورة | `SAL-YYYY-NNNNNN` | PASS |
| 12 | إنشاء دفعة مرتبطة بالفاتورة | 201 | PASS |
| 13 | الدفعة تُرجع `invoiceId` | = معرّف الفاتورة | PASS |
| 14 | `paid_amount` بعد دفعة 40 | 50 | PASS |
| 15 | حالة الفاتورة بعد السداد الكامل | `paid` | PASS |
| 16 | حذف الدفعة | 204 | PASS |
| 17 | `paid_amount` بعد الحذف | 10 | PASS |
| 18 | حالة الفاتورة بعد الحذف | `partial` | PASS |
| 19 | أول طلب بمفتاح Idempotency | 201 | PASS |
| 20 | إعادة نفس الطلب بنفس المفتاح | نفس معرّف الفاتورة | PASS |
| 21 | نفس المفتاح بحمولة مختلفة | 409 | PASS |
| 22 | خطأ تحقق مُعاد بنفس المفتاح | 400 (غير مخزّن) | PASS |
| 23 | 5 فواتير متزامنة | 5 أرقام فريدة | PASS |
| 24 | `/health` مع `X-Forwarded-For` | 200 | PASS |
| 25 | حد المحاولات بعد 5 إخفاقات من IP واحد | رسالة "Too many failed login attempts" | PASS |

### 3.1 ملاحظات تشغيلية صادقة

- في تشغيل مبكر ظهر فشلان في الاختبار **بسبب نقص بيانات/أعمدة في قاعدة الاختبار المحلية**، لا بسبب الشيفرة:
  1. غياب صف العملة `currencies.id=1` المطلوب بواسطة مفتاح خارجي → أُضيف للاختبار المحلي.
  2. غياب عمود `items.opening_quantity` الذي تتوقعه قراءة الأصناف → أُضيف للاختبار المحلي.
- بعد استكمال مخطط الاختبار المحلي، مرّت **كل** الفحوص 25/25.
- الفحص 14 استُخدم بمبلغ 40 (بدل 15) لأن الفاتورة بسطر واحد 2×25 + دفعة 10 = 50؛ والهدف إثبات الوصول إلى حالة `paid`.
- تشغيل API استخدم `launchSettings.json` الذي ثبّت المنفذ 5190 رغم تمرير متغير آخر؛ هذا لا يؤثر على النتائج.

---

## 4. فحوص سلامة البيانات بعد الاختبارات

استعلامات مباشرة على `techtoon_test`:

| الفحص | النتيجة | التفسير |
|---|---|---|
| `orphan_invoice_payments` | **0** | لا بنود دفتر تشير إلى فواتير غير موجودة |
| `paid_amount_mismatch` | **0** | `invoices.paid_amount` يطابق مجموع `invoice_payments` |
| `invoices_total_unique_numbers` | **14 / 14** | 14 فاتورة، 14 رقمًا فريدًا |
| `negative_in_out_movements` | **0** | لا حركات `in`/`out` بكمية سالبة (C5) |
| `zero_cost_invoice_lines` | **0** | لا سطور فاتورة بتكلفة صفرية (H10) |
| `idempotency_keys` | 3 | مفاتيح اختبار متبقية ضمن صلاحية 24 ساعة |
| `login_attempts` بعناوين IP مختلفة | 10.0.0.5/.6/.11/.12 و10.9.9.9 | يثبت تسجيل IP الحقيقي من `X-Forwarded-For` (H5) |

---

## 5. اختبارات نوعية لكل بند

| البند | كيف تم التحقق | النتيجة |
|---|---|---|
| C2 | مستخدم بهاش فارغ + مستخدم بهاش تالف | 401 لكليهما |
| C3 | حفظ فاتورة (نجاح) + مراجعة مسار المعاملة/rollback في الشيفرة | معاملة واحدة، لا كتابة جزئية |
| C5 | استعلام الحركات بعد كل الاختبارات | 0 حركة in/out سالبة |
| C6 | مراجعة استعلامات الدفعات/الكشف + بناء ناجح | أعمدة قياسية، رمي بدل الصمت |
| C7 | `grep Techtoon.Mobile Techtoon.sln` + بناء الحل | 0 مراجع، بناء ناجح، المجلد سليم |
| C8 | مستخدم محدود: POST 403 / GET 200 | مطابق |
| C9 | `tomllib` + فحص `EXPOSE` | صالح ومتطابق |
| C10 | مراجعة ثابتة للشيفرة (لا SDK) | منطق الأخطاء/التوكن/الافتراضي سليم |
| H1 | مراجعة `SaveAsDraftAsync` | لا تكرار لـ`currency_id` |
| H2 | مراجعة القفل + بناء | `FOR UPDATE` للصندوقين بترتيب المعرّف |
| H3 | اختبار حذف دفعة | 50 → 10 واستعادة `partial` |
| H4 | مراجعة الحفظ | `Task.Run` خارج خيط الواجهة |
| H5 | تسجيل IP + تفعيل الحد | IP حقيقي + حظر بعد 5 |
| H6 | مراجعة CORS | لا fallback مفتوح |
| H7 | 4 فحوص Idempotency | كلها مطابقة |
| H8 | 5 طلبات متزامنة | 5 أرقام فريدة |
| H9 | مراجعة منطق `last_seen` | انتهاء وإبطال صحيح |
| H10 | تكلفة السطر + دورة `paid_amount` | 10 و10→50→10 |
| M1 | مراجعة `EnsureDefaultAdminAsync` | لا إنشاء تلقائي |
| M3 | مراجعة `ErrorGuard`/`App.xaml.cs` | لا `Environment.Exit` |
| M8 | مراجعة الاستعلام المُجمَّع والحركات | N+1 مُزال، حقول كاملة |
| M9 | `grep MessageBox في PrintService` | لا نوافذ داخل الخدمة |
| M10 | عدّ علامات التعارض | 0 |

---

## 6. ما تعذّر اختباره

| العنصر | السبب | ما تم بدلًا منه |
|---|---|---|
| بناء/تشغيل Flutter | لا يوجد Flutter SDK | مراجعة ثابتة لـ`api_service.dart` و`login_screen.dart` |
| اختبار واجهة WPF آليًا | لا تتوفر أتمتة سطح مكتب | بناء WPF ناجح + مراجعة مسارات SQL/المعاملات |
| اختبار TLS/شهادة الإنتاج (M6) | يتطلب أسرار/شهادات وموافقة | لم يُغيَّر شيء |
| اختبار تاريخ Git (M2) | يتطلب إعادة كتابة تاريخ | `.gitignore` فقط |
| اختبار قيود اليومية (D1) | قرار معماري غير معتمد | لم يُبنَ |

---

## 7. إعادة إنتاج الاختبارات

1. تشغيل PostgreSQL المحلي على `55432` بقاعدة `techtoon_test`.
2. التأكد من وجود بيانات الاختبار: `testadmin`, `testlimited`, `emptyhash`, `malformedhash`، عميل/صنف/صندوق/عملة.
3. تشغيل API:

```powershell
$env:ASPNETCORE_ENVIRONMENT='Development'
$env:ConnectionStrings__DefaultConnection='Host=127.0.0.1;Port=55432;Database=techtoon_test;Username=postgres;SSL Mode=Disable'
$env:Jwt__Secret='phase2-test-secret-key-0123456789abcdef'
dotnet run --project Techtoon.Api\Techtoon.Api.csproj --no-build
```

4. تشغيل السكربت:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File "C:\Users\fady1\AppData\Local\Temp\opencode\phase2_api_test.ps1"
```

5. إيقاف API ثم تنفيذ استعلامات القسم 4 للتحقق من السلامة.

---

## 8. الخلاصة

- البناء: **ناجح — 0 أخطاء / 0 تحذيرات**.
- اختبارات التكامل: **25/25**.
- سلامة البيانات: **كل الفحوص صفرية الانحراف**.
- الاختبارات لم تمس الإنتاج، ولم تُنفّذ migrations إنتاجية، ولم تُستخدم بيانات إنتاج.

---

## 9. تحقّق النشر على Fly.io (2026-11-09)

النشر تم على التطبيق الموجود `techtoon-mobile` (الإصدار `v9`)، والصورة مدمجة: Nginx + API.

| # | الفحص | الطريقة | النتيجة |
|---|---|---|---|
| 1 | واجهة PWA | `GET https://techtoon-mobile.fly.dev/` | **200** `text/html` |
| 2 | صحة الـAPI وقاعدة Neon | `GET /health` | **200** + `Healthy` |
| 3 | أصل الـAPI داخل الحزمة | فحص `main.dart.js` المنشور | 7/7 `techtoon-mobile.fly.dev`، و0 `techtoon-api.fly.dev` |
| 4 | تحميل محرك Flutter | Chromium headless | `flutter-view` موجود، طلبا `canvaskit.js`/`canvaskit.wasm` ناجحان، 0 أخطاء Console، 0 طلبات فاشلة |
| 5 | MIME لملف wasm | `Content-Type` | `application/wasm` |
| 6 | CORS preflight | `OPTIONS /api/auth/login` من أصل التطبيق | **204** + `Access-Control-Allow-Origin: https://techtoon-mobile.fly.dev` |
| 7 | مسار الدخول | `POST /api/auth/login` ببيانات غير صحيحة | **401** JSON من الـAPI (يثبت وصول الطلب للخادم وقاعدة البيانات) |
| 8 | **اختبار الدخول الواحد** | `POST /api/auth/login` بمستخدم `admin` | **200** — الدور `ADMIN`، 34 صلاحية، تم إصدار access/refresh token |
| 9 | حالة الـmachine | `flyctl status` | `started` + `1 total, 1 passing` |

### 9.1 ملاحظات صادقة

- كلمة مرور `admin` لم تُطبع ولم تُخزَّن. تم التحقق من مطابقة الهاش المخزّن لأحد كلمات المرور الموثقة في وثائق المشروع **محليًا**، ثم أُرسلت محاولة دخول واحدة فقط إلى الخادم.
- قبل اختبار الدخول أُرسلت محاولة تشخيصية باسم مستخدم وهمي (`__probe__`) نتجت عنها **401**، وسُجّل صف فاشل واحد في `login_attempts` لهذا الاسم الوهمي. لا يوجد أي تأثير على `admin`.
- اختبار الدخول الناجح أنشأ/حدّث سجل الجهاز (`user_devices`) و`last_login` وسجل تدقيق، وهي آثار طبيعية ومتوقعة لتسجيل دخول واحد.
- لم تُنفَّذ أي migrations أو تعديلات مخطط على قاعدة الإنتاج.
- خادم API يعمل على `127.0.0.1:8081` فقط داخل الحاوية؛ المنفذ العام الوحيد هو `8080` عبر Nginx.
- تحذير غير مؤثر في السجلات: `Failed to determine the https port for redirect`، لأن TLS ينتهي عند Fly ويُمرَّر `X-Forwarded-Proto: https`.
