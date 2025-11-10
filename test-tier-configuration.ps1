# Script test các API cấu hình cấp độ khách hàng
param(
    [string]$BaseUrl = "http://localhost:5000/api"
)

Write-Host "=== TEST HỆ THỐNG CẤU HÌNH CẤP ĐỘ KHÁCH HÀNG ===" -ForegroundColor Cyan

# Test 1: Lấy cấu hình hiện tại
Write-Host "`n1. Lấy cấu hình cấp độ hiện tại..." -ForegroundColor Yellow
try {
    $settings = Invoke-RestMethod -Uri "$BaseUrl/TierConfiguration/settings" -Method Get
    Write-Host "✓ Lấy cấu hình thành công" -ForegroundColor Green
    Write-Host "- Số hạng hiện tại: $($settings.tiers.Count)" -ForegroundColor White
    Write-Host "- Tổng khách hàng: $($settings.statistics.totalCustomers)" -ForegroundColor White
    
    foreach ($tier in $settings.tiers) {
        Write-Host "  * $($tier.tierName): Chi tiêu >= $('{0:N0}' -f $tier.minSpent) VNĐ, Hệ số x$($tier.pointsMultiplier), Giảm $($tier.discountPercentage)%" -ForegroundColor Cyan
    }
} catch {
    Write-Host "✗ Lỗi: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 2: Validate cấu hình mẫu
Write-Host "`n2. Test validation cấu hình..." -ForegroundColor Yellow
$sampleConfig = @(
    @{
        tierId = 1
        tierName = "Khách hàng mới"
        minSpent = 0
        minPoints = 0
        pointsMultiplier = 1.0
        discountPercentage = 0
        description = "Hạng cơ bản cho khách hàng mới"
        tierColor = "#808080"
        isActive = $true
    },
    @{
        tierId = 2
        tierName = "Thân thiết"
        minSpent = 2000000
        minPoints = 200
        pointsMultiplier = 1.3
        discountPercentage = 5
        description = "Khách hàng thân thiết"
        tierColor = "#4CAF50"
        isActive = $true
    },
    @{
        tierId = 3
        tierName = "VIP"
        minSpent = 10000000
        minPoints = 1000
        pointsMultiplier = 1.8
        discountPercentage = 12
        description = "Khách hàng VIP"
        tierColor = "#FF9800"
        isActive = $true
    }
)

try {
    $validation = Invoke-RestMethod -Uri "$BaseUrl/TierConfiguration/validate" -Method Post -ContentType "application/json" -Body ($sampleConfig | ConvertTo-Json -Depth 10)
    
    if ($validation.isValid) {
        Write-Host "✓ Cấu hình hợp lệ" -ForegroundColor Green
    } else {
        Write-Host "✗ Cấu hình không hợp lệ" -ForegroundColor Red
        foreach ($error in $validation.errors) {
            Write-Host "  - Lỗi: $error" -ForegroundColor Red
        }
    }
    
    if ($validation.warnings.Count -gt 0) {
        Write-Host "⚠ Cảnh báo:" -ForegroundColor Yellow
        foreach ($warning in $validation.warnings) {
            Write-Host "  - $warning" -ForegroundColor Yellow
        }
    }
} catch {
    Write-Host "✗ Lỗi validation: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 3: Preview tác động thay đổi (nếu có hạng với ID 2)
Write-Host "`n3. Xem trước tác động thay đổi hạng Bạc..." -ForegroundColor Yellow
try {
    $impact = Invoke-RestMethod -Uri "$BaseUrl/TierConfiguration/preview-impact/2?newMinSpent=3000000&newMinPoints=300" -Method Get
    Write-Host "✓ Xem trước tác động thành công" -ForegroundColor Green
    Write-Host "- Hạng: $($impact.tierName)" -ForegroundColor White
    Write-Host "- Khách hàng hiện tại: $($impact.impact.currentCustomers)" -ForegroundColor White
    Write-Host "- Sẽ đủ điều kiện mới: $($impact.impact.qualifiedForNew)" -ForegroundColor White
    Write-Host "- Sẽ mất hạng: $($impact.impact.wouldLoseTier)" -ForegroundColor White
    Write-Host "- Thay đổi ròng: $($impact.impact.netChange)" -ForegroundColor Cyan
} catch {
    Write-Host "⚠ Không thể xem trước (có thể chưa có hạng ID=2): $($_.Exception.Message)" -ForegroundColor Yellow
}

# Test 4: Cập nhật cấu hình (chỉ test nếu user xác nhận)
$confirmUpdate = Read-Host "`n4. Bạn có muốn test cập nhật cấu hình không? (y/N)"
if ($confirmUpdate -eq 'y' -or $confirmUpdate -eq 'Y') {
    Write-Host "Đang cập nhật cấu hình mẫu..." -ForegroundColor Yellow
    try {
        # Lấy tiers hiện tại để cập nhật
        $currentSettings = Invoke-RestMethod -Uri "$BaseUrl/TierConfiguration/settings" -Method Get
        
        if ($currentSettings.tiers.Count -gt 0) {
            # Cập nhật hạng đầu tiên với thông số mới
            $firstTier = $currentSettings.tiers[0]
            $firstTier.description = "Hạng cơ bản - Được cập nhật lúc $(Get-Date -Format 'HH:mm:ss')"
            
            $updateResult = Invoke-RestMethod -Uri "$BaseUrl/TierConfiguration/batch-update" -Method Put -ContentType "application/json" -Body (,$currentSettings.tiers | ConvertTo-Json -Depth 10)
            
            Write-Host "✓ Cập nhật thành công: $($updateResult.message)" -ForegroundColor Green
            Write-Host "- Số hạng đã cập nhật: $($updateResult.updatedTiers)" -ForegroundColor White
            
            if ($updateResult.warnings) {
                Write-Host "⚠ Cảnh báo:" -ForegroundColor Yellow
                foreach ($warning in $updateResult.warnings) {
                    Write-Host "  - $warning" -ForegroundColor Yellow
                }
            }
        } else {
            Write-Host "⚠ Không có hạng nào để cập nhật" -ForegroundColor Yellow
        }
    } catch {
        Write-Host "✗ Lỗi cập nhật: $($_.Exception.Message)" -ForegroundColor Red
    }
} else {
    Write-Host "⏭ Bỏ qua test cập nhật" -ForegroundColor Gray
}

# Test 5: Test validation với cấu hình lỗi
Write-Host "`n5. Test validation với cấu hình lỗi..." -ForegroundColor Yellow
$badConfig = @(
    @{
        tierId = 1
        tierName = ""  # Lỗi: tên trống
        minSpent = -1000  # Lỗi: chi tiêu âm
        minPoints = 0
        pointsMultiplier = 15.0  # Lỗi: hệ số quá lớn
        discountPercentage = 150  # Lỗi: giảm giá > 100%
        tierColor = "invalid"  # Lỗi: màu không hợp lệ
        isActive = $true
    },
    @{
        tierId = 2
        tierName = ""  # Lỗi: tên trùng (trống)
        minSpent = 1000000
        minPoints = -10  # Lỗi: điểm âm
        pointsMultiplier = 0.05  # Lỗi: hệ số quá nhỏ
        discountPercentage = 5
        tierColor = "#FF0000"
        isActive = $true
    }
)

try {
    $badValidation = Invoke-RestMethod -Uri "$BaseUrl/TierConfiguration/validate" -Method Post -ContentType "application/json" -Body ($badConfig | ConvertTo-Json -Depth 10)
    
    Write-Host "Test cấu hình lỗi:" -ForegroundColor White
    Write-Host "- Hợp lệ: $($badValidation.isValid)" -ForegroundColor $(if ($badValidation.isValid) { "Red" } else { "Green" })
    Write-Host "- Số lỗi: $($badValidation.errors.Count)" -ForegroundColor Cyan
    
    if ($badValidation.errors.Count -gt 0) {
        Write-Host "Các lỗi được phát hiện:" -ForegroundColor Red
        foreach ($error in $badValidation.errors) {
            Write-Host "  ✗ $error" -ForegroundColor Red
        }
    }
} catch {
    Write-Host "✗ Lỗi test validation: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n=== KẾT THÚC TEST ===" -ForegroundColor Cyan
Write-Host "Hệ thống cấu hình cấp độ khách hàng đã sẵn sàng!" -ForegroundColor Green
Write-Host "`nCác API có thể sử dụng:" -ForegroundColor White
Write-Host "- GET  /api/TierConfiguration/settings (Lấy cấu hình)" -ForegroundColor Gray
Write-Host "- PUT  /api/TierConfiguration/batch-update (Cập nhật hàng loạt)" -ForegroundColor Gray  
Write-Host "- POST /api/TierConfiguration/validate (Kiểm tra hợp lệ)" -ForegroundColor Gray
Write-Host "- GET  /api/TierConfiguration/preview-impact/{id} (Xem trước tác động)" -ForegroundColor Gray
Write-Host "- POST /api/TierConfiguration/reset-defaults (Reset mặc định)" -ForegroundColor Gray