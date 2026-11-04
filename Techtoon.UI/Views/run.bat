@echo off
chcp 65001 >nul
title Techtoon Accounting
echo +----------------------------------------------+
echo    Techtoon Accounting System
echo    C# Edition - v1.0.0
echo +----------------------------------------------+
echo [INFO] Building project...
dotnet build "D:\PROG\Accounting Prog\Techtoon Accounting C+\Techtoon.UI" --no-restore 2>nul
if %ERRORLEVEL% NEQ 0 (
    echo [WARN] First build failed, trying with restore...
    dotnet build "D:\PROG\Accounting Prog\Techtoon Accounting C+\Techtoon.UI"
)
echo [INFO] Starting Techtoon Accounting...
dotnet run --project "D:\PROG\Accounting Prog\Techtoon Accounting C+\Techtoon.UI" --no-build
if %ERRORLEVEL% NEQ 0 (
    echo [INFO] Trying run with build...
    dotnet run --project "D:\PROG\Accounting Prog\Techtoon Accounting C+\Techtoon.UI"
)
pause
