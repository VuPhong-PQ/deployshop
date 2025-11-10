# Debug loyalty points cho order ID 172 (Ruby customer)
Write-Host "Checking loyalty points for Ruby's latest order (ID: 172)..." -ForegroundColor Yellow

$orderId = 172
$customerId = 2  # Ruby customer ID

try {
    # 1. Kiểm tra order details
    Write-Host "1. Order details:" -ForegroundColor Cyan
    $orderResponse = Invoke-RestMethod -Uri "http://101.53.9.76:5273/api/orders/$orderId" -Method GET
    Write-Host "  Order ID: $($orderResponse.orderId)"
    Write-Host "  Customer: $($orderResponse.customerName) (ID: $($orderResponse.customerId))"
    Write-Host "  Total Amount: $($orderResponse.totalAmount)"
    Write-Host "  Status: $($orderResponse.status)"
    Write-Host "  Created: $($orderResponse.createdAt)"
    
    # 2. Kiểm tra loyalty transactions
    Write-Host "`n2. Checking loyalty transactions..." -ForegroundColor Cyan
    $loyaltyResponse = Invoke-RestMethod -Uri "http://101.53.9.76:5273/api/loyalty-transactions" -Method GET
    $orderTransactions = $loyaltyResponse | Where-Object { $_.orderId -eq $orderId }
    
    if ($orderTransactions) {
        Write-Host "Loyalty transactions found for order ${orderId}:" -ForegroundColor Green
        foreach ($transaction in $orderTransactions) {
            Write-Host "  Points: $($transaction.points) | Type: $($transaction.transactionType) | Reason: $($transaction.reason) | Date: $($transaction.processedAt)"
        }
    } else {
        Write-Host "No loyalty transactions found for order ${orderId}!" -ForegroundColor Red
        
        # 3. Thử manually process points
        Write-Host "`n3. Attempting manual points processing..." -ForegroundColor Cyan
        try {
            $processResponse = Invoke-RestMethod -Uri "http://101.53.9.76:5273/api/loyaltyprocess/process-order/$orderId" -Method POST
            Write-Host "Manual processing result:" -ForegroundColor Green
            Write-Host "  Success: $($processResponse.success)"
            Write-Host "  Message: $($processResponse.message)"
        } catch {
            Write-Host "Manual processing error: $($_.Exception.Message)" -ForegroundColor Red
        }
    }
    
    # 4. Kiểm tra customer loyalty status trước và sau
    Write-Host "`n4. Customer loyalty status:" -ForegroundColor Cyan
    try {
        $loyaltyStatus = Invoke-RestMethod -Uri "http://101.53.9.76:5273/api/loyalty-transactions/customer/$customerId/status" -Method GET
        Write-Host "  Total Points: $($loyaltyStatus.totalPoints)"
        Write-Host "  Total Spent: $($loyaltyStatus.totalSpent)"
        if ($loyaltyStatus.currentTier) {
            Write-Host "  Current Tier: $($loyaltyStatus.currentTier.tierName) (Discount: $($loyaltyStatus.currentTier.discountPercentage)%)"
        }
    } catch {
        Write-Host "Error getting loyalty status: $($_.Exception.Message)" -ForegroundColor Red
    }
    
} catch {
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
}