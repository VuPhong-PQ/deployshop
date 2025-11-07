# Quick fix script cho enum mapping
Write-Host "=== FIX ENUM MAPPING ===" -ForegroundColor Green

$baseUrl = "http://101.53.9.76:5273"

# Test kết nối trước
try {
    Write-Host "Testing connection..." -ForegroundColor Cyan
    $test = Invoke-RestMethod -Uri "$baseUrl/api/LoyaltySettings" -Method GET -TimeoutSec 5
    Write-Host "✅ Backend is running" -ForegroundColor Green
} catch {
    Write-Host "❌ Backend not running or wrong URL" -ForegroundColor Red
    Write-Host "Please make sure backend is running on $baseUrl" -ForegroundColor Yellow
    exit 1
}

# Gọi API fix enum mapping
Write-Host "Calling fix enum mapping API..." -ForegroundColor Cyan
try {
    $result = Invoke-RestMethod -Uri "$baseUrl/api/LoyaltySettings/fix-enum-mapping" -Method POST
    Write-Host "✅ $($result.message)" -ForegroundColor Green
    Write-Host "   Fixed: $($result.fixedCount) customers" -ForegroundColor White
    Write-Host "   Total: $($result.totalCustomers) customers" -ForegroundColor White
} catch {
    Write-Host "❌ Error: $($_.Exception.Message)" -ForegroundColor Red
}

# Kiểm tra lại khách hàng Chú hạnh
Write-Host ""
Write-Host "Checking Chu hanh customer..." -ForegroundColor Cyan
try {
    $customers = Invoke-RestMethod -Uri "$baseUrl/api/Customers" -Method GET
    $chuHanh = $customers | Where-Object { $_.hoTen -like "*hanh*" -or $_.hoTen -like "*hạnh*" }
    
    if ($chuHanh) {
        Write-Host "✅ Found Chu hanh:" -ForegroundColor Green
        Write-Host "   - Customer ID: $($chuHanh.customerId)" -ForegroundColor White
        Write-Host "   - Points: $($chuHanh.loyaltyPoints)" -ForegroundColor White
        Write-Host "   - Total Spent: $($chuHanh.totalSpent)" -ForegroundColor White
        Write-Host "   - Tier ID: $($chuHanh.tierId)" -ForegroundColor White
        Write-Host "   - Hang Khach Hang: $($chuHanh.hangKhachHang)" -ForegroundColor Green
        
        # Lấy thông tin tier chi tiết
        $loyaltyStatus = Invoke-RestMethod -Uri "$baseUrl/api/LoyaltySettings/customer-status/$($chuHanh.customerId)" -Method GET
        Write-Host "   - Current Tier: $($loyaltyStatus.currentTier.tierName)" -ForegroundColor Green
    } else {
        Write-Host "❌ Chu hanh not found" -ForegroundColor Red
    }
} catch {
    Write-Host "❌ Error checking customer: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "=== COMPLETED ===" -ForegroundColor Green