# Assembles a minimal Fly build context for the combined PWA + API image.
# Only the API projects and the compiled web bundle are copied, so the uploaded
# context stays small even though the repository carries old backups.
param(
    [string]$Context = "$env:TEMP\opencode\techtoon-deploy-context"
)

$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$projects = @('Techtoon.Api', 'Techtoon.Core', 'Techtoon.Data')

if (Test-Path $Context) { Remove-Item -Recurse -Force $Context }
New-Item -ItemType Directory -Force -Path $Context | Out-Null

foreach ($proj in $projects) {
    $src = Join-Path $root $proj
    $files = Get-ChildItem $src -Recurse -File | Where-Object {
        $_.FullName -notmatch '\\(bin|obj|\.vs)\\' -and $_.Name -ne 'appsettings.Development.json'
    }
    foreach ($f in $files) {
        $rel = $f.FullName.Substring($src.Length + 1)
        $target = Join-Path (Join-Path $Context $proj) $rel
        New-Item -ItemType Directory -Force -Path (Split-Path $target -Parent) | Out-Null
        Copy-Item $f.FullName $target -Force
    }
}

$web = Join-Path $root 'Techtoon.Mobile\build\web'
if (-not (Test-Path (Join-Path $web 'main.dart.js'))) {
    throw "Missing compiled web bundle at $web. Run deploy/mobile/mirror-pwa.ps1 or 'flutter build web' first."
}
$webDest = Join-Path $Context 'Techtoon.Mobile\build\web'
New-Item -ItemType Directory -Force -Path $webDest | Out-Null
Copy-Item (Join-Path $web '*') $webDest -Recurse -Force

$kit = Join-Path $root 'deploy\mobile'
$kitDest = Join-Path $Context 'deploy\mobile'
New-Item -ItemType Directory -Force -Path $kitDest | Out-Null
foreach ($name in @('Dockerfile', 'nginx.conf', 'start.sh')) {
    Copy-Item (Join-Path $kit $name) (Join-Path $kitDest $name) -Force
}
Copy-Item (Join-Path $kit 'fly.toml') (Join-Path $Context 'fly.toml') -Force
Copy-Item (Join-Path $root 'deploy\.dockerignore') (Join-Path $Context '.dockerignore') -Force

# The shipped bundle must point at the same origin that Nginx serves.
$mainJs = Join-Path $web 'main.dart.js'
$txt = [System.IO.File]::ReadAllText($mainJs)
$stale = ([regex]::Matches($txt, [regex]::Escape('techtoon-api.fly.dev'))).Count
if ($stale -gt 0) {
    throw "main.dart.js still targets techtoon-api.fly.dev ($stale occurrences). Run deploy/mobile/mirror-pwa.ps1."
}

$files = Get-ChildItem $Context -Recurse -File
$bytes = ($files | Measure-Object Length -Sum).Sum
Write-Host ("context : {0}" -f $Context)
Write-Host ("files   : {0:N0}" -f $files.Count)
Write-Host ("size    : {0:N1} MB" -f ($bytes / 1MB))
