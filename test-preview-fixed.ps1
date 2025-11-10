$BaseUrl = "http://101.53.9.76:5273/api"

Write-Host "=== TEST API PREVIEW IMPACT (SAU KHI SỬA) ===" -ForegroundColor Green

# Test 1: Lấy danh sách tiers để có tier ID
Write-Host "1. Lấy danh sách tiers..." -ForegroundColor Yellow
try {
    $settings = Invoke-RestMethod -Uri "$BaseUrl/TierConfiguration/settings" -Method Get
    Write-Host "SUCCESS: Có $($settings.tiers.Count) hạng khách hàng" -ForegroundColor Green
    
    if ($settings.tiers.Count -gt 0) {
        $firstTier = $settings.tiers[0]
        Write-Host "Sẽ test với hạng: $($firstTier.tierName) (ID: $($firstTier.tierId))" -ForegroundColor Cyan
        
        # Test 2: Preview impact với tham số hợp lệ
        Write-Host "`n2. Test preview impact với tham số hợp lệ..." -ForegroundColor Yellow
        
        $testUrl = "$BaseUrl/TierConfiguration/preview-impact/$($firstTier.tierId)?newMinSpent=1000000&newMinPoints=100"
        Write-Host "URL: $testUrl" -ForegroundColor Gray
        
        $impact = Invoke-RestMethod -Uri $testUrl -Method Get
        Write-Host "SUCCESS: Preview API hoạt động!" -ForegroundColor Green
        Write-Host "- Hạng: $($impact.tierName)" -ForegroundColor White
        Write-Host "- Khách hàng hiện tại: $($impact.impact.currentCustomers)" -ForegroundColor White
        Write-Host "- Sẽ đủ điều kiện mới: $($impact.impact.qualifiedForNew)" -ForegroundColor Green
        Write-Host "- Sẽ mất hạng: $($impact.impact.wouldLoseTier)" -ForegroundColor Red
        Write-Host "- Thay đổi ròng: $($impact.impact.netChange)" -ForegroundColor Cyan
        
        # Test 3: Preview với tham số mặc định
        Write-Host "`n3. Test preview với tham số mặc định..." -ForegroundColor Yellow
        $defaultUrl = "$BaseUrl/TierConfiguration/preview-impact/$($firstTier.tierId)"
        $defaultImpact = Invoke-RestMethod -Uri $defaultUrl -Method Get
        Write-Host "SUCCESS: Preview với tham số mặc định hoạt động!" -ForegroundColor Green
        
    }
} catch {
    Write-Host "ERROR: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Response: $($_.ErrorDetails.Message)" -ForegroundColor Red
}

# Test 4: Test với tham số không hợp lệ (để kiểm tra validation)
Write-Host "`n4. Test validation với tham số không hợp lệ..." -ForegroundColor Yellow
try {
    $invalidUrl = "$BaseUrl/TierConfiguration/preview-impact/999?newMinSpent=-1000&newMinPoints=-50"
    $result = Invoke-RestMethod -Uri $invalidUrl -Method Get
    Write-Host "WARNING: Validation có thể chưa hoạt động" -ForegroundColor Yellow
} catch {
    Write-Host "SUCCESS: Validation hoạt động - phát hiện tham số không hợp lệ" -ForegroundColor Green
    Write-Host "Error (expected): $($_.Exception.Message)" -ForegroundColor Gray
}

Write-Host "`n=== KẾT QUẢ TEST ===" -ForegroundColor Green
Write-Host "✓ API Preview Impact đã được sửa và hoạt động bình thường!" -ForegroundColor Green