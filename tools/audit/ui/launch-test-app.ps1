param(
  [string]$DbName = "techtoon_test",
  [int]$WaitSec = 120
)
# يشغّل تطبيق WPF متصلًا بقاعدة اختبار معزولة (TECHTOON_PG_CONNECTION) — لا يمس اتصال المستخدم المخزَّن
$ErrorActionPreference = 'Continue'
$probe = "C:\Users\fady1\AppData\Local\Temp\opencode\schema-probe"
$exe = "D:\PROG\Accounting_Prog\Techtoon_Accounting_C+\Techtoon.UI\bin\Debug\net10.0-windows\Techtoon.UI.exe"
$csFile = "C:\Users\fady1\AppData\Local\Temp\opencode\testcs.txt"
if (-not (Test-Path $exe)) { Write-Output "EXE NOT FOUND"; exit 1 }

$env:TECHTOON_TEST_DB = $DbName
$env:TECHTOON_TEST_CS_FILE = $csFile
Push-Location $probe
& dotnet run --nologo -v q 2>&1 | Select-Object -Last 1
Pop-Location

if (-not (Test-Path $csFile)) { Write-Output "NO CS FILE"; exit 1 }
$env:TECHTOON_PG_CONNECTION = (Get-Content $csFile -Raw).Trim()
Write-Output "launching app against isolated test DB..."
$p = Start-Process -FilePath $exe -PassThru
Write-Output "PID=$($p.Id)"
$sw = [Diagnostics.Stopwatch]::StartNew()
while ($sw.Elapsed.TotalSeconds -lt $WaitSec) {
    Start-Sleep -Seconds 2
    if ($p.HasExited) { Write-Output "APP EXITED EARLY code=$($p.ExitCode)"; exit 1 }
    $p.Refresh()
    if ($p.MainWindowTitle -ne "") { Write-Output "WINDOW: '$($p.MainWindowTitle)' after $([int]$sw.Elapsed.TotalSeconds)s"; exit 0 }
}
Write-Output "TIMEOUT waiting for window (app still running: $(-not $p.HasExited))"
