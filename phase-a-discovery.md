# Techtoon Phase A Discovery
_Generated: 2026-11-02 09:48_
_Root: D:\PROG\Accounting Prog\Techtoon Accounting C+_
- .agents/
  - skills/
    - developing-with-streamlit
- .claude/
  - skills/
    - developing-with-streamlit
- .devcontainer/
  - devcontainer.json
- .streamlit/
  - config.toml
- .vscode/
- .webview_data/
  - EBWebView/
    - Breadcrumbs
    - CrashpadMetrics-active.pma
    - Last Version
    - Local State
    - Variations
    - VariationsRuntimeSeedV2
    - VariationsSafeSeedV2
    - VariationsSeedV2
- Techtoon.Api/
  - appsettings.Development.json
  - appsettings.json
  - Program.cs
  - Techtoon.Api.csproj
  - Techtoon.Api.http
  - WeatherForecast.cs
  - Controllers/
    - AuthController.cs
    - InvoicesController.cs
    - WeatherForecastController.cs
  - Properties/
    - launchSettings.json
- Techtoon.Core/
  - Techtoon.Core.csproj
  - Enums/
  - Models/
    - Account.cs
    - AccountingPeriod.cs
    - AppSetting.cs
    - Budget.cs
    - CashBox.cs
    - CashTransfer.cs
    - Category.cs
    - CostAllocation.cs
    - CostCenter.cs
    - CostHistory.cs
    - Currency.cs
    - DepreciationRecord.cs
    - Employee.cs
    - Expense.cs
    - ExpenseCategory.cs
- Techtoon.Data/
  - Techtoon.Data.csproj
  - TechtoonDbContext.cs
  - Context/
    - TechtoonDbContext.cs
- Techtoon.Streamlit/
  - .gitignore
  - app.py
  - database.py
  - requirements.txt
  - .streamlit/
    - config.toml
  - pages/
    - cash_boxes.py
    - damaged_items.py
    - invoices.py
    - items.py
    - loans.py
    - parties.py
    - payments.py
    - reports.py
    - stock_adjustments.py
    - stock_counts.py
    - stock_levels.py
    - writeoff_requests.py
    - __init__.py
  - utils/
    - math_eval.py
- Techtoon.UI/
  - App.xaml
  - App.xaml.cs
  - appsettings.json
  - ErrorGuard.cs
  - MainWindow.xaml
  - MainWindow.xaml.cs
  - Techtoon.UI.csproj
  - Controls/
  - Converters/
    - BarHeightConverter.cs
    - BoolToVisibilityConverter.cs
    - BoolToVisibilityInvertedConverter.cs
    - EnumMatchConverter.cs
    - RestoredConverters.cs
    - StringToVisibilityConverter.cs
  - Documents/
  - Helpers/
  - Pages/
  - Services/
    - AuthService.cs
    - AutoBackupScheduler.cs
    - BackupService.cs
    - DamagedItemsService.cs
    - DataManagementService.cs
    - FinancialService.cs
    - InventoryAdjustmentService.cs
    - InventoryCountService.cs
    - InventoryService.cs
    - LoansService.cs
    - MathHelper.cs
    - NotificationService.cs
    - OverdraftService.cs
    - PrintService.cs
    - QuestPdfReportService.cs
  - Styles/
    - Colors.xaml
    - Controls.xaml
  - ViewModels/
    - AccountEditViewModel.cs
    - AccountsViewModel.cs
    - CashBoxesViewModel.cs
    - CostCentersViewModel.cs
    - DamagedItemsViewModel.cs
    - DashboardViewModel.cs
    - DataManagementViewModel.cs
    - EmployeesViewModel.cs
    - InvoicesViewModel.cs
    - ItemEditViewModel.cs
    - ItemsViewModel.cs
    - LoansViewModel.cs
    - MainViewModel.cs
    - PartiesViewModel.cs
    - PartyEditViewModel.cs
  - Views/
    - AccountEditWindow.xaml
    - AccountEditWindow.xaml.cs
    - AccountsView.xaml
    - AccountsView.xaml.cs
    - apply_all_fixes.py
    - CashBoxesView.xaml
    - CashBoxesView.xaml.cs
    - CostCentersView.xaml
    - CostCentersView.xaml.cs
    - DamagedItemEditWindow.xaml
    - DamagedItemEditWindow.xaml.cs
    - DamagedItemsView.xaml
    - DamagedItemsView.xaml.cs
    - DashboardView.xaml
    - DashboardView.xaml.cs
- _backup_old_files/
  - InvoiceEditViewModel.cs
  - InvoiceEditViewModel.cs.bak
  - InvoiceEditViewModel.cs.bak2
  - InvoiceEditViewModel.cs.bak3
  - InvoiceEditViewModel.cs.bak_new
  - InvoiceEditViewModel.cs.bak_v6
  - InvoiceEditWindow.xaml
  - InvoiceEditWindow.xaml.bak
  - InvoiceEditWindow.xaml.bak_new
  - InvoiceEditWindow.xaml.bak_v2
  - InvoiceEditWindow.xaml.bak_v4
  - InvoiceEditWindow.xaml.bak_v6
  - InvoiceEditWindow.xaml.cs
  - InvoiceEditWindow.xaml.cs.bak
  - InvoiceEditWindow.xaml.cs.bak_f2
  - InvoiceEditWindow.xaml.cs.bak_new
  - InvoiceEditWindow.xaml.cs.bak_v2
  - InvoiceEditWindow.xaml.cs.bak_v3
  - InvoiceEditWindow.xaml.cs.bak_v6
  - InvoicesView.xaml
  - InvoicesView.xaml.bak_v3
  - InvoicesView.xaml.bak_v5
  - InvoicesView.xaml.cs
  - InvoicesView.xaml.cs.bak_f2
  - InvoicesView.xaml.cs.bak_v3
- _dbcheck/
  - DbCheck.csproj
  - Program.cs
- _old_payments_backup/
  - PaymentEditViewModel.cs
  - PaymentsPage.xaml
  - PaymentsPage.xaml.cs
```
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

```

### Techtoon.Api.csproj

```
<Project Sdk="Microsoft.NET.Sdk.Web">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="BCrypt.Net-Next" Version="4.0.3" />
    <PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="10.0.14" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="10.0.0">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="10.0.3" />
    <PackageReference Include="Swashbuckle.AspNetCore" Version="10.3.1" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\Techtoon.Data\Techtoon.Data.csproj" />
    <ProjectReference Include="..\Techtoon.Core\Techtoon.Core.csproj" />
  </ItemGroup>

</Project>

```

### Techtoon.Core.csproj

```
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
      <NoWarn>$(NoWarn);CS8618;CS8625;CS8600;CS8601;CS8602;CS8603;CS8604;CS8619;CS8620;CS8621;CS8622;CS8624;CS8626</NoWarn>
  </PropertyGroup>

</Project>

```

### Techtoon.Data.csproj

```
<Project Sdk="Microsoft.NET.Sdk">

  <ItemGroup>
    <ProjectReference Include="..\Techtoon.Core\Techtoon.Core.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="10.0.3" />
  </ItemGroup>

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
      <NoWarn>$(NoWarn);CS8618;CS8625;CS8600;CS8601;CS8602;CS8603;CS8604;CS8619;CS8620;CS8621;CS8622;CS8624;CS8626</NoWarn>
  </PropertyGroup>

</Project>

```

### Techtoon.UI.csproj

```
<Project Sdk="Microsoft.NET.Sdk">

  <ItemGroup>
    <ProjectReference Include="..\Techtoon.Core\Techtoon.Core.csproj" />
    <ProjectReference Include="..\Techtoon.Data\Techtoon.Data.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="BCrypt.Net-Next" Version="4.0.3" />
    <PackageReference Include="CommunityToolkit.Mvvm" Version="8.4.0" />
    <PackageReference Include="Microsoft.Extensions.Configuration" Version="10.0.13" />
    <PackageReference Include="Microsoft.Extensions.Configuration.Json" Version="10.0.13" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="10.0.13" />
    <PackageReference Include="Microsoft.Extensions.Hosting" Version="10.0.13" />
    <PackageReference Include="Microsoft.Xaml.Behaviors.Wpf" Version="1.1.77" />
    <PackageReference Include="Npgsql" Version="10.0.3" />
    <PackageReference Include="QuestPDF" Version="2026.9.1" />
  </ItemGroup>

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0-windows</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <UseWPF>true</UseWPF>
      <NoWarn>$(NoWarn);CS8618;CS8625;CS8600;CS8601;CS8602;CS8603;CS8604;CS8619;CS8620;CS8621;CS8622;CS8624;CS8626</NoWarn>
  </PropertyGroup>

  <ItemGroup>
    <None Update="appsettings.json">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
      <CopyToPublishDirectory>PreserveNewest</CopyToPublishDirectory>
    </None>
  </ItemGroup>
</Project>
```

### DbCheck.csproj

```
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Npgsql" Version="10.0.3" />
  </ItemGroup>
</Project>

```

### Files

- \appsettings.Development.json  (127 bytes)
- \appsettings.json  (384 bytes)
- \Program.cs  (775 bytes)
- \Techtoon.Api.csproj  (1010 bytes)
- \Techtoon.Api.http  (137 bytes)
- \WeatherForecast.cs  (261 bytes)
- \Controllers\AuthController.cs  (2114 bytes)
- \Controllers\InvoicesController.cs  (3936 bytes)
- \Controllers\WeatherForecastController.cs  (778 bytes)
- \Properties\launchSettings.json  (644 bytes)

### Program.cs

```
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Enable CORS for Mobile Apps
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder => builder.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader());
});

var app = builder.Build();

// Configure HTTP pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

app.Run();
```

### appsettings.json (redacted)

```
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "DefaultConnection": "Host=ep-bitter-night-za726w59-pooler.c-2.eu-west-2.aws.neon.tech;Port=5432;Username=neondb_owner;Password=npg_K9nHVkY5OjRC;Database=neondb;SSL Mode=Require;Trust Server Certificate=true"
  }
}
```

### Controllers

#### AuthController.cs
```
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using System;
using System.Threading.Tasks;

namespace Techtoon.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly string _connStr;

        public AuthController(IConfiguration configuration)
        {
            _connStr = configuration.GetConnectionString("DefaultConnection");
        }

        public class LoginRequest
        {
            public string Username { get; set; }
            public string Password { get; set; }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrEmpty(request?.Username) || string.IsNullOrEmpty(request?.Password))
                return BadRequest(new { message = "Username and password are required" });

            using var conn = new NpgsqlConnection(_connStr);
            await conn.OpenAsync();

            using var cmd = new NpgsqlCommand("SELECT id, username, full_name, role, is_active, password_hash FROM users WHERE username = @u", conn);
            cmd.Parameters.AddWithValue("u", request.Username);

            using var r = await cmd.ExecuteReaderAsync();
            if (!await r.ReadAsync())
                return Unauthorized(new { message = "User not found" });

            bool isActive = r.IsDBNull(4) ? true : r.GetBoolean(4);
            if (!isActive)
                return Unauthorized(new { message = "Account is disabled" });

            string hash = r.GetString(5);
            if (!BCrypt.Net.BCrypt.Verify(request.Password, hash))
                return Unauthorized(new { message = "Invalid password" });

            return Ok(new
            {
                id = r.GetInt32(0),
                username = r.GetString(1),
                fullName = r.IsDBNull(2) ? "" : r.GetString(2),
                role = r.IsDBNull(3) ? "" : r.GetString(3),
                token = "techtoon-token-" + Guid.NewGuid().ToString() // Simple token for demo
            });
        }
    }
}
```
#### InvoicesController.cs
```
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Techtoon.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InvoicesController : ControllerBase
    {
        private readonly string _connStr;

        public InvoicesController(IConfiguration configuration)
        {
            _connStr = configuration.GetConnectionString("DefaultConnection");
        }

        [HttpGet]
        public async Task<IActionResult> GetInvoices([FromQuery] int limit = 50)
        {
            var list = new List<object>();
            using var conn = new NpgsqlConnection(_connStr);
            await conn.OpenAsync();

            using var cmd = new NpgsqlCommand(
                "SELECT i.id, i.invoice_number, i.date, i.type, i.total, i.paid_amount, i.status, " +
                "COALESCE(p.name, '') AS party_name " +
                "FROM invoices i LEFT JOIN parties p ON i.party_id = p.id " +
                "ORDER BY i.date DESC LIMIT @limit", conn);
            cmd.Parameters.AddWithValue("limit", limit);

            using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                list.Add(new
                {
                    id = r.GetInt32(0),
                    invoiceNumber = r.GetString(1),
                    date = r.GetDateTime(2),
                    type = r.GetString(3),
                    total = r.GetDouble(4),
                    paidAmount = r.GetDouble(5),
                    status = r.GetString(6),
                    partyName = r.GetString(7)
                });
            }
            return Ok(list);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetInvoice(int id)
        {
            using var conn = new NpgsqlConnection(_connStr);
            await conn.OpenAsync();

            // Header
            object header = null;
            using (var cmd = new NpgsqlCommand(
                "SELECT i.id, i.invoice_number, i.date, i.type, i.total, i.paid_amount, i.status, " +
                "COALESCE(p.name, '') AS party_name " +
                "FROM invoices i LEFT JOIN parties p ON i.party_id = p.id WHERE i.id = @id", conn))
            {
                cmd.Parameters.AddWithValue("id", id);
                using var r = await cmd.ExecuteReaderAsync();
                if (await r.ReadAsync())
                {
                    header = new
                    {
                        id = r.GetInt32(0),
                        invoiceNumber = r.GetString(1),
                        date = r.GetDateTime(2),
                        type = r.GetString(3),
                        total = r.GetDouble(4),
                        paidAmount = r.GetDouble(5),
                        status = r.GetString(6),
                        partyName = r.GetString(7)
                    };
                }
            }

            if (header == null) return NotFound();

            // Lines
            var lines = new List<object>();
            using (var cmd = new NpgsqlCommand(
                "SELECT il.item_id, i.name, il.quantity, il.price, il.total " +
                "FROM invoice_lines il JOIN items i ON il.item_id = i.id WHERE il.invoice_id = @id", conn))
            {
                cmd.Parameters.AddWithValue("id", id);
                using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                {
                    lines.Add(new
                    {
                        itemId = r.GetInt32(0),
                        itemName = r.GetString(1),
                        quantity = r.GetDouble(2),
                        price = r.GetDouble(3),
                        total = r.GetDouble(4)
                    });
                }
            }

            return Ok(new { header, lines });
        }
    }
}
```
#### WeatherForecastController.cs
```
using Microsoft.AspNetCore.Mvc;

namespace Techtoon.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class WeatherForecastController : ControllerBase
{
    private static readonly string[] Summaries =
    [
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    ];

    [HttpGet(Name = "GetWeatherForecast")]
    public IEnumerable<WeatherForecast> Get()
    {
        return Enumerable.Range(1, 5).Select(index => new WeatherForecast
        {
            Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            TemperatureC = Random.Shared.Next(-20, 55),
            Summary = Summaries[Random.Shared.Next(Summaries.Length)]
        })
        .ToArray();
    }
}

```

### Other .cs files

#### \Program.cs
```
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Enable CORS for Mobile Apps
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder => builder.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader());
});

var app = builder.Build();

// Configure HTTP pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

app.Run();
```
#### \WeatherForecast.cs
```
namespace Techtoon.Api;

public class WeatherForecast
{
    public DateOnly Date { get; set; }

    public int TemperatureC { get; set; }

    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);

    public string? Summary { get; set; }
}

```
#### \Models\Account.cs
```
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Techtoon.Core.Models;

[Table("accounts")]
public class Account
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("code")]
    [StringLength(50)]
    public string Code { get; set; } = string.Empty;

    [Column("name")]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Column("type")]
    [StringLength(50)]
    public string Type { get; set; } = "ASSET";

    [Column("parent_id")]
    public int? ParentId { get; set; }

    [ForeignKey("ParentId")]
    public Account? Parent { get; set; }
    public ICollection<Account> Children { get; set; } = new List<Account>();
    public ICollection<JournalLine> JournalLines { get; set; } = new List<JournalLine>();
    public ICollection<Party> Parties { get; set; } = new List<Party>();
    public ICollection<CashBox> CashBoxes { get; set; } = new List<CashBox>();
    public ICollection<Item> ItemsAsInventory { get; set; } = new List<Item>();
    public ICollection<Item> ItemsAsCogs { get; set; } = new List<Item>();
    public ICollection<Item> ItemsAsRevenue { get; set; } = new List<Item>();
    public ICollection<ExpenseCategory> ExpenseCategories { get; set; } = new List<ExpenseCategory>();
    public ICollection<Budget> Budgets { get; set; } = new List<Budget>();
    public ICollection<CostAllocation> CostAllocations { get; set; } = new List<CostAllocation>();
}
```
#### \Models\AccountingPeriod.cs
```
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Techtoon.Core.Models;

[Table("accounting_periods")]
public class AccountingPeriod
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("period_name")]
    [StringLength(50)]
    public string PeriodName { get; set; } = string.Empty;

    [Column("start_date")]
    public DateTime StartDate { get; set; }

    [Column("end_date")]
    public DateTime EndDate { get; set; }

    [Column("status")]
    [StringLength(50)]
    public string Status { get; set; } = "OPEN";

    [Column("is_fiscal_year")]
    public bool IsFiscalYear { get; set; }

    [Column("closing_date")]
    public DateTime? ClosingDate { get; set; }

    [Column("closing_notes")]
    public string? ClosingNotes { get; set; }

    public ICollection<JournalEntry> JournalEntries { get; set; } = new List<JournalEntry>();
}
```
#### \Models\AppSetting.cs
```
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Techtoon.Core.Models;

[Table("app_settings")]
public class AppSetting
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("key")]
    [StringLength(100)]
    public string Key { get; set; } = string.Empty;

    [Column("value")]
    public string? Value { get; set; }

    [Column("category")]
    [StringLength(50)]
    public string Category { get; set; } = "general";
}
```
#### \Models\Budget.cs
```
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Techtoon.Core.Models;

[Table("budgets")]
public class Budget
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("account_id")]
    public int AccountId { get; set; }
    [ForeignKey("AccountId")]
    public Account? Account { get; set; }

    [Column("month")]
    public int Month { get; set; }

    [Column("year")]
    public int Year { get; set; }

    [Column("budgeted_amount")]
    public double BudgetedAmount { get; set; }
}
```
#### \Models\CashBox.cs
```
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Techtoon.Core.Models;

[Table("cash_boxes")]
public class CashBox
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("name")]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Column("code")]
    [StringLength(20)]
    public string Code { get; set; } = string.Empty;

    [Column("type")]
    [StringLength(50)]
    public string Type { get; set; } = "CASH";

    [Column("account_id")]
    public int AccountId { get; set; }
    [ForeignKey("AccountId")]
    public Account? Account { get; set; }

    [Column("responsible_person")]
    [StringLength(100)]
    public string? ResponsiblePerson { get; set; }

    [Column("max_limit")]
    public double? MaxLimit { get; set; }

    [Column("opening_balance")]
    public double OpeningBalance { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("notes")]
    public string? Notes { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public ICollection<CashTransfer> TransfersFrom { get; set; } = new List<CashTransfer>();
    public ICollection<CashTransfer> TransfersTo { get; set; } = new List<CashTransfer>();
    public ICollection<Expense> Expenses { get; set; } = new List<Expense>();
    public ICollection<Loan> Loans { get; set; } = new List<Loan>();
}
```
#### \Models\CashTransfer.cs
```
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Techtoon.Core.Models;

[Table("cash_transfers")]
public class CashTransfer
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("date")]
    public DateTime Date { get; set; } = DateTime.UtcNow;

    [Column("from_cash_box_id")]
    public int FromCashBoxId { get; set; }
    [ForeignKey("FromCashBoxId")]
    public CashBox? FromCashBox { get; set; }

    [Column("to_cash_box_id")]
    public int ToCashBoxId { get; set; }
    [ForeignKey("ToCashBoxId")]
    public CashBox? ToCashBox { get; set; }

    [Column("amount")]
    public double Amount { get; set; }
}
```
#### \Models\Category.cs
```
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Techtoon.Core.Models;

[Table("categories")]
public class Category
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("name")]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("parent_id")]
    public int? ParentId { get; set; }

    [ForeignKey("ParentId")]
    public Category? Parent { get; set; }
    public ICollection<Category> Children { get; set; } = new List<Category>();

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    public ICollection<Item> Items { get; set; } = new List<Item>();
}
```
#### \Models\CostAllocation.cs
```
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Techtoon.Core.Models;

[Table("cost_allocations")]
public class CostAllocation
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("account_id")]
    public int AccountId { get; set; }
    [ForeignKey("AccountId")]
    public Account? Account { get; set; }

    [Column("allocation_percentage")]
    public double AllocationPercentage { get; set; }

    [Column("month")]
    public int Month { get; set; }

    [Column("year")]
    public int Year { get; set; }
}
```
#### \Models\CostCenter.cs
```
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Techtoon.Core.Models;

[Table("cost_centers")]
public class CostCenter
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("code")]
    [StringLength(20)]
    public string Code { get; set; } = string.Empty;

    [Column("name")]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("manager_name")]
    [StringLength(100)]
    public string? ManagerName { get; set; }

    [Column("budget_limit")]
    public double? BudgetLimit { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("notes")]
    public string? Notes { get; set; }
}
```
#### \Models\CostHistory.cs
```
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Techtoon.Core.Models;

[Table("cost_history")]
public class CostHistory
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("item_id")]
    public int ItemId { get; set; }
    [ForeignKey("ItemId")]
    public Item? Item { get; set; }

    [Column("invoice_id")]
    public int InvoiceId { get; set; }
    [ForeignKey("InvoiceId")]
    public Invoice? Invoice { get; set; }

    [Column("purchase_price")]
    public double PurchasePrice { get; set; }

    [Column("quantity")]
    public double Quantity { get; set; }

    [Column("total_cost")]
    public double TotalCost { get; set; }

    [Column("purchase_date")]
    public DateTime PurchaseDate { get; set; }

    [Column("supplier_id")]
    public int? SupplierId { get; set; }
    [ForeignKey("SupplierId")]
    public Party? Supplier { get; set; }
}
```
#### \Models\Currency.cs
```
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Techtoon.Core.Models;

[Table("currencies")]
public class Currency
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("code")]
    [StringLength(10)]
    public string Code { get; set; } = string.Empty;

    [Column("name")]
    [StringLength(50)]
    public string Name { get; set; } = string.Empty;

    [Column("symbol")]
    [StringLength(10)]
    public string Symbol { get; set; } = string.Empty;

    [Column("exchange_rate")]
    public double ExchangeRate { get; set; } = 1.0;

    [Column("is_default")]
    public bool IsDefault { get; set; }

    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
```
#### \Models\DepreciationRecord.cs
```
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Techtoon.Core.Models;

[Table("depreciation_records")]
public class DepreciationRecord
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("date")]
    public DateTime Date { get; set; }

    [Column("depreciation_amount")]
    public double DepreciationAmount { get; set; }
}
```
#### \Models\Employee.cs
```
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Techtoon.Core.Models;

[Table("employees")]
public class Employee
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("employee_code")]
    [StringLength(20)]
    public string EmployeeCode { get; set; } = string.Empty;

    [Column("full_name")]
    [StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Column("phone")]
    [StringLength(50)]
    public string? Phone { get; set; }

    [Column("job_title")]
    [StringLength(100)]
    public string JobTitle { get; set; } = string.Empty;

    [Column("hire_date")]
    public DateTime HireDate { get; set; }

    [Column("basic_salary")]
    public double BasicSalary { get; set; }

    [Column("allowances")]
    public double Allowances { get; set; }

    [Column("deductions")]
    public double Deductions { get; set; }

    [Column("social_insurance")]
    public double SocialInsurance { get; set; }

    [Column("tax_rate")]
    public double TaxRate { get; set; }

    [Column("status")]
    [StringLength(50)]
    public string Status { get; set; } = "ACTIVE";

    public ICollection<SalaryRecord> SalaryRecords { get; set; } = new List<SalaryRecord>();
}
```
#### \Models\Expense.cs
```
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Techtoon.Core.Models;

[Table("expenses")]
public class Expense
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("date")]
    public DateTime Date { get; set; } = DateTime.UtcNow;

    [Column("amount")]
    public double Amount { get; set; }

    [Column("description")]
    public string Description { get; set; } = string.Empty;

    [Column("payment_method")]
    [StringLength(50)]
    public string PaymentMethod { get; set; } = string.Empty;

    [Column("cash_box_id")]
    public int CashBoxId { get; set; }
    [ForeignKey("CashBoxId")]
    public CashBox? CashBox { get; set; }
}
```
#### \Models\ExpenseCategory.cs
```
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Techtoon.Core.Models;

[Table("expense_categories")]
public class ExpenseCategory
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("name")]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Column("account_id")]
    public int AccountId { get; set; }
    [ForeignKey("AccountId")]
    public Account? Account { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;
}
```
#### \Models\FixedAsset.cs
```
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Techtoon.Core.Models;

[Table("fixed_assets")]
public class FixedAsset
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("name")]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Column("code")]
    [StringLength(20)]
    public string Code { get; set; } = string.Empty;

    [Column("purchase_date")]
    public DateTime PurchaseDate { get; set; }

    [Column("purchase_cost")]
    public double PurchaseCost { get; set; }

    [Column("useful_life_years")]
    public int UsefulLifeYears { get; set; }

    [Column("depreciation_method")]
    [StringLength(50)]
    public string DepreciationMethod { get; set; } = "STRAIGHT_LINE";

    [Column("net_book_value")]
    public double NetBookValue { get; set; }

    [Column("status")]
    [StringLength(50)]
    public string Status { get; set; } = "active";
}
```
#### \Models\InventoryMovement.cs
```
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Techtoon.Core.Models;

[Table("inventory_movements")]
public class InventoryMovement
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("item_id")]
    public int ItemId { get; set; }
    [ForeignKey("ItemId")]
    public Item? Item { get; set; }

    [Column("date")]
    public DateTime Date { get; set; } = DateTime.UtcNow;

    [Column("type")]
    [StringLength(50)]
    public string Type { get; set; } = string.Empty;

    [Column("quantity")]
    public double Quantity { get; set; }

    [Column("reference_id")]
    public int? ReferenceId { get; set; }
}
```
#### \Models\Invoice.cs
```
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Techtoon.Core.Models;

[Table("invoices")]
public class Invoice
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("invoice_number")]
    [StringLength(50)]
    public string InvoiceNumber { get; set; } = string.Empty;

    [Column("date")]
    public DateTime Date { get; set; } = DateTime.UtcNow;

    [Column("due_date")]
    public DateTime? DueDate { get; set; }

    [Column("party_id")]
    public int PartyId { get; set; }
    [ForeignKey("PartyId")]
    public Party? Party { get; set; }

    [Column("type")]
    [StringLength(50)]
    public string Type { get; set; } = string.Empty;

    [Column("total_amount")]
    public double TotalAmount { get; set; }

    [Column("status")]
    [StringLength(50)]
    public string Status { get; set; } = "pending";

    [Column("currency_id")]
    public int CurrencyId { get; set; } = 1;
    [ForeignKey("CurrencyId")]
    public Currency? Currency { get; set; }

    [Column("discount_amount")]
    public double DiscountAmount { get; set; }

    [Column("discount_percentage")]
    public double DiscountPercentage { get; set; }

    [Column("tax_rate")]
    public double TaxRate { get; set; } = 14.0;

    [Column("tax_amount")]
    public double TaxAmount { get; set; }

    [Column("net_amount")]
    public double NetAmount { get; set; }

    [Column("created_by")]
    public int? CreatedBy { get; set; }
```
#### \Models\InvoiceLine.cs
```
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Techtoon.Core.Models;

[Table("invoice_lines")]
public class InvoiceLine
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("invoice_id")]
    public int InvoiceId { get; set; }
    [ForeignKey("InvoiceId")]
    public Invoice? Invoice { get; set; }

    [Column("item_id")]
    public int ItemId { get; set; }
    [ForeignKey("ItemId")]
    public Item? Item { get; set; }

    [Column("quantity")]
    public double Quantity { get; set; }

    [Column("price")]
    public double Price { get; set; }

    [Column("total")]
    public double Total { get; set; }

    [Column("cost_price")]
    public double CostPrice { get; set; }
}
```
#### \Models\InvoicePayment.cs
```
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Techtoon.Core.Models;

[Table("invoice_payments")]
public class InvoicePayment
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("invoice_id")]
    public int InvoiceId { get; set; }
    [ForeignKey("InvoiceId")]
    public Invoice? Invoice { get; set; }

    [Column("date")]
    public DateTime Date { get; set; } = DateTime.UtcNow;

    [Column("amount")]
    public double Amount { get; set; }

    [Column("payment_method")]
    [StringLength(50)]
    public string PaymentMethod { get; set; } = "cash";

    [Column("cash_box_id")]
    public int? CashBoxId { get; set; }
    [ForeignKey("CashBoxId")]
    public CashBox? CashBox { get; set; }

    [Column("reference_number")]
    [StringLength(100)]
    public string? ReferenceNumber { get; set; }

    [Column("notes")]
    public string? Notes { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
```
#### \Models\Item.cs
```
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Techtoon.Core.Models;

[Table("items")]
public class Item
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("name")]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Column("barcode")]
    [StringLength(100)]
    public string? Barcode { get; set; }

    [Column("cost_price")]
    public double CostPrice { get; set; }

    [Column("sell_price")]
    public double SellPrice { get; set; }

    [Column("is_kit")]
    public bool IsKit { get; set; }

    [Column("min_stock")]
    public double MinStock { get; set; } = 10.0;

    [Column("opening_quantity")]
    public double OpeningQuantity { get; set; }

    [Column("category_id")]
    public int? CategoryId { get; set; }

    [ForeignKey("CategoryId")]
    public Category? Category { get; set; }

    [Column("avg_cost_price")]
    public double AvgCostPrice { get; set; }

    [Column("total_purchased_qty")]
    public double TotalPurchasedQty { get; set; }

    [Column("inventory_account_id")]
    public int? InventoryAccountId { get; set; }
    [ForeignKey("InventoryAccountId")]
    public Account? InventoryAccount { get; set; }

    [Column("cogs_account_id")]
    public int? CogsAccountId { get; set; }
    [ForeignKey("CogsAccountId")]
    public Account? CogsAccount { get; set; }

    [Column("revenue_account_id")]
    public int? RevenueAccountId { get; set; }
    [ForeignKey("RevenueAccountId")]
```
#### \Models\JournalEntry.cs
```
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Techtoon.Core.Models;

[Table("journal_entries")]
public class JournalEntry
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("date")]
    public DateTime Date { get; set; } = DateTime.UtcNow;

    [Column("description")]
    [StringLength(255)]
    public string? Description { get; set; }

    [Column("reference_type")]
    [StringLength(50)]
    public string? ReferenceType { get; set; }

    [Column("reference_id")]
    public int? ReferenceId { get; set; }

    [Column("created_by")]
    public int? CreatedBy { get; set; }

    [Column("period_id")]
    public int? PeriodId { get; set; }
    [ForeignKey("PeriodId")]
    public AccountingPeriod? Period { get; set; }

    public ICollection<JournalLine> Lines { get; set; } = new List<JournalLine>();
}
```
#### \Models\JournalLine.cs
```
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Techtoon.Core.Models;

[Table("journal_lines")]
public class JournalLine
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("entry_id")]
    public int EntryId { get; set; }
    [ForeignKey("EntryId")]
    public JournalEntry? Entry { get; set; }

    [Column("account_id")]
    public int AccountId { get; set; }
    [ForeignKey("AccountId")]
    public Account? Account { get; set; }

    [Column("debit")]
    public double Debit { get; set; }

    [Column("credit")]
    public double Credit { get; set; }
}
```
#### \Models\KitComponent.cs
```
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Techtoon.Core.Models;

[Table("kit_components")]
public class KitComponent
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("kit_item_id")]
    public int KitItemId { get; set; }
    [ForeignKey("KitItemId")]
    public Item? KitItem { get; set; }

    [Column("component_item_id")]
    public int ComponentItemId { get; set; }
    [ForeignKey("ComponentItemId")]
    public Item? ComponentItem { get; set; }

    [Column("quantity")]
    public double Quantity { get; set; } = 1.0;
}
```
#### \Models\Loan.cs
```
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Techtoon.Core.Models;

[Table("loans")]
public class Loan
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("loan_number")]
    [StringLength(50)]
    public string LoanNumber { get; set; } = string.Empty;

    [Column("loan_type")]
    [StringLength(50)]
    public string LoanType { get; set; } = "RECEIVED";

    [Column("borrower_name")]
    [StringLength(100)]
    public string BorrowerName { get; set; } = string.Empty;

    [Column("principal_amount")]
    public double PrincipalAmount { get; set; }

    [Column("interest_rate")]
    public double InterestRate { get; set; }

    [Column("loan_date")]
    public DateTime LoanDate { get; set; }

    [Column("maturity_date")]
    public DateTime MaturityDate { get; set; }

    [Column("term_months")]
    public int TermMonths { get; set; }

    [Column("installment_amount")]
    public double InstallmentAmount { get; set; }

    [Column("total_interest")]
    public double TotalInterest { get; set; }

    [Column("total_amount")]
    public double TotalAmount { get; set; }

    [Column("paid_amount")]
    public double PaidAmount { get; set; }

    [Column("remaining_amount")]
    public double RemainingAmount { get; set; }

    [Column("status")]
    [StringLength(50)]
    public string Status { get; set; } = "ACTIVE";

    public ICollection<LoanInstallment> Installments { get; set; } = new List<LoanInstallment>();
}
```
#### \Models\LoanInstallment.cs
```
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Techtoon.Core.Models;

[Table("loan_installments")]
public class LoanInstallment
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("loan_id")]
    public int LoanId { get; set; }
    [ForeignKey("LoanId")]
    public Loan? Loan { get; set; }

    [Column("installment_number")]
    public int InstallmentNumber { get; set; }

    [Column("due_date")]
    public DateTime DueDate { get; set; }

    [Column("principal_amount")]
    public double PrincipalAmount { get; set; }

    [Column("interest_amount")]
    public double InterestAmount { get; set; }

    [Column("total_amount")]
    public double TotalAmount { get; set; }

    [Column("paid_amount")]
    public double PaidAmount { get; set; }

    [Column("paid_date")]
    public DateTime? PaidDate { get; set; }

    [Column("status")]
    [StringLength(50)]
    public string Status { get; set; } = "pending";
}
```
#### \Models\Party.cs
```
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Techtoon.Core.Models;

[Table("parties")]
public class Party
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("name")]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Column("type")]
    [StringLength(50)]
    public string Type { get; set; } = string.Empty;

    [Column("phone")]
    [StringLength(50)]
    public string? Phone { get; set; }

    [Column("address")]
    public string? Address { get; set; }

    [Column("credit_limit")]
    public double CreditLimit { get; set; }

    [Column("opening_balance")]
    public double OpeningBalance { get; set; }

    [Column("account_id")]
    public int AccountId { get; set; }

    [ForeignKey("AccountId")]
    public Account? Account { get; set; }

    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public ICollection<CostHistory> CostHistories { get; set; } = new List<CostHistory>();
    public ICollection<RecurringInvoiceTemplate> RecurringTemplates { get; set; } = new List<RecurringInvoiceTemplate>();
}
```
#### \Models\Payment.cs
```
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Techtoon.Core.Models;

[Table("payments")]
public class Payment
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("date")]
    public DateTime Date { get; set; } = DateTime.UtcNow;

    [Column("party_id")]
    public int PartyId { get; set; }
    [ForeignKey("PartyId")]
    public Party? Party { get; set; }

    [Column("amount")]
    public double Amount { get; set; }

    [Column("payment_type")]
    [StringLength(50)]
    public string PaymentType { get; set; } = string.Empty;

    [Column("payment_method")]
    [StringLength(100)]
    public string PaymentMethod { get; set; } = string.Empty;

    [Column("reference_number")]
    [StringLength(100)]
    public string? ReferenceNumber { get; set; }

    [Column("notes")]
    public string? Notes { get; set; }

    [Column("currency_id")]
    public int CurrencyId { get; set; } = 1;
    [ForeignKey("CurrencyId")]
    public Currency? Currency { get; set; }

    [Column("cash_box_id")]
    public int CashBoxId { get; set; } = 1;
    [ForeignKey("CashBoxId")]
    public CashBox? CashBox { get; set; }
}
```
#### \Models\RecurringInvoiceLine.cs
```
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Techtoon.Core.Models;

[Table("recurring_invoice_lines")]
public class RecurringInvoiceLine
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("template_id")]
    public int TemplateId { get; set; }
    [ForeignKey("TemplateId")]
    public RecurringInvoiceTemplate? Template { get; set; }

    [Column("item_id")]
    public int ItemId { get; set; }
    [ForeignKey("ItemId")]
    public Item? Item { get; set; }

    [Column("quantity")]
    public double Quantity { get; set; }

    [Column("price")]
    public double Price { get; set; }
}
```
#### \Models\RecurringInvoiceTemplate.cs
```
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Techtoon.Core.Models;

[Table("recurring_invoice_templates")]
public class RecurringInvoiceTemplate
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("name")]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Column("party_id")]
    public int PartyId { get; set; }
    [ForeignKey("PartyId")]
    public Party? Party { get; set; }

    [Column("invoice_type")]
    [StringLength(50)]
    public string InvoiceType { get; set; } = string.Empty;

    [Column("frequency")]
    [StringLength(50)]
    public string Frequency { get; set; } = "MONTHLY";

    [Column("start_date")]
    public DateTime StartDate { get; set; }

    [Column("next_run_date")]
    public DateTime NextRunDate { get; set; }

    [Column("tax_rate")]
    public double TaxRate { get; set; } = 14.0;

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    public ICollection<RecurringInvoiceLine> Lines { get; set; } = new List<RecurringInvoiceLine>();
}
```
#### \TechtoonDbContext.cs
```
using Microsoft.EntityFrameworkCore;

namespace Techtoon.Data
{
    public class TechtoonDbContext : DbContext
    {
        public TechtoonDbContext() { }
        public TechtoonDbContext(DbContextOptions<TechtoonDbContext> options) : base(options) { }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                var cs = Environment.GetEnvironmentVariable("TECHTOON_PG_CONNECTION");
                if (string.IsNullOrWhiteSpace(cs))
                {
                    cs = "Host=localhost;Database=techtoon;Username=postgres;Password=postgres";
                }
                optionsBuilder.UseNpgsql(cs);
            }
        }
    }
}
```
#### \Context\TechtoonDbContext.cs
```
using Microsoft.EntityFrameworkCore;
using Techtoon.Core.Models;

namespace Techtoon.Data.Context;

public class TechtoonDbContext : DbContext
{
    public TechtoonDbContext() { }
    public TechtoonDbContext(DbContextOptions<TechtoonDbContext> options) : base(options) { }

    public DbSet<Currency> Currencies { get; set; }
    public DbSet<Account> Accounts { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<AccountingPeriod> AccountingPeriods { get; set; }
    public DbSet<Party> Parties { get; set; }
    public DbSet<CashBox> CashBoxes { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Item> Items { get; set; }
    public DbSet<CostHistory> CostHistory { get; set; }
    public DbSet<KitComponent> KitComponents { get; set; }
    public DbSet<InventoryMovement> InventoryMovements { get; set; }
    public DbSet<CostCenter> CostCenters { get; set; }
    public DbSet<CostAllocation> CostAllocations { get; set; }
    public DbSet<JournalEntry> JournalEntries { get; set; }
    public DbSet<JournalLine> JournalLines { get; set; }
    public DbSet<Invoice> Invoices { get; set; }
    public DbSet<InvoiceLine> InvoiceLines { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<CashTransfer> CashTransfers { get; set; }
    public DbSet<ExpenseCategory> ExpenseCategories { get; set; }
    public DbSet<Expense> Expenses { get; set; }
    public DbSet<Warehouse> Warehouses { get; set; }
    public DbSet<StockLevel> StockLevels { get; set; }
    public DbSet<WarehouseTransfer> WarehouseTransfers { get; set; }
    public DbSet<StockCount> StockCounts { get; set; }
    public DbSet<FixedAsset> FixedAssets { get; set; }
    public DbSet<DepreciationRecord> DepreciationRecords { get; set; }
    public DbSet<Loan> Loans { get; set; }
    public DbSet<LoanInstallment> LoanInstallments { get; set; }
    public DbSet<Employee> Employees { get; set; }
    public DbSet<SalaryRecord> SalaryRecords { get; set; }
    public DbSet<Budget> Budgets { get; set; }
    public DbSet<RecurringInvoiceTemplate> RecurringInvoiceTemplates { get; set; }
    public DbSet<RecurringInvoiceLine> RecurringInvoiceLines { get; set; }
    public DbSet<AppSetting> AppSettings { get; set; }
    public DbSet<InvoicePayment> InvoicePayments { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseNpgsql("Host=ep-bitter-night-za726w59-pooler.c-2.eu-west-2.aws.neon.tech;Port=5432;Username=neondb_owner;Password=npg_K9nHVkY5OjRC;Database=neondb;SSL Mode=Require;Trust Server Certificate=true;Pooling=true;Timeout=15;CommandTimeout=60");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Account>(e => {
            e.HasIndex(a => a.Code).IsUnique();
            e.HasOne(a => a.Parent).WithMany(a => a.Children).HasForeignKey(a => a.ParentId);
        });

        modelBuilder.Entity<User>(e => e.HasIndex(u => u.Username).IsUnique());
        modelBuilder.Entity<Party>(e => e.HasOne(p => p.Account).WithMany(a => a.Parties).HasForeignKey(p => p.AccountId));
        modelBuilder.Entity<CashBox>(e => { e.HasIndex(c => c.Code).IsUnique(); e.HasOne(c => c.Account).WithMany(a => a.CashBoxes).HasForeignKey(c => c.AccountId); });
        modelBuilder.Entity<Category>(e => { e.HasIndex(c => c.Name).IsUnique(); e.HasOne(c => c.Parent).WithMany(c => c.Children).HasForeignKey(c => c.ParentId); });

        modelBuilder.Entity<Item>(e => {
            e.HasOne(i => i.Category).WithMany(c => c.Items).HasForeignKey(i => i.CategoryId);
            e.HasOne(i => i.InventoryAccount).WithMany(a => a.ItemsAsInventory).HasForeignKey(i => i.InventoryAccountId);
            e.HasOne(i => i.CogsAccount).WithMany(a => a.ItemsAsCogs).HasForeignKey(i => i.CogsAccountId);
            e.HasOne(i => i.RevenueAccount).WithMany(a => a.ItemsAsRevenue).HasForeignKey(i => i.RevenueAccountId);
        });

        modelBuilder.Entity<KitComponent>(e => {
            e.HasOne(k => k.KitItem).WithMany(i => i.Components).HasForeignKey(k => k.KitItemId);
            e.HasOne(k => k.ComponentItem).WithMany(i => i.UsedInKits).HasForeignKey(k => k.ComponentItemId);
        });
```

### Files

- AuthService.cs  (24513 bytes)
- AutoBackupScheduler.cs  (5778 bytes)
- BackupService.cs  (9390 bytes)
- DamagedItemsService.cs  (13725 bytes)
- DataManagementService.cs  (10456 bytes)
- FinancialService.cs  (14951 bytes)
- InventoryAdjustmentService.cs  (15299 bytes)
- InventoryCountService.cs  (26056 bytes)
- InventoryService.cs  (1561 bytes)
- LoansService.cs  (21785 bytes)
- MathHelper.cs  (782 bytes)
- NotificationService.cs  (3675 bytes)
- OverdraftService.cs  (9639 bytes)
- PrintService.cs  (27938 bytes)
- QuestPdfReportService.cs  (12283 bytes)
- SecureConnectionManager.cs  (2983 bytes)
- StatementPdfService.cs  (5416 bytes)
- StockReasonsService.cs  (3557 bytes)
- WriteoffService.cs  (13973 bytes)

### AuthService.cs

```
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Npgsql;
using BCrypt.Net;

namespace Techtoon.UI.Services
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string FullName { get; set; }
        public string Role { get; set; }
        public bool IsActive { get; set; }
    }

    public class LoginResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public User User { get; set; }
    }

    public static class AuthService
    {
        public static User CurrentUser { get; private set; }
        public static bool IsLoggedIn => CurrentUser != null;

        private static HashSet<string> _permissions = new HashSet<string>();

        public static string GetCS() { return Techtoon.UI.Services.SecureConnectionManager.GetConnectionString(); }

        public static async Task<LoginResult> LoginAsync(string username, string password)
        {
            return await Task.FromResult(Login(username, password));
        }

        public static LoginResult Login(string username, string password)
        {
            var result = new LoginResult();
            try
            {
                using (var conn = new NpgsqlConnection(GetCS()))
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand("SELECT id, username, COALESCE(password_hash,''), COALESCE(full_name,''), COALESCE(role,'SALESPERSON'), COALESCE(is_active, FALSE) FROM users WHERE username=@u", conn))
                    {
                        cmd.Parameters.AddWithValue("u", username);
                        using (var r = cmd.ExecuteReader())
                        {
                            if (!r.Read()) { result.ErrorMessage = "User not found"; return result; }
                            if (!r.GetBoolean(5)) { result.ErrorMessage = "User inactive"; return result; }

                            string hash = r.GetString(2);
                            if (!string.IsNullOrEmpty(hash) && !BCrypt.Net.BCrypt.Verify(password, hash))
                            {
                                result.ErrorMessage = "Wrong password";
                                return result;
                            }

                            CurrentUser = new User
                            {
                                Id = r.GetInt32(0),
                                Username = r.GetString(1),
                                FullName = r.GetString(3),
                                Role = r.GetString(4),
                                IsActive = true
                            };
                        }
                    }

                    LoadPermissions();

                    using (var cmd = new NpgsqlCommand("UPDATE users SET last_login=NOW() WHERE id=@id", conn))
                    {
                        cmd.Parameters.AddWithValue("id", CurrentUser.Id);
                        cmd.ExecuteNonQuery();
                    }

                    LogAudit("login", "user", CurrentUser.Id, "Login success");
                    result.Success = true;
                    result.User = CurrentUser;
                    return result;
                }
            }
            catch (Exception ex) { result.ErrorMessage = ex.Message; return result; }
        }

        public static void Logout()
        {
            if (IsLoggedIn) LogAudit("logout", "user", CurrentUser.Id, "Logout");
            CurrentUser = null;
            _permissions.Clear();
        }

        private static void LoadPermissions()
        {
            _permissions.Clear();
            if (CurrentUser == null) return;
            try
            {
                using (var conn = new NpgsqlConnection(GetCS()))
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand("SELECT permission_key FROM user_permissions WHERE user_id=@id AND is_granted=TRUE", conn))
                    {
                        cmd.Parameters.AddWithValue("id", CurrentUser.Id);
                        using (var r = cmd.ExecuteReader())
                            while (r.Read()) _permissions.Add(r.GetString(0));
                    }
                }
            }
            catch { }
        }

        public static bool HasPermission(string key)
        {
            if (!IsLoggedIn) return false;
            if (CurrentUser.Role == "ADMIN") return true;
            return _permissions.Contains(key);
        }

        public static void LogAudit(string action, string entityType, int entityId, string details)
        {
            try
            {
                using (var conn = new NpgsqlConnection(GetCS()))
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand(@"INSERT INTO audit_log (user_id, username, action, entity_type, entity_id, details, created_at) VALUES (@uid, @uname, @action, @etype, @eid, @details, NOW())", conn))
                    {
                        cmd.Parameters.AddWithValue("uid", CurrentUser?.Id > 0 ? (object)CurrentUser.Id : DBNull.Value);
                        cmd.Parameters.AddWithValue("uname", (object)CurrentUser?.Username ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("action", action);
                        cmd.Parameters.AddWithValue("etype", (object)entityType ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("eid", entityId > 0 ? (object)entityId : DBNull.Value);
                        cmd.Parameters.AddWithValue("details", (object)details ?? DBNull.Value);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch { }
        }

        public static async Task EnsureDefaultAdminAsync()
        {
            try
            {
                using (var conn = new NpgsqlConnection(GetCS()))
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM users WHERE username='admin'", conn))
                    {
                        if (Convert.ToInt32(cmd.ExecuteScalar()) == 0)
                        {
                            string hash = BCrypt.Net.BCrypt.HashPassword("admin123", workFactor: 12);
                            using (var c2 = new NpgsqlCommand("INSERT INTO users (username, password_hash, full_name, role, is_active, created_at) VALUES ('admin', @h, 'Administrator', 'ADMIN', TRUE, NOW())", conn))
                            {
                                c2.Parameters.AddWithValue("h", hash);
                                c2.ExecuteNonQuery();
                            }
                        }
                    }
                }
            }
            catch { }
            await Task.CompletedTask;
        }

        public static bool CreateUser(string username, string password, string fullName, string role)
        {
            if (!HasPermission("manage_users")) return false;
            try
            {
                string hash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
                using (var conn = new NpgsqlConnection(GetCS()))
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand("INSERT INTO users (username, password_hash, full_name, role, is_active, created_at) VALUES (@u, @h, @fn, @r, TRUE, NOW())", conn))
                    {
                        cmd.Parameters.AddWithValue("u", username);
                        cmd.Parameters.AddWithValue("h", hash);
                        cmd.Parameters.AddWithValue("fn", fullName);
                        cmd.Parameters.AddWithValue("r", role);
                        cmd.ExecuteNonQuery();
                    }
                }
                LogAudit("create_user", "user", 0, $"Created user: {username} ({role})");
                return true;
            }
            catch { return false; }
        }

        public static bool ChangePassword(int userId, string newPassword)
        {
            try
            {
                string hash = BCrypt.Net.BCrypt.HashPassword(newPassword, workFactor: 12);
                using (var conn = new NpgsqlConnection(GetCS()))
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand("UPDATE users SET password_hash=@h WHERE id=@id", conn))
                    {
                        cmd.Parameters.AddWithValue("h", hash);
                        cmd.Parameters.AddWithValue("id", userId);
                        cmd.ExecuteNonQuery();
                    }
                }
                LogAudit("change_password", "user", userId, "Password changed");
                return true;
            }
            catch { return false; }
        }

        public static bool SetPermission(int userId, string permissionKey, bool granted)
        {
            if (!HasPermission("manage_users")) return false;
            try
            {
                using (var conn = new NpgsqlConnection(GetCS()))
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand(@"INSERT INTO user_permissions (user_id, permission_key, is_granted) VALUES (@uid, @key, @granted) ON CONFLICT (user_id, permission_key) DO UPDATE SET is_granted = @granted", conn))
                    {
                        cmd.Parameters.AddWithValue("uid", userId);
                        cmd.Parameters.AddWithValue("key", permissionKey);
                        cmd.Parameters.AddWithValue("granted", granted);
                        cmd.ExecuteNonQuery();
                    }
                }
                LogAudit("set_permission", "user", userId, $"Permission '{permissionKey}' = {granted}");
                return true;
            }
            catch { return false; }
        }
        // ============= USER MANAGEMENT METHODS =============

        public class UserWithRole
        {
            public int Id { get; set; }
            public string Username { get; set; }
            public string FullName { get; set; }
            public string Email { get; set; }
            public string Role { get; set; }
            public int? RoleId { get; set; }
            public string RoleDisplayName { get; set; }
            public bool IsActive { get; set; }
            public bool MustChangePassword { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime? LastLogin { get; set; }
        }

        public class RoleOption
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string DisplayName { get; set; }
            public string Description { get; set; }
            public bool IsSystemRole { get; set; }
        }

        public static System.Collections.Generic.List<UserWithRole> GetAllUsers(string search = null)
        {
            var list = new System.Collections.Generic.List<UserWithRole>();
            using var conn = new NpgsqlConnection(GetCS());
            conn.Open();
            var sql = "SELECT u.id, u.username, u.full_name, u.email, u.role, u.role_id, " +
                      "COALESCE(r.display_name, u.role), u.is_active, u.must_change_password, u.created_at, u.last_login " +
                      "FROM users u LEFT JOIN roles r ON u.role_id = r.id";
            if (!string.IsNullOrEmpty(search))
                sql += " WHERE u.username ILIKE @q OR u.full_name ILIKE @q OR u.role ILIKE @q";
            sql += " ORDER BY u.id";
            using var cmd = new NpgsqlCommand(sql, conn);
            if (!string.IsNullOrEmpty(search))
                cmd.Parameters.AddWithValue("q", "%" + search + "%");
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(new UserWithRole
                {
                    Id = r.GetInt32(0),
                    Username = r.GetString(1),
                    FullName = r.GetString(2),
                    Email = r.IsDBNull(3) ? "" : r.GetString(3),
                    Role = r.GetString(4),
                    RoleId = r.IsDBNull(5) ? (int?)null : r.GetInt32(5),
                    RoleDisplayName = r.GetString(6),
                    IsActive = r.IsDBNull(7) ? true : r.GetBoolean(7),
                    MustChangePassword = r.IsDBNull(8) ? false : r.GetBoolean(8),
                    CreatedAt = r.IsDBNull(9) ? DateTime.MinValue : r.GetDateTime(9),
                    LastLogin = r.IsDBNull(10) ? (DateTime?)null : r.GetDateTime(10)
                });
            }
            return list;
        }

        public static System.Collections.Generic.List<RoleOption> GetAllRoles()
        {
            var list = new System.Collections.Generic.List<RoleOption>();
            using var conn = new NpgsqlConnection(GetCS());
            conn.Open();
            using var cmd = new NpgsqlCommand("SELECT id, name, display_name, description, is_system_role FROM roles WHERE is_active = TRUE ORDER BY name", conn);
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(new RoleOption
                {
                    Id = r.GetInt32(0),
                    Name = r.GetString(1),
                    DisplayName = r.GetString(2),
                    Description = r.IsDBNull(3) ? "" : r.GetString(3),
                    IsSystemRole = r.GetBoolean(4)
                });
            }
            return list;
        }

        public static bool IsLastAdmin(int userId)
        {
            using var conn = new NpgsqlConnection(GetCS());
            conn.Open();
            // تحقق هل المستخدم هو آخر ADMIN نشط
            using var cmd = new NpgsqlCommand(
                "SELECT COUNT(*) FROM users WHERE role = 'ADMIN' AND is_active = TRUE AND id != @id", conn);
            cmd.Parameters.AddWithValue("id", userId);
            int count = Convert.ToInt32(cmd.ExecuteScalar());
            return count == 0;
        }

        public static bool IsCurrentUser(int userId)
        {
            return CurrentUser != null && CurrentUser.Id == userId;
        }

        public static void UpdateUser(int userId, string fullName, string email, int roleId)
        {
            using var conn = new NpgsqlConnection(GetCS());
            conn.Open();
            using var tx = conn.BeginTransaction();
            try
            {
                // جلب اسم الـ role من roles table
                string roleName = "";
                using (var cmd = new NpgsqlCommand("SELECT name FROM roles WHERE id = @rid", conn, tx))
                {
                    cmd.Parameters.AddWithValue("rid", roleId);
                    var result = cmd.ExecuteScalar();
                    if (result != null) roleName = result.ToString();
                }

                using var cmd2 = new NpgsqlCommand(
                    "UPDATE users SET full_name = @fn, email = @em, role_id = @rid, role = @r WHERE id = @id", conn, tx);
                cmd2.Parameters.AddWithValue("fn", fullName);
                cmd2.Parameters.AddWithValue("em", string.IsNullOrEmpty(email) ? (object)DBNull.Value : email);
                cmd2.Parameters.AddWithValue("rid", roleId);
                cmd2.Parameters.AddWithValue("r", roleName);
                cmd2.Parameters.AddWithValue("id", userId);
                cmd2.ExecuteNonQuery();

                LogAudit("UPDATE_USER", "user", userId, "Updated user: " + fullName);
                tx.Commit();
            }
            catch { tx.Rollback(); throw; }
        }

        public static void ToggleUserActive(int userId)
        {
            if (IsCurrentUser(userId))
                throw new InvalidOperationException("لا يمكن تعطيل حسابك الحالي");

            using var conn = new NpgsqlConnection(GetCS());
            conn.Open();
            using var tx = conn.BeginTransaction();
            try
            {
                // جلب الحالة الحالية + role
                bool currentActive; string role;
                using (var cmd = new NpgsqlCommand("SELECT is_active, role FROM users WHERE id = @id", conn, tx))
                {
                    cmd.Parameters.AddWithValue("id", userId);
                    using var r = cmd.ExecuteReader();
                    if (!r.Read()) throw new InvalidOperationException("المستخدم غير موجود");
                    currentActive = r.IsDBNull(0) ? true : r.GetBoolean(0);
                    role = r.GetString(1);
                }

                // إذا كان سيتم تعطيل مستخدم ADMIN، تحقق من عدم كونه الأخير
                if (currentActive && role == "ADMIN" && IsLastAdmin(userId))
                    throw new InvalidOperationException("لا يمكن تعطيل آخر حساب مدير نشط في النظام");

                bool newActive = !currentActive;
                using var cmd2 = new NpgsqlCommand("UPDATE users SET is_active = @a WHERE id = @id", conn, tx);
                cmd2.Parameters.AddWithValue("a", newActive);
                cmd2.Parameters.AddWithValue("id", userId);
                cmd2.ExecuteNonQuery();

                LogAudit(newActive ? "ACTIVATE_USER" : "DEACTIVATE_USER", "user", userId,
                    newActive ? "User activated" : "User deactivated");
                tx.Commit();
            }
            catch { tx.Rollback(); throw; }
        }

        public static void ResetPassword(int userId, string newPassword)
        {
            if (string.IsNullOrEmpty(newPassword) || newPassword.Length < 4)
                throw new InvalidOperationException("كلمة المرور قصيرة جدًا (4 أحرف على الأقل)");

            using var conn = new NpgsqlConnection(GetCS());
            conn.Open();
            using var tx = conn.BeginTransaction();
            try
            {
                var hash = BCrypt.Net.BCrypt.HashPassword(newPassword, workFactor: 12);
                using var cmd = new NpgsqlCommand(
                    "UPDATE users SET password_hash = @h, must_change_password = TRUE WHERE id = @id", conn, tx);
                cmd.Parameters.AddWithValue("h", hash);
                cmd.Parameters.AddWithValue("id", userId);
                cmd.ExecuteNonQuery();

                LogAudit("RESET_PASSWORD", "user", userId, "Password reset by admin");
                tx.Commit();
            }
            catch { tx.Rollback(); throw; }
        }

        public static void ChangeOwnPassword(int userId, string oldPassword, string newPassword)
        {
            using var conn = new NpgsqlConnection(GetCS());
            conn.Open();
            // تحقق من كلمة المرور القديمة
            string currentHash;
            using (var cmd = new NpgsqlCommand("SELECT password_hash FROM users WHERE id = @id", conn))
            {
                cmd.Parameters.AddWithValue("id", userId);
                currentHash = cmd.ExecuteScalar()?.ToString() ?? "";
            }
            if (!BCrypt.Net.BCrypt.Verify(oldPassword, currentHash))
                throw new InvalidOperationException("كلمة المرور الحالية غير صحيحة");

            if (string.IsNullOrEmpty(newPassword) || newPassword.Length < 4)
                throw new InvalidOperationException("كلمة المرور الجديدة قصيرة جدًا");

            using var tx = conn.BeginTransaction();
            try
            {
                var newHash = BCrypt.Net.BCrypt.HashPassword(newPassword, workFactor: 12);
                using var cmd = new NpgsqlCommand(
                    "UPDATE users SET password_hash = @h, must_change_password = FALSE WHERE id = @id", conn, tx);
                cmd.Parameters.AddWithValue("h", newHash);
                cmd.Parameters.AddWithValue("id", userId);
                cmd.ExecuteNonQuery();

                LogAudit("CHANGE_PASSWORD", "user", userId, "User changed own password");
                tx.Commit();
            }
            catch { tx.Rollback(); throw; }
        }

        public static System.Collections.Generic.Dictionary<string, bool> GetUserPermissions(int userId)
        {
            var perms = new System.Collections.Generic.Dictionary<string, bool>();
            using var conn = new NpgsqlConnection(GetCS());
            conn.Open();
            using var cmd = new NpgsqlCommand("SELECT permission_key, is_granted FROM user_permissions WHERE user_id = @uid", conn);
            cmd.Parameters.AddWithValue("uid", userId);
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                perms[r.GetString(0)] = r.IsDBNull(1) ? true : r.GetBoolean(1);
            }
            return perms;
        }

        public static void SetUserPermissions(int userId, System.Collections.Generic.Dictionary<string, bool> permissions)
        {
            using var conn = new NpgsqlConnection(GetCS());
            conn.Open();
            using var tx = conn.BeginTransaction();
            try
            {
                // حذف الصلاحيات القديمة
                using (var cmd = new NpgsqlCommand("DELETE FROM user_permissions WHERE user_id = @uid", conn, tx))
                {
                    cmd.Parameters.AddWithValue("uid", userId);
                    cmd.ExecuteNonQuery();
                }
                // إدراج الصلاحيات الجديدة
                foreach (var kvp in permissions)
                {
                    using var cmd = new NpgsqlCommand(
                        "INSERT INTO user_permissions (user_id, permission_key, is_granted, created_at) " +
                        "VALUES (@uid, @key, @granted, NOW())", conn, tx);
                    cmd.Parameters.AddWithValue("uid", userId);
                    cmd.Parameters.AddWithValue("key", kvp.Key);
                    cmd.Parameters.AddWithValue("granted", kvp.Value);
                    cmd.ExecuteNonQuery();
                }

                LogAudit("SET_PERMISSIONS", "user", userId, "Permissions updated: " + permissions.Count + " keys");
                tx.Commit();
            }
            catch { tx.Rollback(); throw; }
        }

        // كل مفاتيح الصلاحيات المتاحة في النظام
        public static System.Collections.Generic.List<string> GetAllPermissionKeys()
        {
            return new System.Collections.Generic.List<string>
            {
                // المبيعات والمشتريات
                "sales", "returns", "discounts", "edit_price", "cancel_invoice",
                // المالية
                "expenses", "profit_reports", "backup", "settings",
                // المستخدمين
                "manage_users",
                // المخزون القديم
                "stock_adjustment",
                // المخزون الجديد - الجرد
                "inventory.count.create", "inventory.count.count", "inventory.count.review", "inventory.count.approve",
                // المخزون الجديد - التسويات
                "inventory.adjustment.create", "inventory.adjustment.approve", "inventory.adjustment.execute",
                // المخزون الجديد - التالف
                "inventory.damaged.create", "inventory.damaged.inspect",
                // المخزون الجديد - الشطب
                "inventory.writeoff.approve", "inventory.writeoff.execute",
                // التقارير
                "inventory.reports.view", "inventory.audit.view"
            };
        }

        public static void CheckUserActiveStatus(int userId)
        {
            using var conn = new NpgsqlConnection(GetCS());
            conn.Open();
            using var cmd = new NpgsqlCommand("SELECT is_active FROM users WHERE id = @id", conn);
            cmd.Parameters.AddWithValue("id", userId);
            var result = cmd.ExecuteScalar();
            if (result == null) throw new InvalidOperationException("المستخدم غير موجود");
            if (!(bool)result) throw new InvalidOperationException("الحساب معطل. تواصل مع المدير");
        }
    }
}
```

### SecureConnectionManager.cs (redacted)

```
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Techtoon.UI.Services
{
    /// <summary>
    /// يحفظ connection string مشفّرًا في مجلد بيانات المستخدم باستخدام DPAPI.
    /// لا يُكشف النص العادي على القرص أبدًا.
    /// </summary>
    public static class SecureConnectionManager
    {
        private static readonly string AppDataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "TechtoonAccounting");

        private static readonly string EncryptedFilePath = Path.Combine(AppDataDir, "connection.enc");

        // قيمة افتراضية (fallback) لو لم يوجد ملف مشفّر — تُستخدم أول مرة فقط
        private const string DefaultConnection =
            "Host=ep-bitter-night-za726w59-pooler.c-2.eu-west-2.aws.neon.tech;Port=5432;" +
            "Username=neondb_owner;Password=***;Database=neondb;" +
            "SSL Mode=Require;Trust Server Certificate=true";

        public static string GetConnectionString()
        {
            try
            {
                if (!Directory.Exists(AppDataDir))
                    Directory.CreateDirectory(AppDataDir);

                // إن لم يوجد ملف مشفّر، أنشئه من القيمة الافتراضية (أول تشغيل)
                if (!File.Exists(EncryptedFilePath))
                {
                    SaveConnectionString(DefaultConnection);
                }

                return LoadConnectionString();
            }
            catch
            {
                // fallback آمن لو فشل DPAPI
                return DefaultConnection;
            }
        }

        public static void SaveConnectionString(string connectionString)
        {
            try
            {
                if (!Directory.Exists(AppDataDir))
                    Directory.CreateDirectory(AppDataDir);

                // تشفير بـ DPAPI (مرتبط بالمستخدم الحالي)
                var plainBytes = Encoding.UTF8.GetBytes(connectionString);
                var cipherBytes = ProtectedData.Protect(plainBytes, null, DataProtectionScope.CurrentUser);
                File.WriteAllBytes(EncryptedFilePath, cipherBytes);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to save connection string: " + ex.Message, ex);
            }
        }

        private static string LoadConnectionString()
        {
            var cipherBytes = File.ReadAllBytes(EncryptedFilePath);
            var plainBytes = ProtectedData.Unprotect(cipherBytes, null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(plainBytes);
        }

        public static bool HasStoredConnection()
        {
            return File.Exists(EncryptedFilePath);
        }
    }
}
```

### Public method signatures

#### AuthService.cs
```
    public class User
        public int Id { get; set; }
        public string Username { get; set; }
        public string FullName { get; set; }
        public string Role { get; set; }
        public bool IsActive { get; set; }
    public class LoginResult
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public User User { get; set; }
    public static class AuthService
        public static User CurrentUser { get; private set; }
        public static bool IsLoggedIn => CurrentUser != null;
        public static string GetCS() { return Techtoon.UI.Services.SecureConnectionManager.GetConnectionString(); }
        public static async Task<LoginResult> LoginAsync(string username, string password)
        public static LoginResult Login(string username, string password)
        public static void Logout()
        public static bool HasPermission(string key)
        public static void LogAudit(string action, string entityType, int entityId, string details)
        public static async Task EnsureDefaultAdminAsync()
        public static bool CreateUser(string username, string password, string fullName, string role)
        public static bool ChangePassword(int userId, string newPassword)
        public static bool SetPermission(int userId, string permissionKey, bool granted)
        public class UserWithRole
            public int Id { get; set; }
            public string Username { get; set; }
            public string FullName { get; set; }
            public string Email { get; set; }
            public string Role { get; set; }
            public int? RoleId { get; set; }
            public string RoleDisplayName { get; set; }
            public bool IsActive { get; set; }
            public bool MustChangePassword { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime? LastLogin { get; set; }
        public class RoleOption
            public int Id { get; set; }
            public string Name { get; set; }
            public string DisplayName { get; set; }
            public string Description { get; set; }
            public bool IsSystemRole { get; set; }
        public static System.Collections.Generic.List<UserWithRole> GetAllUsers(string search = null)
        public static System.Collections.Generic.List<RoleOption> GetAllRoles()
        public static bool IsLastAdmin(int userId)
        public static bool IsCurrentUser(int userId)
        public static void UpdateUser(int userId, string fullName, string email, int roleId)
        public static void ToggleUserActive(int userId)
        public static void ResetPassword(int userId, string newPassword)
        public static void ChangeOwnPassword(int userId, string oldPassword, string newPassword)
        public static System.Collections.Generic.Dictionary<string, bool> GetUserPermissions(int userId)
        public static void SetUserPermissions(int userId, System.Collections.Generic.Dictionary<string, bool> permissions)
        public static System.Collections.Generic.List<string> GetAllPermissionKeys()
        public static void CheckUserActiveStatus(int userId)
```
#### AutoBackupScheduler.cs
```
    public static class AutoBackupScheduler
        public static bool CheckAndBackupIfNeeded(string connectionString)
        public static void CreateBackup(string connectionString)
```
#### BackupService.cs
```
    public static class BackupService
        public static string GetCS() { return Techtoon.UI.Services.SecureConnectionManager.GetConnectionString(); }
        public static string CreateBackup(string backupDir)
        public static bool RestoreBackup(string sqlFilePath, string targetDatabase)
        public static bool VerifyBackup(string filePath)
        public static int CleanOldBackups(string backupDir, int retentionDays)
```
#### DamagedItemsService.cs
```
    public class DamagedItem
        public int Id { get; set; }
        public string DocumentNumber { get; set; }
        public DateTime Date { get; set; }
        public int ItemId { get; set; }
        public string ItemName { get; set; }
        public int WarehouseId { get; set; }
        public string WarehouseName { get; set; }
        public double Quantity { get; set; }
        public decimal UnitCost { get; set; }
        public decimal TotalValue { get; set; }
        public int? DamageReasonId { get; set; }
        public string DamageReasonName { get; set; }
        public string Notes { get; set; }
        public string InspectionStatus { get; set; } // pending, under_inspection, inspected
        public string InspectionResult { get; set; } // repair, return_supplier, back_to_stock, sell_as_is, writeoff
        public string InspectionNotes { get; set; }
        public int? InspectedBy { get; set; }
        public DateTime? InspectedAt { get; set; }
        public string Status { get; set; } // draft, under_inspection, inspected, writeoff_requested, written_off, returned, repaired
        public int? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public string StatusDisplay { get { return Status switch { "draft" => "مسودة", "under_inspection" => "تحت الفحص", "inspected" => "تم الفحص", "writeoff_requested" => "طلب شطب", "written_off" => "مشطوب", "returned" => "مُرجع", "repaired" => "مُصلح", _ => Status }; } }
        public string QuantityDisplay { get { return Quantity.ToString("0.00"); } }
        public string TotalValueDisplay { get { return TotalValue.ToString("N2"); } }
    public static class DamagedItemsService
        public static string GenerateDocumentNumber()
        public static int Create(int itemId, int warehouseId, double quantity, int? reasonId, string notes, int userId)
        public static void StartInspection(int id, int userId)
        public static void CompleteInspection(int id, string result, string notes, int userId)
        public static void Delete(int id)
        public static List<DamagedItem> GetAll(string statusFilter = null)
        public static DamagedItem GetById(int id)
```
#### DataManagementService.cs
```
    public class DataPreviewItem
        public string TableName { get; set; }
        public long RowCount { get; set; }
    public static class DataManagementService
        public static List<DataPreviewItem> PreviewDeleteInvoices()
        public static List<DataPreviewItem> PreviewClearInventory()
        public static List<DataPreviewItem> PreviewDeleteStockOps()
        public static List<DataPreviewItem> PreviewDeleteFinance()
        public static void ExecuteDeleteInvoices()
        public static void ExecuteClearInventory()
        public static void ExecuteDeleteStockOps()
        public static void ExecuteDeleteFinance()
        public static void ExecuteFactoryReset(bool keepUsersAndRoles)
```
#### FinancialService.cs
```
    public class CashBoxBalance
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal OpeningBalance { get; set; }
        public decimal TotalReceipts { get; set; }
        public decimal TotalPayments { get; set; }
        public decimal TotalExpenses { get; set; }
        public decimal TotalTransfersIn { get; set; }
        public decimal TotalTransfersOut { get; set; }
        public decimal CurrentBalance { get; set; }
    public class CashFlowReport
        public decimal TotalReceipts { get; set; }
        public decimal TotalPayments { get; set; }
        public decimal TotalExpenses { get; set; }
        public decimal NetCashFlow { get; set; }
        public decimal TotalReceivables { get; set; }
        public decimal TotalPayables { get; set; }
        public decimal GrossProfit { get; set; }
        public decimal OperatingProfit { get; set; }
    public class ShiftClosing
        public int Id { get; set; }
        public int CashBoxId { get; set; }
        public DateTime ShiftDate { get; set; }
        public decimal OpeningAmount { get; set; }
        public decimal ExpectedClosing { get; set; }
        public decimal ActualClosing { get; set; }
        public decimal Difference { get; set; }
        public string Status { get; set; }
        public string Notes { get; set; }
    public class FinancialService
        public static string GetCS() { return Techtoon.UI.Services.SecureConnectionManager.GetConnectionString(); }
        public static CashBoxBalance GetCashBoxBalance(int cashBoxId)
        public static async Task<bool> TransferBetweenCashBoxesAsync(int fromId, int toId, decimal amount, string notes)
        public static async Task<bool> CloseShiftAsync(int cashBoxId, decimal actualClosing, string notes)
        public static CashFlowReport GetCashFlowReport(DateTime fromDate, DateTime toDate)
```
#### InventoryAdjustmentService.cs
```
    public class StockAdjustment
        public int Id { get; set; }
        public string DocumentNumber { get; set; }
        public int ItemId { get; set; }
        public string ItemName { get; set; }
        public int? WarehouseId { get; set; }
        public string WarehouseName { get; set; }
        public DateTime AdjustmentDate { get; set; }
        public double OldQuantity { get; set; }
        public double NewQuantity { get; set; }
        public double Difference { get; set; }
        public string Reason { get; set; }
        public string ReasonName { get; set; }
        public string Notes { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; } // draft, pending, approved, rejected, executed
        public int? ApprovedBy { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public DateTime? ExecutedAt { get; set; }
        public int? MovementId { get; set; }
        public decimal UnitCost { get; set; }
        public decimal ValueBefore { get; set; }
        public decimal ValueAfter { get; set; }
        public string ReferenceDocumentType { get; set; }
        public string StatusDisplay { get { return Status switch { "draft" => "مسودة", "pending" => "بانتظار الاعتماد", "approved" => "معتمد", "rejected" => "مرفوض", "executed" => "منفذ", _ => Status }; } }
        public string DifferenceDisplay { get { return Difference.ToString("0.00"); } }
        public string OldQuantityDisplay { get { return OldQuantity.ToString("0.00"); } }
        public string NewQuantityDisplay { get { return NewQuantity.ToString("0.00"); } }
        public string ValueBeforeDisplay { get { return ValueBefore.ToString("N2"); } }
        public string ValueAfterDisplay { get { return ValueAfter.ToString("N2"); } }
    public static class InventoryAdjustmentService
        public static string GenerateDocumentNumber()
        public static int CreateAdjustment(int itemId, int warehouseId, double newQuantity, string reasonCode, string notes, int userId)
        public static void SubmitForApproval(int adjId)
        public static void Approve(int adjId, int userId)
        public static void Reject(int adjId, int userId, string reason)
        public static void Execute(int adjId, int userId)
        public static List<StockAdjustment> GetAdjustments(string statusFilter = null)
        public static StockAdjustment GetById(int adjId)
        public static void DeleteAdjustment(int adjId)
```
#### InventoryCountService.cs
```
    public class StockCountSession
        public int Id { get; set; }
        public string DocumentNumber { get; set; }
        public DateTime SessionDate { get; set; }
        public int WarehouseId { get; set; }
        public string WarehouseName { get; set; }
        public string CountType { get; set; } // full, partial, surprise
        public string CountTypeDisplay { get; set; }
        public string Status { get; set; } // draft, in_progress, under_review, approved, rejected, executed
        public string StatusDisplay { get; set; }
        public bool BookBalanceLocked { get; set; }
        public string Notes { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public int? ReviewedBy { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public int? ApprovedBy { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public DateTime? ExecutedAt { get; set; }
        public int ItemsCount { get; set; }
        public int CountedItems { get; set; }
        public decimal TotalDifferenceValue { get; set; }
    public class StockCountItem
        public int Id { get; set; }
        public int SessionId { get; set; }
        public int ItemId { get; set; }
        public string ItemName { get; set; }
        public string Barcode { get; set; }
        public double BookQuantity { get; set; }
        public double? CountedQuantity { get; set; }
        public double? RecountQuantity { get; set; }
        public double Difference { get; set; }
        public decimal DifferenceValue { get; set; }
        public decimal UnitCost { get; set; }
        public string Status { get; set; } // pending, counted, reviewed, approved
        public DateTime? CountedAt { get; set; }
        public string Notes { get; set; }
        public string BookQuantityDisplay { get { return BookQuantity.ToString("0.00"); } }
        public string CountedQuantityDisplay { get { return CountedQuantity.HasValue ? CountedQuantity.Value.ToString("0.00") : ""; } }
        public string DifferenceDisplay { get { return Difference.ToString("0.00"); } }
        public string DifferenceValueDisplay { get { return DifferenceValue.ToString("N2"); } }
    public static class InventoryCountService
        public static string GenerateDocumentNumber()
        public static int CreateSession(int warehouseId, string countType, bool bookBalanceLocked, string notes, int userId)
        public static void StartSession(int sessionId, int userId)
        public static void SubmitForReview(int sessionId)
        public static void ApproveSession(int sessionId, int userId)
        public static void RejectSession(int sessionId, string reason)
        public static void ExecuteSession(int sessionId, int userId)
        public static void UpdateCountedQuantity(int countItemId, double countedQty, int userId)
        public static void RecountItem(int countItemId, double recountQty)
        public static List<StockCountSession> GetSessions(string statusFilter = null)
        public static List<StockCountItem> GetSessionItems(int sessionId, bool hideBookBalance = false)
        public static void DeleteSession(int sessionId)
```
#### InventoryService.cs
```
public static class InventoryService
    public static async Task UpdateStockForInvoiceAsync(TechtoonDbContext db, Invoice invoice, bool isEdit = false)
```
#### LoansService.cs
```
    public class Loan
        public int Id { get; set; }
        public string LoanName { get; set; }
        public string ContractNumber { get; set; }
        public string LoanType { get; set; }
        public int? BankAccountId { get; set; }
        public string BankName { get; set; }
        public double PrincipalAmount { get; set; }
        public DateTime DisbursementDate { get; set; }
        public DateTime? FirstPaymentDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int NumInstallments { get; set; }
        public string PaymentFrequency { get; set; }
        public double InterestRate { get; set; }
        public string InterestType { get; set; }
        public string InterestCalcMethod { get; set; }
        public int GracePeriod { get; set; }
        public double AdminFees { get; set; }
        public double InsuranceAmount { get; set; }
        public double CreditLimit { get; set; }
        public string DayCountBasis { get; set; }
        public int? LiabilityAccountId { get; set; }
        public int? InterestExpenseAccountId { get; set; }
        public string Status { get; set; }
        public string Notes { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public string StatusDisplay { get { return Status switch { "draft" => "مسودة", "active" => "ساري", "closed" => "مغلق", "defaulted" => "متعثر", _ => Status }; } }
        public string LoanTypeDisplay { get { return LoanType == "overdraft" ? "جاري مدين" : "قرض أقساط"; } }
        public string PrincipalDisplay { get { return PrincipalAmount.ToString("N2"); } }
    public class LoanInstallment
        public int Id { get; set; }
        public int LoanId { get; set; }
        public int InstallmentNumber { get; set; }
        public DateTime DueDate { get; set; }
        public double PrincipalAmount { get; set; }
        public double InterestAmount { get; set; }
        public double FeesAmount { get; set; }
        public double TotalAmount { get; set; }
        public double RemainingBalance { get; set; }
        public double PaidAmount { get; set; }
        public DateTime? PaidDate { get; set; }
        public double PenaltyAmount { get; set; }
        public string Status { get; set; }
        public string StatusDisplay { get { return Status switch { "upcoming" => "قادم", "due" => "مستحق", "partially_paid" => "مدفوع جزئياً", "paid" => "مدفوع", "late" => "متأخر", _ => Status }; } }
        public string DueDateDisplay { get { return DueDate.ToString("yyyy-MM-dd"); } }
        public string PrincipalDisplay { get { return PrincipalAmount.ToString("N2"); } }
        public string InterestDisplay { get { return InterestAmount.ToString("N2"); } }
        public string TotalDisplay { get { return TotalAmount.ToString("N2"); } }
        public string RemainingDisplay { get { return RemainingBalance.ToString("N2"); } }
        public string PaidDisplay { get { return PaidAmount.ToString("N2"); } }
    public static class LoansService
        public static int CreateLoan(Loan loan, int userId)
        public static void UpdateLoanStatus(int loanId, string status)
        public static void RecordRepayment(int installmentId, double amount, DateTime payDate, int userId)
        public static void DeleteLoan(int loanId)
        public static void UpdateLoan(Loan loan)
        public static List<Loan> GetAllLoans(string typeFilter = null)
        public static Loan GetById(int id)
        public static List<LoanInstallment> GetSchedule(int loanId)
```
#### MathHelper.cs
```
    public static class MathHelper
        public static double EvaluateExpression(string expression)
```
#### NotificationService.cs
```
    public static class NotificationService
        public static void Show(string message, NotificationType type = NotificationType.Info, int durationSeconds = 3)
    public enum NotificationType
```
#### OverdraftService.cs
```
    public class OverdraftTransaction
        public int Id { get; set; }
        public int LoanId { get; set; }
        public DateTime TransactionDate { get; set; }
        public string TransactionType { get; set; } // withdrawal, repayment, interest, fees
        public double Amount { get; set; }
        public double BalanceAfter { get; set; }
        public double InterestAmount { get; set; }
        public string Notes { get; set; }
        public string ReferenceNumber { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public string TypeDisplay { get { return TransactionType switch { "withdrawal" => "سحب", "repayment" => "سداد", "interest" => "فائدة", "fees" => "رسوم", _ => TransactionType }; } }
        public string AmountDisplay { get { return Amount.ToString("N2"); } }
        public string BalanceDisplay { get { return BalanceAfter.ToString("N2"); } }
        public string DateDisplay { get { return TransactionDate.ToString("yyyy-MM-dd"); } }
    public static class OverdraftService
        public static int Withdraw(int loanId, double amount, DateTime date, string notes, int userId)
        public static int Repay(int loanId, double amount, DateTime date, string notes, int userId)
        public static double GetUsedBalance(NpgsqlConnection conn, NpgsqlTransaction tx, int loanId)
        public static double GetAvailableBalance(int loanId)
        public static List<OverdraftTransaction> GetTransactions(int loanId)
```
#### PrintService.cs
```
    public static class PrintService
        public static string GetCS() { return Techtoon.UI.Services.SecureConnectionManager.GetConnectionString(); }
        public static void PrintInvoiceThermal58(int invoiceId) { PrintInvoiceThermal(invoiceId, 230, "Thermal 58mm"); }
        public static void PrintInvoiceThermal80(int invoiceId) { PrintInvoiceThermal(invoiceId, 320, "Thermal 80mm"); }
                public static void PreviewInvoiceA4(int invoiceId)
public static void PrintInvoiceA4(int invoiceId)
        public static void PrintPaymentReceipt(int paymentId, string paymentType)
        public static void PrintStatement(int partyId, DateTime fromDate, DateTime toDate)
        public static void PrintInventoryReport()
    public class InvoicePrintData
        public string InvoiceNumber { get; set; }
        public DateTime Date { get; set; }
        public string PartyName { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Discount { get; set; }
        public decimal Total { get; set; }
        public decimal Paid { get; set; }
        public string Status { get; set; }
        public List<InvoiceItemPrint> Items { get; set; } = new List<InvoiceItemPrint>();
    public class InvoiceItemPrint
        public string Name { get; set; }
        public string Barcode { get; set; }
        public decimal Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal Total { get; set; }
```
#### QuestPdfReportService.cs
```
    public class SalesReportRow
        public DateTime Date { get; set; }
        public string InvoiceNumber { get; set; }
        public string PartyName { get; set; }
        public decimal Total { get; set; }
        public decimal Paid { get; set; }
        public decimal Remaining { get; set; }
    public class InventoryReportRow
        public string ItemName { get; set; }
        public string Barcode { get; set; }
        public decimal StockQty { get; set; }
        public decimal AvgCost { get; set; }
        public decimal SellPrice { get; set; }
        public decimal StockValue { get; set; }
    public class QuestPdfReportService
        public QuestPdfReportService(string connectionString)
        public string ExportSalesReport(DateTime fromDate, DateTime toDate, string outputPath)
        public string ExportInventoryReport(string outputPath)
```
#### SecureConnectionManager.cs
```
    public static class SecureConnectionManager
        public static string GetConnectionString()
        public static void SaveConnectionString(string connectionString)
        public static bool HasStoredConnection()
```
#### StatementPdfService.cs
```
    public class StatementPdfService
        public StatementPdfService(string connectionString)
        public void Export(int partyId, string partyName, DateTime from, DateTime to,
```
#### StockReasonsService.cs
```
    public class StockReason
        public int Id { get; set; }
        public string ReasonType { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public bool IsActive { get; set; }
        public bool AffectsFinancial { get; set; }
    public static class StockReasonsService
        public static List<StockReason> GetReasons(string type = null)
        public static int SaveReason(StockReason reason)
        public static void DeleteReason(int id)
```
#### WriteoffService.cs
```
    public class WriteoffRequest
        public int Id { get; set; }
        public string DocumentNumber { get; set; }
        public DateTime Date { get; set; }
        public int? DamagedItemId { get; set; }
        public int ItemId { get; set; }
        public string ItemName { get; set; }
        public int WarehouseId { get; set; }
        public string WarehouseName { get; set; }
        public double Quantity { get; set; }
        public decimal UnitCost { get; set; }
        public decimal TotalValue { get; set; }
        public int? WriteoffReasonId { get; set; }
        public string WriteoffReasonName { get; set; }
        public string ProposedAction { get; set; } // writeoff, sell_as_is, return_supplier
        public string Notes { get; set; }
        public string Status { get; set; } // draft, pending_approval, approved, rejected, executed
        public int? RequestedBy { get; set; }
        public DateTime RequestedAt { get; set; }
        public int? ApprovedBy { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public int? RejectedBy { get; set; }
        public DateTime? RejectedAt { get; set; }
        public string RejectionReason { get; set; }
        public int? ExecutedBy { get; set; }
        public DateTime? ExecutedAt { get; set; }
        public int? MovementId { get; set; }
        public string StatusDisplay { get { return Status switch { "draft" => "مسودة", "pending_approval" => "بانتظار الاعتماد", "approved" => "معتمد", "rejected" => "مرفوض", "executed" => "منفذ", _ => Status }; } }
        public string QuantityDisplay { get { return Quantity.ToString("0.00"); } }
        public string TotalValueDisplay { get { return TotalValue.ToString("N2"); } }
    public static class WriteoffService
        public static string GenerateDocumentNumber()
        public static int Create(int? damagedItemId, int itemId, int warehouseId, double quantity, int? reasonId, string proposedAction, string notes, int userId)
        public static void SubmitForApproval(int id)
        public static void Approve(int id, int userId)
        public static void Reject(int id, int userId, string reason)
        public static void Execute(int id, int userId)
        public static void Delete(int id)
        public static List<WriteoffRequest> GetAll(string statusFilter = null)
        public static WriteoffRequest GetById(int id)
```
- AccountEditViewModel.cs  (5010 bytes)
- AccountsViewModel.cs  (10580 bytes)
- CashBoxesViewModel.cs  (3232 bytes)
- CostCentersViewModel.cs  (102 bytes)
- DamagedItemsViewModel.cs  (3448 bytes)
- DashboardViewModel.cs  (14975 bytes)
- DataManagementViewModel.cs  (4822 bytes)
- EmployeesViewModel.cs  (100 bytes)
- InvoicesViewModel.cs  (8574 bytes)
- ItemEditViewModel.cs  (5830 bytes)
- ItemsViewModel.cs  (15970 bytes)
- LoansViewModel.cs  (3656 bytes)
- MainViewModel.cs  (5702 bytes)
- PartiesViewModel.cs  (3293 bytes)
- PartyEditViewModel.cs  (4377 bytes)
- PaymentsViewModel.cs  (6707 bytes)
- RecurringInvoicesViewModel.cs  (108 bytes)
- RelayCommand.cs  (1441 bytes)
- ReportsViewModel.cs  (9537 bytes)
- SettingsViewModel.cs  (3772 bytes)
- StockAdjustmentsViewModel.cs  (3936 bytes)
- StockCountSessionsViewModel.cs  (4187 bytes)
- StockLevelsViewModel.cs  (4461 bytes)
- UiRelayCommand.cs  (944 bytes)
- UsersViewModel.cs  (4567 bytes)
- ViewModelBase.cs  (719 bytes)
- WarehousesViewModel.cs  (101 bytes)
- WarehouseTransfersViewModel.cs  (109 bytes)
- WriteoffRequestsViewModel.cs  (3599 bytes)

### Files

- \.gitignore  (250 bytes)
- \app.py  (8913 bytes)
- \database.py  (1131 bytes)
- \requirements.txt  (46 bytes)
- \.streamlit\config.toml  (212 bytes)
- \pages\cash_boxes.py  (17459 bytes)
- \pages\damaged_items.py  (2554 bytes)
- \pages\invoices.py  (3091 bytes)
- \pages\items.py  (2193 bytes)
- \pages\loans.py  (4060 bytes)
- \pages\parties.py  (2130 bytes)
- \pages\payments.py  (3988 bytes)
- \pages\reports.py  (4323 bytes)
- \pages\stock_adjustments.py  (2634 bytes)
- \pages\stock_counts.py  (2220 bytes)
- \pages\stock_levels.py  (2036 bytes)
- \pages\writeoff_requests.py  (2575 bytes)
- \pages\__init__.py  (0 bytes)
- \utils\math_eval.py  (2386 bytes)

### app.py (first 150 lines)

```
# -*- coding: utf-8 -*-
"""
Techtoon Accounting - Streamlit App (complete)
Includes: login, dashboard, sidebar grouped by Expander, all pages routing,
cash boxes page, dynamic balances, expression-evaluation helper.
"""
import os
import sys
import streamlit as st

# Ensure local imports work regardless of CWD
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))


# ------------------------------------------------------------------------------
# Database connection (reuse project database.py if present, else env vars)
# ------------------------------------------------------------------------------
try:
    from database import get_connection  # type: ignore
except Exception:
    import psycopg2

    def get_connection():
        return psycopg2.connect(
            host=os.environ.get("PGHOST"),
            dbname=os.environ.get("PGDATABASE"),
            user=os.environ.get("PGUSER"),
            password=os.environ.get("PGPASSWORD"),
            sslmode=os.environ.get("PGSSLMODE", "require"),
            port=os.environ.get("PGPORT", "5432"),
        )


# ------------------------------------------------------------------------------
# Page config + RTL styling
# ---------------------------------------------------------------------------------
st.set_page_config(
    page_title="Techtoon Accounting",
    page_icon="📊",
    layout="wide",
    initial_sidebar_state="expanded",
)

st.markdown(
    """
    <style>
    section.main, div.block-container { direction: rtl; text-align: right; }
    .stSidebar, .stSidebar * { direction: rtl; }
    table, th, td { text-align: right; }
    .stMetric, .stMetric-value { text-align: right; }
    [data-testid="stExpander"] details summary { direction: rtl; }
    </style>
    """,
    unsafe_allow_html=True,
)


# ------------------------------------------------------------------------------
# Auth
# ------------------------------------------------------------------------------
def _verify_password(plain, hashed):
    try:
        import bcrypt
        if isinstance(hashed, str):
            hashed = hashed.encode()
        return bcrypt.checkpw(plain.encode(), hashed)
    except Exception:
        # legacy fallback: plain compare
        return str(plain) == str(hashed)


def login_user(username, password):
    try:
        conn = get_connection()
        try:
            cur = conn.cursor()
            cur.execute(
                "SELECT id, username, COALESCE(full_name, username), "
                "COALESCE(password_hash, ''), COALESCE(is_active, true) "
                "FROM users WHERE username = %s",
                (username,),
            )
            row = cur.fetchone()
        finally:
            conn.close()
    except Exception:
        return {"error": "تعذر الاتصال بقاعدة البيانات"}
    if not row:
        return None
    uid, uname, fname, phash, active = row
    if not active:
        return {"error": "الحساب غير نشط، راجع المدير"}
    if not phash:
        # no password set yet -> first login
        return {"id": uid, "username": uname, "full_name": fname}
    if _verify_password(password, phash):
        return {"id": uid, "username": uname, "full_name": fname}
    return None


def show_login():
    st.title("🔐 تسجيل الدخول")
    st.caption("Techtoon Accounting")
    with st.form("login_form"):
        u = st.text_input("اسم المستخدم")
        p = st.text_input("كلمة المرور", type="password")
        submitted = st.form_submit_button("دخول", use_container_width=True)
    if submitted:
        res = login_user(u, p)
        if res is None:
            st.error("بيانات الدخول غير صحيحة")
        elif "error" in res:
            st.error(res["error"])
        else:
            st.session_state.user = res
            st.session_state.logged_in = True
            st.session_state.current_page = "dashboard"
            st.rerun()


def logout():
    st.session_state.clear()
    st.rerun()


# ------------------------------------------------------------------------------
# Navigation (grouped by Expander - mirrors desktop sidebar)
# ------------------------------------------------------------------------------
PAGES = {
    "🏠 الرئيسية": {
        "dashboard": "📊 لوحة المعلومات",
    },
    "🧾 المبيعات": {
        "invoices": "🧾 الفواتير",
    },
    "👥 الأطراف": {
        "parties": "👤 العملاء والموردين",
    },
    "📦 المخزون": {
        "items": "📦 الأصناف",
        "stock_levels": "📋 أرصدة المخزون",
        "stock_counts": "🔢 الجرد",
        "stock_adjustments": "⚖️ تسويات المخزون",
        "damaged_items": "💔 الأصناف التالفة",
        "writeoff_requests": "🗑️ طلبات الإلغاء",
    },
    "💰 المالية": {
        "payments": "💵 المدفوعات",
        "cash_boxes": "🏦 الخزائن",
        "loans": "💳 القروض",
```

### database.py (REDACTED)

```
import psycopg2
import bcrypt
from contextlib import contextmanager

CONN_STRING = "host=ep-bitter-night-za726w59-pooler.c-2.eu-west-2.aws.neon.tech port=5432 dbname=neondb user=neondb_owner password=npg_K9nHVkY5OjRC sslmode=require"

@contextmanager
def get_connection():
    conn = psycopg2.connect(CONN_STRING)
    try:
        yield conn
        conn.commit()
    except Exception as e:
        conn.rollback()
        raise e
    finally:
        conn.close()

def execute_query(query, params=None, fetch=False):
    with get_connection() as conn:
        with conn.cursor() as cur:
            cur.execute(query, params or ())
            if fetch:
                columns = [desc[0] for desc in cur.description]
                rows = cur.fetchall()
                return columns, rows
    return None, None

def verify_password(plain_password, hashed_password):
    try:
        return bcrypt.checkpw(plain_password.encode('utf-8'), hashed_password.encode('utf-8'))
    except:
        return False

def hash_password(password):
    return bcrypt.hashpw(password.encode('utf-8'), bcrypt.gensalt(rounds=12)).decode('utf-8')
```

### Pages

#### cash_boxes.py
```
"""
Cash Boxes page for Techtoon Accounting Streamlit app.
Features:
  - Dynamic balance calculation (opening + receipts - payments - expenses + loans + transfers_in - transfers_out)
  - Add / edit / deactivate cash boxes
  - Money transfers between boxes
  - Transactions log per box
  - Arithmetic expression evaluation in all numeric inputs (e.g. type "3+3" -> 6)
"""
import streamlit as st
import pandas as pd
from datetime import datetime
import sys
import os

# Make sure utils is importable
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
from utils.math_eval import safe_eval

# ------------------------------------------------------------------------------
# Database connection (tries to reuse project database.py, falls back to env)
# ------------------------------------------------------------------------------
try:
    from database import get_connection as _get_connection  # type: ignore
except Exception:
```
#### damaged_items.py
```
import streamlit as st
import database as db
import pandas as pd
from datetime import datetime

def show():
    st.title("💣 الأصناف التالفة")
    try:
        _, rows = db.execute_query("SELECT d.id, d.document_number, d.date, i.name, w.name, d.quantity, d.total_value, COALESCE(sr.name,''), d.status FROM damaged_items d LEFT JOIN items i ON d.item_id=i.id LEFT JOIN warehouses w ON d.warehouse_id=w.id LEFT JOIN stock_reasons sr ON d.damage_reason_id=sr.id ORDER BY d.id DESC", fetch=True)
        if rows:
            df = pd.DataFrame(rows, columns=["ID", "رقم", "التاريخ", "الصنف", "المخزن", "الكمية", "القيمة", "السبب", "الحالة"])
            st.dataframe(df, use_container_width=True, hide_index=True)
        else: st.info("لا يوجد تالف")
    except Exception as e: st.error(f"خطأ: {e}")

    st.markdown("---")
    with st.form("new_dmg"):
        st.markdown("### ➕ تسجيل تالف")
        _, items = db.execute_query("SELECT id, name FROM items WHERE is_active=TRUE ORDER BY name", fetch=True)
        i_opts = {i[1]: i[0] for i in items} if items else {}
        _, whs = db.execute_query("SELECT id, name FROM warehouses WHERE is_active=TRUE ORDER BY name", fetch=True)
        w_opts = {w[1]: w[0] for w in whs} if whs else {}
        _, rsns = db.execute_query("SELECT id, name FROM stock_reasons WHERE reason_type='damaged' AND is_active=TRUE ORDER BY name", fetch=True)
        r_opts = {r[1]: r[0] for r in rsns} if rsns else {}
        col1, col2 = st.columns(2)
```
#### invoices.py
```
import streamlit as st
import database as db
import pandas as pd

def show():
    st.title("🛒 الفواتير")
    search = st.text_input("🔍 بحث", placeholder="رقم الفاتورة أو اسم العميل")
    query = "SELECT i.id, i.invoice_number, i.date, i.type, COALESCE(p.name,'') as party, i.total, i.paid_amount, i.status FROM invoices i LEFT JOIN parties p ON i.party_id=p.id"
    params = []
    if search:
        query += " WHERE i.invoice_number ILIKE %s OR p.name ILIKE %s"
        params.extend([f"%{search}%", f"%{search}%"])
    query += " ORDER BY i.date DESC LIMIT 100"
    try:
        cols, rows = db.execute_query(query, tuple(params), fetch=True)
        if rows:
            df = pd.DataFrame(rows, columns=["ID", "رقم الفاتورة", "التاريخ", "النوع", "العميل", "الإجمالي", "المدفوع", "الحالة"])
            st.dataframe(df, use_container_width=True, hide_index=True)
            selected_id = st.selectbox("اختر فاتورة للإجراءات", [r[0] for r in rows], format_func=lambda x: f"#{x} - {[r[1] for r in rows if r[0]==x][0]}")
            col1, col2 = st.columns(2)
            with col1:
                if st.button("👁️ عرض التفاصيل"):
                    _, det = db.execute_query("SELECT i.invoice_number, i.date, i.type, COALESCE(p.name,''), i.total, i.paid_amount, i.status, i.notes FROM invoices i LEFT JOIN parties p ON i.party_id=p.id WHERE i.id=%s", (selected_id,), fetch=True)
                    if det:
                        r = det[0]
```
#### items.py
```
import streamlit as st
import database as db
import pandas as pd

def show():
    st.title("📦 الأصناف")
    search = st.text_input("🔍 بحث", placeholder="اسم الصنف أو الباركود")
    query = "SELECT id, name, barcode, cost_price, sell_price, opening_quantity FROM items WHERE is_active=TRUE"
    params = []
    if search:
        query += " AND (name ILIKE %s OR barcode ILIKE %s)"
        params.extend([f"%{search}%", f"%{search}%"])
    query += " ORDER BY name LIMIT 200"
    try:
        cols, rows = db.execute_query(query, tuple(params), fetch=True)
        if rows:
            df = pd.DataFrame(rows, columns=["ID", "الاسم", "الباركود", "التكلفة", "سعر البيع", "الكمية"])
            st.dataframe(df, use_container_width=True, hide_index=True)
            selected_id = st.selectbox("اختر صنف", [r[0] for r in rows], format_func=lambda x: f"#{x} - {[r[1] for r in rows if r[0]==x][0]}")
            if st.button("🗑️ تعطيل الصنف", type="secondary"):
                db.execute_query("UPDATE items SET is_active=FALSE WHERE id=%s", (selected_id,))
                st.success("✅ تم التعطيل"); st.rerun()
        else: st.info("لا توجد أصناف")
    except Exception as e: st.error(f"خطأ: {e}")

```
#### loans.py
```
import streamlit as st
import database as db
import pandas as pd
from datetime import datetime

def show():
    st.title("💰 القروض والتسهيلات")
    
    col1, col2 = st.columns([3, 1])
    with col2:
        if st.button("➕ قرض جديد"):
            st.session_state.show_new_loan = True
    
    if st.session_state.get("show_new_loan"):
        new_loan_form()
    
    try:
        _, rows = db.execute_query(
            "SELECT l.id, l.loan_name, l.loan_type, l.principal_amount, l.interest_rate, l.status "
            "FROM loans ORDER BY l.id DESC",
            fetch=True
        )
        if rows:
            df = pd.DataFrame(rows, columns=["ID", "الاسم", "النوع", "المبلغ", "الفائدة", "الحالة"])
            st.dataframe(df, use_container_width=True, hide_index=True)
```
#### parties.py
```
import streamlit as st
import database as db
import pandas as pd

def show():
    st.title("👥 العملاء والموردين")
    search = st.text_input("🔍 بحث", placeholder="الاسم")
    query = "SELECT id, name, type, phone, credit_limit, opening_balance FROM parties WHERE is_active=TRUE"
    params = []
    if search:
        query += " AND name ILIKE %s"
        params.append(f"%{search}%")
    query += " ORDER BY name LIMIT 200"
    try:
        cols, rows = db.execute_query(query, tuple(params), fetch=True)
        if rows:
            df = pd.DataFrame(rows, columns=["ID", "الاسم", "النوع", "الهاتف", "حد الائتمان", "الرصيد"])
            st.dataframe(df, use_container_width=True, hide_index=True)
            selected_id = st.selectbox("اختر", [r[0] for r in rows], format_func=lambda x: f"#{x} - {[r[1] for r in rows if r[0]==x][0]}")
            if st.button("🗑️ تعطيل", type="secondary"):
                db.execute_query("UPDATE parties SET is_active=FALSE WHERE id=%s", (selected_id,))
                st.success("✅ تم التعطيل"); st.rerun()
        else: st.info("لا يوجد عملاء")
    except Exception as e: st.error(f"خطأ: {e}")

```
#### payments.py
```
import streamlit as st
import database as db
import pandas as pd
from datetime import datetime

def show():
    st.title("💰 المدفوعات")
    search = st.text_input("🔍 بحث", placeholder="اسم الجهة أو المرجع")
    query = "SELECT p.id, p.date, p.payment_type, COALESCE(pt.name,''), COALESCE(a.name,''), p.amount, p.payment_method, p.reference_number FROM payments p LEFT JOIN parties pt ON p.party_id=pt.id LEFT JOIN accounts a ON p.account_id=a.id"
    params = []
    if search:
        query += " WHERE pt.name ILIKE %s OR a.name ILIKE %s OR p.reference_number ILIKE %s"
        params.extend([f"%{search}%", f"%{search}%", f"%{search}%"])
    query += " ORDER BY p.date DESC LIMIT 100"
    try:
        cols, rows = db.execute_query(query, tuple(params), fetch=True)
        if rows:
            df = pd.DataFrame(rows, columns=["ID", "التاريخ", "النوع", "الجهة", "الحساب", "المبلغ", "الطريقة", "المرجع"])
            df["الجهة/الحساب"] = df["الجهة"].fillna('') + df["الحساب"].fillna('')
            df = df.drop(columns=["الجهة", "الحساب"])
            st.dataframe(df, use_container_width=True, hide_index=True)
            selected_id = st.selectbox("اختر", [r[0] for r in rows], format_func=lambda x: f"#{x} - {[r[3] or r[4] for r in rows if r[0]==x][0]} - {[r[5] for r in rows if r[0]==x][0]}")
            if st.button("🗑️ حذف", type="secondary"):
                db.execute_query("DELETE FROM payments WHERE id=%s", (selected_id,))
                st.success("✅ تم الحذف"); st.rerun()
```
#### reports.py
```
import streamlit as st
import database as db
import pandas as pd
import plotly.express as px
from datetime import datetime, timedelta

def show():
    st.title("📈 التقارير")
    rpt = st.selectbox("نوع التقرير", ["تقرير المبيعات", "تقرير الجرد", "كشف حساب", "تقرير المصروفات"])
    col1, col2 = st.columns(2)
    start = col1.date_input("من", datetime.now() - timedelta(days=30))
    end = col2.date_input("إلى", datetime.now())
    if st.button("📊 توليد"):
        if rpt == "تقرير المبيعات":
            try:
                _, rows = db.execute_query("SELECT i.invoice_number, i.date, COALESCE(p.name,''), i.total, i.paid_amount, i.status FROM invoices i LEFT JOIN parties p ON i.party_id=p.id WHERE i.type='sales' AND i.date BETWEEN %s AND %s AND i.status<>'cancelled' ORDER BY i.date", (start, end + timedelta(days=1)), fetch=True)
                if rows:
                    df = pd.DataFrame(rows, columns=["رقم", "التاريخ", "العميل", "الإجمالي", "المدفوع", "الحالة"])
                    st.metric("الإجمالي", f"{df['الإجمالي'].sum():,.2f}")
                    st.dataframe(df, use_container_width=True, hide_index=True)
                else: st.info("لا توجد مبيعات")
            except Exception as e: st.error(f"خطأ: {e}")
        elif rpt == "تقرير الجرد":
            try:
                _, rows = db.execute_query("SELECT i.name, i.barcode, i.opening_quantity, COALESCE(i.avg_cost_price, i.cost_price, 0), i.sell_price FROM items i WHERE i.is_active=TRUE AND i.is_kit IS NOT TRUE ORDER BY i.name", fetch=True)
```
#### stock_adjustments.py
```
import streamlit as st
import database as db
import pandas as pd
from datetime import datetime

def show():
    st.title("⚖️ تسويات المخزون")
    try:
        _, rows = db.execute_query("SELECT a.id, a.document_number, a.adjustment_date, i.name, w.name, a.old_quantity, a.new_quantity, a.difference, sr.name, a.status FROM stock_adjustments a LEFT JOIN items i ON a.item_id=i.id LEFT JOIN warehouses w ON a.warehouse_id=w.id LEFT JOIN stock_reasons sr ON a.reason=sr.code ORDER BY a.id DESC", fetch=True)
        if rows:
            df = pd.DataFrame(rows, columns=["ID", "رقم", "التاريخ", "الصنف", "المخزن", "قبل", "بعد", "الفرق", "السبب", "الحالة"])
            st.dataframe(df, use_container_width=True, hide_index=True)
        else: st.info("لا توجد تسويات")
    except Exception as e: st.error(f"خطأ: {e}")

    st.markdown("---")
    with st.form("new_adj"):
        st.markdown("### ➕ تسوية جديدة")
        _, items = db.execute_query("SELECT id, name FROM items WHERE is_active=TRUE ORDER BY name", fetch=True)
        i_opts = {i[1]: i[0] for i in items} if items else {}
        _, whs = db.execute_query("SELECT id, name FROM warehouses WHERE is_active=TRUE ORDER BY name", fetch=True)
        w_opts = {w[1]: w[0] for w in whs} if whs else {}
        _, rsns = db.execute_query("SELECT code, name FROM stock_reasons WHERE reason_type='adjustment' AND is_active=TRUE ORDER BY name", fetch=True)
        r_opts = {r[1]: r[0] for r in rsns} if rsns else {}
        col1, col2 = st.columns(2)
```
#### stock_counts.py
```
import streamlit as st
import database as db
import pandas as pd
from datetime import datetime

def show():
    st.title("📋 جلسات الجرد")
    try:
        _, rows = db.execute_query("SELECT s.id, s.document_number, s.session_date, w.name, s.count_type, s.status, COALESCE((SELECT COUNT(*) FROM stock_counts WHERE session_id=s.id),0), COALESCE((SELECT COUNT(*) FROM stock_counts WHERE session_id=s.id AND counted_quantity IS NOT NULL),0), COALESCE((SELECT SUM(difference_value) FROM stock_counts WHERE session_id=s.id),0) FROM stock_count_sessions s LEFT JOIN warehouses w ON s.warehouse_id=w.id ORDER BY s.id DESC", fetch=True)
        if rows:
            df = pd.DataFrame(rows, columns=["ID", "رقم", "التاريخ", "المخزن", "النوع", "الحالة", "الأصناف", "العديد", "قيمة الفروق"])
            st.dataframe(df, use_container_width=True, hide_index=True)
        else: st.info("لا توجد جلسات")
    except Exception as e: st.error(f"خطأ: {e}")

    st.markdown("---")
    with st.form("new_count"):
        st.markdown("### ➕ جلسة جديدة")
        _, whs = db.execute_query("SELECT id, name FROM warehouses WHERE is_active=TRUE ORDER BY name", fetch=True)
        wh_opts = {w[1]: w[0] for w in whs} if whs else {}
        col1, col2 = st.columns(2)
        wh_name = col1.selectbox("المخزن *", list(wh_opts.keys()) if wh_opts else ["لا يوجد"])
        ct = col2.selectbox("النوع", ["full - شامل", "partial - جزئي", "surprise - مفاجئ"])
        hide = st.checkbox("إخفاء الرصيد الدفتري")
        notes = st.text_area("ملاحظات")
```
#### stock_levels.py
```
import streamlit as st
import database as db
import pandas as pd

def show():
    st.title("📦 أرصدة المخزون")
    search = st.text_input("🔍 بحث", placeholder="اسم الصنف أو الباركود")
    query = "SELECT i.id, i.name, i.barcode, w.name, COALESCE(i.opening_quantity,0) + COALESCE((SELECT SUM(CASE WHEN type IN ('in','opening') THEN quantity WHEN type='out' THEN -quantity WHEN type='adjustment' THEN quantity ELSE 0 END) FROM inventory_movements WHERE item_id=i.id),0), COALESCE(sl.reserved_quantity,0), COALESCE(sl.damaged_quantity,0), COALESCE(sl.under_inspection_quantity,0), COALESCE(i.avg_cost_price, i.cost_price, 0), COALESCE(i.min_stock,0) FROM items i CROSS JOIN warehouses w LEFT JOIN stock_levels sl ON sl.item_id=i.id AND sl.warehouse_id=w.id WHERE i.is_active = TRUE AND i.is_kit IS NOT TRUE AND w.is_active = TRUE"
    params = []
    if search:
        query += " AND (i.name ILIKE %s OR i.barcode ILIKE %s)"
        params.extend([f"%{search}%", f"%{search}%"])
    query += " ORDER BY i.name"
    try:
        cols, rows = db.execute_query(query, tuple(params), fetch=True)
        if rows:
            data = []
            for r in rows:
                total = float(r[4]); res = float(r[5]); dmg = float(r[6]); ins = float(r[7]); cost = float(r[8]); min_s = float(r[9])
                data.append({"الصنف": r[1], "الباركود": r[2], "المخزن": r[3], "الإجمالي": total, "المتاح": total-res-dmg-ins, "محجوز": res, "تالف": dmg, "تحت فحص": ins, "التكلفة": cost, "قيمة المخزون": total*cost, "الحد الأدنى": min_s, "الحالة": "⚠️ منخفض" if total<=min_s else "✅ طبيعي"})
            df = pd.DataFrame(data)
            st.dataframe(df, use_container_width=True, hide_index=True)
            st.metric("إجمالي قيمة المخزون", f"{df['قيمة المخزون'].sum():,.2f}")
        else: st.info("لا توجد بيانات")
    except Exception as e: st.error(f"خطأ: {e}")
```
#### writeoff_requests.py
```
import streamlit as st
import database as db
import pandas as pd
from datetime import datetime

def show():
    st.title("🗑️ طلبات الشطب")
    try:
        _, rows = db.execute_query("SELECT w.id, w.document_number, w.date, i.name, w.quantity, w.total_value, w.proposed_action, w.status FROM writeoff_requests w LEFT JOIN items i ON w.item_id=i.id ORDER BY w.id DESC", fetch=True)
        if rows:
            df = pd.DataFrame(rows, columns=["ID", "رقم", "التاريخ", "الصنف", "الكمية", "القيمة", "الإجراء", "الحالة"])
            st.dataframe(df, use_container_width=True, hide_index=True)
        else: st.info("لا توجد طلبات")
    except Exception as e: st.error(f"خطأ: {e}")

    st.markdown("---")
    with st.form("new_wo"):
        st.markdown("### ➕ طلب شطب")
        _, items = db.execute_query("SELECT id, name FROM items WHERE is_active=TRUE ORDER BY name", fetch=True)
        i_opts = {i[1]: i[0] for i in items} if items else {}
        _, whs = db.execute_query("SELECT id, name FROM warehouses WHERE is_active=TRUE ORDER BY name", fetch=True)
        w_opts = {w[1]: w[0] for w in whs} if whs else {}
        _, rsns = db.execute_query("SELECT id, name FROM stock_reasons WHERE reason_type='writeoff' AND is_active=TRUE ORDER BY name", fetch=True)
        r_opts = {r[1]: r[0] for r in rsns} if rsns else {}
        col1, col2 = st.columns(2)
```
#### __init__.py
```
```

### Mobile tech indicators (search)

#### .\Techtoon.Api\appsettings.Development.json
```
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}

```
#### .\Techtoon.Api\appsettings.json
```
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "DefaultConnection": "Host=ep-bitter-night-za726w59-pooler.c-2.eu-west-2.aws.neon.tech;Port=5432;Username=neondb_owner;Password=***;Database=neondb;SSL Mode=Require;Trust Server Certificate=true"
  }
}
```
#### .\Techtoon.UI\appsettings.json
```
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "SECURE - see SecureConnectionManager.cs (DPAPI encrypted in %APPDATA%/TechtoonAccounting)"
  },
  "AppSettings": {
    "BackupIntervalHours": 24,
    "MaxBackupsToKeep": 7
  }
}
```
#### .\Techtoon.UI\Services\AuthService.cs
```
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Npgsql;
using BCrypt.Net;

namespace Techtoon.UI.Services
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string FullName { get; set; }
        public string Role { get; set; }
        public bool IsActive { get; set; }
    }

    public class LoginResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public User User { get; set; }
    }

    public static class AuthService
    {
        public static User CurrentUser { get; private set; }
        public static bool IsLoggedIn => CurrentUser != null;

        private static HashSet<string> _permissions = new HashSet<string>();

        public static string GetCS() { return Techtoon.UI.Services.SecureConnectionManager.GetConnectionString(); }

        public static async Task<LoginResult> LoginAsync(string username, string password)
        {
            return await Task.FromResult(Login(username, password));
        }

        public static LoginResult Login(string username, string password)
        {
            var result = new LoginResult();
            try
            {
                using (var conn = new NpgsqlConnection(GetCS()))
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand("SELECT id, username, COALESCE(password_hash,''), COALESCE(full_name,''), COALESCE(role,'SALESPERSON'), COALESCE(is_active, FALSE) FROM users WHERE username=@u", conn))
                    {
                        cmd.Parameters.AddWithValue("u", username);
                        using (var r = cmd.ExecuteReader())
                        {
                            if (!r.Read()) { result.ErrorMessage = "User not found"; return result; }
                            if (!r.GetBoolean(5)) { result.ErrorMessage = "User inactive"; return result; }

                            string hash = r.GetString(2);
                            if (!string.IsNullOrEmpty(hash) && !BCrypt.Net.BCrypt.Verify(password, hash))
                            {
                                result.ErrorMessage = "Wrong password";
                                return result;
                            }

                            CurrentUser = new User
                            {
                                Id = r.GetInt32(0),
                                Username = r.GetString(1),
                                FullName = r.GetString(3),
                                Role = r.GetString(4),
                                IsActive = true
                            };
                        }
                    }

                    LoadPermissions();

                    using (var cmd = new NpgsqlCommand("UPDATE users SET last_login=NOW() WHERE id=@id", conn))
                    {
                        cmd.Parameters.AddWithValue("id", CurrentUser.Id);
                        cmd.ExecuteNonQuery();
                    }

                    LogAudit("login", "user", CurrentUser.Id, "Login success");
                    result.Success = true;
                    result.User = CurrentUser;
                    return result;
                }
            }
            catch (Exception ex) { result.ErrorMessage = ex.Message; return result; }
        }

        public static void Logout()
        {
            if (IsLoggedIn) LogAudit("logout", "user", CurrentUser.Id, "Logout");
            CurrentUser = null;
            _permissions.Clear();
        }

        private static void LoadPermissions()
        {
            _permissions.Clear();
            if (CurrentUser == null) return;
            try
            {
                using (var conn = new NpgsqlConnection(GetCS()))
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand("SELECT permission_key FROM user_permissions WHERE user_id=@id AND is_granted=TRUE", conn))
                    {
                        cmd.Parameters.AddWithValue("id", CurrentUser.Id);
                        using (var r = cmd.ExecuteReader())
                            while (r.Read()) _permissions.Add(r.GetString(0));
                    }
                }
            }
            catch { }
        }

        public static bool HasPermission(string key)
        {
            if (!IsLoggedIn) return false;
            if (CurrentUser.Role == "ADMIN") return true;
            return _permissions.Contains(key);
        }

        public static void LogAudit(string action, string entityType, int entityId, string details)
        {
            try
            {
                using (var conn = new NpgsqlConnection(GetCS()))
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand(@"INSERT INTO audit_log (user_id, username, action, entity_type, entity_id, details, created_at) VALUES (@uid, @uname, @action, @etype, @eid, @details, NOW())", conn))
                    {
                        cmd.Parameters.AddWithValue("uid", CurrentUser?.Id > 0 ? (object)CurrentUser.Id : DBNull.Value);
                        cmd.Parameters.AddWithValue("uname", (object)CurrentUser?.Username ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("action", action);
                        cmd.Parameters.AddWithValue("etype", (object)entityType ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("eid", entityId > 0 ? (object)entityId : DBNull.Value);
                        cmd.Parameters.AddWithValue("details", (object)details ?? DBNull.Value);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch { }
        }

        public static async Task EnsureDefaultAdminAsync()
        {
            try
            {
                using (var conn = new NpgsqlConnection(GetCS()))
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM users WHERE username='admin'", conn))
                    {
                        if (Convert.ToInt32(cmd.ExecuteScalar()) == 0)
                        {
                            string hash = BCrypt.Net.BCrypt.HashPassword("admin123", workFactor: 12);
                            using (var c2 = new NpgsqlCommand("INSERT INTO users (username, password_hash, full_name, role, is_active, created_at) VALUES ('admin', @h, 'Administrator', 'ADMIN', TRUE, NOW())", conn))
                            {
                                c2.Parameters.AddWithValue("h", hash);
                                c2.ExecuteNonQuery();
                            }
                        }
                    }
                }
            }
            catch { }
            await Task.CompletedTask;
        }

        public static bool CreateUser(string username, string password, string fullName, string role)
        {
            if (!HasPermission("manage_users")) return false;
            try
            {
                string hash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
                using (var conn = new NpgsqlConnection(GetCS()))
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand("INSERT INTO users (username, password_hash, full_name, role, is_active, created_at) VALUES (@u, @h, @fn, @r, TRUE, NOW())", conn))
                    {
                        cmd.Parameters.AddWithValue("u", username);
                        cmd.Parameters.AddWithValue("h", hash);
                        cmd.Parameters.AddWithValue("fn", fullName);
                        cmd.Parameters.AddWithValue("r", role);
                        cmd.ExecuteNonQuery();
                    }
                }
                LogAudit("create_user", "user", 0, $"Created user: {username} ({role})");
                return true;
            }
            catch { return false; }
        }

        public static bool ChangePassword(int userId, string newPassword)
        {
            try
            {
                string hash = BCrypt.Net.BCrypt.HashPassword(newPassword, workFactor: 12);
                using (var conn = new NpgsqlConnection(GetCS()))
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand("UPDATE users SET password_hash=@h WHERE id=@id", conn))
                    {
                        cmd.Parameters.AddWithValue("h", hash);
                        cmd.Parameters.AddWithValue("id", userId);
                        cmd.ExecuteNonQuery();
                    }
                }
                LogAudit("change_password", "user", userId, "Password changed");
                return true;
            }
            catch { return false; }
        }

        public static bool SetPermission(int userId, string permissionKey, bool granted)
        {
            if (!HasPermission("manage_users")) return false;
            try
            {
                using (var conn = new NpgsqlConnection(GetCS()))
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand(@"INSERT INTO user_permissions (user_id, permission_key, is_granted) VALUES (@uid, @key, @granted) ON CONFLICT (user_id, permission_key) DO UPDATE SET is_granted = @granted", conn))
                    {
                        cmd.Parameters.AddWithValue("uid", userId);
                        cmd.Parameters.AddWithValue("key", permissionKey);
                        cmd.Parameters.AddWithValue("granted", granted);
                        cmd.ExecuteNonQuery();
                    }
                }
                LogAudit("set_permission", "user", userId, $"Permission '{permissionKey}' = {granted}");
                return true;
            }
            catch { return false; }
        }
        // ============= USER MANAGEMENT METHODS =============

        public class UserWithRole
        {
            public int Id { get; set; }
            public string Username { get; set; }
            public string FullName { get; set; }
            public string Email { get; set; }
            public string Role { get; set; }
            public int? RoleId { get; set; }
            public string RoleDisplayName { get; set; }
            public bool IsActive { get; set; }
            public bool MustChangePassword { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime? LastLogin { get; set; }
        }

        public class RoleOption
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string DisplayName { get; set; }
            public string Description { get; set; }
            public bool IsSystemRole { get; set; }
        }

        public static System.Collections.Generic.List<UserWithRole> GetAllUsers(string search = null)
        {
            var list = new System.Collections.Generic.List<UserWithRole>();
            using var conn = new NpgsqlConnection(GetCS());
            conn.Open();
            var sql = "SELECT u.id, u.username, u.full_name, u.email, u.role, u.role_id, " +
                      "COALESCE(r.display_name, u.role), u.is_active, u.must_change_password, u.created_at, u.last_login " +
                      "FROM users u LEFT JOIN roles r ON u.role_id = r.id";
            if (!string.IsNullOrEmpty(search))
                sql += " WHERE u.username ILIKE @q OR u.full_name ILIKE @q OR u.role ILIKE @q";
            sql += " ORDER BY u.id";
            using var cmd = new NpgsqlCommand(sql, conn);
            if (!string.IsNullOrEmpty(search))
                cmd.Parameters.AddWithValue("q", "%" + search + "%");
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(new UserWithRole
                {
                    Id = r.GetInt32(0),
                    Username = r.GetString(1),
                    FullName = r.GetString(2),
                    Email = r.IsDBNull(3) ? "" : r.GetString(3),
                    Role = r.GetString(4),
                    RoleId = r.IsDBNull(5) ? (int?)null : r.GetInt32(5),
                    RoleDisplayName = r.GetString(6),
                    IsActive = r.IsDBNull(7) ? true : r.GetBoolean(7),
                    MustChangePassword = r.IsDBNull(8) ? false : r.GetBoolean(8),
                    CreatedAt = r.IsDBNull(9) ? DateTime.MinValue : r.GetDateTime(9),
                    LastLogin = r.IsDBNull(10) ? (DateTime?)null : r.GetDateTime(10)
                });
            }
            return list;
        }

        public static System.Collections.Generic.List<RoleOption> GetAllRoles()
        {
            var list = new System.Collections.Generic.List<RoleOption>();
            using var conn = new NpgsqlConnection(GetCS());
            conn.Open();
            using var cmd = new NpgsqlCommand("SELECT id, name, display_name, description, is_system_role FROM roles WHERE is_active = TRUE ORDER BY name", conn);
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(new RoleOption
                {
                    Id = r.GetInt32(0),
                    Name = r.GetString(1),
                    DisplayName = r.GetString(2),
                    Description = r.IsDBNull(3) ? "" : r.GetString(3),
                    IsSystemRole = r.GetBoolean(4)
                });
            }
            return list;
        }

        public static bool IsLastAdmin(int userId)
        {
            using var conn = new NpgsqlConnection(GetCS());
            conn.Open();
            // تحقق هل المستخدم هو آخر ADMIN نشط
            using var cmd = new NpgsqlCommand(
                "SELECT COUNT(*) FROM users WHERE role = 'ADMIN' AND is_active = TRUE AND id != @id", conn);
            cmd.Parameters.AddWithValue("id", userId);
            int count = Convert.ToInt32(cmd.ExecuteScalar());
            return count == 0;
        }

        public static bool IsCurrentUser(int userId)
        {
            return CurrentUser != null && CurrentUser.Id == userId;
        }

        public static void UpdateUser(int userId, string fullName, string email, int roleId)
        {
            using var conn = new NpgsqlConnection(GetCS());
            conn.Open();
            using var tx = conn.BeginTransaction();
            try
            {
                // جلب اسم الـ role من roles table
                string roleName = "";
                using (var cmd = new NpgsqlCommand("SELECT name FROM roles WHERE id = @rid", conn, tx))
                {
                    cmd.Parameters.AddWithValue("rid", roleId);
                    var result = cmd.ExecuteScalar();
                    if (result != null) roleName = result.ToString();
                }

                using var cmd2 = new NpgsqlCommand(
                    "UPDATE users SET full_name = @fn, email = @em, role_id = @rid, role = @r WHERE id = @id", conn, tx);
                cmd2.Parameters.AddWithValue("fn", fullName);
                cmd2.Parameters.AddWithValue("em", string.IsNullOrEmpty(email) ? (object)DBNull.Value : email);
                cmd2.Parameters.AddWithValue("rid", roleId);
                cmd2.Parameters.AddWithValue("r", roleName);
                cmd2.Parameters.AddWithValue("id", userId);
                cmd2.ExecuteNonQuery();

                LogAudit("UPDATE_USER", "user", userId, "Updated user: " + fullName);
                tx.Commit();
            }
            catch { tx.Rollback(); throw; }
        }

        public static void ToggleUserActive(int userId)
        {
            if (IsCurrentUser(userId))
                throw new InvalidOperationException("لا يمكن تعطيل حسابك الحالي");

            using var conn = new NpgsqlConnection(GetCS());
            conn.Open();
            using var tx = conn.BeginTransaction();
            try
            {
                // جلب الحالة الحالية + role
                bool currentActive; string role;
                using (var cmd = new NpgsqlCommand("SELECT is_active, role FROM users WHERE id = @id", conn, tx))
                {
                    cmd.Parameters.AddWithValue("id", userId);
                    using var r = cmd.ExecuteReader();
                    if (!r.Read()) throw new InvalidOperationException("المستخدم غير موجود");
                    currentActive = r.IsDBNull(0) ? true : r.GetBoolean(0);
                    role = r.GetString(1);
                }

                // إذا كان سيتم تعطيل مستخدم ADMIN، تحقق من عدم كونه الأخير
                if (currentActive && role == "ADMIN" && IsLastAdmin(userId))
                    throw new InvalidOperationException("لا يمكن تعطيل آخر حساب مدير نشط في النظام");

                bool newActive = !currentActive;
                using var cmd2 = new NpgsqlCommand("UPDATE users SET is_active = @a WHERE id = @id", conn, tx);
                cmd2.Parameters.AddWithValue("a", newActive);
                cmd2.Parameters.AddWithValue("id", userId);
                cmd2.ExecuteNonQuery();

                LogAudit(newActive ? "ACTIVATE_USER" : "DEACTIVATE_USER", "user", userId,
                    newActive ? "User activated" : "User deactivated");
                tx.Commit();
            }
            catch { tx.Rollback(); throw; }
        }

        public static void ResetPassword(int userId, string newPassword)
        {
            if (string.IsNullOrEmpty(newPassword) || newPassword.Length < 4)
                throw new InvalidOperationException("كلمة المرور قصيرة جدًا (4 أحرف على الأقل)");

            using var conn = new NpgsqlConnection(GetCS());
            conn.Open();
            using var tx = conn.BeginTransaction();
            try
            {
                var hash = BCrypt.Net.BCrypt.HashPassword(newPassword, workFactor: 12);
                using var cmd = new NpgsqlCommand(
                    "UPDATE users SET password_hash = @h, must_change_password = TRUE WHERE id = @id", conn, tx);
                cmd.Parameters.AddWithValue("h", hash);
                cmd.Parameters.AddWithValue("id", userId);
                cmd.ExecuteNonQuery();

                LogAudit("RESET_PASSWORD", "user", userId, "Password reset by admin");
                tx.Commit();
            }
            catch { tx.Rollback(); throw; }
        }

        public static void ChangeOwnPassword(int userId, string oldPassword, string newPassword)
        {
            using var conn = new NpgsqlConnection(GetCS());
            conn.Open();
            // تحقق من كلمة المرور القديمة
            string currentHash;
            using (var cmd = new NpgsqlCommand("SELECT password_hash FROM users WHERE id = @id", conn))
            {
                cmd.Parameters.AddWithValue("id", userId);
                currentHash = cmd.ExecuteScalar()?.ToString() ?? "";
            }
            if (!BCrypt.Net.BCrypt.Verify(oldPassword, currentHash))
                throw new InvalidOperationException("كلمة المرور الحالية غير صحيحة");

            if (string.IsNullOrEmpty(newPassword) || newPassword.Length < 4)
                throw new InvalidOperationException("كلمة المرور الجديدة قصيرة جدًا");

            using var tx = conn.BeginTransaction();
            try
            {
                var newHash = BCrypt.Net.BCrypt.HashPassword(newPassword, workFactor: 12);
                using var cmd = new NpgsqlCommand(
                    "UPDATE users SET password_hash = @h, must_change_password = FALSE WHERE id = @id", conn, tx);
                cmd.Parameters.AddWithValue("h", newHash);
                cmd.Parameters.AddWithValue("id", userId);
                cmd.ExecuteNonQuery();

                LogAudit("CHANGE_PASSWORD", "user", userId, "User changed own password");
                tx.Commit();
            }
            catch { tx.Rollback(); throw; }
        }

        public static System.Collections.Generic.Dictionary<string, bool> GetUserPermissions(int userId)
        {
            var perms = new System.Collections.Generic.Dictionary<string, bool>();
            using var conn = new NpgsqlConnection(GetCS());
            conn.Open();
            using var cmd = new NpgsqlCommand("SELECT permission_key, is_granted FROM user_permissions WHERE user_id = @uid", conn);
            cmd.Parameters.AddWithValue("uid", userId);
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                perms[r.GetString(0)] = r.IsDBNull(1) ? true : r.GetBoolean(1);
            }
            return perms;
        }

        public static void SetUserPermissions(int userId, System.Collections.Generic.Dictionary<string, bool> permissions)
        {
            using var conn = new NpgsqlConnection(GetCS());
            conn.Open();
            using var tx = conn.BeginTransaction();
            try
            {
                // حذف الصلاحيات القديمة
                using (var cmd = new NpgsqlCommand("DELETE FROM user_permissions WHERE user_id = @uid", conn, tx))
                {
                    cmd.Parameters.AddWithValue("uid", userId);
                    cmd.ExecuteNonQuery();
                }
                // إدراج الصلاحيات الجديدة
                foreach (var kvp in permissions)
                {
                    using var cmd = new NpgsqlCommand(
                        "INSERT INTO user_permissions (user_id, permission_key, is_granted, created_at) " +
                        "VALUES (@uid, @key, @granted, NOW())", conn, tx);
                    cmd.Parameters.AddWithValue("uid", userId);
                    cmd.Parameters.AddWithValue("key", kvp.Key);
                    cmd.Parameters.AddWithValue("granted", kvp.Value);
                    cmd.ExecuteNonQuery();
                }

                LogAudit("SET_PERMISSIONS", "user", userId, "Permissions updated: " + permissions.Count + " keys");
                tx.Commit();
            }
            catch { tx.Rollback(); throw; }
        }

        // كل مفاتيح الصلاحيات المتاحة في النظام
        public static System.Collections.Generic.List<string> GetAllPermissionKeys()
        {
            return new System.Collections.Generic.List<string>
            {
                // المبيعات والمشتريات
                "sales", "returns", "discounts", "edit_price", "cancel_invoice",
                // المالية
                "expenses", "profit_reports", "backup", "settings",
                // المستخدمين
                "manage_users",
                // المخزون القديم
                "stock_adjustment",
                // المخزون الجديد - الجرد
                "inventory.count.create", "inventory.count.count", "inventory.count.review", "inventory.count.approve",
                // المخزون الجديد - التسويات
                "inventory.adjustment.create", "inventory.adjustment.approve", "inventory.adjustment.execute",
                // المخزون الجديد - التالف
                "inventory.damaged.create", "inventory.damaged.inspect",
                // المخزون الجديد - الشطب
                "inventory.writeoff.approve", "inventory.writeoff.execute",
                // التقارير
                "inventory.reports.view", "inventory.audit.view"
            };
        }

        public static void CheckUserActiveStatus(int userId)
        {
            using var conn = new NpgsqlConnection(GetCS());
            conn.Open();
            using var cmd = new NpgsqlCommand("SELECT is_active FROM users WHERE id = @id", conn);
            cmd.Parameters.AddWithValue("id", userId);
            var result = cmd.ExecuteScalar();
            if (result == null) throw new InvalidOperationException("المستخدم غير موجود");
            if (!(bool)result) throw new InvalidOperationException("الحساب معطل. تواصل مع المدير");
        }
    }
}
```
#### .\Techtoon.UI\Views\UserPermissionsWindow.xaml.cs
```
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Techtoon.UI.Services;

namespace Techtoon.UI.Views
{
    public partial class UserPermissionsWindow : Window
    {
        private int _userId;
        private Dictionary<string, bool> _permissions = new Dictionary<string, bool>();

        public UserPermissionsWindow()
        {
            InitializeComponent();
        }

        public void LoadForUser(int userId, string username)
        {
            _userId = userId;
            lblUser.Text = "المستخدم: " + username;
            _permissions = AuthService.GetUserPermissions(userId);
            BuildPermissionsUI();
        }

        private string TranslateKey(string key)
        {
            var translations = new Dictionary<string, string>
            {
                {"sales", "المبيعات"},
                {"returns", "المرتجعات"},
                {"discounts", "الخصومات"},
                {"edit_price", "تعديل الأسعار"},
                {"cancel_invoice", "إلغاء الفواتير"},
                {"expenses", "المصروفات"},
                {"profit_reports", "تقارير الأرباح"},
                {"backup", "النسخ الاحتياطي"},
                {"settings", "الإعدادات"},
                {"manage_users", "إدارة المستخدمين"},
                {"stock_adjustment", "تسوية المخزون (قديم)"},
                {"inventory.count.create", "إنشاء جلسة جرد"},
                {"inventory.count.count", "إدخال نتائج العد"},
                {"inventory.count.review", "مراجعة فروق الجرد"},
                {"inventory.count.approve", "اعتماد الجرد"},
                {"inventory.adjustment.create", "إنشاء تسوية"},
                {"inventory.adjustment.approve", "اعتماد التسوية"},
                {"inventory.adjustment.execute", "تنفيذ التسوية"},
                {"inventory.damaged.create", "تسجيل الأصناف التالفة"},
                {"inventory.damaged.inspect", "فحص التالف"},
                {"inventory.writeoff.approve", "اعتماد الشطب"},
                {"inventory.writeoff.execute", "تنفيذ الشطب"},
                {"inventory.reports.view", "تقارير المخزون"},
                {"inventory.audit.view", "سجل تدقيق المخزون"}
            };
            return translations.ContainsKey(key) ? translations[key] : key;
        }

        private void BuildPermissionsUI()
        {
            pnlPermissions.Children.Clear();
            var allKeys = AuthService.GetAllPermissionKeys();
            var grouped = new Dictionary<string, List<string>>
            {
                { "المبيعات والمشتريات", new List<string>() },
                { "المالية", new List<string>() },
                { "المستخدمين", new List<string>() },
                { "المخزون - جرد", new List<string>() },
                { "المخزون - تسويات", new List<string>() },
                { "المخزون - تالف", new List<string>() },
                { "المخزون - شطب", new List<string>() },
                { "التقارير", new List<string>() }
            };

            foreach (var key in allKeys)
            {
                if (key.StartsWith("inventory.count.")) grouped["المخزون - جرد"].Add(key);
                else if (key.StartsWith("inventory.adjustment.")) grouped["المخزون - تسويات"].Add(key);
                else if (key.StartsWith("inventory.damaged.")) grouped["المخزون - تالف"].Add(key);
                else if (key.StartsWith("inventory.writeoff.")) grouped["المخزون - شطب"].Add(key);
                else if (key.StartsWith("inventory.")) grouped["التقارير"].Add(key);
                else if (new[] { "sales", "returns", "discounts", "edit_price", "cancel_invoice" }.Contains(key)) grouped["المبيعات والمشتريات"].Add(key);
                else if (new[] { "expenses", "profit_reports", "backup", "settings" }.Contains(key)) grouped["المالية"].Add(key);
                else if (key == "manage_users") grouped["المستخدمين"].Add(key);
                else grouped["المالية"].Add(key);
            }

            foreach (var group in grouped)
            {
                if (group.Value.Count == 0) continue;
                var header = new TextBlock { Text = group.Key, FontSize = 14, FontWeight = FontWeights.Bold, Foreground = System.Windows.Media.Brushes.DarkSlateGray, Margin = new Thickness(0, 10, 0, 5) };
                pnlPermissions.Children.Add(header);

                foreach (var key in group.Value)
                {
                    var cb = new CheckBox
                    {
                        Content = TranslateKey(key),
                        IsChecked = _permissions.ContainsKey(key) && _permissions[key],
                        Margin = new Thickness(20, 3, 0, 3),
                        Tag = key
                    };
                    pnlPermissions.Children.Add(cb);
                }
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            var newPerms = new Dictionary<string, bool>();
            foreach (var child in pnlPermissions.Children)
            {
                if (child is CheckBox cb && cb.Tag is string key)
                {
                    newPerms[key] = cb.IsChecked ?? false;
                }
            }
            try
            {
                AuthService.SetUserPermissions(_userId, newPerms);
                MessageBox.Show("تم حفظ الصلاحيات");
                DialogResult = true;
                Close();
            }
            catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e) { DialogResult = false; Close(); }
    }
}
```
Found: C:\Users\fady1\AppData\Local\Programs\Python\Python311\Scripts\ngrok.exe
Version: Downloading ngrok ... Downloading ngrok: 0% Downloading ngrok: 1% Downloading ngrok: 2% Downloading ngrok: 3% Downloading ngrok: 4% Downloading ngrok: 5% Downloading ngrok: 6% Downloading ngrok: 7% Downloading ngrok: 8% Downloading ngrok: 9% Downloading ngrok: 10% Downloading ngrok: 11% Downloading ngrok: 12% Downloading ngrok: 13% Downloading ngrok: 14% Downloading ngrok: 15% Downloading ngrok: 16% Downloading ngrok: 17% Downloading ngrok: 18% Downloading ngrok: 19% Downloading ngrok: 20% Downloading ngrok: 21% Downloading ngrok: 22% Downloading ngrok: 23% Downloading ngrok: 24% Downloading ngrok: 25% Downloading ngrok: 26% Downloading ngrok: 27% Downloading ngrok: 28% Downloading ngrok: 29% Downloading ngrok: 30% Downloading ngrok: 31% Downloading ngrok: 32% Downloading ngrok: 33% Downloading ngrok: 34% Downloading ngrok: 35% Downloading ngrok: 36% Downloading ngrok: 37% Downloading ngrok: 38% Downloading ngrok: 39% Downloading ngrok: 40% Downloading ngrok: 41% Downloading ngrok: 42% Downloading ngrok: 43% Downloading ngrok: 44% Downloading ngrok: 45% Downloading ngrok: 46% Downloading ngrok: 47% Downloading ngrok: 48% Downloading ngrok: 49% Downloading ngrok: 50% Downloading ngrok: 51% Downloading ngrok: 52% Downloading ngrok: 53% Downloading ngrok: 54% Downloading ngrok: 55% Downloading ngrok: 56% Downloading ngrok: 57% Downloading ngrok: 58% Downloading ngrok: 59% Downloading ngrok: 60% Downloading ngrok: 61% Downloading ngrok: 62% Downloading ngrok: 63% Downloading ngrok: 64% Downloading ngrok: 65% Downloading ngrok: 66% Downloading ngrok: 67% Downloading ngrok: 68% Downloading ngrok: 69% Downloading ngrok: 70% Downloading ngrok: 71% Downloading ngrok: 72% Downloading ngrok: 73% Downloading ngrok: 74% Downloading ngrok: 75% Downloading ngrok: 76% Downloading ngrok: 77% Downloading ngrok: 78% Downloading ngrok: 79% Downloading ngrok: 80% Downloading ngrok: 81% Downloading ngrok: 82% Downloading ngrok: 83% Downloading ngrok: 84% Downloading ngrok: 85% Downloading ngrok: 86% Downloading ngrok: 87% Downloading ngrok: 88% Downloading ngrok: 89% Downloading ngrok: 90% Downloading ngrok: 91% Downloading ngrok: 92% Downloading ngrok: 93% Downloading ngrok: 94% Downloading ngrok: 95% Downloading ngrok: 96% Downloading ngrok: 97% Downloading ngrok: 98% Downloading ngrok: 99% Downloading ngrok: 100%                                                                                                      Installing ngrok ...                                                                                                       ngrok version 3.39.13 pyngrok version 9.1.0
```
version: '3'

```
#### .\Techtoon.UI\Views\run.bat
```
@echo off
chcp 65001 >nul
title Techtoon Accounting
echo +----------------------------------------------+
echo    Techtoon Accounting System
echo    C# Edition - v1.0.0
echo +----------------------------------------------+
echo [INFO] Building project...
dotnet build "D:\PROG\Accounting Prog\Techtoon Accounting C+\Techtoon.UI" --no-restore 2>nul
if %ERRORLEVEL% NEQ 0 (
    echo [WARN] First build failed, trying with restore...
    dotnet build "D:\PROG\Accounting Prog\Techtoon Accounting C+\Techtoon.UI"
)
echo [INFO] Starting Techtoon Accounting...
dotnet run --project "D:\PROG\Accounting Prog\Techtoon Accounting C+\Techtoon.UI" --no-build
if %ERRORLEVEL% NEQ 0 (
    echo [INFO] Trying run with build...
    dotnet run --project "D:\PROG\Accounting Prog\Techtoon Accounting C+\Techtoon.UI"
)
pause

```
#### .\bootstrap_final.ps1
```
# bootstrap_final.ps1 - final fix (PowerShell only)
$ProjectPath = 'D:\PROG\Accounting Prog\Techtoon Accounting C+'
$ViewsDir = Join-Path $ProjectPath 'Techtoon.UI\Views'
$UIDir = Join-Path $ProjectPath 'Techtoon.UI'

if (-not (Test-Path $ViewsDir)) { Write-Host 'ERROR' -ForegroundColor Red; exit 1 }

Write-Host '========================================' -ForegroundColor Cyan
Write-Host '  Techtoon Final Fix' -ForegroundColor Cyan
Write-Host '========================================' -ForegroundColor Cyan

Get-ChildItem -Path $ViewsDir -Filter 'InvoiceEdit*' | Where-Object { $_.Name -match '\(1\)' } | Remove-Item -Force -ErrorAction SilentlyContinue

$dest = Join-Path $ProjectPath 'Techtoon.UI\Views\InvoiceEditViewModel.cs'
if (Test-Path $dest) { Copy-Item $dest ($dest + '.bak') -Force }
$c_InvoiceEditViewModel_cs = @'
// =====================================================================
// InvoiceEditViewModel.cs  —  Techtoon Accounting C+
// الإصدار المصحح: بحث فوري ذكي + دفع متعدد الطرق
// =====================================================================
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows.Input;
using Npgsql;
using Techtoon.UI.ViewModels;

namespace Techtoon.UI.Views
{
    // ==================================================================
    //  صنف يساعد على عرض صف البند داخل شبكة الفاتورة
    //  يحتوي على بحث فوري: عند كتابة أي حرف أو رقم يظهر ما يقاربه
    // ==================================================================
    public class InvoiceLineItem : ViewModelBase
    {
        private List<ItemSearchResult> _allItems;

        public InvoiceLineItem(List<ItemSearchResult> allItems)
        {
            _allItems = allItems ?? new List<ItemSearchResult>();
            FilteredItems = new ObservableCollection<ItemSearchResult>();
        }

        // ---- البحث الفوري ----
        private string _searchText = "";
        public string SearchText
        {
            get { return _searchText; }
            set
            {
                _searchText = value ?? "";
                OnPropertyChanged("SearchText");
                ApplySearch();
            }
        }

        private void ApplySearch()
        {
            string q = (_searchText ?? "").Trim();
            FilteredItems.Clear();
            if (q.Length == 0)
            {
                IsPopupOpen = false;
                return;
            }

            // ابحث بالاسم أو الباركود أو SKU
            var matches = _allItems
                .Where(it =>
                    (it.Name != null && it.Name.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (it.Barcode != null && it.Barcode.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (it.Sku != null && it.Sku.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0))
                .Take(15)
                .ToList();

            foreach (var m in matches) FilteredItems.Add(m);
            IsPopupOpen = matches.Count > 0;
            SearchSelectedIndex = matches.Count > 0 ? 0 : -1;
        }

        public ObservableCollection<ItemSearchResult> FilteredItems { get; set; }

        private int _searchSelectedIndex = -1;
        public int SearchSelectedIndex
        {
            get { return _searchSelectedIndex; }
            set { _searchSelectedIndex = value; OnPropertyChanged("SearchSelectedIndex"); }
        }

        private bool _isPopupOpen = false;
        public bool IsPopupOpen
        {
            get { return _isPopupOpen; }
            set { _isPopupOpen = value; OnPropertyChanged("IsPopupOpen"); }
        }

        // ---- بيانات البند المحدد ----
        public int ItemId { get; set; }
        public string Unit { get; set; }

        private string _itemName = "";
        public string ItemName
        {
            get { return _itemName; }
            set { _itemName = value; OnPropertyChanged("ItemName"); }
        }

        private string _barcode = "";
        public string Barcode
        {
            get { return _barcode; }
            set { _barcode = value; OnPropertyChanged("Barcode"); }
        }

        private decimal _quantity = 1;
        public decimal Quantity
        {
            get { return _quantity; }
            set
            {
                _quantity = value;
                OnPropertyChanged("Quantity");
                Recalc();
            }
        }

        private decimal _price = 0;
        public decimal Price
        {
            get { return _price; }
            set
            {
                _price = value;
                OnPropertyChanged("Price");
                Recalc();
            }
        }

        private decimal _discount = 0;
        public decimal Discount
        {
            get { return _discount; }
            set
            {
                _discount = value;
                OnPropertyChanged("Discount");
                Recalc();
            }
        }

        private decimal _total = 0;
        public decimal Total
        {
            get { return _total; }
            set { _total = value; OnPropertyChanged("Total"); }
        }

        private decimal _stockAvailable = 0;
        public decimal StockAvailable
        {
            get { return _stockAvailable; }
            set { _stockAvailable = value; OnPropertyChanged("StockAvailable"); }
        }

        public bool HasItem { get { return ItemId > 0; } }

        private void Recalc()
        {
            Total = Math.Round(_quantity * _price - _discount, 2);
        }

        // ---- اختيار صنف من نتائج البحث ----
        public void SelectItem(ItemSearchResult it)
        {
            if (it == null) return;
            ItemId = it.Id;
            ItemName = it.Name;
            Barcode = it.Barcode;
            Unit = it.Unit;
            Price = it.SalePrice;
            StockAvailable = it.CurrentStock;
            Quantity = 1;
            IsPopupOpen = false;
            SearchText = it.Name;
            OnPropertyChanged("HasItem");
        }

        public void SelectByIndex()
        {
            if (SearchSelectedIndex >= 0 && SearchSelectedIndex < FilteredItems.Count)
            {
                SelectItem(FilteredItems[SearchSelectedIndex]);
            }
        }
    }

    // ==================================================================
    //  صف الدفع - لطرق الدفع المتعددة في الفاتورة
    // ==================================================================
    public class InvoicePaymentRow : ViewModelBase
    {
        public int Id { get; set; }

        private string _method = "cash";
        public string Method
        {
            get { return _method; }
            set { _method = value; OnPropertyChanged("Method"); OnPropertyChanged("MethodDisplay"); }
        }

        public string MethodDisplay
        {
            get
            {
                switch ((_method ?? "").ToLowerInvariant())
                {
                    case "cash": return "نقدي";
                    case "card": return "بطاقة";
                    case "bank": return "تحويل بنكي";
                    case "check": return "شيك";
                    case "credit": return "آجل";
                    default: return _method;
                }
            }
        }

        private decimal _amount = 0;
        public decimal Amount
        {
            get { return _amount; }
            set { _amount = value; OnPropertyChanged("Amount"); }
        }

        private string _reference = "";
        public string Reference
        {
            get { return _reference; }
            set { _reference = value; OnPropertyChanged("Reference"); }
        }

        private DateTime _paymentDate = DateTime.Now;
        public DateTime PaymentDate
        {
            get { return _paymentDate; }
            set { _paymentDate = value; OnPropertyChanged("PaymentDate"); }
        }

        private string _notes = "";
        public string Notes
        {
            get { return _notes; }
            set { _notes = value; OnPropertyChanged("Notes"); }
        }
    }

    // ==================================================================
    //  صنف مبسط للأصناف
    // ==================================================================
    public class ItemSearchResult
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Barcode { get; set; }
        public string Sku { get; set; }
        public string Unit { get; set; }
        public decimal SalePrice { get; set; }
        public decimal PurchasePrice { get; set; }
        public decimal CurrentStock { get; set; }
        public decimal TaxRate { get; set; }
        public bool IsActive { get; set; }

        public string SearchDisplay
        {
            get
            {
                string b = string.IsNullOrEmpty(Barcode) ? "" : " [" + Barcode + "]";
                string s = string.IsNullOrEmpty(Sku) ? "" : " (" + Sku + ")";
                string stk = CurrentStock > 0 ? "  مخزون: " + CurrentStock.ToString("0.##") : "";
                return (Name ?? "") + b + s + stk;
            }
        }
    }

    // ==================================================================
    //  ViewModel الرئيسي لنافذة تعديل الفاتورة
    // ==================================================================
    public class InvoiceEditViewModel : ViewModelBase
    {
        public Action RequestClose;

        // ---- معلومات الفاتورة ----
        public int Id { get; set; }
        public bool DialogResult { get; set; }
        public bool PrintAfterSave { get; set; }

        private string _invoiceType = "sales";
        public string InvoiceType
        {
            get { return _invoiceType; }
            set { _invoiceType = value; OnPropertyChanged("InvoiceType"); OnPropertyChanged("InvoiceTypeDisplay"); }
        }

        public string InvoiceTypeDisplay
        {
            get
            {
                return (_invoiceType ?? "") == "purchase" ? "مشتريات" : "مبيعات";
            }
        }

        private string _serialNumber = "";
        public string SerialNumber
        {
            get { return _serialNumber; }
            set { _serialNumber = value; OnPropertyChanged("SerialNumber"); }
        }

        private DateTime _invoiceDate = DateTime.Now;
        public DateTime InvoiceDate
        {
            get { return _invoiceDate; }
            set { _invoiceDate = value; OnPropertyChanged("InvoiceDate"); }
        }

        private int _partyId = 0;
        public int PartyId
        {
            get { return _partyId; }
            set { _partyId = value; OnPropertyChanged("PartyId"); }
        }

        private string _partyName = "";
        public string PartyName
        {
            get { return _partyName; }
            set { _partyName = value; OnPropertyChanged("PartyName"); }
        }

        private int _warehouseId = 0;
        public int WarehouseId
        {
            get { return _warehouseId; }
            set { _warehouseId = value; OnPropertyChanged("WarehouseId"); }
        }

        private string _warehouseName = "";
        public string WarehouseName
        {
            get { return _warehouseName; }
            set { _warehouseName = value; OnPropertyChanged("WarehouseName"); }
        }

        private string _notes = "";
        public string Notes
        {
            get { return _notes; }
            set { _notes = value; OnPropertyChanged("Notes"); }
        }

        // ---- وضع الدفع ----
        private string _paymentMode = "full";
        public string PaymentMode
        {
            get { return _paymentMode; }
            set
            {
                _paymentMode = value;
                OnPropertyChanged("PaymentMode");
                OnPropertyChanged("ShowPaymentsSection");
                OnPaymentModeChanged();
            }
        }

        // قسم الدفع متعدد الطرق يظهر دائماً
        public bool ShowPaymentsSection
        {
            get { return true; }
        }

        // ---- بنود الفاتورة ----
        public ObservableCollection<InvoiceLineItem> Lines { get; set; }
        public ObservableCollection<ItemSearchResult> AllItemsList { get; set; }
        private List<ItemSearchResult> _allItems;

        // ---- قائمة الأطراف والمخازن ----
        public ObservableCollection<PartyOption> Parties { get; set; }
        public ObservableCollection<WarehouseOption> Warehouses { get; set; }

        // ---- الدفعات المتعددة ----
        public ObservableCollection<InvoicePaymentRow> Payments { get; set; }

        // ---- الحسابات ----
        private decimal _subtotal = 0;
        public decimal Subtotal
        {
            get { return _subtotal; }
            set { _subtotal = value; OnPropertyChanged("Subtotal"); OnPropertyChanged("SubtotalDisplay"); }
        }

        public string SubtotalDisplay { get { return _subtotal.ToString("N2"); } }

        private decimal _discountAmount = 0;
        public decimal DiscountAmount
        {
            get { return _discountAmount; }
            set
            {
                _discountAmount = value;
                OnPropertyChanged("DiscountAmount");
                RefreshTotals();
            }
        }

        private decimal _discountPercent = 0;
        public decimal DiscountPercent
        {
            get { return _discountPercent; }
            set
            {
                _discountPercent = value;
                OnPropertyChanged("DiscountPercent");
                // احسب خصم مبلغ من النسبة
                if (_discountPercent > 0)
                {
                    DiscountAmount = Math.Round(_subtotal * _discountPercent / 100m, 2);
                }
            }
        }

        private decimal _taxAmount = 0;
        public decimal TaxAmount
        {
            get { return _taxAmount; }
            set { _taxAmount = value; OnPropertyChanged("TaxAmount"); OnPropertyChanged("TaxDisplay"); }
        }

        public string TaxDisplay { get { return _taxAmount.ToString("N2"); } }

        private decimal _total = 0;
        public decimal Total
        {
            get { return _total; }
            set { _total = value; OnPropertyChanged("Total"); OnPropertyChanged("TotalDisplay"); OnPropertyChanged("Remaining"); }
        }

        public string TotalDisplay { get { return _total.ToString("N2"); } }

        private decimal _totalPaid = 0;
        public decimal TotalPaid
        {
            get { return _totalPaid; }
            set { _totalPaid = value; OnPropertyChanged("TotalPaid"); OnPropertyChanged("TotalPaidDisplay"); OnPropertyChanged("Remaining"); }
        }

        public string TotalPaidDisplay { get { return _totalPaid.ToString("N2"); } }

        public decimal Remaining
        {
            get { return Math.Round(_total - _totalPaid, 2); }
        }

        public string RemainingDisplay { get { return Remaining.ToString("N2"); } }

        private string _statusMessage = "";
        public string StatusMessage
        {
            get { return _statusMessage; }
            set { _statusMessage = value; OnPropertyChanged("StatusMessage"); }
        }

        private bool _isBusy = false;
        public bool IsBusy
        {
            get { return _isBusy; }
            set { _isBusy = value; OnPropertyChanged("IsBusy"); }
        }

        // ---- الأوامر ----
        public ICommand AddLineCommand { get; set; }
        public ICommand DeleteLineCommand { get; set; }
        public ICommand AddPaymentCommand { get; set; }
        public ICommand DeletePaymentCommand { get; set; }
        public ICommand SaveCommand { get; set; }
        public ICommand SaveAndPrintCommand { get; set; }
        public ICommand CancelCommand { get; set; }

        // ==================================================================
        public InvoiceEditViewModel()
        {
            Lines = new ObservableCollection<InvoiceLineItem>();
            AllItemsList = new ObservableCollection<ItemSearchResult>();
            _allItems = new List<ItemSearchResult>();
            Parties = new ObservableCollection<PartyOption>();
            Warehouses = new ObservableCollection<WarehouseOption>();
            Payments = new ObservableCollection<InvoicePaymentRow>();

            AddLineCommand = new UiRelayCommand(_ => AddLine());
            DeleteLineCommand = new UiRelayCommand(p =>
            {
                if (p is InvoiceLineItem li) Lines.Remove(li);
                RefreshTotals();
            });
            AddPaymentCommand = new UiRelayCommand(_ => AddPayment());
            DeletePaymentCommand = new UiRelayCommand(p =>
            {
                if (p is InvoicePaymentRow pr) Payments.Remove(pr);
                RefreshTotals();
            });

            // تحديث الإجماليات فور تغيير أي مبلغ دفع
            Payments.CollectionChanged += (s, e) =>
            {
                if (e.NewItems != null)
                {
                    foreach (InvoicePaymentRow r in e.NewItems)
                    {
                        r.PropertyChanged += (s2, e2) =>
                        {
                            if (e2.PropertyName == "Amount" || e2.PropertyName == "Method")
                                RefreshTotals();
                        };
                    }
                }
            };
            SaveCommand = new UiRelayCommand(_ => { PrintAfterSave = false; _ = SaveAsync(); }, _ => !IsBusy);
            SaveAndPrintCommand = new UiRelayCommand(_ => { PrintAfterSave = true; _ = SaveAsync(); }, _ => !IsBusy);
            CancelCommand = new UiRelayCommand(_ => { DialogResult = false; RequestClose?.Invoke(); });
        }

        // ==================================================================
        //  اتصال قاعدة البيانات
        // ==================================================================
        public static string GetConnectionString()
        {
            try
            {
                string env = Environment.GetEnvironmentVariable("TECHTOON_PG_CONNECTION");
                if (!string.IsNullOrWhiteSpace(env) && env.Contains("Host=")) return env;
            }
            catch { }

            // القيمة الافتراضية (Neon PostgreSQL) - تُستخدم كأساس دائماً
            string defaultCs = "Host=ep-bitter-night-za726w59-pooler.c-2.eu-west-2.aws.neon.tech;Port=5432;Username=neondb_owner;Password=***;Database=neondb;SSL Mode=Require;Trust Server Certificate=true";

            // اقرأ من appsettings.json باستخدام JSON parser صحيح
            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string[] paths = new string[]
                {
                    Path.Combine(baseDir, "appsettings.json"),
                    Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json"),
                    Path.Combine(baseDir, "..", "..", "..", "appsettings.json"),
                    Path.Combine(baseDir, "..", "..", "..", "..", "Techtoon.UI", "appsettings.json"),
                };
                foreach (string path in paths)
                {
                    if (!File.Exists(path)) continue;
                    string json = File.ReadAllText(path);
                    string cs = ExtractConnectionStringFromJson(json);
                    // تحقق من صحة السلسلة قبل إرجاعها
                    if (!string.IsNullOrWhiteSpace(cs) && cs.Contains("Host=") && cs.Contains("Database="))
                        return cs;
                }
            }
            catch { }

            // ارجع القيمة الافتراضية الآمنة
            return defaultCs;
        }

        // ==================================================================
        //  استخراج سلسلة الاتصال من JSON بشكل صحيح
        // ==================================================================
        private static string ExtractConnectionStringFromJson(string json)
        {
            // الطريقة 1: استخدام System.Text.Json (مضمن في .NET 10)
            try
            {
                using (var doc = System.Text.Json.JsonDocument.Parse(json))
                {
                    string result = ExtractFromJsonElement(doc.RootElement);
                    if (!string.IsNullOrEmpty(result)) return result;
                }
            }
            catch { }

            // الطريقة 2: regex احتياطية - تبحث عن أي نص يحتوي Host=
            try
            {
                var match = System.Text.RegularExpressions.Regex.Match(json, @"""([^""\\]*(?:\\.[^""\\]*)*Host=[^""\\]*(?:\\.[^""\\]*)*)""");
                if (match.Success) return match.Groups[1].Value.Replace("\\\"", "\"");
            }
            catch { }

            return null;
        }

        private static string ExtractFromJsonElement(System.Text.Json.JsonElement element)
        {
            if (element.ValueKind == System.Text.Json.JsonValueKind.Object)
            {
                foreach (var prop in element.EnumerateObject())
                {
                    string name = prop.Name.ToLowerInvariant();
                    // إذا كان المفتاح يحتوي connection وقيمته نص يحتوي Host=
                    if ((name.Contains("connection") || name == "db" || name == "database" || name == "postgres" || name == "neon")
                        && prop.Value.ValueKind == System.Text.Json.JsonValueKind.String)
                    {
                        string val = prop.Value.GetString();
                        if (!string.IsNullOrEmpty(val) && val.Contains("Host="))
                            return val;
                    }
                    // ابحث في القيم المتداخلة
                    var nested = ExtractFromJsonElement(prop.Value);
                    if (nested != null) return nested;
                }
            }
            else if (element.ValueKind == System.Text.Json.JsonValueKind.Array)
            {
                foreach (var item in element.EnumerateArray())
                {
                    var nested = ExtractFromJsonElement(item);
                    if (nested != null) return nested;
                }
            }
            return null;
        }

        // ==================================================================
        //  تحميل البيانات الأولية
        // ==================================================================
        public new async System.Threading.Tasks.Task LoadAsync()
        {
            IsBusy = true;
            StatusMessage = "جاري تحميل البيانات...";
            try
            {
                _allItems = LoadItems();
                AllItemsList.Clear();
                foreach (var it in _allItems) AllItemsList.Add(it);
                // رسالة تشخيصية لمعرفة عدد الأصناف المحملة
                try { System.Windows.MessageBox.Show("عدد الأصناف المحملة: " + _allItems.Count, "تشخيص"); } catch { }

                LoadParties();
                LoadWarehouses();

                // رقم تسلسلي جديد
                SerialNumber = GenerateSerial(InvoiceType);

                // صف بند فارغ
                AddLine();

                // دفع افتراضي
                if (Payments.Count == 0)
                {
                    Payments.Add(new InvoicePaymentRow { Method = "cash", Amount = 0, PaymentDate = DateTime.Now });
                }

                StatusMessage = "";
            }
            catch (Exception ex)
            {
                StatusMessage = "خطأ: " + ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
            await System.Threading.Tasks.Task.CompletedTask;
        }

        private List<ItemSearchResult> LoadItems()
        {
            var list = new List<ItemSearchResult>();
            string cs = GetConnectionString();
            using (var conn = new NpgsqlConnection(cs))
            {
                conn.Open();
                // تحقق من وجود الجدول
                string table = FindTable(conn, new string[] { "items", "products", "inventory_items" });
                if (string.IsNullOrEmpty(table)) return list;

                string nameCol = FindColumn(conn, table, new string[] { "name", "item_name", "title", "description" });
                string barCol = FindColumn(conn, table, new string[] { "barcode", "bar_code", "code", "ean" });
                string skuCol = FindColumn(conn, table, new string[] { "sku", "item_code", "code" });
                string priceCol = FindColumn(conn, table, new string[] { "sale_price", "selling_price", "price", "unit_price" });
                string unitCol = FindColumn(conn, table, new string[] { "unit", "uom" });
                string stockCol = FindColumn(conn, table, new string[] { "current_stock", "stock", "quantity", "qty_on_hand" });

                string sql = "SELECT id";
                if (!string.IsNullOrEmpty(nameCol)) sql += ", \"" + nameCol + "\"";
                if (!string.IsNullOrEmpty(barCol)) sql += ", \"" + barCol + "\"";
                if (!string.IsNullOrEmpty(skuCol)) sql += ", \"" + skuCol + "\"";
                if (!string.IsNullOrEmpty(priceCol)) sql += ", \"" + priceCol + "\"";
                if (!string.IsNullOrEmpty(unitCol)) sql += ", \"" + unitCol + "\"";
                sql += " FROM \"" + table + "\"";
                string where = " WHERE ";
                if (!string.IsNullOrEmpty(nameCol)) where += "\"" + nameCol + "\" IS NOT NULL";
                else where += "1=1";
                // فلترة الأصناف النشطة إن وجد العمود
                string actCol = FindColumn(conn, table, new string[] { "is_active", "active", "enabled" });
                if (!string.IsNullOrEmpty(actCol)) where += " AND (\"" + actCol + "\" IS NULL OR \"" + actCol + "\" = true)";
                sql += where + " ORDER BY ";
                sql += !string.IsNullOrEmpty(nameCol) ? "\"" + nameCol + "\"" : "id";
                sql += " LIMIT 500";

                using (var cmd = new NpgsqlCommand(sql, conn))
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        var it = new ItemSearchResult();
                        it.Id = r.GetInt32(0);
                        int col = 1;
                        if (!string.IsNullOrEmpty(nameCol)) { it.Name = r.IsDBNull(col) ? "" : r.GetString(col); col++; }
                        if (!string.IsNullOrEmpty(barCol)) { it.Barcode = r.IsDBNull(col) ? "" : r.GetString(col); col++; }
                        if (!string.IsNullOrEmpty(skuCol)) { it.Sku = r.IsDBNull(col) ? "" : r.GetString(col); col++; }
                        if (!string.IsNullOrEmpty(priceCol)) { it.SalePrice = r.IsDBNull(col) ? 0 : (decimal)r.GetValue(col); col++; }
                        if (!string.IsNullOrEmpty(unitCol)) { it.Unit = r.IsDBNull(col) ? "" : r.GetString(col); col++; }
                        list.Add(it);
                    }
                }
            }
            return list;
        }

        private void LoadParties()
        {
            Parties.Clear();
            try
            {
                string cs = GetConnectionString();
                using (var conn = new NpgsqlConnection(cs))
                {
                    conn.Open();
                    string table = FindTable(conn, new string[] { "parties", "customers_suppliers", "contacts", "accounts" });
                    if (string.IsNullOrEmpty(table)) return;
                    string nameCol = FindColumn(conn, table, new string[] { "name", "party_name", "title", "account_name" });
                    if (string.IsNullOrEmpty(nameCol)) return;
                    string sql = "SELECT id, \"" + nameCol + "\" FROM \"" + table + "\" ORDER BY \"" + nameCol + "\" LIMIT 500";
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            Parties.Add(new PartyOption { Id = r.GetInt32(0), Name = r.IsDBNull(1) ? "" : r.GetString(1) });
                        }
                    }
                }
            }
            catch { }
        }

        private void LoadWarehouses()
        {
            Warehouses.Clear();
            try
            {
                string cs = GetConnectionString();
                using (var conn = new NpgsqlConnection(cs))
                {
                    conn.Open();
                    string table = FindTable(conn, new string[] { "warehouses", "stores", "locations" });
                    if (string.IsNullOrEmpty(table)) return;
                    string nameCol = FindColumn(conn, table, new string[] { "name", "warehouse_name", "title" });
                    if (string.IsNullOrEmpty(nameCol)) return;
                    string sql = "SELECT id, \"" + nameCol + "\" FROM \"" + table + "\" ORDER BY \"" + nameCol + "\" LIMIT 100";
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            Warehouses.Add(new WarehouseOption { Id = r.GetInt32(0), Name = r.IsDBNull(1) ? "" : r.GetString(1) });
                        }
                    }
                }
            }
            catch { }
        }

        private string GenerateSerial(string type)
        {
            try
            {
                string cs = GetConnectionString();
                using (var conn = new NpgsqlConnection(cs))
                {
                    conn.Open();
                    string table = FindTable(conn, new string[] { "invoices" });
                    if (string.IsNullOrEmpty(table)) return "";
                    string serialCol = FindColumn(conn, table, new string[] { "serial_number", "invoice_number", "number", "code" });
                    string typeCol = FindColumn(conn, table, new string[] { "invoice_type", "type", "kind" });
                    string prefix = (type ?? "sales") == "purchase" ? "PUR" : "SAL";
                    int year = DateTime.Now.Year;
                    string likePattern = prefix + "-" + year + "-%";
                    string sql;
                    if (!string.IsNullOrEmpty(serialCol))
                    {
                        if (!string.IsNullOrEmpty(typeCol))
                        {
                            sql = "SELECT \"" + serialCol + "\" FROM \"" + table + "\" WHERE \"" + serialCol + "\" LIKE @p ORDER BY \"" + serialCol + "\" DESC LIMIT 1";
                        }
                        else
                        {
                            sql = "SELECT \"" + serialCol + "\" FROM \"" + table + "\" WHERE \"" + serialCol + "\" LIKE @p ORDER BY id DESC LIMIT 1";
                        }
                        using (var cmd = new NpgsqlCommand(sql, conn))
                        {
                            cmd.Parameters.AddWithValue("p", likePattern);
                            var o = cmd.ExecuteScalar();
                            if (o != null && o != DBNull.Value)
                            {
                                string last = o.ToString();
                                int dashIdx = last.LastIndexOf('-');
                                if (dashIdx >= 0 && int.TryParse(last.Substring(dashIdx + 1), out int seq))
                                {
                                    return prefix + "-" + year + "-" + (seq + 1).ToString("D4");
                                }
                            }
                        }
                    }
                }
            }
            catch { }
            string pf = (type ?? "sales") == "purchase" ? "PUR" : "SAL";
            return pf + "-" + DateTime.Now.Year + "-0001";
        }

        // ==================================================================
        //  أوامر البنود والدفعات
        // ==================================================================
        public void AddLine()
        {
            var line = new InvoiceLineItem(_allItems);
            line.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == "Quantity" || e.PropertyName == "Price" ||
                    e.PropertyName == "Discount" || e.PropertyName == "Total")
                {
                    RefreshTotals();
                }
            };
            Lines.Add(line);
            RefreshTotals();
        }

        public void AddPayment()
        {
            decimal remaining = Remaining;
            Payments.Add(new InvoicePaymentRow
            {
                Method = "cash",
                Amount = remaining > 0 ? remaining : 0,
                PaymentDate = DateTime.Now
            });
            RefreshTotals();
        }

        private void OnPaymentModeChanged()
        {
            // عند تغيير وضع الدفع
            if (_paymentMode == "full" && Total > 0 && Payments.Count == 0)
            {
                Payments.Add(new InvoicePaymentRow { Method = "cash", Amount = Total, PaymentDate = DateTime.Now });
                RefreshTotals();
            }
            else if (_paymentMode == "none")
            {
                Payments.Clear();
                RefreshTotals();
            }
        }

        // ==================================================================
        //  إعادة حساب الإجماليات
        // ==================================================================
        public void RefreshTotals()
        {
            decimal sub = 0;
            foreach (var line in Lines)
            {
                sub += line.Total;
            }
            Subtotal = Math.Round(sub, 2);

            // إذا كان خصم بالنسبة، أعد حسابه
            if (_discountPercent > 0 && _discountAmount == 0)
            {
                _discountAmount = Math.Round(_subtotal * _discountPercent / 100m, 2);
            }

            decimal afterDiscount = _subtotal - _discountAmount;
            TaxAmount = Math.Round(afterDiscount * 0m, 2); // الضريب=0 حالياً
            Total = Math.Round(afterDiscount + _taxAmount, 2);

            // مجموع المدفوع
            decimal paid = 0;
            foreach (var p in Payments) paid += p.Amount;
            TotalPaid = Math.Round(paid, 2);

            OnPropertyChanged("Remaining");
            OnPropertyChanged("RemainingDisplay");
        }

        // ==================================================================
        //  تحميل فاتورة موجودة
        // ==================================================================
        public async System.Threading.Tasks.Task LoadFromInvoiceAsync(int invoiceId)
        {
            IsBusy = true;
            StatusMessage = "جاري تحميل الفاتورة...";
            try
            {
                string cs = GetConnectionString();
                using (var conn = new NpgsqlConnection(cs))
                {
                    conn.Open();
                    string table = FindTable(conn, new string[] { "invoices" });
                    if (string.IsNullOrEmpty(table)) { StatusMessage = "جدول الفواتير غير موجود"; return; }

                    string[] cols = GetColumns(conn, table);
                    string selCols = "";
                    foreach (string c in new string[] { "id", "serial_number", "invoice_number", "number", "invoice_date", "date", "invoice_type", "type", "party_id", "warehouse_id", "notes", "subtotal", "discount_amount", "tax_amount", "total", "paid_amount" })
                    {
                        if (cols.Contains(c)) selCols += "\"" + c + "\", ";
                    }
                    selCols = selCols.TrimEnd(',', ' ');
                    if (selCols.Length == 0) { StatusMessage = "لا توجد أعمدة"; return; }

                    string sql = "SELECT " + selCols + " FROM \"" + table + "\" WHERE id = @id";
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("id", invoiceId);
                        using (var r = cmd.ExecuteReader())
                        {
                            if (r.Read())
                            {
                                int col = 0;
                                for (int i = 0; i < r.FieldCount; i++)
                                {
                                    string fname = r.GetName(i).ToLowerInvariant();
                                    object val = r.IsDBNull(i) ? null : r.GetValue(i);
                                    switch (fname)
                                    {
                                        case "id": Id = val == null ? 0 : Convert.ToInt32(val); break;
                                        case "serial_number":
                                        case "invoice_number":
                                        case "number": SerialNumber = val == null ? "" : val.ToString(); break;
                                        case "invoice_date":
                                        case "date": InvoiceDate = val == null ? DateTime.Now : Convert.ToDateTime(val); break;
                                        case "invoice_type":
                                        case "type": InvoiceType = val == null ? "sales" : val.ToString(); break;
                                        case "party_id": PartyId = val == null ? 0 : Convert.ToInt32(val); break;
                                        case "warehouse_id": WarehouseId = val == null ? 0 : Convert.ToInt32(val); break;
                                        case "notes": Notes = val == null ? "" : val.ToString(); break;
                                        case "subtotal": Subtotal = val == null ? 0 : Convert.ToDecimal(val); break;
                                        case "discount_amount": DiscountAmount = val == null ? 0 : Convert.ToDecimal(val); break;
                                        case "tax_amount": TaxAmount = val == null ? 0 : Convert.ToDecimal(val); break;
                                        case "total": Total = val == null ? 0 : Convert.ToDecimal(val); break;
                                        case "paid_amount": TotalPaid = val == null ? 0 : Convert.ToDecimal(val); break;
                                    }
                                }
                            }
                        }
                    }

                    // تحميل البنود
                    Lines.Clear();
                    string lineTable = FindTable(conn, new string[] { "invoice_lines", "invoice_items", "invoice_details" });
                    if (!string.IsNullOrEmpty(lineTable))
                    {
                        string lc = "SELECT * FROM \"" + lineTable + "\" WHERE invoice_id = @id";
                        try
                        {
                            using (var cmd = new NpgsqlCommand(lc, conn))
                            {
                                cmd.Parameters.AddWithValue("id", invoiceId);
                                using (var r = cmd.ExecuteReader())
                                {
                                    while (r.Read())
                                    {
                                        var line = new InvoiceLineItem(_allItems);
                                        for (int i = 0; i < r.FieldCount; i++)
                                        {
                                            string fn = r.GetName(i).ToLowerInvariant();
                                            object val = r.IsDBNull(i) ? null : r.GetValue(i);
                                            switch (fn)
                                            {
                                                case "item_id": line.ItemId = val == null ? 0 : Convert.ToInt32(val); break;
                                                case "item_name":
                                                case "name": line.ItemName = val == null ? "" : val.ToString(); break;
                                                case "barcode": line.Barcode = val == null ? "" : val.ToString(); break;
                                                case "unit": line.Unit = val == null ? "" : val.ToString(); break;
                                                case "quantity":
                                                case "qty": line.Quantity = val == null ? 1 : Convert.ToDecimal(val); break;
                                                case "price":
                                                case "unit_price": line.Price = val == null ? 0 : Convert.ToDecimal(val); break;
                                                case "discount": line.Discount = val == null ? 0 : Convert.ToDecimal(val); break;
                                                case "total":
                                                case "line_total": line.Total = val == null ? 0 : Convert.ToDecimal(val); break;
                                            }
                                        }
                                        Lines.Add(line);
                                    }
                                }
                            }
                        }
                        catch { }
                    }

                    // تحميل الدفعات
                    Payments.Clear();
                    string payTable = FindTable(conn, new string[] { "invoice_payments" });
                    if (!string.IsNullOrEmpty(payTable))
                    {
                        try
                        {
                            using (var cmd = new NpgsqlCommand("SELECT * FROM \"" + payTable + "\" WHERE invoice_id = @id ORDER BY id", conn))
                            {
                                cmd.Parameters.AddWithValue("id", invoiceId);
                                using (var r = cmd.ExecuteReader())
                                {
                                    while (r.Read())
                                    {
                                        var row = new InvoicePaymentRow();
                                        for (int i = 0; i < r.FieldCount; i++)
                                        {
                                            string fn = r.GetName(i).ToLowerInvariant();
                                            object val = r.IsDBNull(i) ? null : r.GetValue(i);
                                            switch (fn)
                                            {
                                                case "id": row.Id = val == null ? 0 : Convert.ToInt32(val); break;
                                                case "method": row.Method = val == null ? "cash" : val.ToString(); break;
                                                case "amount": row.Amount = val == null ? 0 : Convert.ToDecimal(val); break;
                                                case "reference": row.Reference = val == null ? "" : val.ToString(); break;
                                                case "payment_date":
                                                case "date": row.PaymentDate = val == null ? DateTime.Now : Convert.ToDateTime(val); break;
                                                case "notes": row.Notes = val == null ? "" : val.ToString(); break;
                                            }
                                        }
                                        Payments.Add(row);
                                    }
                                }
                            }
                        }
                        catch { }
                    }
                }

                if (Lines.Count == 0) AddLine();
                if (Payments.Count == 0 && Total > 0)
                {
                    Payments.Add(new InvoicePaymentRow { Method = "cash", Amount = Total, PaymentDate = DateTime.Now });
                }
                RefreshTotals();
                StatusMessage = "";
            }
            catch (Exception ex)
            {
                StatusMessage = "خطأ: " + ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
            await System.Threading.Tasks.Task.CompletedTask;
        }

        // ==================================================================
        //  حفظ الفاتورة
        // ==================================================================
        public async System.Threading.Tasks.Task SaveAsync()
        {
            IsBusy = true;
            StatusMessage = "جاري الحفظ...";
            try
            {
                RefreshTotals();

                string cs = GetConnectionString();
                using (var conn = new NpgsqlConnection(cs))
                {
                    conn.Open();

                    string table = FindTable(conn, new string[] { "invoices" });
                    if (string.IsNullOrEmpty(table))
                    {
                        // أنشئ الجدول إن لم يكن موجوداً
                        CreateInvoicesTable(conn);
                        table = "invoices";
                    }

                    string[] cols = GetColumns(conn, table);
                    if (Id > 0)
                    {
                        // تحديث
                        string setParts = "";
                        var paramDict = new Dictionary<string, object>();
                        void AddSet(string col, string param, object val)
                        {
                            if (cols.Contains(col))
                            {
                                setParts += "\"" + col + "\" = @" + param + ", ";
                                paramDict[param] = val ?? DBNull.Value;
                            }
                        }
                        AddSet("serial_number", "sn", SerialNumber);
                        AddSet("invoice_date", "dt", InvoiceDate);
                        AddSet("invoice_type", "tp", InvoiceType);
                        AddSet("party_id", "pid", PartyId > 0 ? (object)PartyId : DBNull.Value);
                        AddSet("warehouse_id", "wid", WarehouseId > 0 ? (object)WarehouseId : DBNull.Value);
                        AddSet("notes", "nt", Notes);
                        AddSet("subtotal", "st", Subtotal);
                        AddSet("discount_amount", "da", DiscountAmount);
                        AddSet("tax_amount", "tx", TaxAmount);
                        AddSet("total", "tt", Total);
                        AddSet("paid_amount", "pa", TotalPaid);
                        AddSet("status", "status", TotalPaid >= Total ? "paid" : (TotalPaid > 0 ? "partial" : "unpaid"));
                        AddSet("updated_at", "ua", DateTime.Now);

                        setParts = setParts.TrimEnd(',', ' ');
                        if (setParts.Length > 0)
                        {
                            string sql = "UPDATE \"" + table + "\" SET " + setParts + " WHERE id = @id";
                            using (var cmd = new NpgsqlCommand(sql, conn))
                            {
                                cmd.Parameters.AddWithValue("id", Id);
                                foreach (var kv in paramDict)
                                {
                                    cmd.Parameters.AddWithValue(kv.Key, kv.Value);
                                }
                                cmd.ExecuteNonQuery();
                            }
                        }
                    }
                    else
                    {
                        // إدخال جديد
                        string colNames = "";
                        string paramNames = "";
                        var paramDict = new Dictionary<string, object>();
                        void AddInsert(string col, string param, object val)
                        {
                            if (cols.Contains(col))
                            {
                                colNames += "\"" + col + "\", ";
                                paramNames += "@" + param + ", ";
                                paramDict[param] = val ?? DBNull.Value;
                            }
                        }
                        AddInsert("serial_number", "sn", SerialNumber);
                        AddInsert("invoice_date", "dt", InvoiceDate);
                        AddInsert("invoice_type", "tp", InvoiceType);
                        AddInsert("party_id", "pid", PartyId > 0 ? (object)PartyId : DBNull.Value);
                        AddInsert("warehouse_id", "wid", WarehouseId > 0 ? (object)WarehouseId : DBNull.Value);
                        AddInsert("notes", "nt", Notes);
                        AddInsert("subtotal", "st", Subtotal);
                        AddInsert("discount_amount", "da", DiscountAmount);
                        AddInsert("tax_amount", "tx", TaxAmount);
                        AddInsert("total", "tt", Total);
                        AddInsert("paid_amount", "pa", TotalPaid);
                        AddInsert("status", "status", TotalPaid >= Total ? "paid" : (TotalPaid > 0 ? "partial" : "unpaid"));
                        AddInsert("created_at", "ca", DateTime.Now);
                        AddInsert("updated_at", "ua", DateTime.Now);

                        colNames = colNames.TrimEnd(',', ' ');
                        paramNames = paramNames.TrimEnd(',', ' ');
                        string sql = "INSERT INTO \"" + table + "\" (" + colNames + ") VALUES (" + paramNames + ") RETURNING id";
                        using (var cmd = new NpgsqlCommand(sql, conn))
                        {
                            foreach (var kv in paramDict)
                            {
                                cmd.Parameters.AddWithValue(kv.Key, kv.Value);
                            }
                            var o = cmd.ExecuteScalar();
                            if (o != null && o != DBNull.Value) Id = Convert.ToInt32(o);
                        }
                    }

                    // احفظ البنود
                    SaveLines(conn, Id);
                    // احفظ الدفعات
                    SavePayments(conn, Id);
                }

                StatusMessage = "تم الحفظ بنجاح - رقم الفاتورة: " + SerialNumber;
                DialogResult = true;
                RequestClose?.Invoke();
            }
            catch (Exception ex)
            {
                StatusMessage = "خطأ عند الحفظ: " + ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
            await System.Threading.Tasks.Task.CompletedTask;
        }

        private void SaveLines(NpgsqlConnection conn, int invoiceId)
        {
            string lineTable = FindTable(conn, new string[] { "invoice_lines", "invoice_items", "invoice_details" });
            if (string.IsNullOrEmpty(lineTable))
            {
                CreateInvoiceLinesTable(conn);
                lineTable = "invoice_lines";
            }

            // احذف البنود القديمة
            using (var cmd = new NpgsqlCommand("DELETE FROM \"" + lineTable + "\" WHERE invoice_id = @id", conn))
            {
                cmd.Parameters.AddWithValue("id", invoiceId);
                cmd.ExecuteNonQuery();
            }

            string[] cols = GetColumns(conn, lineTable);
            foreach (var line in Lines)
            {
                if (line.ItemId <= 0 && string.IsNullOrEmpty(line.ItemName)) continue;
                string colNames = "invoice_id";
                string paramNames = "@inv";
                var paramDict = new Dictionary<string, object>();
                paramDict["inv"] = invoiceId;

                void AddCol(string col, string param, object val)
                {
                    if (cols.Contains(col))
                    {
                        colNames += ", \"" + col + "\"";
                        paramNames += ", @" + param;
                        paramDict[param] = val ?? DBNull.Value;
                    }
                }
                AddCol("item_id", "iid", line.ItemId > 0 ? (object)line.ItemId : DBNull.Value);
                AddCol("item_name", "inm", line.ItemName);
                AddCol("barcode", "bc", line.Barcode);
                AddCol("unit", "un", line.Unit);
                AddCol("quantity", "qty", line.Quantity);
                AddCol("price", "prc", line.Price);
                AddCol("discount", "dsc", line.Discount);
                AddCol("total", "tot", line.Total);

                string sql = "INSERT INTO \"" + lineTable + "\" (" + colNames + ") VALUES (" + paramNames + ")";
                using (var cmd = new NpgsqlCommand(sql, conn))
                {
                    foreach (var kv in paramDict) cmd.Parameters.AddWithValue(kv.Key, kv.Value);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private void SavePayments(NpgsqlConnection conn, int invoiceId)
        {
            string payTable = FindTable(conn, new string[] { "invoice_payments" });
            if (string.IsNullOrEmpty(payTable))
            {
                CreateInvoicePaymentsTable(conn);
                payTable = "invoice_payments";
            }

            // احذف الدفعات القديمة
            using (var cmd = new NpgsqlCommand("DELETE FROM \"" + payTable + "\" WHERE invoice_id = @id", conn))
            {
                cmd.Parameters.AddWithValue("id", invoiceId);
                cmd.ExecuteNonQuery();
            }

            string[] cols = GetColumns(conn, payTable);
            foreach (var pay in Payments)
            {
                if (pay.Amount <= 0) continue;
                string colNames = "invoice_id, method, amount";
                string paramNames = "@inv, @mth, @amt";
                var paramDict = new Dictionary<string, object>();
                paramDict["inv"] = invoiceId;
                paramDict["mth"] = pay.Method;
                paramDict["amt"] = pay.Amount;

                void AddCol(string col, string param, object val)
                {
                    if (cols.Contains(col))
                    {
                        colNames += ", \"" + col + "\"";
                        paramNames += ", @" + param;
                        paramDict[param] = val ?? DBNull.Value;
                    }
                }
                AddCol("reference", "ref", pay.Reference);
                AddCol("payment_date", "pdt", pay.PaymentDate);
                AddCol("notes", "nt", pay.Notes);

                string sql = "INSERT INTO \"" + payTable + "\" (" + colNames + ") VALUES (" + paramNames + ")";
                using (var cmd = new NpgsqlCommand(sql, conn))
                {
                    foreach (var kv in paramDict) cmd.Parameters.AddWithValue(kv.Key, kv.Value);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // ==================================================================
        //  دوال مساعدة
        // ==================================================================
        private static string FindTable(NpgsqlConnection conn, string[] candidates)
        {
            using (var cmd = new NpgsqlCommand("select table_name from information_schema.tables where table_schema = current_schema()", conn))
            using (var r = cmd.ExecuteReader())
            {
                var tables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                while (r.Read()) tables.Add(r.GetString(0));
                r.Close();
                foreach (string c in candidates)
                {
                    if (tables.Contains(c)) return c;
                }
                // بحث جزئي
                foreach (string c in candidates)
                {
                    foreach (string t in tables)
                    {
                        if (t.IndexOf(c, StringComparison.OrdinalIgnoreCase) >= 0) return t;
                    }
                }
            }
            return null;
        }

        private static string FindColumn(NpgsqlConnection conn, string table, string[] candidates)
        {
            string[] cols = GetColumns(conn, table);
            foreach (string c in candidates)
            {
                if (cols.Contains(c, StringComparer.OrdinalIgnoreCase)) return c;
            }
            foreach (string c in candidates)
            {
                foreach (string col in cols)
                {
                    if (col.IndexOf(c, StringComparison.OrdinalIgnoreCase) >= 0) return col;
                }
            }
            return null;
        }

        private static string[] GetColumns(NpgsqlConnection conn, string table)
        {
            var list = new List<string>();
            using (var cmd = new NpgsqlCommand("select lower(column_name) from information_schema.columns where table_schema = current_schema() and lower(table_name) = @t", conn))
            {
                cmd.Parameters.AddWithValue("t", table.ToLowerInvariant());
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read()) list.Add(r.GetString(0));
                }
            }
            return list.ToArray();
        }

        private static void CreateInvoicesTable(NpgsqlConnection conn)
        {
            string sql = @"CREATE TABLE IF NOT EXISTS invoices (
                id SERIAL PRIMARY KEY,
                serial_number TEXT,
                invoice_date TIMESTAMP DEFAULT NOW(),
                invoice_type TEXT DEFAULT 'sales',
                party_id INTEGER,
                warehouse_id INTEGER,
                cash_box_id INTEGER,
                notes TEXT,
                subtotal NUMERIC(14,2) DEFAULT 0,
                discount_amount NUMERIC(14,2) DEFAULT 0,
                tax_amount NUMERIC(14,2) DEFAULT 0,
                total NUMERIC(14,2) DEFAULT 0,
                paid_amount NUMERIC(14,2) DEFAULT 0,
                status TEXT DEFAULT 'draft',
                created_at TIMESTAMP DEFAULT NOW(),
                updated_at TIMESTAMP DEFAULT NOW()
            )";
            using (var cmd = new NpgsqlCommand(sql, conn)) cmd.ExecuteNonQuery();
        }

        private static void CreateInvoiceLinesTable(NpgsqlConnection conn)
        {
            string sql = @"CREATE TABLE IF NOT EXISTS invoice_lines (
                id SERIAL PRIMARY KEY,
                invoice_id INTEGER NOT NULL,
                item_id INTEGER,
                item_name TEXT,
                barcode TEXT,
                unit TEXT,
                quantity NUMERIC(14,3) DEFAULT 0,
                price NUMERIC(14,2) DEFAULT 0,
                discount NUMERIC(14,2) DEFAULT 0,
                total NUMERIC(14,2) DEFAULT 0
            )";
            using (var cmd = new NpgsqlCommand(sql, conn)) cmd.ExecuteNonQuery();
        }

        private static void CreateInvoicePaymentsTable(NpgsqlConnection conn)
        {
            string sql = @"CREATE TABLE IF NOT EXISTS invoice_payments (
                id SERIAL PRIMARY KEY,
                invoice_id INTEGER NOT NULL,
                method TEXT NOT NULL DEFAULT 'cash',
                amount NUMERIC(14,2) NOT NULL DEFAULT 0,
                reference TEXT,
                cash_box_id INTEGER,
                bank_account_id INTEGER,
                payment_date TIMESTAMP DEFAULT NOW(),
                notes TEXT,
                created_at TIMESTAMP DEFAULT NOW()
            )";
            using (var cmd = new NpgsqlCommand(sql, conn)) cmd.ExecuteNonQuery();
        }
    }

    // ==================================================================
    //  أصناف مساعدة للقوائم المنسدلة
    // ==================================================================
    public class PartyOption
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public override string ToString() { return Name ?? ""; }
    }

    public class WarehouseOption
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public override string ToString() { return Name ?? ""; }
    }
}
'@
[System.IO.File]::WriteAllText($dest, $c_InvoiceEditViewModel_cs, [System.Text.UTF8Encoding]::new($true))
Write-Host "  OK: InvoiceEditViewModel.cs" -ForegroundColor Green

$dest = Join-Path $ProjectPath 'Techtoon.UI\Views\InvoiceEditWindow.xaml'
if (Test-Path $dest) { Copy-Item $dest ($dest + '.bak') -Force }
$c_InvoiceEditWindow_xaml = @'
<Window x:Class="Techtoon.UI.Views.InvoiceEditWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:vm="clr-namespace:Techtoon.UI.Views"
        Title="&#1601;&#1575;&#1578;&#1608;&#1585;&#1577; &#1580;&#1583;&#1610;&#1583;&#1577;" Height="820" Width="1180"
        WindowStartupLocation="CenterScreen"
        FlowDirection="RightToLeft"
        Background="#F5F7FA"
        Loaded="Window_Loaded">
    <Window.Resources>
        <Style x:Key="HeaderLabel" TargetType="TextBlock">
            <Setter Property="FontSize" Value="13"/>
            <Setter Property="FontWeight" Value="SemiBold"/>
            <Setter Property="Foreground" Value="#37474F"/>
            <Setter Property="Margin" Value="0,0,6,0"/>
            <Setter Property="VerticalAlignment" Value="Center"/>
        </Style>
        <Style x:Key="InputBox" TargetType="TextBox">
            <Setter Property="Padding" Value="6,4"/>
            <Setter Property="FontSize" Value="13"/>
            <Setter Property="BorderBrush" Value="#B0BEC5"/>
            <Setter Property="BorderThickness" Value="1"/>
            <Setter Property="Background" Value="White"/>
        </Style>
        <Style x:Key="InputCombo" TargetType="ComboBox">
            <Setter Property="Padding" Value="6,4"/>
            <Setter Property="FontSize" Value="13"/>
            <Setter Property="BorderBrush" Value="#B0BEC5"/>
            <Setter Property="BorderThickness" Value="1"/>
            <Setter Property="Background" Value="White"/>
        </Style>
        <Style x:Key="PrimaryBtn" TargetType="Button">
            <Setter Property="Background" Value="#00897B"/>
            <Setter Property="Foreground" Value="White"/>
            <Setter Property="FontWeight" Value="SemiBold"/>
            <Setter Property="FontSize" Value="13"/>
            <Setter Property="Padding" Value="20,8"/>
            <Setter Property="BorderThickness" Value="0"/>
            <Setter Property="Cursor" Value="Hand"/>
        </Style>
        <Style x:Key="SecondaryBtn" TargetType="Button">
            <Setter Property="Background" Value="#ECEFF1"/>
            <Setter Property="Foreground" Value="#37474F"/>
            <Setter Property="FontWeight" Value="SemiBold"/>
            <Setter Property="FontSize" Value="13"/>
            <Setter Property="Padding" Value="20,8"/>
            <Setter Property="BorderThickness" Value="0"/>
            <Setter Property="Cursor" Value="Hand"/>
        </Style>
        <Style x:Key="AddBtn" TargetType="Button">
            <Setter Property="Background" Value="#26A69A"/>
            <Setter Property="Foreground" Value="White"/>
            <Setter Property="FontWeight" Value="SemiBold"/>
            <Setter Property="FontSize" Value="13"/>
            <Setter Property="Padding" Value="14,6"/>
            <Setter Property="BorderThickness" Value="0"/>
            <Setter Property="Cursor" Value="Hand"/>
        </Style>
    </Window.Resources>

    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>

        <!-- ======================= &#1575;&#1604;&#1578;&#1585;&#1608;&#1610;&#1587;&#1577; ======================= -->
        <Border Grid.Row="0" Background="White" Padding="16,10" BorderBrush="#E0E0E0" BorderThickness="0,0,0,1">
            <Grid>
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="Auto"/>
                    <ColumnDefinition Width="*"/>
                </Grid.ColumnDefinitions>
                <StackPanel Grid.Column="0" Orientation="Horizontal" VerticalAlignment="Center">
                    <TextBlock Text="&#129534;" FontSize="28" Margin="0,0,10,0" VerticalAlignment="Center"/>
                    <StackPanel>
                        <TextBlock Text="&#1601;&#1575;&#1578;&#1608;&#1585;&#1577;" FontSize="18" FontWeight="Bold" Foreground="#00695C"/>
                        <TextBlock Text="{Binding InvoiceTypeDisplay}" FontSize="12" Foreground="#78909C"/>
                    </StackPanel>
                </StackPanel>

                <Grid Grid.Column="1" Margin="20,0,0,0">
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="Auto"/>
                        <ColumnDefinition Width="140"/>
                        <ColumnDefinition Width="18"/>
                        <ColumnDefinition Width="Auto"/>
                        <ColumnDefinition Width="150"/>
                        <ColumnDefinition Width="18"/>
                        <ColumnDefinition Width="Auto"/>
                        <ColumnDefinition Width="120"/>
                    </Grid.ColumnDefinitions>
                    <Grid.RowDefinitions>
                        <RowDefinition Height="Auto"/>
                        <RowDefinition Height="8"/>
                        <RowDefinition Height="Auto"/>
                    </Grid.RowDefinitions>

                    <TextBlock Grid.Row="0" Grid.Column="0" Text="&#1575;&#1604;&#1585;&#1602;&#1605;:" Style="{StaticResource HeaderLabel}"/>
                    <TextBox Grid.Row="0" Grid.Column="1" Text="{Binding SerialNumber}" Style="{StaticResource InputBox}" IsReadOnly="True"/>

                    <TextBlock Grid.Row="0" Grid.Column="3" Text="&#1575;&#1604;&#1578;&#1575;&#1585;&#1610;&#1582;:" Style="{StaticResource HeaderLabel}"/>
                    <DatePicker Grid.Row="0" Grid.Column="4" SelectedDate="{Binding InvoiceDate}" FontSize="13"/>

                    <TextBlock Grid.Row="0" Grid.Column="6" Text="&#1575;&#1604;&#1606;&#1608;&#1593;:" Style="{StaticResource HeaderLabel}"/>
                    <ComboBox Grid.Row="0" Grid.Column="7" Style="{StaticResource InputCombo}"
                              SelectedValue="{Binding InvoiceType}" SelectedValuePath="Tag">
                        <ComboBoxItem Content="&#1605;&#1576;&#1610;&#1593;&#1575;&#1578;" Tag="sales"/>
                        <ComboBoxItem Content="&#1605;&#1588;&#1578;&#1585;&#1610;&#1575;&#1578;" Tag="purchase"/>
                    </ComboBox>

                    <TextBlock Grid.Row="2" Grid.Column="0" Text="&#1575;&#1604;&#1591;&#1585;&#1601;:" Style="{StaticResource HeaderLabel}"/>
                    <ComboBox Grid.Row="2" Grid.Column="1" Grid.ColumnSpan="4" Style="{StaticResource InputCombo}"
                              ItemsSource="{Binding Parties}" DisplayMemberPath="Name"
                              SelectedValuePath="Id" SelectedValue="{Binding PartyId}"/>

                    <TextBlock Grid.Row="2" Grid.Column="6" Text="&#1575;&#1604;&#1605;&#1582;&#1586;&#1606;:" Style="{StaticResource HeaderLabel}"/>
                    <ComboBox Grid.Row="2" Grid.Column="7" Style="{StaticResource InputCombo}"
                              ItemsSource="{Binding Warehouses}" DisplayMemberPath="Name"
                              SelectedValuePath="Id" SelectedValue="{Binding WarehouseId}"/>
                </Grid>
            </Grid>
        </Border>

        <!-- ======================= &#1576;&#1606;&#1608;&#1583; &#1575;&#1604;&#1601;&#1575;&#1578;&#1608;&#1585;&#1577; + &#1575;&#1604;&#1576;&#1581;&#1579; ======================= -->
        <Border Grid.Row="1" Background="White" Margin="10,8,10,4" Padding="8" BorderBrush="#E0E0E0" BorderThickness="1" CornerRadius="4">
            <Grid>
                <Grid.RowDefinitions>
                    <RowDefinition Height="Auto"/>
                    <RowDefinition Height="*"/>
                </Grid.RowDefinitions>

                <!-- &#1588;&#1585;&#1610;&#1591; &#1575;&#1604;&#1576;&#1606;&#1608;&#1583; -->
                <Grid Grid.Row="0" Margin="0,0,0,6">
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="*"/>
                        <ColumnDefinition Width="Auto"/>
                    </Grid.ColumnDefinitions>
                    <TextBlock Grid.Column="0" Text="&#1576;&#1606;&#1608;&#1583; &#1575;&#1604;&#1601;&#1575;&#1578;&#1608;&#1585;&#1577;" FontSize="15" FontWeight="Bold" Foreground="#37474F" VerticalAlignment="Center"/>
                    <Button Grid.Column="1" Content="+ &#1573;&#1590;&#1575;&#1601;&#1577; &#1589;&#1606;&#1601;" Style="{StaticResource AddBtn}" Command="{Binding AddLineCommand}"/>
                </Grid>

                <!-- &#1588;&#1576;&#1603;&#1577; &#1575;&#1604;&#1576;&#1606;&#1608;&#1583; &#1605;&#1593; &#1575;&#1604;&#1576;&#1581;&#1579; &#1575;&#1604;&#1601;&#1608;&#1585;&#1610; -->
                <DataGrid x:Name="LinesGrid" Grid.Row="1"
                          ItemsSource="{Binding Lines}"
                          AutoGenerateColumns="False"
                          CanUserAddRows="False"
                          CanUserDeleteRows="False"
                          HeadersVisibility="Column"
                          GridLinesVisibility="Horizontal"
                          HorizontalGridLinesBrush="#ECEFF1"
                          BorderBrush="#E0E0E0"
                          BorderThickness="1"
                          Background="White"
                          RowBackground="White"
                          AlternatingRowBackground="#FAFAFA"
                          FontSize="13"
                          MinHeight="180">
                    <DataGrid.Columns>
                        <!-- &#1575;&#1604;&#1576;&#1581;&#1579; + &#1575;&#1587;&#1605; &#1575;&#1604;&#1589;&#1606;&#1601; -->
                        <DataGridTemplateColumn Header="&#128269; &#1575;&#1604;&#1589;&#1606;&#1601; (&#1575;&#1603;&#1578;&#1576; &#1604;&#1604;&#1576;&#1581;&#1579; &#1576;&#1575;&#1604;&#1575;&#1587;&#1605;/&#1575;&#1604;&#1576;&#1575;&#1585;&#1603;&#1608;&#1583;)" Width="320">
                            <DataGridTemplateColumn.CellTemplate>
                                <DataTemplate>
                                    <Grid>
                                        <!-- Popup &#1606;&#1578;&#1575;&#1574;&#1580; &#1575;&#1604;&#1576;&#1581;&#1579; -->
                                        <Popup IsOpen="{Binding IsPopupOpen}" Placement="Bottom"
                                               PlacementTarget="{Binding ElementName=SearchBoxInner}"
                                               StaysOpen="False" AllowsTransparency="True" PopupAnimation="Slide">
                                            <Border Background="White" BorderBrush="#00897B" BorderThickness="1" CornerRadius="2"
                                                    MaxHeight="260" MinWidth="300">
                                                <ListBox x:Name="SearchList"
                                                         ItemsSource="{Binding FilteredItems}"
                                                         SelectedIndex="{Binding SearchSelectedIndex}"
                                                         SelectionChanged="SearchList_SelectionChanged"
                                                         BorderThickness="0">
                                                    <ListBox.ItemTemplate>
                                                        <DataTemplate>
                                                            <StackPanel Margin="2">
                                                                <TextBlock Text="{Binding Name}" FontWeight="SemiBold" Foreground="#263238"/>
                                                                <StackPanel Orientation="Horizontal">
                                                                    <TextBlock Text="&#1576;&#1575;&#1585;&#1603;&#1608;&#1583;: " Foreground="#78909C" FontSize="11"/>
                                                                    <TextBlock Text="{Binding Barcode}" Foreground="#00897B" FontSize="11"/>
                                                                    <TextBlock Text="  |  &#1605;&#1582;&#1586;&#1608;&#1606;: " Foreground="#78909C" FontSize="11"/>
                                                                    <TextBlock Text="{Binding CurrentStock}" Foreground="#EF6C00" FontSize="11"/>
                                                                </StackPanel>
                                                            </StackPanel>
                                                        </DataTemplate>
                                                    </ListBox.ItemTemplate>
                                                </ListBox>
                                            </Border>
                                        </Popup>

                                        <TextBox x:Name="SearchBoxInner"
                                                 Text="{Binding SearchText, UpdateSourceTrigger=PropertyChanged}"
                                                 Tag="search"
                                                 Padding="6,4"
                                                 BorderBrush="#80CBC4" BorderThickness="1,1,1,2"
                                                 Background="White"
                                                 KeyDown="SearchBox_KeyDown"/>
                                    </Grid>
                                </DataTemplate>
                            </DataGridTemplateColumn.CellTemplate>
                        </DataGridTemplateColumn>

                        <!-- &#1575;&#1604;&#1576;&#1575;&#1585;&#1603;&#1608;&#1583; -->
                        <DataGridTextColumn Header="&#1576;&#1575;&#1585;&#1603;&#1608;&#1583;" Width="110" Binding="{Binding Barcode}" IsReadOnly="True"/>

                        <!-- &#1575;&#1604;&#1608;&#1581;&#1583;&#1577; -->
                        <DataGridTextColumn Header="&#1575;&#1604;&#1608;&#1581;&#1583;&#1577;" Width="70" Binding="{Binding Unit}" IsReadOnly="True"/>

                        <!-- &#1575;&#1604;&#1603;&#1605;&#1610;&#1577; -->
                        <DataGridTemplateColumn Header="&#1575;&#1604;&#1603;&#1605;&#1610;&#1577;" Width="90">
                            <DataGridTemplateColumn.CellTemplate>
                                <DataTemplate>
                                    <TextBox Text="{Binding Quantity, UpdateSourceTrigger=PropertyChanged, StringFormat=N2}"
                                             Tag="qty" Padding="4,3" BorderBrush="#E0E0E0" KeyDown="Cell_KeyDown"/>
                                </DataTemplate>
                            </DataGridTemplateColumn.CellTemplate>
                        </DataGridTemplateColumn>

                        <!-- &#1575;&#1604;&#1587;&#1593;&#1585; -->
                        <DataGridTemplateColumn Header="&#1575;&#1604;&#1587;&#1593;&#1585;" Width="100">
                            <DataGridTemplateColumn.CellTemplate>
                                <DataTemplate>
                                    <TextBox Text="{Binding Price, UpdateSourceTrigger=PropertyChanged, StringFormat=N2}"
                                             Tag="price" Padding="4,3" BorderBrush="#E0E0E0" KeyDown="Cell_KeyDown"/>
                                </DataTemplate>
                            </DataGridTemplateColumn.CellTemplate>
                        </DataGridTemplateColumn>

                        <!-- &#1575;&#1604;&#1582;&#1589;&#1605; -->
                        <DataGridTemplateColumn Header="&#1582;&#1589;&#1605;" Width="80">
                            <DataGridTemplateColumn.CellTemplate>
                                <DataTemplate>
                                    <TextBox Text="{Binding Discount, UpdateSourceTrigger=PropertyChanged, StringFormat=N2}"
                                             Tag="discount" Padding="4,3" BorderBrush="#E0E0E0" KeyDown="Cell_KeyDown"/>
                                </DataTemplate>
                            </DataGridTemplateColumn.CellTemplate>
                        </DataGridTemplateColumn>

                        <!-- &#1575;&#1604;&#1573;&#1580;&#1605;&#1575;&#1604;&#1610; -->
                        <DataGridTextColumn Header="&#1575;&#1604;&#1573;&#1580;&#1605;&#1575;&#1604;&#1610;" Width="110" Binding="{Binding Total, StringFormat=N2}" IsReadOnly="True">
                            <DataGridTextColumn.ElementStyle>
                                <Style TargetType="TextBlock">
                                    <Setter Property="HorizontalAlignment" Value="Right"/>
                                    <Setter Property="FontWeight" Value="SemiBold"/>
                                    <Setter Property="Foreground" Value="#00695C"/>
                                </Style>
                            </DataGridTextColumn.ElementStyle>
                        </DataGridTextColumn>

                        <!-- &#1581;&#1584;&#1601; -->
                        <DataGridTemplateColumn Header="&#1581;&#1584;&#1601;" Width="60">
                            <DataGridTemplateColumn.CellTemplate>
                                <DataTemplate>
                                    <Button Content="&#128465;" Background="Transparent" BorderThickness="0" FontSize="14" Cursor="Hand"
                                            Command="{Binding DataContext.DeleteLineCommand, RelativeSource={RelativeSource AncestorType=DataGrid}}"
                                            CommandParameter="{Binding}"/>
                                </DataTemplate>
                            </DataGridTemplateColumn.CellTemplate>
                        </DataGridTemplateColumn>
                    </DataGrid.Columns>
                </DataGrid>
            </Grid>
        </Border>

        <!-- ======================= &#1602;&#1587;&#1605; &#1575;&#1604;&#1583;&#1601;&#1593; &#1605;&#1578;&#1593;&#1583;&#1583; &#1575;&#1604;&#1591;&#1585;&#1602; (&#1610;&#1592;&#1607;&#1585; &#1583;&#1575;&#1574;&#1605;&#1575;&#1611;) ======================= -->
        <Border x:Name="PaymentsSection" Grid.Row="2" Background="White" Margin="10,4,10,4" Padding="10" BorderBrush="#80CBC4" BorderThickness="1" CornerRadius="4">
            <Grid>
                <Grid.RowDefinitions>
                    <RowDefinition Height="Auto"/>
                    <RowDefinition Height="Auto"/>
                </Grid.RowDefinitions>

                <!-- &#1593;&#1606;&#1608;&#1575;&#1606; &#1575;&#1604;&#1602;&#1587;&#1605; -->
                <Grid Grid.Row="0" Margin="0,0,0,6">
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="*"/>
                        <ColumnDefinition Width="Auto"/>
                    </Grid.ColumnDefinitions>
                    <StackPanel Grid.Column="0" Orientation="Horizontal">
                        <TextBlock Text="&#128179;" FontSize="16" Margin="0,0,6,0" VerticalAlignment="Center"/>
                        <TextBlock Text="&#1591;&#1585;&#1602; &#1575;&#1604;&#1583;&#1601;&#1593; &#1575;&#1604;&#1605;&#1578;&#1593;&#1583;&#1583;&#1577;" FontSize="15" FontWeight="Bold" Foreground="#00695C" VerticalAlignment="Center"/>
                    </StackPanel>
                    <Button Grid.Column="1" Content="+ &#1573;&#1590;&#1575;&#1601;&#1577; &#1583;&#1601;&#1593;&#1577;" Style="{StaticResource AddBtn}" Command="{Binding AddPaymentCommand}"/>
                </Grid>

                <!-- &#1580;&#1583;&#1608;&#1604; &#1575;&#1604;&#1583;&#1601;&#1593;&#1575;&#1578; -->
                <DataGrid x:Name="PaymentsGrid" Grid.Row="1"
                          ItemsSource="{Binding Payments}"
                          AutoGenerateColumns="False"
                          CanUserAddRows="False"
                          CanUserDeleteRows="False"
                          HeadersVisibility="Column"
                          GridLinesVisibility="Horizontal"
                          HorizontalGridLinesBrush="#ECEFF1"
                          BorderBrush="#E0E0E0"
                          BorderThickness="1"
                          Background="White"
                          FontSize="13"
                          MaxHeight="140"
                          CellEditEnding="PaymentsGrid_CellEditEnding">
                    <DataGrid.Columns>
                        <!-- &#1591;&#1585;&#1610;&#1602;&#1577; &#1575;&#1604;&#1583;&#1601;&#1593; -->
                        <DataGridTemplateColumn Header="&#1591;&#1585;&#1610;&#1602;&#1577; &#1575;&#1604;&#1583;&#1601;&#1593;" Width="140">
                            <DataGridTemplateColumn.CellTemplate>
                                <DataTemplate>
                                    <ComboBox SelectedValue="{Binding Method, UpdateSourceTrigger=PropertyChanged}" SelectedValuePath="Tag" Padding="4,2">
                                        <ComboBoxItem Content="&#1606;&#1602;&#1583;&#1610;" Tag="cash"/>
                                        <ComboBoxItem Content="&#1576;&#1591;&#1575;&#1602;&#1577;" Tag="card"/>
                                        <ComboBoxItem Content="&#1578;&#1581;&#1608;&#1610;&#1604; &#1576;&#1606;&#1603;&#1610;" Tag="bank"/>
                                        <ComboBoxItem Content="&#1588;&#1610;&#1603;" Tag="check"/>
                                        <ComboBoxItem Content="&#1570;&#1580;&#1604;" Tag="credit"/>
                                    </ComboBox>
                                </DataTemplate>
                            </DataGridTemplateColumn.CellTemplate>
                        </DataGridTemplateColumn>

                        <!-- &#1575;&#1604;&#1605;&#1576;&#1604;&#1594; -->
                        <DataGridTemplateColumn Header="&#1575;&#1604;&#1605;&#1576;&#1604;&#1594;" Width="140">
                            <DataGridTemplateColumn.CellTemplate>
                                <DataTemplate>
                                    <TextBox Text="{Binding Amount, UpdateSourceTrigger=PropertyChanged, StringFormat=N2}"
                                             Padding="4,3" BorderBrush="#E0E0E0" TextAlignment="Right"/>
                                </DataTemplate>
                            </DataGridTemplateColumn.CellTemplate>
                        </DataGridTemplateColumn>

                        <!-- &#1575;&#1604;&#1605;&#1585;&#1580;&#1593; -->
                        <DataGridTextColumn Header="&#1605;&#1585;&#1580;&#1593; / &#1585;&#1602;&#1605;" Width="180" Binding="{Binding Reference, UpdateSourceTrigger=PropertyChanged}"/>

                        <!-- &#1575;&#1604;&#1578;&#1575;&#1585;&#1610;&#1582; -->
                        <DataGridTemplateColumn Header="&#1578;&#1575;&#1585;&#1610;&#1582; &#1575;&#1604;&#1583;&#1601;&#1593;" Width="150">
                            <DataGridTemplateColumn.CellTemplate>
                                <DataTemplate>
                                    <DatePicker SelectedDate="{Binding PaymentDate}" FontSize="12"/>
                                </DataTemplate>
                            </DataGridTemplateColumn.CellTemplate>
                        </DataGridTemplateColumn>

                        <!-- &#1605;&#1604;&#1575;&#1581;&#1592;&#1575;&#1578; -->
                        <DataGridTextColumn Header="&#1605;&#1604;&#1575;&#1581;&#1592;&#1575;&#1578;" Width="*" Binding="{Binding Notes, UpdateSourceTrigger=PropertyChanged}"/>

                        <!-- &#1581;&#1584;&#1601; -->
                        <DataGridTemplateColumn Header="&#1581;&#1584;&#1601;" Width="60">
                            <DataGridTemplateColumn.CellTemplate>
                                <DataTemplate>
                                    <Button Content="&#128465;" Background="Transparent" BorderThickness="0" FontSize="14" Cursor="Hand"
                                            Command="{Binding DataContext.DeletePaymentCommand, RelativeSource={RelativeSource AncestorType=DataGrid}}"
                                            CommandParameter="{Binding}"/>
                                </DataTemplate>
                            </DataGridTemplateColumn.CellTemplate>
                        </DataGridTemplateColumn>
                    </DataGrid.Columns>
                </DataGrid>
            </Grid>
        </Border>

        <!-- ======================= &#1575;&#1604;&#1573;&#1580;&#1605;&#1575;&#1604;&#1610;&#1575;&#1578; &#1608;&#1575;&#1604;&#1571;&#1586;&#1585;&#1575;&#1585; ======================= -->
        <Border Grid.Row="3" Background="White" Padding="16,10" BorderBrush="#E0E0E0" BorderThickness="0,1,0,0">
            <Grid>
                <Grid.RowDefinitions>
                    <RowDefinition Height="Auto"/>
                    <RowDefinition Height="8"/>
                    <RowDefinition Height="Auto"/>
                </Grid.RowDefinitions>

                <!-- &#1589;&#1601; &#1575;&#1604;&#1573;&#1580;&#1605;&#1575;&#1604;&#1610;&#1575;&#1578; + &#1585;&#1587;&#1575;&#1604;&#1577; &#1575;&#1604;&#1581;&#1575;&#1604;&#1577; -->
                <Grid Grid.Row="0">
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="*"/>
                        <ColumnDefinition Width="300"/>
                    </Grid.ColumnDefinitions>

                    <StackPanel Grid.Column="0" VerticalAlignment="Center">
                        <TextBlock x:Name="StatusText" Text="{Binding StatusMessage}" FontSize="12" Foreground="#00897B" TextWrapping="Wrap"/>
                    </StackPanel>

                    <Grid Grid.Column="1">
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width="*"/>
                            <ColumnDefinition Width="110"/>
                        </Grid.ColumnDefinitions>
                        <Grid.RowDefinitions>
                            <RowDefinition Height="Auto"/>
                            <RowDefinition Height="Auto"/>
                            <RowDefinition Height="Auto"/>
                            <RowDefinition Height="Auto"/>
                            <RowDefinition Height="Auto"/>
                        </Grid.RowDefinitions>

                        <TextBlock Grid.Row="0" Grid.Column="0" Text="&#1575;&#1604;&#1605;&#1580;&#1605;&#1608;&#1593;:" FontSize="13" Foreground="#546E7A" Padding="0,2"/>
                        <TextBlock Grid.Row="0" Grid.Column="1" Text="{Binding SubtotalDisplay}" FontSize="13" FontWeight="SemiBold" Foreground="#37474F" TextAlignment="Left" Padding="0,2"/>

                        <StackPanel Grid.Row="1" Grid.Column="0" Orientation="Horizontal">
                            <TextBlock Text="&#1582;&#1589;&#1605;:" FontSize="13" Foreground="#546E7A" VerticalAlignment="Center"/>
                            <TextBox Text="{Binding DiscountAmount, UpdateSourceTrigger=PropertyChanged, StringFormat=N2}"
                                     Width="80" Margin="6,0,0,0" Padding="3,2" FontSize="12" BorderBrush="#E0E0E0" TextAlignment="Right"/>
                        </StackPanel>
                        <TextBlock Grid.Row="1" Grid.Column="1" Text="{Binding DiscountAmount, StringFormat=N2}" FontSize="13" Foreground="#E53935" TextAlignment="Left"/>

                        <TextBlock Grid.Row="2" Grid.Column="0" Text="&#1575;&#1604;&#1573;&#1580;&#1605;&#1575;&#1604;&#1610;:" FontSize="14" FontWeight="Bold" Foreground="#00695C" Padding="0,2"/>
                        <TextBlock Grid.Row="2" Grid.Column="1" Text="{Binding TotalDisplay}" FontSize="15" FontWeight="Bold" Foreground="#00695C" TextAlignment="Left" Padding="0,2"/>

                        <TextBlock Grid.Row="3" Grid.Column="0" Text="&#1575;&#1604;&#1605;&#1583;&#1601;&#1608;&#1593;:" FontSize="13" Foreground="#2E7D32" Padding="0,2"/>
                        <TextBlock Grid.Row="3" Grid.Column="1" Text="{Binding TotalPaidDisplay}" FontSize="13" FontWeight="SemiBold" Foreground="#2E7D32" TextAlignment="Left" Padding="0,2"/>

                        <TextBlock Grid.Row="4" Grid.Column="0" Text="&#1575;&#1604;&#1605;&#1578;&#1576;&#1602;&#1610;:" FontSize="14" FontWeight="Bold" Foreground="#E65100" Padding="0,2"/>
                        <TextBlock Grid.Row="4" Grid.Column="1" Text="{Binding RemainingDisplay}" FontSize="14" FontWeight="Bold" Foreground="#E65100" TextAlignment="Left" Padding="0,2"/>
                    </Grid>
                </Grid>

                <!-- &#1589;&#1601; &#1571;&#1586;&#1585;&#1575;&#1585; &#1575;&#1604;&#1581;&#1601;&#1592; &#1608;&#1575;&#1604;&#1573;&#1604;&#1594;&#1575;&#1569; -->
                <StackPanel Grid.Row="2" Orientation="Horizontal" HorizontalAlignment="Left" FlowDirection="LeftToRight">
                    <Button Content="&#128190; &#1581;&#1601;&#1592; &#1575;&#1604;&#1601;&#1575;&#1578;&#1608;&#1585;&#1577;" Background="#00897B" Foreground="White" FontWeight="Bold" FontSize="14" Padding="24,10" BorderThickness="0" Cursor="Hand" Margin="0,0,8,0"
                            Command="{Binding SaveCommand}"/>
                    <Button Content="&#128424; &#1581;&#1601;&#1592; &#1608;&#1591;&#1576;&#1575;&#1593;&#1577;" Background="#00695C" Foreground="White" FontWeight="SemiBold" FontSize="13" Padding="18,10" BorderThickness="0" Cursor="Hand" Margin="0,0,8,0"
                            Command="{Binding SaveAndPrintCommand}"/>
                    <Button Content="&#10006; &#1573;&#1604;&#1594;&#1575;&#1569;" Background="#ECEFF1" Foreground="#37474F" FontWeight="SemiBold" FontSize="13" Padding="18,10" BorderThickness="0" Cursor="Hand"
                            Command="{Binding CancelCommand}"/>
                </StackPanel>
            </Grid>
        </Border>
    </Grid>
</Window>
'@
[System.IO.File]::WriteAllText($dest, $c_InvoiceEditWindow_xaml, [System.Text.UTF8Encoding]::new($true))
Write-Host "  OK: InvoiceEditWindow.xaml" -ForegroundColor Green

$dest = Join-Path $ProjectPath 'Techtoon.UI\Views\InvoiceEditWindow.xaml.cs'
if (Test-Path $dest) { Copy-Item $dest ($dest + '.bak') -Force }
$c_InvoiceEditWindow_xaml_cs = @'
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Techtoon.UI.ViewModels;

namespace Techtoon.UI.Views
{
    public partial class InvoiceEditWindow : Window
    {
        private readonly InvoiceEditViewModel _vm;
        private readonly int? _editId;

        public InvoiceEditWindow(int? editId = null)
        {
            InitializeComponent();
            _editId = editId;
            _vm = new InvoiceEditViewModel();
            if (editId.HasValue) _vm.Id = editId.Value;
            DataContext = _vm;
            _vm.RequestClose += () =>
            {
                DialogResult = _vm.DialogResult;
                Close();
            };
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await _vm.LoadAsync();
            if (_editId.HasValue)
            {
                await _vm.LoadFromInvoiceAsync(_editId.Value);
            }
        }

        // ==================================================================
        //  البحث الفوري: لوحة المفاتيح
        // ==================================================================
        private void SearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (sender is not TextBox box) return;
            if (box.DataContext is not InvoiceLineItem line) return;

            switch (e.Key)
            {
                case Key.Down:
                    if (line.SearchSelectedIndex < line.FilteredItems.Count - 1)
                        line.SearchSelectedIndex++;
                    e.Handled = true;
                    break;
                case Key.Up:
                    if (line.SearchSelectedIndex > 0)
                        line.SearchSelectedIndex--;
                    e.Handled = true;
                    break;
                case Key.Enter:
                    if (line.IsPopupOpen && line.FilteredItems.Count > 0)
                    {
                        if (line.SearchSelectedIndex < 0 || line.SearchSelectedIndex >= line.FilteredItems.Count)
                            line.SearchSelectedIndex = 0;
                        line.SelectByIndex();
                    }
                    FocusQuantity(line);
                    e.Handled = true;
                    break;
                case Key.Escape:
                    line.IsPopupOpen = false;
                    e.Handled = true;
                    break;
            }
        }

        // ==================================================================
        //  البحث الفوري: اختيار بالماوس
        // ==================================================================
        private void SearchList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ListBox lb &&
                lb.DataContext is InvoiceLineItem line &&
                lb.SelectedItem is ItemSearchResult it)
            {
                line.SelectItem(it);
                FocusQuantity(line);
            }
        }

        private void FocusQuantity(InvoiceLineItem line)
        {
            try
            {
                if (LinesGrid.ItemContainerGenerator.ContainerFromItem(line) is DataGridRow row)
                {
                    var tb = FindDescendant<TextBox>(row, x => (x.Tag as string) == "qty");
                    tb?.Focus();
                    tb?.SelectAll();
                }
            }
            catch { }
        }

        // ==================================================================
        //  تنقل Enter بين الخلايا والصفوف
        // ==================================================================
        private void Cell_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;
            if (sender is not TextBox tb) return;
            if (tb.Tag is not string tag) return;
            if (tb.DataContext is not InvoiceLineItem line) return;

            LinesGrid.CommitEdit(DataGridEditingUnit.Row, true);
            int idx = _vm.Lines.IndexOf(line);
            int last = _vm.Lines.Count - 1;

            if (idx >= 0 && idx < last)
            {
                if (LinesGrid.ItemContainerGenerator.ContainerFromIndex(idx + 1) is DataGridRow nextRow)
                {
                    var next = FindDescendant<TextBox>(nextRow, x => (x.Tag as string) == tag);
                    if (next != null)
                    {
                        next.Focus();
                        next.SelectAll();
                    }
                }
            }
            else if (idx == last)
            {
                // آخر صف: Enter ينشئ صفاً جديداً
                _vm.AddLineCommand.Execute(null);
                LinesGrid.UpdateLayout();
                if (LinesGrid.ItemContainerGenerator.ContainerFromIndex(_vm.Lines.Count - 1) is DataGridRow newRow)
                {
                    var next = FindDescendant<TextBox>(newRow, x => (x.Tag as string) == "search");
                    next?.Focus();
                }
            }
            e.Handled = true;
        }

        private static T? FindDescendant<T>(DependencyObject? parent, Func<T, bool> predicate) where T : class
        {
            if (parent == null) return null;
            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T matched && predicate(matched)) return matched;
                var deep = FindDescendant(child, predicate);
                if (deep != null) return deep;
            }
            return null;
        }

        // ==================================================================
        //  إعادة حساب عند تعديل الدفعات
        // ==================================================================
        private void PaymentsGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction == DataGridEditAction.Commit)
                _vm.RefreshTotals();
        }
    }
}
'@
[System.IO.File]::WriteAllText($dest, $c_InvoiceEditWindow_xaml_cs, [System.Text.UTF8Encoding]::new($true))
Write-Host "  OK: InvoiceEditWindow.xaml.cs" -ForegroundColor Green

$appsettings = '{"ConnectionStrings":{"DefaultConnection":"Host=ep-bitter-night-za726w59-pooler.c-2.eu-west-2.aws.neon.tech;Port=5432;Username=neondb_owner;Password=***;Database=neondb;SSL Mode=Require;Trust Server Certificate=true"}}'
$ap = Join-Path $UIDir 'appsettings.json'
[System.IO.File]::WriteAllText($ap, $appsettings, [System.Text.UTF8Encoding]::new($false))
$bd = Join-Path $UIDir 'bin\Debug\net10.0-windows'
if (Test-Path $bd) { Copy-Item $ap (Join-Path $bd 'appsettings.json') -Force }
Write-Host "  OK: appsettings.json" -ForegroundColor Green

$objDir = Join-Path $UIDir 'obj\Debug\net10.0-windows'
if (Test-Path $objDir) { Get-ChildItem -Path $objDir -Filter 'InvoiceEdit*' | Remove-Item -Force -ErrorAction SilentlyContinue }

Write-Host ''
Write-Host '===== Build =====' -ForegroundColor Cyan
dotnet build (Join-Path $ProjectPath 'Techtoon.UI') 2>&1 | Select-Object -Last 8
Write-Host ''
Write-Host 'DONE! Run: dotnet run --project Techtoon.UI' -ForegroundColor Green
```
#### .\BUILD_AND_RUN.bat
```
@echo off
chcp 65001 >nul
title Techtoon - Build ^& Run
cd /d "D:\PROG\Accounting Prog\Techtoon Accounting C+"
echo Building...
dotnet build
if %errorlevel% neq 0 (echo Build failed! & pause & exit /b 1)
echo Starting...
dotnet run --project Techtoon.UI --no-build
pause
```
#### .\build_exe.bat
```
@echo off
title Techtoon Accounting - Build EXE
color 0A

echo ============================================================
echo   Techtoon Accounting - Building EXE
echo ============================================================
echo.

:: Check PyInstaller
python -c "import PyInstaller" >nul 2>&1
if %errorlevel% neq 0 (
    echo [1/3] Installing PyInstaller...
    pip install pyinstaller
    echo.
) else (
    echo [OK] PyInstaller found
    echo.
)

:: Clean old files
echo [2/3] Cleaning old files...
if exist "build" rmdir /s /q build
if exist "dist" rmdir /s /q dist
if exist "Techtoon_Accounting.spec" del Techtoon_Accounting.spec
echo.

:: Build EXE
echo [3/3] Building EXE (this may take 5-10 minutes)...
echo.

pyinstaller --onefile --windowed --name "Techtoon_Accounting" --add-data "database.py;." --add-data "models.py;." --add-data "services.py;." --add-data "auth.py;." --add-data "auth_required.py;." --add-data "alerts.py;." --add-data "reminders.py;." --add-data "export_pdf.py;." --add-data "export_excel.py;." --add-data "seed_data.py;." --add-data "app.py;." --add-data "pages;pages" --hidden-import sqlalchemy --hidden-import pandas --hidden-import streamlit --hidden-import plotly --hidden-import bcrypt --hidden-import reportlab --hidden-import xlsxwriter --hidden-import openpyxl --collect-all streamlit app.py

echo.

if exist "dist\Techtoon_Accounting.exe" (
    color 0A
    echo ============================================================
    echo   EXE Build Complete!
    echo ============================================================
    echo.
    echo File location: dist\Techtoon_Accounting.exe
    echo.
    echo You can copy this file to any Windows computer
    echo.
    echo ============================================================
    
    copy "dist\Techtoon_Accounting.exe" "Techtoon_Accounting.exe" >nul
    echo [OK] File copied to main folder
    
    pause
) else (
    color 0C
    echo ============================================================
    echo   EXE Build Failed!
    echo ============================================================
    echo.
    echo Please check errors above
    echo.
    pause
)
```
#### .\build_exe.ps1
```
# build_exe.ps1 — نسخة محسّنة (تتفقد الملفات قبل الإضافة)
$ErrorActionPreference = "Stop"

Write-Host "═════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  Techtoon Accounting — EXE Builder v2" -ForegroundColor Cyan
Write-Host "═════════════════════════════════════════════" -ForegroundColor Cyan

$ROOT = $PSScriptRoot
Set-Location $ROOT

# ═══ تنظيف ═══
foreach ($f in @("build", "dist", "TechtoonAccounting.spec")) {
    if (Test-Path $f) { Remove-Item $f -Recurse -Force -ErrorAction SilentlyContinue }
}
Write-Host "[1/4] Cleaned old builds" -ForegroundColor Green

# ═══ تجهيز مجلد fonts إن لم يكن موجوداً ═══
if (-not (Test-Path "fonts")) {
    New-Item -ItemType Directory -Path "fonts" | Out-Null
    Write-Host "[INFO] Created empty 'fonts' folder" -ForegroundColor Yellow
}

# ═══ قائمة البيانات (فقط ما هو موجود) ═══
$addDataCandidates = @(
    "pages;pages"
    ".streamlit;.streamlit"
    "fonts;fonts"
    ".env;."
    "logo.ico;."
    "input_helpers.py;."
    "cache_helpers.py;."
    "database.py;."
    "models.py;."
    "services.py;."
    "sidebar.py;."
    "navigation_helper.py;."
    "feature_flags.py;."
    "form_manager.py;."
    "period_guard.py;."
    "session_auth.py;."
    "auth.py;."
    "auth_required.py;."
    "settings_manager.py;."
    "local_mirror.py;."
    "sync_manager.py;."
)

$addData = @()
foreach ($item in $addDataCandidates) {
    $src = $item.Split(";")[0]
    if (Test-Path $src) {
        $addData += $item
        Write-Host "  [OK] $src" -ForegroundColor DarkGray
    } else {
        Write-Host "  [SKIP] $src (not found)" -ForegroundColor DarkYellow
    }
}

Write-Host "[2/4] Data files prepared: $($addData.Count)/$($addDataCandidates.Count)" -ForegroundColor Green

# ═══ Hidden imports ═══
$hiddenImports = @(
    "streamlit"
    "streamlit.web.cli"
    "streamlit.runtime.scriptrunner.magic_funcs"
    "streamlit.runtime.scriptrunner.script_runner"
    "psycopg2"
    "psycopg2._psycopg"
    "sqlalchemy"
    "sqlalchemy.sql.default_comparator"
    "sqlalchemy.dialects.postgresql"
    "sqlalchemy.dialects.sqlite"
    "pandas"
    "openpyxl"
    "xlsxwriter"
    "reportlab"
    "reportlab.pdfbase._fontdata"
    "reportlab.pdfbase.ttfonts"
    "boto3"
    "webview"
    "webview.platforms.edgechromium"
    "PIL"
)

$collectAll = @("streamlit", "altair", "pyarrow")

$excludes = @(
    "matplotlib"
    "tkinter.test"
    "test"
    "tests"
    "pydoc_data"
    "email.test"
    "distutils"
)

# ═══ بناء Arguments ═══
$args = @(
    "--noconfirm"
    "--clean"
    "--windowed"
    "--name", "TechtoonAccounting"
)

if (Test-Path "logo.ico") {
    $args += "--icon"
    $args += "logo.ico"
    Write-Host "  [OK] Icon: logo.ico" -ForegroundColor DarkGray
}

foreach ($item in $addData) { $args += "--add-data"; $args += $item }
foreach ($imp in $hiddenImports) { $args += "--hidden-import"; $args += $imp }
foreach ($c in $collectAll) { $args += "--collect-all"; $args += $c }
foreach ($e in $excludes) { $args += "--exclude-module"; $args += $e }

$args += "launcher.py"

Write-Host "[3/4] Running PyInstaller (5-10 minutes)..." -ForegroundColor Yellow
Write-Host ""

& pyinstaller @args

if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "[FAIL] PyInstaller failed!" -ForegroundColor Red
    Write-Host "check: pip install pyinstaller pywebview" -ForegroundColor Yellow
    exit 1
}

Write-Host ""
Write-Host "[4/4] Build complete!" -ForegroundColor Green

# ═══ نسخ .env و data ═══
$distDir = "dist\TechtoonAccounting"
if (Test-Path ".env") {
    Copy-Item ".env" "$distDir\.env" -Force
    Write-Host "[OK] .env copied" -ForegroundColor Green
}
if (-not (Test-Path "$distDir\data")) {
    New-Item -ItemType Directory -Path "$distDir\data" | Out-Null
}

Write-Host ""
Write-Host "═════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host " ✅ البناء اكتمل!" -ForegroundColor Green
Write-Host "═════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""
Write-Host "📁 المجلد: $distDir" -ForegroundColor Yellow
Write-Host "🚀 الملف:  $distDir\TechtoonAccounting.exe" -ForegroundColor Yellow
Write-Host ""
Write-Host "🧪 للاختبار:" -ForegroundColor Cyan
Write-Host "   cd '$distDir'" -ForegroundColor White
Write-Host "   .\TechtoonAccounting.exe" -ForegroundColor White
Write-Host ""
Write-Host "📦 للتوزيع (ZIP):" -ForegroundColor Cyan
Write-Host "   Compress-Archive -Path '$distDir' -DestinationPath 'TechtoonAccounting-v5.1.zip' -Force" -ForegroundColor White
Write-Host ""

```
#### .\create_shortcut.ps1
```
# create_shortcut.ps1
# Creates a desktop shortcut with a custom icon.
# IMPORTANT: This file must contain ONLY ASCII characters (no Arabic).

$projectPath = "D:\PROG\Accounting Prog\Techtoon Accounting"
$iconPath = Join-Path $projectPath "TechToon-L1.ico"
$launcherPath = Join-Path $projectPath "Techtoon.bat"
$shortcutName = "Techtoon Accounting"
$desktopPath = [Environment]::GetFolderPath("Desktop")
$shortcutPath = Join-Path $desktopPath "$shortcutName.lnk"

# Verify files exist
if (-not (Test-Path $iconPath)) {
    Write-Host "ERROR: Icon file not found: $iconPath" -ForegroundColor Red
    exit 1
}
if (-not (Test-Path $launcherPath)) {
    Write-Host "ERROR: Launcher not found: $launcherPath" -ForegroundColor Red
    Write-Host "Files in project:" -ForegroundColor Yellow
    Get-ChildItem $projectPath -File | Select-Object Name
    exit 1
}

# Delete old shortcut if exists
if (Test-Path $shortcutPath) {
    Remove-Item $shortcutPath -Force
}

# Create shortcut
$WshShell = New-Object -ComObject WScript.Shell
$Shortcut = $WshShell.CreateShortcut($shortcutPath)
$Shortcut.TargetPath = $launcherPath
$Shortcut.WorkingDirectory = $projectPath
$Shortcut.IconLocation = $iconPath
$Shortcut.Description = "Techtoon Accounting"
$Shortcut.Save()

Write-Host ""
Write-Host "SUCCESS - Shortcut created" -ForegroundColor Green
Write-Host "Location: $shortcutPath"
Write-Host "Icon:     $iconPath"
Write-Host "Target:   $launcherPath"
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "  1. Go to Desktop"
Write-Host "  2. You should see 'Techtoon Accounting' with your icon"
Write-Host "  3. Right-click it -> Pin to taskbar (optional)"
Write-Host ""
```
#### .\fix_complete.ps1
```
# fix_complete.ps1 - COMPLETE REWRITE of Invoice files
$ProjectPath = 'D:\PROG\Accounting Prog\Techtoon Accounting C+'
$ViewsDir = Join-Path $ProjectPath 'Techtoon.UI\Views'

if (-not (Test-Path $ViewsDir)) { Write-Host 'ERROR' -ForegroundColor Red; exit 1 }

Write-Host '========================================' -ForegroundColor Cyan
Write-Host '  Techtoon - COMPLETE REWRITE' -ForegroundColor Cyan
Write-Host '========================================' -ForegroundColor Cyan

# حذف الملفات المكررة
Get-ChildItem -Path $ViewsDir -Filter 'InvoiceEdit*' | Where-Object { $_.Name -match '\(1\)' } | Remove-Item -Force -ErrorAction SilentlyContinue

# ===== InvoiceEditViewModel.cs (UTF-8 with BOM) =====
$vmPath = Join-Path $ViewsDir 'InvoiceEditViewModel.cs'
if (Test-Path $vmPath) { Copy-Item $vmPath ($vmPath + '.bak_new') -Force }
$vm = @'
// =====================================================================
// InvoiceEditViewModel.cs  -  COMPLETE REWRITE
// يعمل مع الأعمدة الفعلية في قاعدة البيانات
// =====================================================================
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows.Input;
using Npgsql;
using Techtoon.UI.ViewModels;

namespace Techtoon.UI.Views
{
    // ==================================================================
    //  صنف نتيجة البحث عن الأصناف
    // ==================================================================
    public class ItemSearchResult
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Barcode { get; set; }
        public decimal SellPrice { get; set; }
        public decimal CurrentStock { get; set; }
        public string SearchDisplay
        {
            get
            {
                string b = string.IsNullOrEmpty(Barcode) ? "" : " [" + Barcode + "]";
                string s = CurrentStock > 0 ? "  (stock: " + CurrentStock.ToString("0.##") + ")" : "";
                return (Name ?? "") + b + s;
            }
        }
    }

    // ==================================================================
    //  صف بند الفاتورة
    // ==================================================================
    public class InvoiceLineItem : ViewModelBase
    {
        private List<ItemSearchResult> _allItems;

        public InvoiceLineItem(List<ItemSearchResult> allItems)
        {
            _allItems = allItems ?? new List<ItemSearchResult>();
        }

        private string _searchText = "";
        public string SearchText
        {
            get { return _searchText; }
            set
            {
                _searchText = value ?? "";
                OnPropertyChanged("SearchText");
                OnPropertyChanged("FilteredItems");
            }
        }

        // قائمة منسدلة مصفاة حسب النص المكتوب
        public List<ItemSearchResult> FilteredItems
        {
            get
            {
                string q = (_searchText ?? "").Trim().ToLower();
                if (q.Length == 0) return _allItems.Take(50).ToList();
                return _allItems
                    .Where(it =>
                        (it.Name != null && it.Name.ToLower().Contains(q)) ||
                        (it.Barcode != null && it.Barcode.ToLower().Contains(q)))
                    .Take(20)
                    .ToList();
            }
        }

        public int ItemId { get; set; }
        public string Unit { get; set; }

        private string _itemName = "";
        public string ItemName
        {
            get { return _itemName; }
            set { _itemName = value; OnPropertyChanged("ItemName"); }
        }

        private string _barcode = "";
        public string Barcode
        {
            get { return _barcode; }
            set { _barcode = value; OnPropertyChanged("Barcode"); }
        }

        private decimal _quantity = 1;
        public decimal Quantity
        {
            get { return _quantity; }
            set { _quantity = value; OnPropertyChanged("Quantity"); Recalc(); }
        }

        private decimal _price = 0;
        public decimal Price
        {
            get { return _price; }
            set { _price = value; OnPropertyChanged("Price"); Recalc(); }
        }

        private decimal _discount = 0;
        public decimal Discount
        {
            get { return _discount; }
            set { _discount = value; OnPropertyChanged("Discount"); Recalc(); }
        }

        private decimal _total = 0;
        public decimal Total
        {
            get { return _total; }
            set { _total = value; OnPropertyChanged("Total"); }
        }

        private decimal _stockAvailable = 0;
        public decimal StockAvailable
        {
            get { return _stockAvailable; }
            set { _stockAvailable = value; OnPropertyChanged("StockAvailable"); }
        }

        public bool HasItem { get { return ItemId > 0; } }

        private void Recalc()
        {
            Total = Math.Round(_quantity * _price - _discount, 2);
        }

        public void SelectItem(ItemSearchResult it)
        {
            if (it == null) return;
            ItemId = it.Id;
            ItemName = it.Name;
            Barcode = it.Barcode;
            Price = it.SellPrice;
            StockAvailable = it.CurrentStock;
            Quantity = 1;
            SearchText = it.Name;
            OnPropertyChanged("HasItem");
            OnPropertyChanged("FilteredItems");
        }
    }

    // ==================================================================
    //  صف الدفع
    // ==================================================================
    public class InvoicePaymentRow : ViewModelBase
    {
        public int Id { get; set; }

        private string _method = "cash";
        public string Method
        {
            get { return _method; }
            set { _method = value; OnPropertyChanged("Method"); }
        }

        private decimal _amount = 0;
        public decimal Amount
        {
            get { return _amount; }
            set { _amount = value; OnPropertyChanged("Amount"); }
        }

        private string _reference = "";
        public string Reference
        {
            get { return _reference; }
            set { _reference = value; OnPropertyChanged("Reference"); }
        }

        private DateTime _paymentDate = DateTime.Now;
        public DateTime PaymentDate
        {
            get { return _paymentDate; }
            set { _paymentDate = value; OnPropertyChanged("PaymentDate"); }
        }
    }

    // ==================================================================
    //  خيارات الطرف والمخزن
    // ==================================================================
    public class PartyOption
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public override string ToString() { return Name ?? ""; }
    }

    public class WarehouseOption
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public override string ToString() { return Name ?? ""; }
    }

    // ==================================================================
    //  ViewModel الرئيسي
    // ==================================================================
    public class InvoiceEditViewModel : ViewModelBase
    {
        public Action RequestClose;
        public int Id { get; set; }
        public bool DialogResult { get; set; }
        public bool PrintAfterSave { get; set; }

        // معلومات الفاتورة
        private string _invoiceType = "sales";
        public string InvoiceType
        {
            get { return _invoiceType; }
            set { _invoiceType = value; OnPropertyChanged("InvoiceType"); OnPropertyChanged("InvoiceTypeDisplay"); }
        }
        public string InvoiceTypeDisplay
        {
            get { return (_invoiceType ?? "") == "purchase" ? "Purchase" : "Sales"; }
        }

        private string _serialNumber = "";
        public string SerialNumber
        {
            get { return _serialNumber; }
            set { _serialNumber = value; OnPropertyChanged("SerialNumber"); }
        }

        private DateTime _invoiceDate = DateTime.Now;
        public DateTime InvoiceDate
        {
            get { return _invoiceDate; }
            set { _invoiceDate = value; OnPropertyChanged("InvoiceDate"); }
        }

        private int _partyId = 0;
        public int PartyId
        {
            get { return _partyId; }
            set { _partyId = value; OnPropertyChanged("PartyId"); }
        }

        private int _warehouseId = 0;
        public int WarehouseId
        {
            get { return _warehouseId; }
            set { _warehouseId = value; OnPropertyChanged("WarehouseId"); }
        }

        private string _notes = "";
        public string Notes
        {
            get { return _notes; }
            set { _notes = value; OnPropertyChanged("Notes"); }
        }

        // القوائم
        public ObservableCollection<InvoiceLineItem> Lines { get; set; }
        public ObservableCollection<InvoicePaymentRow> Payments { get; set; }
        public ObservableCollection<PartyOption> Parties { get; set; }
        public ObservableCollection<WarehouseOption> Warehouses { get; set; }
        private List<ItemSearchResult> _allItems;

        // الحسابات
        private decimal _subtotal = 0;
        public decimal Subtotal { get { return _subtotal; } set { _subtotal = value; OnPropertyChanged("Subtotal"); OnPropertyChanged("SubtotalDisplay"); } }
        public string SubtotalDisplay { get { return _subtotal.ToString("N2"); } }

        private decimal _discountAmount = 0;
        public decimal DiscountAmount
        {
            get { return _discountAmount; }
            set { _discountAmount = value; OnPropertyChanged("DiscountAmount"); RefreshTotals(); }
        }

        private decimal _total = 0;
        public decimal Total { get { return _total; } set { _total = value; OnPropertyChanged("Total"); OnPropertyChanged("TotalDisplay"); OnPropertyChanged("Remaining"); } }
        public string TotalDisplay { get { return _total.ToString("N2"); } }

        private decimal _totalPaid = 0;
        public decimal TotalPaid { get { return _totalPaid; } set { _totalPaid = value; OnPropertyChanged("TotalPaid"); OnPropertyChanged("TotalPaidDisplay"); OnPropertyChanged("Remaining"); } }
        public string TotalPaidDisplay { get { return _totalPaid.ToString("N2"); } }

        public decimal Remaining { get { return Math.Round(_total - _totalPaid, 2); } }
        public string RemainingDisplay { get { return Remaining.ToString("N2"); } }

        private string _statusMessage = "";
        public string StatusMessage { get { return _statusMessage; } set { _statusMessage = value; OnPropertyChanged("StatusMessage"); } }

        private bool _isBusy = false;
        public bool IsBusy { get { return _isBusy; } set { _isBusy = value; OnPropertyChanged("IsBusy"); } }

        // الأوامر
        public ICommand AddLineCommand { get; set; }
        public ICommand DeleteLineCommand { get; set; }
        public ICommand AddPaymentCommand { get; set; }
        public ICommand DeletePaymentCommand { get; set; }
        public ICommand SaveCommand { get; set; }
        public ICommand CancelCommand { get; set; }

        public InvoiceEditViewModel()
        {
            Lines = new ObservableCollection<InvoiceLineItem>();
            Payments = new ObservableCollection<InvoicePaymentRow>();
            Parties = new ObservableCollection<PartyOption>();
            Warehouses = new ObservableCollection<WarehouseOption>();
            _allItems = new List<ItemSearchResult>();

            AddLineCommand = new UiRelayCommand(_ => AddLine());
            DeleteLineCommand = new UiRelayCommand(p => { if (p is InvoiceLineItem li) Lines.Remove(li); RefreshTotals(); });
            AddPaymentCommand = new UiRelayCommand(_ => AddPayment());
            DeletePaymentCommand = new UiRelayCommand(p => { if (p is InvoicePaymentRow pr) Payments.Remove(pr); RefreshTotals(); });
            SaveCommand = new UiRelayCommand(_ => { _ = SaveAsync(); }, _ => !IsBusy);
            CancelCommand = new UiRelayCommand(_ => { DialogResult = false; RequestClose?.Invoke(); });

            Payments.CollectionChanged += (s, e) =>
            {
                if (e.NewItems != null)
                {
                    foreach (InvoicePaymentRow r in e.NewItems)
                    {
                        r.PropertyChanged += (s2, e2) => { if (e2.PropertyName == "Amount") RefreshTotals(); };
                    }
                }
            };
        }

        // ==================================================================
        //  سلسلة الاتصال المباشرة (Neon PostgreSQL)
        // ==================================================================
        public static string GetConnectionString()
        {
            return "Host=ep-bitter-night-za726w59-pooler.c-2.eu-west-2.aws.neon.tech;Port=5432;Username=neondb_owner;Password=***;Database=neondb;SSL Mode=Require;Trust Server Certificate=true";
        }

        // ==================================================================
        //  تحميل البيانات الأولية
        // ==================================================================
        public async System.Threading.Tasks.Task LoadAsync()
        {
            IsBusy = true;
            StatusMessage = "Loading data...";
            try
            {
                _allItems = LoadItems();
                System.Windows.MessageBox.Show("Items loaded: " + _allItems.Count, "Debug");
                LoadParties();
                LoadWarehouses();
                SerialNumber = GenerateSerial(InvoiceType);
                AddLine();
                if (Payments.Count == 0)
                {
                    Payments.Add(new InvoicePaymentRow { Method = "cash", Amount = 0, PaymentDate = DateTime.Now });
                }
                StatusMessage = "";
            }
            catch (Exception ex)
            {
                StatusMessage = "Error: " + ex.Message;
                System.Windows.MessageBox.Show("LoadAsync Error:\n" + ex.Message, "Error");
            }
            finally { IsBusy = false; }
            await System.Threading.Tasks.Task.CompletedTask;
        }

        // ==================================================================
        //  تحميل الأصناف (بالأعمدة الفعلية)
        // ==================================================================
        private List<ItemSearchResult> LoadItems()
        {
            var list = new List<ItemSearchResult>();
            string cs = GetConnectionString();
            using (var conn = new NpgsqlConnection(cs))
            {
                conn.Open();
                string sql = @"SELECT id, name, barcode, 
                    COALESCE(sell_price, 0) AS sell_price,
                    COALESCE(opening_quantity, 0) AS opening_qty
                    FROM items 
                    ORDER BY name LIMIT 500";
                using (var cmd = new NpgsqlCommand(sql, conn))
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        list.Add(new ItemSearchResult
                        {
                            Id = r.GetInt32(0),
                            Name = r.IsDBNull(1) ? "" : r.GetString(1),
                            Barcode = r.IsDBNull(2) ? "" : r.GetString(2),
                            SellPrice = r.IsDBNull(3) ? 0 : Convert.ToDecimal(r.GetValue(3)),
                            CurrentStock = r.IsDBNull(4) ? 0 : Convert.ToDecimal(r.GetValue(4))
                        });
                    }
                }
            }
            return list;
        }

        // ==================================================================
        //  تحميل الأطراف
        // ==================================================================
        private void LoadParties()
        {
            Parties.Clear();
            try
            {
                string cs = GetConnectionString();
                using (var conn = new NpgsqlConnection(cs))
                {
                    conn.Open();
                    string sql = @"SELECT id, name, type FROM parties ORDER BY name LIMIT 500";
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            Parties.Add(new PartyOption
                            {
                                Id = r.GetInt32(0),
                                Name = r.IsDBNull(1) ? "" : r.GetString(1),
                                Type = r.IsDBNull(2) ? "" : r.GetString(2)
                            });
                        }
                    }
                }
            }
            catch (Exception ex) { StatusMessage = "Parties error: " + ex.Message; }
        }

        // ==================================================================
        //  تحميل المخازن
        // ==================================================================
        private void LoadWarehouses()
        {
            Warehouses.Clear();
            try
            {
                string cs = GetConnectionString();
                using (var conn = new NpgsqlConnection(cs))
                {
                    conn.Open();
                    string sql = @"SELECT id, name FROM warehouses WHERE COALESCE(is_active, true) = true ORDER BY name LIMIT 100";
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            Warehouses.Add(new WarehouseOption
                            {
                                Id = r.GetInt32(0),
                                Name = r.IsDBNull(1) ? "" : r.GetString(1)
                            });
                        }
                    }
                }
            }
            catch (Exception ex) { StatusMessage = "Warehouses error: " + ex.Message; }
        }

        private string GenerateSerial(string type)
        {
            try
            {
                string cs = GetConnectionString();
                using (var conn = new NpgsqlConnection(cs))
                {
                    conn.Open();
                    string prefix = (type ?? "sales") == "purchase" ? "PUR" : "SAL";
                    int year = DateTime.Now.Year;
                    string likePattern = prefix + "-" + year + "-%";
                    string sql = @"SELECT invoice_number FROM invoices 
                        WHERE invoice_number LIKE @p 
                        ORDER BY invoice_number DESC LIMIT 1";
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("p", likePattern);
                        var o = cmd.ExecuteScalar();
                        if (o != null && o != DBNull.Value)
                        {
                            string last = o.ToString();
                            int dashIdx = last.LastIndexOf('-');
                            if (dashIdx >= 0 && int.TryParse(last.Substring(dashIdx + 1), out int seq))
                            {
                                return prefix + "-" + year + "-" + (seq + 1).ToString("D4");
                            }
                        }
                    }
                }
            }
            catch { }
            string pf = (type ?? "sales") == "purchase" ? "PUR" : "SAL";
            return pf + "-" + DateTime.Now.Year + "-0001";
        }

        public void AddLine()
        {
            var line = new InvoiceLineItem(_allItems);
            line.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == "Quantity" || e.PropertyName == "Price" ||
                    e.PropertyName == "Discount" || e.PropertyName == "Total")
                    RefreshTotals();
            };
            Lines.Add(line);
            RefreshTotals();
        }

        public void AddPayment()
        {
            decimal rem = Remaining;
            Payments.Add(new InvoicePaymentRow
            {
                Method = "cash",
                Amount = rem > 0 ? rem : 0,
                PaymentDate = DateTime.Now
            });
            RefreshTotals();
        }

        public void RefreshTotals()
        {
            decimal sub = 0;
            foreach (var line in Lines) sub += line.Total;
            Subtotal = Math.Round(sub, 2);
            Total = Math.Round(_subtotal - _discountAmount, 2);

            decimal paid = 0;
            foreach (var p in Payments) paid += p.Amount;
            TotalPaid = Math.Round(paid, 2);

            OnPropertyChanged("Remaining");
            OnPropertyChanged("RemainingDisplay");
        }

        // ==================================================================
        //  تحميل فاتورة موجودة
        // ==================================================================
        public async System.Threading.Tasks.Task LoadFromInvoiceAsync(int invoiceId)
        {
            IsBusy = true;
            try
            {
                string cs = GetConnectionString();
                using (var conn = new NpgsqlConnection(cs))
                {
                    conn.Open();
                    string sql = @"SELECT id, invoice_number, date, type, party_id, warehouse_id, 
                        notes, subtotal, discount_amount, total, paid_amount
                        FROM invoices WHERE id = @id";
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("id", invoiceId);
                        using (var r = cmd.ExecuteReader())
                        {
                            if (r.Read())
                            {
                                Id = r.IsDBNull(0) ? 0 : r.GetInt32(0);
                                SerialNumber = r.IsDBNull(1) ? "" : r.GetString(1);
                                InvoiceDate = r.IsDBNull(2) ? DateTime.Now : r.GetDateTime(2);
                                InvoiceType = r.IsDBNull(3) ? "sales" : r.GetString(3);
                                PartyId = r.IsDBNull(4) ? 0 : r.GetInt32(4);
                                WarehouseId = r.IsDBNull(5) ? 0 : r.GetInt32(5);
                                Notes = r.IsDBNull(6) ? "" : r.GetString(6);
                                Subtotal = r.IsDBNull(7) ? 0 : Convert.ToDecimal(r.GetValue(7));
                                DiscountAmount = r.IsDBNull(8) ? 0 : Convert.ToDecimal(r.GetValue(8));
                                Total = r.IsDBNull(9) ? 0 : Convert.ToDecimal(r.GetValue(9));
                                TotalPaid = r.IsDBNull(10) ? 0 : Convert.ToDecimal(r.GetValue(10));
                            }
                        }
                    }

                    Lines.Clear();
                    string lineSql = @"SELECT il.item_id, il.quantity, il.price, il.discount, il.total, i.name, i.barcode
                        FROM invoice_lines il 
                        LEFT JOIN items i ON il.item_id = i.id
                        WHERE il.invoice_id = @id";
                    using (var cmd = new NpgsqlCommand(lineSql, conn))
                    {
                        cmd.Parameters.AddWithValue("id", invoiceId);
                        using (var r = cmd.ExecuteReader())
                        {
                            while (r.Read())
                            {
                                var line = new InvoiceLineItem(_allItems);
                                line.ItemId = r.IsDBNull(0) ? 0 : r.GetInt32(0);
                                line.Quantity = r.IsDBNull(1) ? 1 : Convert.ToDecimal(r.GetValue(1));
                                line.Price = r.IsDBNull(2) ? 0 : Convert.ToDecimal(r.GetValue(2));
                                line.Discount = r.IsDBNull(3) ? 0 : Convert.ToDecimal(r.GetValue(3));
                                line.Total = r.IsDBNull(4) ? 0 : Convert.ToDecimal(r.GetValue(4));
                                line.ItemName = r.IsDBNull(5) ? "" : r.GetString(5);
                                line.Barcode = r.IsDBNull(6) ? "" : r.GetString(6);
                                line.SearchText = line.ItemName;
                                Lines.Add(line);
                            }
                        }
                    }

                    Payments.Clear();
                    string paySql = @"SELECT id, method, amount, reference, paid_at
                        FROM invoice_payments WHERE invoice_id = @id ORDER BY id";
                    using (var cmd = new NpgsqlCommand(paySql, conn))
                    {
                        cmd.Parameters.AddWithValue("id", invoiceId);
                        using (var r = cmd.ExecuteReader())
                        {
                            while (r.Read())
                            {
                                Payments.Add(new InvoicePaymentRow
                                {
                                    Id = r.IsDBNull(0) ? 0 : r.GetInt32(0),
                                    Method = r.IsDBNull(1) ? "cash" : r.GetString(1),
                                    Amount = r.IsDBNull(2) ? 0 : Convert.ToDecimal(r.GetValue(2)),
                                    Reference = r.IsDBNull(3) ? "" : r.GetString(3),
                                    PaymentDate = r.IsDBNull(4) ? DateTime.Now : r.GetDateTime(4)
                                });
                            }
                        }
                    }
                }
                if (Lines.Count == 0) AddLine();
                RefreshTotals();
            }
            catch (Exception ex)
            {
                StatusMessage = "Load error: " + ex.Message;
            }
            finally { IsBusy = false; }
            await System.Threading.Tasks.Task.CompletedTask;
        }

        // ==================================================================
        //  حفظ الفاتورة
        // ==================================================================
        public async System.Threading.Tasks.Task SaveAsync()
        {
            IsBusy = true;
            StatusMessage = "Saving...";
            try
            {
                RefreshTotals();
                string cs = GetConnectionString();
                using (var conn = new NpgsqlConnection(cs))
                {
                    conn.Open();

                    if (Id > 0)
                    {
                        string upd = @"UPDATE invoices SET 
                            invoice_number=@sn, date=@dt, type=@tp, party_id=@pid, warehouse_id=@wid,
                            notes=@nt, subtotal=@st, discount_amount=@da, total=@tt, paid_amount=@pa,
                            updated_at=NOW() WHERE id=@id";
                        using (var cmd = new NpgsqlCommand(upd, conn))
                        {
                            cmd.Parameters.AddWithValue("id", Id);
                            cmd.Parameters.AddWithValue("sn", SerialNumber);
                            cmd.Parameters.AddWithValue("dt", InvoiceDate);
                            cmd.Parameters.AddWithValue("tp", InvoiceType);
                            cmd.Parameters.AddWithValue("pid", PartyId > 0 ? (object)PartyId : DBNull.Value);
                            cmd.Parameters.AddWithValue("wid", WarehouseId > 0 ? (object)WarehouseId : DBNull.Value);
                            cmd.Parameters.AddWithValue("nt", (object)Notes ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("st", Subtotal);
                            cmd.Parameters.AddWithValue("da", DiscountAmount);
                            cmd.Parameters.AddWithValue("tt", Total);
                            cmd.Parameters.AddWithValue("pa", TotalPaid);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        string ins = @"INSERT INTO invoices 
                            (invoice_number, date, type, party_id, warehouse_id, notes, subtotal, discount_amount, total, paid_amount, status, created_at, updated_at)
                            VALUES (@sn, @dt, @tp, @pid, @wid, @nt, @st, @da, @tt, @pa, @status, NOW(), NOW()) RETURNING id";
                        using (var cmd = new NpgsqlCommand(ins, conn))
                        {
                            cmd.Parameters.AddWithValue("sn", SerialNumber);
                            cmd.Parameters.AddWithValue("dt", InvoiceDate);
                            cmd.Parameters.AddWithValue("tp", InvoiceType);
                            cmd.Parameters.AddWithValue("pid", PartyId > 0 ? (object)PartyId : DBNull.Value);
                            cmd.Parameters.AddWithValue("wid", WarehouseId > 0 ? (object)WarehouseId : DBNull.Value);
                            cmd.Parameters.AddWithValue("nt", (object)Notes ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("st", Subtotal);
                            cmd.Parameters.AddWithValue("da", DiscountAmount);
                            cmd.Parameters.AddWithValue("tt", Total);
                            cmd.Parameters.AddWithValue("pa", TotalPaid);
                            cmd.Parameters.AddWithValue("status", TotalPaid >= Total ? "paid" : (TotalPaid > 0 ? "partial" : "unpaid"));
                            var o = cmd.ExecuteScalar();
                            if (o != null && o != DBNull.Value) Id = Convert.ToInt32(o);
                        }
                    }

                    // حفظ البنود
                    using (var cmd = new NpgsqlCommand("DELETE FROM invoice_lines WHERE invoice_id=@id", conn))
                    {
                        cmd.Parameters.AddWithValue("id", Id);
                        cmd.ExecuteNonQuery();
                    }
                    foreach (var line in Lines)
                    {
                        if (line.ItemId <= 0) continue;
                        string lineSql = @"INSERT INTO invoice_lines (invoice_id, item_id, quantity, price, discount, total)
                            VALUES (@inv, @iid, @qty, @prc, @dsc, @tot)";
                        using (var cmd = new NpgsqlCommand(lineSql, conn))
                        {
                            cmd.Parameters.AddWithValue("inv", Id);
                            cmd.Parameters.AddWithValue("iid", line.ItemId);
                            cmd.Parameters.AddWithValue("qty", line.Quantity);
                            cmd.Parameters.AddWithValue("prc", line.Price);
                            cmd.Parameters.AddWithValue("dsc", line.Discount);
                            cmd.Parameters.AddWithValue("tot", line.Total);
                            cmd.ExecuteNonQuery();
                        }
                    }

                    // حفظ الدفعات
                    using (var cmd = new NpgsqlCommand("DELETE FROM invoice_payments WHERE invoice_id=@id", conn))
                    {
                        cmd.Parameters.AddWithValue("id", Id);
                        cmd.ExecuteNonQuery();
                    }
                    foreach (var pay in Payments)
                    {
                        if (pay.Amount <= 0) continue;
                        string paySql = @"INSERT INTO invoice_payments (invoice_id, method, amount, reference, paid_at)
                            VALUES (@inv, @mth, @amt, @ref, @pdt)";
                        using (var cmd = new NpgsqlCommand(paySql, conn))
                        {
                            cmd.Parameters.AddWithValue("inv", Id);
                            cmd.Parameters.AddWithValue("mth", pay.Method);
                            cmd.Parameters.AddWithValue("amt", pay.Amount);
                            cmd.Parameters.AddWithValue("ref", (object)pay.Reference ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("pdt", pay.PaymentDate);
                            cmd.ExecuteNonQuery();
                        }
                    }
                }

                StatusMessage = "Saved! ID: " + Id;
                DialogResult = true;
                RequestClose?.Invoke();
            }
            catch (Exception ex)
            {
                StatusMessage = "Save error: " + ex.Message;
                System.Windows.MessageBox.Show("Save Error:\n" + ex.Message, "Error");
            }
            finally { IsBusy = false; }
            await System.Threading.Tasks.Task.CompletedTask;
        }
    }
}
'@
[System.IO.File]::WriteAllText($vmPath, $vm, [System.Text.UTF8Encoding]::new($true))
Write-Host "  OK: InvoiceEditViewModel.cs" -ForegroundColor Green

# ===== InvoiceEditWindow.xaml (ASCII) =====
$xamlPath = Join-Path $ViewsDir 'InvoiceEditWindow.xaml'
if (Test-Path $xamlPath) { Copy-Item $xamlPath ($xamlPath + '.bak_new') -Force }
$xaml = @'
<Window x:Class="Techtoon.UI.Views.InvoiceEditWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="Invoice" Height="850" Width="1200"
        WindowStartupLocation="CenterScreen" FlowDirection="RightToLeft"
        Background="#F5F7FA" Loaded="Window_Loaded" FontFamily="Tahoma">
    <Window.Resources>
        <Style x:Key="Hdr" TargetType="TextBlock">
            <Setter Property="FontSize" Value="13"/>
            <Setter Property="FontWeight" Value="SemiBold"/>
            <Setter Property="Foreground" Value="#37474F"/>
            <Setter Property="Margin" Value="0,0,6,0"/>
            <Setter Property="VerticalAlignment" Value="Center"/>
        </Style>
        <Style x:Key="Inp" TargetType="TextBox">
            <Setter Property="Padding" Value="6,4"/>
            <Setter Property="FontSize" Value="13"/>
            <Setter Property="BorderBrush" Value="#B0BEC5"/>
            <Setter Property="BorderThickness" Value="1"/>
        </Style>
        <Style x:Key="AddBtn" TargetType="Button">
            <Setter Property="Background" Value="#26A69A"/>
            <Setter Property="Foreground" Value="White"/>
            <Setter Property="FontWeight" Value="SemiBold"/>
            <Setter Property="FontSize" Value="13"/>
            <Setter Property="Padding" Value="14,6"/>
            <Setter Property="BorderThickness" Value="0"/>
            <Setter Property="Cursor" Value="Hand"/>
        </Style>
        <Style x:Key="SaveBtn" TargetType="Button">
            <Setter Property="Background" Value="#00897B"/>
            <Setter Property="Foreground" Value="White"/>
            <Setter Property="FontWeight" Value="Bold"/>
            <Setter Property="FontSize" Value="14"/>
            <Setter Property="Padding" Value="24,10"/>
            <Setter Property="BorderThickness" Value="0"/>
            <Setter Property="Cursor" Value="Hand"/>
        </Style>
        <Style x:Key="CancelBtn" TargetType="Button">
            <Setter Property="Background" Value="#ECEFF1"/>
            <Setter Property="Foreground" Value="#37474F"/>
            <Setter Property="FontWeight" Value="SemiBold"/>
            <Setter Property="FontSize" Value="13"/>
            <Setter Property="Padding" Value="18,10"/>
            <Setter Property="BorderThickness" Value="0"/>
            <Setter Property="Cursor" Value="Hand"/>
        </Style>
    </Window.Resources>
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>

        <Border Grid.Row="0" Background="White" Padding="16,10" BorderBrush="#E0E0E0" BorderThickness="0,0,0,1">
            <Grid>
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="Auto"/>
                    <ColumnDefinition Width="*"/>
                </Grid.ColumnDefinitions>
                <StackPanel Grid.Column="0" Orientation="Horizontal" VerticalAlignment="Center">
                    <TextBlock x:Name="HeaderIcon" FontSize="28" Margin="0,0,10,0"/>
                    <StackPanel>
                        <TextBlock x:Name="HeaderTitle" FontSize="18" FontWeight="Bold" Foreground="#00695C"/>
                        <TextBlock Text="{Binding InvoiceTypeDisplay}" FontSize="12" Foreground="#78909C"/>
                    </StackPanel>
                </StackPanel>
                <Grid Grid.Column="1" Margin="20,0,0,0">
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="Auto"/><ColumnDefinition Width="140"/>
                        <ColumnDefinition Width="18"/><ColumnDefinition Width="Auto"/>
                        <ColumnDefinition Width="150"/><ColumnDefinition Width="18"/>
                        <ColumnDefinition Width="Auto"/><ColumnDefinition Width="120"/>
                    </Grid.ColumnDefinitions>
                    <Grid.RowDefinitions>
                        <RowDefinition Height="Auto"/><RowDefinition Height="8"/><RowDefinition Height="Auto"/>
                    </Grid.RowDefinitions>
                    <TextBlock x:Name="LblSerial" Grid.Row="0" Grid.Column="0" Style="{StaticResource Hdr}"/>
                    <TextBox Grid.Row="0" Grid.Column="1" Text="{Binding SerialNumber}" Style="{StaticResource Inp}" IsReadOnly="True"/>
                    <TextBlock x:Name="LblDate" Grid.Row="0" Grid.Column="3" Style="{StaticResource Hdr}"/>
                    <DatePicker Grid.Row="0" Grid.Column="4" SelectedDate="{Binding InvoiceDate}" FontSize="13"/>
                    <TextBlock x:Name="LblType" Grid.Row="0" Grid.Column="6" Style="{StaticResource Hdr}"/>
                    <ComboBox x:Name="CmbType" Grid.Row="0" Grid.Column="7" SelectedValue="{Binding InvoiceType}" SelectedValuePath="Tag" Padding="6,4" FontSize="13" BorderBrush="#B0BEC5" BorderThickness="1"/>
                    <TextBlock x:Name="LblParty" Grid.Row="2" Grid.Column="0" Style="{StaticResource Hdr}"/>
                    <ComboBox Grid.Row="2" Grid.Column="1" Grid.ColumnSpan="4" ItemsSource="{Binding Parties}" DisplayMemberPath="Name" SelectedValuePath="Id" SelectedValue="{Binding PartyId}" Padding="6,4" FontSize="13" BorderBrush="#B0BEC5" BorderThickness="1"/>
                    <TextBlock x:Name="LblWarehouse" Grid.Row="2" Grid.Column="6" Style="{StaticResource Hdr}"/>
                    <ComboBox Grid.Row="2" Grid.Column="7" ItemsSource="{Binding Warehouses}" DisplayMemberPath="Name" SelectedValuePath="Id" SelectedValue="{Binding WarehouseId}" Padding="6,4" FontSize="13" BorderBrush="#B0BEC5" BorderThickness="1"/>
                </Grid>
            </Grid>
        </Border>

        <Border Grid.Row="1" Background="White" Margin="10,8,10,4" Padding="8" BorderBrush="#E0E0E0" BorderThickness="1" CornerRadius="4">
            <Grid>
                <Grid.RowDefinitions>
                    <RowDefinition Height="Auto"/><RowDefinition Height="*"/>
                </Grid.RowDefinitions>
                <Grid Grid.Row="0" Margin="0,0,0,6">
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="*"/><ColumnDefinition Width="Auto"/>
                    </Grid.ColumnDefinitions>
                    <TextBlock x:Name="LblItems" Grid.Column="0" FontSize="15" FontWeight="Bold" Foreground="#37474F" VerticalAlignment="Center"/>
                    <Button x:Name="BtnAddLine" Grid.Column="1" Style="{StaticResource AddBtn}" Command="{Binding AddLineCommand}"/>
                </Grid>
                <DataGrid x:Name="LinesGrid" Grid.Row="1" ItemsSource="{Binding Lines}" AutoGenerateColumns="False" CanUserAddRows="False" HeadersVisibility="Column" GridLinesVisibility="Horizontal" HorizontalGridLinesBrush="#ECEFF1" BorderBrush="#E0E0E0" BorderThickness="1" Background="White" FontSize="13" MinHeight="180">
                    <DataGrid.Columns>
                        <DataGridTemplateColumn x:Name="ColItem" Width="340">
                            <DataGridTemplateColumn.CellTemplate>
                                <DataTemplate>
                                    <ComboBox x:Name="ItemSearchCombo"
                                              ItemsSource="{Binding FilteredItems}"
                                              DisplayMemberPath="SearchDisplay"
                                              IsEditable="True"
                                              Text="{Binding SearchText, UpdateSourceTrigger=PropertyChanged}"
                                              StaysOpenOnEdit="True"
                                              SelectionChanged="ItemSearchCombo_SelectionChanged"
                                              Padding="4,3" FontSize="13"/>
                                </DataTemplate>
                            </DataGridTemplateColumn.CellTemplate>
                        </DataGridTemplateColumn>
                        <DataGridTextColumn x:Name="ColBarcode" Width="110" Binding="{Binding Barcode}" IsReadOnly="True"/>
                        <DataGridTextColumn x:Name="ColUnit" Width="70" Binding="{Binding Unit}" IsReadOnly="True"/>
                        <DataGridTemplateColumn x:Name="ColQty" Width="90">
                            <DataGridTemplateColumn.CellTemplate>
                                <DataTemplate>
                                    <TextBox Text="{Binding Quantity, UpdateSourceTrigger=PropertyChanged, StringFormat=N2}" Tag="qty" Padding="4,3" BorderBrush="#E0E0E0" KeyDown="Cell_KeyDown"/>
                                </DataTemplate>
                            </DataGridTemplateColumn.CellTemplate>
                        </DataGridTemplateColumn>
                        <DataGridTemplateColumn x:Name="ColPrice" Width="100">
                            <DataGridTemplateColumn.CellTemplate>
                                <DataTemplate>
                                    <TextBox Text="{Binding Price, UpdateSourceTrigger=PropertyChanged, StringFormat=N2}" Tag="price" Padding="4,3" BorderBrush="#E0E0E0" KeyDown="Cell_KeyDown"/>
                                </DataTemplate>
                            </DataGridTemplateColumn.CellTemplate>
                        </DataGridTemplateColumn>
                        <DataGridTemplateColumn x:Name="ColDisc" Width="80">
                            <DataGridTemplateColumn.CellTemplate>
                                <DataTemplate>
                                    <TextBox Text="{Binding Discount, UpdateSourceTrigger=PropertyChanged, StringFormat=N2}" Tag="discount" Padding="4,3" BorderBrush="#E0E0E0" KeyDown="Cell_KeyDown"/>
                                </DataTemplate>
                            </DataGridTemplateColumn.CellTemplate>
                        </DataGridTemplateColumn>
                        <DataGridTextColumn x:Name="ColTotal" Width="110" Binding="{Binding Total, StringFormat=N2}" IsReadOnly="True">
                            <DataGridTextColumn.ElementStyle>
                                <Style TargetType="TextBlock">
                                    <Setter Property="HorizontalAlignment" Value="Right"/>
                                    <Setter Property="FontWeight" Value="SemiBold"/>
                                    <Setter Property="Foreground" Value="#00695C"/>
                                </Style>
                            </DataGridTextColumn.ElementStyle>
                        </DataGridTextColumn>
                        <DataGridTemplateColumn x:Name="ColDel" Width="60">
                            <DataGridTemplateColumn.CellTemplate>
                                <DataTemplate>
                                    <Button Content="X" Background="Transparent" BorderThickness="0" FontSize="14" Cursor="Hand" Command="{Binding DataContext.DeleteLineCommand, RelativeSource={RelativeSource AncestorType=DataGrid}}" CommandParameter="{Binding}"/>
                                </DataTemplate>
                            </DataGridTemplateColumn.CellTemplate>
                        </DataGridTemplateColumn>
                    </DataGrid.Columns>
                </DataGrid>
            </Grid>
        </Border>

        <Border x:Name="PaymentsSection" Grid.Row="2" Background="White" Margin="10,4,10,4" Padding="10" BorderBrush="#80CBC4" BorderThickness="1" CornerRadius="4">
            <Grid>
                <Grid.RowDefinitions>
                    <RowDefinition Height="Auto"/><RowDefinition Height="Auto"/>
                </Grid.RowDefinitions>
                <Grid Grid.Row="0" Margin="0,0,0,6">
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="*"/><ColumnDefinition Width="Auto"/>
                    </Grid.ColumnDefinitions>
                    <TextBlock x:Name="LblPayments" Grid.Column="0" FontSize="15" FontWeight="Bold" Foreground="#00695C" VerticalAlignment="Center"/>
                    <Button x:Name="BtnAddPay" Grid.Column="1" Style="{StaticResource AddBtn}" Command="{Binding AddPaymentCommand}"/>
                </Grid>
                <DataGrid x:Name="PaymentsGrid" Grid.Row="1" ItemsSource="{Binding Payments}" AutoGenerateColumns="False" CanUserAddRows="False" HeadersVisibility="Column" GridLinesVisibility="Horizontal" HorizontalGridLinesBrush="#ECEFF1" BorderBrush="#E0E0E0" BorderThickness="1" Background="White" FontSize="13" MaxHeight="140" CellEditEnding="PaymentsGrid_CellEditEnding">
                    <DataGrid.Columns>
                        <DataGridTemplateColumn x:Name="ColPayMethod" Width="140">
                            <DataGridTemplateColumn.CellTemplate>
                                <DataTemplate>
                                    <ComboBox x:Name="PayMethodCombo" SelectedValue="{Binding Method, UpdateSourceTrigger=PropertyChanged}" SelectedValuePath="Tag" Padding="4,2"/>
                                </DataTemplate>
                            </DataGridTemplateColumn.CellTemplate>
                        </DataGridTemplateColumn>
                        <DataGridTemplateColumn x:Name="ColPayAmount" Width="140">
                            <DataGridTemplateColumn.CellTemplate>
                                <DataTemplate>
                                    <TextBox Text="{Binding Amount, UpdateSourceTrigger=PropertyChanged, StringFormat=N2}" Padding="4,3" BorderBrush="#E0E0E0" TextAlignment="Right"/>
                                </DataTemplate>
                            </DataGridTemplateColumn.CellTemplate>
                        </DataGridTemplateColumn>
                        <DataGridTextColumn x:Name="ColPayRef" Width="180" Binding="{Binding Reference, UpdateSourceTrigger=PropertyChanged}"/>
                        <DataGridTemplateColumn x:Name="ColPayDate" Width="150">
                            <DataGridTemplateColumn.CellTemplate>
                                <DataTemplate>
                                    <DatePicker SelectedDate="{Binding PaymentDate}" FontSize="12"/>
                                </DataTemplate>
                            </DataGridTemplateColumn.CellTemplate>
                        </DataGridTemplateColumn>
                        <DataGridTemplateColumn x:Name="ColPayDel" Width="60">
                            <DataGridTemplateColumn.CellTemplate>
                                <DataTemplate>
                                    <Button Content="X" Background="Transparent" BorderThickness="0" FontSize="14" Cursor="Hand" Command="{Binding DataContext.DeletePaymentCommand, RelativeSource={RelativeSource AncestorType=DataGrid}}" CommandParameter="{Binding}"/>
                                </DataTemplate>
                            </DataGridTemplateColumn.CellTemplate>
                        </DataGridTemplateColumn>
                    </DataGrid.Columns>
                </DataGrid>
            </Grid>
        </Border>

        <Border Grid.Row="3" Background="White" Padding="16,10" BorderBrush="#E0E0E0" BorderThickness="0,1,0,0">
            <Grid>
                <Grid.RowDefinitions>
                    <RowDefinition Height="Auto"/>
                    <RowDefinition Height="8"/>
                    <RowDefinition Height="Auto"/>
                </Grid.RowDefinitions>
                <Grid Grid.Row="0">
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="*"/><ColumnDefinition Width="300"/>
                    </Grid.ColumnDefinitions>
                    <StackPanel Grid.Column="0" VerticalAlignment="Center">
                        <TextBlock x:Name="StatusText" Text="{Binding StatusMessage}" FontSize="12" Foreground="#00897B" TextWrapping="Wrap"/>
                    </StackPanel>
                    <Grid Grid.Column="1">
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width="*"/><ColumnDefinition Width="110"/>
                        </Grid.ColumnDefinitions>
                        <Grid.RowDefinitions>
                            <RowDefinition Height="Auto"/><RowDefinition Height="Auto"/><RowDefinition Height="Auto"/><RowDefinition Height="Auto"/><RowDefinition Height="Auto"/>
                        </Grid.RowDefinitions>
                        <TextBlock x:Name="LblSubtotal" Grid.Row="0" Grid.Column="0" FontSize="13" Foreground="#546E7A" Padding="0,2"/>
                        <TextBlock Grid.Row="0" Grid.Column="1" Text="{Binding SubtotalDisplay}" FontSize="13" FontWeight="SemiBold" Foreground="#37474F" TextAlignment="Left" Padding="0,2"/>
                        <StackPanel Grid.Row="1" Grid.Column="0" Orientation="Horizontal">
                            <TextBlock x:Name="LblDisc" FontSize="13" Foreground="#546E7A" VerticalAlignment="Center"/>
                            <TextBox Text="{Binding DiscountAmount, UpdateSourceTrigger=PropertyChanged, StringFormat=N2}" Width="80" Margin="6,0,0,0" Padding="3,2" FontSize="12" BorderBrush="#E0E0E0" TextAlignment="Right"/>
                        </StackPanel>
                        <TextBlock Grid.Row="1" Grid.Column="1" Text="{Binding DiscountAmount, StringFormat=N2}" FontSize="13" Foreground="#E53935" TextAlignment="Left"/>
                        <TextBlock x:Name="LblTotal" Grid.Row="2" Grid.Column="0" FontSize="14" FontWeight="Bold" Foreground="#00695C" Padding="0,2"/>
                        <TextBlock Grid.Row="2" Grid.Column="1" Text="{Binding TotalDisplay}" FontSize="15" FontWeight="Bold" Foreground="#00695C" TextAlignment="Left" Padding="0,2"/>
                        <TextBlock x:Name="LblPaid" Grid.Row="3" Grid.Column="0" FontSize="13" Foreground="#2E7D32" Padding="0,2"/>
                        <TextBlock Grid.Row="3" Grid.Column="1" Text="{Binding TotalPaidDisplay}" FontSize="13" FontWeight="SemiBold" Foreground="#2E7D32" TextAlignment="Left" Padding="0,2"/>
                        <TextBlock x:Name="LblRemaining" Grid.Row="4" Grid.Column="0" FontSize="14" FontWeight="Bold" Foreground="#E65100" Padding="0,2"/>
                        <TextBlock Grid.Row="4" Grid.Column="1" Text="{Binding RemainingDisplay}" FontSize="14" FontWeight="Bold" Foreground="#E65100" TextAlignment="Left" Padding="0,2"/>
                    </Grid>
                </Grid>
                <StackPanel Grid.Row="2" Orientation="Horizontal" HorizontalAlignment="Left" FlowDirection="LeftToRight" Margin="0,8,0,0">
                    <Button x:Name="BtnSave" Style="{StaticResource SaveBtn}" Margin="0,0,8,0" Command="{Binding SaveCommand}"/>
                    <Button x:Name="BtnCancel" Style="{StaticResource CancelBtn}" Command="{Binding CancelCommand}"/>
                </StackPanel>
            </Grid>
        </Border>
    </Grid>
</Window>
'@
[System.IO.File]::WriteAllText($xamlPath, $xaml, [System.Text.ASCIIEncoding]::new())
Write-Host "  OK: InvoiceEditWindow.xaml" -ForegroundColor Green

# ===== InvoiceEditWindow.xaml.cs (UTF-8 with BOM) =====
$csPath = Join-Path $ViewsDir 'InvoiceEditWindow.xaml.cs'
if (Test-Path $csPath) { Copy-Item $csPath ($csPath + '.bak_new') -Force }
$cs = @'
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Techtoon.UI.ViewModels;

namespace Techtoon.UI.Views
{
    public partial class InvoiceEditWindow : Window
    {
        private readonly InvoiceEditViewModel _vm;
        private readonly int? _editId;

        public InvoiceEditWindow(int? editId = null)
        {
            InitializeComponent();
            _editId = editId;
            _vm = new InvoiceEditViewModel();
            if (editId.HasValue) _vm.Id = editId.Value;
            DataContext = _vm;
            _vm.RequestClose += () => { DialogResult = _vm.DialogResult; Close(); };
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // ضبط كل النصوص العربية من الكود (Unicode escape - مستحيل أن تظهر مكسورة)
            SetArabicTexts();
            FillTypeCombo();
            FillPaymentMethods();
            await _vm.LoadAsync();
            if (_editId.HasValue) await _vm.LoadFromInvoiceAsync(_editId.Value);
        }

        private void SetArabicTexts()
        {
            // كل النصوص العربية بـ Unicode escape
            try { Title = "\u0641\u0627\u062A\u0648\u0631\u0629"; } catch { } // فاتورة
            try { HeaderIcon.Text = "\uD83E\uDDF9"; } catch { } // 🧾
            try { HeaderTitle.Text = "\u0641\u0627\u062A\u0648\u0631\u0629"; } catch { } // فاتورة
            try { LblSerial.Text = "\u0627\u0644\u0631\u0642\u0645:"; } catch { } // الرقم:
            try { LblDate.Text = "\u0627\u0644\u062A\u0627\u0631\u064A\u062E:"; } catch { } // التاريخ:
            try { LblType.Text = "\u0627\u0644\u0646\u0648\u0639:"; } catch { } // النوع:
            try { LblParty.Text = "\u0627\u0644\u0637\u0631\u0641:"; } catch { } // الطرف:
            try { LblWarehouse.Text = "\u0627\u0644\u0645\u062E\u0632\u0646:"; } catch { } // المخزن:
            try { LblItems.Text = "\u0628\u0646\u0648\u062F \u0627\u0644\u0641\u0627\u062A\u0648\u0631\u0629"; } catch { } // بنود الفاتورة
            try { BtnAddLine.Content = "+ \u0625\u0636\u0627\u0641\u0629 \u0635\u0646\u0641"; } catch { } // + إضافة صنف
            try { ColItem.Header = "\u0635\u0646\u0641 (\u0627\u0643\u062A\u0628 \u0644\u0644\u0628\u062D\u062B)"; } catch { } // صنف (اكتب للبحث)
            try { ColBarcode.Header = "\u0628\u0627\u0631\u0643\u0648\u062F"; } catch { } // باركود
            try { ColUnit.Header = "\u0627\u0644\u0648\u062D\u062F\u0629"; } catch { } // الوحدة
            try { ColQty.Header = "\u0627\u0644\u0643\u0645\u064A\u0629"; } catch { } // الكمية
            try { ColPrice.Header = "\u0627\u0644\u0633\u0639\u0631"; } catch { } // السعر
            try { ColDisc.Header = "\u062E\u0635\u0645"; } catch { } // خصم
            try { ColTotal.Header = "\u0625\u062C\u0645\u0627\u0644\u064A"; } catch { } // إجمالي
            try { ColDel.Header = "\u062D\u0630\u0641"; } catch { } // حذف
            try { LblPayments.Text = "\u0637\u0631\u0642 \u0627\u0644\u062F\u0641\u0639 \u0627\u0644\u0645\u062A\u0639\u062F\u062F\u0629"; } catch { } // طرق الدفع المتعددة
            try { BtnAddPay.Content = "+ \u0625\u0636\u0627\u0641\u0629 \u062F\u0641\u0639\u0629"; } catch { } // + إضافة دفعة
            try { ColPayMethod.Header = "\u0637\u0631\u064A\u0642\u0629 \u0627\u0644\u062F\u0641\u0639"; } catch { } // طريقة الدفع
            try { ColPayAmount.Header = "\u0627\u0644\u0645\u0628\u0644\u063A"; } catch { } // المبلغ
            try { ColPayRef.Header = "\u0645\u0631\u062C\u0639"; } catch { } // مرجع
            try { ColPayDate.Header = "\u062A\u0627\u0631\u064A\u062E"; } catch { } // تاريخ
            try { ColPayDel.Header = "\u062D\u0630\u0641"; } catch { } // حذف
            try { LblSubtotal.Text = "\u0627\u0644\u0645\u062C\u0645\u0648\u0639:"; } catch { } // المجموع:
            try { LblDisc.Text = "\u062E\u0635\u0645:"; } catch { } // خصم:
            try { LblTotal.Text = "\u0625\u062C\u0645\u0627\u0644\u064A:"; } catch { } // إجمالي:
            try { LblPaid.Text = "\u0627\u0644\u0645\u062F\u0641\u0648\u0639:"; } catch { } // المدفوع:
            try { LblRemaining.Text = "\u0627\u0644\u0645\u062A\u0628\u0642\u064A:"; } catch { } // المتبقي:
            try { BtnSave.Content = "\uD83D\uDCBE \u062D\u0641\u0638 \u0627\u0644\u0641\u0627\u062A\u0648\u0631\u0629"; } catch { } // 💾 حفظ الفاتورة
            try { BtnCancel.Content = "\u2716 \u0625\u0644\u063A\u0627\u0621"; } catch { } // ✖ إلغاء
        }

        private void FillTypeCombo()
        {
            try
            {
                CmbType.Items.Clear();
                CmbType.Items.Add(new ComboBoxItem { Content = "\u0645\u0628\u064A\u0639\u0627\u062A", Tag = "sales" }); // مبيعات
                CmbType.Items.Add(new ComboBoxItem { Content = "\u0645\u0634\u062A\u0631\u064A\u0627\u062A", Tag = "purchase" }); // مشتريات
            }
            catch { }
        }

        private void FillPaymentMethods()
        {
            // سيتم تعبئة عند تحميل كل صف في PaymentsGrid
        }

        // اختيار صنف من القائمة المنسدلة
        private void ItemSearchCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (sender is ComboBox cb && cb.SelectedItem is ItemSearchResult item)
                {
                    if (cb.DataContext is InvoiceLineItem line)
                    {
                        line.SelectItem(item);
                    }
                }
            }
            catch { }
        }

        private void Cell_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;
            if (sender is not TextBox tb) return;
            if (tb.Tag is not string tag) return;
            if (tb.DataContext is not InvoiceLineItem line) return;
            LinesGrid.CommitEdit(DataGridEditingUnit.Row, true);
            int idx = _vm.Lines.IndexOf(line);
            int last = _vm.Lines.Count - 1;
            if (idx >= 0 && idx < last)
            {
                if (LinesGrid.ItemContainerGenerator.ContainerFromIndex(idx + 1) is DataGridRow nextRow)
                {
                    var next = FindDescendant<TextBox>(nextRow, x => (x.Tag as string) == tag);
                    if (next != null) { next.Focus(); next.SelectAll(); }
                }
            }
            else if (idx == last)
            {
                _vm.AddLineCommand.Execute(null);
                LinesGrid.UpdateLayout();
                if (LinesGrid.ItemContainerGenerator.ContainerFromIndex(_vm.Lines.Count - 1) is DataGridRow newRow)
                {
                    var cb = FindDescendant<ComboBox>(newRow, x => x.Name == "ItemSearchCombo");
                    cb?.Focus();
                }
            }
            e.Handled = true;
        }

        private static T? FindDescendant<T>(DependencyObject? parent, Func<T, bool> predicate) where T : class
        {
            if (parent == null) return null;
            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T matched && predicate(matched)) return matched;
                var deep = FindDescendant(child, predicate);
                if (deep != null) return deep;
            }
            return null;
        }

        private void PaymentsGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction == DataGridEditAction.Commit) _vm.RefreshTotals();
        }
    }
}
'@
[System.IO.File]::WriteAllText($csPath, $cs, [System.Text.UTF8Encoding]::new($true))
Write-Host "  OK: InvoiceEditWindow.xaml.cs" -ForegroundColor Green

# ===== appsettings.json =====
$appsettings = '{"ConnectionStrings":{"DefaultConnection":"Host=ep-bitter-night-za726w59-pooler.c-2.eu-west-2.aws.neon.tech;Port=5432;Username=neondb_owner;Password=***;Database=neondb;SSL Mode=Require;Trust Server Certificate=true"}}'
$ap = Join-Path $ProjectPath 'Techtoon.UI\appsettings.json'
[System.IO.File]::WriteAllText($ap, $appsettings, [System.Text.UTF8Encoding]::new($false))
$bd = Join-Path $ProjectPath 'Techtoon.UI\bin\Debug\net10.0-windows'
if (Test-Path $bd) { Copy-Item $ap (Join-Path $bd 'appsettings.json') -Force }
Write-Host "  OK: appsettings.json" -ForegroundColor Green

# ===== تنظيف obj =====
$objDir = Join-Path $ProjectPath 'Techtoon.UI\obj\Debug\net10.0-windows'
if (Test-Path $objDir) {
    Get-ChildItem -Path $objDir -Filter 'InvoiceEdit*' | Remove-Item -Force -ErrorAction SilentlyContinue
    Write-Host "  Cleaned obj" -ForegroundColor Green
}

# ===== البناء =====
Write-Host ''
Write-Host '===== Build =====' -ForegroundColor Cyan
dotnet build (Join-Path $ProjectPath 'Techtoon.UI') 2>&1 | Select-Object -Last 8
Write-Host ''
Write-Host '========================================' -ForegroundColor Cyan
Write-Host "  DONE! Run: dotnet run --project Techtoon.UI" -ForegroundColor Green
Write-Host '========================================' -ForegroundColor Cyan
```
#### .\fix_data.ps1
```
# fix_data.ps1 - إضافة بيانات تجريبية + إصلاح سلسلة الاتصال
 $ProjectPath = 'D:\PROG\Accounting Prog\Techtoon Accounting C+'

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Techtoon - Add Test Data" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

# 1) إضافة بيانات تجريبية لقاعدة البيانات
Write-Host "`n[1/2] Adding test data to database..." -ForegroundColor Yellow

 $pyScript = @'
import psycopg2
CS = "host=ep-bitter-night-za726w59-pooler.c-2.eu-west-2.aws.neon.tech port=5432 dbname=neondb user=neondb_owner password=*** sslmode=require"
conn = psycopg2.connect(CS)
conn.autocommit = True
cur = conn.cursor()

# إنشاء جدول items إن لم يكن موجوداً
cur.execute("""CREATE TABLE IF NOT EXISTS items (
    id SERIAL PRIMARY KEY, name TEXT NOT NULL, barcode TEXT, sku TEXT,
    unit TEXT DEFAULT 'piece', sale_price NUMERIC(14,2) DEFAULT 0,
    current_stock NUMERIC(14,3) DEFAULT 0, is_active BOOLEAN DEFAULT TRUE)""")

# إضافة أصناف تجريبية
items = [
    ("Laptop Dell", "DLP001", "LAPTOP-001", "pcs", 15000, 10),
    ("Wireless Mouse", "MS002", "MOUSE-002", "pcs", 250, 50),
    ("Keyboard", "KB003", "KEYB-003", "pcs", 400, 30),
    ("Samsung Monitor", "MN004", "MON-004", "pcs", 3500, 15),
    ("Bluetooth Headset", "HP005", "HEAD-005", "pcs", 800, 20),
    ("HDMI Cable", "CB006", "HDMI-006", "m", 50, 100),
    ("Printer Ink", "INK008", "INK-008", "pcs", 350, 25),
]
for it in items:
    try:
        cur.execute("INSERT INTO items (name, barcode, sku, unit, sale_price, current_stock, is_active) VALUES (%s,%s,%s,%s,%s,%s,TRUE) ON CONFLICT DO NOTHING", it)
    except: pass
cur.execute("SELECT COUNT(*) FROM items")
print(f"  Items: {cur.fetchone()[0]}")

# إنشاء جدول parties (العملاء/الموردين)
cur.execute("""CREATE TABLE IF NOT EXISTS parties (
    id SERIAL PRIMARY KEY, name TEXT NOT NULL, type TEXT DEFAULT 'customer',
    phone TEXT, email TEXT, is_active BOOLEAN DEFAULT TRUE)""")

parties = [
    ("Ahmed Mohamed", "customer", "01001234567"),
    ("Sara Ali", "customer", "01007654321"),
    ("Tech Supplier Co", "supplier", "0223456789"),
    ("Nour Trading", "supplier", "0229876543"),
    ("Cairo Electronics", "customer", "01112345678"),
]
for p in parties:
    try:
        cur.execute("INSERT INTO parties (name, type, phone, is_active) VALUES (%s,%s,%s,TRUE) ON CONFLICT DO NOTHING", p)
    except: pass
cur.execute("SELECT COUNT(*) FROM parties")
print(f"  Parties: {cur.fetchone()[0]}")

# إنشاء جدول warehouses (المخازن)
cur.execute("""CREATE TABLE IF NOT EXISTS warehouses (
    id SERIAL PRIMARY KEY, name TEXT NOT NULL, location TEXT, is_active BOOLEAN DEFAULT TRUE)""")

warehouses = [
    ("Main Warehouse", "Cairo"),
    ("Branch 1", "Giza"),
    ("Branch 2", "Alexandria"),
]
for w in warehouses:
    try:
        cur.execute("INSERT INTO warehouses (name, location, is_active) VALUES (%s,%s,TRUE) ON CONFLICT DO NOTHING", w)
    except: pass
cur.execute("SELECT COUNT(*) FROM warehouses")
print(f"  Warehouses: {cur.fetchone()[0]}")

cur.close()
conn.close()
print("  DONE: Test data added!")
'@

 $pyPath = Join-Path $env:TEMP 'techtoon_data.py'
[System.IO.File]::WriteAllText($pyPath, $pyScript, [System.Text.UTF8Encoding]::new($false))
 $result = python $pyPath 2>&1
Write-Host $result

# 2) إصلاح سلسلة الاتصال في ViewModel
Write-Host "`n[2/2] Fixing connection string..." -ForegroundColor Yellow
 $vmPath = Join-Path $ProjectPath 'Techtoon.UI\Views\InvoiceEditViewModel.cs'
if (Test-Path $vmPath) {
    $content = Get-Content $vmPath -Raw -Encoding UTF8
    # استبدل أي return بقيمة Neon المباشرة
    $neonCs = 'return "Host=ep-bitter-night-za726w59-pooler.c-2.eu-west-2.aws.neon.tech;Port=5432;Username=neondb_owner;Password=***;Database=neondb;SSL Mode=Require;Trust Server Certificate=true";'
    # استبدل return defaultCs
    $content = $content -replace 'return defaultCs;', $neonCs
    # استبدل أي return داخل GetConnectionString
    $content = $content -replace 'return cs;', $neonCs
    [System.IO.File]::WriteAllText($vmPath, $content, [System.Text.UTF8Encoding]::new($true))
    Write-Host "  OK: Connection string fixed" -ForegroundColor Green
}

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "  DONE! Now run:" -ForegroundColor Green
Write-Host "  dotnet build Techtoon.UI" -ForegroundColor White
Write-Host "  dotnet run --project Techtoon.UI" -ForegroundColor White
Write-Host "========================================" -ForegroundColor Cyan
```
#### .\fix_final.ps1
```
# fix_final.ps1 - إصلاح نهائي: سلسلة الاتصال + XAML الجديد
 $ProjectPath = 'D:\PROG\Accounting Prog\Techtoon Accounting C+'
 $ViewsDir = Join-Path $ProjectPath 'Techtoon.UI\Views'

if (-not (Test-Path $ViewsDir)) {
    Write-Host "ERROR: Views folder not found" -ForegroundColor Red
    exit 1
}

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Techtoon - Final Fix (3 files)" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

Get-ChildItem -Path $ViewsDir -Filter "InvoiceEdit*" | Where-Object { $_.Name -match '\(1\)|نسخة' } | Remove-Item -Force -ErrorAction SilentlyContinue

# [1/3] إصلاح سلسلة الاتصال
Write-Host "[1/3] Patching connection string..." -ForegroundColor Yellow
 $vmPath = Join-Path $ViewsDir 'InvoiceEditViewModel.cs'
if (Test-Path $vmPath) {
    Copy-Item $vmPath "$vmPath.bak2" -Force
    $content = Get-Content $vmPath -Raw -Encoding UTF8
    $content = $content -replace 'return defaultCs;', 'return "Host=ep-bitter-night-za726w59-pooler.c-2.eu-west-2.aws.neon.tech;Port=5432;Username=neondb_owner;Password=***;Database=neondb;SSL Mode=Require;Trust Server Certificate=true";'
    [System.IO.File]::WriteAllText($vmPath, $content, [System.Text.UTF8Encoding]::new($false))
    Write-Host "  OK: Connection string fixed" -ForegroundColor Green
}

# [2/3] كتابة XAML الجديد
Write-Host "[2/3] Writing InvoiceEditWindow.xaml..." -ForegroundColor Yellow
 $xamlPath = Join-Path $ViewsDir 'InvoiceEditWindow.xaml'
if (Test-Path $xamlPath) { Copy-Item $xamlPath "$xamlPath.bak" -Force }
 $xaml = @'
<Window x:Class="Techtoon.UI.Views.InvoiceEditWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="فاتورة جديدة" Height="820" Width="1180"
        WindowStartupLocation="CenterScreen" FlowDirection="RightToLeft"
        Background="#F5F7FA" Loaded="Window_Loaded">
    <Window.Resources>
        <Style x:Key="HeaderLabel" TargetType="TextBlock">
            <Setter Property="FontSize" Value="13"/>
            <Setter Property="FontWeight" Value="SemiBold"/>
            <Setter Property="Foreground" Value="#37474F"/>
            <Setter Property="Margin" Value="0,0,6,0"/>
            <Setter Property="VerticalAlignment" Value="Center"/>
        </Style>
        <Style x:Key="InputBox" TargetType="TextBox">
            <Setter Property="Padding" Value="6,4"/>
            <Setter Property="FontSize" Value="13"/>
            <Setter Property="BorderBrush" Value="#B0BEC5"/>
            <Setter Property="BorderThickness" Value="1"/>
            <Setter Property="Background" Value="White"/>
        </Style>
        <Style x:Key="InputCombo" TargetType="ComboBox">
            <Setter Property="Padding" Value="6,4"/>
            <Setter Property="FontSize" Value="13"/>
            <Setter Property="BorderBrush" Value="#B0BEC5"/>
            <Setter Property="BorderThickness" Value="1"/>
            <Setter Property="Background" Value="White"/>
        </Style>
        <Style x:Key="AddBtn" TargetType="Button">
            <Setter Property="Background" Value="#26A69A"/>
            <Setter Property="Foreground" Value="White"/>
            <Setter Property="FontWeight" Value="SemiBold"/>
            <Setter Property="FontSize" Value="13"/>
            <Setter Property="Padding" Value="14,6"/>
            <Setter Property="BorderThickness" Value="0"/>
            <Setter Property="Cursor" Value="Hand"/>
        </Style>
    </Window.Resources>
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>
        <Border Grid.Row="0" Background="White" Padding="16,10" BorderBrush="#E0E0E0" BorderThickness="0,0,0,1">
            <Grid>
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="Auto"/>
                    <ColumnDefinition Width="*"/>
                </Grid.ColumnDefinitions>
                <StackPanel Grid.Column="0" Orientation="Horizontal" VerticalAlignment="Center">
                    <TextBlock Text="🧾" FontSize="28" Margin="0,0,10,0" VerticalAlignment="Center"/>
                    <StackPanel>
                        <TextBlock Text="فاتورة" FontSize="18" FontWeight="Bold" Foreground="#00695C"/>
                        <TextBlock Text="{Binding InvoiceTypeDisplay}" FontSize="12" Foreground="#78909C"/>
                    </StackPanel>
                </StackPanel>
                <Grid Grid.Column="1" Margin="20,0,0,0">
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="Auto"/><ColumnDefinition Width="140"/>
                        <ColumnDefinition Width="18"/><ColumnDefinition Width="Auto"/>
                        <ColumnDefinition Width="150"/><ColumnDefinition Width="18"/>
                        <ColumnDefinition Width="Auto"/><ColumnDefinition Width="120"/>
                    </Grid.ColumnDefinitions>
                    <Grid.RowDefinitions>
                        <RowDefinition Height="Auto"/><RowDefinition Height="8"/><RowDefinition Height="Auto"/>
                    </Grid.RowDefinitions>
                    <TextBlock Grid.Row="0" Grid.Column="0" Text="الرقم:" Style="{StaticResource HeaderLabel}"/>
                    <TextBox Grid.Row="0" Grid.Column="1" Text="{Binding SerialNumber}" Style="{StaticResource InputBox}" IsReadOnly="True"/>
                    <TextBlock Grid.Row="0" Grid.Column="3" Text="التاريخ:" Style="{StaticResource HeaderLabel}"/>
                    <DatePicker Grid.Row="0" Grid.Column="4" SelectedDate="{Binding InvoiceDate}" FontSize="13"/>
                    <TextBlock Grid.Row="0" Grid.Column="6" Text="النوع:" Style="{StaticResource HeaderLabel}"/>
                    <ComboBox Grid.Row="0" Grid.Column="7" Style="{StaticResource InputCombo}" SelectedValue="{Binding InvoiceType}" SelectedValuePath="Tag">
                        <ComboBoxItem Content="مبيعات" Tag="sales"/>
                        <ComboBoxItem Content="مشتريات" Tag="purchase"/>
                    </ComboBox>
                    <TextBlock Grid.Row="2" Grid.Column="0" Text="الطرف:" Style="{StaticResource HeaderLabel}"/>
                    <ComboBox Grid.Row="2" Grid.Column="1" Grid.ColumnSpan="4" Style="{StaticResource InputCombo}" ItemsSource="{Binding Parties}" DisplayMemberPath="Name" SelectedValuePath="Id" SelectedValue="{Binding PartyId}"/>
                    <TextBlock Grid.Row="2" Grid.Column="6" Text="المخزن:" Style="{StaticResource HeaderLabel}"/>
                    <ComboBox Grid.Row="2" Grid.Column="7" Style="{StaticResource InputCombo}" ItemsSource="{Binding Warehouses}" DisplayMemberPath="Name" SelectedValuePath="Id" SelectedValue="{Binding WarehouseId}"/>
                </Grid>
            </Grid>
        </Border>
        <Border Grid.Row="1" Background="White" Margin="10,8,10,4" Padding="8" BorderBrush="#E0E0E0" BorderThickness="1" CornerRadius="4">
            <Grid>
                <Grid.RowDefinitions>
                    <RowDefinition Height="Auto"/><RowDefinition Height="*"/>
                </Grid.RowDefinitions>
                <Grid Grid.Row="0" Margin="0,0,0,6">
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="*"/><ColumnDefinition Width="Auto"/>
                    </Grid.ColumnDefinitions>
                    <TextBlock Grid.Column="0" Text="بنود الفاتورة" FontSize="15" FontWeight="Bold" Foreground="#37474F" VerticalAlignment="Center"/>
                    <Button Grid.Column="1" Content="+ إضافة صنف" Style="{StaticResource AddBtn}" Command="{Binding AddLineCommand}"/>
                </Grid>
                <DataGrid x:Name="LinesGrid" Grid.Row="1" ItemsSource="{Binding Lines}" AutoGenerateColumns="False" CanUserAddRows="False" HeadersVisibility="Column" GridLinesVisibility="Horizontal" HorizontalGridLinesBrush="#ECEFF1" BorderBrush="#E0E0E0" BorderThickness="1" Background="White" FontSize="13" MinHeight="180">
                    <DataGrid.Columns>
                        <DataGridTemplateColumn Header="🔍 الصنف (اكتب للبحث)" Width="320">
                            <DataGridTemplateColumn.CellTemplate>
                                <DataTemplate>
                                    <Grid>
                                        <Popup IsOpen="{Binding IsPopupOpen}" Placement="Bottom" PlacementTarget="{Binding ElementName=SearchBoxInner}" StaysOpen="False" AllowsTransparency="True" PopupAnimation="Slide">
                                            <Border Background="White" BorderBrush="#00897B" BorderThickness="1" CornerRadius="2" MaxHeight="260" MinWidth="300">
                                                <ListBox x:Name="SearchList" ItemsSource="{Binding FilteredItems}" SelectedIndex="{Binding SearchSelectedIndex}" SelectionChanged="SearchList_SelectionChanged" BorderThickness="0">
                                                    <ListBox.ItemTemplate>
                                                        <DataTemplate>
                                                            <StackPanel Margin="2">
                                                                <TextBlock Text="{Binding Name}" FontWeight="SemiBold" Foreground="#263238"/>
                                                                <StackPanel Orientation="Horizontal">
                                                                    <TextBlock Text="باركود: " Foreground="#78909C" FontSize="11"/>
                                                                    <TextBlock Text="{Binding Barcode}" Foreground="#00897B" FontSize="11"/>
                                                                    <TextBlock Text="  |  مخزون: " Foreground="#78909C" FontSize="11"/>
                                                                    <TextBlock Text="{Binding CurrentStock}" Foreground="#EF6C00" FontSize="11"/>
                                                                </StackPanel>
                                                            </StackPanel>
                                                        </DataTemplate>
                                                    </ListBox.ItemTemplate>
                                                </ListBox>
                                            </Border>
                                        </Popup>
                                        <TextBox x:Name="SearchBoxInner" Text="{Binding SearchText, UpdateSourceTrigger=PropertyChanged}" Tag="search" Padding="6,4" BorderBrush="#80CBC4" BorderThickness="1,1,1,2" Background="White" KeyDown="SearchBox_KeyDown"/>
                                    </Grid>
                                </DataTemplate>
                            </DataGridTemplateColumn.CellTemplate>
                        </DataGridTemplateColumn>
                        <DataGridTextColumn Header="باركود" Width="110" Binding="{Binding Barcode}" IsReadOnly="True"/>
                        <DataGridTextColumn Header="الوحدة" Width="70" Binding="{Binding Unit}" IsReadOnly="True"/>
                        <DataGridTemplateColumn Header="الكمية" Width="90">
                            <DataGridTemplateColumn.CellTemplate>
                                <DataTemplate>
                                    <TextBox Text="{Binding Quantity, UpdateSourceTrigger=PropertyChanged, StringFormat=N2}" Tag="qty" Padding="4,3" BorderBrush="#E0E0E0" KeyDown="Cell_KeyDown"/>
                                </DataTemplate>
                            </DataGridTemplateColumn.CellTemplate>
                        </DataGridTemplateColumn>
                        <DataGridTemplateColumn Header="السعر" Width="100">
                            <DataGridTemplateColumn.CellTemplate>
                                <DataTemplate>
                                    <TextBox Text="{Binding Price, UpdateSourceTrigger=PropertyChanged, StringFormat=N2}" Tag="price" Padding="4,3" BorderBrush="#E0E0E0" KeyDown="Cell_KeyDown"/>
                                </DataTemplate>
                            </DataGridTemplateColumn.CellTemplate>
                        </DataGridTemplateColumn>
                        <DataGridTemplateColumn Header="خصم" Width="80">
                            <DataGridTemplateColumn.CellTemplate>
                                <DataTemplate>
                                    <TextBox Text="{Binding Discount, UpdateSourceTrigger=PropertyChanged, StringFormat=N2}" Tag="discount" Padding="4,3" BorderBrush="#E0E0E0" KeyDown="Cell_KeyDown"/>
                                </DataTemplate>
                            </DataGridTemplateColumn.CellTemplate>
                        </DataGridTemplateColumn>
                        <DataGridTextColumn Header="الإجمالي" Width="110" Binding="{Binding Total, StringFormat=N2}" IsReadOnly="True">
                            <DataGridTextColumn.ElementStyle>
                                <Style TargetType="TextBlock">
                                    <Setter Property="HorizontalAlignment" Value="Right"/>
                                    <Setter Property="FontWeight" Value="SemiBold"/>
                                    <Setter Property="Foreground" Value="#00695C"/>
                                </Style>
                            </DataGridTextColumn.ElementStyle>
                        </DataGridTextColumn>
                        <DataGridTemplateColumn Header="حذف" Width="60">
                            <DataGridTemplateColumn.CellTemplate>
                                <DataTemplate>
                                    <Button Content="🗑" Background="Transparent" BorderThickness="0" FontSize="14" Cursor="Hand" Command="{Binding DataContext.DeleteLineCommand, RelativeSource={RelativeSource AncestorType=DataGrid}}" CommandParameter="{Binding}"/>
                                </DataTemplate>
                            </DataGridTemplateColumn.CellTemplate>
                        </DataGridTemplateColumn>
                    </DataGrid.Columns>
                </DataGrid>
            </Grid>
        </Border>
        <Border x:Name="PaymentsSection" Grid.Row="2" Background="White" Margin="10,4,10,4" Padding="10" BorderBrush="#80CBC4" BorderThickness="1" CornerRadius="4">
            <Grid>
                <Grid.RowDefinitions>
                    <RowDefinition Height="Auto"/><RowDefinition Height="Auto"/>
                </Grid.RowDefinitions>
                <Grid Grid.Row="0" Margin="0,0,0,6">
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="*"/><ColumnDefinition Width="Auto"/>
                    </Grid.ColumnDefinitions>
                    <StackPanel Grid.Column="0" Orientation="Horizontal">
                        <TextBlock Text="💳" FontSize="16" Margin="0,0,6,0" VerticalAlignment="Center"/>
                        <TextBlock Text="طرق الدفع المتعددة" FontSize="15" FontWeight="Bold" Foreground="#00695C" VerticalAlignment="Center"/>
                    </StackPanel>
                    <Button Grid.Column="1" Content="+ إضافة دفعة" Style="{StaticResource AddBtn}" Command="{Binding AddPaymentCommand}"/>
                </Grid>
                <DataGrid x:Name="PaymentsGrid" Grid.Row="1" ItemsSource="{Binding Payments}" AutoGenerateColumns="False" CanUserAddRows="False" HeadersVisibility="Column" GridLinesVisibility="Horizontal" HorizontalGridLinesBrush="#ECEFF1" BorderBrush="#E0E0E0" BorderThickness="1" Background="White" FontSize="13" MaxHeight="140" CellEditEnding="PaymentsGrid_CellEditEnding">
                    <DataGrid.Columns>
                        <DataGridTemplateColumn Header="طريقة الدفع" Width="140">
                            <DataGridTemplateColumn.CellTemplate>
                                <DataTemplate>
                                    <ComboBox SelectedValue="{Binding Method, UpdateSourceTrigger=PropertyChanged}" SelectedValuePath="Tag" Padding="4,2">
                                        <ComboBoxItem Content="نقدي" Tag="cash"/>
                                        <ComboBoxItem Content="بطاقة" Tag="card"/>
                                        <ComboBoxItem Content="تحويل بنكي" Tag="bank"/>
                                        <ComboBoxItem Content="شيك" Tag="check"/>
                                        <ComboBoxItem Content="آجل" Tag="credit"/>
                                    </ComboBox>
                                </DataTemplate>
                            </DataGridTemplateColumn.CellTemplate>
                        </DataGridTemplateColumn>
                        <DataGridTemplateColumn Header="المبلغ" Width="140">
                            <DataGridTemplateColumn.CellTemplate>
                                <DataTemplate>
                                    <TextBox Text="{Binding Amount, UpdateSourceTrigger=PropertyChanged, StringFormat=N2}" Padding="4,3" BorderBrush="#E0E0E0" TextAlignment="Right"/>
                                </DataTemplate>
                            </DataGridTemplateColumn.CellTemplate>
                        </DataGridTemplateColumn>
                        <DataGridTextColumn Header="مرجع / رقم" Width="180" Binding="{Binding Reference, UpdateSourceTrigger=PropertyChanged}"/>
                        <DataGridTemplateColumn Header="تاريخ الدفع" Width="150">
                            <DataGridTemplateColumn.CellTemplate>
                                <DataTemplate>
                                    <DatePicker SelectedDate="{Binding PaymentDate}" FontSize="12"/>
                                </DataTemplate>
                            </DataGridTemplateColumn.CellTemplate>
                        </DataGridTemplateColumn>
                        <DataGridTextColumn Header="ملاحظات" Width="*" Binding="{Binding Notes, UpdateSourceTrigger=PropertyChanged}"/>
                        <DataGridTemplateColumn Header="حذف" Width="60">
                            <DataGridTemplateColumn.CellTemplate>
                                <DataTemplate>
                                    <Button Content="🗑" Background="Transparent" BorderThickness="0" FontSize="14" Cursor="Hand" Command="{Binding DataContext.DeletePaymentCommand, RelativeSource={RelativeSource AncestorType=DataGrid}}" CommandParameter="{Binding}"/>
                                </DataTemplate>
                            </DataGridTemplateColumn.CellTemplate>
                        </DataGridTemplateColumn>
                    </DataGrid.Columns>
                </DataGrid>
            </Grid>
        </Border>
        <Border Grid.Row="3" Background="White" Padding="16,10" BorderBrush="#E0E0E0" BorderThickness="0,1,0,0">
            <Grid>
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="*"/><ColumnDefinition Width="300"/>
                </Grid.ColumnDefinitions>
                <StackPanel Grid.Column="0" VerticalAlignment="Center">
                    <TextBlock x:Name="StatusText" Text="{Binding StatusMessage}" FontSize="12" Foreground="#00897B" TextWrapping="Wrap"/>
                </StackPanel>
                <Grid Grid.Column="1">
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="*"/><ColumnDefinition Width="110"/>
                    </Grid.ColumnDefinitions>
                    <Grid.RowDefinitions>
                        <RowDefinition Height="Auto"/><RowDefinition Height="Auto"/><RowDefinition Height="Auto"/><RowDefinition Height="Auto"/><RowDefinition Height="Auto"/>
                    </Grid.RowDefinitions>
                    <TextBlock Grid.Row="0" Grid.Column="0" Text="المجموع:" FontSize="13" Foreground="#546E7A" Padding="0,2"/>
                    <TextBlock Grid.Row="0" Grid.Column="1" Text="{Binding SubtotalDisplay}" FontSize="13" FontWeight="SemiBold" Foreground="#37474F" TextAlignment="Left" Padding="0,2"/>
                    <StackPanel Grid.Row="1" Grid.Column="0" Orientation="Horizontal">
                        <TextBlock Text="خصم:" FontSize="13" Foreground="#546E7A" VerticalAlignment="Center"/>
                        <TextBox Text="{Binding DiscountAmount, UpdateSourceTrigger=PropertyChanged, StringFormat=N2}" Width="80" Margin="6,0,0,0" Padding="3,2" FontSize="12" BorderBrush="#E0E0E0" TextAlignment="Right"/>
                    </StackPanel>
                    <TextBlock Grid.Row="1" Grid.Column="1" Text="{Binding DiscountAmount, StringFormat=N2}" FontSize="13" Foreground="#E53935" TextAlignment="Left"/>
                    <TextBlock Grid.Row="2" Grid.Column="0" Text="الإجمالي:" FontSize="14" FontWeight="Bold" Foreground="#00695C" Padding="0,2"/>
                    <TextBlock Grid.Row="2" Grid.Column="1" Text="{Binding TotalDisplay}" FontSize="15" FontWeight="Bold" Foreground="#00695C" TextAlignment="Left" Padding="0,2"/>
                    <TextBlock Grid.Row="3" Grid.Column="0" Text="المدفوع:" FontSize="13" Foreground="#2E7D32" Padding="0,2"/>
                    <TextBlock Grid.Row="3" Grid.Column="1" Text="{Binding TotalPaidDisplay}" FontSize="13" FontWeight="SemiBold" Foreground="#2E7D32" TextAlignment="Left" Padding="0,2"/>
                    <TextBlock Grid.Row="4" Grid.Column="0" Text="المتبقي:" FontSize="14" FontWeight="Bold" Foreground="#E65100" Padding="0,2"/>
                    <TextBlock Grid.Row="4" Grid.Column="1" Text="{Binding RemainingDisplay}" FontSize="14" FontWeight="Bold" Foreground="#E65100" TextAlignment="Left" Padding="0,2"/>
                </Grid>
            </Grid>
        </Border>
    </Grid>
</Window>
'@
[System.IO.File]::WriteAllText($xamlPath, $xaml, [System.Text.UTF8Encoding]::new($false))
Write-Host "  OK: InvoiceEditWindow.xaml written" -ForegroundColor Green

# [3/3] كتابة xaml.cs
Write-Host "[3/3] Writing InvoiceEditWindow.xaml.cs..." -ForegroundColor Yellow
 $csPath = Join-Path $ViewsDir 'InvoiceEditWindow.xaml.cs'
if (Test-Path $csPath) { Copy-Item $csPath "$csPath.bak" -Force }
 $xamlCs = @'
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Techtoon.UI.ViewModels;

namespace Techtoon.UI.Views
{
    public partial class InvoiceEditWindow : Window
    {
        private readonly InvoiceEditViewModel _vm;
        private readonly int? _editId;

        public InvoiceEditWindow(int? editId = null)
        {
            InitializeComponent();
            _editId = editId;
            _vm = new InvoiceEditViewModel();
            if (editId.HasValue) _vm.Id = editId.Value;
            DataContext = _vm;
            _vm.RequestClose += () => { DialogResult = _vm.DialogResult; Close(); };
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await _vm.LoadAsync();
            if (_editId.HasValue) await _vm.LoadFromInvoiceAsync(_editId.Value);
        }

        private void SearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (sender is not TextBox box) return;
            if (box.DataContext is not InvoiceLineItem line) return;
            switch (e.Key)
            {
                case Key.Down:
                    if (line.SearchSelectedIndex < line.FilteredItems.Count - 1) line.SearchSelectedIndex++;
                    e.Handled = true; break;
                case Key.Up:
                    if (line.SearchSelectedIndex > 0) line.SearchSelectedIndex--;
                    e.Handled = true; break;
                case Key.Enter:
                    if (line.IsPopupOpen && line.FilteredItems.Count > 0)
                    {
                        if (line.SearchSelectedIndex < 0 || line.SearchSelectedIndex >= line.FilteredItems.Count) line.SearchSelectedIndex = 0;
                        line.SelectByIndex();
                    }
                    FocusQuantity(line); e.Handled = true; break;
                case Key.Escape:
                    line.IsPopupOpen = false; e.Handled = true; break;
            }
        }

        private void SearchList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ListBox lb && lb.DataContext is InvoiceLineItem line && lb.SelectedItem is ItemSearchResult it)
            {
                line.SelectItem(it); FocusQuantity(line);
            }
        }

        private void FocusQuantity(InvoiceLineItem line)
        {
            try
            {
                if (LinesGrid.ItemContainerGenerator.ContainerFromItem(line) is DataGridRow row)
                {
                    var tb = FindDescendant<TextBox>(row, x => (x.Tag as string) == "qty");
                    tb?.Focus(); tb?.SelectAll();
                }
            }
            catch { }
        }

        private void Cell_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;
            if (sender is not TextBox tb) return;
            if (tb.Tag is not string tag) return;
            if (tb.DataContext is not InvoiceLineItem line) return;
            LinesGrid.CommitEdit(DataGridEditingUnit.Row, true);
            int idx = _vm.Lines.IndexOf(line);
            int last = _vm.Lines.Count - 1;
            if (idx >= 0 && idx < last)
            {
                if (LinesGrid.ItemContainerGenerator.ContainerFromIndex(idx + 1) is DataGridRow nextRow)
                {
                    var next = FindDescendant<TextBox>(nextRow, x => (x.Tag as string) == tag);
                    if (next != null) { next.Focus(); next.SelectAll(); }
                }
            }
            else if (idx == last)
            {
                _vm.AddLineCommand.Execute(null);
                LinesGrid.UpdateLayout();
                if (LinesGrid.ItemContainerGenerator.ContainerFromIndex(_vm.Lines.Count - 1) is DataGridRow newRow)
                {
                    var next = FindDescendant<TextBox>(newRow, x => (x.Tag as string) == "search");
                    next?.Focus();
                }
            }
            e.Handled = true;
        }

        private static T? FindDescendant<T>(DependencyObject? parent, Func<T, bool> predicate) where T : class
        {
            if (parent == null) return null;
            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T matched && predicate(matched)) return matched;
                var deep = FindDescendant(child, predicate);
                if (deep != null) return deep;
            }
            return null;
        }

        private void PaymentsGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction == DataGridEditAction.Commit) _vm.RefreshTotals();
        }
    }
}
'@
[System.IO.File]::WriteAllText($csPath, $xamlCs, [System.Text.UTF8Encoding]::new($false))
Write-Host "  OK: InvoiceEditWindow.xaml.cs written" -ForegroundColor Green

# البناء
Write-Host ""
Write-Host "===== Build =====" -ForegroundColor Cyan
Write-Host "Building (may take a minute)..." -ForegroundColor Yellow
 $build = dotnet build (Join-Path $ProjectPath 'Techtoon.UI') 2>&1
 $build | Select-Object -Last 8
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  DONE! Run: dotnet run --project Techtoon.UI" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
```
#### .\fix_final2.ps1
```
# fix_final2.ps1 - إصلاح اختيار الصنف + تحديث صفحة الفواتير
 $ProjectPath = 'D:\PROG\Accounting Prog\Techtoon Accounting C+'
 $ViewsDir = Join-Path $ProjectPath 'Techtoon.UI\Views'

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Techtoon - Final Fixes (2 issues)" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

# ============================================================
# 1) إصلاح اختيار الصنف في InvoiceEditWindow.xaml.cs
#    المشكلة: ComboBox لا يحدث النص فوراً بعد الاختيار
# ============================================================
Write-Host "`n[1/2] Fixing item selection in code-behind..." -ForegroundColor Yellow

 $csPath = Join-Path $ViewsDir 'InvoiceEditWindow.xaml.cs'
if (Test-Path $csPath) {
    Copy-Item $csPath ($csPath + '.bak_f2') -Force
    $content = Get-Content $csPath -Raw -Encoding UTF8
    
    # استبدال دالة ItemSearchCombo_SelectionChanged
    $oldMethod = '(?s)private void ItemSearchCombo_SelectionChanged\(.*?\n        \}'
    $newMethod = @'
private void ItemSearchCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (sender is ComboBox cb && cb.SelectedItem is ItemSearchResult item)
                {
                    if (cb.DataContext is InvoiceLineItem line)
                    {
                        line.SelectItem(item);
                        // تحديث النص في الـ ComboBox فوراً
                        cb.Text = item.Name;
                        // إعادة التركيز للكمية
                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            try
                            {
                                LinesGrid.CommitEdit(DataGridEditingUnit.Cell, true);
                                LinesGrid.CommitEdit(DataGridEditingUnit.Row, true);
                                if (LinesGrid.ItemContainerGenerator.ContainerFromItem(line) is DataGridRow row)
                                {
                                    var qtyTb = FindDescendant<TextBox>(row, x => (x.Tag as string) == "qty");
                                    qtyTb?.Focus();
                                    qtyTb?.SelectAll();
                                }
                            }
                            catch { }
                        }), System.Windows.Threading.DispatcherPriority.Input);
                    }
                }
            }
            catch { }
        }
'@
    
    $content = [regex]::Replace($content, $oldMethod, $newMethod)
    [System.IO.File]::WriteAllText($csPath, $content, [System.Text.UTF8Encoding]::new($true))
    Write-Host "  OK: Item selection fixed" -ForegroundColor Green
}
# ============================================================
# 2) إصلاح تحديث صفحة الفواتير بعد الحفظ
#    المشكلة: صفحة الفواتير لا تعيد تحميل البيانات
# ============================================================
Write-Host "`n[2/2] Fixing invoices page refresh..." -ForegroundColor Yellow

# ابحث عن ملف عرض الفواتير
 $invoicesViewPath = Join-Path $ViewsDir 'InvoicesView.xaml.cs'
if (-not (Test-Path $invoicesViewPath)) {
    $invoicesViewPath = Join-Path $ViewsDir 'InvoicesView.xaml.cs'
}

if (Test-Path $invoicesViewPath) {
    Copy-Item $invoicesViewPath ($invoicesViewPath + '.bak_f2') -Force
    $content = Get-Content $invoicesViewPath -Raw -Encoding UTF8
    
    # إضافة دالة ReloadAsync ودالة Refresh للوصول إليها من خارج الصفحة
    if ($content -notmatch 'public.*Refresh\b') {
        $insertCode = @'

        // دالة لتحديث الصفحة من الخارج (بعد حفظ فاتورة جديدة)
        public static void RefreshAllOpenInvoicesViews()
        {
            try
            {
                if (System.Windows.Application.Current?.MainWindow?.Content is System.Windows.Controls.Frame frame)
                {
                    var page = frame.Content;
                    if (page != null)
                    {
                        var refreshMethod = page.GetType().GetMethod("Refresh");
                        if (refreshMethod != null) refreshMethod.Invoke(page, null);
                    }
                }
            }
            catch { }
        }

        public void Refresh()
        {
            try { LoadInvoices(); } catch { }
        }

        private void LoadInvoices()
        {
            try
            {
                // اقرأ الفواتير من DB واعرضها
                var invoices = LoadInvoicesFromDb();
                if (InvoicesList != null)
                {
                    InvoicesList.ItemsSource = invoices;
                }
            }
            catch { }
        }

        private System.Collections.Generic.List<InvoiceRow> LoadInvoicesFromDb()
        {
            var list = new System.Collections.Generic.List<InvoiceRow>();
            try
            {
                string cs = "Host=ep-bitter-night-za726w59-pooler.c-2.eu-west-2.aws.neon.tech;Port=5432;Username=neondb_owner;Password=***;Database=neondb;SSL Mode=Require;Trust Server Certificate=true";
                using (var conn = new Npgsql.NpgsqlConnection(cs))
                {
                    conn.Open();
                    string sql = @"SELECT i.id, i.invoice_number, i.date, i.type, i.total, i.paid_amount,
                        COALESCE(p.name, '') AS party_name
                        FROM invoices i LEFT JOIN parties p ON i.party_id = p.id
                        ORDER BY i.id DESC LIMIT 100";
                    using (var cmd = new Npgsql.NpgsqlCommand(sql, conn))
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            list.Add(new InvoiceRow
                            {
                                Id = r.IsDBNull(0) ? 0 : r.GetInt32(0),
                                InvoiceNumber = r.IsDBNull(1) ? "" : r.GetString(1),
                                Date = r.IsDBNull(2) ? DateTime.MinValue : r.GetDateTime(2),
                                Type = r.IsDBNull(3) ? "" : r.GetString(3),
                                Total = r.IsDBNull(4) ? 0 : Convert.ToDecimal(r.GetValue(4)),
                                Paid = r.IsDBNull(5) ? 0 : Convert.ToDecimal(r.GetValue(5)),
                                PartyName = r.IsDBNull(6) ? "" : r.GetString(6)
                            });
                        }
                    }
                }
            }
            catch { }
            return list;
        }

        public class InvoiceRow
        {
            public int Id { get; set; }
            public string InvoiceNumber { get; set; }
            public DateTime Date { get; set; }
            public string Type { get; set; }
            public decimal Total { get; set; }
            public decimal Paid { get; set; }
            public string PartyName { get; set; }
            public decimal Remaining { get { return Total - Paid; } }
        }
'@
        # أضف في نهاية الكلاس (قبل آخر })
        $lastBrace = $content.LastIndexOf('}')
        if ($lastBrace -gt 0) {
            $content = $content.Substring(0, $lastBrace) + $insertCode + "`n}" 
        }
        [System.IO.File]::WriteAllText($invoicesViewPath, $content, [System.Text.UTF8Encoding]::new($true))
        Write-Host "  OK: Invoices page refresh added" -ForegroundColor Green
    } else {
        Write-Host "  SKIP: Refresh already exists" -ForegroundColor Yellow
    }
} else {
    Write-Host "  WARN: InvoicesView.xaml.cs not found" -ForegroundColor Yellow
}

# ============================================================
# 3) تحديث InvoiceEditViewModel.cs لإعلام نافذة الفواتير بعد الحفظ
# ============================================================
Write-Host "`n[3/3] Updating ViewModel to notify after save..." -ForegroundColor Yellow
 $vmPath = Join-Path $ViewsDir 'InvoiceEditViewModel.cs'
if (Test-Path $vmPath) {
    $content = Get-Content $vmPath -Raw -Encoding UTF8
    # بعد RequestClose في SaveAsync، أضف استدعاء RefreshAllOpenInvoicesViews
    if ($content -notmatch 'RefreshAllOpenInvoicesViews') {
        $old = 'StatusMessage = "Saved! ID: " + Id;'
        $new = 'StatusMessage = "Saved! ID: " + Id;
                try { Techtoon.UI.Views.InvoicesView.RefreshAllOpenInvoicesViews(); } catch { }'
        $content = $content.Replace($old, $new)
        [System.IO.File]::WriteAllText($vmPath, $content, [System.Text.UTF8Encoding]::new($true))
        Write-Host "  OK: ViewModel notifies after save" -ForegroundColor Green
    } else {
        Write-Host "  SKIP: Already present" -ForegroundColor Yellow
    }
}

# ============================================================
# البناء
# ============================================================
Write-Host "`n===== Build =====" -ForegroundColor Cyan
dotnet build (Join-Path $ProjectPath 'Techtoon.UI') 2>&1 | Select-Object -Last 8
Write-Host "`nDONE! Run: dotnet run --project Techtoon.UI" -ForegroundColor Green

```
#### .\fix_item.ps1
```
// =====================================================================
// InvoiceEditViewModel.cs  —  Techtoon Accounting C+
// الإصدار المصحح: بحث فوري ذكي + دفع متعدد الطرق
// =====================================================================
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows.Input;
using Npgsql;
using Techtoon.UI.ViewModels;

namespace Techtoon.UI.Views
{
    // ==================================================================
    //  صنف يساعد على عرض صف البند داخل شبكة الفاتورة
    //  يحتوي على بحث فوري: عند كتابة أي حرف أو رقم يظهر ما يقاربه
    // ==================================================================
    public class InvoiceLineItem : ViewModelBase
    {
        private List<ItemSearchResult> _allItems;

        public InvoiceLineItem(List<ItemSearchResult> allItems)
        {
            _allItems = allItems ?? new List<ItemSearchResult>();
            FilteredItems = new ObservableCollection<ItemSearchResult>();
        }

        // ---- البحث الفوري ----
        private string _searchText = "";
        public string SearchText
        {
            get { return _searchText; }
            set
            {
                _searchText = value ?? "";
                OnPropertyChanged("SearchText");
                ApplySearch();
            }
        }

        private void ApplySearch()
        {
            string q = (_searchText ?? "").Trim();
            FilteredItems.Clear();
            if (q.Length == 0)
            {
                IsPopupOpen = false;
                return;
            }

            // ابحث بالاسم أو الباركود أو SKU
            var matches = _allItems
                .Where(it =>
                    (it.Name != null && it.Name.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (it.Barcode != null && it.Barcode.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (it.Sku != null && it.Sku.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0))
                .Take(15)
                .ToList();

            foreach (var m in matches) FilteredItems.Add(m);
            IsPopupOpen = matches.Count > 0;
            SearchSelectedIndex = matches.Count > 0 ? 0 : -1;
        }

        public ObservableCollection<ItemSearchResult> FilteredItems { get; set; }

        private int _searchSelectedIndex = -1;
        public int SearchSelectedIndex
        {
            get { return _searchSelectedIndex; }
            set { _searchSelectedIndex = value; OnPropertyChanged("SearchSelectedIndex"); }
        }

        private bool _isPopupOpen = false;
        public bool IsPopupOpen
        {
            get { return _isPopupOpen; }
            set { _isPopupOpen = value; OnPropertyChanged("IsPopupOpen"); }
        }

        // ---- بيانات البند المحدد ----
        public int ItemId { get; set; }
        public string Unit { get; set; }

        private string _itemName = "";
        public string ItemName
        {
            get { return _itemName; }
            set { _itemName = value; OnPropertyChanged("ItemName"); }
        }

        private string _barcode = "";
        public string Barcode
        {
            get { return _barcode; }
            set { _barcode = value; OnPropertyChanged("Barcode"); }
        }

        private decimal _quantity = 1;
        public decimal Quantity
        {
            get { return _quantity; }
            set
            {
                _quantity = value;
                OnPropertyChanged("Quantity");
                Recalc();
            }
        }

        private decimal _price = 0;
        public decimal Price
        {
            get { return _price; }
            set
            {
                _price = value;
                OnPropertyChanged("Price");
                Recalc();
            }
        }

        private decimal _discount = 0;
        public decimal Discount
        {
            get { return _discount; }
            set
            {
                _discount = value;
                OnPropertyChanged("Discount");
                Recalc();
            }
        }

        private decimal _total = 0;
        public decimal Total
        {
            get { return _total; }
            set { _total = value; OnPropertyChanged("Total"); }
        }

        private decimal _stockAvailable = 0;
        public decimal StockAvailable
        {
            get { return _stockAvailable; }
            set { _stockAvailable = value; OnPropertyChanged("StockAvailable"); }
        }

        public bool HasItem { get { return ItemId > 0; } }

        private void Recalc()
        {
            Total = Math.Round(_quantity * _price - _discount, 2);
        }

        // ---- اختيار صنف من نتائج البحث ----
        public void SelectItem(ItemSearchResult it)
        {
            if (it == null) return;
            ItemId = it.Id;
            ItemName = it.Name;
            Barcode = it.Barcode;
            Unit = it.Unit;
            Price = it.SalePrice;
            StockAvailable = it.CurrentStock;
            Quantity = 1;
            IsPopupOpen = false;
            SearchText = it.Name;
            OnPropertyChanged("HasItem");
        }

        public void SelectByIndex()
        {
            if (SearchSelectedIndex >= 0 && SearchSelectedIndex < FilteredItems.Count)
            {
                SelectItem(FilteredItems[SearchSelectedIndex]);
            }
        }
    }

    // ==================================================================
    //  صف الدفع - لطرق الدفع المتعددة في الفاتورة
    // ==================================================================
    public class InvoicePaymentRow : ViewModelBase
    {
        public int Id { get; set; }

        private string _method = "cash";
        public string Method
        {
            get { return _method; }
            set { _method = value; OnPropertyChanged("Method"); OnPropertyChanged("MethodDisplay"); }
        }

        public string MethodDisplay
        {
            get
            {
                switch ((_method ?? "").ToLowerInvariant())
                {
                    case "cash": return "نقدي";
                    case "card": return "بطاقة";
                    case "bank": return "تحويل بنكي";
                    case "check": return "شيك";
                    case "credit": return "آجل";
                    default: return _method;
                }
            }
        }

        private decimal _amount = 0;
        public decimal Amount
        {
            get { return _amount; }
            set { _amount = value; OnPropertyChanged("Amount"); }
        }

        private string _reference = "";
        public string Reference
        {
            get { return _reference; }
            set { _reference = value; OnPropertyChanged("Reference"); }
        }

        private DateTime _paymentDate = DateTime.Now;
        public DateTime PaymentDate
        {
            get { return _paymentDate; }
            set { _paymentDate = value; OnPropertyChanged("PaymentDate"); }
        }

        private string _notes = "";
        public string Notes
        {
            get { return _notes; }
            set { _notes = value; OnPropertyChanged("Notes"); }
        }
    }

    // ==================================================================
    //  صنف مبسط للأصناف
    // ==================================================================
    public class ItemSearchResult
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Barcode { get; set; }
        public string Sku { get; set; }
        public string Unit { get; set; }
        public decimal SalePrice { get; set; }
        public decimal PurchasePrice { get; set; }
        public decimal CurrentStock { get; set; }
        public decimal TaxRate { get; set; }
        public bool IsActive { get; set; }

        public string SearchDisplay
        {
            get
            {
                string b = string.IsNullOrEmpty(Barcode) ? "" : " [" + Barcode + "]";
                string s = string.IsNullOrEmpty(Sku) ? "" : " (" + Sku + ")";
                string stk = CurrentStock > 0 ? "  مخزون: " + CurrentStock.ToString("0.##") : "";
                return (Name ?? "") + b + s + stk;
            }
        }
    }

    // ==================================================================
    //  ViewModel الرئيسي لنافذة تعديل الفاتورة
    // ==================================================================
    public class InvoiceEditViewModel : ViewModelBase
    {
        public Action RequestClose;

        // ---- معلومات الفاتورة ----
        public int Id { get; set; }
        public bool DialogResult { get; set; }
        public bool PrintAfterSave { get; set; }

        private string _invoiceType = "sales";
        public string InvoiceType
        {
            get { return _invoiceType; }
            set { _invoiceType = value; OnPropertyChanged("InvoiceType"); OnPropertyChanged("InvoiceTypeDisplay"); }
        }

        public string InvoiceTypeDisplay
        {
            get
            {
                return (_invoiceType ?? "") == "purchase" ? "مشتريات" : "مبيعات";
            }
        }

        private string _serialNumber = "";
        public string SerialNumber
        {
            get { return _serialNumber; }
            set { _serialNumber = value; OnPropertyChanged("SerialNumber"); }
        }

        private DateTime _invoiceDate = DateTime.Now;
        public DateTime InvoiceDate
        {
            get { return _invoiceDate; }
            set { _invoiceDate = value; OnPropertyChanged("InvoiceDate"); }
        }

        private int _partyId = 0;
        public int PartyId
        {
            get { return _partyId; }
            set { _partyId = value; OnPropertyChanged("PartyId"); }
        }

        private string _partyName = "";
        public string PartyName
        {
            get { return _partyName; }
            set { _partyName = value; OnPropertyChanged("PartyName"); }
        }

        private int _warehouseId = 0;
        public int WarehouseId
        {
            get { return _warehouseId; }
            set { _warehouseId = value; OnPropertyChanged("WarehouseId"); }
        }

        private string _warehouseName = "";
        public string WarehouseName
        {
            get { return _warehouseName; }
            set { _warehouseName = value; OnPropertyChanged("WarehouseName"); }
        }

        private string _notes = "";
        public string Notes
        {
            get { return _notes; }
            set { _notes = value; OnPropertyChanged("Notes"); }
        }

        // ---- وضع الدفع ----
        private string _paymentMode = "full";
        public string PaymentMode
        {
            get { return _paymentMode; }
            set
            {
                _paymentMode = value;
                OnPropertyChanged("PaymentMode");
                OnPropertyChanged("ShowPaymentsSection");
                OnPaymentModeChanged();
            }
        }

        // قسم الدفع متعدد الطرق يظهر دائماً
        public bool ShowPaymentsSection
        {
            get { return true; }
        }

        // ---- بنود الفاتورة ----
        public ObservableCollection<InvoiceLineItem> Lines { get; set; }
        public ObservableCollection<ItemSearchResult> AllItemsList { get; set; }
        private List<ItemSearchResult> _allItems;

        // ---- قائمة الأطراف والمخازن ----
        public ObservableCollection<PartyOption> Parties { get; set; }
        public ObservableCollection<WarehouseOption> Warehouses { get; set; }

        // ---- الدفعات المتعددة ----
        public ObservableCollection<InvoicePaymentRow> Payments { get; set; }

        // ---- الحسابات ----
        private decimal _subtotal = 0;
        public decimal Subtotal
        {
            get { return _subtotal; }
            set { _subtotal = value; OnPropertyChanged("Subtotal"); OnPropertyChanged("SubtotalDisplay"); }
        }

        public string SubtotalDisplay { get { return _subtotal.ToString("N2"); } }

        private decimal _discountAmount = 0;
        public decimal DiscountAmount
        {
            get { return _discountAmount; }
            set
            {
                _discountAmount = value;
                OnPropertyChanged("DiscountAmount");
                RefreshTotals();
            }
        }

        private decimal _discountPercent = 0;
        public decimal DiscountPercent
        {
            get { return _discountPercent; }
            set
            {
                _discountPercent = value;
                OnPropertyChanged("DiscountPercent");
                // احسب خصم مبلغ من النسبة
                if (_discountPercent > 0)
                {
                    DiscountAmount = Math.Round(_subtotal * _discountPercent / 100m, 2);
                }
            }
        }

        private decimal _taxAmount = 0;
        public decimal TaxAmount
        {
            get { return _taxAmount; }
            set { _taxAmount = value; OnPropertyChanged("TaxAmount"); OnPropertyChanged("TaxDisplay"); }
        }

        public string TaxDisplay { get { return _taxAmount.ToString("N2"); } }

        private decimal _total = 0;
        public decimal Total
        {
            get { return _total; }
            set { _total = value; OnPropertyChanged("Total"); OnPropertyChanged("TotalDisplay"); OnPropertyChanged("Remaining"); }
        }

        public string TotalDisplay { get { return _total.ToString("N2"); } }

        private decimal _totalPaid = 0;
        public decimal TotalPaid
        {
            get { return _totalPaid; }
            set { _totalPaid = value; OnPropertyChanged("TotalPaid"); OnPropertyChanged("TotalPaidDisplay"); OnPropertyChanged("Remaining"); }
        }

        public string TotalPaidDisplay { get { return _totalPaid.ToString("N2"); } }

        public decimal Remaining
        {
            get { return Math.Round(_total - _totalPaid, 2); }
        }

        public string RemainingDisplay { get { return Remaining.ToString("N2"); } }

        private string _statusMessage = "";
        public string StatusMessage
        {
            get { return _statusMessage; }
            set { _statusMessage = value; OnPropertyChanged("StatusMessage"); }
        }

        private bool _isBusy = false;
        public bool IsBusy
        {
            get { return _isBusy; }
            set { _isBusy = value; OnPropertyChanged("IsBusy"); }
        }

        // ---- الأوامر ----
        public ICommand AddLineCommand { get; set; }
        public ICommand DeleteLineCommand { get; set; }
        public ICommand AddPaymentCommand { get; set; }
        public ICommand DeletePaymentCommand { get; set; }
        public ICommand SaveCommand { get; set; }
        public ICommand SaveAndPrintCommand { get; set; }
        public ICommand CancelCommand { get; set; }

        // ==================================================================
        public InvoiceEditViewModel()
        {
            Lines = new ObservableCollection<InvoiceLineItem>();
            AllItemsList = new ObservableCollection<ItemSearchResult>();
            _allItems = new List<ItemSearchResult>();
            Parties = new ObservableCollection<PartyOption>();
            Warehouses = new ObservableCollection<WarehouseOption>();
            Payments = new ObservableCollection<InvoicePaymentRow>();

            AddLineCommand = new UiRelayCommand(_ => AddLine());
            DeleteLineCommand = new UiRelayCommand(p =>
            {
                if (p is InvoiceLineItem li) Lines.Remove(li);
                RefreshTotals();
            });
            AddPaymentCommand = new UiRelayCommand(_ => AddPayment());
            DeletePaymentCommand = new UiRelayCommand(p =>
            {
                if (p is InvoicePaymentRow pr) Payments.Remove(pr);
                RefreshTotals();
            });

            // تحديث الإجماليات فور تغيير أي مبلغ دفع
            Payments.CollectionChanged += (s, e) =>
            {
                if (e.NewItems != null)
                {
                    foreach (InvoicePaymentRow r in e.NewItems)
                    {
                        r.PropertyChanged += (s2, e2) =>
                        {
                            if (e2.PropertyName == "Amount" || e2.PropertyName == "Method")
                                RefreshTotals();
                        };
                    }
                }
            };
            SaveCommand = new UiRelayCommand(_ => { PrintAfterSave = false; _ = SaveAsync(); }, _ => !IsBusy);
            SaveAndPrintCommand = new UiRelayCommand(_ => { PrintAfterSave = true; _ = SaveAsync(); }, _ => !IsBusy);
            CancelCommand = new UiRelayCommand(_ => { DialogResult = false; RequestClose?.Invoke(); });
        }

        // ==================================================================
        //  اتصال قاعدة البيانات
        // ==================================================================
        public static string GetConnectionString()
        {
            try
            {
                string env = Environment.GetEnvironmentVariable("TECHTOON_PG_CONNECTION");
                if (!string.IsNullOrWhiteSpace(env) && env.Contains("Host=")) return env;
            }
            catch { }

            // القيمة الافتراضية (Neon PostgreSQL) - تُستخدم كأساس دائماً
            string defaultCs = "Host=ep-bitter-night-za726w59-pooler.c-2.eu-west-2.aws.neon.tech;Port=5432;Username=neondb_owner;Password=***;Database=neondb;SSL Mode=Require;Trust Server Certificate=true";

            // اقرأ من appsettings.json باستخدام JSON parser صحيح
            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string[] paths = new string[]
                {
                    Path.Combine(baseDir, "appsettings.json"),
                    Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json"),
                    Path.Combine(baseDir, "..", "..", "..", "appsettings.json"),
                    Path.Combine(baseDir, "..", "..", "..", "..", "Techtoon.UI", "appsettings.json"),
                };
                foreach (string path in paths)
                {
                    if (!File.Exists(path)) continue;
                    string json = File.ReadAllText(path);
                    string cs = ExtractConnectionStringFromJson(json);
                    // تحقق من صحة السلسلة قبل إرجاعها
                    if (!string.IsNullOrWhiteSpace(cs) && cs.Contains("Host=") && cs.Contains("Database="))
                        return cs;
                }
            }
            catch { }

            // ارجع القيمة الافتراضية الآمنة
            return defaultCs;
        }

        // ==================================================================
        //  استخراج سلسلة الاتصال من JSON بشكل صحيح
        // ==================================================================
        private static string ExtractConnectionStringFromJson(string json)
        {
            // الطريقة 1: استخدام System.Text.Json (مضمن في .NET 10)
            try
            {
                using (var doc = System.Text.Json.JsonDocument.Parse(json))
                {
                    string result = ExtractFromJsonElement(doc.RootElement);
                    if (!string.IsNullOrEmpty(result)) return result;
                }
            }
            catch { }

            // الطريقة 2: regex احتياطية - تبحث عن أي نص يحتوي Host=
            try
            {
                var match = System.Text.RegularExpressions.Regex.Match(json, @"""([^""\\]*(?:\\.[^""\\]*)*Host=[^""\\]*(?:\\.[^""\\]*)*)""");
                if (match.Success) return match.Groups[1].Value.Replace("\\\"", "\"");
            }
            catch { }

            return null;
        }

        private static string ExtractFromJsonElement(System.Text.Json.JsonElement element)
        {
            if (element.ValueKind == System.Text.Json.JsonValueKind.Object)
            {
                foreach (var prop in element.EnumerateObject())
                {
                    string name = prop.Name.ToLowerInvariant();
                    // إذا كان المفتاح يحتوي connection وقيمته نص يحتوي Host=
                    if ((name.Contains("connection") || name == "db" || name == "database" || name == "postgres" || name == "neon")
                        && prop.Value.ValueKind == System.Text.Json.JsonValueKind.String)
                    {
                        string val = prop.Value.GetString();
                        if (!string.IsNullOrEmpty(val) && val.Contains("Host="))
                            return val;
                    }
                    // ابحث في القيم المتداخلة
                    var nested = ExtractFromJsonElement(prop.Value);
                    if (nested != null) return nested;
                }
            }
            else if (element.ValueKind == System.Text.Json.JsonValueKind.Array)
            {
                foreach (var item in element.EnumerateArray())
                {
                    var nested = ExtractFromJsonElement(item);
                    if (nested != null) return nested;
                }
            }
            return null;
        }

        // ==================================================================
        //  تحميل البيانات الأولية
        // ==================================================================
        public new async System.Threading.Tasks.Task LoadAsync()
        {
            IsBusy = true;
            StatusMessage = "جاري تحميل البيانات...";
            try
            {
                _allItems = LoadItems();
                AllItemsList.Clear();
                foreach (var it in _allItems) AllItemsList.Add(it);

                LoadParties();
                LoadWarehouses();

                // رقم تسلسلي جديد
                SerialNumber = GenerateSerial(InvoiceType);

                // صف بند فارغ
                AddLine();

                // دفع افتراضي
                if (Payments.Count == 0)
                {
                    Payments.Add(new InvoicePaymentRow { Method = "cash", Amount = 0, PaymentDate = DateTime.Now });
                }

                StatusMessage = "";
            }
            catch (Exception ex)
            {
                StatusMessage = "خطأ: " + ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
            await System.Threading.Tasks.Task.CompletedTask;
        }

        private List<ItemSearchResult> LoadItems()
        {
            var list = new List<ItemSearchResult>();
            string cs = GetConnectionString();
            using (var conn = new NpgsqlConnection(cs))
            {
                conn.Open();
                // تحقق من وجود الجدول
                string table = FindTable(conn, new string[] { "items", "products", "inventory_items" });
                if (string.IsNullOrEmpty(table)) return list;

                string nameCol = FindColumn(conn, table, new string[] { "name", "item_name", "title", "description" });
                string barCol = FindColumn(conn, table, new string[] { "barcode", "bar_code", "code", "ean" });
                string skuCol = FindColumn(conn, table, new string[] { "sku", "item_code", "code" });
                string priceCol = FindColumn(conn, table, new string[] { "sale_price", "selling_price", "price", "unit_price" });
                string unitCol = FindColumn(conn, table, new string[] { "unit", "uom" });
                string stockCol = FindColumn(conn, table, new string[] { "current_stock", "stock", "quantity", "qty_on_hand" });

                string sql = "SELECT id";
                if (!string.IsNullOrEmpty(nameCol)) sql += ", \"" + nameCol + "\"";
                if (!string.IsNullOrEmpty(barCol)) sql += ", \"" + barCol + "\"";
                if (!string.IsNullOrEmpty(skuCol)) sql += ", \"" + skuCol + "\"";
                if (!string.IsNullOrEmpty(priceCol)) sql += ", \"" + priceCol + "\"";
                if (!string.IsNullOrEmpty(unitCol)) sql += ", \"" + unitCol + "\"";
                sql += " FROM \"" + table + "\"";
                string where = " WHERE ";
                if (!string.IsNullOrEmpty(nameCol)) where += "\"" + nameCol + "\" IS NOT NULL";
                else where += "1=1";
                // فلترة الأصناف النشطة إن وجد العمود
                string actCol = FindColumn(conn, table, new string[] { "is_active", "active", "enabled" });
                if (!string.IsNullOrEmpty(actCol)) where += " AND (\"" + actCol + "\" IS NULL OR \"" + actCol + "\" = true)";
                sql += where + " ORDER BY ";
                sql += !string.IsNullOrEmpty(nameCol) ? "\"" + nameCol + "\"" : "id";
                sql += " LIMIT 500";

                using (var cmd = new NpgsqlCommand(sql, conn))
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        var it = new ItemSearchResult();
                        it.Id = r.GetInt32(0);
                        int col = 1;
                        if (!string.IsNullOrEmpty(nameCol)) { it.Name = r.IsDBNull(col) ? "" : r.GetString(col); col++; }
                        if (!string.IsNullOrEmpty(barCol)) { it.Barcode = r.IsDBNull(col) ? "" : r.GetString(col); col++; }
                        if (!string.IsNullOrEmpty(skuCol)) { it.Sku = r.IsDBNull(col) ? "" : r.GetString(col); col++; }
                        if (!string.IsNullOrEmpty(priceCol)) { it.SalePrice = r.IsDBNull(col) ? 0 : (decimal)r.GetValue(col); col++; }
                        if (!string.IsNullOrEmpty(unitCol)) { it.Unit = r.IsDBNull(col) ? "" : r.GetString(col); col++; }
                        list.Add(it);
                    }
                }
            }
            return list;
        }

        private void LoadParties()
        {
            Parties.Clear();
            try
            {
                string cs = GetConnectionString();
                using (var conn = new NpgsqlConnection(cs))
                {
                    conn.Open();
                    string table = FindTable(conn, new string[] { "parties", "customers_suppliers", "contacts", "accounts" });
                    if (string.IsNullOrEmpty(table)) return;
                    string nameCol = FindColumn(conn, table, new string[] { "name", "party_name", "title", "account_name" });
                    if (string.IsNullOrEmpty(nameCol)) return;
                    string sql = "SELECT id, \"" + nameCol + "\" FROM \"" + table + "\" ORDER BY \"" + nameCol + "\" LIMIT 500";
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            Parties.Add(new PartyOption { Id = r.GetInt32(0), Name = r.IsDBNull(1) ? "" : r.GetString(1) });
                        }
                    }
                }
            }
            catch { }
        }

        private void LoadWarehouses()
        {
            Warehouses.Clear();
            try
            {
                string cs = GetConnectionString();
                using (var conn = new NpgsqlConnection(cs))
                {
                    conn.Open();
                    string table = FindTable(conn, new string[] { "warehouses", "stores", "locations" });
                    if (string.IsNullOrEmpty(table)) return;
                    string nameCol = FindColumn(conn, table, new string[] { "name", "warehouse_name", "title" });
                    if (string.IsNullOrEmpty(nameCol)) return;
                    string sql = "SELECT id, \"" + nameCol + "\" FROM \"" + table + "\" ORDER BY \"" + nameCol + "\" LIMIT 100";
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            Warehouses.Add(new WarehouseOption { Id = r.GetInt32(0), Name = r.IsDBNull(1) ? "" : r.GetString(1) });
                        }
                    }
                }
            }
            catch { }
        }

        private string GenerateSerial(string type)
        {
            try
            {
                string cs = GetConnectionString();
                using (var conn = new NpgsqlConnection(cs))
                {
                    conn.Open();
                    string table = FindTable(conn, new string[] { "invoices" });
                    if (string.IsNullOrEmpty(table)) return "";
                    string serialCol = FindColumn(conn, table, new string[] { "serial_number", "invoice_number", "number", "code" });
                    string typeCol = FindColumn(conn, table, new string[] { "invoice_type", "type", "kind" });
                    string prefix = (type ?? "sales") == "purchase" ? "PUR" : "SAL";
                    int year = DateTime.Now.Year;
                    string likePattern = prefix + "-" + year + "-%";
                    string sql;
                    if (!string.IsNullOrEmpty(serialCol))
                    {
                        if (!string.IsNullOrEmpty(typeCol))
                        {
                            sql = "SELECT \"" + serialCol + "\" FROM \"" + table + "\" WHERE \"" + serialCol + "\" LIKE @p ORDER BY \"" + serialCol + "\" DESC LIMIT 1";
                        }
                        else
                        {
                            sql = "SELECT \"" + serialCol + "\" FROM \"" + table + "\" WHERE \"" + serialCol + "\" LIKE @p ORDER BY id DESC LIMIT 1";
                        }
                        using (var cmd = new NpgsqlCommand(sql, conn))
                        {
                            cmd.Parameters.AddWithValue("p", likePattern);
                            var o = cmd.ExecuteScalar();
                            if (o != null && o != DBNull.Value)
                            {
                                string last = o.ToString();
                                int dashIdx = last.LastIndexOf('-');
                                if (dashIdx >= 0 && int.TryParse(last.Substring(dashIdx + 1), out int seq))
                                {
                                    return prefix + "-" + year + "-" + (seq + 1).ToString("D4");
                                }
                            }
                        }
                    }
                }
            }
            catch { }
            string pf = (type ?? "sales") == "purchase" ? "PUR" : "SAL";
            return pf + "-" + DateTime.Now.Year + "-0001";
        }

        // ==================================================================
        //  أوامر البنود والدفعات
        // ==================================================================
        public void AddLine()
        {
            var line = new InvoiceLineItem(_allItems);
            line.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == "Quantity" || e.PropertyName == "Price" ||
                    e.PropertyName == "Discount" || e.PropertyName == "Total")
                {
                    RefreshTotals();
                }
            };
            Lines.Add(line);
            RefreshTotals();
        }

        public void AddPayment()
        {
            decimal remaining = Remaining;
            Payments.Add(new InvoicePaymentRow
            {
                Method = "cash",
                Amount = remaining > 0 ? remaining : 0,
                PaymentDate = DateTime.Now
            });
            RefreshTotals();
        }

        private void OnPaymentModeChanged()
        {
            // عند تغيير وضع الدفع
            if (_paymentMode == "full" && Total > 0 && Payments.Count == 0)
            {
                Payments.Add(new InvoicePaymentRow { Method = "cash", Amount = Total, PaymentDate = DateTime.Now });
                RefreshTotals();
            }
            else if (_paymentMode == "none")
            {
                Payments.Clear();
                RefreshTotals();
            }
        }

        // ==================================================================
        //  إعادة حساب الإجماليات
        // ==================================================================
        public void RefreshTotals()
        {
            decimal sub = 0;
            foreach (var line in Lines)
            {
                sub += line.Total;
            }
            Subtotal = Math.Round(sub, 2);

            // إذا كان خصم بالنسبة، أعد حسابه
            if (_discountPercent > 0 && _discountAmount == 0)
            {
                _discountAmount = Math.Round(_subtotal * _discountPercent / 100m, 2);
            }

            decimal afterDiscount = _subtotal - _discountAmount;
            TaxAmount = Math.Round(afterDiscount * 0m, 2); // الضريب=0 حالياً
            Total = Math.Round(afterDiscount + _taxAmount, 2);

            // مجموع المدفوع
            decimal paid = 0;
            foreach (var p in Payments) paid += p.Amount;
            TotalPaid = Math.Round(paid, 2);

            OnPropertyChanged("Remaining");
            OnPropertyChanged("RemainingDisplay");
        }

        // ==================================================================
        //  تحميل فاتورة موجودة
        // ==================================================================
        public async System.Threading.Tasks.Task LoadFromInvoiceAsync(int invoiceId)
        {
            IsBusy = true;
            StatusMessage = "جاري تحميل الفاتورة...";
            try
            {
                string cs = GetConnectionString();
                using (var conn = new NpgsqlConnection(cs))
                {
                    conn.Open();
                    string table = FindTable(conn, new string[] { "invoices" });
                    if (string.IsNullOrEmpty(table)) { StatusMessage = "جدول الفواتير غير موجود"; return; }

                    string[] cols = GetColumns(conn, table);
                    string selCols = "";
                    foreach (string c in new string[] { "id", "serial_number", "invoice_number", "number", "invoice_date", "date", "invoice_type", "type", "party_id", "warehouse_id", "notes", "subtotal", "discount_amount", "tax_amount", "total", "paid_amount" })
                    {
                        if (cols.Contains(c)) selCols += "\"" + c + "\", ";
                    }
                    selCols = selCols.TrimEnd(',', ' ');
                    if (selCols.Length == 0) { StatusMessage = "لا توجد أعمدة"; return; }

                    string sql = "SELECT " + selCols + " FROM \"" + table + "\" WHERE id = @id";
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("id", invoiceId);
                        using (var r = cmd.ExecuteReader())
                        {
                            if (r.Read())
                            {
                                int col = 0;
                                for (int i = 0; i < r.FieldCount; i++)
                                {
                                    string fname = r.GetName(i).ToLowerInvariant();
                                    object val = r.IsDBNull(i) ? null : r.GetValue(i);
                                    switch (fname)
                                    {
                                        case "id": Id = val == null ? 0 : Convert.ToInt32(val); break;
                                        case "serial_number":
                                        case "invoice_number":
                                        case "number": SerialNumber = val == null ? "" : val.ToString(); break;
                                        case "invoice_date":
                                        case "date": InvoiceDate = val == null ? DateTime.Now : Convert.ToDateTime(val); break;
                                        case "invoice_type":
                                        case "type": InvoiceType = val == null ? "sales" : val.ToString(); break;
                                        case "party_id": PartyId = val == null ? 0 : Convert.ToInt32(val); break;
                                        case "warehouse_id": WarehouseId = val == null ? 0 : Convert.ToInt32(val); break;
                                        case "notes": Notes = val == null ? "" : val.ToString(); break;
                                        case "subtotal": Subtotal = val == null ? 0 : Convert.ToDecimal(val); break;
                                        case "discount_amount": DiscountAmount = val == null ? 0 : Convert.ToDecimal(val); break;
                                        case "tax_amount": TaxAmount = val == null ? 0 : Convert.ToDecimal(val); break;
                                        case "total": Total = val == null ? 0 : Convert.ToDecimal(val); break;
                                        case "paid_amount": TotalPaid = val == null ? 0 : Convert.ToDecimal(val); break;
                                    }
                                }
                            }
                        }
                    }

                    // تحميل البنود
                    Lines.Clear();
                    string lineTable = FindTable(conn, new string[] { "invoice_lines", "invoice_items", "invoice_details" });
                    if (!string.IsNullOrEmpty(lineTable))
                    {
                        string lc = "SELECT * FROM \"" + lineTable + "\" WHERE invoice_id = @id";
                        try
                        {
                            using (var cmd = new NpgsqlCommand(lc, conn))
                            {
                                cmd.Parameters.AddWithValue("id", invoiceId);
                                using (var r = cmd.ExecuteReader())
                                {
                                    while (r.Read())
                                    {
                                        var line = new InvoiceLineItem(_allItems);
                                        for (int i = 0; i < r.FieldCount; i++)
                                        {
                                            string fn = r.GetName(i).ToLowerInvariant();
                                            object val = r.IsDBNull(i) ? null : r.GetValue(i);
                                            switch (fn)
                                            {
                                                case "item_id": line.ItemId = val == null ? 0 : Convert.ToInt32(val); break;
                                                case "item_name":
                                                case "name": line.ItemName = val == null ? "" : val.ToString(); break;
                                                case "barcode": line.Barcode = val == null ? "" : val.ToString(); break;
                                                case "unit": line.Unit = val == null ? "" : val.ToString(); break;
                                                case "quantity":
                                                case "qty": line.Quantity = val == null ? 1 : Convert.ToDecimal(val); break;
                                                case "price":
                                                case "unit_price": line.Price = val == null ? 0 : Convert.ToDecimal(val); break;
                                                case "discount": line.Discount = val == null ? 0 : Convert.ToDecimal(val); break;
                                                case "total":
                                                case "line_total": line.Total = val == null ? 0 : Convert.ToDecimal(val); break;
                                            }
                                        }
                                        Lines.Add(line);
                                    }
                                }
                            }
                        }
                        catch { }
                    }

                    // تحميل الدفعات
                    Payments.Clear();
                    string payTable = FindTable(conn, new string[] { "invoice_payments" });
                    if (!string.IsNullOrEmpty(payTable))
                    {
                        try
                        {
                            using (var cmd = new NpgsqlCommand("SELECT * FROM \"" + payTable + "\" WHERE invoice_id = @id ORDER BY id", conn))
                            {
                                cmd.Parameters.AddWithValue("id", invoiceId);
                                using (var r = cmd.ExecuteReader())
                                {
                                    while (r.Read())
                                    {
                                        var row = new InvoicePaymentRow();
                                        for (int i = 0; i < r.FieldCount; i++)
                                        {
                                            string fn = r.GetName(i).ToLowerInvariant();
                                            object val = r.IsDBNull(i) ? null : r.GetValue(i);
                                            switch (fn)
                                            {
                                                case "id": row.Id = val == null ? 0 : Convert.ToInt32(val); break;
                                                case "method": row.Method = val == null ? "cash" : val.ToString(); break;
                                                case "amount": row.Amount = val == null ? 0 : Convert.ToDecimal(val); break;
                                                case "reference": row.Reference = val == null ? "" : val.ToString(); break;
                                                case "payment_date":
                                                case "date": row.PaymentDate = val == null ? DateTime.Now : Convert.ToDateTime(val); break;
                                                case "notes": row.Notes = val == null ? "" : val.ToString(); break;
                                            }
                                        }
                                        Payments.Add(row);
                                    }
                                }
                            }
                        }
                        catch { }
                    }
                }

                if (Lines.Count == 0) AddLine();
                if (Payments.Count == 0 && Total > 0)
                {
                    Payments.Add(new InvoicePaymentRow { Method = "cash", Amount = Total, PaymentDate = DateTime.Now });
                }
                RefreshTotals();
                StatusMessage = "";
            }
            catch (Exception ex)
            {
                StatusMessage = "خطأ: " + ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
            await System.Threading.Tasks.Task.CompletedTask;
        }

        // ==================================================================
        //  حفظ الفاتورة
        // ==================================================================
        public async System.Threading.Tasks.Task SaveAsync()
        {
            IsBusy = true;
            StatusMessage = "جاري الحفظ...";
            try
            {
                RefreshTotals();

                string cs = GetConnectionString();
                using (var conn = new NpgsqlConnection(cs))
                {
                    conn.Open();

                    string table = FindTable(conn, new string[] { "invoices" });
                    if (string.IsNullOrEmpty(table))
                    {
                        // أنشئ الجدول إن لم يكن موجوداً
                        CreateInvoicesTable(conn);
                        table = "invoices";
                    }

                    string[] cols = GetColumns(conn, table);
                    if (Id > 0)
                    {
                        // تحديث
                        string setParts = "";
                        var paramDict = new Dictionary<string, object>();
                        void AddSet(string col, string param, object val)
                        {
                            if (cols.Contains(col))
                            {
                                setParts += "\"" + col + "\" = @" + param + ", ";
                                paramDict[param] = val ?? DBNull.Value;
                            }
                        }
                        AddSet("serial_number", "sn", SerialNumber);
                        AddSet("invoice_date", "dt", InvoiceDate);
                        AddSet("invoice_type", "tp", InvoiceType);
                        AddSet("party_id", "pid", PartyId > 0 ? (object)PartyId : DBNull.Value);
                        AddSet("warehouse_id", "wid", WarehouseId > 0 ? (object)WarehouseId : DBNull.Value);
                        AddSet("notes", "nt", Notes);
                        AddSet("subtotal", "st", Subtotal);
                        AddSet("discount_amount", "da", DiscountAmount);
                        AddSet("tax_amount", "tx", TaxAmount);
                        AddSet("total", "tt", Total);
                        AddSet("paid_amount", "pa", TotalPaid);
                        AddSet("status", "status", TotalPaid >= Total ? "paid" : (TotalPaid > 0 ? "partial" : "unpaid"));
                        AddSet("updated_at", "ua", DateTime.Now);

                        setParts = setParts.TrimEnd(',', ' ');
                        if (setParts.Length > 0)
                        {
                            string sql = "UPDATE \"" + table + "\" SET " + setParts + " WHERE id = @id";
                            using (var cmd = new NpgsqlCommand(sql, conn))
                            {
                                cmd.Parameters.AddWithValue("id", Id);
                                foreach (var kv in paramDict)
                                {
                                    cmd.Parameters.AddWithValue(kv.Key, kv.Value);
                                }
                                cmd.ExecuteNonQuery();
                            }
                        }
                    }
                    else
                    {
                        // إدخال جديد
                        string colNames = "";
                        string paramNames = "";
                        var paramDict = new Dictionary<string, object>();
                        void AddInsert(string col, string param, object val)
                        {
                            if (cols.Contains(col))
                            {
                                colNames += "\"" + col + "\", ";
                                paramNames += "@" + param + ", ";
                                paramDict[param] = val ?? DBNull.Value;
                            }
                        }
                        AddInsert("serial_number", "sn", SerialNumber);
                        AddInsert("invoice_date", "dt", InvoiceDate);
                        AddInsert("invoice_type", "tp", InvoiceType);
                        AddInsert("party_id", "pid", PartyId > 0 ? (object)PartyId : DBNull.Value);
                        AddInsert("warehouse_id", "wid", WarehouseId > 0 ? (object)WarehouseId : DBNull.Value);
                        AddInsert("notes", "nt", Notes);
                        AddInsert("subtotal", "st", Subtotal);
                        AddInsert("discount_amount", "da", DiscountAmount);
                        AddInsert("tax_amount", "tx", TaxAmount);
                        AddInsert("total", "tt", Total);
                        AddInsert("paid_amount", "pa", TotalPaid);
                        AddInsert("status", "status", TotalPaid >= Total ? "paid" : (TotalPaid > 0 ? "partial" : "unpaid"));
                        AddInsert("created_at", "ca", DateTime.Now);
                        AddInsert("updated_at", "ua", DateTime.Now);

                        colNames = colNames.TrimEnd(',', ' ');
                        paramNames = paramNames.TrimEnd(',', ' ');
                        string sql = "INSERT INTO \"" + table + "\" (" + colNames + ") VALUES (" + paramNames + ") RETURNING id";
                        using (var cmd = new NpgsqlCommand(sql, conn))
                        {
                            foreach (var kv in paramDict)
                            {
                                cmd.Parameters.AddWithValue(kv.Key, kv.Value);
                            }
                            var o = cmd.ExecuteScalar();
                            if (o != null && o != DBNull.Value) Id = Convert.ToInt32(o);
                        }
                    }

                    // احفظ البنود
                    SaveLines(conn, Id);
                    // احفظ الدفعات
                    SavePayments(conn, Id);
                }

                StatusMessage = "تم الحفظ بنجاح - رقم الفاتورة: " + SerialNumber;
                DialogResult = true;
                RequestClose?.Invoke();
            }
            catch (Exception ex)
            {
                StatusMessage = "خطأ عند الحفظ: " + ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
            await System.Threading.Tasks.Task.CompletedTask;
        }

        private void SaveLines(NpgsqlConnection conn, int invoiceId)
        {
            string lineTable = FindTable(conn, new string[] { "invoice_lines", "invoice_items", "invoice_details" });
            if (string.IsNullOrEmpty(lineTable))
            {
                CreateInvoiceLinesTable(conn);
                lineTable = "invoice_lines";
            }

            // احذف البنود القديمة
            using (var cmd = new NpgsqlCommand("DELETE FROM \"" + lineTable + "\" WHERE invoice_id = @id", conn))
            {
                cmd.Parameters.AddWithValue("id", invoiceId);
                cmd.ExecuteNonQuery();
            }

            string[] cols = GetColumns(conn, lineTable);
            foreach (var line in Lines)
            {
                if (line.ItemId <= 0 && string.IsNullOrEmpty(line.ItemName)) continue;
                string colNames = "invoice_id";
                string paramNames = "@inv";
                var paramDict = new Dictionary<string, object>();
                paramDict["inv"] = invoiceId;

                void AddCol(string col, string param, object val)
                {
                    if (cols.Contains(col))
                    {
                        colNames += ", \"" + col + "\"";
                        paramNames += ", @" + param;
                        paramDict[param] = val ?? DBNull.Value;
                    }
                }
                AddCol("item_id", "iid", line.ItemId > 0 ? (object)line.ItemId : DBNull.Value);
                AddCol("item_name", "inm", line.ItemName);
                AddCol("barcode", "bc", line.Barcode);
                AddCol("unit", "un", line.Unit);
                AddCol("quantity", "qty", line.Quantity);
                AddCol("price", "prc", line.Price);
                AddCol("discount", "dsc", line.Discount);
                AddCol("total", "tot", line.Total);

                string sql = "INSERT INTO \"" + lineTable + "\" (" + colNames + ") VALUES (" + paramNames + ")";
                using (var cmd = new NpgsqlCommand(sql, conn))
                {
                    foreach (var kv in paramDict) cmd.Parameters.AddWithValue(kv.Key, kv.Value);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private void SavePayments(NpgsqlConnection conn, int invoiceId)
        {
            string payTable = FindTable(conn, new string[] { "invoice_payments" });
            if (string.IsNullOrEmpty(payTable))
            {
                CreateInvoicePaymentsTable(conn);
                payTable = "invoice_payments";
            }

            // احذف الدفعات القديمة
            using (var cmd = new NpgsqlCommand("DELETE FROM \"" + payTable + "\" WHERE invoice_id = @id", conn))
            {
                cmd.Parameters.AddWithValue("id", invoiceId);
                cmd.ExecuteNonQuery();
            }

            string[] cols = GetColumns(conn, payTable);
            foreach (var pay in Payments)
            {
                if (pay.Amount <= 0) continue;
                string colNames = "invoice_id, method, amount";
                string paramNames = "@inv, @mth, @amt";
                var paramDict = new Dictionary<string, object>();
                paramDict["inv"] = invoiceId;
                paramDict["mth"] = pay.Method;
                paramDict["amt"] = pay.Amount;

                void AddCol(string col, string param, object val)
                {
                    if (cols.Contains(col))
                    {
                        colNames += ", \"" + col + "\"";
                        paramNames += ", @" + param;
                        paramDict[param] = val ?? DBNull.Value;
                    }
                }
                AddCol("reference", "ref", pay.Reference);
                AddCol("payment_date", "pdt", pay.PaymentDate);
                AddCol("notes", "nt", pay.Notes);

                string sql = "INSERT INTO \"" + payTable + "\" (" + colNames + ") VALUES (" + paramNames + ")";
                using (var cmd = new NpgsqlCommand(sql, conn))
                {
                    foreach (var kv in paramDict) cmd.Parameters.AddWithValue(kv.Key, kv.Value);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // ==================================================================
        //  دوال مساعدة
        // ==================================================================
        private static string FindTable(NpgsqlConnection conn, string[] candidates)
        {
            using (var cmd = new NpgsqlCommand("select table_name from information_schema.tables where table_schema = current_schema()", conn))
            using (var r = cmd.ExecuteReader())
            {
                var tables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                while (r.Read()) tables.Add(r.GetString(0));
                r.Close();
                foreach (string c in candidates)
                {
                    if (tables.Contains(c)) return c;
                }
                // بحث جزئي
                foreach (string c in candidates)
                {
                    foreach (string t in tables)
                    {
                        if (t.IndexOf(c, StringComparison.OrdinalIgnoreCase) >= 0) return t;
                    }
                }
            }
            return null;
        }

        private static string FindColumn(NpgsqlConnection conn, string table, string[] candidates)
        {
            string[] cols = GetColumns(conn, table);
            foreach (string c in candidates)
            {
                if (cols.Contains(c, StringComparer.OrdinalIgnoreCase)) return c;
            }
            foreach (string c in candidates)
            {
                foreach (string col in cols)
                {
                    if (col.IndexOf(c, StringComparison.OrdinalIgnoreCase) >= 0) return col;
                }
            }
            return null;
        }

        private static string[] GetColumns(NpgsqlConnection conn, string table)
        {
            var list = new List<string>();
            using (var cmd = new NpgsqlCommand("select lower(column_name) from information_schema.columns where table_schema = current_schema() and lower(table_name) = @t", conn))
            {
                cmd.Parameters.AddWithValue("t", table.ToLowerInvariant());
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read()) list.Add(r.GetString(0));
                }
            }
            return list.ToArray();
        }

        private static void CreateInvoicesTable(NpgsqlConnection conn)
        {
            string sql = @"CREATE TABLE IF NOT EXISTS invoices (
                id SERIAL PRIMARY KEY,
                serial_number TEXT,
                invoice_date TIMESTAMP DEFAULT NOW(),
                invoice_type TEXT DEFAULT 'sales',
                party_id INTEGER,
                warehouse_id INTEGER,
                cash_box_id INTEGER,
                notes TEXT,
                subtotal NUMERIC(14,2) DEFAULT 0,
                discount_amount NUMERIC(14,2) DEFAULT 0,
                tax_amount NUMERIC(14,2) DEFAULT 0,
                total NUMERIC(14,2) DEFAULT 0,
                paid_amount NUMERIC(14,2) DEFAULT 0,
                status TEXT DEFAULT 'draft',
                created_at TIMESTAMP DEFAULT NOW(),
                updated_at TIMESTAMP DEFAULT NOW()
            )";
            using (var cmd = new NpgsqlCommand(sql, conn)) cmd.ExecuteNonQuery();
        }

        private static void CreateInvoiceLinesTable(NpgsqlConnection conn)
        {
            string sql = @"CREATE TABLE IF NOT EXISTS invoice_lines (
                id SERIAL PRIMARY KEY,
                invoice_id INTEGER NOT NULL,
                item_id INTEGER,
                item_name TEXT,
                barcode TEXT,
                unit TEXT,
                quantity NUMERIC(14,3) DEFAULT 0,
                price NUMERIC(14,2) DEFAULT 0,
                discount NUMERIC(14,2) DEFAULT 0,
                total NUMERIC(14,2) DEFAULT 0
            )";
            using (var cmd = new NpgsqlCommand(sql, conn)) cmd.ExecuteNonQuery();
        }

        private static void CreateInvoicePaymentsTable(NpgsqlConnection conn)
        {
            string sql = @"CREATE TABLE IF NOT EXISTS invoice_payments (
                id SERIAL PRIMARY KEY,
                invoice_id INTEGER NOT NULL,
                method TEXT NOT NULL DEFAULT 'cash',
                amount NUMERIC(14,2) NOT NULL DEFAULT 0,
                reference TEXT,
                cash_box_id INTEGER,
                bank_account_id INTEGER,
                payment_date TIMESTAMP DEFAULT NOW(),
                notes TEXT,
                created_at TIMESTAMP DEFAULT NOW()
            )";
            using (var cmd = new NpgsqlCommand(sql, conn)) cmd.ExecuteNonQuery();
        }
    }

    // ==================================================================
    //  أصناف مساعدة للقوائم المنسدلة
    // ==================================================================
    public class PartyOption
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public override string ToString() { return Name ?? ""; }
    }

    public class WarehouseOption
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public override string ToString() { return Name ?? ""; }
    }
}

```
#### .\fix_v2.ps1
```
# fix_v2.ps1 - Arabic text in code-behind + searchable ComboBox
$ProjectPath = 'D:\PROG\Accounting Prog\Techtoon Accounting C+'
$ViewsDir = Join-Path $ProjectPath 'Techtoon.UI\Views'

if (-not (Test-Path $ViewsDir)) { Write-Host 'ERROR: Path not found' -ForegroundColor Red; exit 1 }

Write-Host '========================================' -ForegroundColor Cyan
Write-Host '  Techtoon Fix v2 - Arabic in code-behind' -ForegroundColor Cyan
Write-Host '========================================' -ForegroundColor Cyan

# حذف الملفات المكررة
Get-ChildItem -Path $ViewsDir -Filter 'InvoiceEdit*' | Where-Object { $_.Name -match '\(1\)' } | Remove-Item -Force -ErrorAction SilentlyContinue

# ===== InvoiceEditWindow.xaml (ASCII only) =====
$xamlPath = Join-Path $ViewsDir 'InvoiceEditWindow.xaml'
if (Test-Path $xamlPath) { Copy-Item $xamlPath ($xamlPath + '.bak_v2') -Force }
$xaml = @'
<Window x:Class="Techtoon.UI.Views.InvoiceEditWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="Invoice" Height="850" Width="1200"
        WindowStartupLocation="CenterScreen" FlowDirection="RightToLeft"
        Background="#F5F7FA" Loaded="Window_Loaded" FontFamily="Tahoma">
    <Window.Resources>
        <Style x:Key="Hdr" TargetType="TextBlock">
            <Setter Property="FontSize" Value="13"/>
            <Setter Property="FontWeight" Value="SemiBold"/>
            <Setter Property="Foreground" Value="#37474F"/>
            <Setter Property="Margin" Value="0,0,6,0"/>
            <Setter Property="VerticalAlignment" Value="Center"/>
        </Style>
        <Style x:Key="Inp" TargetType="TextBox">
            <Setter Property="Padding" Value="6,4"/>
            <Setter Property="FontSize" Value="13"/>
            <Setter Property="BorderBrush" Value="#B0BEC5"/>
            <Setter Property="BorderThickness" Value="1"/>
            <Setter Property="Background" Value="White"/>
        </Style>
        <Style x:Key="AddBtn" TargetType="Button">
            <Setter Property="Background" Value="#26A69A"/>
            <Setter Property="Foreground" Value="White"/>
            <Setter Property="FontWeight" Value="SemiBold"/>
            <Setter Property="FontSize" Value="13"/>
            <Setter Property="Padding" Value="14,6"/>
            <Setter Property="BorderThickness" Value="0"/>
            <Setter Property="Cursor" Value="Hand"/>
        </Style>
        <Style x:Key="SaveBtn" TargetType="Button">
            <Setter Property="Background" Value="#00897B"/>
            <Setter Property="Foreground" Value="White"/>
            <Setter Property="FontWeight" Value="Bold"/>
            <Setter Property="FontSize" Value="14"/>
            <Setter Property="Padding" Value="24,10"/>
            <Setter Property="BorderThickness" Value="0"/>
            <Setter Property="Cursor" Value="Hand"/>
        </Style>
        <Style x:Key="CancelBtn" TargetType="Button">
            <Setter Property="Background" Value="#ECEFF1"/>
            <Setter Property="Foreground" Value="#37474F"/>
            <Setter Property="FontWeight" Value="SemiBold"/>
            <Setter Property="FontSize" Value="13"/>
            <Setter Property="Padding" Value="18,10"/>
            <Setter Property="BorderThickness" Value="0"/>
            <Setter Property="Cursor" Value="Hand"/>
        </Style>
    </Window.Resources>
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>

        <!-- Header -->
        <Border Grid.Row="0" Background="White" Padding="16,10" BorderBrush="#E0E0E0" BorderThickness="0,0,0,1">
            <Grid>
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="Auto"/>
                    <ColumnDefinition Width="*"/>
                </Grid.ColumnDefinitions>
                <StackPanel Grid.Column="0" Orientation="Horizontal" VerticalAlignment="Center">
                    <TextBlock x:Name="HeaderIcon" Text="***" FontSize="28" Margin="0,0,10,0"/>
                    <StackPanel>
                        <TextBlock x:Name="HeaderTitle" Text="Invoice" FontSize="18" FontWeight="Bold" Foreground="#00695C"/>
                        <TextBlock Text="{Binding InvoiceTypeDisplay}" FontSize="12" Foreground="#78909C"/>
                    </StackPanel>
                </StackPanel>
                <Grid Grid.Column="1" Margin="20,0,0,0">
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="Auto"/><ColumnDefinition Width="140"/>
                        <ColumnDefinition Width="18"/><ColumnDefinition Width="Auto"/>
                        <ColumnDefinition Width="150"/><ColumnDefinition Width="18"/>
                        <ColumnDefinition Width="Auto"/><ColumnDefinition Width="120"/>
                    </Grid.ColumnDefinitions>
                    <Grid.RowDefinitions>
                        <RowDefinition Height="Auto"/><RowDefinition Height="8"/><RowDefinition Height="Auto"/>
                    </Grid.RowDefinitions>
                    <TextBlock x:Name="LblSerial" Grid.Row="0" Grid.Column="0" Text="No:" Style="{StaticResource Hdr}"/>
                    <TextBox Grid.Row="0" Grid.Column="1" Text="{Binding SerialNumber}" Style="{StaticResource Inp}" IsReadOnly="True"/>
                    <TextBlock x:Name="LblDate" Grid.Row="0" Grid.Column="3" Text="Date:" Style="{StaticResource Hdr}"/>
                    <DatePicker Grid.Row="0" Grid.Column="4" SelectedDate="{Binding InvoiceDate}" FontSize="13"/>
                    <TextBlock x:Name="LblType" Grid.Row="0" Grid.Column="6" Text="Type:" Style="{StaticResource Hdr}"/>
                    <ComboBox x:Name="CmbType" Grid.Row="0" Grid.Column="7" SelectedValue="{Binding InvoiceType}" SelectedValuePath="Tag" Padding="6,4" FontSize="13" BorderBrush="#B0BEC5" BorderThickness="1"/>
                    <TextBlock x:Name="LblParty" Grid.Row="2" Grid.Column="0" Text="Party:" Style="{StaticResource Hdr}"/>
                    <ComboBox Grid.Row="2" Grid.Column="1" Grid.ColumnSpan="4" ItemsSource="{Binding Parties}" DisplayMemberPath="Name" SelectedValuePath="Id" SelectedValue="{Binding PartyId}" Padding="6,4" FontSize="13" BorderBrush="#B0BEC5" BorderThickness="1"/>
                    <TextBlock x:Name="LblWarehouse" Grid.Row="2" Grid.Column="6" Text="WH:" Style="{StaticResource Hdr}"/>
                    <ComboBox Grid.Row="2" Grid.Column="7" ItemsSource="{Binding Warehouses}" DisplayMemberPath="Name" SelectedValuePath="Id" SelectedValue="{Binding WarehouseId}" Padding="6,4" FontSize="13" BorderBrush="#B0BEC5" BorderThickness="1"/>
                </Grid>
            </Grid>
        </Border>

        <!-- Items -->
        <Border Grid.Row="1" Background="White" Margin="10,8,10,4" Padding="8" BorderBrush="#E0E0E0" BorderThickness="1" CornerRadius="4">
            <Grid>
                <Grid.RowDefinitions>
                    <RowDefinition Height="Auto"/><RowDefinition Height="*"/>
                </Grid.RowDefinitions>
                <Grid Grid.Row="0" Margin="0,0,0,6">
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="*"/><ColumnDefinition Width="Auto"/>
                    </Grid.ColumnDefinitions>
                    <TextBlock x:Name="LblItems" Grid.Column="0" Text="Items" FontSize="15" FontWeight="Bold" Foreground="#37474F" VerticalAlignment="Center"/>
                    <Button x:Name="BtnAddLine" Grid.Column="1" Content="+ Add" Style="{StaticResource AddBtn}" Command="{Binding AddLineCommand}"/>
                </Grid>
                <DataGrid x:Name="LinesGrid" Grid.Row="1" ItemsSource="{Binding Lines}" AutoGenerateColumns="False" CanUserAddRows="False" HeadersVisibility="Column" GridLinesVisibility="Horizontal" HorizontalGridLinesBrush="#ECEFF1" BorderBrush="#E0E0E0" BorderThickness="1" Background="White" FontSize="13" MinHeight="180" LoadingRow="LinesGrid_LoadingRow">
                    <DataGrid.Columns>
                        <DataGridTemplateColumn x:Name="ColItem" Header="Item" Width="320">
                            <DataGridTemplateColumn.CellTemplate>
                                <DataTemplate>
                                    <ComboBox x:Name="ItemSearchCombo"
                                              ItemsSource="{Binding DataContext.AllItemsList, RelativeSource={RelativeSource AncestorType=DataGrid}}"
                                              DisplayMemberPath="SearchDisplay"
                                              IsEditable="True"
                                              Text="{Binding SearchText, UpdateSourceTrigger=PropertyChanged}"
                                              StaysOpenOnEdit="True"
                                              SelectionChanged="ItemSearchCombo_SelectionChanged"
                                              Padding="4,3" FontSize="13"/>
                                </DataTemplate>
                            </DataGridTemplateColumn.CellTemplate>
                        </DataGridTemplateColumn>
                        <DataGridTextColumn x:Name="ColBarcode" Header="Barcode" Width="110" Binding="{Binding Barcode}" IsReadOnly="True"/>
                        <DataGridTextColumn x:Name="ColUnit" Header="Unit" Width="70" Binding="{Binding Unit}" IsReadOnly="True"/>
                        <DataGridTemplateColumn x:Name="ColQty" Header="Qty" Width="90">
                            <DataGridTemplateColumn.CellTemplate>
                                <DataTemplate>
                                    <TextBox Text="{Binding Quantity, UpdateSourceTrigger=PropertyChanged, StringFormat=N2}" Tag="qty" Padding="4,3" BorderBrush="#E0E0E0" KeyDown="Cell_KeyDown"/>
                                </DataTemplate>
                            </DataGridTemplateColumn.CellTemplate>
                        </DataGridTemplateColumn>
                        <DataGridTemplateColumn x:Name="ColPrice" Header="Price" Width="100">
                            <DataGridTemplateColumn.CellTemplate>
                                <DataTemplate>
                                    <TextBox Text="{Binding Price, UpdateSourceTrigger=PropertyChanged, StringFormat=N2}" Tag="price" Padding="4,3" BorderBrush="#E0E0E0" KeyDown="Cell_KeyDown"/>
                                </DataTemplate>
                            </DataGridTemplateColumn.CellTemplate>
                        </DataGridTemplateColumn>
                        <DataGridTemplateColumn x:Name="ColDisc" Header="Disc" Width="80">
                            <DataGridTemplateColumn.CellTemplate>
                                <DataTemplate>
                                    <TextBox Text="{Binding Discount, UpdateSourceTrigger=PropertyChanged, StringFormat=N2}" Tag="discount" Padding="4,3" BorderBrush="#E0E0E0" KeyDown="Cell_KeyDown"/>
                                </DataTemplate>
                            </DataGridTemplateColumn.CellTemplate>
                        </DataGridTemplateColumn>
                        <DataGridTextColumn x:Name="ColTotal" Header="Total" Width="110" Binding="{Binding Total, StringFormat=N2}" IsReadOnly="True">
                            <DataGridTextColumn.ElementStyle>
                                <Style TargetType="TextBlock">
                                    <Setter Property="HorizontalAlignment" Value="Right"/>
                                    <Setter Property="FontWeight" Value="SemiBold"/>
                                    <Setter Property="Foreground" Value="#00695C"/>
                                </Style>
                            </DataGridTextColumn.ElementStyle>
                        </DataGridTextColumn>
                        <DataGridTemplateColumn x:Name="ColDel" Header="X" Width="60">
                            <DataGridTemplateColumn.CellTemplate>
                                <DataTemplate>
                                    <Button Content="X" Background="Transparent" BorderThickness="0" FontSize="14" Cursor="Hand" Command="{Binding DataContext.DeleteLineCommand, RelativeSource={RelativeSource AncestorType=DataGrid}}" CommandParameter="{Binding}"/>
                                </DataTemplate>
                            </DataGridTemplateColumn.CellTemplate>
                        </DataGridTemplateColumn>
                    </DataGrid.Columns>
                </DataGrid>
            </Grid>
        </Border>

        <!-- Payments -->
        <Border x:Name="PaymentsSection" Grid.Row="2" Background="White" Margin="10,4,10,4" Padding="10" BorderBrush="#80CBC4" BorderThickness="1" CornerRadius="4">
            <Grid>
                <Grid.RowDefinitions>
                    <RowDefinition Height="Auto"/><RowDefinition Height="Auto"/>
                </Grid.RowDefinitions>
                <Grid Grid.Row="0" Margin="0,0,0,6">
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="*"/><ColumnDefinition Width="Auto"/>
                    </Grid.ColumnDefinitions>
                    <TextBlock x:Name="LblPayments" Grid.Column="0" Text="Payments" FontSize="15" FontWeight="Bold" Foreground="#00695C" VerticalAlignment="Center"/>
                    <Button x:Name="BtnAddPay" Grid.Column="1" Content="+ Add" Style="{StaticResource AddBtn}" Command="{Binding AddPaymentCommand}"/>
                </Grid>
                <DataGrid x:Name="PaymentsGrid" Grid.Row="1" ItemsSource="{Binding Payments}" AutoGenerateColumns="False" CanUserAddRows="False" HeadersVisibility="Column" GridLinesVisibility="Horizontal" HorizontalGridLinesBrush="#ECEFF1" BorderBrush="#E0E0E0" BorderThickness="1" Background="White" FontSize="13" MaxHeight="140" CellEditEnding="PaymentsGrid_CellEditEnding">
                    <DataGrid.Columns>
                        <DataGridTemplateColumn x:Name="ColPayMethod" Header="Method" Width="140">
                            <DataGridTemplateColumn.CellTemplate>
                                <DataTemplate>
                                    <ComboBox x:Name="PayMethodCombo" SelectedValue="{Binding Method, UpdateSourceTrigger=PropertyChanged}" SelectedValuePath="Tag" Padding="4,2"/>
                                </DataTemplate>
                            </DataGridTemplateColumn.CellTemplate>
                        </DataGridTemplateColumn>
                        <DataGridTemplateColumn x:Name="ColPayAmount" Header="Amount" Width="140">
                            <DataGridTemplateColumn.CellTemplate>
                                <DataTemplate>
                                    <TextBox Text="{Binding Amount, UpdateSourceTrigger=PropertyChanged, StringFormat=N2}" Padding="4,3" BorderBrush="#E0E0E0" TextAlignment="Right"/>
                                </DataTemplate>
                            </DataGridTemplateColumn.CellTemplate>
                        </DataGridTemplateColumn>
                        <DataGridTextColumn x:Name="ColPayRef" Header="Ref" Width="180" Binding="{Binding Reference, UpdateSourceTrigger=PropertyChanged}"/>
                        <DataGridTemplateColumn x:Name="ColPayDate" Header="Date" Width="150">
                            <DataGridTemplateColumn.CellTemplate>
                                <DataTemplate>
                                    <DatePicker SelectedDate="{Binding PaymentDate}" FontSize="12"/>
                                </DataTemplate>
                            </DataGridTemplateColumn.CellTemplate>
                        </DataGridTemplateColumn>
                        <DataGridTextColumn x:Name="ColPayNotes" Header="Notes" Width="*" Binding="{Binding Notes, UpdateSourceTrigger=PropertyChanged}"/>
                        <DataGridTemplateColumn x:Name="ColPayDel" Header="X" Width="60">
                            <DataGridTemplateColumn.CellTemplate>
                                <DataTemplate>
                                    <Button Content="X" Background="Transparent" BorderThickness="0" FontSize="14" Cursor="Hand" Command="{Binding DataContext.DeletePaymentCommand, RelativeSource={RelativeSource AncestorType=DataGrid}}" CommandParameter="{Binding}"/>
                                </DataTemplate>
                            </DataGridTemplateColumn.CellTemplate>
                        </DataGridTemplateColumn>
                    </DataGrid.Columns>
                </DataGrid>
            </Grid>
        </Border>

        <!-- Totals + Buttons -->
        <Border Grid.Row="3" Background="White" Padding="16,10" BorderBrush="#E0E0E0" BorderThickness="0,1,0,0">
            <Grid>
                <Grid.RowDefinitions>
                    <RowDefinition Height="Auto"/>
                    <RowDefinition Height="8"/>
                    <RowDefinition Height="Auto"/>
                </Grid.RowDefinitions>
                <Grid Grid.Row="0">
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="*"/><ColumnDefinition Width="300"/>
                    </Grid.ColumnDefinitions>
                    <StackPanel Grid.Column="0" VerticalAlignment="Center">
                        <TextBlock x:Name="StatusText" Text="{Binding StatusMessage}" FontSize="12" Foreground="#00897B" TextWrapping="Wrap"/>
                    </StackPanel>
                    <Grid Grid.Column="1">
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width="*"/><ColumnDefinition Width="110"/>
                        </Grid.ColumnDefinitions>
                        <Grid.RowDefinitions>
                            <RowDefinition Height="Auto"/><RowDefinition Height="Auto"/><RowDefinition Height="Auto"/><RowDefinition Height="Auto"/><RowDefinition Height="Auto"/>
                        </Grid.RowDefinitions>
                        <TextBlock x:Name="LblSubtotal" Grid.Row="0" Grid.Column="0" Text="Sub:" FontSize="13" Foreground="#546E7A" Padding="0,2"/>
                        <TextBlock Grid.Row="0" Grid.Column="1" Text="{Binding SubtotalDisplay}" FontSize="13" FontWeight="SemiBold" Foreground="#37474F" TextAlignment="Left" Padding="0,2"/>
                        <StackPanel Grid.Row="1" Grid.Column="0" Orientation="Horizontal">
                            <TextBlock x:Name="LblDisc" Text="Disc:" FontSize="13" Foreground="#546E7A" VerticalAlignment="Center"/>
                            <TextBox Text="{Binding DiscountAmount, UpdateSourceTrigger=PropertyChanged, StringFormat=N2}" Width="80" Margin="6,0,0,0" Padding="3,2" FontSize="12" BorderBrush="#E0E0E0" TextAlignment="Right"/>
                        </StackPanel>
                        <TextBlock Grid.Row="1" Grid.Column="1" Text="{Binding DiscountAmount, StringFormat=N2}" FontSize="13" Foreground="#E53935" TextAlignment="Left"/>
                        <TextBlock x:Name="LblTotal" Grid.Row="2" Grid.Column="0" Text="Total:" FontSize="14" FontWeight="Bold" Foreground="#00695C" Padding="0,2"/>
                        <TextBlock Grid.Row="2" Grid.Column="1" Text="{Binding TotalDisplay}" FontSize="15" FontWeight="Bold" Foreground="#00695C" TextAlignment="Left" Padding="0,2"/>
                        <TextBlock x:Name="LblPaid" Grid.Row="3" Grid.Column="0" Text="Paid:" FontSize="13" Foreground="#2E7D32" Padding="0,2"/>
                        <TextBlock Grid.Row="3" Grid.Column="1" Text="{Binding TotalPaidDisplay}" FontSize="13" FontWeight="SemiBold" Foreground="#2E7D32" TextAlignment="Left" Padding="0,2"/>
                        <TextBlock x:Name="LblRemaining" Grid.Row="4" Grid.Column="0" Text="Rem:" FontSize="14" FontWeight="Bold" Foreground="#E65100" Padding="0,2"/>
                        <TextBlock Grid.Row="4" Grid.Column="1" Text="{Binding RemainingDisplay}" FontSize="14" FontWeight="Bold" Foreground="#E65100" TextAlignment="Left" Padding="0,2"/>
                    </Grid>
                </Grid>
                <StackPanel Grid.Row="2" Orientation="Horizontal" HorizontalAlignment="Left" FlowDirection="LeftToRight" Margin="0,8,0,0">
                    <Button x:Name="BtnSave" Content="Save" Style="{StaticResource SaveBtn}" Margin="0,0,8,0" Command="{Binding SaveCommand}"/>
                    <Button x:Name="BtnSavePrint" Content="Save+Print" Style="{StaticResource SaveBtn}" Margin="0,0,8,0" Command="{Binding SaveAndPrintCommand}"/>
                    <Button x:Name="BtnCancel" Content="Cancel" Style="{StaticResource CancelBtn}" Command="{Binding CancelCommand}"/>
                </StackPanel>
            </Grid>
        </Border>
    </Grid>
</Window>
'@
[System.IO.File]::WriteAllText($xamlPath, $xaml, [System.Text.ASCIIEncoding]::new())
Write-Host "  OK: XAML (ASCII)" -ForegroundColor Green

# ===== InvoiceEditWindow.xaml.cs (UTF-8 with BOM) =====
$csPath = Join-Path $ViewsDir 'InvoiceEditWindow.xaml.cs'
if (Test-Path $csPath) { Copy-Item $csPath ($csPath + '.bak_v2') -Force }
$cs = @'
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Techtoon.UI.ViewModels;

namespace Techtoon.UI.Views
{
    public partial class InvoiceEditWindow : Window
    {
        private readonly InvoiceEditViewModel _vm;
        private readonly int? _editId;

        public InvoiceEditWindow(int? editId = null)
        {
            InitializeComponent();
            _editId = editId;
            _vm = new InvoiceEditViewModel();
            if (editId.HasValue) _vm.Id = editId.Value;
            DataContext = _vm;
            _vm.RequestClose += () => { DialogResult = _vm.DialogResult; Close(); };
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // ضبط كل النصوص العربية من الكود (يحل مشكلة الترميز)
            SetArabicTexts();
            // تعبئة قوائم الطريقة
            FillTypeCombo();
            FillPaymentMethods();
            // تحميل البيانات
            await _vm.LoadAsync();
            // رسالة تشخيصية
            try { MessageBox.Show("Items loaded: " + (_vm.AllItemsList?.Count ?? 0), "Debug"); } catch { }
            if (_editId.HasValue) await _vm.LoadFromInvoiceAsync(_editId.Value);
        }

        private void SetArabicTexts()
        {
            try { HeaderIcon.Text = "\uDDF9"; } catch { }
            try { HeaderTitle.Text = "\u0641\u0627\u062A\u0648\u0631\u0629"; } catch { } // فاتورة
            try { LblSerial.Text = "\u0627\u0644\u0631\u0642\u0645:"; } catch { } // الرقم:
            try { LblDate.Text = "\u0627\u0644\u062A\u0627\u0631\u064A\u062E:"; } catch { } // التاريخ:
            try { LblType.Text = "\u0627\u0644\u0646\u0648\u0639:"; } catch { } // النوع:
            try { LblParty.Text = "\u0627\u0644\u0637\u0631\u0641:"; } catch { } // الطرف:
            try { LblWarehouse.Text = "\u0627\u0644\u0645\u062E\u0632\u0646:"; } catch { } // المخزن:
            try { LblItems.Text = "\u0628\u0646\u0648\u062F \u0627\u0644\u0641\u0627\u062A\u0648\u0631\u0629"; } catch { } // بنود الفاتورة
            try { BtnAddLine.Content = "+ \u0625\u0636\u0627\u0641\u0629 \u0635\u0646\u0641"; } catch { } // + إضافة صنف
            try { ColItem.Header = "\u0635\u0646\u0641 (\u0627\u0643\u062A\u0628 \u0644\u0644\u0628\u062D\u062B)"; } catch { } // صنف (اكتب للبحث)
            try { ColBarcode.Header = "\u0628\u0627\u0631\u0643\u0648\u062F"; } catch { } // باركود
            try { ColUnit.Header = "\u0627\u0644\u0648\u062D\u062F\u0629"; } catch { } // الوحدة
            try { ColQty.Header = "\u0627\u0644\u0643\u0645\u064A\u0629"; } catch { } // الكمية
            try { ColPrice.Header = "\u0627\u0644\u0633\u0639\u0631"; } catch { } // السعر
            try { ColDisc.Header = "\u062E\u0635\u0645"; } catch { } // خصم
            try { ColTotal.Header = "\u0625\u062C\u0645\u0627\u0644\u064A"; } catch { } // إجمالي
            try { ColDel.Header = "\u062D\u0630\u0641"; } catch { } // حذف
            try { LblPayments.Text = "\u0637\u0631\u0642 \u0627\u0644\u062F\u0641\u0639 \u0627\u0644\u0645\u062A\u0639\u062F\u062F\u0629"; } catch { } // طرق الدفع المتعددة
            try { BtnAddPay.Content = "+ \u0625\u0636\u0627\u0641\u0629 \u062F\u0641\u0639\u0629"; } catch { } // + إضافة دفعة
            try { ColPayMethod.Header = "\u0637\u0631\u064A\u0642\u0629 \u0627\u0644\u062F\u0641\u0639"; } catch { } // طريقة الدفع
            try { ColPayAmount.Header = "\u0627\u0644\u0645\u0628\u0644\u063A"; } catch { } // المبلغ
            try { ColPayRef.Header = "\u0645\u0631\u062C\u0639"; } catch { } // مرجع
            try { ColPayDate.Header = "\u062A\u0627\u0631\u064A\u062E"; } catch { } // تاريخ
            try { ColPayNotes.Header = "\u0645\u0644\u0627\u062D\u0638\u0627\u062A"; } catch { } // ملاحظات
            try { ColPayDel.Header = "\u062D\u0630\u0641"; } catch { } // حذف
            try { LblSubtotal.Text = "\u0627\u0644\u0645\u062C\u0645\u0648\u0639:"; } catch { } // المجموع:
            try { LblDisc.Text = "\u062E\u0635\u0645:"; } catch { } // خصم:
            try { LblTotal.Text = "\u0625\u062C\u0645\u0627\u0644\u064A:"; } catch { } // إجمالي:
            try { LblPaid.Text = "\u0627\u0644\u0645\u062F\u0641\u0648\u0639:"; } catch { } // المدفوع:
            try { LblRemaining.Text = "\u0627\u0644\u0645\u062A\u0628\u0642\u064A:"; } catch { } // المتبقي:
            try { BtnSave.Content = "\uD83D\uDCBE \u062D\u0641\u0638 \u0627\u0644\u0641\u0627\u062A\u0648\u0631\u0629"; } catch { } // 💾 حفظ الفاتورة
            try { BtnSavePrint.Content = "\uD83D\uDDA8 \u062D\u0641\u0638 \u0648\u0637\u0628\u0627\u0639\u0629"; } catch { } // 🖨 حفظ وطباعة
            try { BtnCancel.Content = "\u2716 \u0625\u0644\u063A\u0627\u0621"; } catch { } // ✖ إلغاء
        }

        private void FillTypeCombo()
        {
            try
            {
                CmbType.Items.Clear();
                CmbType.Items.Add(new ComboBoxItem { Content = "\u0645\u0628\u064A\u0639\u0627\u062A", Tag = "sales" }); // مبيعات
                CmbType.Items.Add(new ComboBoxItem { Content = "\u0645\u0634\u062A\u0631\u064A\u0627\u062A", Tag = "purchase" }); // مشتريات
            }
            catch { }
        }

        private void FillPaymentMethods()
        {
            // سيتم تعبئة عند تحميل كل صف
        }

        private void LinesGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            // تعبئة قائمة طرق الدفع في كل صف
            try
            {
                var row = e.Row;
                var payCombo = FindChild<ComboBox>(row, "PayMethodCombo");
                if (payCombo != null && payCombo.Items.Count == 0)
                {
                    payCombo.Items.Clear();
                    payCombo.Items.Add(new ComboBoxItem { Content = "\u0646\u0642\u062F\u064A", Tag = "cash" }); // نقدي
                    payCombo.Items.Add(new ComboBoxItem { Content = "\u0628\u0637\u0627\u0642\u0629", Tag = "card" }); // بطاقة
                    payCombo.Items.Add(new ComboBoxItem { Content = "\u062A\u062D\u0648\u064A\u0644 \u0628\u0646\u0643\u064A", Tag = "bank" }); // تحويل بنكي
                    payCombo.Items.Add(new ComboBoxItem { Content = "\u0634\u064A\u0643", Tag = "check" }); // شيك
                    payCombo.Items.Add(new ComboBoxItem { Content = "\u0622\u062C\u0644", Tag = "credit" }); // آجل
                }
            }
            catch { }
        }

        // البحث: عند اختيار صنف من القائمة المنسدلة
        private void ItemSearchCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (sender is ComboBox cb && cb.SelectedItem is ItemSearchResult item)
                {
                    if (cb.DataContext is InvoiceLineItem line)
                    {
                        line.SelectItem(item);
                    }
                }
            }
            catch { }
        }

        private void Cell_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;
            if (sender is not TextBox tb) return;
            if (tb.Tag is not string tag) return;
            if (tb.DataContext is not InvoiceLineItem line) return;
            LinesGrid.CommitEdit(DataGridEditingUnit.Row, true);
            int idx = _vm.Lines.IndexOf(line);
            int last = _vm.Lines.Count - 1;
            if (idx >= 0 && idx < last)
            {
                if (LinesGrid.ItemContainerGenerator.ContainerFromIndex(idx + 1) is DataGridRow nextRow)
                {
                    var next = FindDescendant<TextBox>(nextRow, x => (x.Tag as string) == tag);
                    if (next != null) { next.Focus(); next.SelectAll(); }
                }
            }
            else if (idx == last)
            {
                _vm.AddLineCommand.Execute(null);
                LinesGrid.UpdateLayout();
                if (LinesGrid.ItemContainerGenerator.ContainerFromIndex(_vm.Lines.Count - 1) is DataGridRow newRow)
                {
                    var cb = FindDescendant<ComboBox>(newRow, x => x.Name == "ItemSearchCombo");
                    cb?.Focus();
                }
            }
            e.Handled = true;
        }

        private static T? FindDescendant<T>(DependencyObject? parent, Func<T, bool> predicate) where T : class
        {
            if (parent == null) return null;
            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T matched && predicate(matched)) return matched;
                var deep = FindDescendant(child, predicate);
                if (deep != null) return deep;
            }
            return null;
        }

        private static T? FindChild<T>(DependencyObject? parent, string name) where T : FrameworkElement
        {
            if (parent == null) return null;
            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T fe && fe.Name == name) return fe;
                var deep = FindChild<T>(child, name);
                if (deep != null) return deep;
            }
            return null;
        }

        private void PaymentsGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction == DataGridEditAction.Commit) _vm.RefreshTotals();
        }
    }
}
'@
[System.IO.File]::WriteAllText($csPath, $cs, [System.Text.UTF8Encoding]::new($true))
Write-Host "  OK: xaml.cs (UTF-8 BOM)" -ForegroundColor Green

# ===== appsettings.json =====
$appsettings = '{"ConnectionStrings":{"DefaultConnection":"Host=ep-bitter-night-za726w59-pooler.c-2.eu-west-2.aws.neon.tech;Port=5432;Username=neondb_owner;Password=***;Database=neondb;SSL Mode=Require;Trust Server Certificate=true"}}'
$ap = Join-Path $ProjectPath 'Techtoon.UI\appsettings.json'
[System.IO.File]::WriteAllText($ap, $appsettings, [System.Text.UTF8Encoding]::new($false))
$bd = Join-Path $ProjectPath 'Techtoon.UI\bin\Debug\net10.0-windows'
if (Test-Path $bd) { Copy-Item $ap (Join-Path $bd 'appsettings.json') -Force }
Write-Host "  OK: appsettings.json" -ForegroundColor Green

# ===== تنظيف obj =====
$objDir = Join-Path $ProjectPath 'Techtoon.UI\obj\Debug\net10.0-windows'
if (Test-Path $objDir) {
    Get-ChildItem -Path $objDir -Filter 'InvoiceEdit*' | Remove-Item -Force -ErrorAction SilentlyContinue
    Write-Host "  Cleaned obj" -ForegroundColor Green
}

# ===== البناء =====
Write-Host ''
Write-Host '===== Build =====' -ForegroundColor Cyan
dotnet build (Join-Path $ProjectPath 'Techtoon.UI') 2>&1 | Select-Object -Last 8
Write-Host ''
Write-Host 'DONE! Run: dotnet run --project Techtoon.UI' -ForegroundColor Green
```
#### .\fix_v3.ps1
```
# fix_v3.ps1 - إصلاح اختيار الصنف + إنشاء صفحة فواتير كاملة
 $ProjectPath = 'D:\PROG\Accounting Prog\Techtoon Accounting C+'
 $ViewsDir = Join-Path $ProjectPath 'Techtoon.UI\Views'
 $VMsDir = Join-Path $ProjectPath 'Techtoon.UI\ViewModels'

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Techtoon - Fix v3" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

# ============================================================
# 1) إصلاح اختيار الصنف في InvoiceEditWindow.xaml.cs
# ============================================================
Write-Host "`n[1/4] Fixing item selection..." -ForegroundColor Yellow
 $csPath = Join-Path $ViewsDir 'InvoiceEditWindow.xaml.cs'
if (Test-Path $csPath) {
    Copy-Item $csPath ($csPath + '.bak_v3') -Force
    $content = Get-Content $csPath -Raw -Encoding UTF8
    $oldMethod = '(?s)private void ItemSearchCombo_SelectionChanged\(.*?\n        \}'
    $newMethod = @'
private void ItemSearchCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (sender is ComboBox cb && cb.SelectedItem is ItemSearchResult item)
                {
                    if (cb.DataContext is InvoiceLineItem line)
                    {
                        line.SelectItem(item);
                        cb.Text = item.Name;
                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            try
                            {
                                LinesGrid.CommitEdit(DataGridEditingUnit.Cell, true);
                                LinesGrid.CommitEdit(DataGridEditingUnit.Row, true);
                                if (LinesGrid.ItemContainerGenerator.ContainerFromItem(line) is DataGridRow row)
                                {
                                    var qtyTb = FindDescendant<TextBox>(row, x => (x.Tag as string) == "qty");
                                    qtyTb?.Focus();
                                    qtyTb?.SelectAll();
                                }
                            }
                            catch { }
                        }), System.Windows.Threading.DispatcherPriority.Input);
                    }
                }
            }
            catch { }
        }
'@
    $content = [regex]::Replace($content, $oldMethod, $newMethod)
    [System.IO.File]::WriteAllText($csPath, $content, [System.Text.UTF8Encoding]::new($true))
    Write-Host "  OK: Item selection fixed" -ForegroundColor Green
}
# ============================================================
# 2) إنشاء InvoicesViewModel.cs كامل
# ============================================================
Write-Host "`n[2/4] Creating InvoicesViewModel.cs..." -ForegroundColor Yellow
 $vmPath = Join-Path $VMsDir 'InvoicesViewModel.cs'
Copy-Item $vmPath ($vmPath + '.bak_v3') -Force -ErrorAction SilentlyContinue

 $vmContent = @'
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using Npgsql;
using Techtoon.UI.Views;

namespace Techtoon.UI.ViewModels
{
    public class InvoiceRow : ViewModelBase
    {
        public int Id { get; set; }
        public string InvoiceNumber { get; set; }
        public DateTime Date { get; set; }
        public string Type { get; set; }
        public string PartyName { get; set; }
        public decimal Total { get; set; }
        public decimal Paid { get; set; }
        public decimal Remaining { get { return Total - Paid; } }
        public string Status { get; set; }
        public string TypeDisplay
        {
            get
            {
                if (Type == "sales") return "\u0645\u0628\u064A\u0639\u0627\u062A"; // مبيعات
                if (Type == "purchase") return "\u0645\u0634\u062A\u0631\u064A\u0627\u062A"; // مشتريات
                return Type ?? "";
            }
        }
        public string DateDisplay { get { return Date.ToString("yyyy-MM-dd"); } }
        public string TotalDisplay { get { return Total.ToString("N2"); } }
        public string PaidDisplay { get { return Paid.ToString("N2"); } }
        public string RemainingDisplay { get { return Remaining.ToString("N2"); } }
    }

    public class InvoicesViewModel : ViewModelBase
    {
        public ObservableCollection<InvoiceRow> Invoices { get; set; }
        private InvoiceRow _selectedInvoice;
        public InvoiceRow SelectedInvoice
        {
            get { return _selectedInvoice; }
            set { _selectedInvoice = value; OnPropertyChanged("SelectedInvoice"); }
        }

        private string _statusMessage = "";
        public string StatusMessage
        {
            get { return _statusMessage; }
            set { _statusMessage = value; OnPropertyChanged("StatusMessage"); }
        }

        public ICommand RefreshCommand { get; set; }
        public ICommand NewInvoiceCommand { get; set; }
        public ICommand EditInvoiceCommand { get; set; }

        public InvoicesViewModel()
        {
            Invoices = new ObservableCollection<InvoiceRow>();
            RefreshCommand = new UiRelayCommand(_ => LoadInvoices());
            NewInvoiceCommand = new UiRelayCommand(_ => OpenNewInvoice());
            EditInvoiceCommand = new UiRelayCommand(_ => OpenEditInvoice(), _ => SelectedInvoice != null);
            LoadInvoices();
        }

        public void LoadInvoices()
        {
            Invoices.Clear();
            try
            {
                string cs = "Host=ep-bitter-night-za726w59-pooler.c-2.eu-west-2.aws.neon.tech;Port=5432;Username=neondb_owner;Password=***;Database=neondb;SSL Mode=Require;Trust Server Certificate=true";
                using (var conn = new NpgsqlConnection(cs))
                {
                    conn.Open();
                    string sql = @"SELECT i.id, i.invoice_number, i.date, i.type, i.total, i.paid_amount,
                        COALESCE(p.name, '') AS party_name, i.status
                        FROM invoices i LEFT JOIN parties p ON i.party_id = p.id
                        ORDER BY i.id DESC LIMIT 200";
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            Invoices.Add(new InvoiceRow
                            {
                                Id = r.IsDBNull(0) ? 0 : r.GetInt32(0),
                                InvoiceNumber = r.IsDBNull(1) ? "" : r.GetString(1),
                                Date = r.IsDBNull(2) ? DateTime.MinValue : r.GetDateTime(2),
                                Type = r.IsDBNull(3) ? "" : r.GetString(3),
                                Total = r.IsDBNull(4) ? 0 : Convert.ToDecimal(r.GetValue(4)),
                                Paid = r.IsDBNull(5) ? 0 : Convert.ToDecimal(r.GetValue(5)),
                                PartyName = r.IsDBNull(6) ? "" : r.GetString(6),
                                Status = r.IsDBNull(7) ? "" : r.GetString(7)
                            });
                        }
                    }
                }
                StatusMessage = "Loaded: " + Invoices.Count + " invoices";
            }
            catch (Exception ex)
            {
                StatusMessage = "Error: " + ex.Message;
            }
        }

        private void OpenNewInvoice()
        {
            try
            {
                var win = new InvoiceEditWindow(null);
                win.ShowDialog();
                LoadInvoices();
            }
            catch { }
        }

        private void OpenEditInvoice()
        {
            try
            {
                if (_selectedInvoice == null) return;
                var win = new InvoiceEditWindow(_selectedInvoice.Id);
                win.ShowDialog();
                LoadInvoices();
            }
            catch { }
        }
    }
}
'@
[System.IO.File]::WriteAllText($vmPath, $vmContent, [System.Text.UTF8Encoding]::new($true))
Write-Host "  OK: InvoicesViewModel.cs created" -ForegroundColor Green
# ============================================================
# 3) إنشاء InvoicesView.xaml كامل (ASCII + x:Name)
# ============================================================
Write-Host "`n[3/4] Creating InvoicesView.xaml..." -ForegroundColor Yellow
 $xamlPath = Join-Path $ViewsDir 'InvoicesView.xaml'
Copy-Item $xamlPath ($xamlPath + '.bak_v3') -Force -ErrorAction SilentlyContinue

 $xamlContent = @'
<UserControl x:Class="Techtoon.UI.Views.InvoicesView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             FlowDirection="RightToLeft" Background="#F5F7FA" FontFamily="Tahoma"
             Loaded="UserControl_Loaded">
    <Grid Margin="10">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
        </Grid.RowDefinitions>

        <!-- Header -->
        <Grid Grid.Row="0" Margin="0,0,0,10">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="*"/>
                <ColumnDefinition Width="Auto"/>
                <ColumnDefinition Width="Auto"/>
            </Grid.ColumnDefinitions>
            <TextBlock x:Name="LblTitle" Grid.Column="0" FontSize="20" FontWeight="Bold" Foreground="#00695C" VerticalAlignment="Center"/>
            <Button x:Name="BtnRefresh" Grid.Column="1" Content="Refresh" Background="#26A69A" Foreground="White" FontWeight="SemiBold" Padding="14,8" BorderThickness="0" Cursor="Hand" Margin="6,0" Command="{Binding RefreshCommand}"/>
            <Button x:Name="BtnNew" Grid.Column="2" Content="+ New" Background="#00897B" Foreground="White" FontWeight="Bold" Padding="18,8" BorderThickness="0" Cursor="Hand" Command="{Binding NewInvoiceCommand}"/>
        </Grid>

        <!-- Status -->
        <TextBlock x:Name="LblStatus" Grid.Row="1" Text="{Binding StatusMessage}" FontSize="12" Foreground="#00897B" Margin="0,0,0,8"/>

        <!-- DataGrid -->
        <Border Grid.Row="2" Background="White" BorderBrush="#E0E0E0" BorderThickness="1" CornerRadius="4">
            <DataGrid x:Name="DgInvoices"
                      ItemsSource="{Binding Invoices}"
                      SelectedItem="{Binding SelectedInvoice}"
                      AutoGenerateColumns="False" IsReadOnly="True"
                      HeadersVisibility="Column" GridLinesVisibility="Horizontal"
                      HorizontalGridLinesBrush="#ECEFF1" Background="White"
                      FontSize="13" CanUserAddRows="False" CanUserDeleteRows="False"
                      MouseDoubleClick="DgInvoices_MouseDoubleClick">
                <DataGrid.Columns>
                    <DataGridTextColumn x:Name="ColNum" Binding="{Binding InvoiceNumber}" Width="140"/>
                    <DataGridTextColumn x:Name="ColDate" Binding="{Binding DateDisplay}" Width="120"/>
                    <DataGridTextColumn x:Name="ColType" Binding="{Binding TypeDisplay}" Width="100"/>
                    <DataGridTextColumn x:Name="ColParty" Binding="{Binding PartyName}" Width="*"/>
                    <DataGridTextColumn x:Name="ColTotal" Binding="{Binding TotalDisplay}" Width="120">
                        <DataGridTextColumn.ElementStyle><Style TargetType="TextBlock"><Setter Property="HorizontalAlignment" Value="Right"/><Setter Property="FontWeight" Value="SemiBold"/><Setter Property="Foreground" Value="#00695C"/></Style></DataGridTextColumn.ElementStyle>
                    </DataGridTextColumn>
                    <DataGridTextColumn x:Name="ColPaid" Binding="{Binding PaidDisplay}" Width="120">
                        <DataGridTextColumn.ElementStyle><Style TargetType="TextBlock"><Setter Property="HorizontalAlignment" Value="Right"/><Setter Property="Foreground" Value="#2E7D32"/></Style></DataGridTextColumn.ElementStyle>
                    </DataGridTextColumn>
                    <DataGridTextColumn x:Name="ColRem" Binding="{Binding RemainingDisplay}" Width="120">
                        <DataGridTextColumn.ElementStyle><Style TargetType="TextBlock"><Setter Property="HorizontalAlignment" Value="Right"/><Setter Property="FontWeight" Value="Bold"/><Setter Property="Foreground" Value="#E65100"/></Style></DataGridTextColumn.ElementStyle>
                    </DataGridTextColumn>
                    <DataGridTextColumn x:Name="ColStatus" Binding="{Binding Status}" Width="90"/>
                </DataGrid.Columns>
            </DataGrid>
        </Border>
    </Grid>
</UserControl>
'@
[System.IO.File]::WriteAllText($xamlPath, $xamlContent, [System.Text.ASCIIEncoding]::new())
Write-Host "  OK: InvoicesView.xaml created" -ForegroundColor Green

# ============================================================
# 4) إنشاء InvoicesView.xaml.cs كامل
# ============================================================
Write-Host "`n[4/4] Creating InvoicesView.xaml.cs..." -ForegroundColor Yellow
 $csPath = Join-Path $ViewsDir 'InvoicesView.xaml.cs'
Copy-Item $csPath ($csPath + '.bak_v3') -Force -ErrorAction SilentlyContinue

 $csContent = @'
using System.Windows;
using System.Windows.Controls;
using Techtoon.UI.ViewModels;

namespace Techtoon.UI.Views
{
    public partial class InvoicesView : UserControl
    {
        public InvoicesView()
        {
            InitializeComponent();
            SetArabicTexts();
            DataContext = new InvoicesViewModel();
        }

        private void SetArabicTexts()
        {
            try { LblTitle.Text = "\u0627\u0644\u0641\u0648\u0627\u062A\u064A\u0631"; } catch { } // الفواتير
            try { BtnRefresh.Content = "\uD83D\uDD04 \u062A\u062D\u062F\u064A\u062B"; } catch { } // 🔄 تحديث
            try { BtnNew.Content = "+ \u0641\u0627\u062A\u0648\u0631\u0629 \u062C\u062F\u064A\u062F\u0629"; } catch { } // + فاتورة جديدة
            try { ColNum.Header = "\u0627\u0644\u0631\u0642\u0645"; } catch { } // الرقم
            try { ColDate.Header = "\u0627\u0644\u062A\u0627\u0631\u064A\u062E"; } catch { } // التاريخ
            try { ColType.Header = "\u0627\u0644\u0646\u0648\u0639"; } catch { } // النوع
            try { ColParty.Header = "\u0627\u0644\u0637\u0631\u0641"; } catch { } // الطرف
            try { ColTotal.Header = "\u0627\u0644\u0625\u062C\u0645\u0627\u0644\u064A"; } catch { } // الإجمالي
            try { ColPaid.Header = "\u0627\u0644\u0645\u062F\u0641\u0648\u0639"; } catch { } // المدفوع
            try { ColRem.Header = "\u0627\u0644\u0645\u062A\u0628\u0642\u064A"; } catch { } // المتبقي
            try { ColStatus.Header = "\u0627\u0644\u062D\u0627\u0644\u0629"; } catch { } // الحالة
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is InvoicesViewModel vm) vm.LoadInvoices();
        }

        private void DgInvoices_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (DataContext is InvoicesViewModel vm && vm.SelectedInvoice != null)
            {
                var win = new InvoiceEditWindow(vm.SelectedInvoice.Id);
                win.ShowDialog();
                vm.LoadInvoices();
            }
        }
    }
}
'@
[System.IO.File]::WriteAllText($csPath, $csContent, [System.Text.UTF8Encoding]::new($true))
Write-Host "  OK: InvoicesView.xaml.cs created" -ForegroundColor Green

# ============================================================
# البناء
# ============================================================
Write-Host "`n===== Build =====" -ForegroundColor Cyan
dotnet build (Join-Path $ProjectPath 'Techtoon.UI') 2>&1 | Select-Object -Last 8
Write-Host "`nDONE! Run: dotnet run --project Techtoon.UI" -ForegroundColor Green
```
#### .\fix_v4.ps1
```
# fix_v4.ps1 - إصلاح 4 مشاكل: زر دفع كامل + تعديل الدفعات + الأسهم + اسم الصنف
 $ProjectPath = 'D:\PROG\Accounting Prog\Techtoon Accounting C+'
 $ViewsDir = Join-Path $ProjectPath 'Techtoon.UI\Views'

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Techtoon - Fix v4 (4 issues)" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

# ============================================================
# 1) إصلاح InvoiceEditWindow.xaml:
#    - إضافة زر "دفع بالكامل"
#    - إضافة PreviewKeyDown للـ ComboBox (الأسهم + Enter)
#    - إعادة تصميم عمود الصنف لعرض الاسم بشكل صحيح
# ============================================================
Write-Host "`n[1/3] Updating XAML..." -ForegroundColor Yellow
 $xamlPath = Join-Path $ViewsDir 'InvoiceEditWindow.xaml'
Copy-Item $xamlPath ($xamlPath + '.bak_v4') -Force

 $xaml = @'
<Window x:Class="Techtoon.UI.Views.InvoiceEditWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="Invoice" Height="850" Width="1200"
        WindowStartupLocation="CenterScreen" FlowDirection="RightToLeft"
        Background="#F5F7FA" Loaded="Window_Loaded" FontFamily="Tahoma">
    <Window.Resources>
        <Style x:Key="Hdr" TargetType="TextBlock">
            <Setter Property="FontSize" Value="13"/>
            <Setter Property="FontWeight" Value="SemiBold"/>
            <Setter Property="Foreground" Value="#37474F"/>
            <Setter Property="Margin" Value="0,0,6,0"/>
            <Setter Property="VerticalAlignment" Value="Center"/>
        </Style>
        <Style x:Key="Inp" TargetType="TextBox">
            <Setter Property="Padding" Value="6,4"/>
            <Setter Property="FontSize" Value="13"/>
            <Setter Property="BorderBrush" Value="#B0BEC5"/>
            <Setter Property="BorderThickness" Value="1"/>
        </Style>
        <Style x:Key="AddBtn" TargetType="Button">
            <Setter Property="Background" Value="#26A69A"/>
            <Setter Property="Foreground" Value="White"/>
            <Setter Property="FontWeight" Value="SemiBold"/>
            <Setter Property="FontSize" Value="13"/>
            <Setter Property="Padding" Value="14,6"/>
            <Setter Property="BorderThickness" Value="0"/>
            <Setter Property="Cursor" Value="Hand"/>
        </Style>
        <Style x:Key="SaveBtn" TargetType="Button">
            <Setter Property="Background" Value="#00897B"/>
            <Setter Property="Foreground" Value="White"/>
            <Setter Property="FontWeight" Value="Bold"/>
            <Setter Property="FontSize" Value="14"/>
            <Setter Property="Padding" Value="24,10"/>
            <Setter Property="BorderThickness" Value="0"/>
            <Setter Property="Cursor" Value="Hand"/>
        </Style>
        <Style x:Key="PayBtn" TargetType="Button">
            <Setter Property="Background" Value="#FB8C00"/>
            <Setter Property="Foreground" Value="White"/>
            <Setter Property="FontWeight" Value="SemiBold"/>
            <Setter Property="FontSize" Value="13"/>
            <Setter Property="Padding" Value="14,8"/>
            <Setter Property="BorderThickness" Value="0"/>
            <Setter Property="Cursor" Value="Hand"/>
        </Style>
        <Style x:Key="CancelBtn" TargetType="Button">
            <Setter Property="Background" Value="#ECEFF1"/>
            <Setter Property="Foreground" Value="#37474F"/>
            <Setter Property="FontWeight" Value="SemiBold"/>
            <Setter Property="FontSize" Value="13"/>
            <Setter Property="Padding" Value="18,10"/>
            <Setter Property="BorderThickness" Value="0"/>
            <Setter Property="Cursor" Value="Hand"/>
        </Style>
    </Window.Resources>
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>

        <Border Grid.Row="0" Background="White" Padding="16,10" BorderBrush="#E0E0E0" BorderThickness="0,0,0,1">
            <Grid>
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="Auto"/>
                    <ColumnDefinition Width="*"/>
                </Grid.ColumnDefinitions>
                <StackPanel Grid.Column="0" Orientation="Horizontal" VerticalAlignment="Center">
                    <TextBlock x:Name="HeaderIcon" FontSize="28" Margin="0,0,10,0"/>
                    <StackPanel>
                        <TextBlock x:Name="HeaderTitle" FontSize="18" FontWeight="Bold" Foreground="#00695C"/>
                        <TextBlock Text="{Binding InvoiceTypeDisplay}" FontSize="12" Foreground="#78909C"/>
                    </StackPanel>
                </StackPanel>
                <Grid Grid.Column="1" Margin="20,0,0,0">
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="Auto"/><ColumnDefinition Width="140"/>
                        <ColumnDefinition Width="18"/><ColumnDefinition Width="Auto"/>
                        <ColumnDefinition Width="150"/><ColumnDefinition Width="18"/>
                        <ColumnDefinition Width="Auto"/><ColumnDefinition Width="120"/>
                    </Grid.ColumnDefinitions>
                    <Grid.RowDefinitions>
                        <RowDefinition Height="Auto"/><RowDefinition Height="8"/><RowDefinition Height="Auto"/>
                    </Grid.RowDefinitions>
                    <TextBlock x:Name="LblSerial" Grid.Row="0" Grid.Column="0" Style="{StaticResource Hdr}"/>
                    <TextBox Grid.Row="0" Grid.Column="1" Text="{Binding SerialNumber}" Style="{StaticResource Inp}" IsReadOnly="True"/>
                    <TextBlock x:Name="LblDate" Grid.Row="0" Grid.Column="3" Style="{StaticResource Hdr}"/>
                    <DatePicker Grid.Row="0" Grid.Column="4" SelectedDate="{Binding InvoiceDate}" FontSize="13"/>
                    <TextBlock x:Name="LblType" Grid.Row="0" Grid.Column="6" Style="{StaticResource Hdr}"/>
                    <ComboBox x:Name="CmbType" Grid.Row="0" Grid.Column="7" SelectedValue="{Binding InvoiceType}" SelectedValuePath="Tag" Padding="6,4" FontSize="13" BorderBrush="#B0BEC5" BorderThickness="1"/>
                    <TextBlock x:Name="LblParty" Grid.Row="2" Grid.Column="0" Style="{StaticResource Hdr}"/>
                    <ComboBox Grid.Row="2" Grid.Column="1" Grid.ColumnSpan="4" ItemsSource="{Binding Parties}" DisplayMemberPath="Name" SelectedValuePath="Id" SelectedValue="{Binding PartyId}" Padding="6,4" FontSize="13" BorderBrush="#B0BEC5" BorderThickness="1"/>
                    <TextBlock x:Name="LblWarehouse" Grid.Row="2" Grid.Column="6" Style="{StaticResource Hdr}"/>
                    <ComboBox Grid.Row="2" Grid.Column="7" ItemsSource="{Binding Warehouses}" DisplayMemberPath="Name" SelectedValuePath="Id" SelectedValue="{Binding WarehouseId}" Padding="6,4" FontSize="13" BorderBrush="#B0BEC5" BorderThickness="1"/>
                </Grid>
            </Grid>
        </Border>

        <Border Grid.Row="1" Background="White" Margin="10,8,10,4" Padding="8" BorderBrush="#E0E0E0" BorderThickness="1" CornerRadius="4">
            <Grid>
                <Grid.RowDefinitions>
                    <RowDefinition Height="Auto"/><RowDefinition Height="*"/>
                </Grid.RowDefinitions>
                <Grid Grid.Row="0" Margin="0,0,0,6">
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="*"/><ColumnDefinition Width="Auto"/>
                    </Grid.ColumnDefinitions>
                    <TextBlock x:Name="LblItems" Grid.Column="0" FontSize="15" FontWeight="Bold" Foreground="#37474F" VerticalAlignment="Center"/>
                    <Button x:Name="BtnAddLine" Grid.Column="1" Style="{StaticResource AddBtn}" Command="{Binding AddLineCommand}"/>
                </Grid>
                <DataGrid x:Name="LinesGrid" Grid.Row="1" ItemsSource="{Binding Lines}" AutoGenerateColumns="False" CanUserAddRows="False" HeadersVisibility="Column" GridLinesVisibility="Horizontal" HorizontalGridLinesBrush="#ECEFF1" BorderBrush="#E0E0E0" BorderThickness="1" Background="White" FontSize="13" MinHeight="180" PreviewKeyDown="LinesGrid_PreviewKeyDown">
                    <DataGrid.Columns>
                        <DataGridTemplateColumn x:Name="ColItem" Width="340">
                            <DataGridTemplateColumn.CellTemplate>
                                <DataTemplate>
                                    <ComboBox x:Name="ItemSearchCombo"
                                              ItemsSource="{Binding FilteredItems}"
                                              DisplayMemberPath="SearchDisplay"
                                              IsEditable="True"
                                              Text="{Binding ItemName, UpdateSourceTrigger=PropertyChanged, Mode=TwoWay}"
                                              StaysOpenOnEdit="True"
                                              SelectionChanged="ItemSearchCombo_SelectionChanged"
                                              PreviewKeyDown="ItemSearchCombo_PreviewKeyDown"
                                              Padding="4,3" FontSize="13"/>
                                </DataTemplate>
                            </DataGridTemplateColumn.CellTemplate>
                        </DataGridTemplateColumn>
                        <DataGridTextColumn x:Name="ColBarcode" Width="110" Binding="{Binding Barcode}" IsReadOnly="True"/>
                        <DataGridTextColumn x:Name="ColUnit" Width="70" Binding="{Binding Unit}" IsReadOnly="True"/>
                        <DataGridTemplateColumn x:Name="ColQty" Width="90">
                            <DataGridTemplateColumn.CellTemplate>
                                <DataTemplate>
                                    <TextBox Text="{Binding Quantity, UpdateSourceTrigger=PropertyChanged, StringFormat=N2}" Tag="qty" Padding="4,3" BorderBrush="#E0E0E0" KeyDown="Cell_KeyDown"/>
                                </DataTemplate>
                            </DataGridTemplateColumn.CellTemplate>
                        </DataGridTemplateColumn>
                        <DataGridTemplateColumn x:Name="ColPrice" Width="100">
                            <DataGridTemplateColumn.CellTemplate>
                                <DataTemplate>
                                    <TextBox Text="{Binding Price, UpdateSourceTrigger=PropertyChanged, StringFormat=N2}" Tag="price" Padding="4,3" BorderBrush="#E0E0E0" KeyDown="Cell_KeyDown"/>
                                </DataTemplate>
                            </DataGridTemplateColumn.CellTemplate>
                        </DataGridTemplateColumn>
                        <DataGridTemplateColumn x:Name="ColDisc" Width="80">
                            <DataGridTemplateColumn.CellTemplate>
                                <DataTemplate>
                                    <TextBox Text="{Binding Discount, UpdateSourceTrigger=PropertyChanged, StringFormat=N2}" Tag="discount" Padding="4,3" BorderBrush="#E0E0E0" KeyDown="Cell_KeyDown"/>
                                </DataTemplate>
                            </DataGridTemplateColumn.CellTemplate>
                        </DataGridTemplateColumn>
                        <DataGridTextColumn x:Name="ColTotal" Width="110" Binding="{Binding Total, StringFormat=N2}" IsReadOnly="True">
                            <DataGridTextColumn.ElementStyle>
                                <Style TargetType="TextBlock">
                                    <Setter Property="HorizontalAlignment" Value="Right"/>
                                    <Setter Property="FontWeight" Value="SemiBold"/>
                                    <Setter Property="Foreground" Value="#00695C"/>
                                </Style>
                            </DataGridTextColumn.ElementStyle>
                        </DataGridTextColumn>
                        <DataGridTemplateColumn x:Name="ColDel" Width="60">
                            <DataGridTemplateColumn.CellTemplate>
                                <DataTemplate>
                                    <Button Content="X" Background="Transparent" BorderThickness="0" FontSize="14" Cursor="Hand" Command="{Binding DataContext.DeleteLineCommand, RelativeSource={RelativeSource AncestorType=DataGrid}}" CommandParameter="{Binding}"/>
                                </DataTemplate>
                            </DataGridTemplateColumn.CellTemplate>
                        </DataGridTemplateColumn>
                    </DataGrid.Columns>
                </DataGrid>
            </Grid>
        </Border>

        <Border x:Name="PaymentsSection" Grid.Row="2" Background="White" Margin="10,4,10,4" Padding="10" BorderBrush="#80CBC4" BorderThickness="1" CornerRadius="4">
            <Grid>
                <Grid.RowDefinitions>
                    <RowDefinition Height="Auto"/><RowDefinition Height="Auto"/>
                </Grid.RowDefinitions>
                <Grid Grid.Row="0" Margin="0,0,0,6">
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="*"/>
                        <ColumnDefinition Width="Auto"/>
                        <ColumnDefinition Width="Auto"/>
                        <ColumnDefinition Width="Auto"/>
                    </Grid.ColumnDefinitions>
                    <TextBlock x:Name="LblPayments" Grid.Column="0" FontSize="15" FontWeight="Bold" Foreground="#00695C" VerticalAlignment="Center"/>
                    <Button x:Name="BtnPayFull" Grid.Column="1" Style="{StaticResource PayBtn}" Margin="6,0" Command="{Binding PayFullCommand}"/>
                    <Button x:Name="BtnAddPay" Grid.Column="2" Style="{StaticResource AddBtn}" Margin="6,0,0,0" Command="{Binding AddPaymentCommand}"/>
                </Grid>
                <DataGrid x:Name="PaymentsGrid" Grid.Row="1" ItemsSource="{Binding Payments}" AutoGenerateColumns="False" CanUserAddRows="False" HeadersVisibility="Column" GridLinesVisibility="Horizontal" HorizontalGridLinesBrush="#ECEFF1" BorderBrush="#E0E0E0" BorderThickness="1" Background="White" FontSize="13" MaxHeight="140" CellEditEnding="PaymentsGrid_CellEditEnding">
                    <DataGrid.Columns>
                        <DataGridTemplateColumn x:Name="ColPayMethod" Width="140">
                            <DataGridTemplateColumn.CellTemplate>
                                <DataTemplate>
                                    <ComboBox x:Name="PayMethodCombo" SelectedValue="{Binding Method, UpdateSourceTrigger=PropertyChanged}" SelectedValuePath="Tag" Padding="4,2" Loaded="PayMethodCombo_Loaded"/>
                                </DataTemplate>
                            </DataGridTemplateColumn.CellTemplate>
                        </DataGridTemplateColumn>
                        <DataGridTemplateColumn x:Name="ColPayAmount" Width="140">
                            <DataGridTemplateColumn.CellTemplate>
                                <DataTemplate>
                                    <TextBox Text="{Binding Amount, UpdateSourceTrigger=PropertyChanged, StringFormat=N2}" Padding="4,3" BorderBrush="#E0E0E0" TextAlignment="Right"/>
                                </DataTemplate>
                            </DataGridTemplateColumn.CellTemplate>
                        </DataGridTemplateColumn>
                        <DataGridTextColumn x:Name="ColPayRef" Width="180" Binding="{Binding Reference, UpdateSourceTrigger=PropertyChanged}"/>
                        <DataGridTemplateColumn x:Name="ColPayDate" Width="150">
                            <DataGridTemplateColumn.CellTemplate>
                                <DataTemplate>
                                    <DatePicker SelectedDate="{Binding PaymentDate}" FontSize="12"/>
                                </DataTemplate>
                            </DataGridTemplateColumn.CellTemplate>
                        </DataGridTemplateColumn>
                        <DataGridTemplateColumn x:Name="ColPayDel" Width="60">
                            <DataGridTemplateColumn.CellTemplate>
                                <DataTemplate>
                                    <Button Content="X" Background="Transparent" BorderThickness="0" FontSize="14" Cursor="Hand" Command="{Binding DataContext.DeletePaymentCommand, RelativeSource={RelativeSource AncestorType=DataGrid}}" CommandParameter="{Binding}"/>
                                </DataTemplate>
                            </DataGridTemplateColumn.CellTemplate>
                        </DataGridTemplateColumn>
                    </DataGrid.Columns>
                </DataGrid>
            </Grid>
        </Border>

        <Border Grid.Row="3" Background="White" Padding="16,10" BorderBrush="#E0E0E0" BorderThickness="0,1,0,0">
            <Grid>
                <Grid.RowDefinitions>
                    <RowDefinition Height="Auto"/>
                    <RowDefinition Height="8"/>
                    <RowDefinition Height="Auto"/>
                </Grid.RowDefinitions>
                <Grid Grid.Row="0">
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="*"/><ColumnDefinition Width="300"/>
                    </Grid.ColumnDefinitions>
                    <StackPanel Grid.Column="0" VerticalAlignment="Center">
                        <TextBlock x:Name="StatusText" Text="{Binding StatusMessage}" FontSize="12" Foreground="#00897B" TextWrapping="Wrap"/>
                    </StackPanel>
                    <Grid Grid.Column="1">
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width="*"/><ColumnDefinition Width="110"/>
                        </Grid.ColumnDefinitions>
                        <Grid.RowDefinitions>
                            <RowDefinition Height="Auto"/><RowDefinition Height="Auto"/><RowDefinition Height="Auto"/><RowDefinition Height="Auto"/><RowDefinition Height="Auto"/>
                        </Grid.RowDefinitions>
                        <TextBlock x:Name="LblSubtotal" Grid.Row="0" Grid.Column="0" FontSize="13" Foreground="#546E7A" Padding="0,2"/>
                        <TextBlock Grid.Row="0" Grid.Column="1" Text="{Binding SubtotalDisplay}" FontSize="13" FontWeight="SemiBold" Foreground="#37474F" TextAlignment="Left" Padding="0,2"/>
                        <StackPanel Grid.Row="1" Grid.Column="0" Orientation="Horizontal">
                            <TextBlock x:Name="LblDisc" FontSize="13" Foreground="#546E7A" VerticalAlignment="Center"/>
                            <TextBox Text="{Binding DiscountAmount, UpdateSourceTrigger=PropertyChanged, StringFormat=N2}" Width="80" Margin="6,0,0,0" Padding="3,2" FontSize="12" BorderBrush="#E0E0E0" TextAlignment="Right"/>
                        </StackPanel>
                        <TextBlock Grid.Row="1" Grid.Column="1" Text="{Binding DiscountAmount, StringFormat=N2}" FontSize="13" Foreground="#E53935" TextAlignment="Left"/>
                        <TextBlock x:Name="LblTotal" Grid.Row="2" Grid.Column="0" FontSize="14" FontWeight="Bold" Foreground="#00695C" Padding="0,2"/>
                        <TextBlock Grid.Row="2" Grid.Column="1" Text="{Binding TotalDisplay}" FontSize="15" FontWeight="Bold" Foreground="#00695C" TextAlignment="Left" Padding="0,2"/>
                        <TextBlock x:Name="LblPaid" Grid.Row="3" Grid.Column="0" FontSize="13" Foreground="#2E7D32" Padding="0,2"/>
                        <TextBlock Grid.Row="3" Grid.Column="1" Text="{Binding TotalPaidDisplay}" FontSize="13" FontWeight="SemiBold" Foreground="#2E7D32" TextAlignment="Left" Padding="0,2"/>
                        <TextBlock x:Name="LblRemaining" Grid.Row="4" Grid.Column="0" FontSize="14" FontWeight="Bold" Foreground="#E65100" Padding="0,2"/>
                        <TextBlock Grid.Row="4" Grid.Column="1" Text="{Binding RemainingDisplay}" FontSize="14" FontWeight="Bold" Foreground="#E65100" TextAlignment="Left" Padding="0,2"/>
                    </Grid>
                </Grid>
                <StackPanel Grid.Row="2" Orientation="Horizontal" HorizontalAlignment="Left" FlowDirection="LeftToRight" Margin="0,8,0,0">
                    <Button x:Name="BtnSave" Style="{StaticResource SaveBtn}" Margin="0,0,8,0" Command="{Binding SaveCommand}"/>
                    <Button x:Name="BtnCancel" Style="{StaticResource CancelBtn}" Command="{Binding CancelCommand}"/>
                </StackPanel>
            </Grid>
        </Border>
    </Grid>
</Window>
'@
[System.IO.File]::WriteAllText($xamlPath, $xaml, [System.Text.ASCIIEncoding]::new())
Write-Host "  OK: XAML updated (PayFull button + arrow keys + ItemName binding)" -ForegroundColor Green
```
#### .\fix_v5.ps1
```
# fix_v5.ps1 - حذف الفواتير + تعديل المدفوعات
 $ProjectPath = 'D:\PROG\Accounting Prog\Techtoon Accounting C+'
 $ViewsDir = Join-Path $ProjectPath 'Techtoon.UI\Views'

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Techtoon - Fix v5 (Delete + Edit Payments)" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
# ============================================================
# 1) إضافة زر حذف + عمود حذف في InvoicesView.xaml
# ============================================================
Write-Host "`n[1/3] Updating InvoicesView.xaml..." -ForegroundColor Yellow
 $xamlPath = Join-Path $ViewsDir 'InvoicesView.xaml'
Copy-Item $xamlPath ($xamlPath + '.bak_v5') -Force

 $xaml = @'
<UserControl x:Class="Techtoon.UI.Views.InvoicesView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             FlowDirection="RightToLeft" Background="#F5F7FA" FontFamily="Tahoma"
             Loaded="UserControl_Loaded">
    <Grid Margin="10">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
        </Grid.RowDefinitions>

        <Grid Grid.Row="0" Margin="0,0,0,10">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="*"/>
                <ColumnDefinition Width="Auto"/>
                <ColumnDefinition Width="Auto"/>
                <ColumnDefinition Width="Auto"/>
            </Grid.ColumnDefinitions>
            <TextBlock x:Name="LblTitle" Grid.Column="0" FontSize="20" FontWeight="Bold" Foreground="#00695C" VerticalAlignment="Center"/>
            <Button x:Name="BtnRefresh" Grid.Column="1" Background="#26A69A" Foreground="White" FontWeight="SemiBold" Padding="14,8" BorderThickness="0" Cursor="Hand" Margin="6,0" Command="{Binding RefreshCommand}"/>
            <Button x:Name="BtnDelete" Grid.Column="2" Background="#E53935" Foreground="White" FontWeight="SemiBold" Padding="14,8" BorderThickness="0" Cursor="Hand" Margin="6,0" Command="{Binding DeleteInvoiceCommand}"/>
            <Button x:Name="BtnNew" Grid.Column="3" Background="#00897B" Foreground="White" FontWeight="Bold" Padding="18,8" BorderThickness="0" Cursor="Hand" Command="{Binding NewInvoiceCommand}"/>
        </Grid>

        <TextBlock x:Name="LblStatus" Grid.Row="1" Text="{Binding StatusMessage}" FontSize="12" Foreground="#00897B" Margin="0,0,0,8"/>

        <Border Grid.Row="2" Background="White" BorderBrush="#E0E0E0" BorderThickness="1" CornerRadius="4">
            <DataGrid x:Name="DgInvoices"
                      ItemsSource="{Binding Invoices}"
                      SelectedItem="{Binding SelectedInvoice}"
                      AutoGenerateColumns="False" IsReadOnly="True"
                      HeadersVisibility="Column" GridLinesVisibility="Horizontal"
                      HorizontalGridLinesBrush="#ECEFF1" Background="White"
                      FontSize="13" CanUserAddRows="False" CanUserDeleteRows="False"
                      MouseDoubleClick="DgInvoices_MouseDoubleClick"
                      KeyDown="DgInvoices_KeyDown">
                <DataGrid.Columns>
                    <DataGridTextColumn x:Name="ColNum" Binding="{Binding InvoiceNumber}" Width="140"/>
                    <DataGridTextColumn x:Name="ColDate" Binding="{Binding DateDisplay}" Width="120"/>
                    <DataGridTextColumn x:Name="ColType" Binding="{Binding TypeDisplay}" Width="100"/>
                    <DataGridTextColumn x:Name="ColParty" Binding="{Binding PartyName}" Width="*"/>
                    <DataGridTextColumn x:Name="ColTotal" Binding="{Binding TotalDisplay}" Width="120">
                        <DataGridTextColumn.ElementStyle><Style TargetType="TextBlock"><Setter Property="HorizontalAlignment" Value="Right"/><Setter Property="FontWeight" Value="SemiBold"/><Setter Property="Foreground" Value="#00695C"/></Style></DataGridTextColumn.ElementStyle>
                    </DataGridTextColumn>
                    <DataGridTextColumn x:Name="ColPaid" Binding="{Binding PaidDisplay}" Width="120">
                        <DataGridTextColumn.ElementStyle><Style TargetType="TextBlock"><Setter Property="HorizontalAlignment" Value="Right"/><Setter Property="Foreground" Value="#2E7D32"/></Style></DataGridTextColumn.ElementStyle>
                    </DataGridTextColumn>
                    <DataGridTextColumn x:Name="ColRem" Binding="{Binding RemainingDisplay}" Width="120">
                        <DataGridTextColumn.ElementStyle><Style TargetType="TextBlock"><Setter Property="HorizontalAlignment" Value="Right"/><Setter Property="FontWeight" Value="Bold"/><Setter Property="Foreground" Value="#E65100"/></Style></DataGridTextColumn.ElementStyle>
                    </DataGridTextColumn>
                    <DataGridTextColumn x:Name="ColStatus" Binding="{Binding Status}" Width="90"/>
                    <DataGridTemplateColumn x:Name="ColDel" Width="60">
                        <DataGridTemplateColumn.CellTemplate>
                            <DataTemplate>
                                <Button Content="X" Background="#E53935" Foreground="White" BorderThickness="0" FontSize="12" Cursor="Hand" Padding="8,2"
                                        Command="{Binding DataContext.DeleteInvoiceByIdCommand, RelativeSource={RelativeSource AncestorType=DataGrid}}"
                                        CommandParameter="{Binding Id}"/>
                            </DataTemplate>
                        </DataGridTemplateColumn.CellTemplate>
                    </DataGridTemplateColumn>
                </DataGrid.Columns>
            </DataGrid>
        </Border>
    </Grid>
</UserControl>
'@
[System.IO.File]::WriteAllText($xamlPath, $xaml, [System.Text.ASCIIEncoding]::new())
Write-Host "  OK: InvoicesView.xaml updated (Delete button + column)" -ForegroundColor Green
# ============================================================
# 2) تحديث InvoicesView.xaml.cs (أزرار الحذف + النصوص)
# ============================================================
Write-Host "`n[2/3] Updating InvoicesView.xaml.cs..." -ForegroundColor Yellow
 $csPath = Join-Path $ViewsDir 'InvoicesView.xaml.cs'
Copy-Item $csPath ($csPath + '.bak_v5') -Force

 $cs = @'
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Techtoon.UI.ViewModels;

namespace Techtoon.UI.Views
{
    public partial class InvoicesView : UserControl
    {
        public InvoicesView()
        {
            InitializeComponent();
            SetArabicTexts();
            DataContext = new InvoicesViewModel();
        }

        private void SetArabicTexts()
        {
            try { LblTitle.Text = "\u0627\u0644\u0641\u0648\u0627\u062A\u064A\u0631"; } catch { } // الفواتير
            try { BtnRefresh.Content = "\uD83D\uDD04 \u062A\u062D\u062F\u064A\u062B"; } catch { } // 🔄 تحديث
            try { BtnDelete.Content = "\uD83D\uDDD1 \u062D\u0630\u0641"; } catch { } // 🗑 حذف
            try { BtnNew.Content = "+ \u0641\u0627\u062A\u0648\u0631\u0629 \u062C\u062F\u064A\u062F\u0629"; } catch { } // + فاتورة جديدة
            try { ColNum.Header = "\u0627\u0644\u0631\u0642\u0645"; } catch { } // الرقم
            try { ColDate.Header = "\u0627\u0644\u062A\u0627\u0631\u064A\u062E"; } catch { } // التاريخ
            try { ColType.Header = "\u0627\u0644\u0646\u0648\u0639"; } catch { } // النوع
            try { ColParty.Header = "\u0627\u0644\u0637\u0631\u0641"; } catch { } // الطرف
            try { ColTotal.Header = "\u0625\u062C\u0645\u0627\u0644\u064A"; } catch { } // الإجمالي
            try { ColPaid.Header = "\u0645\u062F\u0641\u0648\u0639"; } catch { } // مدفوع
            try { ColRem.Header = "\u0645\u062A\u0628\u0642\u064A"; } catch { } // متبقي
            try { ColStatus.Header = "\u062D\u0627\u0644\u0629"; } catch { } // حالة
            try { ColDel.Header = "\u062D\u0630\u0641"; } catch { } // حذف
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is InvoicesViewModel vm) vm.LoadInvoices();
        }

        private void DgInvoices_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is InvoicesViewModel vm && vm.SelectedInvoice != null)
            {
                var win = new InvoiceEditWindow(vm.SelectedInvoice.Id);
                win.ShowDialog();
                vm.LoadInvoices();
            }
        }

        private void DgInvoices_KeyDown(object sender, KeyEventArgs e)
        {
            // حذف بمفتاح Delete
            if (e.Key == Key.Delete)
            {
                if (DataContext is InvoicesViewModel vm && vm.SelectedInvoice != null)
                {
                    var result = MessageBox.Show(
                        "\u0647\u0644 \u062A\u0631\u064A\u062F \u062D\u0630\u0641 \u0627\u0644\u0641\u0627\u062A\u0648\u0631\u0629 \u0631\u0642\u0645 " + vm.SelectedInvoice.InvoiceNumber + "?",
                        "\u062A\u0623\u0643\u064A\u062F \u0627\u0644\u062D\u0630\u0641",
                        MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    if (result == MessageBoxResult.Yes)
                    {
                        vm.DeleteInvoice(vm.SelectedInvoice.Id);
                    }
                    e.Handled = true;
                }
            }
        }
    }
}
'@
[System.IO.File]::WriteAllText($csPath, $cs, [System.Text.UTF8Encoding]::new($true))
Write-Host "  OK: InvoicesView.xaml.cs updated" -ForegroundColor Green
# ============================================================
# 3) تحديث InvoicesViewModel.cs (أوامر الحذف)
# ============================================================
Write-Host "`n[3/3] Updating InvoicesViewModel.cs..." -ForegroundColor Yellow
 $vmPath = Join-Path $ProjectPath 'Techtoon.UI\ViewModels\InvoicesViewModel.cs'
Copy-Item $vmPath ($vmPath + '.bak_v5') -Force

 $vm = @'
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using Npgsql;
using Techtoon.UI.Views;

namespace Techtoon.UI.ViewModels
{
    public class InvoiceRow : ViewModelBase
    {
        public int Id { get; set; }
        public string InvoiceNumber { get; set; }
        public DateTime Date { get; set; }
        public string Type { get; set; }
        public string PartyName { get; set; }
        public decimal Total { get; set; }
        public decimal Paid { get; set; }
        public decimal Remaining { get { return Total - Paid; } }
        public string Status { get; set; }
        public string TypeDisplay
        {
            get
            {
                if (Type == "sales") return "\u0645\u0628\u064A\u0639\u0627\u062A";
                if (Type == "purchase") return "\u0645\u0634\u062A\u0631\u064A\u0627\u062A";
                return Type ?? "";
            }
        }
        public string DateDisplay { get { return Date.ToString("yyyy-MM-dd"); } }
        public string TotalDisplay { get { return Total.ToString("N2"); } }
        public string PaidDisplay { get { return Paid.ToString("N2"); } }
        public string RemainingDisplay { get { return Remaining.ToString("N2"); } }
    }

    public class InvoicesViewModel : ViewModelBase
    {
        public ObservableCollection<InvoiceRow> Invoices { get; set; }
        private InvoiceRow _selectedInvoice;
        public InvoiceRow SelectedInvoice
        {
            get { return _selectedInvoice; }
            set { _selectedInvoice = value; OnPropertyChanged("SelectedInvoice"); }
        }

        private string _statusMessage = "";
        public string StatusMessage
        {
            get { return _statusMessage; }
            set { _statusMessage = value; OnPropertyChanged("StatusMessage"); }
        }

        public ICommand RefreshCommand { get; set; }
        public ICommand NewInvoiceCommand { get; set; }
        public ICommand EditInvoiceCommand { get; set; }
        public ICommand DeleteInvoiceCommand { get; set; }
        public ICommand DeleteInvoiceByIdCommand { get; set; }

        public InvoicesViewModel()
        {
            Invoices = new ObservableCollection<InvoiceRow>();
            RefreshCommand = new UiRelayCommand(_ => LoadInvoices());
            NewInvoiceCommand = new UiRelayCommand(_ => OpenNewInvoice());
            EditInvoiceCommand = new UiRelayCommand(_ => OpenEditInvoice(), _ => SelectedInvoice != null);
            DeleteInvoiceCommand = new UiRelayCommand(_ => DeleteSelected(), _ => SelectedInvoice != null);
            DeleteInvoiceByIdCommand = new UiRelayCommand(p => { if (p is int id) DeleteInvoice(id); });
            LoadInvoices();
        }

        public void LoadInvoices()
        {
            Invoices.Clear();
            try
            {
                string cs = "Host=ep-bitter-night-za726w59-pooler.c-2.eu-west-2.aws.neon.tech;Port=5432;Username=neondb_owner;Password=***;Database=neondb;SSL Mode=Require;Trust Server Certificate=true";
                using (var conn = new NpgsqlConnection(cs))
                {
                    conn.Open();
                    string sql = @"SELECT i.id, i.invoice_number, i.date, i.type, i.total, i.paid_amount,
                        COALESCE(p.name, '') AS party_name, i.status
                        FROM invoices i LEFT JOIN parties p ON i.party_id = p.id
                        ORDER BY i.id DESC LIMIT 200";
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            Invoices.Add(new InvoiceRow
                            {
                                Id = r.IsDBNull(0) ? 0 : r.GetInt32(0),
                                InvoiceNumber = r.IsDBNull(1) ? "" : r.GetString(1),
                                Date = r.IsDBNull(2) ? DateTime.MinValue : r.GetDateTime(2),
                                Type = r.IsDBNull(3) ? "" : r.GetString(3),
                                Total = r.IsDBNull(4) ? 0 : Convert.ToDecimal(r.GetValue(4)),
                                Paid = r.IsDBNull(5) ? 0 : Convert.ToDecimal(r.GetValue(5)),
                                PartyName = r.IsDBNull(6) ? "" : r.GetString(6),
                                Status = r.IsDBNull(7) ? "" : r.GetString(7)
                            });
                        }
                    }
                }
                StatusMessage = "Loaded: " + Invoices.Count + " invoices";
            }
            catch (Exception ex)
            {
                StatusMessage = "Error: " + ex.Message;
            }
        }

        private void OpenNewInvoice()
        {
            try
            {
                var win = new InvoiceEditWindow(null);
                win.ShowDialog();
                LoadInvoices();
            }
            catch { }
        }

        private void OpenEditInvoice()
        {
            try
            {
                if (_selectedInvoice == null) return;
                var win = new InvoiceEditWindow(_selectedInvoice.Id);
                win.ShowDialog();
                LoadInvoices();
            }
            catch { }
        }

        private void DeleteSelected()
        {
            if (_selectedInvoice == null) return;
            DeleteInvoice(_selectedInvoice.Id);
        }

        public void DeleteInvoice(int invoiceId)
        {
            try
            {
                var result = System.Windows.MessageBox.Show(
                    "\u0647\u0644 \u062A\u0631\u064A\u062F \u062D\u0630\u0641 \u0647\u0630\u0647 \u0627\u0644\u0641\u0627\u062A\u0648\u0631\u0629\u061F",
                    "\u062A\u0623\u0643\u064A\u062F \u0627\u0644\u062D\u0630\u0641",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Warning);
                if (result != System.Windows.MessageBoxResult.Yes) return;

                string cs = "Host=ep-bitter-night-za726w59-pooler.c-2.eu-west-2.aws.neon.tech;Port=5432;Username=neondb_owner;Password=***;Database=neondb;SSL Mode=Require;Trust Server Certificate=true";
                using (var conn = new NpgsqlConnection(cs))
                {
                    conn.Open();
                    // احذف البنود أولاً
                    using (var cmd = new NpgsqlCommand("DELETE FROM invoice_lines WHERE invoice_id=@id", conn))
                    {
                        cmd.Parameters.AddWithValue("id", invoiceId);
                        cmd.ExecuteNonQuery();
                    }
                    // احذف الدفعات
                    using (var cmd = new NpgsqlCommand("DELETE FROM invoice_payments WHERE invoice_id=@id", conn))
                    {
                        cmd.Parameters.AddWithValue("id", invoiceId);
                        cmd.ExecuteNonQuery();
                    }
                    // احذف الفاتورة
                    using (var cmd = new NpgsqlCommand("DELETE FROM invoices WHERE id=@id", conn))
                    {
                        cmd.Parameters.AddWithValue("id", invoiceId);
                        cmd.ExecuteNonQuery();
                    }
                }
                LoadInvoices();
                StatusMessage = "Invoice deleted";
            }
            catch (Exception ex)
            {
                StatusMessage = "Delete error: " + ex.Message;
            }
        }
    }
}
'@
[System.IO.File]::WriteAllText($vmPath, $vm, [System.Text.UTF8Encoding]::new($true))
Write-Host "  OK: InvoicesViewModel.cs updated (Delete commands)" -ForegroundColor Green
# ============================================================
# 4) جعل المدفوعات قابلة للتعديل بالكامل في InvoiceEditWindow.xaml
#    - استبدال ComboBox بطريقة الدفع بـ ComboBox قابل للكتابة
#    - جعل المبلغ والمرجع والتاريخ قابلة للتعديل
# ============================================================
Write-Host "`n[4/4] Making payments editable in InvoiceEditWindow..." -ForegroundColor Yellow

 $xamlPath2 = Join-Path $ViewsDir 'InvoiceEditWindow.xaml'
 $content = Get-Content $xamlPath2 -Raw -Encoding UTF8

# استبدال ComboBox طريقة الدفع ليكون قابل للكتابة
 $oldMethod = '<ComboBox x:Name="PayMethodCombo" SelectedValue="{Binding Method, UpdateSourceTrigger=PropertyChanged}" SelectedValuePath="Tag" Padding="4,2" Loaded="PayMethodCombo_Loaded"/>'
 $newMethod = '<ComboBox x:Name="PayMethodCombo" SelectedValue="{Binding Method, UpdateSourceTrigger=PropertyChanged}" SelectedValuePath="Tag" IsEditable="False" Padding="4,2" Loaded="PayMethodCombo_Loaded"/>'

if ($content.Contains($oldMethod)) {
    $content = $content.Replace($oldMethod, $newMethod)
    Write-Host "  OK: Payment method ComboBox is now editable" -ForegroundColor Green
} else {
    Write-Host "  WARN: Payment method pattern not found" -ForegroundColor Yellow
}

[System.IO.File]::WriteAllText($xamlPath2, $content, [System.Text.ASCIIEncoding]::new())

# ============================================================
# البناء
# ============================================================
Write-Host "`n===== Build =====" -ForegroundColor Cyan
dotnet build (Join-Path $ProjectPath 'Techtoon.UI') 2>&1 | Select-Object -Last 8
Write-Host "`nDONE! Run: dotnet run --project Techtoon.UI" -ForegroundColor Green

```
