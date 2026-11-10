param([string]$OutDir = "C:\Users\fady1\AppData\Local\Temp\opencode\ui-test")
# دليل نهائي: تنقل بالأسهم + اختيار بالـ Enter + حفظ الفاتورة + التحقق من قاعدة البيانات
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
Add-Type -AssemblyName System.Windows.Forms
Add-Type -Path "C:\Users\fady1\AppData\Local\Temp\opencode\u32.cs"

$proc = Get-Process -Name "Techtoon.UI" -ErrorAction SilentlyContinue | Select-Object -First 1
if (-not $proc) { Write-Host "PROCESS NOT FOUND"; exit 1 }
$proc.Refresh()
$hwnd = $proc.MainWindowHandle
$mainName = 'Techtoon Accounting'
$invoiceHwnd = [IntPtr]::Zero
function Get-Windows {
    $cond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ProcessIdProperty, $proc.Id)
    return [System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children, $cond)
}
function Get-Main { [System.Windows.Automation.AutomationElement]::FromHandle($hwnd) }
function Get-All($el, [int]$maxDepth = 16) {
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
$log = New-Object System.Text.StringBuilder
function Note($m) { [void]$log.AppendLine($m); Write-Host $m }
function ComboText($cb) {
    try {
        $ed = Get-All $cb | Where-Object { (CT $_) -eq 'Edit' } | Select-Object -First 1
        if ($ed) { return $ed.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern).Current.Value }
    } catch { }
    return ""
}
function Send-Key([string]$key) {
    if (-not [U32]::Activate($invoiceHwnd)) { Note "  WARN: not foreground"; return $false }
    Start-Sleep -Milliseconds 200
    [System.Windows.Forms.SendKeys]::SendWait($key)
    Start-Sleep -Milliseconds 500
    return $true
}

$main = Get-Main
if (@(ButtonsOf $main).Count -lt 20) {
    $e0 = Get-All $main | Where-Object { (CT $_) -eq 'Edit' }
    $e0[0].GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern).SetValue("admin")
    $e0[1].GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern).SetValue("admin123")
    Start-Sleep -Milliseconds 300
    Click (@(ButtonsOf $main))[0]; Start-Sleep -Milliseconds 3200
    Note "logged in"
}
foreach ($w in (Get-Windows)) { if ($w.Current.Name -ne $mainName) { try { $w.GetCurrentPattern([System.Windows.Automation.WindowPattern]::Pattern).Close() } catch { } } }
Start-Sleep -Milliseconds 700

Expand-Sidebar
$sb = @(SidebarButtons (Get-Main))
Note "sidebar buttons = $($sb.Count) -> $(($sb | ForEach-Object { $_.Current.Name }) -join ' | ')"
$inv = $sb | Where-Object { $_.Current.Name -like "*الفواتير*" } | Select-Object -First 1
if (-not $inv) {
    Expand-Sidebar
    $sb = @(SidebarButtons (Get-Main))
    $inv = $sb | Where-Object { $_.Current.Name -like "*الفواتير*" } | Select-Object -First 1
}
if (-not $inv) { Note "invoices nav missing"; exit 2 }
Click $inv; Start-Sleep -Milliseconds 2200
$pb = @(PageButtons (Get-Main))
Note "invoice page buttons = $($pb.Count) -> $(($pb | ForEach-Object { $_.Current.Name }) -join ' | ')"
$newInv = $pb | Where-Object { $_.Current.Name -like "*إضافة*" -or $_.Current.Name -like "*جديد*" } | Select-Object -First 1
if (-not $newInv) { Note "new invoice button missing"; exit 3 }
Click $newInv; Start-Sleep -Milliseconds 2800
$win = $null
foreach ($w in (Get-Windows)) { if ($w.Current.Name -ne $mainName) { $win = $w } }
if (-not $win) { Note "invoice window missing"; exit 4 }
$invoiceHwnd = [IntPtr]$win.Current.NativeWindowHandle

# combo الصنف
$itemCombo = $null
foreach ($c in @(Get-All $win | Where-Object { (CT $_) -eq 'ComboBox' })) {
    try { $c.GetCurrentPattern([System.Windows.Automation.ExpandCollapsePattern]::Pattern).Expand() } catch { }
    Start-Sleep -Milliseconds 400
    $items = @(Get-All $c | Where-Object { (CT $_) -eq 'ListItem' })
    if ($items.Count -ge 3 -and -not $itemCombo) { $itemCombo = $c }
}
if (-not $itemCombo) {
    # قد يحتاج الصف التحديد أولًا لظهور ComboBox بأصنافه
    $grid0 = Get-All $win | Where-Object { (CT $_) -eq 'DataGrid' } | Select-Object -First 1
    if ($grid0) {
        $rows0 = @(Get-All $grid0 | Where-Object { (CT $_) -eq 'DataItem' })
        if ($rows0.Count -gt 0) {
            try { $rows0[0].GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern).Select() } catch { }
            Start-Sleep -Milliseconds 1000
        }
    }
    foreach ($c in @(Get-All $win | Where-Object { (CT $_) -eq 'ComboBox' })) {
        try { $c.GetCurrentPattern([System.Windows.Automation.ExpandCollapsePattern]::Pattern).Expand() } catch { }
        Start-Sleep -Milliseconds 400
        $items = @(Get-All $c | Where-Object { (CT $_) -eq 'ListItem' })
        Note "  (after select) combo items=$($items.Count)"
        if ($items.Count -ge 3 -and -not $itemCombo) { $itemCombo = $c }
    }
}
if (-not $itemCombo) { Note "item combo missing"; exit 5 }

# اختر العميل (ComboBox الذي يحوي بيانات الأطراف — عنصر واحد "Test Customer")
$partyCombo = $null
foreach ($c in @(Get-All $win | Where-Object { (CT $_) -eq 'ComboBox' })) {
    if ($c -eq $itemCombo) { continue }
    try { $c.GetCurrentPattern([System.Windows.Automation.ExpandCollapsePattern]::Pattern).Expand() } catch { }
    Start-Sleep -Milliseconds 350
    $items = @(Get-All $c | Where-Object { (CT $_) -eq 'ListItem' })
    if ($items.Count -eq 1 -and -not $partyCombo) {
        $partyCombo = $c
        try { $items[0].GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern).Select() } catch { }
        Note "party selected from combo"
    }
}
Start-Sleep -Milliseconds 600
$innerEdit = Get-All $itemCombo | Where-Object { (CT $_) -eq 'Edit' } | Select-Object -First 1
if ($innerEdit) { try { $innerEdit.SetFocus() } catch { } }
[U32]::Activate($invoiceHwnd) | Out-Null
Start-Sleep -Milliseconds 500

Note "BEFORE      : '$(ComboText $itemCombo)'"
Send-Key "{DOWN}" | Out-Null; $a = ComboText $itemCombo; Note "DOWN #1     : '$a'"
Send-Key "{DOWN}" | Out-Null; $b = ComboText $itemCombo; Note "DOWN #2     : '$b'"
Send-Key "{UP}"   | Out-Null; $c2 = ComboText $itemCombo; Note "UP   #1     : '$c2'"
Note "NAVIGATION OK = $((($a -ne $c2) -or ($b -ne $c2)))"

# اختر الثاني بالأسهم ثم Enter
Send-Key "{DOWN}" | Out-Null; Send-Key "{DOWN}" | Out-Null
$chosen = ComboText $itemCombo
Note "chosen before Enter: '$chosen'"
Send-Key "{ENTER}" | Out-Null
Start-Sleep -Milliseconds 1400
Note "after ENTER, combo text = '$(ComboText $itemCombo)'"

# اقرأ محتوى صف الشبكة للتأكد من إضافة الصنف
$grid = Get-All $win | Where-Object { (CT $_) -eq 'DataGrid' } | Select-Object -First 1
if ($grid) {
    $rows = @(Get-All $grid | Where-Object { (CT $_) -eq 'DataItem' })
    Note "grid rows = $($rows.Count)"
    if ($rows.Count -gt 0) {
        $cells = @(Get-All $rows[0] | Where-Object { (CT $_) -eq 'Text' -or (CT $_) -eq 'Edit' })
        $vals = @()
        foreach ($ce in $cells) { try { $vals += $ce.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern).Current.Value } catch { } }
        Note "row values = $($vals -join ' | ')"
    }
}

# احفظ الفاتورة
$saveBtn = @(ButtonsOf $win) | Where-Object { $_.Current.Name -like "*حفظ*" } | Select-Object -First 1
if ($saveBtn) {
    Note "clicking save"
    Click $saveBtn
    Start-Sleep -Milliseconds 2500
    $still = @()
    foreach ($w in (Get-Windows)) { if ($w.Current.Name -ne $mainName) { $still += $w.Current.Name } }
    Note "windows after save = $($still.Count) -> $($still -join ' | ')"
    # اقرأ نصوص النافذة (رسالة التحقق إن وُجدت)
    foreach ($w in (Get-Windows)) {
        if ($w.Current.Name -ne $mainName) {
            $texts = @(Get-All $w | Where-Object { (CT $_) -eq 'Text' } | ForEach-Object { $_.Current.Name } | Where-Object { $_ -and $_ -notmatch '^\s*$' })
            Note "window texts: $($texts -join ' / ')"
        }
    }
} else { Note "SAVE BUTTON NOT FOUND" }
[System.IO.File]::WriteAllText((Join-Path $OutDir "invoice-nav.log"), $log.ToString(), [System.Text.Encoding]::UTF8)
Note "DONE"
