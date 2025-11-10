# Debug script để xem customer có sẵn
Write-Host "Getting all customers to debug..." -ForegroundColor Yellow

try {
    $customerResponse = Invoke-RestMethod -Uri "http://101.53.9.76:5273/api/customers" -Method GET
    Write-Host "Found $($customerResponse.Count) customers:" -ForegroundColor Green
    
    foreach ($customer in $customerResponse) {
        Write-Host "ID: $($customer.customerId) - Name: $($customer.hoTen) - HangKhachHang: $($customer.hangKhachHang) - TierId: $($customer.tierId)" -ForegroundColor Cyan
    }
} catch {
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
}