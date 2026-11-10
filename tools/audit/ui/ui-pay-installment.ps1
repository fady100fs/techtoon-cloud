param([string]$OutDir = "C:\Users\fady1\AppData\Local\Temp\opencode\ui-test")
# يختبر تسديد قسط من نافذة جدول الأقساط (وحدة تعمل بالكامل عبر UI)
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
Add-Type -AssemblyName System.Drawing
Add-Type -Path "C:\Users\fady1\AppData\Local\Temp\opencode\u32.cs"

$proc = Get-Process -Name "Techtoon.UI" -ErrorAction SilentlyContinue | Select-Object -First 1
if (-not $proc) { Write-Output "PROCESS NOT FOUND"; exit 1 }
$proc.Refresh()
$hwnd = $proc.MainWindowHandle

function Get-Windows {
    $cond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ProcessIdProperty, $proc.Id)
    return [System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children, $cond)
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
function Click($el) { $el.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke() }
function SetText($el, [string]$t) { $el.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern).SetValue($t) }

# نافذة جدول الأقساط = النافذة غير الرئيسية
$mainName = 'Techtoon Accounting'
$sched = $null
foreach ($w in (Get-Windows)) { if ($w.Current.Name -ne $mainName) { $sched = $w } }
if (-not $sched) { Write-Output "SCHEDULE WINDOW NOT OPEN"; exit 1 }
Write-Output "schedule window found"

# جدول الأقساط: DataGrid -> صف واحد -> خلية
$grid = Get-All $sched | Where-Object { (CT $_) -eq 'DataGrid' } | Select-Object -First 1
if (-not $grid) { Write-Output "NO DATAGRID IN SCHEDULE"; exit 2 }
$items = Get-All $grid | Where-Object { (CT $_) -eq 'DataItem' }
Write-Output "data rows in grid = $($items.Count)"

# زر تسديد القسط
$btns = Get-All $sched | Where-Object { (CT $_) -eq 'Button' }
Write-Output ("schedule buttons = " + (($btns | ForEach-Object { $_.Current.Name }) -join ' | '))
$payBtn = $btns | Where-Object { $_.Current.Name -like "*تسديد*" } | Select-Object -First 1
if (-not $payBtn) { Write-Output "PAY BUTTON NOT FOUND"; exit 3 }

# نحدد أول صف (DataItem) لا الخلية
if ($items.Count -gt 0) {
    try {
        $items[0].GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern).Select()
        Start-Sleep -Milliseconds 500
    } catch { Write-Output "row select failed: $($_.Exception.Message)" }
}
Start-Sleep -Milliseconds 600
Click $payBtn
Start-Sleep -Milliseconds 1800

# نافذة التسديد
$pay = $null
foreach ($w in (Get-Windows)) { if ($w.Current.Name -ne $mainName -and $w.Current.Name -ne $sched.Current.Name) { $pay = $w } }
if (-not $pay) { Write-Output "PAY DIALOG NOT OPEN"; exit 4 }
Write-Output "pay dialog open"
$edits = Get-All $pay | Where-Object { (CT $_) -eq 'Edit' }
Write-Output "pay dialog edits = $($edits.Count)"
if ($edits.Count -ge 1) { SetText $edits[0] "8884.88" }
$btns2 = Get-All $pay | Where-Object { (CT $_) -eq 'Button' }
Write-Output ("pay dialog buttons = " + (($btns2 | ForEach-Object { $_.Current.Name }) -join ' | '))
$confirm = $btns2 | Where-Object { $_.Current.Name -like "*تسديد*" -and $_.Current.Name -notlike "*القسط*" } | Select-Object -First 1
if (-not $confirm) { $confirm = $btns2[0] }
Click $confirm
Start-Sleep -Milliseconds 2500
$still = @()
foreach ($w in (Get-Windows)) { if ($w.Current.Name -ne $mainName) { $still += $w.Current.Name } }
Write-Output "windows after payment = $($still.Count) -> $($still -join ' | ')"
Write-Output "PAYMENT FLOW DONE"
