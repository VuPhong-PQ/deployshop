# Test API với server thực
$BaseUrl = "http://101.53.9.76:5273/api"

Write-Host "Testing connection to server: $BaseUrl" -ForegroundColor Cyan

# Test 1: API cũ (CustomerTierManagement)
Write-Host "`nTest 1: CustomerTierManagement API..." -ForegroundColor Yellow
try {
    $oldApi = Invoke-RestMethod -Uri "$BaseUrl/CustomerTierManagement" -Method Get -TimeoutSec 10
    Write-Host "✓ API cũ hoạt động - Số hạng: $($oldApi.Count)" -ForegroundColor Green
    
    if ($oldApi.Count -gt 0) {
        Write-Host "Hạng hiện có:" -ForegroundColor White
        foreach ($tier in $oldApi) {
            Write-Host "  - $($tier.tierName): Chi tiêu >= $('{0:N0}' -f $tier.minSpent) VNĐ" -ForegroundColor Gray
        }
    }
} catch {
    Write-Host "✗ Lỗi API cũ: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Response: $($_.Exception.Response)" -ForegroundColor Red
}

# Test 2: API mới (TierConfiguration)
Write-Host "`nTest 2: TierConfiguration API..." -ForegroundColor Yellow
try {
    $newApi = Invoke-RestMethod -Uri "$BaseUrl/TierConfiguration/settings" -Method Get -TimeoutSec 10
    Write-Host "✓ API mới hoạt động!" -ForegroundColor Green
    Write-Host "- Số hạng: $($newApi.tiers.Count)" -ForegroundColor White
    Write-Host "- Tổng khách hàng: $($newApi.statistics.totalCustomers)" -ForegroundColor White
} catch {
    Write-Host "⚠ API mới chưa có - Cần deploy code mới" -ForegroundColor Yellow
    Write-Host "Lỗi: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 3: Kiểm tra server status
Write-Host "`nTest 3: Server health check..." -ForegroundColor Yellow
try {
    # Test basic connectivity
    $response = Invoke-WebRequest -Uri "http://101.53.9.76:5273" -Method Get -TimeoutSec 5 -UseBasicParsing
    Write-Host "✓ Server đang chạy - Status: $($response.StatusCode)" -ForegroundColor Green
} catch {
    Write-Host "✗ Không thể kết nối server: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n=== KẾT LUẬN ===" -ForegroundColor Cyan
Write-Host "Để sử dụng API mới TierConfiguration, cần:" -ForegroundColor White
Write-Host "1. Build và deploy code backend mới" -ForegroundColor Yellow
Write-Host "2. Restart service" -ForegroundColor Yellow
Write-Host "3. Test lại API TierConfiguration" -ForegroundColor Yellow