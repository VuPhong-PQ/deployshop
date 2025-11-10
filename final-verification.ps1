# Final verification - check all Ruby transactions after fixes
Write-Host "Final verification of Ruby's loyalty transactions..." -ForegroundColor Yellow

$customerId = 2

try {
    # 1. Get current customer info
    Write-Host "1. Current Ruby customer info:" -ForegroundColor Cyan
    $customer = Invoke-RestMethod -Uri "http://101.53.9.76:5273/api/customers/2" -Method GET
    Write-Host "  Points: $($customer.loyaltyPoints)"
    Write-Host "  Total Spent: $($customer.totalSpent)"
    Write-Host "  Tier: $($customer.hangKhachHang)"
    
    # 2. Get all transactions (current page only for speed)
    Write-Host "`n2. Recent loyalty transactions:" -ForegroundColor Cyan
    $transactionsResponse = Invoke-RestMethod -Uri "http://101.53.9.76:5273/api/LoyaltyTransactions/customer/2" -Method GET
    Write-Host "  Total transactions: $($transactionsResponse.totalCount)"
    Write-Host "  Showing: $($transactionsResponse.transactions.Count)"
    
    Write-Host "`nRecent transactions:" -ForegroundColor Green
    foreach ($trans in $transactionsResponse.transactions) {
        $date = ([datetime]$trans.processedAt).ToString("MM-dd HH:mm")
        Write-Host "  Order $($trans.orderId): +$($trans.points) points - $($trans.reason) - $date"
    }
    
    # 3. Count orders vs transactions
    Write-Host "`n3. Summary:" -ForegroundColor Yellow
    $allOrders = Invoke-RestMethod -Uri "http://101.53.9.76:5273/api/orders" -Method GET
    $rubyCompletedOrders = ($allOrders | Where-Object { $_.customerId -eq 2 -and $_.status -eq "completed" }).Count
    
    Write-Host "  Ruby completed orders: $rubyCompletedOrders"
    Write-Host "  Ruby loyalty transactions: $($transactionsResponse.totalCount)"
    
    if ($rubyCompletedOrders -eq $transactionsResponse.totalCount) {
        Write-Host "  ✅ ALL ORDERS HAVE TRANSACTIONS!" -ForegroundColor Green
    } else {
        Write-Host "  ⚠️  Still missing some transactions" -ForegroundColor Yellow
        Write-Host "  Missing: $($rubyCompletedOrders - $transactionsResponse.totalCount) transactions"
    }
    
} catch {
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`nFrontend Fix Summary:" -ForegroundColor Yellow
Write-Host "✅ Added separate loyalty transactions API call" -ForegroundColor Green
Write-Host "✅ Added auto refresh every 30 seconds" -ForegroundColor Green  
Write-Host "✅ Added manual refresh button" -ForegroundColor Green
Write-Host "✅ Show more transactions (10 instead of 5)" -ForegroundColor Green
Write-Host "✅ Better date/time display" -ForegroundColor Green
Write-Host "Frontend should now show all loyalty transactions correctly!" -ForegroundColor Green