# Script kiểm tra sau deploy
param(
    [string]$ServerUrl = "http://your-server-ip"
)

Write-Host "=== KIỂM TRA BACKEND SAU DEPLOY ===" -ForegroundColor Green
Write-Host "Server: $ServerUrl" -ForegroundColor Yellow
Write-Host ""

# Test 1: Kiểm tra API hoạt động
Write-Host "1. Testing API connection..." -ForegroundColor Cyan
try {
    $response = Invoke-RestMethod -Uri "$ServerUrl/api/LoyaltySettings" -Method GET -TimeoutSec 10
    Write-Host "✅ API hoạt động bình thường" -ForegroundColor Green
    Write-Host "   - Hệ thống tích điểm: $($response.config.isEnabled)" -ForegroundColor White
    Write-Host "   - Số hạng khách hàng: $($response.tiers.Count)" -ForegroundColor White
} catch {
    Write-Host "❌ API không hoạt động: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Test 2: Fix enum mapping
Write-Host ""
Write-Host "2. Fixing enum mapping..." -ForegroundColor Cyan
try {
    $result = Invoke-RestMethod -Uri "$ServerUrl/api/LoyaltySettings/fix-enum-mapping" -Method POST
    Write-Host "✅ $($result.message)" -ForegroundColor Green
    Write-Host "   - Đã sửa: $($result.fixedCount) khách hàng" -ForegroundColor White
} catch {
    Write-Host "❌ Lỗi fix enum: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 3: Update tất cả hạng khách hàng
Write-Host ""
Write-Host "3. Updating all customer tiers..." -ForegroundColor Cyan
try {
    $result = Invoke-RestMethod -Uri "$ServerUrl/api/LoyaltySettings/update-all-tiers" -Method POST
    Write-Host "✅ $($result.message)" -ForegroundColor Green
} catch {
    Write-Host "❌ Lỗi update tiers: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 4: Kiểm tra khách hàng mẫu
Write-Host ""
Write-Host "4. Checking sample customers..." -ForegroundColor Cyan
try {
    $customers = Invoke-RestMethod -Uri "$ServerUrl/api/Customers" -Method GET
    $highPointCustomers = $customers | Where-Object { $_.loyaltyPoints -gt 1000 } | Sort-Object loyaltyPoints -Descending | Select-Object -First 5
    
    Write-Host "✅ Top 5 khách hàng có điểm cao:" -ForegroundColor Green
    foreach ($customer in $highPointCustomers) {
        $tierName = if ($customer.tierId) {
            $tierResult = Invoke-RestMethod -Uri "$ServerUrl/api/CustomerTiers/$($customer.tierId)" -Method GET
            $tierResult.tierName
        } else { "Chưa có" }
        
        Write-Host "   - $($customer.hoTen): $($customer.loyaltyPoints) điểm, Hạng: $tierName" -ForegroundColor White
    }
} catch {
    Write-Host "❌ Lỗi kiểm tra khách hàng: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 5: Test tính điểm
Write-Host ""
Write-Host "5. Testing points calculation..." -ForegroundColor Cyan
$testOrder = @{
    amount = 100000
    customerId = 1
} | ConvertTo-Json

try {
    $result = Invoke-RestMethod -Uri "$ServerUrl/api/LoyaltySettings/calculate-points" -Method POST -Headers @{"Content-Type"="application/json"} -Body $testOrder
    Write-Host "✅ Tính điểm hoạt động:" -ForegroundColor Green
    Write-Host "   - 100,000 VNĐ = $($result.points) điểm" -ForegroundColor White
    Write-Host "   - Công thức: $($result.formula)" -ForegroundColor White
} catch {
    Write-Host "❌ Lỗi tính điểm: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "=== DEPLOY VALIDATION COMPLETED ===" -ForegroundColor Green
Write-Host ""
Write-Host "🚀 HỆ THỐNG TÍCH ĐIỂM ĐÃ SẴN SÀNG!" -ForegroundColor Yellow
Write-Host "Frontend có thể sử dụng các API endpoints:" -ForegroundColor White
Write-Host "- GET  /api/LoyaltySettings - Lấy cài đặt tích điểm" -ForegroundColor Gray
Write-Host "- PUT  /api/LoyaltySettings - Cập nhật cài đặt" -ForegroundColor Gray
Write-Host "- GET  /api/LoyaltySettings/customer-status/{id} - Thông tin khách hàng" -ForegroundColor Gray
Write-Host "- POST /api/LoyaltySettings/calculate-points - Tính điểm" -ForegroundColor Gray