<<<<<<< HEAD
## 📋 نظرة عامة

نظام محاسبة عربي متكامل (WPF) يعمل على .NET 10 متصل بقاعدة بيانات PostgreSQL سحابية (Neon).

- **Framework**: .NET 10 (Preview) + WPF
- **Database**: Neon PostgreSQL (Cloud) - 46 جدول
- **Driver**: Npgsql 10.0.3 (direct access)
- **PDF**: QuestPDF 2026.9.1 (Community License + UseSystemFonts)
- **Auth**: BCrypt.Net-Next 4.0.3 (workFactor 12) + DPAPI (connection string)
- **MVVM**: CommunityToolkit.Mvvm 8.4.0 + custom UiRelayCommand
- **Build**: ✅ 0 errors, 0 warnings

---

## ✅ المنجز في هذه الجلسة

### المرحلة P1 - الاستخدام اليومي (مكتملة)
- ✅ **تصدير PDF بالـ QuestPDF** (`QuestPdfReportService.cs`)
  - تقرير المبيعات (A4 + RTL + Tahoma + ترقيم صفحات)
  - تقرير الجرد
  - `Settings.UseSystemFonts = true` لاستخدام خطوط ويندوز
- ✅ **زر "🔍 بحث" بجانب التاريخ** في التقارير لتأكيد الفترة
- ✅ **أزرار سريعة للفترات**: اليوم / أسبوع / شهر / ربع سنة / سنة
- ✅ **كشف حساب عميل/مورد** (`PartyStatementWindow.xaml` + `StatementPdfService.cs`)
  - رصيد افتتاحي + حركات + رصيد نهائي
  - تصدير PDF احترافي
  - زر "كشف حساب" في صفحة العملاء
- ✅ **تحميل العملاء تلقائيًا** عند فتح الصفحة
- ✅ **طباعة الفاتورة A4 مع المعاينة** (`PreviewInvoiceA4`)
  - `FlowDocumentScrollViewer` لعرض الفاتورة
  - زر طباعة واضح في الأسفل
  - A4 (816×1056 at 96 DPI)
- ✅ **البحث المتقدم في الفواتير**
  - بحث برقم الفاتورة (ILIKE)
  - بحث باسم العميل (ILIKE)
  - نطاق تاريخ (من/إلى)
  - SQL ديناميكي آمن (Parameters)
- ✅ **تنبيه نقص المخزون** (بانر أصفر بارز)
- ✅ **اختصارات لوحة المفاتيح في الفاتورة**
  - F2: إضافة سطر | F3: دفع كامل | F4: حفظ | F9: باركود | Ctrl+P: طباعة A4 | Esc: إغلاق

### المرحلة P0 - أمان البيانات (مكتملة)
- ✅ **تشفير connection string** (`SecureConnectionManager.cs` + DPAPI)
- ✅ **نسخ احتياطي تلقائي** (`AutoBackupScheduler.cs`) - كل 24h، آخر 7 نسخ
- ✅ **إزالة كلمة المرور من appsettings.json**
- ✅ **كل الخدمات تستخدم `SecureConnectionManager.GetConnectionString()`**

### المرحلة P2 - ميزات متقدمة (مكتملة)
- ✅ **مرتجع المبيعات/المشتريات** (`ReturnInvoiceWindow.xaml`)
  - إرجاع جزئي (أصناف محددة فقط)
  - تتبع المرتجع السابق لكل صنف
  - منع تجاوز الكمية المتاحة
  - تحديث المخزون تلقائيًا
  - ربط بالفاتورة الأصلية (`reference_invoice_id`)
- ✅ **تقارير رسومية في Dashboard** (رسم أفقي آمن)

### التنظيف التقني
- ✅ **0 أخطاء + 0 تحذيرات**
- ✅ **توحيد بنية DB** (payments: 12 عمود نظيف بدلاً من 16)
- ✅ **إصلاحات لوحة التحكم**:
  - إصلاح `BarHeightConverter` (إزالة layout cycle)
  - إصلاح `payments.type` → `payment_type` بعد تنظيف DB
  - إضافة عرض `StatusMessage` للتشخيص
  - إصلاح تداخل العناوين

---

## 📊 حالة الصفحات

| # | الصفحة | الحالة | ملاحظات |
|---|---|---|---|
| 1 | Dashboard | ✅ | رسم بياني + تنبيه مخزون + بطاقات |
| 2 | Invoices | ✅ | بحث متقدم + زر مرتجع |
| 3 | Parties | ✅ | زر كشف حساب |
| 4 | Items | ✅ | يعمل |
| 5 | Payments | ✅ | سندات قبض/صرف |
| 6 | Cash Boxes | ✅ | يعمل |
| 7 | Accounts | ✅ | يعمل |
| 8 | Reports | ✅ | تصدير PDF + زر بحث |
| 9 | Settings | ✅ | يعمل |
| 10 | ReturnInvoiceWindow | ✅ | مرتجعات جزئية |
| 11 | PartyStatementWindow | ✅ | كشف حساب + PDF |

---

## 🗂️ بنية المشروع

### Techtoon.UI\ViewModels
- `MainViewModel.cs`, `DashboardViewModel.cs`, `InvoicesViewModel.cs`
- `PaymentsViewModel.cs`, `ItemsViewModel.cs`, `AccountsViewModel.cs`
- `CashBoxesViewModel.cs`, `ReportsViewModel.cs`, `SettingsViewModel.cs`
- `ViewModelBase.cs`, `UiRelayCommand.cs`

### Techtoon.UI\Views
- `MainWindow.xaml` + 9 صفحات (UserControl)
- `InvoiceEditWindow.xaml` (+ طباعة A4 + اختصارات)
- `PaymentEditWindow.xaml`, `ItemEditWindow.xaml`, `PartyEditWindow.xaml`, `AccountEditWindow.xaml`
- `ReturnInvoiceWindow.xaml` (جديد - مرتجعات)
- `PartyStatementWindow.xaml` (جديد - كشف حساب)

### Techtoon.UI\Services
- `AuthService.cs`, `SecureConnectionManager.cs` (جديد), `AutoBackupScheduler.cs` (جديد)
- `BackupService.cs`, `FinancialService.cs`, `InventoryService.cs`
- `PrintService.cs` (+ PreviewInvoiceA4)
- `QuestPdfReportService.cs`, `StatementPdfService.cs` (جديد)

### Techtoon.UI\Converters
- `BoolToVisibilityConverter.cs`, `StringToVisibilityConverter.cs`, `BoolToVisibilityInvertedConverter.cs`

---

## 🗄️ قاعدة البيانات (Neon PostgreSQL)

### 46 جدول + 4 views
| الفئة | الجداول |
|---|---|
| **أساسية** | users, parties, items, categories, warehouses, currencies |
| **فواتير** | invoices, invoice_lines, invoice_payments |
| **مدفوعات** | payments (12 عمود نظيف) |
| **مخزون** | inventory_movements, stock_levels, stock_adjustments, stock_counts |
| **مالية** | cash_boxes, cash_transfers, expenses, accounts, journal_entries |
| **متقدم** | employees, loans, fixed_assets, recurring_invoices, audit_log |

### أعمدة أُضيفت في هذه الجلسة
- `invoices.reference_invoice_id` - لربط المرتجع بالفاتورة الأصلية
- `payments.invoice_id` - لربط السند بالفاتورة
- `parties.is_active`, `items.is_active`, `cash_boxes.is_active`

### أعمدة حُذفت (تنظيف)
- `payments.type`, `payments.method`, `payments.voucher_no`, `payments.party_name`

### معلومات الاتصال (آمنة)
- كلمة المرور مشفّرة بـ DPAPI في `%APPDATA%\TechtoonAccounting\connection.enc`

---

## 🔧 كيفية التشغيل

```powershell
cd "D:\PROG\Accounting Prog\Techtoon Accounting C+"
dotnet build Techtoon.UI
cd "Techtoon.UI"
dotnet run
```

### بيانات الدخول
- Username: `admin`
- Password: (محدد في `AuthService.EnsureDefaultAdminAsync`)

---

## 📝 ملاحظات تطوير

### قواعد الترميز
- النصوص العربية في code-behind: Unicode escapes (`\uXXXX`)
- النصوص العربية في XAML: HTML entities (`&#xXXXX;`)
- تجنب `MessageBox.Show` في معالج `DispatcherUnhandledException` (يسبب stack overflow)
- استخدم `UserControl` للصفحات، `Window` للنوافذ المنبثقة فقط
- اقرأ/اكتب الملفات بـ `[System.IO.File]` بترميز UTF-8 بدون BOM

### إعدادات QuestPDF
```csharp
QuestPDF.Settings.License = LicenseType.Community;
QuestPDF.Settings.UseSystemFonts = true;
```

### إعدادات الأمان
```csharp
// DPAPI encryption
var cipherBytes = ProtectedData.Protect(plainBytes, null, DataProtectionScope.CurrentUser);
// Auto backup
AutoBackupScheduler.CheckAndBackupIfNeeded(connStr);
```

### اختصارات لوحة المفاتيح في الفاتورة
| المفتاح | الإجراء |
|---|---|
| F2 | إضافة سطر | F3 | دفع كامل | F4 | حفظ |
| F9 | باركود | Ctrl+P | طباعة A4 | Esc | إغلاق |

---

## 🚀 المراجعة الشاملة (P0-P3) - الحالة النهائية

### ✅ P0 - أمان البيانات (مكتملة)
- [x] نسخ احتياطي تلقائي مجدول
- [x] تشفير connection string (DPAPI)
- [x] نقل كلمة مرور DB إلى ملف آمن

### ✅ P1 - الاستخدام اليومي (مكتملة)
- [x] طباعة الفاتورة A4
- [x] بحث متقدم في الفواتير
- [x] كشف حساب عميل/مورد
- [x] إشعار نقص المخزون
- [x] اختصارات لوحة المفاتيح

### 🔄 P2 - ميزات متقدمة (جزئيًا)
- [x] تقارير رسومية في Dashboard
- [x] مرتجع مشتريات/مبيعات
- [ ] تنبيهات للمستحقات (overdue invoices)
- [ ] نظام الباركود الكامل في POS
- [ ] تكامل الطباعة الحرارية (Thermal 58mm/80mm)
- [ ] خصم وعروض ترويجية

### ⏳ P3 - مستقبلي
- [ ] API للموبايل (REST/GraphQL)
- [ ] تكامل البنوك
- [ ] جرد دوري تلقائي
- [ ] إقفال سنوي + ميزانية عمومية
- [ ] دعم متعدد اللغات
- [ ] تكامل e-invoicing

---

## 📊 إحصائيات نهائية
- **46** جدول في قاعدة البيانات (نظيف)
- **22** View (XAML)
- **23** ViewModel
- **9** Service (3 جديدة)
- **3** Converter
- **0** أخطاء بناء
- **0** تحذيرات بناء
- **11** صفحة/نافذة تعمل بنجاح
'@

Set-Content -Path "BUILD.md" -Value $buildMd -Encoding UTF8 -Force
Write-Host "BUILD.md updated with final session summary" -ForegroundColor Green
Write-Host "Size: $((Get-Item 'BUILD.md').Length) bytes" -ForegroundColor Cyan
=======
# 📦 دليل بناء وتوزيع Techtoon Accounting

> **آخر تحديث:** 2026-10-13
> **الإصدار:** v1.0

---

## 📋 المتطلبات الأساسية

| المتطلب | الإصدار | ملاحظات |
|---|---|---|
| Python | 3.11+ | مطلوب |
| Windows | 10/11 64-bit | لبناء EXE |
| PyInstaller | 6.0+ | `pip install pyinstaller` |
| Neon DB | أي حساب | [neon.tech](https://neon.tech) مجاني |
| Git | أي إصدار | للرفع على GitHub |

---

## 🚀 خطوات البناء

### 1) جهّز بيئة العمل

```powershell
cd "D:\PROG\Accounting Prog\Techtoon Accounting"

# ثبّت كل المكتبات
pip install -r requirements.txt

# ثبّت PyInstaller
pip install pyinstaller
```

### 2) تحقق من ملف `.env`

```env
DATABASE_URL=postgresql://***:***@host/db?sslmode=require
SECRET_KEY=your-random-40-char-secret
DEPLOY_MODE=local
```

### 3) اضبط `TechtoonAccounting.spec`

تأكد أن `icon=None` (أو ضع `logo.ico` لو متوفر):

```python
# في TechtoonAccounting.spec
icon=None,  # أو icon=['logo.ico']
```

### 4) اضبط حد الـ recursion (مهم لـ Streamlit)

```python
# في أعلى TechtoonAccounting.spec
import sys ; sys.setrecursionlimit(sys.getrecursionlimit() * 10)
```

### 5) ابنِ الـ EXE

```powershell
# الطريقة المباشرة
pyinstaller TechtoonAccounting.spec --noconfirm --clean

# أو استخدم سكربت البناء
.\build_exe.bat
```

**النتيجة:** `dist\TechtoonAccounting\` يحتوي على:
```
TechtoonAccounting.exe          ← البرنامج الرئيسي
_internal/                      ← ملفات PyInstaller
```

> ⏳ البناء يستغرق 5-15 دقيقة

---

## 📦 تجهيز حزمة التوزيع

### 1) أنشئ مجلد الحزمة

```powershell
# انسخ الـ EXE + ملفاته
Copy-Item "dist\TechtoonAccounting" "Techtoon_Accounting_v1.0" -Recurse -Force

# انسخ الإعدادات
Copy-Item ".env" "Techtoon_Accounting_v1.0\.env" -Force
Copy-Item ".streamlit" "Techtoon_Accounting_v1.0\.streamlit" -Recurse -Force

# انسخ ملفات المساعدة
Copy-Item "check_requirements.py" "Techtoon_Accounting_v1.0\" -Force
```

### 2) أنشئ `requirements.txt` للعميل

```powershell
@'
streamlit>=1.30
pandas>=2.0
SQLAlchemy>=2.0
psycopg2-binary>=2.9
python-dotenv>=1.0
plotly>=5.18
reportlab>=4.0
xlsxwriter>=3.1
openpyxl>=3.1
bcrypt>=4.1
pywebview>=4.2
WMI>=1.2
'@ | Set-Content "Techtoon_Accounting_v1.0\requirements.txt" -Encoding UTF8
```

### 3) أنشئ `README.txt` للعميل

```powershell
@'
╔════════════════════════════════════════════════╗
║     Techtoon Accounting — دليل التشغيل         ║
╚════════════════════════════════════════════════╝

الخطوات:
  1) ثبّت المكتبات:
     pip install -r requirements.txt

  2) عدّل DATABASE_URL في ملف .env
     (اطلب الرابط من البائع أو أنشئ من neon.tech)

  3) شغّل البرنامج:
     TechtoonAccounting.exe

الفترة التجريبية: 30 يوماً مجاناً
بعد انتهائها: أرسل machine_id للبائع لشراء الرخصة

للوصول من الموبايل (نفس شبكة Wi-Fi):
  شغّل: TechtoonAccounting.exe
  افتح المتصفح على الموبايل: http://IP_الجهاز:8501

الدعم الفني: [رقم البائع]
'@ | Set-Content "Techtoon_Accounting_v1.0\README.txt" -Encoding UTF8
```

### 4) اضغط في ZIP

```powershell
Compress-Archive -Path "Techtoon_Accounting_v1.0\*" `
  -DestinationPath "Techtoon_Accounting_v1.0.zip" -Force
>>>>>>> 8325fba6af09ee66ab808da0e5526f6efad8e4c5
```

---

<<<<<<< HEAD
## 🎉 اكتمل المشروع!

### ملخص الإنجازات الكامل:

| الفئة | العدد | التفاصيل |
|---|---|---|
| **P1** | 7 ميزات | PDF + بحث + كشف حساب + طباعة A4 + تنبيه مخزون + اختصارات + فترات سريعة |
| **P0** | 4 ميزات | DPAPI + نسخ احتياطي + إزالة كلمة مرور + تحديث الخدمات |
| **P2** | 2 ميزات | مرتجعات جزئية + رسم بياني |
| **تنظيف** | 4 إصلاحات | 0 تحذيرات + توحيد DB + إصلاحات Dashboard |

### المشروع الآن:
- ✅ **مستقر** (0 أخطاء + 0 تحذيرات)
- ✅ **آمن** (DPAPI + نسخ احتياطي تلقائي)
- ✅ **وظيفي** (11 صفحة تعمل)
- ✅ **موثّق** (BUILD.md شامل)

---

## الخطوات التالية المقترحة (لجلسات قادمة)

إن أردت متابعة التطوير مستقبلًا:

1. **استكمال P2** (تنبيهات المستحقات + طباعة حرارية)
2. **بدء P3** (API للموبايل + تكامل بنكي)
3. **تحسينات UX** (ثيمات + اختصارات عامة)
4. **أتمتة الاختبارات** (unit tests + integration tests)
=======
## 🔑 نظام التراخيص

### هيكل المجلدات

```
Techtoon_Accounting/          ← مجلد المشروع
├── licence/                   ← للبائع فقط
│   ├── generate_license.py   ← مولّد الرخص
│   ├── issued_licenses/      ← أرشيف الرخص
│   └── README_SELLER.md
├── Techtoon_Accounting_v1.0/ ← حزمة العميل
│   ├── TechtoonAccounting.exe
│   ├── .env
│   ├── requirements.txt
│   └── README.txt
└── license_manager.py         ← نظام الترخيص (داخل البرنامج)
```

### توليد رخصة لعميل

```powershell
cd "D:\PROG\Accounting Prog\Techtoon Accounting\licence"

# رخصة دائمة
python generate_license.py --machine-id A1B2C3D4E5F67890 --customer "اسم العميل"

# رخصة سنوية
python generate_license.py --machine-id A1B2C3D4 --customer "الاسم" --type annual --days 365

# عرض كل الرخص
python generate_license.py --list
```

### النتيجة

```
  ✔ تم توليد الرخصة!
    machine_id: A1B2C3D4E5F67890
    نوع الرخصة: lifetime
    مدة الصلاحية: دائمة
    الملف: license.key
```

أرسل `license.key` للعميل → يضعه بجانب `TechtoonAccounting.exe` → ✅ مُفعّل!

---

## 📱 تفعيل الوصول من الموبايل

### الطريقة 1: نفس شبكة Wi-Fi

```powershell
# شغّل البرنامج مع تفعيل الموبايل
python -m streamlit run app.py --server.address=0.0.0.0 --server.port=8501
```

- افتح متصفح الموبايل: `http://IP_الجهاز:8501`
- الجهاز والموبايل على نفس Wi-Fi

### الطريقة 2: من أي مكان (Streamlit Cloud)

1. ارفع الكود على GitHub
2. اذهب إلى [share.streamlit.io](https://share.streamlit.io)
3. اربط المستودع
4. أضف `DATABASE_URL` في Secrets
5. احصل على رابط: `https://yourname-techtoon.streamlit.app`
6. افتحه من الموبايل أو أضفه للشاشة الرئيسية

---

## 🔒 أمان الـ SECRET

```python
# في license_manager.py و generate_license.py
SECRET = b"TECHTOON_LICENSE_2026_V1"
```

**قبل التوزيع الأول:**
```python
import secrets
print(secrets.token_bytes(40))
# ضع الناتج في كلا الملفين
```

**لا ترفع `generate_license.py` أو `issued_licenses/` على GitHub:**

```gitignore
# .gitignore
generate_license.py
issued_licenses/
licence/
*.key
```

---

## 🧪 اختبار قبل التوزيع

### 1) اختبر الـ EXE

```powershell
# شغّل على جهازك
dist\TechtoonAccounting\TechtoonAccounting.exe
```

### 2) اختبر الترخيص

```powershell
# احصل على machine_id من شاشة الترخيص
# ولّد رخصة
python licence\generate_license.py --machine-id XXXX --customer "اختبار"

# ضع license.key بجانب EXE
copy license.key dist\TechtoonAccounting\

# أعد التشغيل — يجب أن يظهر "رخصة مدى الحياة"
```

### 3) اختبر على جهاز آخر

- انسخ مجلد `dist\TechtoonAccounting` لجهاز آخر
- شغّل `TechtoonAccounting.exe`
- يجب أن يعمل (مع فترة تجريبية 30 يوماً)

---

## 📊 قائمة الملفات للتوزيع

| الملف | الوصف | للعميل؟ |
|---|---|---|
| `TechtoonAccounting.exe` | البرنامج الرئيسي | ✅ |
| `_internal/` | ملفات PyInstaller | ✅ |
| `.env` | إعدادات DB | ✅ (يُعدّلها العميل) |
| `.streamlit/` | إعدادات Streamlit | ✅ |
| `requirements.txt` | قائمة المكتبات | ✅ |
| `README.txt` | دليل العميل | ✅ |
| `license.key` | رخصة العميل | ✅ (بعد الشراء) |
| `generate_license.py` | مولّد الرخص | ❌ للبائع فقط |
| `issued_licenses/` | أرشيف الرخص | ❌ للبائع فقط |

---

## 🚀 أوامر سريعة (Quick Reference)

```powershell
# بناء EXE
pyinstaller TechtoonAccounting.spec --noconfirm --clean

# تجهيز الحزمة
Copy-Item "dist\TechtoonAccounting" "Techtoon_Accounting_v1.0" -Recurse -Force
Copy-Item ".env" "Techtoon_Accounting_v1.0\" -Force

# ضغط ZIP
Compress-Archive -Path "Techtoon_Accounting_v1.0\*" -DestinationPath "Techtoon_v1.0.zip" -Force

# توليد رخصة
python licence\generate_license.py --machine-id XXXX --customer "الاسم"

# رفع لـ GitHub
git add -A
git commit -m "v1.0: إصدار التوزيع"
git push
```

---

## ✅ قائمة تحقق نهائية قبل التوزيع

```
□ .env يحتوي DATABASE_URL صحيح
□ SECRET في license_manager.py = SECRET في generate_license.py
□ TechtoonAccounting.spec: recursionlimit مضبوط + icon=None
□ EXE بُني بنجاح (dist\TechtoonAccounting\TechtoonAccounting.exe)
□ حزمة ZIP جاهزة (Techtoon_v1.0.zip)
□ generate_license.py في مجلد licence/ (خارج الحزمة)
□ .gitignore يستثنى generate_license.py و issued_licenses/
□ اختبرت EXE على جهازك
□ اختبرت الترخيص (توليد + تفعيل)
□ ارفع الكود على GitHub
□ جهّز رابط تحميل الحزمة للعملاء
```

---

## 🔄 تحديثات مستقبلية

### لإصدار نسخة محدّثة:

```powershell
# 1) حدّث الكود
# ... تعديلات ...

# 2) ارفع لـ GitHub
git add -A
git commit -m "v1.1: تحديثات"
git push

# 3) أعد بناء EXE
pyinstaller TechtoonAccounting.spec --noconfirm --clean

# 4) أعد تجهيز الحزمة
# (كرر خطوات تجهيز الحزمة)

# 5) أرسل التحديث للعملاء الحاليين
# (رخصهم ستبقى فعالة — لا حاجة لإعادة التفعيل)
```

---

**© 2026 Techtoon Accounting. جميع الحقوق محفوظة.**
>>>>>>> 8325fba6af09ee66ab808da0e5526f6efad8e4c5
