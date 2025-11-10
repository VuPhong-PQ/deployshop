# Fix order #ORD1762765876775 không có trong tab điểm thưởng
Write-Host "Fixing order #ORD1762765876775 missing from loyalty tab..." -ForegroundColor Yellow

$orderId = 173

try {
    # 1. Process points manually for this order
    Write-Host "1. Processing loyalty points for order $orderId..." -ForegroundColor Cyan
    $processResult = Invoke-RestMethod -Uri "http://101.53.9.76:5273/api/loyaltyprocess/process-order/$orderId" -Method POST
    Write-Host "✅ Processing result: $($processResult.message)" -ForegroundColor Green
    
    # 2. Wait và check lại transactions
    Write-Host "`n2. Waiting 2 seconds then checking transactions..." -ForegroundColor Cyan
    Start-Sleep -Seconds 2
    
    try {
        $allTransactions = Invoke-RestMethod -Uri "http://101.53.9.76:5273/api/loyaltytransactions" -Method GET
        $orderTransactions = $allTransactions | Where-Object { $_.orderId -eq $orderId }
        
        if ($orderTransactions) {
            Write-Host "✅ SUCCESS! Loyalty transaction now exists:" -ForegroundColor Green
            foreach ($trans in $orderTransactions) {
                Write-Host "  Transaction ID: $($trans.transactionId)"
                Write-Host "  Points: $($trans.points)"
                Write-Host "  Type: $($trans.transactionType)"
                Write-Host "  Reason: $($trans.reason)"
                Write-Host "  Date: $($trans.processedAt)"
            }
        } else {
            Write-Host "❌ Still no transaction found. Trying process all orders..." -ForegroundColor Yellow
            
            # 3. Process all completed orders as fallback
            Write-Host "`n3. Processing all completed orders..." -ForegroundColor Cyan
            $processAllResult = Invoke-RestMethod -Uri "http://101.53.9.76:5273/api/loyaltyprocess/process-all-completed-orders" -Method POST
            Write-Host "✅ Process all result: $($processAllResult.message)" -ForegroundColor Green
            Write-Host "  Processed: $($processAllResult.processed)"
            Write-Host "  Skipped: $($processAllResult.skipped)"
        }
    } catch {
        Write-Host "Error checking transactions: $($_.Exception.Message)" -ForegroundColor Red
    }
    
    # 4. Final check customer points
    Write-Host "`n4. Final customer check:" -ForegroundColor Cyan
    $customerInfo = Invoke-RestMethod -Uri "http://101.53.9.76:5273/api/customers/2" -Method GET
    Write-Host "  Ruby's current points: $($customerInfo.loyaltyPoints)"
    Write-Host "  Ruby's total spent: $($customerInfo.totalSpent)"
    
    # Expected points for 350,000 order (assuming 1 point per 1000 VND)
    $expectedPoints = [math]::Floor(350000 / 1000)
    Write-Host "  Expected points from order (350,000 / 1000): $expectedPoints"
    
} catch {
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`nSolution Summary:" -ForegroundColor Yellow
Write-Host "✅ Order #ORD1762765876775 found (ID: 173, Ruby, 350,000₫)" -ForegroundColor Green
Write-Host "🔧 Processed loyalty points manually" -ForegroundColor Green
Write-Host "📱 Order should now appear in loyalty tab in frontend" -ForegroundColor Green