# Script tạo dữ liệu test cho discount system
$baseUrl = "http://localhost:5271"

Write-Host "Đang tạo dữ liệu test cho discount system..." -ForegroundColor Green

# 1. Tạo một đơn hàng test
$orderData = @{
    CustomerName = "Khách hàng test"
    PaymentMethod = "cash"
    PaymentStatus = "paid"
    Status = "completed"
    SubTotal = 500000
    TaxAmount = 0
    TotalAmount = 450000
    DiscountAmount = 50000
    OrderItems = @(
        @{
            ProductName = "Sản phẩm test 1"
            Price = 300000
            Quantity = 1
            TotalPrice = 300000
            FinalPrice = 270000
            DiscountAmount = 30000
        },
        @{
            ProductName = "Sản phẩm test 2"  
            Price = 200000
            Quantity = 1
            TotalPrice = 200000
            FinalPrice = 180000
            DiscountAmount = 20000
        }
    )
} | ConvertTo-Json -Depth 3

try {
    $response = Invoke-RestMethod -Uri "$baseUrl/api/Orders" -Method Post -Body $orderData -ContentType "application/json"
    $orderId = $response.orderId
    Write-Host "Đã tạo đơn hàng ID: $orderId" -ForegroundColor Yellow
    
    # 2. Tạo discount cho đơn hàng vừa tạo
    $discountData = @{
        OrderId = $orderId
        DiscountType = "PercentageTotal"
        DiscountValue = 10
        DiscountAmount = 50000
        DiscountName = "Giảm giá 10% tổng bill"
        AppliedBy = "admin"
    } | ConvertTo-Json
    
    $discountResponse = Invoke-RestMethod -Uri "$baseUrl/api/OrderDiscounts" -Method Post -Body $discountData -ContentType "application/json"
    Write-Host "Đã tạo discount ID: $($discountResponse.orderDiscountId)" -ForegroundColor Yellow
    
    Write-Host "Hoàn thành tạo dữ liệu test!" -ForegroundColor Green
    
} catch {
    Write-Host "Lỗi: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Chi tiết: $($_.Exception.Response)" -ForegroundColor Red
}