# Simple Backend Deploy Script
Write-Host "=== Starting Backend Deployment ===" -ForegroundColor Green

# Paths
$SourcePath = "C:\shop\Backend\RetailPointBackend\bin\Release\net8.0"
$ServerPath = "C:\RetailPoint-Backend"

Write-Host "Source: $SourcePath" -ForegroundColor Cyan
Write-Host "Target: $ServerPath" -ForegroundColor Cyan

# Step 1: Build project
Write-Host "`n1. Building project..." -ForegroundColor Yellow
Set-Location "C:\shop\Backend\RetailPointBackend"
dotnet build -c Release

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Build failed!" -ForegroundColor Red
    exit 1
}

# Step 2: Check if exe exists
if (-not (Test-Path "$SourcePath\RetailPointBackend.exe")) {
    Write-Host "❌ RetailPointBackend.exe not found after build!" -ForegroundColor Red
    exit 1
}

# Step 3: Stop existing service if running
Write-Host "`n2. Stopping existing service..." -ForegroundColor Yellow
try {
    $process = Get-Process "RetailPointBackend" -ErrorAction SilentlyContinue
    if ($process) {
        Stop-Process -Name "RetailPointBackend" -Force
        Start-Sleep -Seconds 3
        Write-Host "✅ Service stopped" -ForegroundColor Green
    } else {
        Write-Host "ℹ️ No running service found" -ForegroundColor Cyan
    }
} catch {
    Write-Host "Warning: Could not stop service: $($_.Exception.Message)" -ForegroundColor Yellow
}

# Step 4: Copy files
Write-Host "`n3. Copying files..." -ForegroundColor Yellow
try {
    if (-not (Test-Path $ServerPath)) {
        New-Item -ItemType Directory -Path $ServerPath -Force
    }
    
    Copy-Item -Path "$SourcePath\*" -Destination $ServerPath -Recurse -Force
    Write-Host "✅ Files copied successfully" -ForegroundColor Green
} catch {
    Write-Host "❌ Copy failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Step 5: Start service
Write-Host "`n4. Starting service..." -ForegroundColor Yellow
Set-Location $ServerPath
Start-Process -FilePath ".\RetailPointBackend.exe" -WindowStyle Hidden

Start-Sleep -Seconds 5

# Step 6: Test service
Write-Host "`n5. Testing service..." -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "http://localhost:5273/api/health" -Method GET -TimeoutSec 10
    Write-Host "✅ Service is running!" -ForegroundColor Green
} catch {
    Write-Host "⚠️ Service test failed, but deployment completed" -ForegroundColor Yellow
}

Write-Host "`n=== Deployment Completed ===" -ForegroundColor Green
Set-Location "C:\shop"