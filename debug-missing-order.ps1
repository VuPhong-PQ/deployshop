# Debug order #ORD1762765876775 không hiện trong tab điểm thưởng
Write-Host "Debugging order #ORD1762765876775 not showing in loyalty tab..." -ForegroundColor Yellow

$orderNumber = "ORD1762765876775"

try {
    # 1. Tìm order theo number
    Write-Host "1. Finding order by number..." -ForegroundColor Cyan
    $allOrders = Invoke-RestMethod -Uri "http://101.53.9.76:5273/api/orders" -Method GET
    $order = $allOrders | Where-Object { $_.orderNumber -eq $orderNumber }
    
    if ($order) {
        Write-Host "Order found:" -ForegroundColor Green
        Write-Host "  Order ID: $($order.orderId)"
        Write-Host "  Customer: $($order.customerName) (ID: $($order.customerId))"
        Write-Host "  Total Amount: $($order.totalAmount)"
        Write-Host "  Status: $($order.status)"
        Write-Host "  Created: $($order.createdAt)"
        
        $orderId = $order.orderId
        $customerId = $order.customerId
    } else {
        # Tìm theo pattern gần đúng
        Write-Host "Order not found with exact number. Searching similar..." -ForegroundColor Yellow
        $similarOrders = $allOrders | Where-Object { $_.orderNumber -like "*$orderNumber*" -or $orderNumber -like "*$($_.orderNumber)*" }
        
        if ($similarOrders) {
            Write-Host "Similar orders found:" -ForegroundColor Yellow
            foreach ($sim in $similarOrders) {
                Write-Host "  Order: $($sim.orderNumber) | ID: $($sim.orderId) | Customer: $($sim.customerName)"
            }
        }
        
        # Lấy orders gần đây nhất để so sánh
        Write-Host "`nRecent orders for reference:" -ForegroundColor Cyan
        $recentOrders = $allOrders | Sort-Object { [datetime]$_.createdAt } -Descending | Select-Object -First 5
        foreach ($recent in $recentOrders) {
            Write-Host "  Order: $($recent.orderNumber) | ID: $($recent.orderId) | Customer: $($recent.customerName) | Amount: $($recent.totalAmount)"
        }
        return
    }
    
    # 2. Kiểm tra loyalty transactions cho order này
    Write-Host "`n2. Checking loyalty transactions for this order..." -ForegroundColor Cyan
    try {
        # Lấy tất cả transactions
        $allTransactions = Invoke-RestMethod -Uri "http://101.53.9.76:5273/api/loyaltytransactions" -Method GET
        $orderTransactions = $allTransactions | Where-Object { $_.orderId -eq $orderId }
        
        if ($orderTransactions) {
            Write-Host "Loyalty transactions found:" -ForegroundColor Green
            foreach ($trans in $orderTransactions) {
                Write-Host "  Transaction ID: $($trans.transactionId)"
                Write-Host "  Points: $($trans.points)"
                Write-Host "  Type: $($trans.transactionType)"
                Write-Host "  Reason: $($trans.reason)"
                Write-Host "  Date: $($trans.processedAt)"
            }
        } else {
            Write-Host "No loyalty transactions found for this order!" -ForegroundColor Red
            
            # Thử process manually
            Write-Host "Attempting manual processing..." -ForegroundColor Yellow
            try {
                $processResult = Invoke-RestMethod -Uri "http://101.53.9.76:5273/api/loyaltyprocess/process-order/$orderId" -Method POST
                Write-Host "Manual processing result: $($processResult.message)" -ForegroundColor Green
            } catch {
                Write-Host "Manual processing failed: $($_.Exception.Message)" -ForegroundColor Red
            }
        }
    } catch {
        Write-Host "Error getting transactions: $($_.Exception.Message)" -ForegroundColor Red
    }
    
    # 3. Kiểm tra customer transactions để xem có thiếu không
    if ($customerId) {
        Write-Host "`n3. Checking all customer transactions..." -ForegroundColor Cyan
        try {
            $customerTransactions = $allTransactions | Where-Object { $_.customerId -eq $customerId }
            Write-Host "Customer has $($customerTransactions.Count) total transactions"
            
            # Show recent transactions
            $recentTransactions = $customerTransactions | Sort-Object { [datetime]$_.processedAt } -Descending | Select-Object -First 5
            Write-Host "Recent customer transactions:"
            foreach ($trans in $recentTransactions) {
                Write-Host "  Order $($trans.orderId): $($trans.points) points ($($trans.transactionType)) - $($trans.processedAt)"
            }
        } catch {
            Write-Host "Error getting customer transactions: $($_.Exception.Message)" -ForegroundColor Red
        }
    }
    
} catch {
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
}