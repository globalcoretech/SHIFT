@echo off
echo ============================================================
echo STAFF AUTOMATION - PHASE 4 INSTALLER BUILD SCRIPT
echo ============================================================
echo.
echo STEP 1: Cleaning and Publishing WinForms Application (Release ^| win-x64)
echo.
cd /d "%~dp0..\src\StaffAutomation.WinForms"

REM Clean existing publish output
if exist "bin\Release\net8.0-windows\win-x64\publish" rmdir /s /q "bin\Release\net8.0-windows\win-x64\publish"

dotnet publish "StaffAutomation.WinForms.vbproj" -c Release -r win-x64 --self-contained false
if %ERRORLEVEL% NEQ 0 (
    echo.
    echo [ERROR] Dotnet publish failed!
    exit /b %ERRORLEVEL%
)

echo.
echo STEP 2: Compiling Inno Setup Installer
echo.
cd /d "%~dp0"
"C:\Program Files (x86)\Inno Setup 6\iscc.exe" installer.iss
if %ERRORLEVEL% NEQ 0 (
    echo.
    echo [ERROR] Inno Setup Compilation failed!
    exit /b %ERRORLEVEL%
)

echo.
echo ============================================================
echo SUCCESS: Installer generated in installer\Output folder
echo ============================================================
pause
