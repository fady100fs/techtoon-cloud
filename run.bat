@echo off
title Techtoon Accounting
echo Starting Techtoon Accounting...
echo.

cd /d "%~dp0"

echo Building project...
dotnet build Techtoon.UI\Techtoon.UI.csproj -c Release --nologo

if %errorlevel% neq 0 (
    echo.
    echo Build failed! Press any key to exit.
    pause >nul
    exit /b
)

echo.
echo Starting application...
start "" "Techtoon.UI\bin\Release\net10.0-windows\Techtoon.UI.exe"