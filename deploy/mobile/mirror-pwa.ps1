# Mirrors the currently deployed Flutter web bundle from techtoon-mobile.fly.dev
# into Techtoon.Mobile/build/web, then rewrites the compiled API base URL to the
# same origin served by Nginx. Needed because this machine has no Flutter SDK;
# replace it with `flutter build web --release --dart-define=API_BASE_URL=...`
# whenever the app is rebuilt from source.
param(
    [string]$BaseUrl = 'https://techtoon-mobile.fly.dev',
    [string]$Destination = ''
)

$ErrorActionPreference = 'Stop'
if ([string]::IsNullOrWhiteSpace($Destination)) {
    $Destination = Join-Path $PSScriptRoot '..\..\Techtoon.Mobile\build\web'
}

$paths = @(
    'index.html', 'flutter.js', 'flutter_bootstrap.js', 'main.dart.js', 'manifest.json',
    'favicon.png', 'version.json', 'flutter_service_worker.js',
    'assets/AssetManifest.bin.json', 'assets/AssetManifest.bin', 'assets/FontManifest.json',
    'assets/NOTICES', 'assets/fonts/MaterialIcons-Regular.otf',
    'assets/packages/cupertino_icons/assets/CupertinoIcons.ttf',
    'canvaskit/canvaskit.js', 'canvaskit/canvaskit.wasm', 'canvaskit/canvaskit.js.symbols',
    'canvaskit/chromium/canvaskit.js', 'canvaskit/chromium/canvaskit.wasm',
    'canvaskit/skwasm.js', 'canvaskit/skwasm.wasm',
    'canvaskit/skwasm_heavy.js', 'canvaskit/skwasm_heavy.wasm',
    'icons/Icon-192.png', 'icons/Icon-512.png',
    'icons/Icon-maskable-192.png', 'icons/Icon-maskable-512.png'
)

New-Item -ItemType Directory -Force -Path $Destination | Out-Null

$total = 0
$failed = @()
foreach ($p in $paths) {
    $out = Join-Path $Destination ($p -replace '/', '\')
    $dir = Split-Path $out -Parent
    if (-not (Test-Path $dir)) { New-Item -ItemType Directory -Force -Path $dir | Out-Null }
    try {
        Invoke-WebRequest -Uri "$BaseUrl/$p" -OutFile $out -UseBasicParsing -TimeoutSec 120
        $total += (Get-Item $out).Length
    } catch {
        $failed += $p
    }
}
if ($failed.Count -gt 0) {
    throw "Failed to download: $($failed -join ', ')"
}

$mainJs = Join-Path $Destination 'main.dart.js'
$txt = [System.IO.File]::ReadAllText($mainJs)
$before = ([regex]::Matches($txt, [regex]::Escape('https://techtoon-api.fly.dev'))).Count
$txt = $txt.Replace('https://techtoon-api.fly.dev', $BaseUrl)
[System.IO.File]::WriteAllText($mainJs, $txt, (New-Object System.Text.UTF8Encoding($false)))

# Cache-bust the entry points so browsers with a stale cached bundle fetch fresh.
$indexHtml = Join-Path $Destination 'index.html'
$ih = [System.IO.File]::ReadAllText($indexHtml)
$ih = $ih.Replace("flutter_service_worker.js?v=1498177836", "flutter_service_worker.js?v=1498177837")
$ih = $ih.Replace("scriptTag.src = 'main.dart.js';", "scriptTag.src = 'main.dart.js?v=2';")
[System.IO.File]::WriteAllText($indexHtml, $ih, (New-Object System.Text.UTF8Encoding($false)))

Write-Host ("downloaded {0} files, {1:N0} bytes; api url rewrites: {2}" -f $paths.Count, $total, $before)
