# Deploy Backend to Production Server
param(
    [string]$ServerIP = "101.53.9.76",
    [int]$Port = 5273
)

$SourcePath = "c:\shop\backend-deploy"

Write-Host "=== DEPLOY TO PRODUCTION SERVER ===" -ForegroundColor Green
Write-Host "Server: $ServerIP`:$Port" -ForegroundColor Yellow
Write-Host "Source: $SourcePath" -ForegroundColor Yellow
Write-Host ""

# Check source files
if (-not (Test-Path "$SourcePath\RetailPointBackend.exe")) {
    Write-Error "Backend executable not found. Please build first."
    exit 1
}

Write-Host "OK - Backend files ready for deploy" -ForegroundColor Green
Write-Host ""

# Test connection
Write-Host "Testing connection to $ServerIP`:$Port..." -ForegroundColor Yellow
try {
    $result = Test-NetConnection -ComputerName $ServerIP -Port $Port -WarningAction SilentlyContinue
    if ($result.TcpTestSucceeded) {
        Write-Host "WARNING: Service is running on port $Port - Stop it before deploy" -ForegroundColor Red
    } else {
        Write-Host "OK - Port $Port is available for deployment" -ForegroundColor Green
    }
} catch {
    Write-Host "Cannot test connection: $($_.Exception.Message)" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "=== DEPLOYMENT INSTRUCTIONS ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "1. Copy files to server:" -ForegroundColor Yellow
Write-Host "   Source: $SourcePath" -ForegroundColor White
Write-Host "   Target: Server $ServerIP" -ForegroundColor White
Write-Host ""
Write-Host "2. On server, run:" -ForegroundColor Yellow
Write-Host "   cd [backend-folder]" -ForegroundColor White
Write-Host "   .\RetailPointBackend.exe --urls `"http://0.0.0.0:$Port`"" -ForegroundColor White
Write-Host ""
Write-Host "3. Test API:" -ForegroundColor Yellow
Write-Host "   http://$ServerIP`:$Port/weatherforecast" -ForegroundColor White
Write-Host ""

# Create test script
$TestContent = @"
# Test Production API
`$url = "http://$ServerIP`:$Port/weatherforecast"
Write-Host "Testing: `$url" -ForegroundColor Yellow
try {
    `$response = Invoke-RestMethod -Uri `$url -TimeoutSec 10
    Write-Host "SUCCESS: API is working" -ForegroundColor Green
    `$response
} catch {
    Write-Host "ERROR: `$(`$_.Exception.Message)" -ForegroundColor Red
}
"@

$TestContent | Out-File -FilePath "c:\shop\test-api.ps1" -Encoding UTF8
Write-Host "Created test script: c:\shop\test-api.ps1" -ForegroundColor Green