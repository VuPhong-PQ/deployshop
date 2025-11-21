# Deploy Foreign Currency Payment Methods
# Date: 2025-01-21
# Description: Deploy USD and EUR payment method support

Write-Host "🚀 Deploying Foreign Currency Payment Methods..." -ForegroundColor Green
Write-Host "=====================================`n" -ForegroundColor Cyan

# 1. Database Migration
Write-Host "📊 Step 1: Database Migration" -ForegroundColor Yellow
Write-Host "Please run the SQL migration manually:" -ForegroundColor White
Write-Host "sqlcmd -S your_server -d your_database -i add_foreign_currency_payment_methods.sql`n" -ForegroundColor Gray

# 2. Backend Build
Write-Host "🔧 Step 2: Building Backend..." -ForegroundColor Yellow
Set-Location "Backend\RetailPointBackend"

Write-Host "Building .NET project..." -ForegroundColor White
dotnet build --configuration Release

if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Backend build successful!" -ForegroundColor Green
} else {
    Write-Host "❌ Backend build failed!" -ForegroundColor Red
    exit 1
}

# 3. Frontend Build  
Write-Host "`n🎨 Step 3: Building Frontend..." -ForegroundColor Yellow
Set-Location "..\..\client"

Write-Host "Installing dependencies..." -ForegroundColor White
npm install

Write-Host "Building React project..." -ForegroundColor White  
npm run build

if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Frontend build successful!" -ForegroundColor Green
} else {
    Write-Host "❌ Frontend build failed!" -ForegroundColor Red
    exit 1
}

# 4. Backend Start (Development)
Write-Host "`n🔧 Step 4: Starting Backend..." -ForegroundColor Yellow
Set-Location "..\Backend\RetailPointBackend"

Start-Process powershell -ArgumentList "-NoExit", "-Command", "dotnet run" -WindowStyle Normal

Write-Host "Backend starting in new window..." -ForegroundColor White
Start-Sleep 3

# 5. Frontend Start (Development)
Write-Host "`n🎨 Step 5: Starting Frontend..." -ForegroundColor Yellow  
Set-Location "..\..\client"

Start-Process powershell -ArgumentList "-NoExit", "-Command", "npm run dev" -WindowStyle Normal

Write-Host "Frontend starting in new window..." -ForegroundColor White

# Summary
Write-Host "`n✅ DEPLOYMENT SUMMARY" -ForegroundColor Green
Write-Host "=====================" -ForegroundColor Cyan
Write-Host "✅ Backend: Built and starting" -ForegroundColor White
Write-Host "✅ Frontend: Built and starting" -ForegroundColor White
Write-Host "⚠️  Database: Manual migration required" -ForegroundColor Yellow
Write-Host ""
Write-Host "🎯 NEW FEATURES AVAILABLE:" -ForegroundColor Green
Write-Host "• USD Payment Method (💲)" -ForegroundColor White
Write-Host "• EUR Payment Method (€)" -ForegroundColor White
Write-Host "• Settings → Thanh toán tab updated" -ForegroundColor White
Write-Host "• Sales page with new payment options" -ForegroundColor White
Write-Host "• Reports include foreign currencies" -ForegroundColor White
Write-Host ""
Write-Host "🔗 Access URLs:" -ForegroundColor Cyan
Write-Host "Frontend: http://localhost:3000" -ForegroundColor White
Write-Host "Backend:  http://localhost:5000" -ForegroundColor White
Write-Host ""
Write-Host "📖 See FOREIGN_CURRENCY_IMPLEMENTATION.md for full details" -ForegroundColor Blue

Set-Location "..\..\"