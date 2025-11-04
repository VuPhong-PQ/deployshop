# Script deploy nhanh cho server local
# Sử dụng: .\quick-deploy.ps1

$SourcePath = "c:\shop\backend-deploy"
$ServerPath = "c:\inetpub\wwwroot\retailpoint-api"  # Thay đổi path này theo server của bạn

Write-Host "=== QUICK DEPLOY BACKEND ===" -ForegroundColor Green

# Kiểm tra source
if (-not (Test-Path "$SourcePath\RetailPointBackend.exe")) {
    Write-Error "Backend chưa được build. Chạy lệnh sau trước:"
    Write-Host "cd c:\shop\Backend\RetailPointBackend" -ForegroundColor Yellow
    Write-Host "dotnet publish -c Release -o c:\shop\backend-deploy" -ForegroundColor Yellow
    exit 1
}

# Stop service nếu đang chạy
$ProcessName = "RetailPointBackend"
$Process = Get-Process -Name $ProcessName -ErrorAction SilentlyContinue
if ($Process) {
    Write-Host "Stopping existing backend process..." -ForegroundColor Yellow
    Stop-Process -Name $ProcessName -Force
    Start-Sleep -Seconds 2
    Write-Host "✓ Process stopped" -ForegroundColor Green
}

# Tạo thư mục server nếu chưa có
if (-not (Test-Path $ServerPath)) {
    New-Item -ItemType Directory -Path $ServerPath -Force | Out-Null
    Write-Host "✓ Created server directory" -ForegroundColor Green
}

# Copy files
Write-Host "Copying files to server..." -ForegroundColor Yellow
try {
    Copy-Item -Path "$SourcePath\*" -Destination $ServerPath -Recurse -Force
    Write-Host "✓ Files copied successfully" -ForegroundColor Green
} catch {
    Write-Error "Failed to copy files: $($_.Exception.Message)"
    exit 1
}

Write-Host ""
Write-Host "=== DEPLOYMENT COMPLETE ===" -ForegroundColor Green
Write-Host "Backend deployed to: $ServerPath" -ForegroundColor Yellow
Write-Host ""
Write-Host "To start the backend:" -ForegroundColor Yellow
Write-Host "cd `"$ServerPath`"" -ForegroundColor Cyan
Write-Host ".\RetailPointBackend.exe" -ForegroundColor Cyan
Write-Host ""
Write-Host "Or run in background:" -ForegroundColor Yellow
Write-Host "Start-Process -FilePath `"$ServerPath\RetailPointBackend.exe`" -WorkingDirectory `"$ServerPath`"" -ForegroundColor Cyan