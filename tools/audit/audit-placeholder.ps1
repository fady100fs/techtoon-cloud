$ErrorActionPreference = 'SilentlyContinue'
Write-Output "=========== 1) XAML BUTTONS WITHOUT COMMAND OR CLICK (dead buttons) ==========="
$dead = @()
Get-ChildItem "Techtoon.UI" -Recurse -Include *.xaml | Where-Object { $_.FullName -notmatch 'obj\\|bin\\' } | ForEach-Object {
    $f = $_
    $lines = Get-Content $f.FullName
    for ($i = 0; $i -lt $lines.Count; $i++) {
        $l = $lines[$i]
        if ($l -match '<Button\b') {
            # collect the whole button tag (may span lines)
            $tag = $l
            $j = $i
            while ($tag -notmatch '/>' -and $j -lt $lines.Count - 1) { $j++; $tag += " " + $lines[$j] }
            if ($tag -notmatch 'Command=' -and $tag -notmatch 'Click=') {
                $name = if ($tag -match 'x:Name="([^"]+)"') { $Matches[1] } else { '(no name)' }
                $content = if ($tag -match 'Content="([^"]{0,40})') { $Matches[1] } else { '' }
                $dead += [PSCustomObject]@{ File = $f.Name; Line = ($i + 1); Name = $name; Content = $content }
            }
        }
    }
}
if ($dead.Count -eq 0) { Write-Output "NONE" } else { $dead | Format-Table -AutoSize | Out-String -Width 200 }

Write-Output "=========== 2) EMPTY PUBLIC/PRIVATE METHOD BODIES (placeholder methods) ==========="
$empty = @()
Get-ChildItem "Techtoon.UI","Techtoon.Api","Techtoon.Core" -Recurse -Include *.cs | Where-Object { $_.FullName -notmatch 'obj\\|bin\\' } | ForEach-Object {
    $f = $_
    $lines = Get-Content $f.FullName
    for ($i = 0; $i -lt $lines.Count; $i++) {
        $l = $lines[$i].Trim()
        if ($l -match '^(public|private|protected|internal)\s+[\w<>\[\],\s\?]+\s+\w+\s*\([^)]*\)\s*\{\s*\}$') {
            $empty += [PSCustomObject]@{ File = $f.Name; Line = ($i + 1); Sig = $l }
        }
    }
}
if ($empty.Count -eq 0) { Write-Output "NONE" } else { $empty | Format-Table -AutoSize | Out-String -Width 250 }
