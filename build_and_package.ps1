$ErrorActionPreference = "Stop"

Write-Host "=============================================" -ForegroundColor Green
Write-Host "   Staff Automation - Release Build Script   " -ForegroundColor Green
Write-Host "=============================================" -ForegroundColor Green
Write-Host ""

Write-Host "[1/5] Cleaning previous build artifacts..." -ForegroundColor Cyan
dotnet clean -c Release
if ($LASTEXITCODE -ne 0) { throw "Clean failed" }

Write-Host "[2/5] Building the complete solution in Release mode..." -ForegroundColor Cyan
dotnet build StaffAutomation.sln -c Release
if ($LASTEXITCODE -ne 0) { throw "Build failed" }

Write-Host "[3/5] Running the complete test suite..." -ForegroundColor Cyan
dotnet test tests\StaffAutomation.Tests\StaffAutomation.Tests.vbproj -c Release --no-build
if ($LASTEXITCODE -ne 0) { throw "Tests failed" }

Write-Host "[4/5] Creating fresh Release publish output..." -ForegroundColor Cyan
dotnet publish src\StaffAutomation.WinForms\StaffAutomation.WinForms.vbproj -c Release -f net8.0-windows -r win-x64 --self-contained false -p:PublishSingleFile=false
if ($LASTEXITCODE -ne 0) { throw "Publish failed" }

Write-Host "[5/5] Building Inno Setup installer..." -ForegroundColor Cyan
$isccPath = "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
if (-not (Test-Path $isccPath)) {
    Write-Host "Inno Setup Compiler (ISCC.exe) not found at default location: $isccPath" -ForegroundColor Yellow
    Write-Host "Please ensure Inno Setup 6 is installed, or update the path in this script." -ForegroundColor Yellow
    exit 1
}

& $isccPath "installer\installer.iss"
if ($LASTEXITCODE -ne 0) { throw "Installer build failed" }

Write-Host ""
Write-Host "=============================================" -ForegroundColor Green
Write-Host "SUCCESS! Installer created." -ForegroundColor Green
Write-Host "Path: e:\Staff automation\installer\Output\SHIFTWorkforceSetup.exe" -ForegroundColor Green
Write-Host "=============================================" -ForegroundColor Green
