# Script kiểm tra hệ thống Loyalty trên Production
# Chạy: .\test-loyalty-production.ps1

Write-Host "🎯 KIỂM TRA HỆ THỐNG LOYALTY TRÊN PRODUCTION" -ForegroundColor Green
Write-Host "=" * 60 -ForegroundColor Yellow

$API_BASE = "http://localhost:5000/api"  # URL production
$headers = @{
    "Content-Type" = "application/json"
}

# Test 1: Kiểm tra Loyalty Settings
Write-Host "`n1. 📋 KIỂM TRA CÀI ĐẶT LOYALTY..." -ForegroundColor Cyan
try {
    $loyaltySettings = Invoke-RestMethod -Uri "$API_BASE/LoyaltySystemSettings/settings" -Method Get
    Write-Host "✅ Loyalty Settings OK:" -ForegroundColor Green
    Write-Host "   - Tích điểm: $($loyaltySettings.isEnabled)" -ForegroundColor White
    Write-Host "   - Đổi điểm: $($loyaltySettings.isRedemptionEnabled)" -ForegroundColor White
    Write-Host "   - Tỷ lệ tích: $($loyaltySettings.pointsRate) VNĐ/điểm" -ForegroundColor White
    Write-Host "   - Hết hạn: $($loyaltySettings.pointsExpirationDays) ngày" -ForegroundColor White
} catch {
    Write-Host "❌ Lỗi Loyalty Settings: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 2: Kiểm tra Customer Tiers
Write-Host "`n2. 🏆 KIỂM TRA CÁC HẠNG KHÁCH HÀNG..." -ForegroundColor Cyan
try {
    $tiers = Invoke-RestMethod -Uri "$API_BASE/CustomerTierManagement" -Method Get
    Write-Host "✅ Customer Tiers OK - Tổng: $($tiers.Count) hạng" -ForegroundColor Green
    foreach ($tier in $tiers) {
        $color = if ($tier.isActive) { "Green" } else { "Red" }
        Write-Host "   - $($tier.tierName): Chi tiêu ≥ $([math]::Round($tier.minSpent/1000))K, Hệ số x$($tier.pointsMultiplier), Giảm $($tier.discountPercentage)%" -ForegroundColor $color
    }
} catch {
    Write-Host "❌ Lỗi Customer Tiers: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 3: Kiểm tra Tier Benefits API
Write-Host "`n3. 🎁 KIỂM TRA QUYỀN LỢI THEO HẠNG..." -ForegroundColor Cyan
try {
    $benefits = Invoke-RestMethod -Uri "$API_BASE/TierBenefits/tier-benefits" -Method Get
    Write-Host "✅ Tier Benefits OK - Tổng: $($benefits.Count) hạng có quyền lợi" -ForegroundColor Green
    foreach ($benefit in $benefits) {
        Write-Host "   - $($benefit.tierName):" -ForegroundColor White
        Write-Host "     * Bonus cuối tuần: $($benefit.weekendBonusEnabled)" -ForegroundColor Gray
        Write-Host "     * Bonus sinh nhật: $($benefit.birthdayBonusEnabled)" -ForegroundColor Gray
        Write-Host "     * Mô tả: $($benefit.description)" -ForegroundColor Gray
    }
} catch {
    Write-Host "❌ Lỗi Tier Benefits: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 4: Test Tính Toán Điểm cho Khách Hàng Mẫu
Write-Host "`n4. 🧮 TEST TÍNH TOÁN ĐIỂM THỰC TẾ..." -ForegroundColor Cyan
try {
    # Lấy thông tin khách hàng đầu tiên
    $customers = Invoke-RestMethod -Uri "$API_BASE/customers" -Method Get
    if ($customers -and $customers.Count -gt 0) {
        $testCustomer = $customers[0]
        Write-Host "✅ Test với khách hàng: $($testCustomer.name) (ID: $($testCustomer.customerId))" -ForegroundColor Green
        
        # Lấy thông tin loyalty của khách hàng
        $customerStatus = Invoke-RestMethod -Uri "$API_BASE/LoyaltySystemSettings/customer-status/$($testCustomer.customerId)" -Method Get
        Write-Host "   - Hạng hiện tại: $($customerStatus.currentTier)" -ForegroundColor White
        Write-Host "   - Điểm hiện có: $($customerStatus.currentPoints)" -ForegroundColor White
        Write-Host "   - Tổng chi tiêu: $([math]::Round($customerStatus.totalSpent/1000))K VNĐ" -ForegroundColor White
    } else {
        Write-Host "⚠️ Không có khách hàng để test" -ForegroundColor Yellow
    }
} catch {
    Write-Host "❌ Lỗi Test Customer: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 5: Test Tạo Đơn Hàng với Loyalty
Write-Host "`n5. 💰 TEST TẠO ĐƠN HÀNG VỚI LOYALTY..." -ForegroundColor Cyan
try {
    # Lấy sản phẩm đầu tiên để test
    $products = Invoke-RestMethod -Uri "$API_BASE/products" -Method Get
    if ($products -and $products.Count -gt 0) {
        $testProduct = $products[0]
        Write-Host "✅ Sản phẩm test: $($testProduct.name) - Giá: $([math]::Round($testProduct.price/1000))K VNĐ" -ForegroundColor Green
        
        # Tính toán điểm sẽ nhận được
        $orderAmount = $testProduct.price
        $pointsRate = if ($loyaltySettings.pointsRate) { $loyaltySettings.pointsRate } else { 1000 }
        $expectedPoints = [math]::Floor($orderAmount / $pointsRate)
        Write-Host "   - Điểm dự kiến nhận: $expectedPoints điểm (trước hệ số hạng)" -ForegroundColor White
        Write-Host "   - Với hạng Vàng (x1.5): $([math]::Floor($expectedPoints * 1.5)) điểm" -ForegroundColor White
        Write-Host "   - Với hạng VIP (x3.0): $([math]::Floor($expectedPoints * 3.0)) điểm" -ForegroundColor White
    }
} catch {
    Write-Host "❌ Lỗi Test Order: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 6: Kiểm tra Database Tables
Write-Host "`n6. 🗄️ KIỂM TRA DATABASE..." -ForegroundColor Cyan
try {
    # Kiểm tra các bảng loyalty trong database
    $sqlQueries = @(
        "SELECT COUNT(*) as TotalCustomers FROM Customers",
        "SELECT COUNT(*) as TotalTiers FROM CustomerTiers", 
        "SELECT COUNT(*) as TotalLoyaltyTransactions FROM LoyaltyTransactions",
        "SELECT COUNT(*) as LoyaltySettingsCount FROM LoyaltySettings"
    )
    
    foreach ($query in $sqlQueries) {
        $result = & sqlcmd -S "TEST-PC\KTEAM" -d RetailPoint -U sa -P "sa@123" -Q $query -h -1 -W 2>&1
        if ($result -match '\d+') {
            $count = [regex]::Match($result, '\d+').Value
            $tableName = ($query -split ' ')[3]
            Write-Host "   - Bảng $tableName : $count records" -ForegroundColor Green
        }
    }
} catch {
    Write-Host "⚠️ Không thể kiểm tra database trực tiếp" -ForegroundColor Yellow
}

# Test 7: Kiểm tra Log Files
Write-Host "`n7. 📋 KIỂM TRA LOGS..." -ForegroundColor Cyan
$logPath = "C:\shop\backend-deploy\logs"
if (Test-Path $logPath) {
    $logFiles = Get-ChildItem $logPath -Filter "*.log" | Sort-Object LastWriteTime -Descending | Select-Object -First 3
    if ($logFiles) {
        Write-Host "✅ Log files mới nhất:" -ForegroundColor Green
        foreach ($log in $logFiles) {
            Write-Host "   - $($log.Name) ($($log.LastWriteTime))" -ForegroundColor White
        }
    }
} else {
    Write-Host "⚠️ Không tìm thấy thư mục logs" -ForegroundColor Yellow
}

# Kết luận
Write-Host "`n" + "=" * 60 -ForegroundColor Yellow
Write-Host "🎉 HOÀN THÀNH KIỂM TRA HỆ THỐNG LOYALTY!" -ForegroundColor Green
Write-Host "`n📝 HƯỚNG DẪN SỬ DỤNG:" -ForegroundColor Cyan
Write-Host "1. Vào giao diện admin → Cài đặt → Tích điểm thưởng" -ForegroundColor White
Write-Host "2. Bật/tắt tính năng tích điểm và đổi điểm" -ForegroundColor White
Write-Host "3. Vào Quản lý hạng khách hàng để tùy chỉnh các hạng" -ForegroundColor White
Write-Host "4. Khi bán hàng, hệ thống sẽ tự động:" -ForegroundColor White
Write-Host "   - Tính điểm theo hạng khách hàng" -ForegroundColor Gray
Write-Host "   - Áp dụng giảm giá tự động" -ForegroundColor Gray
Write-Host "   - Nâng hạng khi đủ điều kiện" -ForegroundColor Gray
Write-Host "   - Cong bonus diem ngay dac biet" -ForegroundColor Gray

Write-Host "`n🌐 Test trên trình duyệt: http://localhost:5000" -ForegroundColor Magenta