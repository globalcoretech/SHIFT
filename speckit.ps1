# PowerShell Helper Script for GitHub Spec-Kit in StaffAutomation project
param (
    [string]$Command = "status",
    [string]$Name = ""
)

Write-Host "==================================================" -ForegroundColor Cyan
Write-Host " 🚀 STAFF AUTOMATION - GITHUB SPEC-KIT ENGINE" -ForegroundColor Green
Write-Host "==================================================" -ForegroundColor Cyan

$specifyDir = Join-Path $PSScriptRoot ".specify"
$specsDir = Join-Path $PSScriptRoot "specs"

if (!(Test-Path $specifyDir)) {
    New-Item -ItemType Directory -Path $specifyDir | Out-Null
    Write-Host "✅ Created .specify directory" -ForegroundColor Yellow
}

if (!(Test-Path $specsDir)) {
    New-Item -ItemType Directory -Path $specsDir | Out-Null
    Write-Host "✅ Created specs directory" -ForegroundColor Yellow
}

switch ($Command.ToLower()) {
    "status" {
        Write-Host "📌 Spec-Kit Status: INSTALLED AND ACTIVE!" -ForegroundColor Green
        Write-Host "• Constitution Path: .specify/constitution.md" -ForegroundColor Gray
        Write-Host "• Spec Directory: specs/" -ForegroundColor Gray
        
        $specFiles = Get-ChildItem -Path $specsDir -Filter "*.md" -Recurse
        Write-Host "• Active Specifications: $($specFiles.Count)" -ForegroundColor Yellow
        foreach ($file in $specFiles) {
            Write-Host "  - $($file.Name)" -ForegroundColor White
        }
    }
    "new" {
        if ([string]::IsNullOrWhiteSpace($Name)) {
            Write-Host "❌ Please specify a feature name. Example: .\speckit.ps1 -Command new -Name 'whatsapp-alert'" -ForegroundColor Red
            return
        }
        $fileName = "$Name.spec.md"
        $filePath = Join-Path $specsDir $fileName
        $templatePath = Join-Path $specsDir "templates\feature_template.md"
        
        if (Test-Path $templatePath) {
            Copy-Item $templatePath $filePath
        } else {
            Set-Content -Path $filePath -Value "# $Name Feature Specification"
        }
        Write-Host "✅ Created new Spec file: specs/$fileName" -ForegroundColor Green
        Write-Host "👉 Fill in requirements in specs/$fileName and ask agent to implement!" -ForegroundColor Yellow
    }
    "converge" {
        Write-Host "🔍 Running Spec-Kit Audit & Convergence check..." -ForegroundColor Cyan
        Write-Host "• Checking .NET 8 Build Status..." -ForegroundColor Gray
        dotnet build "$PSScriptRoot\StaffAutomation.sln"
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✅ CONVERGENCE SUCCESS: 0 Build Errors!" -ForegroundColor Green
        } else {
            Write-Host "❌ CONVERGENCE WARNING: Build errors detected." -ForegroundColor Red
        }
    }
    default {
        Write-Host "Available Commands:" -ForegroundColor Yellow
        Write-Host "  .\speckit.ps1 -Command status               (Check Spec-Kit status)" -ForegroundColor White
        Write-Host "  .\speckit.ps1 -Command new -Name 'feature'  (Create new Spec file)" -ForegroundColor White
        Write-Host "  .\speckit.ps1 -Command converge             (Run build & convergence audit)" -ForegroundColor White
    }
}
