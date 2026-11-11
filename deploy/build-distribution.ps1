# =====================================================================
#  بناء حزمة توزيع Techtoon Accounting (Windows x64 — مستقل بالكامل)
#
#  المخرجات: deploy\distribution\
#    Application\   → كل ملفات البرنامج (EXE + DLL) — self-contained (لا يحتاج .NET مثبّت)
#    Licenses\      → رخص موقّعة حقيقية (تجريبي / شهري / سنوي / مدى الحياة) — غير مربوطة بجهاز
#    Documentation\ → الدليل
#    Tools\         → Techtoon.LicenseTool.exe (self-contained) لإصدار الرخص
#
#  ملاحظة: الرخص تُولَّد بأداة الترخيص الحقيقية (توقيع HMAC صالح) — لا توجد ملفات وهمية.
# =====================================================================
param(
    [string]$Configuration = "Release",
    [switch]$SkipPublish
)
$ErrorActionPreference = "Stop"
$root = Resolve-Path (Join-Path $PSScriptRoot "..")
Set-Location $root

$dist     = Join-Path $root "deploy\distribution"
$appDir   = Join-Path $dist "Application"
$licDir   = Join-Path $dist "Licenses"
$docDir   = Join-Path $dist "Documentation"
$toolsDir = Join-Path $dist "Tools"

Write-Host "=== Techtoon Distribution Builder ===" -ForegroundColor Cyan

# ---------- 1) تنظيف ----------
# إغلاق أي نسخة قيد التشغيل (تحتجز ملف EXE)
Get-Process -Name "Techtoon.UI" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Start-Sleep -Milliseconds 500

foreach ($d in @($appDir, $licDir, $docDir, $toolsDir)) {
    if (Test-Path $d) { Remove-Item $d -Recurse -Force -ErrorAction SilentlyContinue }
}
New-Item -ItemType Directory -Force -Path $appDir, $licDir, $docDir, $toolsDir | Out-Null
foreach ($d in @("Logs", "Backups", "Manuals")) {
    New-Item -ItemType Directory -Force -Path (Join-Path $dist $d) | Out-Null
}

# ---------- 2) نشر التطبيق (self-contained, single file) ----------
if (-not $SkipPublish) {
    Write-Host "[1/4] publishing Techtoon.UI (win-x64, self-contained, single-file)..." -ForegroundColor Green
    dotnet publish "Techtoon.UI\Techtoon.UI.csproj" `
        -c $Configuration `
        -r win-x64 `
        --self-contained true `
        -p:PublishSingleFile=true `
        -p:IncludeNativeLibrariesForSelfExtract=true `
        -p:EnableCompressionInSingleFile=true `
        -p:DebugType=None `
        -p:DebugSymbols=false `
        -o $appDir `
        --nologo
    if ($LASTEXITCODE -ne 0) { Write-Host "publish failed" -ForegroundColor Red; exit 1 }

    Write-Host "[2/4] publishing Techtoon.LicenseTool (win-x64, self-contained)..." -ForegroundColor Green
    dotnet publish "Techtoon.LicenseTool\Techtoon.LicenseTool.csproj" `
        -c $Configuration `
        -r win-x64 `
        --self-contained true `
        -p:PublishSingleFile=true `
        -p:IncludeNativeLibrariesForSelfExtract=true `
        -p:EnableCompressionInSingleFile=true `
        -p:DebugType=None `
        -p:DebugSymbols=false `
        -o $toolsDir `
        --nologo
    if ($LASTEXITCODE -ne 0) { Write-Host "LicenseTool publish failed" -ForegroundColor Red; exit 1 }
} else {
    Write-Host "[1-2/4] publish skipped" -ForegroundColor Yellow
}

# ---------- 3) نسخ ملفات إضافية ----------
foreach ($extra in @("logo.ico", "LICENSE.txt")) {
    $src = Join-Path $root $extra
    if (Test-Path $src) { Copy-Item $src -Destination $appDir -Force }
}

# الوثائق (من deploy\docs — مصدر واحد يُنسخ مع كل بناء)
$docsSrc = Join-Path $root "deploy\docs"
if (Test-Path $docsSrc) {
    Copy-Item (Join-Path $docsSrc "*") -Destination $docDir -Force
}

# سكربت إنشاء قاعدة البيانات (لمستخدم PCs جديدة)
$schemaSrc = Join-Path $root "deploy\migrations\schema_full.sql"
if (Test-Path $schemaSrc) {
    Copy-Item $schemaSrc -Destination (Join-Path $docDir "schema_full.sql") -Force
}

# ---------- 4) توليد الرخص الموقّعة الحقيقية ----------
Write-Host "[3/4] generating signed license files..." -ForegroundColor Green
$tool = Join-Path $toolsDir "Techtoon.LicenseTool.exe"
if (-not (Test-Path $tool)) {
    Write-Host "LicenseTool.exe not found at $tool - cannot generate licenses" -ForegroundColor Red
    exit 1
}

$licenseSpecs = @(
    @{ Name = "License_Trial.key";    Type = "trial";    Days = 10; Customer = "Techtoon Trial" },
    @{ Name = "License_Monthly.key";  Type = "monthly"; Days = 30; Customer = "Techtoon Monthly" },
    @{ Name = "License_Yearly.key";   Type = "yearly";  Days = 365; Customer = "Techtoon Yearly" },
    @{ Name = "License_Lifetime.key"; Type = "lifetime"; Days = 0;  Customer = "Techtoon Lifetime" }
)

foreach ($spec in $licenseSpecs) {
    $out = Join-Path $licDir $spec.Name
    $argsList = @("generate", "--type", $spec.Type, "--customer", $spec.Customer, "--out", $out)
    if ($spec.Days -gt 0) { $argsList += @("--days", "$($spec.Days)") }
    $toolOut = & $tool @argsList 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Host "license generation failed for $($spec.Name): $toolOut" -ForegroundColor Red
        exit 1
    }
    Write-Host "      $($spec.Name) -> OK" -ForegroundColor DarkGray
}

# التحقق من أن كل رخصة مولّدة تُقبل فعليًا (توقيع + صلاحية)
foreach ($spec in $licenseSpecs) {
    $out = Join-Path $licDir $spec.Name
    $v = & $tool verify $out 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Host "VERIFY FAILED for $($spec.Name):" -ForegroundColor Red
        Write-Host $v -ForegroundColor Red
        exit 1
    }
    Write-Host "      verify $($spec.Name) -> OK" -ForegroundColor DarkGray
}

# ---------- 5) أدوات مساعدة للعميل ----------
$bat = Join-Path $dist "run_techtoon.bat"
@"
@echo off
chcp 65001 >nul
title Techtoon Accounting
cd /d "%~dp0Application"
if not exist "Techtoon.UI.exe" (
    echo [ERROR] Techtoon.UI.exe not found in "%~dp0Application"
    pause
    exit /b 1
)
start "" "Techtoon.UI.exe"
"@ | Set-Content -Path $bat -Encoding ASCII

$statusBat = Join-Path $dist "check_license.bat"
@"
@echo off
chcp 65001 >nul
title Techtoon - License Status
cd /d "%~dp0Tools"
if not exist "Techtoon.LicenseTool.exe" (
    echo [ERROR] Techtoon.LicenseTool.exe not found.
    pause
    exit /b 1
)
echo === Techtoon Accounting - License Status ===
echo.
Techtoon.LicenseTool.exe status
echo.
pause
"@ | Set-Content -Path $statusBat -Encoding ASCII

# ---------- 6) ملخص ----------
Write-Host "[4/4] verifying output..." -ForegroundColor Green
$exe = Join-Path $appDir "Techtoon.UI.exe"
if (-not (Test-Path $exe)) { Write-Host "Techtoon.UI.exe missing!" -ForegroundColor Red; exit 1 }
$sizeMb = [math]::Round((Get-Item $exe).Length / 1MB, 1)
Write-Host ""
Write-Host "  Application : $exe ($sizeMb MB)" -ForegroundColor Green
Write-Host "  Tools       : $tool" -ForegroundColor Green
Write-Host "  Licenses    : $licDir ($($licenseSpecs.Count) signed licenses)" -ForegroundColor Green
Write-Host "  Documentation: $docDir" -ForegroundColor Green
Write-Host ""
Write-Host "DONE." -ForegroundColor Cyan
