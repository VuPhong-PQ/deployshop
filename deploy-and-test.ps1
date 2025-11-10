# Script deploy lên production
Write-Host "Building và deploying backend lên production..." -ForegroundColor Yellow

# 1. Build project
Write-Host "Building project..." -ForegroundColor Cyan
Set-Location "c:\shop\Backend\RetailPointBackend"
dotnet build --configuration Release

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}

# 2. Publish project
Write-Host "Publishing project..." -ForegroundColor Cyan
dotnet publish --configuration Release --output ".\publish"

if ($LASTEXITCODE -ne 0) {
    Write-Host "Publish failed!" -ForegroundColor Red
    exit 1
}

Write-Host "Build completed successfully!" -ForegroundColor Green
Write-Host "Deploy files ready in: c:\shop\Backend\RetailPointBackend\publish" -ForegroundColor Green

# 3. Test với production server
Write-Host "`nTesting with production server..." -ForegroundColor Yellow
Set-Location "c:\shop"

# Chờ một chút để server restart
Write-Host "Waiting for server to restart (30 seconds)..." -ForegroundColor Yellow
Start-Sleep -Seconds 30

# Run test
Write-Host "Running auto-discount test..." -ForegroundColor Cyan
.\test-no-auto-discount.ps1