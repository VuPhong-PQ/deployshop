# Kiểm tra dữ liệu thực tế trong database
Write-Host "Kiểm tra dữ liệu Orders có Currency không..." -ForegroundColor Green

$apiBase = "http://101.53.9.76:5273/api"

# Test trực tiếp bằng cách gọi API với debug detail
try {
    Write-Host "Gọi API với tham số chi tiết..." -ForegroundColor Yellow
    $url = "$apiBase/PaymentStats?fromDate=2024-01-01&toDate=2025-12-31"
    Write-Host "URL: $url" -ForegroundColor Gray
    
    $result = Invoke-RestMethod -Uri $url -Method GET -TimeoutSec 15
    
    Write-Host "=== PHÂN TÍCH CHI TIẾT ===" -ForegroundColor Cyan
    
    foreach ($method in $result.paymentStats) {
        Write-Host "Method ID: '$($method.paymentMethodId)'" -ForegroundColor Yellow
        Write-Host "Method Name: '$($method.paymentMethod)'" -ForegroundColor White
        
        if ($method.orders -and $method.orders.Count -gt 0) {
            Write-Host "  Có $($method.orders.Count) đơn hàng:" -ForegroundColor Gray
            
            foreach ($order in $method.orders | Select-Object -First 3) {
                $currencyText = if ($order.currency) { $order.currency } else { "NULL" }
                Write-Host "    Order #$($order.orderId): Currency='$currencyText', Amount=$($order.totalAmount)" -ForegroundColor Gray
            }
        }
        Write-Host "---" -ForegroundColor Gray
    }
    
    # Tìm orders có currency
    Write-Host "`n=== PHÂN TÍCH CURRENCY ===" -ForegroundColor Yellow
    $allOrders = @()
    foreach ($method in $result.paymentStats) {
        if ($method.orders) {
            $allOrders += $method.orders
        }
    }
    
    $ordersWithCurrency = $allOrders | Where-Object { $_.currency -and $_.currency -ne "" }
    $ordersWithoutCurrency = $allOrders | Where-Object { -not $_.currency -or $_.currency -eq "" }
    
    Write-Host "Đơn hàng CÓ currency: $($ordersWithCurrency.Count)" -ForegroundColor Green
    Write-Host "Đơn hàng KHÔNG CÓ currency: $($ordersWithoutCurrency.Count)" -ForegroundColor Red
    
    if ($ordersWithCurrency.Count -gt 0) {
        Write-Host "`nCác currency có sẵn:" -ForegroundColor Cyan
        $currencies = $ordersWithCurrency | Group-Object currency | Sort-Object Name
        foreach ($curr in $currencies) {
            Write-Host "  - $($curr.Name): $($curr.Count) đơn hàng" -ForegroundColor White
        }
    }
    
    # Kiểm tra banktransfer methods
    $bankTransferOrders = $allOrders | Where-Object { 
        # Tìm trong tất cả methods có payment method là banktransfer
        $orderId = $_.orderId
        foreach ($m in $result.paymentStats) {
            if ($m.paymentMethodId -eq "banktransfer" -and $m.orders) {
                foreach ($o in $m.orders) {
                    if ($o.orderId -eq $orderId) {
                        return $true
                    }
                }
            }
        }
        return $false
    }
    
    Write-Host "`n=== PHÂN TÍCH BANKTRANSFER ===" -ForegroundColor Yellow
    Write-Host "Số đơn hàng banktransfer: $($bankTransferOrders.Count)" -ForegroundColor White
    
    if ($bankTransferOrders.Count -gt 0) {
        Write-Host "Chi tiết banktransfer orders:" -ForegroundColor Gray
        foreach ($order in $bankTransferOrders | Select-Object -First 5) {
            $curr = if ($order.currency) { $order.currency } else { "NONE" }
            Write-Host "  Order #$($order.orderId): Currency='$curr'" -ForegroundColor Gray
        }
    }
    
} catch {
    Write-Host "❌ Lỗi: $($_.Exception.Message)" -ForegroundColor Red
}