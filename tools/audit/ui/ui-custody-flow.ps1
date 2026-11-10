param([string]$OutDir = "C:\Users\fady1\AppData\Local\Temp\opencode\ui-test")
# اختبار العهدة النثرية: التحقق أن القوائم تحمّل عناصر + إنشاء سجل + التقاط أي رسالة خطأ
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
function SidebarButtons($el) {
    $out = @()
    foreach ($b in (ButtonsOf $el)) { try { if ($b.Current.BoundingRectangle.X -gt 1250) { $out += $b } } catch { } }
    return $out
}
function PageButtons($el) {
    $out = @()
    foreach ($b in (ButtonsOf $el)) { try { if ($b.Current.BoundingRectangle.X -lt 1250) { $out += $b } } catch { } }
    return $out
}
function Expand-Sidebar {
    for ($i = 0; $i -lt 3; $i++) {
        foreach ($e in (Get-All (Get-Main) | Where-Object { $_.Current.ClassName -eq 'Expander' })) {
            try { $p = $e.GetCurrentPattern([System.Windows.Automation.ExpandCollapsePattern]::Pattern); if ($p.Current.ExpandCollapseState -eq 'Collapsed') { $p.Expand(); Start-Sleep -Milliseconds 220 } } catch { }
        }
    }
    Start-Sleep -Milliseconds 400
}
$out = New-Object System.Text.StringBuilder
function Note($m) { [void]$out.AppendLine($m); Write-Output $m }

$main = Get-Main
if (@(ButtonsOf $main).Count -lt 20) {
    $e0 = Get-All $main | Where-Object { (CT $_) -eq 'Edit' }
    SetText $e0[0] "admin"; SetText $e0[1] "admin123"; Start-Sleep -Milliseconds 300
    Click (@(ButtonsOf $main))[0]; Start-Sleep -Milliseconds 3200
    Note "0: logged in"
}

# close any leftover dialogs
foreach ($w in (Get-Windows)) { if ($w.Current.Name -ne $mainName) { try { $w.GetCurrentPattern([System.Windows.Automation.WindowPattern]::Pattern).Close() } catch { } } }
Start-Sleep -Milliseconds 800

Expand-Sidebar
$sb = @(SidebarButtons (Get-Main))
$target = $sb | Where-Object { $_.Current.Name -like "*العهد والمصروفات*" } | Select-Object -First 1
if (-not $target) { Note "petty cash nav not found"; exit 2 }
Click $target; Start-Sleep -Milliseconds 2000
Note "navigated to petty cash page"

$pb = @(PageButtons (Get-Main))
Note "page buttons = $($pb.Count)"
$add = $pb | Where-Object { $_.Current.Name -like "*إضافة*" } | Select-Object -First 1
if (-not $add) { Note "add button missing"; exit 3 }
Click $add; Start-Sleep -Milliseconds 1800

# find the custody dialog (not main)
$dlg = $null
foreach ($w in (Get-Windows)) { if ($w.Current.Name -ne $mainName) { $dlg = $w } }
if (-not $dlg) { Note "custody dialog missing"; exit 4 }
Note "custody dialog opened"

$combos = Get-All $dlg | Where-Object { (CT $_) -eq 'ComboBox' }
$edits = Get-All $dlg | Where-Object { (CT $_) -eq 'Edit' }
Note "combos=$($combos.Count) edits=$($edits.Count)"

# افتح كل قائمة واقرأ عناصرها (للتأكد أنها ليست فارغة)
$ci = 0
foreach ($c in $combos) {
    $items = @()
    try {
        $c.GetCurrentPattern([System.Windows.Automation.ExpandCollapsePattern]::Pattern).Expand()
        Start-Sleep -Milliseconds 500
        $items = Get-All $c | Where-Object { (CT $_) -eq 'ListItem' }
        Note "  combo[$ci] items = $($items.Count) -> $(($items | ForEach-Object { $_.Current.Name }) -join ' | ')"
        if ($items.Count -gt 0) {
            $items[0].GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern).Select()
            Start-Sleep -Milliseconds 400
        }
    } catch { Note "  combo[$ci] expand failed: $($_.Exception.Message)" }
    $ci++
}
if ($edits.Count -ge 1) { SetText $edits[0] "750"; Note "amount set" }

$btns = ButtonsOf $dlg
Note "dialog buttons = $(($btns | ForEach-Object { $_.Current.Name }) -join ' | ')"
$save = $btns | Where-Object { $_.Current.Name -like "*حفظ*" } | Select-Object -First 1
if (-not $save) { Note "save button missing"; exit 5 }
Click $save; Start-Sleep -Milliseconds 2500

# التقاط أي رسالة/نافذة ظهرت
$extra = @()
foreach ($w in (Get-Windows)) {
    if ($w.Current.Name -ne $mainName) {
        $extra += $w.Current.Name
        $txt = (Get-All $w | Where-Object { (CT $_) -eq 'Text' } | ForEach-Object { $_.Current.Name }) -join ' / '
        Note "EXTRA WINDOW '$($w.Current.Name)' text: $txt"
    }
}
Note "extra windows = $($extra.Count)"
[System.IO.File]::WriteAllText((Join-Path $OutDir "D-custody.log"), $out.ToString(), [System.Text.Encoding]::UTF8)
Note "DONE"
