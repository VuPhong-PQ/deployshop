# Tạo test data USD/EUR qua API thay vì SQL trực tiếp
Write-Host "Tạo test data USD/EUR qua API..." -ForegroundColor Green

$apiBase = "http://101.53.9.76:5273/api"

# Hàm tạo đơn hàng
function Create-TestOrder {
    param(
        [string]$PaymentMethod,
        [string]$Currency,
        [string]$CustomerName,
        [int]$Amount
    )
    
    $orderData = @{
        customerName = $CustomerName
        paymentMethod = $PaymentMethod
        currency = $Currency
        paymentStatus = "paid"
        status = "completed" 
        totalAmount = $Amount
        taxAmount = 0
        items = @(
            @{
                productId = 1
                productName = "Test Product - $Currency"
                quantity = 1
                price = $Amount
                totalPrice = $Amount
            }
        )
    } | ConvertTo-Json -Depth 10
    
    try {
        Write-Host "Tạo đơn hàng: $CustomerName - $PaymentMethod ($Currency) - $Amount VNĐ" -ForegroundColor Yellow
        
        $response = Invoke-RestMethod -Uri "$apiBase/orders" -Method POST -Body $orderData -ContentType "application/json" -TimeoutSec 30
        Write-Host "✅ Thành công: OrderId = $($response.orderId)" -ForegroundColor Green
        return $response
    } catch {
        Write-Host "❌ Lỗi: $($_.Exception.Message)" -ForegroundColor Red
        return $null
    }
}

# Tạo test data
Write-Host "Đang tạo các đơn hàng test..." -ForegroundColor Cyan

$orders = @()
$orders += Create-TestOrder -PaymentMethod "banktransfer" -Currency "USD" -CustomerName "Khách test USD 1" -Amount 720000
$orders += Create-TestOrder -PaymentMethod "banktransfer" -Currency "USD" -CustomerName "Khách test USD 2" -Amount 480000  
$orders += Create-TestOrder -PaymentMethod "banktransfer" -Currency "EUR" -CustomerName "Khách test EUR 1" -Amount 160000
$orders += Create-TestOrder -PaymentMethod "banktransfer" -Currency "EUR" -CustomerName "Khách test EUR 2" -Amount 160000
$orders += Create-TestOrder -PaymentMethod "cash" -Currency $null -CustomerName "Khách test tiền mặt" -Amount 480000

$successCount = ($orders | Where-Object { $_ -ne $null }).Count
Write-Host "Đã tạo thành công $successCount/$($orders.Count) đơn hàng" -ForegroundColor Green

# Test API PaymentStats
Write-Host "`nKiểm tra API PaymentStats..." -ForegroundColor Cyan
try {
    $paymentStats = Invoke-RestMethod -Uri "$apiBase/PaymentStats" -Method GET -TimeoutSec 15
    Write-Host "✅ API PaymentStats hoạt động!" -ForegroundColor Green
    Write-Host "Tổng doanh thu: $($paymentStats.totalRevenue) VNĐ" -ForegroundColor White
    Write-Host "Tổng đơn hàng: $($paymentStats.totalOrders)" -ForegroundColor White
    
    Write-Host "Các phương thức thanh toán:" -ForegroundColor White
    foreach ($method in $paymentStats.paymentStats) {
        Write-Host "  - $($method.paymentMethod) ($($method.paymentMethodId)): $($method.totalAmount) VNĐ - $($method.orderCount) đơn" -ForegroundColor Gray
    }
    
} catch {
    Write-Host "❌ Lỗi khi gọi API PaymentStats: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`nBây giờ hãy kiểm tra báo cáo trong frontend để xem USD và EUR hiển thị riêng biệt!" -ForegroundColor Cyan