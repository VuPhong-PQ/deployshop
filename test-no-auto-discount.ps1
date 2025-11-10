# Test script để kiểm tra không có discount tự động
# Tạo order cho customer Ruby và kiểm tra

Write-Host "Testing no automatic discount for tier customers..." -ForegroundColor Yellow

# 1. Get Ruby customer info  
$customerResponse = Invoke-RestMethod -Uri "http://101.53.9.76:5273/api/customers" -Method GET
$rubyCustomer = $customerResponse | Where-Object { $_.customerId -eq 2 } | Select-Object -First 1

if (-not $rubyCustomer) {
    Write-Host "No Ruby/VIP customer found!" -ForegroundColor Red
    exit 1
}

Write-Host "Using customer: $($rubyCustomer.hoTen) (ID: $($rubyCustomer.customerId)) - Tier: $($rubyCustomer.hangKhachHang)" -ForegroundColor Green

# 2. Create order without any discount selected
$orderData = @{
    customerId = $rubyCustomer.customerId
    subtotal = "170000"
    taxAmount = "0"  
    discountAmount = "0"  # NO discount applied
    total = "170000"
    paymentMethod = "cash"
    paymentStatus = "paid"
    status = "completed"  # This will trigger loyalty processing
    orderNumber = "TEST$(Get-Date -Format 'yyyyMMddHHmmss')"
    items = @(
        @{
            productId = 1
            quantity = 1
            unitPrice = 170000
            totalPrice = 170000
            productName = "Test Product"
        }
    )
}

Write-Host "Creating order with NO discount..." -ForegroundColor Cyan
$createResponse = Invoke-RestMethod -Uri "http://101.53.9.76:5273/api/orders" -Method POST -Body ($orderData | ConvertTo-Json) -ContentType "application/json"
$orderId = $createResponse.orderId

Write-Host "Order created with ID: $orderId" -ForegroundColor Green

# 3. Wait a moment for background processing
Start-Sleep -Seconds 3

# 4. Check order details to see if discount was automatically applied
$orderResponse = Invoke-RestMethod -Uri "http://101.53.9.76:5273/api/orders/$orderId" -Method GET

Write-Host "`nOrder Details:" -ForegroundColor Yellow
Write-Host "Order ID: $($orderResponse.orderId)"
Write-Host "SubTotal: $($orderResponse.subTotal)"
Write-Host "DiscountAmount: $($orderResponse.discountAmount)"
Write-Host "TotalAmount: $($orderResponse.totalAmount)"
Write-Host "Status: $($orderResponse.status)"

# 5. Verify no automatic discount was applied
if ($orderResponse.discountAmount -gt 0) {
    Write-Host "`nFAILED: Automatic discount was applied! DiscountAmount: $($orderResponse.discountAmount)" -ForegroundColor Red
    Write-Host "Expected: 0, Actual: $($orderResponse.discountAmount)" -ForegroundColor Red
} else {
    Write-Host "`nPASSED: No automatic discount applied as expected!" -ForegroundColor Green
}

# 6. Check if total matches expected (should be 170000)
if ($orderResponse.totalAmount -ne 170000) {
    Write-Host "FAILED: Total amount mismatch! Expected: 170000, Actual: $($orderResponse.totalAmount)" -ForegroundColor Red
} else {
    Write-Host "PASSED: Total amount correct (170000)" -ForegroundColor Green
}

Write-Host "`nTest completed." -ForegroundColor Yellow