# Test script cho Loyalty System API
# Chạy script này khi backend server đang chạy

$baseUrl = "http://localhost:5273"
$headers = @{
    "Content-Type" = "application/json"
}

Write-Host "=== TESTING LOYALTY SYSTEM API ===" -ForegroundColor Green
Write-Host "Base URL: $baseUrl" -ForegroundColor Yellow
Write-Host ""

# Test 1: Lấy cài đặt tích điểm hiện tại
Write-Host "1. Testing GET /api/LoyaltySettings" -ForegroundColor Cyan
try {
    $response = Invoke-RestMethod -Uri "$baseUrl/api/LoyaltySettings" -Method GET -Headers $headers
    Write-Host "✅ Success: Lấy cài đặt tích điểm thành công" -ForegroundColor Green
    Write-Host "   - Hệ thống tích điểm: $($response.config.isEnabled)" -ForegroundColor White
    Write-Host "   - Số điểm cho 1K VNĐ: $($response.config.pointsPerCurrency)" -ForegroundColor White
    Write-Host "   - Số hạng khách hàng: $($response.tiers.Count)" -ForegroundColor White
    
    # Hiển thị các hạng khách hàng
    Write-Host "   Các hạng khách hàng:" -ForegroundColor White
    foreach ($tier in $response.tiers) {
        Write-Host "     + $($tier.tierName): Chi tiêu tối thiểu $($tier.minSpent), Điểm tối thiểu $($tier.minPoints)" -ForegroundColor Gray
    }
} catch {
    Write-Host "❌ Error: $($_.Exception.Message)" -ForegroundColor Red
}
Write-Host ""

# Test 2: Lấy thông tin config tích điểm cũ (compatibility)
Write-Host "2. Testing GET /api/LoyaltyConfig" -ForegroundColor Cyan
try {
    $response = Invoke-RestMethod -Uri "$baseUrl/api/LoyaltyConfig" -Method GET -Headers $headers
    Write-Host "✅ Success: API tương thích cũ hoạt động" -ForegroundColor Green
    Write-Host "   - Enabled: $($response.isEnabled)" -ForegroundColor White
    Write-Host "   - Points per currency: $($response.pointsPerCurrency)" -ForegroundColor White
} catch {
    Write-Host "❌ Error: $($_.Exception.Message)" -ForegroundColor Red
}
Write-Host ""

# Test 3: Lấy danh sách hạng khách hàng
Write-Host "3. Testing GET /api/CustomerTiers" -ForegroundColor Cyan
try {
    $response = Invoke-RestMethod -Uri "$baseUrl/api/CustomerTiers" -Method GET -Headers $headers
    Write-Host "✅ Success: Lấy danh sách hạng thành công" -ForegroundColor Green
    Write-Host "   - Số hạng: $($response.Count)" -ForegroundColor White
    foreach ($tier in $response) {
        Write-Host "     $($tier.tierName): $($tier.description)" -ForegroundColor Gray
    }
} catch {
    Write-Host "❌ Error: $($_.Exception.Message)" -ForegroundColor Red
}
Write-Host ""

# Test 4: Tính điểm cho đơn hàng mẫu
Write-Host "4. Testing POST /api/LoyaltySettings/calculate-points" -ForegroundColor Cyan
$testOrder = @{
    amount = 100000
    customerId = 1
} | ConvertTo-Json

try {
    $response = Invoke-RestMethod -Uri "$baseUrl/api/LoyaltySettings/calculate-points" -Method POST -Headers $headers -Body $testOrder
    Write-Host "✅ Success: Tính điểm thành công" -ForegroundColor Green
    Write-Host "   - Đơn hàng 100,000 VNĐ = $($response.points) điểm" -ForegroundColor White
    Write-Host "   - Công thức: $($response.formula)" -ForegroundColor White
    if ($response.bonusInfo.Count -gt 0) {
        Write-Host "   - Bonus: $($response.bonusInfo -join ', ')" -ForegroundColor Yellow
    }
} catch {
    Write-Host "❌ Error: $($_.Exception.Message)" -ForegroundColor Red
}
Write-Host ""

# Test 5: Kiểm tra khách hàng có ID = 1 (nếu có)
Write-Host "5. Testing GET /api/LoyaltySettings/customer-status/1" -ForegroundColor Cyan
try {
    $response = Invoke-RestMethod -Uri "$baseUrl/api/LoyaltySettings/customer-status/1" -Method GET -Headers $headers
    Write-Host "✅ Success: Lấy thông tin tích điểm khách hàng thành công" -ForegroundColor Green
    Write-Host "   - Khách hàng: $($response.customerName)" -ForegroundColor White
    Write-Host "   - Tổng chi tiêu: $($response.totalSpent)" -ForegroundColor White
    Write-Host "   - Tổng điểm: $($response.totalPoints)" -ForegroundColor White
    if ($response.currentTier) {
        Write-Host "   - Hạng hiện tại: $($response.currentTier.tierName)" -ForegroundColor Green
    }
    if ($response.nextTier) {
        Write-Host "   - Hạng tiếp theo: $($response.nextTier.tierName)" -ForegroundColor Yellow
        Write-Host "   - Cần thêm $($response.progress.spentToNext) VNĐ và $($response.progress.pointsToNext) điểm" -ForegroundColor Yellow
    }
} catch {
    Write-Host "❌ Error: $($_.Exception.Message)" -ForegroundColor Red
}
Write-Host ""

# Test 6: Lấy danh sách khách hàng để kiểm tra
Write-Host "6. Testing GET /api/Customers (first 5)" -ForegroundColor Cyan
try {
    $response = Invoke-RestMethod -Uri "$baseUrl/api/Customers" -Method GET -Headers $headers
    Write-Host "✅ Success: Có $($response.Count) khách hàng trong hệ thống" -ForegroundColor Green
    
    $firstFive = $response | Select-Object -First 5
    foreach ($customer in $firstFive) {
        Write-Host "   - ID $($customer.customerId): $($customer.hoTen) (Điểm: $($customer.loyaltyPoints), Chi tiêu: $($customer.totalSpent))" -ForegroundColor Gray
    }
} catch {
    Write-Host "❌ Error: $($_.Exception.Message)" -ForegroundColor Red
}
Write-Host ""

# Test 7: Kiểm tra notification system
Write-Host "7. Testing GET /api/Notifications" -ForegroundColor Cyan
try {
    $response = Invoke-RestMethod -Uri "$baseUrl/api/Notifications" -Method GET -Headers $headers
    Write-Host "✅ Success: Có $($response.Count) thông báo trong hệ thống" -ForegroundColor Green
    
    # Tìm thông báo liên quan đến tier upgrade
    $tierNotifications = $response | Where-Object { $_.type -eq "tier_upgrade" }
    if ($tierNotifications.Count -gt 0) {
        Write-Host "   - Có $($tierNotifications.Count) thông báo nâng hạng" -ForegroundColor Yellow
    }
} catch {
    Write-Host "❌ Error: $($_.Exception.Message)" -ForegroundColor Red
}
Write-Host ""

Write-Host "=== TEST COMPLETED ===" -ForegroundColor Green
Write-Host ""
Write-Host "HƯỚNG DẪN SỬ DỤNG API:" -ForegroundColor Yellow
Write-Host "1. Cài đặt tích điểm: PUT /api/LoyaltySettings" -ForegroundColor White
Write-Host "2. Xem thông tin tích điểm khách hàng: GET /api/LoyaltySettings/customer-status/{customerId}" -ForegroundColor White
Write-Host "3. Tính điểm: POST /api/LoyaltySettings/calculate-points" -ForegroundColor White
Write-Host "4. Cập nhật hạng tất cả khách hàng: POST /api/LoyaltySettings/update-all-tiers" -ForegroundColor White
Write-Host ""
Write-Host "LƯU Ý: Khi tạo đơn hàng mới, hệ thống sẽ tự động:" -ForegroundColor Yellow
Write-Host "- Tính điểm và tích cho khách hàng" -ForegroundColor White
Write-Host "- Kiểm tra và nâng hạng khách hàng nếu đủ điều kiện" -ForegroundColor White
Write-Host "- Tạo thông báo nâng hạng cho khách hàng" -ForegroundColor White