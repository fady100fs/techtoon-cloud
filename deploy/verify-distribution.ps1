# =====================================================================
#  --- ---- ----- ------- - -- --- ------- ------ (-- --------)
#
#  1) ------: ------ --------
#  2) EXE -------: ---- ----- ------- (----- -----)
#  3) ---- -------: status / verify / machine-id / generate
#  4) -----: ---- ------ ------
#  5) ------ -----: ----- ------ -----
#  6) ------ -----------
# =====================================================================
param(
    [string]$Distribution = "deploy\distribution",
    [switch]$SkipBuild
)
$ErrorActionPreference = "Continue"
$root = Resolve-Path (Join-Path $PSScriptRoot "..")
Set-Location $root

$pass = 0; $fail = 0
function Check($name, $condition, $detail = "") {
    if ($condition) {
        Write-Host ("  [PASS] " + $name) -ForegroundColor Green
        $script:pass++
    } else {
        $suffix = ""
        if ($detail) { $suffix = " -- " + $detail }
        Write-Host ("  [FAIL] " + $name + $suffix) -ForegroundColor Red
        $script:fail++
    }
}

Write-Host "=== Techtoon Distribution Verification ===" -ForegroundColor Cyan

# ---------- 1) ------ ----------
Write-Host "`n[1] Package structure" -ForegroundColor Yellow
$dist = Join-Path $root $Distribution
Check "distribution folder exists" (Test-Path $dist)
Check "Techtoon.UI.exe exists" (Test-Path (Join-Path $dist "Application\Techtoon.UI.exe"))
Check "Techtoon.LicenseTool.exe exists" (Test-Path (Join-Path $dist "Tools\Techtoon.LicenseTool.exe"))
Check "run_techtoon.bat exists" (Test-Path (Join-Path $dist "run_techtoon.bat"))
Check "check_license.bat exists" (Test-Path (Join-Path $dist "check_license.bat"))
Check "issue_license.bat exists" (Test-Path (Join-Path $dist "issue_license.bat"))
Check "README.md exists" (Test-Path (Join-Path $dist "Documentation\README.md"))
Check "INSTALLATION_GUIDE.md exists" (Test-Path (Join-Path $dist "Documentation\INSTALLATION_GUIDE.md"))
Check "schema_full.sql exists" (Test-Path (Join-Path $dist "Documentation\schema_full.sql"))
foreach ($n in @("License_Trial.key","License_Monthly.key","License_Yearly.key","License_Lifetime.key")) {
    Check "license file $n exists" (Test-Path (Join-Path $dist "Licenses\$n"))
}

# ---------- 2) EXE ------- ----------
Write-Host "`n[2] Main EXE launches" -ForegroundColor Yellow
Get-Process -Name "Techtoon.UI" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Start-Sleep -Milliseconds 500
$exe = Join-Path $dist "Application\Techtoon.UI.exe"
$log = Join-Path $dist "Application\techtoon.log"
if (Test-Path $log) { Remove-Item $log -Force }
$p = Start-Process -FilePath $exe -PassThru -WorkingDirectory (Join-Path $dist "Application")
Start-Sleep -Seconds 12
$running = -not $p.HasExited
Check "Techtoon.UI.exe stays running" $running ("exit=" + $(if ($p.HasExited) { $p.ExitCode } else { "n/a" }))
if (Test-Path $log) {
    $logText = Get-Content $log -Raw
    Check "no fatal error in log" ($logText -notmatch "FATAL") "found FATAL in techtoon.log"
    Check "main window shown" ($logText -match "MainWindow shown")
} else {
    Check "log file written" $false "no techtoon.log"
}

# ---------- 3) ---- ------- ----------
Write-Host "`n[3] License tool" -ForegroundColor Yellow
$tool = Join-Path $dist "Tools\Techtoon.LicenseTool.exe"
$st = & $tool status 2>&1
Check "license tool 'status' runs" ($LASTEXITCODE -eq 0 -or $LASTEXITCODE -eq 1) ($st | Out-String)
$mid = & $tool machine-id 2>&1
Check "license tool 'machine-id' runs" ($LASTEXITCODE -eq 0 -and $mid[0].Length -ge 8)

# ---------- 4) ----- ----------
Write-Host "`n[4] Signed licenses valid" -ForegroundColor Yellow
foreach ($n in @("License_Trial.key","License_Monthly.key","License_Yearly.key","License_Lifetime.key")) {
    $v = & $tool verify (Join-Path $dist "Licenses\$n") 2>&1
    Check "verify $n" ($LASTEXITCODE -eq 0) ($v | Out-String)
}

# ---------- 5) ------ ----- ----------
# ---------- 4b) Ø§ØµØ¯Ø§Ø± Ø±Ø®ØµØ© Ù„Ø¹Ù…ÙŠÙ„ ----------
Write-Host "`n[4b] Issue a customer license" -ForegroundColor Yellow
$issued = Join-Path $env:TEMP ("techtoon_issued_" + [guid]::NewGuid().ToString("N") + ".key")
$toolExe = Join-Path $dist "Tools\Techtoon.LicenseTool.exe"
if (Test-Path $toolExe) {
    $out = & $toolExe generate --type yearly --customer "Verify Customer" --out $issued 2>&1
    Check "customer license is generated" ($LASTEXITCODE -eq 0 -and (Test-Path $issued)) ($out | Out-String)
    if (Test-Path $issued) {
        $v = & $toolExe verify $issued 2>&1
        Check "issued license verifies" ($LASTEXITCODE -eq 0) ($v | Out-String)
        $txt = Get-Content $issued -Raw
        Check "issued license is not machine-bound" ($txt -match '"machine_id": ""') "machine_id must be empty"
        Check "issued license carries the customer name" ($txt -match "Verify Customer")
    }
    Remove-Item $issued -Force -ErrorAction SilentlyContinue
}
Write-Host "`n[5] Tamper rejection" -ForegroundColor Yellow
$tamper = Join-Path $env:TEMP ("techtoon_tamper_" + [guid]::NewGuid().ToString("N") + ".key")
$orig = Get-Content (Join-Path $dist "Licenses\License_Lifetime.key") -Raw
($orig -replace '"issued_to": "Techtoon Lifetime"', '"issued_to": "Hacker"') | Set-Content $tamper -Encoding UTF8
$v = & $tool verify $tamper 2>&1
Check "tampered license rejected" ($LASTEXITCODE -ne 0) ($v | Out-String)
Remove-Item $tamper -Force -ErrorAction SilentlyContinue

# ---------- 6) ------ ----------- ----------
if (-not $SkipBuild) {
    Write-Host "`n[6] Build + tests" -ForegroundColor Yellow
    $b = dotnet build Techtoon.sln -v q --nologo 2>&1
    Check "solution builds" ($LASTEXITCODE -eq 0) ($b | Select-Object -Last 5 | Out-String)
    $t = dotnet test Techtoon.Tests\Techtoon.Tests.csproj -v q --nologo 2>&1
    Check "all tests pass" ($LASTEXITCODE -eq 0) ($t | Select-Object -Last 5 | Out-String)
}

Get-Process -Name "Techtoon.UI" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue

Write-Host ""
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host (" PASSED: " + $pass + "   FAILED: " + $fail) -ForegroundColor $(if ($fail -eq 0) { "Green" } else { "Red" })
Write-Host "=====================================" -ForegroundColor Cyan
if ($fail -gt 0) { exit 1 }
exit 0
