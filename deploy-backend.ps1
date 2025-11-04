# Script deploy backend lên server
# Sử dụng: .\deploy-backend.ps1 -ServerPath "\\server\share\path" hoặc scp cho Linux server

param(
    [Parameter(Mandatory=$true)]
    [string]$ServerPath,
    
    [Parameter(Mandatory=$false)]
    [string]$ServiceName = "RetailPointBackend",
    
    [Parameter(Mandatory=$false)]
    [switch]$UseSSH,
    
    [Parameter(Mandatory=$false)]
    [string]$SSHUser,
    
    [Parameter(Mandatory=$false)]
    [string]$SSHHost
)

$SourcePath = "c:\shop\backend-deploy"
$BackupPath = "$SourcePath\backup-$(Get-Date -Format 'yyyyMMdd-HHmmss')"

Write-Host "=== DEPLOY RETAIL POINT BACKEND ===" -ForegroundColor Green
Write-Host "Source: $SourcePath" -ForegroundColor Yellow
Write-Host "Destination: $ServerPath" -ForegroundColor Yellow

# Kiểm tra source path
if (-not (Test-Path $SourcePath)) {
    Write-Error "Source path không tồn tại: $SourcePath"
    exit 1
}

# Kiểm tra file exe
$ExeFile = Join-Path $SourcePath "RetailPointBackend.exe"
if (-not (Test-Path $ExeFile)) {
    Write-Error "File executable không tồn tại: $ExeFile"
    exit 1
}

Write-Host "✓ Kiểm tra source files - OK" -ForegroundColor Green

# Tạo backup nếu cần
Write-Host "Tạo backup..." -ForegroundColor Yellow
try {
    New-Item -ItemType Directory -Path $BackupPath -Force | Out-Null
    Write-Host "✓ Tạo backup folder - OK" -ForegroundColor Green
} catch {
    Write-Warning "Không thể tạo backup folder: $($_.Exception.Message)"
}

if ($UseSSH) {
    # Deploy qua SSH (Linux server)
    if (-not $SSHUser -or -not $SSHHost) {
        Write-Error "Cần cung cấp SSHUser và SSHHost khi sử dụng SSH"
        exit 1
    }
    
    Write-Host "Deploying qua SSH..." -ForegroundColor Yellow
    
    # Tạo thư mục trên server
    ssh $SSHUser@$SSHHost "mkdir -p $ServerPath"
    
    # Copy files
    scp -r "$SourcePath/*" "$SSHUser@${SSHHost}:$ServerPath/"
    
    # Set permissions
    ssh $SSHUser@$SSHHost "chmod +x $ServerPath/RetailPointBackend.exe"
    
    Write-Host "✓ Deploy qua SSH - Complete" -ForegroundColor Green
    
} else {
    # Deploy qua network share hoặc local path
    Write-Host "Deploying qua file system..." -ForegroundColor Yellow
    
    # Tạo thư mục đích nếu chưa có
    if (-not (Test-Path $ServerPath)) {
        try {
            New-Item -ItemType Directory -Path $ServerPath -Force | Out-Null
            Write-Host "✓ Tạo destination folder - OK" -ForegroundColor Green
        } catch {
            Write-Error "Không thể tạo destination folder: $($_.Exception.Message)"
            exit 1
        }
    }
    
    # Copy files
    try {
        Write-Host "Copying files..." -ForegroundColor Yellow
        Copy-Item -Path "$SourcePath\*" -Destination $ServerPath -Recurse -Force
        Write-Host "✓ Copy files - Complete" -ForegroundColor Green
    } catch {
        Write-Error "Lỗi khi copy files: $($_.Exception.Message)"
        exit 1
    }
}

# Kiểm tra file đã copy
$DestExe = Join-Path $ServerPath "RetailPointBackend.exe"
if (Test-Path $DestExe) {
    $SourceSize = (Get-Item $ExeFile).Length
    $DestSize = (Get-Item $DestExe).Length
    
    if ($SourceSize -eq $DestSize) {
        Write-Host "✓ Verification - OK (Size: $SourceSize bytes)" -ForegroundColor Green
    } else {
        Write-Warning "File size khác nhau - Source: $SourceSize, Dest: $DestSize"
    }
} else {
    Write-Error "File executable không tồn tại ở destination"
    exit 1
}

Write-Host ""
Write-Host "=== DEPLOYMENT SUCCESSFUL ===" -ForegroundColor Green
Write-Host "Backend đã được deploy thành công!" -ForegroundColor Green
Write-Host ""
Write-Host "Các bước tiếp theo:" -ForegroundColor Yellow
Write-Host "1. Restart service trên server (nếu có)" -ForegroundColor White
Write-Host "2. Kiểm tra log để đảm bảo service chạy OK" -ForegroundColor White
Write-Host "3. Test API endpoints" -ForegroundColor White
Write-Host ""

# Hiển thị lệnh để start service
Write-Host "Để start backend trên server:" -ForegroundColor Yellow
if ($UseSSH) {
    Write-Host "ssh $SSHUser@$SSHHost 'cd $ServerPath && ./RetailPointBackend.exe'" -ForegroundColor Cyan
} else {
    Write-Host "cd `"$ServerPath`" && .\RetailPointBackend.exe" -ForegroundColor Cyan
}