# Tạo test data với "ngoại tệ" payment method
Write-Host "Tạo test data với phương thức 'ngoại tệ'..." -ForegroundColor Green

$apiBase = "http://101.53.9.76:5273/api"

# Hàm tạo đơn hàng với "ngoại tệ"
function Create-ForeignCurrencyOrder {
    param(
        [string]$Currency,
        [string]$CustomerName,
        [int]$Amount
    )
    
    $orderData = @{
        customerName = $CustomerName
        paymentMethod = "ngoại tệ"  # Sử dụng "ngoại tệ" thay vì "banktransfer"
        currency = $Currency
        paymentStatus = "paid"
        status = "completed" 
        totalAmount = $Amount
        taxAmount = 0
        items = @(
            @{
                productId = 1
                productName = "Test Product $Currency"
                quantity = 1
                price = $Amount
                totalPrice = $Amount
            }
        )
    } | ConvertTo-Json -Depth 10
    
    try {
        Write-Host "Tạo đơn hàng ngoại tệ: $CustomerName - $Currency - $Amount VNĐ" -ForegroundColor Yellow
        
        $response = Invoke-RestMethod -Uri "$apiBase/orders" -Method POST -Body $orderData -ContentType "application/json" -TimeoutSec 30
        Write-Host "✅ Thành công: OrderId = $($response.orderId)" -ForegroundColor Green
        return $response
    } catch {
        Write-Host "❌ Lỗi: $($_.Exception.Message)" -ForegroundColor Red
        Write-Host "Response: $($_.Exception.Response)" -ForegroundColor Red
        return $null
    }
}

# Tạo test data với "ngoại tệ"
Write-Host "Đang tạo các đơn hàng test với 'ngoại tệ'..." -ForegroundColor Cyan

$orders = @()
$orders += Create-ForeignCurrencyOrder -Currency "USD" -CustomerName "Khách test ngoại tệ USD 1" -Amount 720000
$orders += Create-ForeignCurrencyOrder -Currency "USD" -CustomerName "Khách test ngoại tệ USD 2" -Amount 480000  
$orders += Create-ForeignCurrencyOrder -Currency "EUR" -CustomerName "Khách test ngoại tệ EUR 1" -Amount 160000
$orders += Create-ForeignCurrencyOrder -Currency "EUR" -CustomerName "Khách test ngoại tệ EUR 2" -Amount 160000

$successCount = ($orders | Where-Object { $_ -ne $null }).Count
Write-Host "Đã tạo thành công $successCount/$($orders.Count) đơn hàng ngoại tệ" -ForegroundColor Green

# Delay để đảm bảo data đã được lưu
Start-Sleep -Seconds 2

# Test API PaymentStats với dữ liệu mới
Write-Host "`nKiểm tra API PaymentStats với dữ liệu mới..." -ForegroundColor Cyan
try {
    $paymentStats = Invoke-RestMethod -Uri "$apiBase/PaymentStats" -Method GET -TimeoutSec 15
    Write-Host "✅ API PaymentStats hoạt động!" -ForegroundColor Green
    Write-Host "Tổng doanh thu: $($paymentStats.totalRevenue) VNĐ" -ForegroundColor White
    Write-Host "Tổng đơn hàng: $($paymentStats.totalOrders)" -ForegroundColor White
    
    Write-Host "Các phương thức thanh toán:" -ForegroundColor White
    foreach ($method in $paymentStats.paymentStats) {
        Write-Host "  - $($method.paymentMethod) ($($method.paymentMethodId)): $($method.totalAmount) VNĐ - $($method.orderCount) đơn" -ForegroundColor Gray
    }
    
    # Kiểm tra có tách USD/EUR riêng không
    $foreignCurrencyMethods = $paymentStats.paymentStats | Where-Object { $_.paymentMethodId -like "*ngoại tệ*" -or $_.paymentMethodId -like "*USD*" -or $_.paymentMethodId -like "*EUR*" }
    
    if ($foreignCurrencyMethods.Count -gt 0) {
        Write-Host "`n✅ Phát hiện các phương thức ngoại tệ:" -ForegroundColor Green
        foreach ($method in $foreignCurrencyMethods) {
            Write-Host "  🌍 $($method.paymentMethod) (ID: $($method.paymentMethodId))" -ForegroundColor Cyan
        }
    } else {
        Write-Host "`n❌ Chưa phát hiện USD/EUR riêng biệt" -ForegroundColor Red
    }
    
} catch {
    Write-Host "❌ Lỗi khi gọi API PaymentStats: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`nBây giờ hãy build + deploy và kiểm tra báo cáo trong frontend!" -ForegroundColor Cyan