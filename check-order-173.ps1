# Kiểm tra order ID 173 (Ruby, 350.000₫) - có thể là order #ORD1762765876775
Write-Host "Checking order ID 173 (Ruby's latest order)..." -ForegroundColor Yellow

$orderId = 173
$customerId = 2  # Ruby

try {
    # 1. Get order details
    Write-Host "1. Order details:" -ForegroundColor Cyan
    $order = Invoke-RestMethod -Uri "http://101.53.9.76:5273/api/orders/$orderId" -Method GET
    Write-Host "  Order ID: $($order.orderId)"
    Write-Host "  Order Number: '$($order.orderNumber)'"
    Write-Host "  Customer: $($order.customerName) (ID: $($order.customerId))"
    Write-Host "  Total Amount: $($order.totalAmount)"
    Write-Host "  Status: $($order.status)"
    Write-Host "  Created: $($order.createdAt)"
    
    # 2. Kiểm tra loyalty transactions cho order này
    Write-Host "`n2. Checking loyalty transactions..." -ForegroundColor Cyan
    try {
        $allTransactions = Invoke-RestMethod -Uri "http://101.53.9.76:5273/api/loyaltytransactions" -Method GET
        $orderTransactions = $allTransactions | Where-Object { $_.orderId -eq $orderId }
        
        if ($orderTransactions) {
            Write-Host "Loyalty transactions found for order ${orderId}:" -ForegroundColor Green
            foreach ($trans in $orderTransactions) {
                Write-Host "  Transaction ID: $($trans.transactionId)"
                Write-Host "  Points: $($trans.points)"
                Write-Host "  Type: $($trans.transactionType)"
                Write-Host "  Reason: $($trans.reason)"
                Write-Host "  Date: $($trans.processedAt)"
                Write-Host "  Order ID in transaction: $($trans.orderId)"
            }
        } else {
            Write-Host "❌ NO loyalty transactions found for order ${orderId}!" -ForegroundColor Red
            
            # Check if order qualifies for points
            if ($order.status -eq "completed" -and $order.customerId -eq $customerId) {
                Write-Host "`n3. Order qualifies for points processing. Attempting manual process..." -ForegroundColor Yellow
                try {
                    $processResult = Invoke-RestMethod -Uri "http://101.53.9.76:5273/api/loyaltyprocess/process-order/${orderId}" -Method POST
                    Write-Host "✅ Manual processing result: $($processResult.message)" -ForegroundColor Green
                    
                    # Check again after processing
                    Write-Host "`n4. Checking transactions again after processing..." -ForegroundColor Cyan
                    $allTransactionsAfter = Invoke-RestMethod -Uri "http://101.53.9.76:5273/api/loyaltytransactions" -Method GET
                    $orderTransactionsAfter = $allTransactionsAfter | Where-Object { $_.orderId -eq ${orderId} }
                    
                    if ($orderTransactionsAfter) {
                        Write-Host "✅ NOW FOUND transactions after processing:" -ForegroundColor Green
                        foreach ($trans in $orderTransactionsAfter) {
                            Write-Host "  Points: $($trans.points) | Type: $($trans.transactionType) | Reason: $($trans.reason)"
                        }
                    } else {
                        Write-Host "❌ Still no transactions found after processing!" -ForegroundColor Red
                    }
                } catch {
                    Write-Host "❌ Manual processing failed: $($_.Exception.Message)" -ForegroundColor Red
                }
            } else {
                Write-Host "Order does not qualify for points: Status=$($order.status), CustomerID=$($order.customerId)" -ForegroundColor Yellow
            }
        }
    } catch {
        Write-Host "Error getting transactions: $($_.Exception.Message)" -ForegroundColor Red
    }
    
    # 5. Kiểm tra customer transactions tổng
    Write-Host "`n5. Customer Ruby loyalty summary:" -ForegroundColor Cyan
    try {
        $customerInfo = Invoke-RestMethod -Uri "http://101.53.9.76:5273/api/customers/2" -Method GET
        Write-Host "  Current Points: $($customerInfo.loyaltyPoints)"
        Write-Host "  Total Spent: $($customerInfo.totalSpent)"
        
        # Show recent customer transactions
        $customerTransactions = $allTransactions | Where-Object { $_.customerId -eq 2 } | Sort-Object { [datetime]$_.processedAt } -Descending | Select-Object -First 3
        Write-Host "  Recent transactions:"
        foreach ($trans in $customerTransactions) {
            Write-Host "    Order $($trans.orderId): $($trans.points) points ($($trans.transactionType)) - $($trans.processedAt)"
        }
    } catch {
        Write-Host "Error getting customer info: $($_.Exception.Message)" -ForegroundColor Red
    }
    
} catch {
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`nDiagnosis:" -ForegroundColor Yellow
Write-Host "- Order ID 173 is Ruby's latest order (350,000₫)" -ForegroundColor Cyan
Write-Host "- This might be the missing order #ORD1762765876775 you mentioned" -ForegroundColor Cyan
Write-Host "- If no loyalty transactions found, it means order wasn't processed for points" -ForegroundColor Cyan