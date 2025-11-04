# Script quản lý Backend Service
# Sử dụng: .\manage-backend.ps1 -Action [start|stop|restart|status]

param(
    [Parameter(Mandatory=$true)]
    [ValidateSet("start", "stop", "restart", "status", "logs")]
    [string]$Action
)

$BackendPath = "C:\shop\server"
$ExeName = "RetailPointBackend"
$Port = 5273
$ApiUrl = "http://101.53.9.76:$Port/weatherforecast"

function Get-BackendProcess {
    return Get-Process -Name $ExeName -ErrorAction SilentlyContinue
}

function Start-Backend {
    $process = Get-BackendProcess
    if ($process) {
        Write-Host "Backend already running (PID: $($process.Id))" -ForegroundColor Yellow
        return
    }
    
    Write-Host "Starting backend..." -ForegroundColor Green
    $proc = Start-Process -FilePath "$BackendPath\$ExeName.exe" -ArgumentList "--urls", "http://0.0.0.0:$Port" -WorkingDirectory $BackendPath -WindowStyle Hidden -PassThru
    
    Start-Sleep -Seconds 3
    $newProcess = Get-BackendProcess
    if ($newProcess) {
        Write-Host "✓ Backend started successfully (PID: $($newProcess.Id))" -ForegroundColor Green
        Test-API
    } else {
        Write-Host "✗ Failed to start backend" -ForegroundColor Red
    }
}

function Stop-Backend {
    $process = Get-BackendProcess
    if (-not $process) {
        Write-Host "Backend is not running" -ForegroundColor Yellow
        return
    }
    
    Write-Host "Stopping backend (PID: $($process.Id))..." -ForegroundColor Yellow
    Stop-Process -Id $process.Id -Force
    Start-Sleep -Seconds 2
    
    $stillRunning = Get-BackendProcess
    if (-not $stillRunning) {
        Write-Host "✓ Backend stopped successfully" -ForegroundColor Green
    } else {
        Write-Host "✗ Failed to stop backend" -ForegroundColor Red
    }
}

function Get-BackendStatus {
    $process = Get-BackendProcess
    if ($process) {
        Write-Host "✓ Backend is RUNNING" -ForegroundColor Green
        Write-Host "  PID: $($process.Id)" -ForegroundColor White
        Write-Host "  Memory: $([math]::Round($process.WorkingSet / 1MB, 2)) MB" -ForegroundColor White
        Write-Host "  CPU Time: $($process.CPU)" -ForegroundColor White
        Test-API
    } else {
        Write-Host "✗ Backend is NOT RUNNING" -ForegroundColor Red
    }
}

function Test-API {
    try {
        Write-Host "Testing API..." -ForegroundColor Yellow
        $response = Invoke-RestMethod -Uri $ApiUrl -TimeoutSec 5
        Write-Host "✓ API is responding" -ForegroundColor Green
    } catch {
        Write-Host "✗ API is not responding: $($_.Exception.Message)" -ForegroundColor Red
    }
}

# Main logic
switch ($Action) {
    "start" { Start-Backend }
    "stop" { Stop-Backend }
    "restart" { 
        Stop-Backend
        Start-Sleep -Seconds 2
        Start-Backend 
    }
    "status" { Get-BackendStatus }
    "logs" { 
        Write-Host "Backend logs not available in current setup" -ForegroundColor Yellow
        Write-Host "Consider using: Get-EventLog or redirect output to log file" -ForegroundColor White
    }
}