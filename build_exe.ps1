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
