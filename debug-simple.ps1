# Debug script đơn giản để kiểm tra PaymentStats API
Write-Host "Kiểm tra API PaymentStats sau restart IIS..." -ForegroundColor Green

$apiBase = "http://101.53.9.76:5273/api"

try {
    $result = Invoke-RestMethod -Uri "$apiBase/PaymentStats" -Method GET -TimeoutSec 15
    
    Write-Host "✅ API hoạt động! Chi tiết:" -ForegroundColor Green
    Write-Host "Tổng doanh thu: $($result.totalRevenue) VNĐ" -ForegroundColor White
    Write-Host "Tổng đơn hàng: $($result.totalOrders)" -ForegroundColor White
    Write-Host "" -ForegroundColor White
    
    Write-Host "=== TẤT CẢ PAYMENT METHODS ===" -ForegroundColor Yellow
    foreach ($method in $result.paymentStats) {
        Write-Host "ID: '$($method.paymentMethodId)' -> Hiển thị: '$($method.paymentMethod)'" -ForegroundColor Cyan
        Write-Host "  Số tiền: $($method.totalAmount) VNĐ - Số đơn: $($method.orderCount)" -ForegroundColor White
        Write-Host "---" -ForegroundColor Gray
    }
    
    # Kiểm tra có USD/EUR riêng không
    $hasUSD = $result.paymentStats | Where-Object { $_.paymentMethodId -like "*USD*" }
    $hasEUR = $result.paymentStats | Where-Object { $_.paymentMethodId -like "*EUR*" }
    
    if ($hasUSD) {
        Write-Host "✅ Tìm thấy USD riêng: $($hasUSD.paymentMethod)" -ForegroundColor Green
    } else {
        Write-Host "❌ CHƯA thấy USD riêng" -ForegroundColor Red
    }
    
    if ($hasEUR) {
        Write-Host "✅ Tìm thấy EUR riêng: $($hasEUR.paymentMethod)" -ForegroundColor Green
    } else {
        Write-Host "❌ CHƯA thấy EUR riêng" -ForegroundColor Red
    }
    
} catch {
    Write-Host "❌ Lỗi API: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`nVấn đề có thể:" -ForegroundColor Yellow
Write-Host "1. Code backend chưa được deploy đúng vào IIS" -ForegroundColor Gray
Write-Host "2. Application Pool chưa restart" -ForegroundColor Gray
Write-Host "3. Dữ liệu trong DB vẫn là 'banktransfer' thay vì 'ngoại tệ'" -ForegroundColor Gray