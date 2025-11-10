$BaseUrl = "http://101.53.9.76:5273/api"

Write-Host "Testing Preview Impact API..." -ForegroundColor Green

# Get tiers first
$settings = Invoke-RestMethod -Uri "$BaseUrl/TierConfiguration/settings" -Method Get
$firstTier = $settings.tiers[0]

Write-Host "Testing with tier: $($firstTier.tierName) (ID: $($firstTier.tierId))" -ForegroundColor Cyan

# Test preview impact
$testUrl = "$BaseUrl/TierConfiguration/preview-impact/$($firstTier.tierId)?newMinSpent=1000000&newMinPoints=100"
Write-Host "URL: $testUrl" -ForegroundColor Gray

try {
    $impact = Invoke-RestMethod -Uri $testUrl -Method Get
    Write-Host "SUCCESS: Preview API works!" -ForegroundColor Green
    Write-Host "- Tier: $($impact.tierName)" -ForegroundColor White
    Write-Host "- Current customers: $($impact.impact.currentCustomers)" -ForegroundColor White
    Write-Host "- Would qualify: $($impact.impact.qualifiedForNew)" -ForegroundColor Green
    Write-Host "- Would lose tier: $($impact.impact.wouldLoseTier)" -ForegroundColor Red
    Write-Host "- Net change: $($impact.impact.netChange)" -ForegroundColor Cyan
} catch {
    Write-Host "ERROR: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "Preview Impact API test completed!" -ForegroundColor Green