# Tìm API endpoints có sẵn và test loyalty
Write-Host "Testing different loyalty API endpoints..." -ForegroundColor Yellow

$orderId = 172
$customerId = 2

# Test các endpoints có thể
$endpoints = @(
    "http://101.53.9.76:5273/api/loyalty-transactions/customer/$customerId",
    "http://101.53.9.76:5273/api/loyaltytransactions/customer/$customerId", 
    "http://101.53.9.76:5273/api/loyaltytransactions",
    "http://101.53.9.76:5273/api/loyalty/customer/$customerId",
    "http://101.53.9.76:5273/api/loyalty/transactions/customer/$customerId"
)

foreach ($endpoint in $endpoints) {
    try {
        Write-Host "`nTrying: $endpoint" -ForegroundColor Cyan
        $response = Invoke-RestMethod -Uri $endpoint -Method GET
        Write-Host "SUCCESS! Response:" -ForegroundColor Green
        if ($response.Count -gt 0) {
            Write-Host "Found $($response.Count) records"
            # Show recent transactions
            $recent = $response | Select-Object -First 3
            foreach ($item in $recent) {
                Write-Host "  $($item | ConvertTo-Json -Compress)"
            }
        } else {
            Write-Host "Empty response or no transactions"
        }
        break
    } catch {
        Write-Host "Failed: $($_.Exception.Message)" -ForegroundColor Red
    }
}

# Test manual processing
Write-Host "`nTrying manual points processing..." -ForegroundColor Cyan
try {
    $processResponse = Invoke-RestMethod -Uri "http://101.53.9.76:5273/api/loyaltyprocess/process-order/$orderId" -Method POST
    Write-Host "Manual processing result:" -ForegroundColor Green
    Write-Host "  Success: $($processResponse.success)"
    Write-Host "  Message: $($processResponse.message)"
} catch {
    Write-Host "Manual processing failed: $($_.Exception.Message)" -ForegroundColor Red
}