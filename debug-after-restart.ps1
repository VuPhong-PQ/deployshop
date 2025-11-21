# Debug script để kiểm tra sau khi restart IIS
Write-Host "Debug PaymentStats API sau khi restart IIS..." -ForegroundColor Green

$apiBase = "http://101.53.9.76:5273/api"

# Test API với debug chi tiết
Write-Host "1. Kiểm tra API PaymentStats có hoạt động không..." -ForegroundColor Yellow
try {
    $result = Invoke-RestMethod -Uri "$apiBase/PaymentStats" -Method GET -TimeoutSec 15
    
    Write-Host "✅ API hoạt động! Phân tích chi tiết:" -ForegroundColor Green
    Write-Host "Tổng doanh thu: $($result.totalRevenue) VNĐ" -ForegroundColor White
    Write-Host "Tổng đơn hàng: $($result.totalOrders)" -ForegroundColor White
    Write-Host "" -ForegroundColor White
    
    Write-Host "=== TẤT CẢ PAYMENT METHODS ===" -ForegroundColor Cyan
    foreach ($method in $result.paymentStats) {
        Write-Host "PaymentMethodId: '$($method.paymentMethodId)'" -ForegroundColor Yellow
        Write-Host "PaymentMethod (hiển thị): '$($method.paymentMethod)'" -ForegroundColor White
        Write-Host "TotalAmount: $($method.totalAmount) VNĐ" -ForegroundColor White
        Write-Host "OrderCount: $($method.orderCount)" -ForegroundColor White
        
        # Kiểm tra orders trong method này
        if ($method.orders -and $method.orders.Count -gt 0) {
            Write-Host "Sample orders:" -ForegroundColor Gray
            foreach ($order in $method.orders | Select-Object -First 2) {
                Write-Host "  - Order #$($order.orderId): Customer='$($order.customerName)' Currency='$($order.currency)' Amount=$($order.totalAmount)" -ForegroundColor Gray
            }
        }
        Write-Host "---" -ForegroundColor Gray
    }
    
    # Phân tích cụ thể về ngoại tệ
    Write-Host "=== PHÂN TÍCH NGOẠI TỆ ===" -ForegroundColor Yellow
    
    $ngoaiTeMethods = $result.paymentStats | Where-Object { 
        $_.paymentMethodId -eq "ngoại tệ" -or 
        $_.paymentMethodId -like "*ngoại tệ*" -or 
        $_.paymentMethodId -like "*USD*" -or 
        $_.paymentMethodId -like "*EUR*"
    }
    
    if ($ngoaiTeMethods.Count -gt 0) {
        Write-Host "✅ Tìm thấy $($ngoaiTeMethods.Count) ngoại tệ methods:" -ForegroundColor Green
        foreach ($method in $ngoaiTeMethods) {
            Write-Host "  🌍 ID: '$($method.paymentMethodId)' -> Display: '$($method.paymentMethod)'" -ForegroundColor Cyan
        }
    } else {
        Write-Host "❌ KHÔNG tìm thấy methods ngoại tệ nào" -ForegroundColor Red
        
        # Tìm bất kỳ method nào có "transfer" hoặc currency
        $anyTransfer = $result.paymentStats | Where-Object { 
            $_.paymentMethodId -like "*transfer*" -or 
            $_.paymentMethod -like "*ngoại*" -or
            $_.paymentMethod -like "*USD*" -or
            $_.paymentMethod -like "*EUR*"
        }
        
        if ($anyTransfer.Count -gt 0) {
            Write-Host "⚠️  Tìm thấy methods có liên quan:" -ForegroundColor Yellow
            foreach ($method in $anyTransfer) {
                Write-Host "  📋 ID: '$($method.paymentMethodId)' -> Display: '$($method.paymentMethod)'" -ForegroundColor Yellow
            }
        }
    }
    
} catch {
    Write-Host "❌ Lỗi khi gọi API: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n2. Kiểm tra xem có phải deploy chưa có hiệu lực không..." -ForegroundColor Yellow
Write-Host "Nếu vẫn thấy 'banktransfer' thay vì 'ngoại tệ', có nghĩa deploy chưa có hiệu lực" -ForegroundColor Gray
Write-Host "Hãy kiểm tra IIS Application Pool đã restart chưa" -ForegroundColor Gray