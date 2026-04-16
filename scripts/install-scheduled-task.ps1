<#
.SYNOPSIS
    Cài đặt Scheduled Task để chạy health-monitor.ps1 định kỳ.

.DESCRIPTION
    Script này sẽ tạo một Windows Scheduled Task chạy mỗi 5 phút
    để kiểm tra sức khỏe backend và tự động restart nếu cần.

.EXAMPLE
    .\install-scheduled-task.ps1

.NOTES
    Yêu cầu chạy với quyền Administrator.
#>

param(
    [string]$TaskName = 'RetailPoint-HealthMonitor',
    [string]$ScriptPath = 'C:\shop\scripts\health-monitor.ps1',
    [string]$HealthUrl = 'http://127.0.0.1:5273/weatherforecast',
    [int]$IntervalMinutes = 5
)

$ErrorActionPreference = 'Stop'

# Kiểm tra quyền admin
$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Host "[ERROR] Script này cần chạy với quyền Administrator!" -ForegroundColor Red
    exit 1
}

if (-not (Test-Path $ScriptPath)) {
    Write-Host "[ERROR] Không tìm thấy script: $ScriptPath" -ForegroundColor Red
    exit 2
}

Write-Host "[INFO] Đang cài đặt Scheduled Task '$TaskName'..." -ForegroundColor Cyan

# Xóa task cũ nếu có
$existingTask = Get-ScheduledTask -TaskName $TaskName -ErrorAction SilentlyContinue
if ($existingTask) {
    Write-Host "[INFO] Xóa task cũ..." -ForegroundColor Yellow
    Unregister-ScheduledTask -TaskName $TaskName -Confirm:$false
}

# Tạo action - chạy PowerShell với script
$arguments = "-NoProfile -ExecutionPolicy Bypass -File `"$ScriptPath`" -HealthUrl `"$HealthUrl`" -AutoRestart"
$action = New-ScheduledTaskAction -Execute 'powershell.exe' -Argument $arguments

# Tạo trigger - chạy mỗi X phút
$trigger = New-ScheduledTaskTrigger -Once -At (Get-Date) -RepetitionInterval (New-TimeSpan -Minutes $IntervalMinutes) -RepetitionDuration (New-TimeSpan -Days 9999)

# Cấu hình - chạy dù user có login hay không, chạy với quyền cao nhất
$principal = New-ScheduledTaskPrincipal -UserId 'SYSTEM' -LogonType ServiceAccount -RunLevel Highest

# Settings
$settings = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -StartWhenAvailable -RestartCount 3 -RestartInterval (New-TimeSpan -Minutes 1)

# Đăng ký task
$task = Register-ScheduledTask -TaskName $TaskName -Action $action -Trigger $trigger -Principal $principal -Settings $settings -Description "Giám sát sức khỏe RetailPoint Backend API và tự động restart nếu cần"

if ($task) {
    Write-Host "[OK] Scheduled Task đã được cài đặt!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Thông tin task:" -ForegroundColor Yellow
    Write-Host "  Tên task      : $TaskName"
    Write-Host "  Chạy mỗi      : $IntervalMinutes phút"
    Write-Host "  Script        : $ScriptPath"
    Write-Host "  Health URL    : $HealthUrl"
    Write-Host ""
    Write-Host "Các lệnh quản lý:" -ForegroundColor Yellow
    Write-Host "  Xem trạng thái  : Get-ScheduledTask -TaskName '$TaskName'"
    Write-Host "  Chạy ngay       : Start-ScheduledTask -TaskName '$TaskName'"
    Write-Host "  Tắt task        : Disable-ScheduledTask -TaskName '$TaskName'"
    Write-Host "  Bật task        : Enable-ScheduledTask -TaskName '$TaskName'"
    Write-Host "  Xóa task        : Unregister-ScheduledTask -TaskName '$TaskName' -Confirm:`$false"
    Write-Host ""
} else {
    Write-Host "[ERROR] Không thể tạo Scheduled Task" -ForegroundColor Red
    exit 3
}
