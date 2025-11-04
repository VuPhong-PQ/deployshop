# Complete Deploy Script for Production Server
# Run from: C:\shop

Write-Host "=== DEPLOY BACKEND TO PRODUCTION ===" -ForegroundColor Green
Write-Host "Current Directory: $(Get-Location)" -ForegroundColor Yellow
Write-Host ""

$SourcePath = "C:\shop\backend-deploy"
$ServerIP = "101.53.9.76"
$Port = 5273

# Step 1: Verify we're in correct directory
if ((Get-Location).Path -ne "C:\shop") {
    Write-Error "Please run this script from C:\shop directory"
    Write-Host "Run: cd C:\shop" -ForegroundColor Yellow
    exit 1
}

# Step 2: Check source files
Write-Host "Step 1: Checking backend files..." -ForegroundColor Cyan
if (-not (Test-Path "$SourcePath\RetailPointBackend.exe")) {
    Write-Error "Backend not built. Please run:"
    Write-Host "cd C:\shop\Backend\RetailPointBackend" -ForegroundColor Yellow
    Write-Host "dotnet publish -c Release -o C:\shop\backend-deploy" -ForegroundColor Yellow
    exit 1
}

$exeDate = (Get-Item "$SourcePath\RetailPointBackend.exe").LastWriteTime
Write-Host "✓ Backend files ready (Built: $exeDate)" -ForegroundColor Green

# Step 3: Check if service is stopped
Write-Host ""
Write-Host "Step 2: Checking if service is stopped..." -ForegroundColor Cyan
try {
    $connection = Test-NetConnection -ComputerName $ServerIP -Port $Port -WarningAction SilentlyContinue
    if ($connection.TcpTestSucceeded) {
        Write-Warning "Service still running on $ServerIP`:$Port"
        Write-Host "Please stop the service first!" -ForegroundColor Red
        exit 1
    } else {
        Write-Host "✓ Service stopped - Ready for deploy" -ForegroundColor Green
    }
} catch {
    Write-Host "✓ Cannot connect to service - Assuming it's stopped" -ForegroundColor Green
}

# Step 4: Display copy instructions
Write-Host ""
Write-Host "Step 3: Copy files to server..." -ForegroundColor Cyan
Write-Host "Source folder: $SourcePath" -ForegroundColor White
Write-Host "Target server: $ServerIP" -ForegroundColor White
Write-Host ""
Write-Host "MANUAL COPY REQUIRED:" -ForegroundColor Yellow
Write-Host "1. Open Remote Desktop to $ServerIP" -ForegroundColor White
Write-Host "2. Copy ALL files from: $SourcePath" -ForegroundColor White
Write-Host "3. Paste to server backend folder" -ForegroundColor White
Write-Host "4. Overwrite existing files" -ForegroundColor White

# Step 5: Start service instructions
Write-Host ""
Write-Host "Step 4: Start service on server..." -ForegroundColor Cyan
Write-Host "On server, run in backend folder:" -ForegroundColor White
Write-Host "   .\RetailPointBackend.exe --urls `"http://0.0.0.0:$Port`"" -ForegroundColor Yellow

# Step 6: Test instructions
Write-Host ""
Write-Host "Step 5: Test after deploy..." -ForegroundColor Cyan
Write-Host "After starting service, run from this computer:" -ForegroundColor White
Write-Host "   .\test-api.ps1" -ForegroundColor Yellow

Write-Host ""
Write-Host "=== IMPORTANT NOTES ===" -ForegroundColor Red
Write-Host "1. Must run backend in correct folder on server" -ForegroundColor White
Write-Host "2. Backend needs appsettings.json in same folder" -ForegroundColor White
Write-Host "3. Working directory affects file paths" -ForegroundColor White
Write-Host "4. Check firewall if connection fails" -ForegroundColor White

Write-Host ""
Write-Host "Ready to proceed? Copy files manually then run .\test-api.ps1" -ForegroundColor Green