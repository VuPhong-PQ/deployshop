# Test Production API
$url = "http://101.53.9.76:5273/weatherforecast"
Write-Host "Testing: $url" -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri $url -TimeoutSec 10
    Write-Host "SUCCESS: API is working" -ForegroundColor Green
    $response
} catch {
    Write-Host "ERROR: $($_.Exception.Message)" -ForegroundColor Red
}
