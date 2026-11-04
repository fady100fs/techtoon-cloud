@echo off
chcp 65001 >nul
title Techtoon - Build ^& Run
cd /d "D:\PROG\Accounting Prog\Techtoon Accounting C+"
echo Building...
dotnet build
if %errorlevel% neq 0 (echo Build failed! & pause & exit /b 1)
echo Starting...
dotnet run --project Techtoon.UI --no-build
pause