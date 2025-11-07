# Script kiem tra he thong Loyalty tren Production
Write-Host "KIEM TRA HE THONG LOYALTY TREN PRODUCTION" -ForegroundColor Green

$API_BASE = "http://localhost:5000/api"

Write-Host "1. Kiem tra Loyalty Settings..." -ForegroundColor Cyan
try {
    $loyaltySettings = Invoke-RestMethod -Uri "$API_BASE/LoyaltySystemSettings/settings" -Method Get
    Write-Host "Loyalty Settings OK:" -ForegroundColor Green
    Write-Host "- Tich diem: $($loyaltySettings.isEnabled)" -ForegroundColor White
    Write-Host "- Doi diem: $($loyaltySettings.isRedemptionEnabled)" -ForegroundColor White
    Write-Host "- Ty le tich: $($loyaltySettings.pointsRate) VND/diem" -ForegroundColor White
} catch {
    Write-Host "Loi Loyalty Settings: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n2. Kiem tra Customer Tiers..." -ForegroundColor Cyan
try {
    $tiers = Invoke-RestMethod -Uri "$API_BASE/CustomerTierManagement" -Method Get
    Write-Host "Customer Tiers OK - Tong: $($tiers.Count) hang" -ForegroundColor Green
    foreach ($tier in $tiers) {
        Write-Host "- $($tier.tierName): Chi tieu >= $([math]::Round($tier.minSpent/1000))K, He so x$($tier.pointsMultiplier), Giam $($tier.discountPercentage)%" -ForegroundColor White
    }
} catch {
    Write-Host "Loi Customer Tiers: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n3. Kiem tra Customers..." -ForegroundColor Cyan
try {
    $customers = Invoke-RestMethod -Uri "$API_BASE/customers" -Method Get
    Write-Host "Customers OK - Tong: $($customers.Count) khach hang" -ForegroundColor Green
    if ($customers.Count -gt 0) {
        $testCustomer = $customers[0]
        Write-Host "Test voi khach hang: $($testCustomer.name)" -ForegroundColor White
    }
} catch {
    Write-Host "Loi Customers: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`nHoan thanh kiem tra!" -ForegroundColor Green
Write-Host "Truy cap: http://localhost:5000" -ForegroundColor Magenta