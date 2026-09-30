# Script quan ly Backend Service
param(
    [Parameter(Mandatory=$true)]
    [ValidateSet("start", "stop", "restart", "status")]
    [string]$Action
)

$BackendPath = "C:\shop\server"
$ExeName = "RetailPointBackend"
$Port = 5273
$ApiUrl = "http://localhost:$Port/weatherforecast"

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
        Write-Host "Backend started successfully (PID: $($newProcess.Id))" -ForegroundColor Green
        Test-API
    } else {
        Write-Host "Failed to start backend" -ForegroundColor Red
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
        Write-Host "Backend stopped successfully" -ForegroundColor Green
    } else {
        Write-Host "Failed to stop backend" -ForegroundColor Red
    }
}

function Get-BackendStatus {
    $process = Get-BackendProcess
    if ($process) {
        Write-Host "Backend is RUNNING" -ForegroundColor Green
        Write-Host "  PID: $($process.Id)" -ForegroundColor White
        Write-Host "  Memory: $([math]::Round($process.WorkingSet / 1MB, 2)) MB" -ForegroundColor White
        Test-API
    } else {
        Write-Host "Backend is NOT RUNNING" -ForegroundColor Red
    }
}

function Test-API {
    try {
        Write-Host "Testing API on $ApiUrl ..." -ForegroundColor Yellow
        $response = Invoke-RestMethod -Uri $ApiUrl -TimeoutSec 5
        Write-Host "API is responding OK!" -ForegroundColor Green
    } catch {
        Write-Host "API is not responding: $($_.Exception.Message)" -ForegroundColor Red
    }
}

switch ($Action) {
    "start" { Start-Backend }
    "stop" { Stop-Backend }
    "restart" { 
        Stop-Backend
        Start-Sleep -Seconds 2
        Start-Backend 
    }
    "status" { Get-BackendStatus }
}