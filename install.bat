@echo off
title Techtoon Accounting - Installation
color 0A

echo ============================================================
echo   Techtoon Accounting - Installation
echo ============================================================
echo.

:: Check Python
python --version >nul 2>&1
if %errorlevel% neq 0 (
    color 0C
    echo [ERROR] Python is not installed!
    echo.
    echo Please install Python from:
    echo https://www.python.org/downloads/
    echo.
    echo IMPORTANT: Check "Add Python to PATH" during installation
    echo.
    pause
    exit /b 1
)

echo [OK] Python found
python --version
echo.

:: Upgrade pip
echo [1/4] Upgrading pip...
python -m pip install --upgrade pip
echo.

:: Install requirements
echo [2/4] Installing required libraries...
echo (This may take a few minutes)
echo.
pip install -r requirements.txt
if %errorlevel% neq 0 (
    color 0C
    echo [ERROR] Failed to install libraries!
    pause
    exit /b 1
)
echo [OK] All libraries installed
echo.

:: Create database
echo [3/4] Setting up database...
python seed_data.py
echo.

:: Create backup folder
if not exist "backups" mkdir backups
echo [OK] Backup folder created
echo.

:: Create desktop shortcut
echo [4/4] Creating desktop shortcut...
powershell -Command "$WshShell = New-Object -ComObject WScript.Shell; $Shortcut = $WshShell.CreateShortcut([Environment]::GetFolderPath('Desktop') + '\Techtoon Accounting.lnk'); $Shortcut.TargetPath = '%~dp0run.bat'; $Shortcut.WorkingDirectory = '%~dp0'; $Shortcut.IconLocation = '%SystemRoot%\System32\shell32.dll,4'; $Shortcut.Save()"
echo [OK] Desktop shortcut created
echo.

color 0A
echo ============================================================
echo   Installation Complete!
echo ============================================================
echo.
echo You can now run the program from:
echo 1. Desktop shortcut: "Techtoon Accounting"
echo 2. Double-click: run.bat
echo 3. Command: streamlit run app.py
echo.
echo Default login:
echo Username: admin
echo Password: admin123
echo.
echo ============================================================
pause