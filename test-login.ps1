# Test Login API Script
Write-Host "Testing Login API..." -ForegroundColor Yellow

try {
    $body = @{
        username = "admin"
        password = "vuphong"
    } | ConvertTo-Json

    $headers = @{
        'Content-Type' = 'application/json'
        'Origin' = 'http://101.53.9.76'
    }

    Write-Host "Sending request to: http://101.53.9.76:5273/api/staff/login" -ForegroundColor Cyan
    Write-Host "Body: $body" -ForegroundColor Gray

    $response = Invoke-RestMethod -Uri 'http://101.53.9.76:5273/api/staff/login' -Method POST -Body $body -Headers $headers

    Write-Host "✅ Login Successful!" -ForegroundColor Green
    Write-Host "Response:" -ForegroundColor Green
    $response | ConvertTo-Json -Depth 3 | Write-Host

} catch {
    Write-Host "❌ Login Failed!" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    
    if ($_.Exception.Response) {
        Write-Host "Status Code: $($_.Exception.Response.StatusCode)" -ForegroundColor Red
    }
}

Write-Host "Test completed. Press any key to continue..." -ForegroundColor Yellow
Read-Host