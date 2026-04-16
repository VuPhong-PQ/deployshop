<#
.SYNOPSIS
    Script giám sát sức khỏe backend và SQL Server, tự động restart nếu cần.
    Thiết kế để chạy như Scheduled Task định kỳ (mỗi 5 phút).

.DESCRIPTION
    Script này sẽ:
    1. Kiểm tra backend có phản hồi hay không (health check)
    2. Kiểm tra kết nối SQL Server
    3. Nếu có vấn đề, ghi log và restart service/SQL nếu cần
    4. Gửi thông báo (tùy chọn) qua email hoặc webhook

.EXAMPLE
    # Chạy kiểm tra cơ bản
    .\health-monitor.ps1

    # Chạy với tự động restart
    .\health-monitor.ps1 -AutoRestart

    # Chạy với custom URL
    .\health-monitor.ps1 -HealthUrl 'http://101.53.9.76:5273/weatherforecast' -AutoRestart

.NOTES
    Để cài đặt như Scheduled Task, xem README hoặc chạy install-scheduled-task.ps1
#>

param(
    [string]$HealthUrl = 'http://127.0.0.1:5273/weatherforecast',
    [string]$AppSettingsPath,
    [string]$ServiceName = 'RetailPointBackend',
    [switch]$AutoRestart,
    [switch]$AutoRestartSql,
    [string]$LogPath = 'C:\shop\logs\health-monitor.log',
    [int]$TimeoutSeconds = 10
)

$ErrorActionPreference = 'Continue'

# Tạo thư mục log nếu chưa có
$logDir = Split-Path $LogPath -Parent
if (-not (Test-Path $logDir)) { New-Item -ItemType Directory -Path $logDir -Force | Out-Null }

function Write-Log($level, $msg) {
    $ts = Get-Date -Format 'yyyy-MM-dd HH:mm:ss'
    $line = "[$ts] [$level] $msg"
    Add-Content -Path $LogPath -Value $line -Encoding UTF8
    
    switch ($level) {
        'INFO'  { Write-Host $line -ForegroundColor Cyan }
        'OK'    { Write-Host $line -ForegroundColor Green }
        'WARN'  { Write-Host $line -ForegroundColor Yellow }
        'ERROR' { Write-Host $line -ForegroundColor Red }
        default { Write-Host $line }
    }
}

function Test-BackendHealth($url, $timeout) {
    try {
        $response = Invoke-WebRequest -Uri $url -UseBasicParsing -Method Get -TimeoutSec $timeout -ErrorAction Stop
        return @{ Success = ($response.StatusCode -ge 200 -and $response.StatusCode -lt 300); StatusCode = $response.StatusCode }
    } catch {
        return @{ Success = $false; Error = $_.Exception.Message }
    }
}

function Test-SqlConnection($connString) {
    try {
        Add-Type -AssemblyName System.Data -ErrorAction SilentlyContinue
        $conn = New-Object System.Data.SqlClient.SqlConnection $connString
        $conn.Open()
        $cmd = $conn.CreateCommand()
        $cmd.CommandText = 'SELECT 1'
        $result = $cmd.ExecuteScalar()
        $conn.Close()
        return @{ Success = ($result -eq 1) }
    } catch {
        return @{ Success = $false; Error = $_.Exception.Message }
    }
}

# ========== BẮT ĐẦU KIỂM TRA ==========
Write-Log 'INFO' '===== BẮT ĐẦU KIỂM TRA SỨC KHỎE ====='

$backendOk = $false
$sqlOk = $false

# 1. Kiểm tra Backend API
Write-Log 'INFO' "Kiểm tra backend health: $HealthUrl"
$healthResult = Test-BackendHealth -url $HealthUrl -timeout $TimeoutSeconds

if ($healthResult.Success) {
    Write-Log 'OK' "Backend đang hoạt động (HTTP $($healthResult.StatusCode))"
    $backendOk = $true
} else {
    Write-Log 'ERROR' "Backend KHÔNG phản hồi: $($healthResult.Error)"
}

# 2. Kiểm tra SQL Server (nếu có appsettings)
$connString = $null
$defaultAppSettings = @(
    'C:\inetpub\wwwroot\RetailPointBackend\appsettings.json',
    'C:\shop\backend-deploy\appsettings.json',
    'C:\shop\Backend\RetailPointBackend\appsettings.json'
)

if ($AppSettingsPath -and (Test-Path $AppSettingsPath)) {
    $settingsFile = $AppSettingsPath
} else {
    foreach ($p in $defaultAppSettings) {
        if (Test-Path $p) { $settingsFile = $p; break }
    }
}

if ($settingsFile) {
    try {
        $json = Get-Content $settingsFile -Raw | ConvertFrom-Json
        $connString = $json.ConnectionStrings.DefaultConnection
    } catch {
        Write-Log 'WARN' "Không thể đọc appsettings: $($_.Exception.Message)"
    }
}

if ($connString) {
    Write-Log 'INFO' "Kiểm tra kết nối SQL Server..."
    $sqlResult = Test-SqlConnection -connString $connString
    
    if ($sqlResult.Success) {
        Write-Log 'OK' "SQL Server đang hoạt động"
        $sqlOk = $true
    } else {
        Write-Log 'ERROR' "SQL Server KHÔNG kết nối được: $($sqlResult.Error)"
    }
} else {
    Write-Log 'WARN' "Không tìm thấy connection string, bỏ qua kiểm tra SQL"
    $sqlOk = $true  # Không kiểm tra = coi như OK
}

# 3. Xử lý tự động restart nếu cần
if (-not $backendOk -and $AutoRestart) {
    Write-Log 'INFO' "Đang restart service '$ServiceName'..."
    
    $svc = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
    if ($svc) {
        try {
            Restart-Service -Name $ServiceName -Force -ErrorAction Stop
            Start-Sleep -Seconds 5
            
            # Kiểm tra lại
            $healthResult = Test-BackendHealth -url $HealthUrl -timeout $TimeoutSeconds
            if ($healthResult.Success) {
                Write-Log 'OK' "Backend đã khôi phục sau restart!"
                $backendOk = $true
            } else {
                Write-Log 'ERROR' "Backend vẫn không hoạt động sau restart"
            }
        } catch {
            Write-Log 'ERROR' "Không thể restart service: $($_.Exception.Message)"
        }
    } else {
        Write-Log 'WARN' "Service '$ServiceName' không tồn tại. Thử khởi động exe trực tiếp..."
        
        # Tìm và start exe
        $exePaths = @(
            'C:\shop\backend-deploy\RetailPointBackend.exe',
            'C:\inetpub\wwwroot\RetailPointBackend\RetailPointBackend.exe'
        )
        foreach ($exe in $exePaths) {
            if (Test-Path $exe) {
                Write-Log 'INFO' "Khởi động $exe..."
                Start-Process -FilePath $exe -WorkingDirectory (Split-Path $exe) -WindowStyle Hidden
                Start-Sleep -Seconds 5
                
                $healthResult = Test-BackendHealth -url $HealthUrl -timeout $TimeoutSeconds
                if ($healthResult.Success) {
                    Write-Log 'OK' "Backend đã khởi động thành công!"
                    $backendOk = $true
                }
                break
            }
        }
    }
}

if (-not $sqlOk -and $AutoRestartSql) {
    Write-Log 'INFO' "Đang restart SQL Server service..."
    
    # Parse instance name từ connection string
    $svcName = 'MSSQLSERVER'
    if ($connString -match 'Server=([^;\\]+)\\([^;]+)') {
        $svcName = "MSSQL`$$($Matches[2])"
    }
    
    $sqlSvc = Get-Service -Name $svcName -ErrorAction SilentlyContinue
    if ($sqlSvc) {
        try {
            Restart-Service -Name $svcName -Force -ErrorAction Stop
            Start-Sleep -Seconds 10
            
            $sqlResult = Test-SqlConnection -connString $connString
            if ($sqlResult.Success) {
                Write-Log 'OK' "SQL Server đã khôi phục!"
                $sqlOk = $true
            }
        } catch {
            Write-Log 'ERROR' "Không thể restart SQL: $($_.Exception.Message)"
        }
    } else {
        Write-Log 'WARN' "Không tìm thấy SQL service '$svcName' trên máy này"
    }
}

# 4. Tổng kết
Write-Log 'INFO' '===== KẾT QUẢ KIỂM TRA ====='
if ($backendOk -and $sqlOk) {
    Write-Log 'OK' "Hệ thống hoạt động bình thường"
    exit 0
} else {
    if (-not $backendOk) { Write-Log 'ERROR' "Backend: KHÔNG OK" }
    if (-not $sqlOk) { Write-Log 'ERROR' "SQL Server: KHÔNG OK" }
    exit 1
}
