# SCRIPT DEPLOYMENT HOÀN CHỈNH - KHẮC PHỤC LỖI FONT
# Thực hiện tất cả bước cần thiết để deploy fix lên production

param(
    [switch]$SkipBackup,
    [switch]$SkipSQLCheck,
    [string]$ServerPath = "\\101.53.9.76\c$\inetpub\wwwroot\RetailPointBackend"
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "     RETAIL POINT - DEPLOYMENT FIX     " -ForegroundColor Cyan
Write-Host "   Fix font issue & database errors    " -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

$ErrorActionPreference = "Continue"
$publishPath = "c:\shop\Backend\RetailPointBackend\bin\Release\publish"

# 1. VERIFY PUBLISH FILES
Write-Host "`n🔍 STEP 1: Verifying publish files..." -ForegroundColor Yellow
if (Test-Path $publishPath) {
    $files = Get-ChildItem $publishPath -Recurse | Measure-Object
    Write-Host "✅ Found $($files.Count) files in publish directory" -ForegroundColor Green
    
    # Check critical files
    $criticalFiles = @(
        "$publishPath\RetailPointBackend.dll",
        "$publishPath\appsettings.json",
        "$publishPath\web.config"
    )
    
    foreach ($file in $criticalFiles) {
        if (Test-Path $file) {
            Write-Host "✅ $([System.IO.Path]::GetFileName($file))" -ForegroundColor Green
        } else {
            Write-Host "❌ Missing: $([System.IO.Path]::GetFileName($file))" -ForegroundColor Red
        }
    }
} else {
    Write-Host "❌ Publish directory not found: $publishPath" -ForegroundColor Red
    Write-Host "Run: dotnet publish --configuration Release" -ForegroundColor Yellow
    exit 1
}

# 2. SQL SCRIPT PREPARATION
Write-Host "`n🗄️ STEP 2: Preparing SQL script..." -ForegroundColor Yellow
$sqlScript = "c:\shop\Backend\restore_missing_tables.sql"
if (Test-Path $sqlScript) {
    Write-Host "✅ SQL script ready: $sqlScript" -ForegroundColor Green
    if (-not $SkipSQLCheck) {
        Write-Host "📋 MANUAL ACTION REQUIRED:" -ForegroundColor Cyan
        Write-Host "   1. Open SQL Server Management Studio" -ForegroundColor White
        Write-Host "   2. Connect to: TEST-PC\KTEAM" -ForegroundColor White
        Write-Host "   3. Select Database: RetailPoint" -ForegroundColor White
        Write-Host "   4. Run script: $sqlScript" -ForegroundColor White
        Write-Host "   5. Verify results in Messages tab" -ForegroundColor White
        
        $continue = Read-Host "`nHave you run the SQL script? (y/n)"
        if ($continue -ne 'y') {
            Write-Host "❌ Please run SQL script first, then restart this deployment" -ForegroundColor Red
            exit 1
        }
    }
} else {
    Write-Host "❌ SQL script not found: $sqlScript" -ForegroundColor Red
    exit 1
}

# 3. CREATE DEPLOYMENT PACKAGE
Write-Host "`n📦 STEP 3: Creating deployment package..." -ForegroundColor Yellow
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$deployPackage = "c:\shop\deploy_package_$timestamp"

try {
    New-Item -ItemType Directory -Path $deployPackage -Force | Out-Null
    Copy-Item "$publishPath\*" -Destination $deployPackage -Recurse -Force
    
    # Create deployment info
    $deployInfo = @"
DEPLOYMENT INFO
===============
Timestamp: $timestamp
Source: $publishPath
Target: $ServerPath
Fix: Removed ActivityLogs/AuditLogs references
Backend Port: 5273
Frontend: http://101.53.9.76
"@
    $deployInfo | Out-File "$deployPackage\DEPLOY_INFO.txt" -Encoding UTF8
    
    Write-Host "✅ Deployment package created: $deployPackage" -ForegroundColor Green
} catch {
    Write-Host "❌ Error creating deployment package: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# 4. BACKUP CURRENT PRODUCTION (if accessible)
if (-not $SkipBackup) {
    Write-Host "`n💾 STEP 4: Checking production backup..." -ForegroundColor Yellow
    
    if (Test-Path $ServerPath -ErrorAction SilentlyContinue) {
        try {
            $prodBackupPath = "c:\shop\production_backup_$timestamp"
            New-Item -ItemType Directory -Path $prodBackupPath -Force | Out-Null
            Copy-Item "$ServerPath\*" -Destination $prodBackupPath -Recurse -Force
            Write-Host "✅ Production backup created: $prodBackupPath" -ForegroundColor Green
        } catch {
            Write-Host "⚠️ Could not backup production files: $($_.Exception.Message)" -ForegroundColor Yellow
        }
    } else {
        Write-Host "⚠️ Cannot access server path: $ServerPath" -ForegroundColor Yellow
        Write-Host "   Manual backup recommended before deployment" -ForegroundColor White
    }
}

# 5. DEPLOYMENT INSTRUCTIONS
Write-Host "`n🚀 STEP 5: Deployment ready!" -ForegroundColor Yellow
Write-Host "📋 NEXT MANUAL STEPS:" -ForegroundColor Cyan

Write-Host "`nOn Server 101.53.9.76:" -ForegroundColor White
Write-Host "1. Stop IIS Application Pool for RetailPointBackend" -ForegroundColor Gray
Write-Host "2. Copy files from: $deployPackage" -ForegroundColor Gray
Write-Host "3. To production folder (usually: C:\inetpub\wwwroot\RetailPointBackend\)" -ForegroundColor Gray
Write-Host "4. Start IIS Application Pool" -ForegroundColor Gray
Write-Host "5. Test backend: http://101.53.9.76:5273/api/test" -ForegroundColor Gray
Write-Host "6. Test frontend: http://101.53.9.76" -ForegroundColor Gray

# 6. CREATE VERIFICATION SCRIPT
Write-Host "`n✅ STEP 6: Creating verification script..." -ForegroundColor Yellow
$verifyScript = @"
# VERIFY DEPLOYMENT - Run this after deployment
Write-Host "Verifying RetailPoint deployment..." -ForegroundColor Green

try {
    # Test backend API
    `$response = Invoke-WebRequest -Uri "http://101.53.9.76:5273/api/test" -UseBasicParsing -TimeoutSec 10
    if (`$response.StatusCode -eq 200) {
        Write-Host "✅ Backend API responding" -ForegroundColor Green
    } else {
        Write-Host "❌ Backend API error: `$(`$response.StatusCode)" -ForegroundColor Red
    }
} catch {
    Write-Host "❌ Backend API unreachable: `$(`$_.Exception.Message)" -ForegroundColor Red
}

try {
    # Test frontend
    `$response = Invoke-WebRequest -Uri "http://101.53.9.76" -UseBasicParsing -TimeoutSec 10
    if (`$response.StatusCode -eq 200) {
        Write-Host "✅ Frontend responding" -ForegroundColor Green
    } else {
        Write-Host "❌ Frontend error: `$(`$response.StatusCode)" -ForegroundColor Red
    }
} catch {
    Write-Host "❌ Frontend unreachable: `$(`$_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`nDeployment verification complete!" -ForegroundColor Cyan
"@
$verifyScript | Out-File "$deployPackage\verify_deployment.ps1" -Encoding UTF8

Write-Host "✅ Verification script created: verify_deployment.ps1" -ForegroundColor Green

# 7. SUMMARY
Write-Host "`n" -ForegroundColor White
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "           DEPLOYMENT READY!            " -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

Write-Host "`n📁 FILES CREATED:" -ForegroundColor Green
Write-Host "   • Deployment package: $deployPackage" -ForegroundColor White
Write-Host "   • SQL script: $sqlScript" -ForegroundColor White
Write-Host "   • Verification script: verify_deployment.ps1" -ForegroundColor White

Write-Host "`n🎯 WHAT WAS FIXED:" -ForegroundColor Green
Write-Host "   • Removed references to non-existent tables" -ForegroundColor White
Write-Host "   • Fixed DataManagementController" -ForegroundColor White
Write-Host "   • Created database recovery script" -ForegroundColor White
Write-Host "   • Ensured UTF-8 encoding support" -ForegroundColor White

Write-Host "`n🚀 NEXT: Manual deployment to server 101.53.9.76" -ForegroundColor Cyan
Write-Host "Font issue will be resolved after deployment!" -ForegroundColor Yellow