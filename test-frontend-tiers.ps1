# Test frontend customer tier display
Write-Host "=== TESTING FRONTEND CUSTOMER TIER DISPLAY ===" -ForegroundColor Green

# Khách hàng test case
$testCases = @(
    @{ Name = "Chu hanh"; ExpectedTier = "Platinum"; ExpectedDisplay = "Vang" },
    @{ Name = "Ruby"; ExpectedTier = "Bronze"; ExpectedDisplay = "Dong" }
)

Write-Host "Expected mappings:" -ForegroundColor Yellow
Write-Host "  Bronze -> Dong (Đồng)" -ForegroundColor White
Write-Host "  Silver -> Bac (Bạc)" -ForegroundColor White
Write-Host "  Platinum -> Vang (Vàng)" -ForegroundColor White
Write-Host "  VIP -> Kim cuong (Kim cương)" -ForegroundColor White

Write-Host ""
Write-Host "Checking backend data:" -ForegroundColor Cyan

try {
    $customers = Invoke-RestMethod -Uri "http://101.53.9.76:5273/api/Customers" -Method GET
    
    foreach ($testCase in $testCases) {
        $customer = $customers | Where-Object { $_.hoTen -like "*$($testCase.Name)*" }
        
        if ($customer) {
            Write-Host "✅ $($customer.hoTen):" -ForegroundColor Green
            Write-Host "   Backend hangKhachHang: $($customer.hangKhachHang)" -ForegroundColor White
            Write-Host "   TierId: $($customer.tierId)" -ForegroundColor White
            Write-Host "   Loyalty Points: $($customer.loyaltyPoints)" -ForegroundColor White
            
            # Check if matches expected
            if ($customer.hangKhachHang -eq $testCase.ExpectedTier) {
                Write-Host "   ✅ Backend tier đúng!" -ForegroundColor Green
            } else {
                Write-Host "   ❌ Backend tier sai! Expected: $($testCase.ExpectedTier)" -ForegroundColor Red
            }
        } else {
            Write-Host "❌ Không tìm thấy khách hàng: $($testCase.Name)" -ForegroundColor Red
        }
        Write-Host ""
    }
    
    Write-Host "All customers with tiers:" -ForegroundColor Cyan
    foreach ($customer in $customers | Sort-Object loyaltyPoints -Descending) {
        $tierDisplay = switch ($customer.hangKhachHang) {
            "Bronze" { "Đồng" }
            "Silver" { "Bạc" }
            "Platinum" { "Vàng" }
            "VIP" { "Kim cương" }
            default { $customer.hangKhachHang }
        }
        
        Write-Host "  $($customer.hoTen): $($customer.hangKhachHang) -> $tierDisplay" -ForegroundColor White
    }
    
} catch {
    Write-Host "❌ Lỗi kết nối backend: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "HƯỚNG DẪN KIỂM TRA FRONTEND:" -ForegroundColor Yellow
Write-Host "1. Mở trang customers trong browser" -ForegroundColor White
Write-Host "2. Kiểm tra khách hàng 'Chú hạnh' hiển thị hạng 'Vàng'" -ForegroundColor White
Write-Host "3. Kiểm tra dropdown filter có các tùy chọn:" -ForegroundColor White
Write-Host "   - Thường, Premium, Vàng, Kim cương" -ForegroundColor Gray
Write-Host "4. Kiểm tra form thêm/sửa có đúng tùy chọn hạng" -ForegroundColor White
Write-Host ""
Write-Host "Nếu không hiển thị đúng:" -ForegroundColor Red
Write-Host "1. Hard refresh (Ctrl+F5)" -ForegroundColor White
Write-Host "2. Clear browser cache" -ForegroundColor White
Write-Host "3. Kiểm tra console browser có lỗi không" -ForegroundColor White