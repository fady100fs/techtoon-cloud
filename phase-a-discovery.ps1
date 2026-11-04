# ============================================================================
# Techtoon - Phase A Discovery Script
# Generates: phase-a-discovery.md  (in current folder)
# SAFE: read-only. Does NOT modify any project file. Redacts secrets.
# ============================================================================
$ErrorActionPreference = "Continue"
$root = (Get-Location).Path
$out = Join-Path $root "phase-a-discovery.md"
$sw = [System.IO.StreamWriter]::new($out, $false, (New-Object System.Text.UTF8Encoding($false)))

function W($s="") { $sw.WriteLine($s) }
function H($s) { W; W "## $s"; W }
function Sub($s) { W; W "### $s"; W }
function Code($s) { W '```'; W $s; W '```' }
function CodeLines($lines) { W '```'; foreach ($l in $lines) { W $l }; W '```' }

W "# Techtoon - Phase A Discovery Report"
W "_Generated: $(Get-Date -Format 'yyyy-MM-dd HH:mm')_"
W "_Root: $root_"

# ---------- 0. Directory tree (depth 3, excluding noise) ----------
H "0. Directory Tree (depth 3)"
$exclude = @('bin','obj','.git','node_modules','__pycache__','.vs','packages','.idea','.venv','venv')
try {
    $tree = Get-ChildItem -Path $root -Directory | Where-Object { $exclude -notcontains $_.Name }
    foreach ($d in $tree) {
        W "- $($d.Name)/"
        $sub = Get-ChildItem -Path $d.FullName -Directory -ErrorAction SilentlyContinue | Where-Object { $exclude -notcontains $_.Name }
        foreach ($sd in $sub) {
            W "  - $($sd.Name)/"
            $files = Get-ChildItem -Path $sd.FullName -File -ErrorAction SilentlyContinue | Select-Object -First 30
            foreach ($f in $files) { W "    - $($f.Name)" }
        }
        $rootFiles = Get-ChildItem -Path $d.FullName -File -ErrorAction SilentlyContinue | Select-Object -First 30
        foreach ($f in $rootFiles) { W "  - $($f.Name)" }
    }
} catch { W "_error: $_" }

# ---------- 1. BUILD.md ----------
H "1. BUILD.md (primary reference)"
$buildMd = Join-Path $root "BUILD.md"
if (Test-Path $buildMd) {
    $content = Get-Content $buildMd -Raw -Encoding UTF8
    # redact obvious secrets
    $content = $content -replace 'password\s*=\s*["'']?[^;"''\s]+', 'password=***REDACTED***' -replace 'Password\s*=\s*[^;\s]+', 'Password=***REDACTED***' -replace 'postgresql://[^:\s]+:[^@\s]+@', 'postgresql://***:***@'
    W "_Size: $($content.Length) chars_"
    Code $content
} else {
    W "**BUILD.md NOT FOUND at root**"
    # search for it
    $found = Get-ChildItem -Path $root -Recurse -Filter "BUILD.md" -ErrorAction SilentlyContinue | Select-Object -First 5
    if ($found) { W "Found elsewhere:"; $found | ForEach-Object { W "- $($_.FullName)" } }
}

# ---------- 2. Solution + project files ----------
H "2. Solution & Project Files"
$slns = Get-ChildItem -Path $root -Recurse -Filter "*.sln" -ErrorAction SilentlyContinue | Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' }
foreach ($sln in $slns) {
    Sub "Solution: $($sln.Name)"
    Code (Get-Content $sln.FullName -Raw -Encoding UTF8)
}
$csprojs = Get-ChildItem -Path $root -Recurse -Filter "*.csproj" -ErrorAction SilentlyContinue | Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' }
foreach ($cp in $csprojs) {
    Sub "Project: $($cp.FullName.Replace($root,'.'))"
    Code (Get-Content $cp.FullName -Raw -Encoding UTF8)
}

# ---------- 3. Techtoon.Api current state (CRITICAL) ----------
H "3. Techtoon.Api - Current State"
$apiDir = Join-Path $root "Techtoon.Api"
if (Test-Path $apiDir) {
    Sub "Files"
    $apiFiles = Get-ChildItem -Path $apiDir -Recurse -File -ErrorAction SilentlyContinue | Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' }
    foreach ($f in $apiFiles) { W "- $($f.FullName.Replace($apiDir,''))  ($($f.Length) bytes)" }

    Sub "Program.cs / Startup"
    $prog = Get-ChildItem -Path $apiDir -Recurse -Include "Program.cs","Startup.cs" -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($prog) { Code (Get-Content $prog.FullName -Raw -Encoding UTF8) } else { W "_not found_" }

    Sub "appsettings.json (redacted)"
    $appset = Join-Path $apiDir "appsettings.json"
    if (Test-Path $appset) {
        $c = Get-Content $appset -Raw -Encoding UTF8
        $c = $c -replace '(?i)("password"\s*:\s*")[^"]+', '$1***REDACTED***'
        $c = $c -replace '(?i)("connectionstring"\s*:\s*")[^"]+', '$1***REDACTED***'
        $c = $c -replace '(?i)("secret"[^:]*:\s*")[^"]+', '$1***REDACTED***'
        Code $c
    } else { W "_not found_" }

    Sub "Controllers"
    $ctrls = Get-ChildItem -Path $apiDir -Recurse -Filter "*Controller*.cs" -ErrorAction SilentlyContinue
    foreach ($c in $ctrls) {
        W "#### $($c.Name)"
        Code (Get-Content $c.FullName -Raw -Encoding UTF8)
    }

    Sub "Other key files (DTOs, Services, Hubs)"
    $others = Get-ChildItem -Path $apiDir -Recurse -File -Include "*.cs" -ErrorAction SilentlyContinue | Where-Object { $_.Name -notmatch 'Controller' -and $_.FullName -notmatch '\\(bin|obj)\\' }
    foreach ($o in $others | Select-Object -First 30) {
        W "#### $($o.FullName.Replace($apiDir,''))"
        Code (Get-Content $o.FullName -Raw -Encoding UTF8)
    }
} else {
    W "**Techtoon.Api directory NOT FOUND** - will need to be created in Phase B"
}

# ---------- 4. Techtoon.Core ----------
H "4. Techtoon.Core - Entities & Services"
$coreDir = Join-Path $root "Techtoon.Core"
if (Test-Path $coreDir) {
    Sub "All .cs files (names + first 80 lines each)"
    $coreFiles = Get-ChildItem -Path $coreDir -Recurse -Filter "*.cs" -ErrorAction SilentlyContinue | Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' }
    foreach ($f in $coreFiles | Select-Object -First 40) {
        W "#### $($f.FullName.Replace($coreDir,''))"
        $lines = Get-Content $f.FullName -Encoding UTF8 | Select-Object -First 80
        CodeLines $lines
    }
} else { W "**Techtoon.Core NOT FOUND**" }

# ---------- 5. Techtoon.Data ----------
H "5. Techtoon.Data - DbContext & Repositories"
$dataDir = Join-Path $root "Techtoon.Data"
if (Test-Path $dataDir) {
    $dataFiles = Get-ChildItem -Path $dataDir -Recurse -Filter "*.cs" -ErrorAction SilentlyContinue | Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' }
    foreach ($f in $dataFiles | Select-Object -First 30) {
        W "#### $($f.FullName.Replace($dataDir,''))"
        $lines = Get-Content $f.FullName -Encoding UTF8 | Select-Object -First 100
        CodeLines $lines
    }
} else { W "**Techtoon.Data NOT FOUND**" }

# ---------- 6. Techtoon.UI - Services list ----------
H "6. Techtoon.UI/Services - File List"
$svcDir = Join-Path $root "Techtoon.UI\Services"
if (Test-Path $svcDir) {
    $svcs = Get-ChildItem -Path $svcDir -Filter "*.cs" -ErrorAction SilentlyContinue
    foreach ($s in $svcs) {
        W "- $($s.Name)  ($($s.Length) bytes)"
    }

    Sub "AuthService.cs (FULL - critical for Phase C)"
    $auth = Join-Path $svcDir "AuthService.cs"
    if (Test-Path $auth) { Code (Get-Content $auth -Raw -Encoding UTF8) }

    Sub "SecureConnectionManager.cs (FULL - critical for security review)"
    $scm = Join-Path $svcDir "SecureConnectionManager.cs"
    if (Test-Path $scm) {
        $c = Get-Content $scm -Raw -Encoding UTF8
        $c = $c -replace '(?i)(password\s*=\s*)[^;\s"''<>]+', '$1***REDACTED***' -replace '(?i)(postgresql://[^:]+:)[^@]+@', '$1***@'
        Code $c
    }

    Sub "Public method signatures across all services"
    foreach ($s in $svcs) {
        W "#### $($s.Name)"
        $lines = Get-Content $s.FullName -Encoding UTF8 | Where-Object { $_ -match '^\s*public\s+(async\s+)?(Task|void|bool|int|string|List|IEnumerable|decimal|double|float|DateTime|[A-Z]\w+)' }
        CodeLines $lines
    }
} else { W "**Techtoon.UI/Services NOT FOUND**" }

# ---------- 7. Techtoon.UI - ViewModels list ----------
H "7. Techtoon.UI/ViewModels - File List"
$vmDir = Join-Path $root "Techtoon.UI\ViewModels"
if (Test-Path $vmDir) {
    Get-ChildItem -Path $vmDir -Filter "*.cs" -ErrorAction SilentlyContinue | ForEach-Object {
        W "- $($_.Name)  ($($_.Length) bytes)"
    }
}

# ---------- 8. Streamlit current state ----------
H "8. Techtoon.Streamlit - Current State"
$stDir = Join-Path $root "Techtoon.Streamlit"
if (Test-Path $stDir) {
    Sub "File tree"
    Get-ChildItem -Path $stDir -Recurse -File -ErrorAction SilentlyContinue | Where-Object { $_.FullName -notmatch '\\(__pycache__|\.venv|venu)\\' } | ForEach-Object {
        W "- $($_.FullName.Replace($stDir,''))  ($($_.Length) bytes)"
    }

    Sub "app.py (first 150 lines)"
    $app = Join-Path $stDir "app.py"
    if (Test-Path $app) { CodeLines (Get-Content $app -Encoding UTF8 | Select-Object -First 150) }

    Sub "database.py (REDACTED - critical: this is the direct DB connection)"
    $db = Join-Path $stDir "database.py"
    if (Test-Path $db) {
        $c = Get-Content $db -Raw -Encoding UTF8
        $c = $c -replace '(?i)(password\s*=\s*["'']?)[^;"''\s]+', '$1***REDACTED***' -replace '(?i)(host\s*=\s*["'']?)[^;"''\s]+', '$1***REDACTED***' -replace '(?i)(user\s*=\s*["'']?)[^;"''\s]+', '$1***REDACTED***' -replace 'postgresql://[^:]+:[^@]+@', 'postgresql://***:***@'
        Code $c
    }

    Sub "Pages list"
    $pagesDir = Join-Path $stDir "pages"
    if (Test-Path $pagesDir) {
        Get-ChildItem -Path $pagesDir -Filter "*.py" -ErrorAction SilentlyContinue | ForEach-Object {
            W "- $($_.Name)  ($($_.Length) bytes)"
            # first 30 lines of each page (function names give the operations)
            $lines = Get-Content $_.FullName -Encoding UTF8 | Select-Object -First 30
            CodeLines $lines
        }
    }
} else { W "**Techtoon.Streamlit NOT FOUND**" }

# ---------- 9. Connection config (redacted) ----------
H "9. Connection Configuration (REDACTED)"
W "Looking for: appsettings.json, .env, database.py, connection.enc references, SecureConnectionManager usage"
$cfgFiles = @()
$cfgFiles += Get-ChildItem -Path $root -Recurse -Include "appsettings*.json",".env*","*.config" -ErrorAction SilentlyContinue | Where-Object { $_.FullName -notmatch '\\(bin|obj|node_modules)\\' } | Select-Object -First 10
foreach ($f in $cfgFiles) {
    W "#### $($f.FullName.Replace($root,'.'))"
    $c = Get-Content $f.FullName -Raw -Encoding UTF8
    $c = $c -replace '(?i)("password"\s*:\s*")[^"]+', '$1***REDACTED***' -replace '(?i)(password\s*=\s*)[^;\s"''<>]+', '$1***REDACTED***' -replace '(?i)("connectionstring"\s*:\s*")[^"]+', '$1***REDACTED***' -replace 'postgresql://[^:]+:[^@]+@', 'postgresql://***:***@' -replace '(?i)(host\s*=\s*["'']?)[^;"''\s]+', '$1***REDACTED***'
    Code $c
}

# ---------- 10. DB schema (if we can connect) ----------
H "10. Database Schema (from information_schema)"
W "_Will attempt to query Neon via Streamlit's database.py or appsettings. If credentials not available in this shell, section will be empty - please paste schema separately if so._"
# try to read connection from Streamlit database.py raw (we need it to query)
$dbPy = Join-Path $stDir "database.py"
$connStr = $null
if (Test-Path $dbPy) {
    $raw = Get-Content $dbPy -Raw -Encoding UTF8
    # extract a psycopg2.connect(...) block - just try to find host/db/user/pass
    $m = [regex]::Match($raw, "(?i)host\s*=\s*['""]([^'""]+)['""]")
    if ($m.Success) {
        $h = $m.Groups[1].Value
        $m2 = [regex]::Match($raw, "(?i)dbname\s*=\s*['""]([^'""]+)['""]"); $dbn = if ($m2.Success) { $m2.Groups[1].Value } else { "" }
        $m3 = [regex]::Match($raw, "(?i)user\s*=\s*['""]([^'""]+)['""]"); $u = if ($m3.Success) { $m3.Groups[1].Value } else { "" }
        $m4 = [regex]::Match($raw, "(?i)password\s*=\s*['""]([^'""]+)['""]"); $p = if ($m4.Success) { $m4.Groups[1].Value } else { "" }
        W "_Connection target (redacted): host=***, db=$dbn, user=$u_"
        # try python
        try {
            $py = "import psycopg2,sys; c=psycopg2.connect(host='$h',dbname='$dbn',user='$u',password='$p',sslmode='require'); cur=c.cursor(); cur.execute(\"SELECT table_name FROM information_schema.tables WHERE table_schema='public' ORDER BY table_name\"); print('\n'.join(r[0] for r in cur.fetchall())); c.close()"
            $tablesRaw = python -c $py 2>&1
            if ($LASTEXITCODE -eq 0) {
                Sub "Tables"
                $tables = $tablesRaw -split "`n" | Where-Object { $_.Trim() }
                foreach ($t in $tables) {
                    W "#### $t"
                    $cols = "import psycopg2; c=psycopg2.connect(host='$h',dbname='$dbn',user='$u',password='$p',sslmode='require'); cur=c.cursor(); cur.execute(\"SELECT column_name,data_type,is_nullable,column_default FROM information_schema.columns WHERE table_schema='public' AND table_name='$t' ORDER BY ordinal_position\"); print('\n'.join('|'.join(str(x) for x in r) for r in cur.fetchall())); c.close()"
                    $colsRaw = python -c $cols 2>&1
                    CodeLines ($colsRaw -split "`n")
                }
            } else { W "_python query failed: $tablesRaw_" }
        } catch { W "_python not available or error: $_" }
    } else { W "_could not parse connection from database.py - please paste schema manually_" }
} else { W "_database.py not found at $dbPy_" }

# ---------- 11. Permissions / Auth files in UI ----------
H "11. Permissions & Auth System"
$authRelated = Get-ChildItem -Path $root -Recurse -Include "AuthService.cs","*Permission*.cs","*Role*.cs","*Jwt*.cs","*Token*.cs" -ErrorAction SilentlyContinue | Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' }
foreach ($f in $authRelated | Select-Object -First 20) {
    W "#### $($f.FullName.Replace($root,'.'))"
    Code (Get-Content $f.FullName -Raw -Encoding UTF8)
}

# ---------- 12. ngrok availability ----------
H "12. ngrok Availability"
$ngrok = Get-Command ngrok -ErrorAction SilentlyContinue
if ($ngrok) {
    W "ngrok found at: $($ngrok.Source)"
    try {
        $ver = & ngrok version 2>&1
        W "Version: $ver"
    } catch {}
    $cfg = Join-Path $env:USERPROFILE "AppData\Local\ngrok\ngrok.yml"
    if (Test-Path $cfg) {
        $c = Get-Content $cfg -Raw
        $c = $c -replace '(?i)(authtoken:\s*).+', '$1***REDACTED***'
        Code $c
    } else { W "_no ngrok.yml config found_" }
} else {
    W "**ngrok NOT installed** - will need to be installed in Phase D"
}

# ---------- 13. Startup scripts ----------
H "13. Startup Scripts (RUN.bat, etc.)"
$starters = Get-ChildItem -Path $root -Recurse -Include "*.bat","*.ps1","*.cmd" -ErrorAction SilentlyContinue | Where-Object { $_.FullName -notmatch '\\(bin|obj|node_modules|\.git)\\' }
foreach ($f in $starters | Select-Object -First 20) {
    W "#### $($f.FullName.Replace($root,'.'))"
    $c = Get-Content $f.FullName -Raw -Encoding UTF8
    $c = $c -replace '(?i)(password\s*=\s*)[^;\s"''<>]+', '$1***REDACTED***' -replace 'postgresql://[^:]+:[^@]+@', 'postgresql://***:***@'
    Code $c
}

# ---------- 14. RUN.bat / run scripts mention ----------
H "14. Mention of 'run.bat' / 'RUN.bat' anywhere"
try {
    $refs = Get-ChildItem -Path $root -Recurse -File -ErrorAction SilentlyContinue | Where-Object { $_.FullName -notmatch '\\(bin|obj|node_modules|\.git)\\' } | Select-String -Pattern "run\.bat" -SimpleMatch | Select-Object -First 10
    foreach ($r in $refs) { W "- $($r.Path.Replace($root,'.')):$($r.LineNumber): $($r.Line.Trim())" }
} catch {}

# ---------- 15. Existing SignalR / Hub references ----------
H "15. Existing SignalR / Real-time References"
try {
    $sig = Get-ChildItem -Path $root -Recurse -File -Include "*.cs","*.csproj" -ErrorAction SilentlyContinue | Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' } | Select-String -Pattern "SignalR|Hub|Microsoft.AspNetCore.SignalR" | Select-Object -First 20
    foreach ($r in $sig) { W "- $($r.Path.Replace($root,'.')):$($r.LineNumber): $($r.Line.Trim())" }
} catch {}

$sw.Close()
$sw.Dispose()

Write-Host ""
Write-Host "=== Discovery complete ===" -ForegroundColor Green
Write-Host "Report written to: $out" -ForegroundColor Cyan
Write-Host "Size: $((Get-Item $out).Length) bytes" -ForegroundColor Cyan
Write-Host ""
Write-Host "Next: share this file (or paste its content) so Phase A report can be produced." -ForegroundColor Yellow
