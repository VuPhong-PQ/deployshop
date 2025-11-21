# VERIFICATION SCRIPT - Run after deployment
Write-Host 'Testing RetailPoint deployment...' -ForegroundColor Yellow

try {
    $backend = Invoke-WebRequest -Uri 'http://101.53.9.76:5273' -UseBasicParsing -TimeoutSec 10
    Write-Host 'Backend: OK' -ForegroundColor Green
} catch {
    Write-Host 'Backend: ERROR' -ForegroundColor Red
}

try {
    $frontend = Invoke-WebRequest -Uri 'http://101.53.9.76' -UseBasicParsing -TimeoutSec 10
    Write-Host 'Frontend: OK' -ForegroundColor Green
} catch {
    Write-Host 'Frontend: ERROR' -ForegroundColor Red
}
