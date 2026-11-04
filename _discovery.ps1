 $ErrorActionPreference = "Continue"
 $root = (Get-Location).Path
 $out = Join-Path $root "phase-a-discovery.md"
 $sb = [System.Text.StringBuilder]::new()
function W($s="") { [void]$sb.AppendLine($s) }
function H($s) { W; W "## $s"; W }
function Sub($s) { W; W "### $s"; W }
function Code($s) { W "```"; W $s; W "```" }

W "# Techtoon Phase A Discovery"
W "_Generated: $(Get-Date -Format 'yyyy-MM-dd HH:mm')_"
W "_Root: $root_"

H "0. Directory Tree"
 $excl = @('bin','obj','.git','node_modules','__pycache__','.vs','packages','.idea','.venv','venv')
Get-ChildItem -Path $root -Directory | Where-Object { $excl -notcontains $_.Name } | ForEach-Object {
    $d = $_
    W "- $($d.Name)/"
    Get-ChildItem -Path $d.FullName -File -ErrorAction SilentlyContinue | Select-Object -First 25 | ForEach-Object { W "  - $($_.Name)" }
    Get-ChildItem -Path $d.FullName -Directory -ErrorAction SilentlyContinue | Where-Object { $excl -notcontains $_.Name } | ForEach-Object {
        W "  - $($_.Name)/"
        Get-ChildItem -Path $_.FullName -File -ErrorAction SilentlyContinue | Select-Object -First 15 | ForEach-Object { W "    - $($_.Name)" }
    }
}

H "1. BUILD.md"
 $b = Join-Path $root "BUILD.md"
if (Test-Path $b) {
    $c = Get-Content $b -Raw -Encoding UTF8
    $c = $c -replace '(?i)password\s*=\s*[^;\s"<>'']+', 'password=***'
    $c = $c -replace '(?i)postgresql://[^:]+:[^@]+@', 'postgresql://***:***@'
    Code $c
} else {
    W "**NOT FOUND**"
    Get-ChildItem -Path $root -Recurse -Filter "BUILD.md" -ErrorAction SilentlyContinue | Select-Object -First 3 | ForEach-Object { W "- $($_.FullName)" }
}

H "2. Solution & csproj"
Get-ChildItem -Path $root -Recurse -Include "*.sln","*.csproj" -ErrorAction SilentlyContinue | Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' } | ForEach-Object {
    Sub $_.Name
    Code (Get-Content $_.FullName -Raw -Encoding UTF8)
}

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
    Sub "Other .cs"
    Get-ChildItem -Path $apiDir -Recurse -Filter "*.cs" -ErrorAction SilentlyContinue | Where-Object { $_.Name -notmatch 'Controller' -and $_.FullName -notmatch '\\(bin|obj)\\' } | Select-Object -First 20 | ForEach-Object {
        W "#### $($_.FullName.Replace($apiDir,''))"
        Code (Get-Content $_.FullName -Raw -Encoding UTF8)
    }
} else { W "**Techtoon.Api NOT FOUND**" }

H "4. Techtoon.Core"
 $cDir = Join-Path $root "Techtoon.Core"
if (Test-Path $cDir) {
    Get-ChildItem -Path $cDir -Recurse -Filter "*.cs" -ErrorAction SilentlyContinue | Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' } | Select-Object -First 30 | ForEach-Object {
        W "#### $($_.FullName.Replace($cDir,''))"
        $lines = Get-Content $_.FullName -Encoding UTF8 | Select-Object -First 60
        W "```"; $lines | ForEach-Object { W $_ }; W "```"
    }
} else { W "**NOT FOUND**" }

H "5. Techtoon.Data"
 $dDir = Join-Path $root "Techtoon.Data"
if (Test-Path $dDir) {
    Get-ChildItem -Path $dDir -Recurse -Filter "*.cs" -ErrorAction SilentlyContinue | Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' } | Select-Object -First 20 | ForEach-Object {
        W "#### $($_.FullName.Replace($dDir,''))"
        $lines = Get-Content $_.FullName -Encoding UTF8 | Select-Object -First 80
        W "```"; $lines | ForEach-Object { W $_ }; W "```"
    }
} else { W "**NOT FOUND**" }

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
        W "```"; $lines | ForEach-Object { W $_ }; W "```"
    }
} else { W "**NOT FOUND**" }

H "7. ViewModels"
 $vDir = Join-Path $root "Techtoon.UI\ViewModels"
if (Test-Path $vDir) {
    Get-ChildItem -Path $vDir -Filter "*.cs" -ErrorAction SilentlyContinue | ForEach-Object { W "- $($_.Name)  ($($_.Length) bytes)" }
}

H "8. Techtoon.Streamlit"
 $stDir = Join-Path $root "Techtoon.Streamlit"
if (Test-Path $stDir) {
    Sub "Files"
    Get-ChildItem -Path $stDir -Recurse -File -ErrorAction SilentlyContinue | Where-Object { $_.FullName -notmatch '\\(__pycache__|\.venv)\\' } | ForEach-Object { W "- $($_.FullName.Replace($stDir,''))  ($($_.Length) bytes)" }
    Sub "app.py (first 150 lines)"
    $a = Join-Path $stDir "app.py"
    if (Test-Path $a) { $lines = Get-Content $a -Encoding UTF8 | Select-Object -First 150; W "```"; $lines | ForEach-Object { W $_ }; W "```" }
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
            $lines = Get-Content $_.FullName -Encoding UTF8 | Select-Object -First 25
            W "```"; $lines | ForEach-Object { W $_ }; W "```"
        }
    }
} else { W "**NOT FOUND**" }

H "9. Mobile App Files"
 $mDirs = @("Techtoon.Mobile","Techtoon.MobileApp","Techtoon.App.Mobile","mobile","app")
foreach ($mn in $mDirs) {
    $mp = Join-Path $root $mn
    if (Test-Path $mp) {
        Sub "Found: $mn"
        Get-ChildItem -Path $mp -Recurse -File -ErrorAction SilentlyContinue | Where-Object { $_.FullName -notmatch '\\(bin|obj|node_modules|build|\.gradle|\.dart_tool)\\' } | Select-Object -First 40 | ForEach-Object { W "- $($_.FullName.Replace($mp,''))  ($($_.Length) bytes)" }
        # show pubspec.yaml or build.gradle or info.plist if exists
        $manifest = Get-ChildItem -Path $mp -Recurse -Include "pubspec.yaml","build.gradle","build.gradle.kts","AndroidManifest.xml","Info.plist","Package.appxmanifest","app.json","app.config.js","package.json" -ErrorAction SilentlyContinue | Select-Object -First 5
        foreach ($mf in $manifest) {
            W "#### $($mf.Name)"
            Code (Get-Content $mf.FullName -Raw -Encoding UTF8)
        }
    }
}
# Also search for any mobile-related folders
W ""
W "### Search for mobile tech indicators"
 $indicators = Get-ChildItem -Path $root -Recurse -File -ErrorAction SilentlyContinue | Where-Object { $_.FullName -notmatch '\\(bin|obj|node_modules|\.git)\\' -and $_.Name -match '^(pubspec\.yaml|build\.gradle|MainActivity|Info\.plist|package\.json|app\.json|capacitor\.config)$' } | Select-Object -First 10
foreach ($i in $indicators) { W "- $($i.FullName.Replace($root,'.'))" }

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

H "11. DB Schema (best effort)"
 $dbPy = Join-Path $root "Techtoon.Streamlit\database.py"
if (Test-Path $dbPy) {
    $raw = Get-Content $dbPy -Raw -Encoding UTF8
    $h=""; $dbn=""; $u=""; $p=""
    $m = [regex]::Match($raw, "(?i)host\s*=\s*[''""]([^''""]+)[''""]"); if ($m.Success) { $h = $m.Groups[1].Value }
    $m = [regex]::Match($raw, "(?i)dbname\s*=\s*[''""]([^''""]+)[''""]"); if ($m.Success) { $dbn = $m.Groups[1].Value }
    $m = [regex]::Match($raw, "(?i)user\s*=\s*[''""]([^''""]+)[''""]"); if ($m.Success) { $u = $m.Groups[1].Value }
    $m = [regex]::Match($raw, "(?i)password\s*=\s*[''""]([^''""]+)[''""]"); if ($m.Success) { $p = $m.Groups[1].Value }
    if ($h -and $u -and $p) {
        W "_Attempting connection (db=$dbn user=$u)_"
        try {
            $py = "import psycopg2; c=psycopg2.connect(host='$h',dbname='$dbn',user='$u',password='$p',sslmode='require'); cur=c.cursor(); cur.execute(\"SELECT table_name FROM information_schema.tables WHERE table_schema='public' ORDER BY 1\"); [print(r[0]) for r in cur.fetchall()]"
            $tr = python -c $py 2>&1
            if ($LASTEXITCODE -eq 0) {
                $tables = $tr -split "`n" | Where-Object { $_.Trim() }
                foreach ($t in $tables) {
                    W "#### $t"
                    $cs = "import psycopg2; c=psycopg2.connect(host='$h',dbname='$dbn',user='$u',password='$p',sslmode='require'); cur=c.cursor(); cur.execute(\"SELECT column_name,data_type,is_nullable FROM information_schema.columns WHERE table_schema='public' AND table_name='$t' ORDER BY ordinal_position\"); [print('|'.join(str(x) for x in r)) for r in cur.fetchall()]"
                    $cr = python -c $cs 2>&1
                    W "```"; ($cr -split "`n") | ForEach-Object { W $_ }; W "```"
                }
            } else { W "_python query failed: $tr_" }
        } catch { W "_error: $_" }
    } else { W "_could not parse connection_" }
} else { W "_database.py not found_" }

H "12. Auth & Permissions"
Get-ChildItem -Path $root -Recurse -Include "AuthService.cs","*Permission*.cs","*Role*.cs","*Jwt*.cs","*Token*.cs" -ErrorAction SilentlyContinue | Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' } | Select-Object -First 15 | ForEach-Object {
    W "#### $($_.FullName.Replace($root,'.'))"
    Code (Get-Content $_.FullName -Raw -Encoding UTF8)
}

H "13. ngrok"
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
    # Check for static domain capability
    try {
        $api = & ngrok api reserved-domains list 2>&1
        W "Reserved domains check:"
        W "```"; $api | ForEach-Object { W $_ }; W "```"
    } catch {}
} else { W "**ngrok NOT installed**" }

H "14. Startup Scripts"
Get-ChildItem -Path $root -Recurse -Include "*.bat","*.ps1","*.cmd" -ErrorAction SilentlyContinue | Where-Object { $_.FullName -notmatch '\\(bin|obj|node_modules|\.git)\\' } | Select-Object -First 15 | ForEach-Object {
    W "#### $($_.FullName.Replace($root,'.'))"
    $c = Get-Content $_.FullName -Raw -Encoding UTF8
    $c = $c -replace '(?i)(password\s*=\s*)[^;\s"''<>]+', '$1***'
    Code $c
}

H "15. SignalR References"
Get-ChildItem -Path $root -Recurse -File -Include "*.cs","*.csproj" -ErrorAction SilentlyContinue | Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' } | Select-String -Pattern "SignalR|Hub|Microsoft.AspNetCore.SignalR" | Select-Object -First 15 | ForEach-Object {
    W "- $($_.Path.Replace($root,'.')):$($_.LineNumber): $($_.Line.Trim())"
}

[System.IO.File]::WriteAllText($out, $sb.ToString(), (New-Object System.Text.UTF8Encoding($false)))
Write-Host ""
Write-Host "=== Discovery complete ===" -ForegroundColor Green
Write-Host "Report: $out" -ForegroundColor Cyan
Write-Host "Size: $((Get-Item $out).Length) bytes" -ForegroundColor Cyan