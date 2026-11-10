$ErrorActionPreference = 'SilentlyContinue'
Write-Output "=== INSERT statements in product code (Techtoon.UI / Techtoon.Api) ==="
$hits = @()
Get-ChildItem "Techtoon.UI","Techtoon.Api" -Recurse -Include *.cs | Where-Object { $_.FullName -notmatch 'obj\\|bin\\|Backup\\' } | ForEach-Object {
    $f = $_
    $lines = Get-Content $f.FullName
    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($lines[$i] -match 'INSERT INTO\s+(\w+)') {
            $t = $Matches[1]
            # gather the column list (may span lines)
            $seg = ""
            $j = $i
            while ($seg.Length -lt 600 -and $j -lt $lines.Count) { $seg += " " + $lines[$j]; $j++ }
            $cols = ""
            if ($seg -match 'INSERT INTO\s+' + $t + '\s*\(([^)]*)\)') { $cols = $Matches[1] }
            $hits += [PSCustomObject]@{
                Table = $t; File = $f.Name; Line = ($i + 1)
                Cols = ($cols -replace '\s+', ' ').Trim()
            }
        }
    }
}
$hits | Sort-Object Table, File | Format-Table -AutoSize -Wrap | Out-String -Width 200
