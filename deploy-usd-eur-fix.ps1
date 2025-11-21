# Deploy script hoàn chỉnh - Build và copy lên IIS
Write-Host "=== DEPLOY HOÀN CHỈNH USD/EUR FIX LÊN IIS ===" -ForegroundColor Green

Set-Location "c:\shop"

# Step 1: Build Backend
Write-Host "1. Building Backend..." -ForegroundColor Yellow
dotnet clean Backend\RetailPointBackend
dotnet build Backend\RetailPointBackend --configuration Release

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Backend build failed!" -ForegroundColor Red
    exit 1
}
Write-Host "✅ Backend build success!" -ForegroundColor Green

# Step 2: Build Frontend  
Write-Host "2. Building Frontend..." -ForegroundColor Yellow
Set-Location "c:\shop\client"
npm run build

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Frontend build failed!" -ForegroundColor Red
    exit 1
}
Write-Host "✅ Frontend build success!" -ForegroundColor Green

# Step 3: Prepare for IIS deployment
Write-Host "3. Preparing for IIS deployment..." -ForegroundColor Yellow
Set-Location "c:\shop"

Write-Host "✅ Build completed! Ready for IIS deployment." -ForegroundColor Green
Write-Host "" -ForegroundColor White
Write-Host "=== MANUAL COPY TO IIS ===" -ForegroundColor Cyan
Write-Host "Backend source: Backend\RetailPointBackend\bin\Release\net9.0\" -ForegroundColor White
Write-Host "Frontend source: client\dist\" -ForegroundColor White  
Write-Host "" -ForegroundColor White
Write-Host "After copying:" -ForegroundColor Yellow
Write-Host "1. Restart IIS Application Pool" -ForegroundColor White
Write-Host "2. Test: http://101.53.9.76:5273/api/PaymentStats" -ForegroundColor White
Write-Host "3. Should see USD and EUR separated!" -ForegroundColor Green

# Show exact paths
$backendPath = "$(Get-Location)\Backend\RetailPointBackend\bin\Release\net9.0"
$frontendPath = "$(Get-Location)\client\dist"

Write-Host "" -ForegroundColor White
Write-Host "=== EXACT PATHS ===" -ForegroundColor Yellow
Write-Host "Backend: $backendPath" -ForegroundColor Gray
Write-Host "Frontend: $frontendPath" -ForegroundColor Gray

if (Test-Path $backendPath) {
    Write-Host "✅ Backend files ready" -ForegroundColor Green
} else {
    Write-Host "❌ Backend files not found" -ForegroundColor Red
}

if (Test-Path $frontendPath) {
    Write-Host "✅ Frontend files ready" -ForegroundColor Green  
} else {
    Write-Host "❌ Frontend files not found" -ForegroundColor Red
}