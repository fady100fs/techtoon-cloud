# create_shortcut.ps1
# Creates a desktop shortcut with a custom icon.
# IMPORTANT: This file must contain ONLY ASCII characters (no Arabic).

$projectPath = "D:\PROG\Accounting Prog\Techtoon Accounting"
$iconPath = Join-Path $projectPath "TechToon-L1.ico"
$launcherPath = Join-Path $projectPath "Techtoon.bat"
$shortcutName = "Techtoon Accounting"
$desktopPath = [Environment]::GetFolderPath("Desktop")
$shortcutPath = Join-Path $desktopPath "$shortcutName.lnk"

# Verify files exist
if (-not (Test-Path $iconPath)) {
    Write-Host "ERROR: Icon file not found: $iconPath" -ForegroundColor Red
    exit 1
}
if (-not (Test-Path $launcherPath)) {
    Write-Host "ERROR: Launcher not found: $launcherPath" -ForegroundColor Red
    Write-Host "Files in project:" -ForegroundColor Yellow
    Get-ChildItem $projectPath -File | Select-Object Name
    exit 1
}

# Delete old shortcut if exists
if (Test-Path $shortcutPath) {
    Remove-Item $shortcutPath -Force
}

# Create shortcut
$WshShell = New-Object -ComObject WScript.Shell
$Shortcut = $WshShell.CreateShortcut($shortcutPath)
$Shortcut.TargetPath = $launcherPath
$Shortcut.WorkingDirectory = $projectPath
$Shortcut.IconLocation = $iconPath
$Shortcut.Description = "Techtoon Accounting"
$Shortcut.Save()

Write-Host ""
Write-Host "SUCCESS - Shortcut created" -ForegroundColor Green
Write-Host "Location: $shortcutPath"
Write-Host "Icon:     $iconPath"
Write-Host "Target:   $launcherPath"
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "  1. Go to Desktop"
Write-Host "  2. You should see 'Techtoon Accounting' with your icon"
Write-Host "  3. Right-click it -> Pin to taskbar (optional)"
Write-Host ""