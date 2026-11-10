param([string]$OutDir = "C:\Users\fady1\AppData\Local\Temp\opencode\ui-test")
# اختبار وظيفي كامل من واجهة المستخدم (قاعدة اختبار معزولة) — لا نقرات فأرة
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Windows.Forms
Add-Type -Path "C:\Users\fady1\AppData\Local\Temp\opencode\u32.cs"
if (-not (Test-Path $OutDir)) { New-Item -ItemType Directory -Force -Path $OutDir | Out-Null }

$proc = Get-Process -Name "Techtoon.UI" -ErrorAction SilentlyContinue | Select-Object -First 1
if (-not $proc) { Write-Output "PROCESS NOT FOUND"; exit 1 }
$proc.Refresh()
$hwnd = $proc.MainWindowHandle
[U32]::ShowWindow($hwnd, 9) | Out-Null
Start-Sleep -Milliseconds 400
$r0 = New-Object U32+RECT
[U32]::GetWindowRect($hwnd, [ref]$r0) | Out-Null
if (($r0.Right - $r0.Left) -lt 600) { [U32]::ShowWindow($hwnd, 3) | Out-Null; Start-Sleep -Milliseconds 600 }
[U32]::MoveWindow($hwnd, 0, 0, 1600, 1000, $true) | Out-Null
Start-Sleep -Milliseconds 700

function Get-Windows {
    $cond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ProcessIdProperty, $proc.Id)
    return [System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children, $cond)
}
function Get-Main { [System.Windows.Automation.AutomationElement]::FromHandle($hwnd) }
function Get-Dialog {
    $mainName = (Get-Main).Current.Name
    foreach ($w in (Get-Windows)) { if ($w.Current.Name -ne $mainName) { return $w } }
    return $null
}
function Get-All($el, [int]$maxDepth = 12) {
    $list = New-Object System.Collections.ArrayList
    function Rec($e, $d) {
        if ($d -gt $maxDepth) { return }
        $w = [System.Windows.Automation.TreeWalker]::ControlViewWalker
        $c = $w.GetFirstChild($e)
        while ($c) { [void]$list.Add($c); Rec $c ($d + 1); $c = $w.GetNextSibling($c) }
    }
    Rec $el 0
    return $list
}
function CT($e) { $e.Current.ControlType.ProgrammaticName -replace 'ControlType\.','' }
function ButtonsOf($el) { Get-All $el | Where-Object { (CT $_) -eq 'Button' } }
function TopButtonsOf($el) {
    $out = @()
    $w = [System.Windows.Automation.TreeWalker]::ControlViewWalker
    $c = $w.GetFirstChild($el)
    while ($c) { if ((CT $c) -eq 'Button') { $out += $c }; $c = $w.GetNextSibling($c) }
    return $out
}
# أزرار منطقة المحتوى فقط (الشريط الجانبي عند x>1300)
function PageButtons($el) {
    $out = @()
    foreach ($b in (ButtonsOf $el)) {
        try { if ($b.Current.BoundingRectangle.X -lt 1250) { $out += $b } } catch { }
    }
    return $out
}
function Click($el) { $el.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke() }
function SetText($el, [string]$t) { $el.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern).SetValue($t) }
function EditsOf($el) { Get-All $el | Where-Object { (CT $_) -eq 'Edit' } }
function CombosOf($el) { Get-All $el | Where-Object { (CT $_) -eq 'ComboBox' } }
function SetCombo($combo, [int]$index) {
    try {
        $ep = $combo.GetCurrentPattern([System.Windows.Automation.ExpandCollapsePattern]::Pattern)
        $ep.Expand(); Start-Sleep -Milliseconds 350
        $items = Get-All $combo | Where-Object { (CT $_) -eq 'ListItem' }
        if ($items.Count -gt $index) { $items[$index].GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern).Select(); Start-Sleep -Milliseconds 250; return $true }
    } catch { }
    return $false
}
function Shot([string]$name) {
    Start-Sleep -Milliseconds 700
    $r = New-Object U32+RECT
    [U32]::GetWindowRect($hwnd, [ref]$r) | Out-Null
    $w = $r.Right - $r.Left; $h = $r.Bottom - $r.Top
    if ($w -le 0 -or $h -le 0) { return "no-window" }
    $bmp = New-Object System.Drawing.Bitmap($w, $h)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $hdc = $g.GetHdc(); [U32]::PrintWindow($hwnd, $hdc, 2) | Out-Null; $g.ReleaseHdc($hdc)
    $p = Join-Path $OutDir "$name.png"; $bmp.Save($p, [System.Drawing.Imaging.ImageFormat]::Png); $g.Dispose(); $bmp.Dispose(); $p
}
function DumpAll([string]$name) {
    $sb = New-Object System.Text.StringBuilder
    foreach ($w in (Get-Windows)) {
        [void]$sb.AppendLine("=== WINDOW: '$($w.Current.Name)'")
        function W($e, $d) {
            $wr = [System.Windows.Automation.TreeWalker]::ControlViewWalker
            $c = $wr.GetFirstChild($e)
            while ($c) { [void]$sb.AppendLine(('  ' * $d) + "[$(CT $c)] '$($c.Current.Name)'"); W $c ($d + 1); $c = $wr.GetNextSibling($c) }
        }
        W $w 1
    }
    [System.IO.File]::WriteAllText((Join-Path $OutDir "$name.txt"), $sb.ToString(), [System.Text.Encoding]::UTF8)
}
function Expand-Sidebar {
    for ($i = 0; $i -lt 3; $i++) {
        foreach ($e in (Get-All (Get-Main) | Where-Object { $_.Current.ClassName -eq 'Expander' })) {
            try {
                $p = $e.GetCurrentPattern([System.Windows.Automation.ExpandCollapsePattern]::Pattern)
                if ($p.Current.ExpandCollapseState -eq 'Collapsed') { $p.Expand(); Start-Sleep -Milliseconds 250 }
            } catch { }
        }
    }
    Start-Sleep -Milliseconds 500
}
function Nav([int]$index) {
    Expand-Sidebar
    $b = @(ButtonsOf (Get-Main))
    if ($index -ge $b.Count) { throw "sidebar index $index out of range ($($b.Count))" }
    Click $b[$index]; Start-Sleep -Milliseconds 1900
}
function SelectTab([int]$index) {
    $tabs = Get-All (Get-Main) | Where-Object { (CT $_) -eq 'TabItem' }
    if ($tabs.Count -le $index) { throw "tab $index missing" }
    try { $tabs[$index].GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern).Select() }
    catch { Click $tabs[$index] }
    Start-Sleep -Milliseconds 1500
}
$log = @()
function Note($m) { $script:log += $m; Write-Output $m }
function SaveLog { [System.IO.File]::WriteAllText((Join-Path $OutDir "run.log"), ($log -join "`n"), [System.Text.Encoding]::UTF8) }

# ================= 0: login =================
$main = Get-Main
if (@(ButtonsOf $main).Count -lt 20) {
    Note "=== 0: Login ==="
    $e0 = EditsOf $main
    SetText $e0[0] "admin"; SetText $e0[1] "admin123"
    Start-Sleep -Milliseconds 300
    Click (@(ButtonsOf $main))[0]
    Start-Sleep -Milliseconds 3200
    Note "0: logged in (buttons now = $(@(ButtonsOf (Get-Main)).Count))"
}

# ================= B: enterprise loan =================
Note "=== B: Loans tab 2 -> add enterprise loan ==="
Nav 17
SelectTab 1
$main = Get-Main
$pageBtns = @(PageButtons $main)
Note "B: page buttons = $($pageBtns.Count) -> $(($pageBtns | ForEach-Object { $_.Current.Name }) -join ' | ')"
$addLoan = $pageBtns | Where-Object { $_.Current.Name -like "*إضافة قرض*" } | Select-Object -First 1
if (-not $addLoan) { Note "B: ADD LOAN BUTTON NOT FOUND"; SaveLog; exit 5 }
Click $addLoan
Start-Sleep -Milliseconds 1600
$dlg = Get-Dialog
if (-not $dlg) { Note "B: NO LOAN DIALOG"; SaveLog; exit 5 }
$edits = EditsOf $dlg
$combos = CombosOf $dlg
Note "B: loan dialog open; edits=$($edits.Count) combos=$($combos.Count)"
SetText $edits[0] "UI Test Bank"
SetText $edits[1] "100000"     # principal
SetText $edits[2] "12"         # rate %
SetText $edits[3] "12"         # installments
Start-Sleep -Milliseconds 400
$topBtns = @(TopButtonsOf $dlg)
Note "B: dialog top-level buttons = $($topBtns.Count)"
$saveBtn = $topBtns[0]
Click $saveBtn
Start-Sleep -Milliseconds 3000
$wins = Get-Windows | Where-Object { $_.Current.Name -ne 'Techtoon Accounting' }
Note "B: windows after save = $($wins.Count) -> $(($wins | ForEach-Object { $_.Current.Name }) -join ' | ')"
DumpAll "B-after-save" | Out-Null
Shot "B-after-save" | Out-Null
SaveLog
Note "B done"
