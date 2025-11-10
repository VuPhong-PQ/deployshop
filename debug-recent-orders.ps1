# Debug để xem recent orders
Write-Host "Getting recent orders to find the correct order..." -ForegroundColor Yellow

try {
    $orderResponse = Invoke-RestMethod -Uri "http://101.53.9.76:5273/api/orders" -Method GET
    
    Write-Host "Found $($orderResponse.Count) orders. Showing recent ones:" -ForegroundColor Green
    
    # Lấy 10 orders gần nhất
    $recentOrders = $orderResponse | Sort-Object { [datetime]$_.createdAt } -Descending | Select-Object -First 10
    
    foreach ($order in $recentOrders) {
        $createdDate = if ($order.createdAt) { ([datetime]$order.createdAt).ToString("yyyy-MM-dd HH:mm") } else { "N/A" }
        Write-Host "  OrderNumber: $($order.orderNumber) | ID: $($order.orderId) | Customer: $($order.customerName) | Amount: $($order.totalAmount) | Status: $($order.status) | Date: $createdDate" -ForegroundColor Cyan
    }
    
    # Tìm orders có Ruby customer
    Write-Host "`nOrders for Ruby customer:" -ForegroundColor Yellow
    $rubyOrders = $orderResponse | Where-Object { $_.customerName -like "*Ruby*" -or $_.customerName -like "*ruby*" }
    foreach ($order in $rubyOrders) {
        $createdDate = if ($order.createdAt) { ([datetime]$order.createdAt).ToString("yyyy-MM-dd HH:mm") } else { "N/A" }
        Write-Host "  OrderNumber: $($order.orderNumber) | ID: $($order.orderId) | Customer: $($order.customerName) | Amount: $($order.totalAmount) | Status: $($order.status) | Date: $createdDate" -ForegroundColor Green
    }
    
} catch {
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
}