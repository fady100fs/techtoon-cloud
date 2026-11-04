@echo off
title Techtoon Accounting - Build EXE
color 0A

echo ============================================================
echo   Techtoon Accounting - Building EXE
echo ============================================================
echo.

:: Check PyInstaller
python -c "import PyInstaller" >nul 2>&1
if %errorlevel% neq 0 (
    echo [1/3] Installing PyInstaller...
    pip install pyinstaller
    echo.
) else (
    echo [OK] PyInstaller found
    echo.
)

:: Clean old files
echo [2/3] Cleaning old files...
if exist "build" rmdir /s /q build
if exist "dist" rmdir /s /q dist
if exist "Techtoon_Accounting.spec" del Techtoon_Accounting.spec
echo.

:: Build EXE
echo [3/3] Building EXE (this may take 5-10 minutes)...
echo.

pyinstaller --onefile --windowed --name "Techtoon_Accounting" --add-data "database.py;." --add-data "models.py;." --add-data "services.py;." --add-data "auth.py;." --add-data "auth_required.py;." --add-data "alerts.py;." --add-data "reminders.py;." --add-data "export_pdf.py;." --add-data "export_excel.py;." --add-data "seed_data.py;." --add-data "app.py;." --add-data "pages;pages" --hidden-import sqlalchemy --hidden-import pandas --hidden-import streamlit --hidden-import plotly --hidden-import bcrypt --hidden-import reportlab --hidden-import xlsxwriter --hidden-import openpyxl --collect-all streamlit app.py

echo.

if exist "dist\Techtoon_Accounting.exe" (
    color 0A
    echo ============================================================
    echo   EXE Build Complete!
    echo ============================================================
    echo.
    echo File location: dist\Techtoon_Accounting.exe
    echo.
    echo You can copy this file to any Windows computer
    echo.
    echo ============================================================
    
    copy "dist\Techtoon_Accounting.exe" "Techtoon_Accounting.exe" >nul
    echo [OK] File copied to main folder
    
    pause
) else (
    color 0C
    echo ============================================================
    echo   EXE Build Failed!
    echo ============================================================
    echo.
    echo Please check errors above
    echo.
    pause
)