# Script kiểm tra và fix hạng khách hàng
param(
    [string]$BaseUrl = "http://localhost:5273"
)

Write-Host "=== KIỂM TRA VÀ SỬA HẠNG KHÁCH HÀNG ===" -ForegroundColor Green
Write-Host "Base URL: $BaseUrl" -ForegroundColor Yellow

# Thử các port khác nếu 5273 không hoạt động
$ports = @("5273", "5000", "5001", "80", "8080")
$workingUrl = $null

foreach ($port in $ports) {
    $testUrl = if ($port -eq "80") { "http://101.53.9.76" } else { "http://101.53.9.76:$port" }
    try {
        Write-Host "Testing $testUrl..." -ForegroundColor Cyan
        $response = Invoke-RestMethod -Uri "$testUrl/api/LoyaltySettings" -Method GET -TimeoutSec 5
        $workingUrl = $testUrl
        Write-Host "✅ Found working URL: $workingUrl" -ForegroundColor Green
        break
    } catch {
        Write-Host "❌ Failed: $testUrl" -ForegroundColor Red
    }
}

if (-not $workingUrl) {
    # Thử localhost
    foreach ($port in $ports) {
        $testUrl = "http://localhost:$port"
        try {
            Write-Host "Testing $testUrl..." -ForegroundColor Cyan
            $response = Invoke-RestMethod -Uri "$testUrl/api/LoyaltySettings" -Method GET -TimeoutSec 5
            $workingUrl = $testUrl
            Write-Host "✅ Found working URL: $workingUrl" -ForegroundColor Green
            break
        } catch {
            Write-Host "❌ Failed: $testUrl" -ForegroundColor Red
        }
    }
}

if (-not $workingUrl) {
    Write-Host "❌ Không tìm thấy backend server đang chạy!" -ForegroundColor Red
    Write-Host "Hãy chắc chắn backend đang chạy trên một trong các port: 5273, 5000, 5001, 80, 8080" -ForegroundColor Yellow
    exit 1
}

Write-Host ""
Write-Host "=== SỬ DỤNG $workingUrl ===" -ForegroundColor Green

# 1. Lấy danh sách khách hàng
Write-Host "1. Lấy danh sách khách hàng..." -ForegroundColor Cyan
try {
    $customers = Invoke-RestMethod -Uri "$workingUrl/api/Customers" -Method GET
    Write-Host "✅ Có $($customers.Count) khách hàng" -ForegroundColor Green
    
    # Tìm khách hàng có điểm cao
    $highPointCustomers = $customers | Where-Object { $_.loyaltyPoints -gt 2000 } | Sort-Object loyaltyPoints -Descending
    
    Write-Host "Khách hàng có điểm cao:" -ForegroundColor White
    foreach ($customer in $highPointCustomers | Select-Object -First 10) {
        $tierName = if ($customer.customerTier) { $customer.customerTier.tierName } else { "Chưa có hạng" }
        Write-Host "  - $($customer.hoTen): $($customer.loyaltyPoints) điểm, Chi tiêu: $($customer.totalSpent), Hạng: $tierName" -ForegroundColor Gray
    }
    
} catch {
    Write-Host "❌ Lỗi lấy danh sách khách hàng: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""

# 2. Lấy cấu hình tích điểm
Write-Host "2. Kiểm tra cấu hình tích điểm..." -ForegroundColor Cyan
try {
    $loyaltySettings = Invoke-RestMethod -Uri "$workingUrl/api/LoyaltySettings" -Method GET
    Write-Host "✅ Hệ thống tích điểm: $($loyaltySettings.config.isEnabled)" -ForegroundColor Green
    Write-Host "   Các hạng khách hàng:" -ForegroundColor White
    foreach ($tier in $loyaltySettings.tiers | Sort-Object minSpent) {
        Write-Host "     + $($tier.tierName): Chi tiêu >= $($tier.minSpent), Điểm >= $($tier.minPoints)" -ForegroundColor Gray
    }
} catch {
    Write-Host "❌ Lỗi lấy cấu hình: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""

# 3. Cập nhật hạng cho tất cả khách hàng
Write-Host "3. Cập nhật hạng cho tất cả khách hàng..." -ForegroundColor Cyan
try {
    $updateResult = Invoke-RestMethod -Uri "$workingUrl/api/LoyaltySettings/update-all-tiers" -Method POST
    Write-Host "✅ $($updateResult.message)" -ForegroundColor Green
} catch {
    Write-Host "❌ Lỗi cập nhật hạng: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""

# 4. Kiểm tra lại khách hàng sau khi update
Write-Host "4. Kiểm tra lại khách hàng có điểm cao..." -ForegroundColor Cyan
try {
    $customers = Invoke-RestMethod -Uri "$workingUrl/api/Customers" -Method GET
    $highPointCustomers = $customers | Where-Object { $_.loyaltyPoints -gt 2000 } | Sort-Object loyaltyPoints -Descending
    
    Write-Host "Sau khi update:" -ForegroundColor White
    foreach ($customer in $highPointCustomers | Select-Object -First 10) {
        $tierName = if ($customer.customerTier) { $customer.customerTier.tierName } else { "Chưa có hạng" }
        Write-Host "  - $($customer.hoTen): $($customer.loyaltyPoints) điểm, Chi tiêu: $($customer.totalSpent), Hạng: $tierName" -ForegroundColor Gray
        
        # Kiểm tra chi tiết cho khách hàng "Chú hạnh" hoặc khách có điểm > 20000
        if ($customer.hoTen -like "*hanh*" -or $customer.loyaltyPoints -gt 20000) {
            Write-Host ""
            Write-Host "    Chi tiet khach hang $($customer.hoTen):" -ForegroundColor Yellow
            try {
                $customerStatus = Invoke-RestMethod -Uri "$workingUrl/api/LoyaltySettings/customer-status/$($customer.customerId)" -Method GET
                Write-Host "       - Tong chi tieu: $($customerStatus.totalSpent)" -ForegroundColor White
                Write-Host "       - Tong diem: $($customerStatus.totalPoints)" -ForegroundColor White
                if ($customerStatus.currentTier) {
                    Write-Host "       - Hang hien tai: $($customerStatus.currentTier.tierName)" -ForegroundColor Green
                } else {
                    Write-Host "       - Hang hien tai: Chua co" -ForegroundColor Red
                }
                if ($customerStatus.nextTier) {
                    Write-Host "       - Hang tiep theo: $($customerStatus.nextTier.tierName)" -ForegroundColor Yellow
                    Write-Host "       - Can them: $($customerStatus.progress.spentToNext) VND, $($customerStatus.progress.pointsToNext) diem" -ForegroundColor Yellow
                }
            } catch {
                Write-Host "       Loi lay chi tiet: $($_.Exception.Message)" -ForegroundColor Red
            }
            Write-Host ""
        }
    }
} catch {
    Write-Host "Loi kiem tra lai: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "=== HOAN THANH ===" -ForegroundColor Green