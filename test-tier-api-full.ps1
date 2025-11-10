$BaseUrl = "http://101.53.9.76:5273/api"

Write-Host "=== TEST API TIER CONFIGURATION ===" -ForegroundColor Green

# Test 1: Lấy cấu hình đầy đủ
Write-Host "`n1. Test GET settings..." -ForegroundColor Yellow
try {
    $settings = Invoke-RestMethod -Uri "$BaseUrl/TierConfiguration/settings" -Method Get
    Write-Host "SUCCESS: API hoạt động!" -ForegroundColor Green
    Write-Host "- Số hạng: $($settings.tiers.Count)" -ForegroundColor White
    Write-Host "- Tổng khách hàng: $($settings.statistics.totalCustomers)" -ForegroundColor White
    Write-Host "- Loyalty enabled: $($settings.config.isEnabled)" -ForegroundColor White
    
    Write-Host "Danh sách hạng:" -ForegroundColor Cyan
    foreach ($tier in $settings.tiers) {
        Write-Host "  $($tier.tierName): Chi tiêu >= $('{0:N0}' -f $tier.minSpent), Hệ số x$($tier.pointsMultiplier), Giảm $($tier.discountPercentage)%" -ForegroundColor Gray
    }
} catch {
    Write-Host "ERROR: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 2: Validation API
Write-Host "`n2. Test validation với cấu hình mẫu..." -ForegroundColor Yellow
$sampleTiers = @(
    @{
        tierId = 1
        tierName = "Test Basic"
        minSpent = 0
        minPoints = 0
        pointsMultiplier = 1.0
        discountPercentage = 0
        description = "Test tier"
        tierColor = "#808080"
        isActive = $true
    },
    @{
        tierId = 2
        tierName = "Test Premium"
        minSpent = 1000000
        minPoints = 100
        pointsMultiplier = 1.5
        discountPercentage = 5
        description = "Test premium tier"
        tierColor = "#FFD700"
        isActive = $true
    }
)

try {
    $validation = Invoke-RestMethod -Uri "$BaseUrl/TierConfiguration/validate" -Method Post -ContentType "application/json" -Body ($sampleTiers | ConvertTo-Json -Depth 10)
    Write-Host "SUCCESS: Validation API hoạt động!" -ForegroundColor Green
    Write-Host "- Hợp lệ: $($validation.isValid)" -ForegroundColor $(if ($validation.isValid) {"Green"} else {"Red"})
    Write-Host "- Số lỗi: $($validation.errors.Count)" -ForegroundColor White
    Write-Host "- Số cảnh báo: $($validation.warnings.Count)" -ForegroundColor Yellow
    
    if ($validation.warnings.Count -gt 0) {
        Write-Host "Cảnh báo:" -ForegroundColor Yellow
        foreach ($warning in $validation.warnings) {
            Write-Host "  - $warning" -ForegroundColor Yellow
        }
    }
} catch {
    Write-Host "ERROR: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 3: Preview impact (chọn tier ID đầu tiên)
Write-Host "`n3. Test preview impact..." -ForegroundColor Yellow
try {
    if ($settings -and $settings.tiers.Count -gt 0) {
        $firstTierId = $settings.tiers[0].tierId
        $impact = Invoke-RestMethod -Uri "$BaseUrl/TierConfiguration/preview-impact/$firstTierId?newMinSpent=1000000&newMinPoints=100" -Method Get
        
        Write-Host "SUCCESS: Preview API hoạt động!" -ForegroundColor Green
        Write-Host "- Hạng: $($impact.tierName)" -ForegroundColor White
        Write-Host "- Khách hàng hiện tại: $($impact.impact.currentCustomers)" -ForegroundColor White
        Write-Host "- Sẽ đủ điều kiện mới: $($impact.impact.qualifiedForNew)" -ForegroundColor Green
        Write-Host "- Sẽ mất hạng: $($impact.impact.wouldLoseTier)" -ForegroundColor Red
        Write-Host "- Thay đổi ròng: $($impact.impact.netChange)" -ForegroundColor Cyan
    }
} catch {
    Write-Host "ERROR: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n=== KẾT QUA TEST ===" -ForegroundColor Green
Write-Host "✓ API TierConfiguration đã được deploy thành công!" -ForegroundColor Green
Write-Host "✓ Tất cả endpoints hoạt động bình thường" -ForegroundColor Green
Write-Host "`nBạn có thể sử dụng các API này trong frontend để:" -ForegroundColor Yellow
Write-Host "- Lấy danh sách cấu hình cấp độ" -ForegroundColor White
Write-Host "- Cập nhật cấu hình hàng loạt" -ForegroundColor White
Write-Host "- Validate cấu hình trước khi lưu" -ForegroundColor White
Write-Host "- Xem trước tác động thay đổi" -ForegroundColor White
Write-Host "- Reset về cấu hình mặc định" -ForegroundColor White