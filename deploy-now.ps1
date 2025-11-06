# Simple Deploy Script - Copy files to server
$sourcePath = "C:\shop\backend-deploy"
$serverPath = "\\101.53.9.76\backend-share"  # Network share path

Write-Host "=== DEPLOYING BACKEND ===" -ForegroundColor Green
Write-Host "Source: $sourcePath" -ForegroundColor Yellow
Write-Host "Target: $serverPath" -ForegroundColor Yellow

# Check if source exists
if (-not (Test-Path $sourcePath)) {
    Write-Error "Source path not found: $sourcePath"
    exit 1
}

# Create target if not exists (for local test)
$localTarget = "C:\inetpub\wwwroot\retailpoint-backend"
if (-not (Test-Path $localTarget)) {
    Write-Host "Creating local target: $localTarget" -ForegroundColor Yellow
    New-Item -ItemType Directory -Path $localTarget -Force | Out-Null
}

# Copy files locally first (for demo)
Write-Host "Copying files..." -ForegroundColor Yellow
try {
    Copy-Item -Path "$sourcePath\*" -Destination $localTarget -Recurse -Force
    Write-Host "✓ Files copied successfully" -ForegroundColor Green
} catch {
    Write-Error "Failed to copy files: $($_.Exception.Message)"
    exit 1
}

# Verify copy
$exeTarget = Join-Path $localTarget "RetailPointBackend.exe"
if (Test-Path $exeTarget) {
    $fileInfo = Get-Item $exeTarget
    Write-Host "✓ Verified: RetailPointBackend.exe ($($fileInfo.Length) bytes)" -ForegroundColor Green
} else {
    Write-Error "Verification failed: exe not found"
    exit 1
}

Write-Host ""
Write-Host "=== DEPLOYMENT COMPLETE ===" -ForegroundColor Green
Write-Host "Backend deployed to: $localTarget" -ForegroundColor Yellow
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "1. Stop current service on 101.53.9.76:5273" -ForegroundColor White
Write-Host "2. Copy files from $localTarget to server" -ForegroundColor White  
Write-Host "3. Start service on server" -ForegroundColor White
Write-Host "4. Test with: .\test-api.ps1" -ForegroundColor White