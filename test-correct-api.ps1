# Test với đúng API endpoint cho LoyaltyTransactions
Write-Host "Testing correct LoyaltyTransactions API endpoint..." -ForegroundColor Yellow

$customerId = 2  # Ruby
$orderId = 173

try {
    # 1. Test đúng endpoint
    Write-Host "1. Testing correct endpoint: /api/LoyaltyTransactions/customer/$customerId" -ForegroundColor Cyan
    $transactions = Invoke-RestMethod -Uri "http://101.53.9.76:5273/api/LoyaltyTransactions/customer/$customerId" -Method GET
    
    Write-Host "✅ SUCCESS! Found transactions for Ruby:" -ForegroundColor Green
    Write-Host "  Total transactions: $($transactions.totalCount)"
    Write-Host "  Current page transactions: $($transactions.transactions.Count)"
    
    # 2. Tìm transaction cho order 173
    Write-Host "`n2. Looking for order 173 transactions..." -ForegroundColor Cyan
    $order173Transactions = $transactions.transactions | Where-Object { $_.orderId -eq $orderId }
    
    if ($order173Transactions) {
        Write-Host "✅ FOUND transaction for order 173!" -ForegroundColor Green
        foreach ($trans in $order173Transactions) {
            Write-Host "  Transaction ID: $($trans.transactionId)"
            Write-Host "  Points: $($trans.points)"
            Write-Host "  Type: $($trans.transactionType)"
            Write-Host "  Reason: $($trans.reason)"
            Write-Host "  Date: $($trans.processedAt)"
        }
    } else {
        Write-Host "❌ No transaction found for order 173 in current page" -ForegroundColor Red
        Write-Host "Showing recent transactions to debug:" -ForegroundColor Yellow
        
        foreach ($trans in ($transactions.transactions | Select-Object -First 5)) {
            Write-Host "  Order $($trans.orderId): $($trans.points) points - $($trans.reason) ($($trans.processedAt))"
        }
        
        # Check if there are more pages
        if ($transactions.totalPages -gt 1) {
            Write-Host "`n3. Checking other pages (total pages: $($transactions.totalPages))..." -ForegroundColor Cyan
            for ($page = 2; $page -le [Math]::Min($transactions.totalPages, 3); $page++) {
                Write-Host "  Checking page $page..." -ForegroundColor Yellow
                $pageTransactions = Invoke-RestMethod -Uri "http://101.53.9.76:5273/api/LoyaltyTransactions/customer/$customerId?page=$page" -Method GET
                $pageOrder173 = $pageTransactions.transactions | Where-Object { $_.orderId -eq $orderId }
                
                if ($pageOrder173) {
                    Write-Host "  ✅ FOUND on page $page!" -ForegroundColor Green
                    foreach ($trans in $pageOrder173) {
                        Write-Host "    Points: $($trans.points) | Reason: $($trans.reason) | Date: $($trans.processedAt)"
                    }
                    break
                }
            }
        }
    }
    
} catch {
    Write-Host "❌ Error: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`nConclusion:" -ForegroundColor Yellow
Write-Host "- Used correct API: /api/LoyaltyTransactions/customer/{customerId}" -ForegroundColor Cyan  
Write-Host "- This should show transactions in frontend loyalty tab" -ForegroundColor Cyan
Write-Host "- If order 173 transaction found, it means points were processed correctly" -ForegroundColor Cyan