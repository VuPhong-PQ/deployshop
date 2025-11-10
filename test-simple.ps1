$BaseUrl = "http://101.53.9.76:5273/api"

Write-Host "Testing server connection..." -ForegroundColor Cyan

Write-Host "Test 1: Old API (CustomerTierManagement)..." -ForegroundColor Yellow
try {
    $oldApi = Invoke-RestMethod -Uri "$BaseUrl/CustomerTierManagement" -Method Get -TimeoutSec 10
    Write-Host "SUCCESS: Old API works - Tiers count: $($oldApi.Count)" -ForegroundColor Green
} catch {
    Write-Host "ERROR: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "Test 2: New API (TierConfiguration)..." -ForegroundColor Yellow
try {
    $newApi = Invoke-RestMethod -Uri "$BaseUrl/TierConfiguration/settings" -Method Get -TimeoutSec 10
    Write-Host "SUCCESS: New API works!" -ForegroundColor Green
    Write-Host "- Tiers: $($newApi.tiers.Count)" -ForegroundColor White
} catch {
    Write-Host "WARNING: New API not deployed yet" -ForegroundColor Yellow
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "CONCLUSION: Need to deploy new backend code to use TierConfiguration API" -ForegroundColor Cyan