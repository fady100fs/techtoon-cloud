Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass -Force

$out = Join-Path (Get-Location) "phase-a-discovery.md"
$sb = [System.Text.StringBuilder]::new()
function W($s = "") { [void]$sb.AppendLine($s) }
function H($s) { W; W "## $s"; W }
function Sub($s) { W; W "### $s"; W }
function Code($s) { W '```'; W $s; W '```' }
function CodeLines($lines) { W '```'; foreach ($l in $lines) { W $l }; W '```' }

W "# Techtoon Phase A Discovery"
W "_Generated: $(Get-Date -Format 'yyyy-MM-dd HH:mm')_"
W "_Root: $((Get-Location).Path)_"

$root = (Get-Location).Path
$excl = @('bin','obj','.git','node_modules','__pycache__','.vs','packages','.idea','.venv','venv')

# 0. Directory tree
H "0. Directory Tree"
Get-ChildItem -Path $root -Directory | Where-Object { $excl -notcontains $_.Name } | ForEach-Object {
    $d = $_
    W "- $($d.Name)/"
    Get-ChildItem -Path $d.FullName -File -ErrorAction SilentlyContinue | Select-Object -First 25 | ForEach-Object { W "  - $($_.Name)" }
    Get-ChildItem -Path $d.FullName -Directory -ErrorAction SilentlyContinue | Where-Object { $excl -notcontains $_.Name } | ForEach-Object {
        W "  - $($_.Name)/"
        Get-ChildItem -Path $_.FullName -File -ErrorAction SilentlyContinue | Select-Object -First 15 | ForEach-Object { W "    - $($_.Name)" }
    }
}

# 1. BUILD.md
H "1. BUILD.md"
$b = Join-Path $root "BUILD.md"
if (Test-Path $b) {
    $c = Get-Content $b -Raw -Encoding UTF8
    $c = $c -replace '(?i)password\s*=\s*[^;\s"<>'']+', 'password=***'
    $c = $c -replace '(?i)postgresql://[^:]+:[^@]+@', 'postgresql://***:***@'
    Code $c
} else {
    W "**NOT FOUND at root**"
    Get-ChildItem -Path $root -Recurse -Filter "BUILD.md" -ErrorAction SilentlyContinue | Select-Object -First 3 | ForEach-Object { W "- $($_.FullName)" }
}

# 2. Solution & csproj
H "2. Solution & csproj"
Get-ChildItem -Path $root -Recurse -Include "*.sln","*.csproj" -ErrorAction SilentlyContinue | Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' } | ForEach-Object {
    Sub $_.Name
    Code (Get-Content $_.FullName -Raw -Encoding UTF8)
}

# 3. Techtoon.Api
H "3. Techtoon.Api"
$apiDir = Join-Path $root "Techtoon.Api"
if (Test-Path $apiDir) {
    Sub "Files"
    Get-ChildItem -Path $apiDir -Recurse -File -ErrorAction SilentlyContinue | Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' } | ForEach-Object { W "- $($_.FullName.Replace($apiDir,''))  ($($_.Length) bytes)" }
    Sub "Program.cs"
    $p = Get-ChildItem -Path $apiDir -Recurse -Include "Program.cs","Startup.cs" -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($p) { Code (Get-Content $p.FullName -Raw -Encoding UTF8) }
    Sub "appsettings.json (redacted)"
    $a = Join-Path $apiDir "appsettings.json"
    if (Test-Path $a) {
        $c = Get-Content $a -Raw -Encoding UTF8
        $c = $c -replace '(?i)("password"[^:]*:\s*")[^"]+', '$1***'
        $c = $c -replace '(?i)("connectionstring"[^:]*:\s*")[^"]+', '$1***'
        $c = $c -replace '(?i)("secret"[^:]*:\s*")[^"]+', '$1***'
        Code $c
    }
    Sub "Controllers"
    Get-ChildItem -Path $apiDir -Recurse -Filter "*Controller*.cs" -ErrorAction SilentlyContinue | ForEach-Object {
        W "#### $($_.Name)"
        Code (Get-Content $_.FullName -Raw -Encoding UTF8)
    }
    Sub "Other .cs files"
    Get-ChildItem -Path $apiDir -Recurse -Filter "*.cs" -ErrorAction SilentlyContinue | Where-Object { $_.Name -notmatch 'Controller' -and $_.FullName -notmatch '\\(bin|obj)\\' } | Select-Object -First 20 | ForEach-Object {
        W "#### $($_.FullName.Replace($apiDir,''))"
        Code (Get-Content $_.FullName -Raw -Encoding UTF8)
    }
} else { W "**Techtoon.Api NOT FOUND**" }

# 4. Techtoon.Core
H "4. Techtoon.Core"
$cDir = Join-Path $root "Techtoon.Core"
if (Test-Path $cDir) {
    Get-ChildItem -Path $cDir -Recurse -Filter "*.cs" -ErrorAction SilentlyContinue | Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' } | Select-Object -First 30 | ForEach-Object {
        W "#### $($_.FullName.Replace($cDir,''))"
        $lines = Get-Content $_.FullName -Encoding UTF8 | Select-Object -First 60
        CodeLines $lines
    }
} else { W "**NOT FOUND**" }

# 5. Techtoon.Data
H "5. Techtoon.Data"
$dDir = Join-Path $root "Techtoon.Data"
if (Test-Path $dDir) {
    Get-ChildItem -Path $dDir -Recurse -Filter "*.cs" -ErrorAction SilentlyContinue | Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' } | Select-Object -First 20 | ForEach-Object {
        W "#### $($_.FullName.Replace($dDir,''))"
        $lines = Get-Content $_.FullName -Encoding UTF8 | Select-Object -First 80
        CodeLines $lines
    }
} else { W "**NOT FOUND**" }

# 6. Techtoon.UI/Services
H "6. Techtoon.UI/Services"
$sDir = Join-Path $root "Techtoon.UI\Services"
if (Test-Path $sDir) {
    Sub "Files"
    Get-ChildItem -Path $sDir -Filter "*.cs" -ErrorAction SilentlyContinue | ForEach-Object { W "- $($_.Name)  ($($_.Length) bytes)" }
    Sub "AuthService.cs"
    $a = Join-Path $sDir "AuthService.cs"
    if (Test-Path $a) { Code (Get-Content $a -Raw -Encoding UTF8) }
    Sub "SecureConnectionManager.cs (redacted)"
    $sc = Join-Path $sDir "SecureConnectionManager.cs"
    if (Test-Path $sc) {
        $c = Get-Content $sc -Raw -Encoding UTF8
        $c = $c -replace '(?i)(password\s*=\s*)[^;\s"''<>]+', '$1***'
        $c = $c -replace '(?i)postgresql://[^:]+:[^@]+@', 'postgresql://***@'
        Code $c
    }
    Sub "Public method signatures"
    Get-ChildItem -Path $sDir -Filter "*.cs" -ErrorAction SilentlyContinue | ForEach-Object {
        W "#### $($_.Name)"
        $lines = Get-Content $_.FullName -Encoding UTF8 | Where-Object { $_ -match '^\s*public\s+(async\s+)?(Task|void|bool|int|string|List|IEnumerable|decimal|double|float|DateTime|[A-Z]\w+)' }
        CodeLines $lines
    }
} else { W "**NOT FOUND**" }

# 7. ViewModels
H "7. ViewModels"
$vDir = Join-Path $root "Techtoon.UI\ViewModels"
if (Test-Path $vDir) {
    Get-ChildItem -Path $vDir -Filter "*.cs" -ErrorAction SilentlyContinue | ForEach-Object { W "- $($_.Name)  ($($_.Length) bytes)" }
}

# 8. Streamlit
H "8. Techtoon.Streamlit"
$stDir = Join-Path $root "Techtoon.Streamlit"
if (Test-Path $stDir) {
    Sub "Files"
    Get-ChildItem -Path $stDir -Recurse -File -ErrorAction SilentlyContinue | Where-Object { $_.FullName -notmatch '\\(__pycache__|\.venv)\\' } | ForEach-Object { W "- $($_.FullName.Replace($stDir,''))  ($($_.Length) bytes)" }
    Sub "app.py (first 150 lines)"
    $a = Join-Path $stDir "app.py"
    if (Test-Path $a) { CodeLines (Get-Content $a -Encoding UTF8 | Select-Object -First 150) }
    Sub "database.py (REDACTED)"
    $d = Join-Path $stDir "database.py"
    if (Test-Path $d) {
        $c = Get-Content $d -Raw -Encoding UTF8
        $c = $c -replace '(?i)(password\s*=\s*["''])[^;"''\s]+', '$1***'
        $c = $c -replace '(?i)(host\s*=\s*["''])[^;"''\s]+', '$1***'
        $c = $c -replace '(?i)(user\s*=\s*["''])[^;"''\s]+', '$1***'
        $c = $c -replace 'postgresql://[^:]+:[^@]+@', 'postgresql://***:***@'
        Code $c
    }
    Sub "Pages"
    $pDir = Join-Path $stDir "pages"
    if (Test-Path $pDir) {
        Get-ChildItem -Path $pDir -Filter "*.py" -ErrorAction SilentlyContinue | ForEach-Object {
            W "#### $($_.Name)"
            CodeLines (Get-Content $_.FullName -Encoding UTF8 | Select-Object -First 25)
        }
    }
} else { W "**NOT FOUND**" }

# 9. Mobile app files
H "9. Mobile App"
$mDirs = @("Techtoon.Mobile","Techtoon.MobileApp","Techtoon.App.Mobile","mobile","app")
foreach ($mn in $mDirs) {
    $mp = Join-Path $root $mn
    if (Test-Path $mp) {
        Sub "Found: $mn"
        Get-ChildItem -Path $mp -Recurse -File -ErrorAction SilentlyContinue | Where-Object { $_.FullName -notmatch '\\(bin|obj|node_modules|build|\.gradle|\.dart_tool)\\' } | Select-Object -First 40 | ForEach-Object { W "- $($_.FullName.Replace($mp,''))  ($($_.Length) bytes)" }
        $manifest = Get-ChildItem -Path $mp -Recurse -Include "pubspec.yaml","build.gradle","build.gradle.kts","AndroidManifest.xml","Info.plist","app.json","package.json" -ErrorAction SilentlyContinue | Select-Object -First 5
        foreach ($mf in $manifest) {
            W "#### $($mf.Name)"
            Code (Get-Content $mf.FullName -Raw -Encoding UTF8)
        }
    }
}
Sub "Mobile tech indicators (search)"
$indicators = Get-ChildItem -Path $root -Recurse -File -ErrorAction SilentlyContinue | Where-Object { $_.FullName -notmatch '\\(bin|obj|node_modules|\.git)\\' -and $_.Name -match '^(pubspec\.yaml|build\.gradle|MainActivity|Info\.plist|package\.json|app\.json|capacitor\.config)$' } | Select-Object -First 10
foreach ($i in $indicators) { W "- $($i.FullName.Replace($root,'.'))" }

# 10. Connection config
H "10. Connection Config (redacted)"
Get-ChildItem -Path $root -Recurse -Include "appsettings*.json",".env*","*.config" -ErrorAction SilentlyContinue | Where-Object { $_.FullName -notmatch '\\(bin|obj|node_modules)\\' } | Select-Object -First 8 | ForEach-Object {
    W "#### $($_.FullName.Replace($root,'.'))"
    $c = Get-Content $_.FullName -Raw -Encoding UTF8
    $c = $c -replace '(?i)("password"[^:]*:\s*")[^"]+', '$1***'
    $c = $c -replace '(?i)(password\s*=\s*)[^;\s"''<>]+', '$1***'
    $c = $c -replace '(?i)("connectionstring"[^:]*:\s*")[^"]+', '$1***'
    $c = $c -replace 'postgresql://[^:]+:[^@]+@', 'postgresql://***:***@'
    Code $c
}

# 11. Auth & Permissions
H "11. Auth & Permissions"
Get-ChildItem -Path $root -Recurse -Include "AuthService.cs","*Permission*.cs","*Role*.cs","*Jwt*.cs","*Token*.cs" -ErrorAction SilentlyContinue | Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' } | Select-Object -First 15 | ForEach-Object {
    W "#### $($_.FullName.Replace($root,'.'))"
    Code (Get-Content $_.FullName -Raw -Encoding UTF8)
}

# 12. ngrok
H "12. ngrok"
$ng = Get-Command ngrok -ErrorAction SilentlyContinue
if ($ng) {
    W "Found: $($ng.Source)"
    try { $v = & ngrok version 2>&1; W "Version: $v" } catch {}
    $cfg = Join-Path $env:USERPROFILE "AppData\Local\ngrok\ngrok.yml"
    if (Test-Path $cfg) {
        $c = Get-Content $cfg -Raw
        $c = $c -replace '(?i)(authtoken:\s*).+', '$1***'
        Code $c
    }
} else { W "**ngrok NOT installed**" }

# 13. Startup scripts
H "13. Startup Scripts"
Get-ChildItem -Path $root -Recurse -Include "*.bat","*.ps1","*.cmd" -ErrorAction SilentlyContinue | Where-Object { $_.FullName -notmatch '\\(bin|obj|node_modules|\.git)\\' } | Select-Object -First 15 | ForEach-Object {
    W "#### $($_.FullName.Replace($root,'.'))"
    $c = Get-Content $_.FullName -Raw -Encoding UTF8
    $c = $c -replace '(?i)(password\s*=\s*)[^;\s"''<>]+', '$1***'
    Code $c
}

# 14. SignalR references
H "14. SignalR References"
Get-ChildItem -Path $root -Recurse -File -Include "*.cs","*.csproj" -ErrorAction SilentlyContinue | Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' } | Select-String -Pattern "SignalR|Hub|Microsoft.AspNetCore.SignalR" | Select-Object -First 15 | ForEach-Object {
    W "- $($_.Path.Replace($root,'.')):$($_.LineNumber): $($_.Line.Trim())"
}

[System.IO.File]::WriteAllText($out, $sb.ToString(), (New-Object System.Text.UTF8Encoding($false)))
Write-Host ""
Write-Host "=== Discovery complete ===" -ForegroundColor Green
Write-Host "Report: $out" -ForegroundColor Cyan
Write-Host "Size: $((Get-Item $out).Length) bytes" -ForegroundColor Cyan
