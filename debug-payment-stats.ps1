# Kiểm tra chi tiết dữ liệu trong database
Write-Host "Kiểm tra chi tiết dữ liệu Orders..." -ForegroundColor Green

$apiBase = "http://101.53.9.76:5273/api"

# Tạo một API endpoint để debug dữ liệu
$testPaymentStatsUrl = "$apiBase/PaymentStats?fromDate=2024-01-01&toDate=2025-12-31"
Write-Host "URL: $testPaymentStatsUrl" -ForegroundColor Gray

try {
    $result = Invoke-RestMethod -Uri $testPaymentStatsUrl -Method GET -TimeoutSec 15
    
    Write-Host "=== KẾT QUẢ API PAYMENSTATS ===" -ForegroundColor Cyan
    Write-Host "Từ ngày: $($result.fromDate)" -ForegroundColor White
    Write-Host "Đến ngày: $($result.toDate)" -ForegroundColor White  
    Write-Host "Tổng doanh thu: $($result.totalRevenue) VNĐ" -ForegroundColor White
    Write-Host "Tổng đơn hàng: $($result.totalOrders)" -ForegroundColor White
    Write-Host "" -ForegroundColor White
    
    Write-Host "CHI TIẾT CÁC PHƯƠNG THỨC THANH TOÁN:" -ForegroundColor Yellow
    foreach ($method in $result.paymentStats) {
        Write-Host "PaymentMethodId: '$($method.paymentMethodId)'" -ForegroundColor Cyan
        Write-Host "PaymentMethod: '$($method.paymentMethod)'" -ForegroundColor White
        Write-Host "TotalAmount: $($method.totalAmount) VNĐ" -ForegroundColor White
        Write-Host "OrderCount: $($method.orderCount)" -ForegroundColor White
        Write-Host "Percentage: $($method.percentage)%" -ForegroundColor White
        
        if ($method.orders -and $method.orders.Count -gt 0) {
            Write-Host "  Orders:" -ForegroundColor Gray
            foreach ($order in $method.orders | Select-Object -First 3) {
                Write-Host "    - Order #$($order.orderId): $($order.customerName) - $($order.totalAmount) VNĐ - Currency: '$($order.currency)'" -ForegroundColor Gray
            }
        }
        Write-Host "---" -ForegroundColor Gray
    }
    
    Write-Host "=== PHÂN TÍCH ===" -ForegroundColor Yellow
    $bankTransferMethods = $result.paymentStats | Where-Object { $_.paymentMethodId -like "banktransfer*" }
    
    if ($bankTransferMethods.Count -eq 0) {
        Write-Host "❌ KHÔNG CÓ banktransfer methods nào!" -ForegroundColor Red
    } else {
        Write-Host "✅ Tìm thấy $($bankTransferMethods.Count) banktransfer methods:" -ForegroundColor Green
        foreach ($method in $bankTransferMethods) {
            Write-Host "  - $($method.paymentMethodId): $($method.paymentMethod)" -ForegroundColor White
        }
    }
    
    # Kiểm tra có USD/EUR riêng không
    $usdMethod = $result.paymentStats | Where-Object { $_.paymentMethodId -eq "banktransfer_USD" }
    $eurMethod = $result.paymentStats | Where-Object { $_.paymentMethodId -eq "banktransfer_EUR" }
    
    if ($usdMethod) {
        Write-Host "✅ Tìm thấy USD riêng: $($usdMethod.paymentMethod)" -ForegroundColor Green
    } else {
        Write-Host "❌ KHÔNG tìm thấy banktransfer_USD" -ForegroundColor Red
    }
    
    if ($eurMethod) {
        Write-Host "✅ Tìm thấy EUR riêng: $($eurMethod.paymentMethod)" -ForegroundColor Green  
    } else {
        Write-Host "❌ KHÔNG tìm thấy banktransfer_EUR" -ForegroundColor Red
    }
    
} catch {
    Write-Host "❌ Lỗi khi gọi API: $($_.Exception.Message)" -ForegroundColor Red
}