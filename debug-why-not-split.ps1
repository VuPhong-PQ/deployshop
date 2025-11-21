# Kiểm tra chi tiết tại sao ngoại tệ chưa tách
Write-Host "=== KIỂM TRA CHI TIẾT VẤN ĐỀ ===" -ForegroundColor Red

$apiBase = "http://101.53.9.76:5273/api"

Write-Host "1. Kiểm tra API PaymentStats có hoạt động..." -ForegroundColor Yellow
try {
    $result = Invoke-RestMethod -Uri "$apiBase/PaymentStats" -Method GET -TimeoutSec 15
    Write-Host "✅ API hoạt động" -ForegroundColor Green
    
    # Tìm banktransfer/ngoại tệ method
    $foreignMethod = $result.paymentStats | Where-Object { 
        $_.paymentMethodId -eq "banktransfer" -or 
        $_.paymentMethodId -eq "ngoại tệ" -or 
        $_.paymentMethod -like "*ngoại*" 
    }
    
    if ($foreignMethod) {
        Write-Host "✅ Tìm thấy foreign payment method:" -ForegroundColor Green
        Write-Host "   ID: '$($foreignMethod.paymentMethodId)'" -ForegroundColor White
        Write-Host "   Name: '$($foreignMethod.paymentMethod)'" -ForegroundColor White
        Write-Host "   Orders: $($foreignMethod.orderCount)" -ForegroundColor White
        
        if ($foreignMethod.orders -and $foreignMethod.orders.Count -gt 0) {
            Write-Host "✅ Có orders, kiểm tra Currency:" -ForegroundColor Green
            
            $ordersWithCurrency = 0
            $ordersWithoutCurrency = 0
            
            foreach ($order in $foreignMethod.orders) {
                if ($order.currency -and $order.currency -ne "") {
                    $ordersWithCurrency++
                    Write-Host "   Order #$($order.orderId): Currency = '$($order.currency)'" -ForegroundColor Cyan
                } else {
                    $ordersWithoutCurrency++
                    Write-Host "   Order #$($order.orderId): Currency = NULL/EMPTY" -ForegroundColor Red
                }
            }
            
            Write-Host "" -ForegroundColor White
            Write-Host "📊 PHÂN TÍCH:" -ForegroundColor Yellow
            Write-Host "   Orders có Currency: $ordersWithCurrency" -ForegroundColor $(if ($ordersWithCurrency -gt 0) { "Green" } else { "Red" })
            Write-Host "   Orders không có Currency: $ordersWithoutCurrency" -ForegroundColor $(if ($ordersWithoutCurrency -gt 0) { "Red" } else { "Green" })
            
            if ($ordersWithCurrency -eq 0) {
                Write-Host "" -ForegroundColor White
                Write-Host "❌ VẤN ĐỀ: KHÔNG CÓ ORDERS NÀO CÓ CURRENCY!" -ForegroundColor Red
                Write-Host "🔧 GIẢI PHÁP: Cần update database để thêm Currency cho orders banktransfer" -ForegroundColor Yellow
            }
        } else {
            Write-Host "❌ Không có orders nào" -ForegroundColor Red
        }
    } else {
        Write-Host "❌ Không tìm thấy foreign payment method nào" -ForegroundColor Red
        Write-Host "Các methods hiện có:" -ForegroundColor Yellow
        foreach ($method in $result.paymentStats) {
            Write-Host "   - $($method.paymentMethodId) -> $($method.paymentMethod)" -ForegroundColor Gray
        }
    }
    
} catch {
    Write-Host "❌ API Error: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "" -ForegroundColor White
Write-Host "=== KHẮC PHỤC ===" -ForegroundColor Cyan
Write-Host "Nếu orders không có Currency, chạy SQL sau trong SSMS:" -ForegroundColor White
Write-Host "UPDATE Orders SET Currency = 'USD' WHERE PaymentMethod = 'banktransfer' AND OrderId IN (SELECT TOP 2 OrderId FROM Orders WHERE PaymentMethod = 'banktransfer' ORDER BY CreatedAt DESC);" -ForegroundColor Gray
Write-Host "UPDATE Orders SET Currency = 'EUR' WHERE PaymentMethod = 'banktransfer' AND Currency IS NULL AND OrderId IN (SELECT TOP 2 OrderId FROM Orders WHERE PaymentMethod = 'banktransfer' AND Currency IS NULL ORDER BY CreatedAt DESC);" -ForegroundColor Gray