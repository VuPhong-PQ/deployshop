# Script deploy lên server production 101.53.9.76
# Sử dụng: .\deploy-to-production.ps1

param(
    [Parameter(Mandatory=$false)]
    [string]$ServerIP = "101.53.9.76",
    
    [Parameter(Mandatory=$false)]
    [int]$Port = 5273,
    
    [Parameter(Mandatory=$false)]
    [string]$Username = "",
    
    [Parameter(Mandatory=$false)]
    [string]$RemotePath = "/var/www/retailpoint-api"
)

$SourcePath = "c:\shop\backend-deploy"

Write-Host "=== DEPLOY TO PRODUCTION SERVER ===" -ForegroundColor Green
Write-Host "Server: $ServerIP`:$Port" -ForegroundColor Yellow
Write-Host "Source: $SourcePath" -ForegroundColor Yellow

# Kiểm tra source path
if (-not (Test-Path "$SourcePath\RetailPointBackend.exe")) {
    Write-Error "Backend executable không tồn tại. Hãy build trước."
    exit 1
}

# Test kết nối
Write-Host "Testing connection..." -ForegroundColor Yellow
try {
    $connection = Test-NetConnection -ComputerName $ServerIP -Port $Port -WarningAction SilentlyContinue
    if ($connection.TcpTestSucceeded) {
        Write-Host "✓ Server đang chạy trên port $Port" -ForegroundColor Green
        Write-Host "Canh bao: Can stop service truoc khi deploy" -ForegroundColor Yellow
    } else {
        Write-Host "✓ Port $Port không có service đang chạy - OK for deploy" -ForegroundColor Green
    }
} catch {
    Write-Host "Canh bao: Khong the test connection: $($_.Exception.Message)" -ForegroundColor Yellow
}

# Hiển thị hướng dẫn deploy
Write-Host ""
Write-Host "=== HUONG DAN DEPLOY ===" -ForegroundColor Cyan
Write-Host ""

if ($Username) {
    Write-Host "1. Deploy qua SSH/SCP:" -ForegroundColor Yellow
    Write-Host "   scp -r `"$SourcePath\*`" $Username@$ServerIP`:$RemotePath/" -ForegroundColor White
    Write-Host ""
    Write-Host "2. SSH vào server và start service:" -ForegroundColor Yellow
    Write-Host "   ssh $Username@$ServerIP" -ForegroundColor White
    Write-Host "   cd $RemotePath" -ForegroundColor White
    Write-Host "   ./RetailPointBackend.exe --urls `"http://0.0.0.0:$Port`"" -ForegroundColor White
} else {
    Write-Host "1. Copy files qua Network Share hoặc Remote Desktop:" -ForegroundColor Yellow
    Write-Host "   - Copy toàn bộ nội dung từ: $SourcePath" -ForegroundColor White
    Write-Host "   - Den server: $ServerIP" -ForegroundColor White
    Write-Host ""
    Write-Host "2. Trên server, chạy lệnh:" -ForegroundColor Yellow
    Write-Host "   cd [duong dan backend]" -ForegroundColor White
    Write-Host "   .\RetailPointBackend.exe --urls `"http://0.0.0.0:$Port`"" -ForegroundColor White
}

Write-Host ""
Write-Host "3. Kiem tra service dang chay:" -ForegroundColor Yellow
Write-Host "   curl http://$ServerIP`:$Port/weatherforecast" -ForegroundColor White
Write-Host ""

Write-Host "=== CURRENT CONFIG ===" -ForegroundColor Cyan
Write-Host "Database Server: $ServerIP" -ForegroundColor White
Write-Host "Backend URL: http://$ServerIP`:$Port" -ForegroundColor White
Write-Host ""

# Tạo quick test script
$TestScript = @"
# Test backend API
`$baseUrl = "http://$ServerIP`:$Port"
Write-Host "Testing backend API..." -ForegroundColor Yellow

try {
    `$response = Invoke-RestMethod -Uri "`$baseUrl/weatherforecast" -Method Get -TimeoutSec 10
    Write-Host "✓ API Response OK" -ForegroundColor Green
    `$response | ConvertTo-Json
} catch {
    Write-Host "✗ API Error: `$(`$_.Exception.Message)" -ForegroundColor Red
}
"@

$TestScript | Out-File -FilePath "c:\shop\test-production-api.ps1" -Encoding UTF8
Write-Host "✓ Created test script: c:\shop\test-production-api.ps1" -ForegroundColor Green