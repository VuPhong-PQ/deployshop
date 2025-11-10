# Debug script để kiểm tra tích điểm cho order cụ thể
Write-Host "Checking loyalty points for order #ORD1762764531499..." -ForegroundColor Yellow

$orderNumber = "ORD1762764531499"

try {
    # 1. Lấy thông tin order
    Write-Host "1. Getting order details..." -ForegroundColor Cyan
    $orderResponse = Invoke-RestMethod -Uri "http://101.53.9.76:5273/api/orders" -Method GET
    $order = $orderResponse | Where-Object { $_.orderNumber -eq $orderNumber } | Select-Object -First 1
    
    if (-not $order) {
        Write-Host "Order not found!" -ForegroundColor Red
        exit 1
    }
    
    Write-Host "Order found:" -ForegroundColor Green
    Write-Host "  Order ID: $($order.orderId)"
    Write-Host "  Customer ID: $($order.customerId)"
    Write-Host "  Customer Name: $($order.customerName)"
    Write-Host "  Total Amount: $($order.totalAmount)"
    Write-Host "  Status: $($order.status)"
    Write-Host "  Created: $($order.createdAt)"
    
    # 2. Kiểm tra loyalty transactions cho order này
    Write-Host "`n2. Checking loyalty transactions for this order..." -ForegroundColor Cyan
    $loyaltyResponse = Invoke-RestMethod -Uri "http://101.53.9.76:5273/api/loyalty-transactions" -Method GET
    $orderTransactions = $loyaltyResponse | Where-Object { $_.orderId -eq $order.orderId }
    
    if ($orderTransactions) {
        Write-Host "Loyalty transactions found:" -ForegroundColor Green
        foreach ($transaction in $orderTransactions) {
            Write-Host "  Transaction ID: $($transaction.transactionId)"
            Write-Host "  Customer ID: $($transaction.customerId)"
            Write-Host "  Points: $($transaction.points)"
            Write-Host "  Type: $($transaction.transactionType)"
            Write-Host "  Reason: $($transaction.reason)"
            Write-Host "  Date: $($transaction.processedAt)"
        }
    } else {
        Write-Host "No loyalty transactions found for this order!" -ForegroundColor Red
    }
    
    # 3. Kiểm tra thông tin loyalty tổng của customer
    if ($order.customerId) {
        Write-Host "`n3. Checking customer loyalty status..." -ForegroundColor Cyan
        $loyaltyStatus = Invoke-RestMethod -Uri "http://101.53.9.76:5273/api/loyalty-transactions/customer/$($order.customerId)/status" -Method GET
        
        Write-Host "Customer loyalty status:" -ForegroundColor Green
        Write-Host "  Total Points: $($loyaltyStatus.totalPoints)"
        Write-Host "  Total Spent: $($loyaltyStatus.totalSpent)"
        Write-Host "  Current Tier: $($loyaltyStatus.currentTier.tierName)"
    }
    
    # 4. Thử manually process points cho order này
    Write-Host "`n4. Attempting to manually process points for this order..." -ForegroundColor Cyan
    try {
        $processResponse = Invoke-RestMethod -Uri "http://101.53.9.76:5273/api/loyaltyprocess/process-order/$($order.orderId)" -Method POST
        Write-Host "Manual processing result:" -ForegroundColor Green
        Write-Host "  Success: $($processResponse.success)"
        Write-Host "  Message: $($processResponse.message)"
    } catch {
        Write-Host "Manual processing failed: $($_.Exception.Message)" -ForegroundColor Red
    }
    
} catch {
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
}