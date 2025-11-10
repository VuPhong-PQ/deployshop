# Kiểm tra tất cả transactions của Ruby và so sánh với orders
Write-Host "Checking all Ruby's transactions vs orders..." -ForegroundColor Yellow

$customerId = 2  # Ruby

try {
    # 1. Lấy tất cả transactions của Ruby (nhiều pages)
    Write-Host "1. Getting all Ruby's loyalty transactions..." -ForegroundColor Cyan
    $allTransactions = @()
    $page = 1
    $hasMorePages = $true
    
    while ($hasMorePages) {
        try {
            $pageData = Invoke-RestMethod -Uri "http://101.53.9.76:5273/api/LoyaltyTransactions/customer/$customerId?page=$page&pageSize=50" -Method GET
            Write-Host "  Page ${page}: $($pageData.transactions.Count) transactions"
            $allTransactions += $pageData.transactions
            
            if ($page -ge $pageData.totalPages) {
                $hasMorePages = $false
            } else {
                $page++
            }
        } catch {
            Write-Host "  Error on page ${page}: $($_.Exception.Message)" -ForegroundColor Red
            break
        }
    }
    
    Write-Host "✅ Total transactions found: $($allTransactions.Count)" -ForegroundColor Green
    
    # 2. Lấy tất cả orders của Ruby
    Write-Host "`n2. Getting all Ruby's orders..." -ForegroundColor Cyan
    $allOrders = Invoke-RestMethod -Uri "http://101.53.9.76:5273/api/orders" -Method GET
    $rubyOrders = $allOrders | Where-Object { $_.customerId -eq $customerId -and $_.status -eq "completed" }
    Write-Host "✅ Total completed orders: $($rubyOrders.Count)" -ForegroundColor Green
    
    # 3. So sánh orders vs transactions
    Write-Host "`n3. Comparing orders vs transactions..." -ForegroundColor Cyan
    
    Write-Host "Recent Ruby orders:" -ForegroundColor Yellow
    $recentOrders = $rubyOrders | Sort-Object { [datetime]$_.createdAt } -Descending | Select-Object -First 10
    foreach ($order in $recentOrders) {
        $hasTransaction = $allTransactions | Where-Object { $_.orderId -eq $order.orderId }
        $status = if ($hasTransaction) { "✅ HAS TRANSACTION" } else { "❌ MISSING TRANSACTION" }
        $statusColor = if ($hasTransaction) { "Green" } else { "Red" }
        $date = ([datetime]$order.createdAt).ToString("MM-dd HH:mm")
        Write-Host "  Order $($order.orderId) ($($order.orderNumber)) - $($order.totalAmount)₫ - $date - $status" -ForegroundColor $statusColor
    }
    
    # 4. Tìm orders thiếu transactions
    Write-Host "`n4. Orders missing transactions:" -ForegroundColor Red
    $missingOrders = @()
    foreach ($order in $rubyOrders) {
        $hasTransaction = $allTransactions | Where-Object { $_.orderId -eq $order.orderId }
        if (-not $hasTransaction) {
            $missingOrders += $order
        }
    }
    
    Write-Host "Found $($missingOrders.Count) orders without transactions:" -ForegroundColor Red
    foreach ($missing in ($missingOrders | Select-Object -First 5)) {
        $date = ([datetime]$missing.createdAt).ToString("MM-dd HH:mm")
        Write-Host "  Order $($missing.orderId) - $($missing.totalAmount)₫ - $date" -ForegroundColor Red
    }
    
    # 5. Process missing orders
    if ($missingOrders.Count -gt 0) {
        Write-Host "`n5. Processing missing orders..." -ForegroundColor Yellow
        $processedCount = 0
        foreach ($missing in $missingOrders) {
            try {
                Write-Host "  Processing order $($missing.orderId)..." -ForegroundColor Cyan
                $result = Invoke-RestMethod -Uri "http://101.53.9.76:5273/api/loyaltyprocess/process-order/$($missing.orderId)" -Method POST
                if ($result.success) {
                    Write-Host "    ✅ $($result.message)" -ForegroundColor Green
                    $processedCount++
                } else {
                    Write-Host "    ❌ Failed: $($result.message)" -ForegroundColor Red
                }
            } catch {
                Write-Host "    ❌ Error: $($_.Exception.Message)" -ForegroundColor Red
            }
        }
        Write-Host "`n✅ Processed $processedCount out of $($missingOrders.Count) missing orders" -ForegroundColor Green
    }
    
} catch {
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`nSummary:" -ForegroundColor Yellow
Write-Host "- Check if all Ruby's orders now have loyalty transactions" -ForegroundColor Cyan
Write-Host "- Frontend should refresh and show all transactions in loyalty tab" -ForegroundColor Cyan