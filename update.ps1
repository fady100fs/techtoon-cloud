# update.ps1
# Commit and push all changes to GitHub in one command.
# Usage: powershell -ExecutionPolicy Bypass -File update.ps1

$projectPath = "D:\PROG\Accounting Prog\Techtoon Accounting"
Set-Location $projectPath

Write-Host ""
Write-Host "=== Techtoon Update Tool ===" -ForegroundColor Cyan
Write-Host ""

Write-Host "Checking for changes..." -ForegroundColor Yellow
$status = git status --porcelain

if (-not $status) {
    Write-Host "No changes to commit." -ForegroundColor Green
    Read-Host "Press ENTER to exit"
    exit 0
}

Write-Host ""
Write-Host "Changes detected:" -ForegroundColor Yellow
git status --short
Write-Host ""

$msg = Read-Host "Commit message (or ENTER for auto)"
if (-not $msg) {
    $msg = "Update " + (Get-Date -Format "yyyy-MM-dd HH:mm")
}

Write-Host ""
Write-Host "Staging changes..." -ForegroundColor Cyan
git add .

Write-Host "Committing..." -ForegroundColor Cyan
git commit -m "$msg"

Write-Host "Pushing to GitHub..." -ForegroundColor Cyan
git push

Write-Host ""
Write-Host "=======================================" -ForegroundColor Green
Write-Host "SUCCESS - Pushed to GitHub" -ForegroundColor Green
Write-Host "=======================================" -ForegroundColor Green
Write-Host ""
Write-Host "Streamlit Cloud will redeploy in ~30 seconds." -ForegroundColor Cyan
Write-Host "Track progress: https://share.streamlit.io" -ForegroundColor Cyan
Write-Host ""
Read-Host "Press ENTER to exit"