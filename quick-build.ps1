# Build và deploy nhanh với fix cho banktransfer
Write-Host "Build và deploy nhanh với fix xử lý cả banktransfer và ngoại tệ..." -ForegroundColor Green

# Build backend
Set-Location "c:\shop"
Write-Host "Building backend..." -ForegroundColor Yellow
dotnet build Backend\RetailPointBackend
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Build failed!" -ForegroundColor Red
    exit 1
}

# Build frontend
Set-Location "c:\shop\client"
Write-Host "Building frontend..." -ForegroundColor Yellow
npm run build
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Frontend build failed!" -ForegroundColor Red
    exit 1
}

Write-Host "✅ Build thành công!" -ForegroundColor Green
Write-Host "Bây giờ hãy:" -ForegroundColor Cyan
Write-Host "1. Copy files deploy vào IIS (nếu cần)" -ForegroundColor White
Write-Host "2. Restart IIS Application Pool" -ForegroundColor White
Write-Host "3. Test lại API" -ForegroundColor White

Set-Location "c:\shop"