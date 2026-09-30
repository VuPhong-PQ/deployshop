# Script install auto-start for RetailPoint Backend API
param(
    [string]$BackendExe = "C:\shop\server\RetailPointBackend.exe",
    [string]$WorkingDirectory = "C:\shop\server",
    [string]$Urls = "http://0.0.0.0:5273",
    [string]$TaskName = "RetailPointBackendAutoStart"
)

Write-Host "==================================================" -ForegroundColor Cyan
Write-Host " TAO AUTOMATIC STARTUP TASK CHO BACKEND SERVICE   " -ForegroundColor Cyan
Write-Host "==================================================" -ForegroundColor Cyan

# 1. Add shortcut in current User's Startup folder (Runs automatically on User logon, no Admin required)
try {
    $UserStartup = [Environment]::GetFolderPath('Startup')
    $UserShortcutPath = Join-Path $UserStartup "RetailPointBackend.lnk"
    $BatchScriptPath = "C:\shop\Deploy\start-backend-service.bat"

    $WshShell = New-Object -ComObject WScript.Shell
    $Shortcut = $WshShell.CreateShortcut($UserShortcutPath)
    $Shortcut.TargetPath = $BatchScriptPath
    $Shortcut.WorkingDirectory = $WorkingDirectory
    $Shortcut.Description = "Start RetailPoint Backend API on Startup"
    $Shortcut.Save()
    Write-Host "[OK] Da tao shortcut khoi dong trong Startup Folder cua User: $UserShortcutPath" -ForegroundColor Green
} catch {
    Write-Host "[WARN] Khong the tao shortcut Startup User: $($_.Exception.Message)" -ForegroundColor Yellow
}

# 2. Try registering Scheduled Task (Runs on System Boot)
try {
    $action = New-ScheduledTaskAction -Execute $BackendExe -Argument "--urls $Urls" -WorkingDirectory $WorkingDirectory
    $trigger = New-ScheduledTaskTrigger -AtStartup
    $settings = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -RestartCount 3 -RestartInterval (New-TimeSpan -Minutes 1)
    
    Register-ScheduledTask -TaskName $TaskName -Action $action -Trigger $trigger -Settings $settings -User "NT AUTHORITY\SYSTEM" -RunLevel Highest -Force -ErrorAction Stop | Out-Null
    Write-Host "[OK] Da dang ky Task Scheduler '$TaskName' (Chay tu dong ngay khi Windows boot, truoc khi Login)!" -ForegroundColor Green
} catch {
    Write-Host "[NOTE] Register-ScheduledTask can quyen Admin. Vui long chay file 'REGISTER_AUTOSTART_TASK_AS_ADMIN.bat' voi quyen Run as Administrator de dang ky khoi dong theo Windows Boot." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "==================================================" -ForegroundColor Green
Write-Host " KIEM TRA TASK DANG KY: " -ForegroundColor Green
Get-ScheduledTask -TaskName $TaskName -ErrorAction SilentlyContinue | Select-Object TaskName, State, TaskPath | Format-Table -AutoSize
Write-Host "==================================================" -ForegroundColor Green
