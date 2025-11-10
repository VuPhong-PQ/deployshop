# Kiểm tra thông tin customer Ruby và loyalty settings
Write-Host "Checking Ruby customer info and loyalty settings..." -ForegroundColor Yellow

try {
    # 1. Customer info
    Write-Host "1. Customer Ruby info:" -ForegroundColor Cyan
    $customerResponse = Invoke-RestMethod -Uri "http://101.53.9.76:5273/api/customers/2" -Method GET
    Write-Host "  Customer ID: $($customerResponse.customerId)"
    Write-Host "  Name: $($customerResponse.hoTen)"
    Write-Host "  Tier ID: $($customerResponse.tierId)"
    Write-Host "  Hang Khach Hang: $($customerResponse.hangKhachHang)"
    Write-Host "  Loyalty Points: $($customerResponse.loyaltyPoints)"
    Write-Host "  Total Spent: $($customerResponse.totalSpent)"
    Write-Host "  Is Active: $($customerResponse.isActive)"
    
    # 2. Loyalty Settings
    Write-Host "`n2. Loyalty Settings:" -ForegroundColor Cyan
    try {
        $loyaltySettings = Invoke-RestMethod -Uri "http://101.53.9.76:5273/api/loyalty-system-settings" -Method GET
        Write-Host "  Points Enabled: $($loyaltySettings.isPointsEnabled)"
        Write-Host "  Points Rate: $($loyaltySettings.pointsRate)"
        Write-Host "  Min Order Amount: $($loyaltySettings.minOrderAmount)"
        Write-Host "  Redemption Rate: $($loyaltySettings.redemptionRate)"
    } catch {
        Write-Host "  Could not get loyalty settings: $($_.Exception.Message)" -ForegroundColor Yellow
    }
    
    # 3. Tất cả loyalty transactions
    Write-Host "`n3. All loyalty transactions:" -ForegroundColor Cyan
    try {
        $allTransactions = Invoke-RestMethod -Uri "http://101.53.9.76:5273/api/loyaltytransactions" -Method GET
        Write-Host "  Total transactions in system: $($allTransactions.Count)"
        
        # Show Ruby's transactions
        $rubyTransactions = $allTransactions | Where-Object { $_.customerId -eq 2 }
        Write-Host "  Ruby's transactions: $($rubyTransactions.Count)"
        
        if ($rubyTransactions.Count -gt 0) {
            Write-Host "  Recent Ruby transactions:"
            $rubyTransactions | Select-Object -First 5 | ForEach-Object {
                Write-Host "    Order $($_.orderId): $($_.points) points ($($_.transactionType)) - $($_.reason)"
            }
        }
    } catch {
        Write-Host "  Could not get all transactions: $($_.Exception.Message)" -ForegroundColor Yellow
    }
    
    # 4. Thử process tất cả completed orders
    Write-Host "`n4. Processing all completed orders:" -ForegroundColor Cyan
    try {
        $processAll = Invoke-RestMethod -Uri "http://101.53.9.76:5273/api/loyaltyprocess/process-all-completed-orders" -Method POST
        Write-Host "  Success: $($processAll.success)"
        Write-Host "  Message: $($processAll.message)"
        Write-Host "  Processed: $($processAll.processed)"
        Write-Host "  Skipped: $($processAll.skipped)"
    } catch {
        Write-Host "  Error processing all orders: $($_.Exception.Message)" -ForegroundColor Red
    }
    
} catch {
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
}