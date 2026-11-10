param([string]$OutDir = "C:\Users\fady1\AppData\Local\Temp\opencode\ui-test")
# اختبار نهائي: (1) قرض جديد مع ربط الحسابات (2) تعديل خزينة (3) حذف خزينة
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
Add-Type -Path "C:\Users\fady1\AppData\Local\Temp\opencode\u32.cs"

$proc = Get-Process -Name "Techtoon.UI" -ErrorAction SilentlyContinue | Select-Object -First 1
if (-not $proc) { Write-Output "PROCESS NOT FOUND"; exit 1 }
$proc.Refresh()
$hwnd = $proc.MainWindowHandle
$mainName = 'Techtoon Accounting'
function Get-Windows {
    $cond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ProcessIdProperty, $proc.Id)
    return [System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children, $cond)
}
function Get-Main { [System.Windows.Automation.AutomationElement]::FromHandle($hwnd) }
function Get-Dialog {
    $mn = (Get-Main).Current.Name
    foreach ($w in (Get-Windows)) { if ($w.Current.Name -ne $mn -and $w.Current.Name -ne 'جدول الأقساط') { return $w } }
    return $null
}
function Get-All($el, [int]$maxDepth = 14) {
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
function Click($el) { $el.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke() }
function SetText($el, [string]$t) { $el.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern).SetValue($t) }
function ButtonsOf($el) { Get-All $el | Where-Object { (CT $_) -eq 'Button' } }
function SidebarButtons($el) { $o = @(); foreach ($b in (ButtonsOf $el)) { try { if ($b.Current.BoundingRectangle.X -gt 1250) { $o += $b } } catch { } }; return $o }
function PageButtons($el) { $o = @(); foreach ($b in (ButtonsOf $el)) { try { if ($b.Current.BoundingRectangle.X -lt 1250) { $o += $b } } catch { } }; return $o }
function Expand-Sidebar {
    for ($i = 0; $i -lt 3; $i++) {
        foreach ($e in (Get-All (Get-Main) | Where-Object { $_.Current.ClassName -eq 'Expander' })) {
            try { $p = $e.GetCurrentPattern([System.Windows.Automation.ExpandCollapsePattern]::Pattern); if ($p.Current.ExpandCollapseState -eq 'Collapsed') { $p.Expand(); Start-Sleep -Milliseconds 220 } } catch { }
        }
    }
    Start-Sleep -Milliseconds 400
}
function Nav([string]$part) {
    Expand-Sidebar
    $t = @(SidebarButtons (Get-Main)) | Where-Object { $_.Current.Name -like "*$part*" } | Select-Object -First 1
    if (-not $t) { throw "nav '$part' missing" }
    Click $t; Start-Sleep -Milliseconds 1900
}
function SelectTab([int]$i) {
    $tabs = Get-All (Get-Main) | Where-Object { (CT $_) -eq 'TabItem' }
    $tabs[$i].GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern).Select()
    Start-Sleep -Milliseconds 1400
}
$log = New-Object System.Text.StringBuilder
function Note($m) { [void]$log.AppendLine($m); Write-Output $m }

$main = Get-Main
if (@(ButtonsOf $main).Count -lt 20) {
    $e0 = Get-All $main | Where-Object { (CT $_) -eq 'Edit' }
    SetText $e0[0] "admin"; SetText $e0[1] "admin123"; Start-Sleep -Milliseconds 300
    Click (@(ButtonsOf $main))[0]; Start-Sleep -Milliseconds 3200
    Note "0: logged in"
}

# ===== E: new enterprise loan (auto account linking) =====
Note "=== E: enterprise loan with auto account linking ==="
Nav "السلف والقروض"
SelectTab 1
$pb = @(PageButtons (Get-Main))
$addLoan = $pb | Where-Object { $_.Current.Name -like "*إضافة قرض*" } | Select-Object -First 1
Click $addLoan; Start-Sleep -Milliseconds 1600
$dlg = Get-Dialog
$edits = Get-All $dlg | Where-Object { (CT $_) -eq 'Edit' }
$combos = Get-All $dlg | Where-Object { (CT $_) -eq 'ComboBox' }
Note "E: edits=$($edits.Count) combos=$($combos.Count)"
# اقرأ القيم الافتراضية لكل قائمة (للتأكد من الربط التلقائي)
$ci = 0
foreach ($c in $combos) {
    $sel = ""
    try { $sel = $c.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern).Current.IsSelected } catch { }
    $items = @()
    try {
        $c.GetCurrentPattern([System.Windows.Automation.ExpandCollapsePattern]::Pattern).Expand(); Start-Sleep -Milliseconds 400
        $items = Get-All $c | Where-Object { (CT $_) -eq 'ListItem' }
        $c.GetCurrentPattern([System.Windows.Automation.ExpandCollapsePattern]::Pattern).Collapse(); Start-Sleep -Milliseconds 200
    } catch { }
    Note "  combo[$ci] items=$($items.Count) -> $(($items | ForEach-Object { $_.Current.Name }) -join ' | ')"
    $ci++
}
SetText $edits[0] "Second Bank"
SetText $edits[1] "50000"
SetText $edits[2] "9"
SetText $edits[3] "6"
Start-Sleep -Milliseconds 300
$topBtns = @(Get-All $dlg | Where-Object { (CT $_) -eq 'Button' -and $_.Current.Name -ne '' })
Note "E: bottom buttons = $($topBtns.Count) -> $(($topBtns | ForEach-Object { $_.Current.Name }) -join ' | ')"
$saveBtn = $topBtns | Where-Object { $_.Current.Name -ne 'Show Calendar' } | Select-Object -First 1
Click $saveBtn
Start-Sleep -Milliseconds 3200
$extra = @()
foreach ($w in (Get-Windows)) { if ($w.Current.Name -ne $mainName) { $extra += $w.Current.Name } }
Note "E: windows after save = $($extra.Count) -> $($extra -join ' | ')"
# إغلاق نافذة جدول الأقساط إن فتحت
foreach ($w in (Get-Windows)) {
    if ($w.Current.Name -eq 'جدول الأقساط') {
        try { $w.GetCurrentPattern([System.Windows.Automation.WindowPattern]::Pattern).Close() } catch { }
    }
}
Start-Sleep -Milliseconds 900

# ===== F: cash box edit + delete =====
Note "=== F: CashBox edit + delete ==="
Nav "الخزائن والبنوك"
$grid = Get-All (Get-Main) | Where-Object { (CT $_) -eq 'DataGrid' } | Select-Object -First 1
$rows = Get-All $grid | Where-Object { (CT $_) -eq 'DataItem' }
Note "F: cash box rows = $($rows.Count)"
if ($rows.Count -gt 0) {
    try { $rows[0].GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern).Select() } catch { }
    Start-Sleep -Milliseconds 600
    $pb2 = @(PageButtons (Get-Main))
    $edit = $pb2 | Where-Object { $_.Current.Name -like "*تعديل*" } | Select-Object -First 1
    if ($edit) {
        Click $edit; Start-Sleep -Milliseconds 1500
        $dlg2 = Get-Dialog
        if ($dlg2) {
            $e2 = Get-All $dlg2 | Where-Object { (CT $_) -eq 'Edit' }
            Note "F: edit dialog edits=$($e2.Count)"
            if ($e2.Count -ge 1) { SetText $e2[0] "UITest CashBox RENAMED" }
            $sb2 = @(Get-All $dlg2 | Where-Object { (CT $_) -eq 'Button' })
            $save2 = $sb2 | Where-Object { $_.Current.Name -like "*حفظ*" } | Select-Object -First 1
            if ($save2) { Click $save2; Start-Sleep -Milliseconds 2000 }
            Note "F: edited"
        } else { Note "F: EDIT DIALOG MISSING" }
    } else { Note "F: EDIT BUTTON MISSING" }
}
# delete
$grid = Get-All (Get-Main) | Where-Object { (CT $_) -eq 'DataGrid' } | Select-Object -First 1
$rows = Get-All $grid | Where-Object { (CT $_) -eq 'DataItem' }
if ($rows.Count -gt 0) {
    try { $rows[0].GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern).Select() } catch { }
    Start-Sleep -Milliseconds 600
    $pb3 = @(PageButtons (Get-Main))
    $del = $pb3 | Where-Object { $_.Current.Name -like "*حذف*" } | Select-Object -First 1
    if ($del) {
        Click $del; Start-Sleep -Milliseconds 1500
        # نافذة تأكيد: اختر "نعم"
        foreach ($w in (Get-Windows)) {
            if ($w.Current.Name -ne $mainName -and $w.Current.Name -ne 'جدول الأقساط') {
                $bb = Get-All $w | Where-Object { (CT $_) -eq 'Button' }
                $yes = $bb | Where-Object { $_.Current.Name -like "*نعم*" } | Select-Object -First 1
                if ($yes) { Click $yes; Start-Sleep -Milliseconds 1500; Note "F: delete confirmed" }
                else { Note "F: confirm buttons = $(($bb | ForEach-Object { $_.Current.Name }) -join ' | ')"; try { $w.GetCurrentPattern([System.Windows.Automation.WindowPattern]::Pattern).Close() } catch { } }
            }
        }
    }
}
[System.IO.File]::WriteAllText((Join-Path $OutDir "final.log"), $log.ToString(), [System.Text.Encoding]::UTF8)
Note "DONE"
