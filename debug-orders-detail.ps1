# Tạo API test để xem dữ liệu thô từ database
Write-Host "Kiểm tra dữ liệu orders với currency..." -ForegroundColor Green

$apiBase = "http://101.53.9.76:5273/api"

try {
    # Gọi API để lấy chi tiết orders
    $result = Invoke-RestMethod -Uri "$apiBase/PaymentStats" -Method GET -TimeoutSec 15
    
    Write-Host "=== PHÂN TÍCH CHI TIẾT ORDERS ===" -ForegroundColor Yellow
    
    foreach ($method in $result.paymentStats) {
        if ($method.paymentMethodId -eq "banktransfer") {
            Write-Host "BANKTRANSFER METHOD DETAILS:" -ForegroundColor Cyan
            Write-Host "PaymentMethodId: $($method.paymentMethodId)" -ForegroundColor White
            Write-Host "PaymentMethod: $($method.paymentMethod)" -ForegroundColor White
            Write-Host "Orders count: $($method.orders.Count)" -ForegroundColor White
            
            if ($method.orders -and $method.orders.Count -gt 0) {
                Write-Host "Sample orders:" -ForegroundColor Gray
                foreach ($order in $method.orders | Select-Object -First 5) {
                    $curr = if ($order.currency) { "'$($order.currency)'" } else { "NULL" }
                    Write-Host "  Order #$($order.orderId): Currency=$curr, Amount=$($order.totalAmount)" -ForegroundColor Gray
                }
            }
            break
        }
    }
    
    # Kiểm tra xem có method nào khác với USD/EUR không
    $usdMethods = $result.paymentStats | Where-Object { $_.paymentMethodId -like "*USD*" }
    $eurMethods = $result.paymentStats | Where-Object { $_.paymentMethodId -like "*EUR*" }
    
    Write-Host "`n=== KẾT QUẢ KIỂM TRA ===" -ForegroundColor Yellow
    Write-Host "USD methods: $($usdMethods.Count)" -ForegroundColor $(if ($usdMethods.Count -gt 0) { "Green" } else { "Red" })
    Write-Host "EUR methods: $($eurMethods.Count)" -ForegroundColor $(if ($eurMethods.Count -gt 0) { "Green" } else { "Red" })
    
    if ($usdMethods.Count -eq 0 -and $eurMethods.Count -eq 0) {
        Write-Host "`nVẤN ĐỀ CÓ THỂ LÀ:" -ForegroundColor Red
        Write-Host "1. Dữ liệu banktransfer không có Currency trong database" -ForegroundColor Yellow
        Write-Host "2. Backend chưa được deploy với code mới" -ForegroundColor Yellow
        Write-Host "3. SQL script chưa được chạy" -ForegroundColor Yellow
    }
    
} catch {
    Write-Host "ERROR: $($_.Exception.Message)" -ForegroundColor Red
}