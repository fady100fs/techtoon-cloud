# =====================================================================
#  بناء حزمة التثبيت: TechtoonSetup.exe
#  1) نشر التطبيق كملف EXE واحد (self-contained) — لا يحتاج .NET مثبّت
#  2) تشغيل Inno Setup (ISCC) لإنتاج ملف التثبيت
#  المتطلبات: Inno Setup 6.x مثبّت  (https://jrsoftware.org/isdl.php)
# =====================================================================
param(
    [switch]$SkipInstaller,      # نشر الـ EXE فقط (بدون Inno)
    [string]$Configuration = "Release"
)
$ErrorActionPreference = "Stop"
$root = Resolve-Path (Join-Path $PSScriptRoot "..\..")
Set-Location $root

$dist = Join-Path $root "dist"
$publish = Join-Path $dist "publish"
Write-Host "=== Techtoon Installer Builder ===" -ForegroundColor Cyan

# ---------- 1) تنظيف ----------
foreach ($d in @($dist)) { if (Test-Path $d) { Remove-Item $d -Recurse -Force } }
New-Item -ItemType Directory -Force -Path $dist | Out-Null

# ---------- 2) نشر التطبيق (ملف واحد) ----------
Write-Host "[1/3] publish (win-x64, self-contained, single file)..." -ForegroundColor Green
dotnet publish "Techtoon.UI\Techtoon.UI.csproj" `
    -c $Configuration `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -p:DebugType=None `
    -p:DebugSymbols=false `
    -o $publish `
    --nologo
if ($LASTEXITCODE -ne 0) { Write-Host "publish failed" -ForegroundColor Red; exit 1 }

# نسخ ملفات إضافية (خطوط/شعار) إن وُجدت
foreach ($extra in @("fonts", "logo.ico", "LICENSE.txt")) {
    $src = Join-Path $root $extra
    if (Test-Path $src) {
        if ((Get-Item $src).PSIsContainer) {
            Copy-Item $src -Destination $publish -Recurse -Force -ErrorAction SilentlyContinue
        } else {
            Copy-Item $src -Destination $publish -Force
        }
    }
}
$exe = Join-Path $publish "Techtoon.UI.exe"
if (-not (Test-Path $exe)) { Write-Host "EXE not produced!" -ForegroundColor Red; exit 1 }
$sizeMb = [math]::Round((Get-Item $exe).Length / 1MB, 1)
Write-Host "      EXE: $exe ($sizeMb MB)" -ForegroundColor DarkGray

# ---------- 3) Inno Setup ----------
if ($SkipInstaller) {
    Write-Host "[2/3] skipped (SkipInstaller)" -ForegroundColor Yellow
    Write-Host "[3/3] EXE جاهز: $exe" -ForegroundColor Green
    exit 0
}

$isccCandidates = @(
    "${env:ProgramFiles(x86)}\Inno Setup 7\ISCC.exe",
    "${env:ProgramFiles}\Inno Setup 7\ISCC.exe",
    "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
    "${env:ProgramFiles}\Inno Setup 6\ISCC.exe",
    "${env:ProgramFiles(x86)}\Inno Setup 5\ISCC.exe",
    "${env:ProgramFiles}\Inno Setup 5\ISCC.exe"
)
$iscc = $isccCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $iscc) {
    $cmd = Get-Command ISCC.exe -ErrorAction SilentlyContinue
    if ($cmd) { $iscc = $cmd.Source }
}
if (-not $iscc) {
    Write-Host "[2/3] ISCC.exe غير موجود — ثبّت Inno Setup 6 ثم أعد التشغيل:" -ForegroundColor Yellow
    Write-Host "        https://jrsoftware.org/isdl.php" -ForegroundColor Yellow
    Write-Host "      الـ EXE جاهز الآن: $exe" -ForegroundColor Green
    exit 0
}

Write-Host "[2/3] building installer with $iscc ..." -ForegroundColor Green
$iss = Join-Path $PSScriptRoot "Techtoon.iss"
& $iscc $iss
if ($LASTEXITCODE -ne 0) { Write-Host "ISCC failed" -ForegroundColor Red; exit 1 }

$setup = Join-Path $dist "TechtoonSetup.exe"
if (Test-Path $setup) {
    $mb = [math]::Round((Get-Item $setup).Length / 1MB, 1)
    Write-Host "[3/3] DONE: $setup ($mb MB)" -ForegroundColor Green
} else {
    Write-Host "[3/3] installer not produced" -ForegroundColor Red
    exit 1
}
