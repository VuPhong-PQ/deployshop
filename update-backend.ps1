# Script update backend to production server
# Run this script to deploy latest backend

$SourcePath = "c:\shop\backend-deploy"
$ServerIP = "101.53.9.76"
$Port = 5273

Write-Host "=== UPDATING BACKEND TO PRODUCTION ===" -ForegroundColor Green
Write-Host "Source: $SourcePath" -ForegroundColor Yellow
Write-Host "Target Server: $ServerIP`:$Port" -ForegroundColor Yellow
Write-Host ""

# Check source files
$ExeFile = "$SourcePath\RetailPointBackend.exe"
if (-not (Test-Path $ExeFile)) {
    Write-Error "Backend executable not found: $ExeFile"
    exit 1
}

$BuildTime = (Get-Item $ExeFile).LastWriteTime
Write-Host "✓ Backend ready - Built: $BuildTime" -ForegroundColor Green

# Check if service is running
Write-Host "Checking if service is stopped..." -ForegroundColor Yellow
try {
    $TestResult = Test-NetConnection -ComputerName $ServerIP -Port $Port -WarningAction SilentlyContinue
    if ($TestResult.TcpTestSucceeded) {
        Write-Host "WARNING: Service still running on port $Port" -ForegroundColor Red
        Write-Host "Please stop the service first!" -ForegroundColor Red
        return
    } else {
        Write-Host "OK: Service is stopped - Ready for deployment" -ForegroundColor Green
    }
} catch {
    Write-Host "Cannot test connection - proceeding anyway" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "=== DEPLOYMENT STEPS ===" -ForegroundColor Cyan
Write-Host ""

Write-Host "1. COPY FILES TO SERVER:" -ForegroundColor Yellow
Write-Host "   Use Remote Desktop or file sharing to copy all files from:" -ForegroundColor White
Write-Host "   $SourcePath" -ForegroundColor Cyan
Write-Host "   To the backend folder on server $ServerIP" -ForegroundColor White
Write-Host ""

Write-Host "2. ON SERVER, RUN THESE COMMANDS:" -ForegroundColor Yellow
Write-Host "   cd [backend-folder]" -ForegroundColor White
Write-Host "   .\RetailPointBackend.exe --urls `"http://0.0.0.0:$Port`"" -ForegroundColor Cyan
Write-Host ""

Write-Host "3. VERIFY DEPLOYMENT:" -ForegroundColor Yellow
Write-Host "   Run: .\test-api.ps1" -ForegroundColor Cyan
Write-Host "   Or visit: http://$ServerIP`:$Port/weatherforecast" -ForegroundColor White
Write-Host ""

Write-Host "=== KEY FILES TO COPY ===" -ForegroundColor Yellow
Get-ChildItem $SourcePath | Where-Object { $_.Name -match "\.(exe|dll|json|config)$" } | 
    ForEach-Object { Write-Host "  $($_.Name)" -ForegroundColor White }

Write-Host ""
Write-Host "Press any key after copying files to test the deployment..." -ForegroundColor Green
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")