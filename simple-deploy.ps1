# Simple Deploy Script
Write-Host "=== BACKEND DEPLOY CHECKLIST ===" -ForegroundColor Green
Write-Host ""

# Check current directory
Write-Host "Current Directory: $(Get-Location)" -ForegroundColor Yellow
if ((Get-Location).Path -ne "C:\shop") {
    Write-Warning "Should run from C:\shop directory"
}

# Check backend files
$SourcePath = "C:\shop\backend-deploy"
if (Test-Path "$SourcePath\RetailPointBackend.exe") {
    $buildDate = (Get-Item "$SourcePath\RetailPointBackend.exe").LastWriteTime
    Write-Host "✓ Backend files ready (Built: $buildDate)" -ForegroundColor Green
} else {
    Write-Host "✗ Backend files not found" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "=== DEPLOYMENT STEPS ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "1. COPY FILES TO SERVER:" -ForegroundColor Yellow
Write-Host "   Source: $SourcePath" -ForegroundColor White
Write-Host "   Target: Server 101.53.9.76" -ForegroundColor White
Write-Host ""
Write-Host "2. ON SERVER, START SERVICE:" -ForegroundColor Yellow
Write-Host "   cd [backend-folder]" -ForegroundColor White
Write-Host "   .\RetailPointBackend.exe --urls http://0.0.0.0:5273" -ForegroundColor White
Write-Host ""
Write-Host "3. TEST FROM HERE:" -ForegroundColor Yellow
Write-Host "   .\test-api.ps1" -ForegroundColor White
Write-Host ""
Write-Host "=== IMPORTANT ===" -ForegroundColor Red
Write-Host "- Backend must run from folder containing appsettings.json" -ForegroundColor White
Write-Host "- Working directory affects database connections" -ForegroundColor White
Write-Host "- Stop any existing service before copying files" -ForegroundColor White