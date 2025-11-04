# Simple Backend Management Script

param([string]$Action = "status")

$ProcessName = "RetailPointBackend"
$ApiUrl = "http://101.53.9.76:5273/weatherforecast"

if ($Action -eq "status") {
    $process = Get-Process -Name $ProcessName -ErrorAction SilentlyContinue
    if ($process) {
        Write-Host "Backend RUNNING - PID: $($process.Id) - Memory: $([math]::Round($process.WorkingSet / 1MB, 2)) MB" -ForegroundColor Green
        try {
            $response = Invoke-RestMethod -Uri $ApiUrl -TimeoutSec 5
            Write-Host "API OK - Responding normally" -ForegroundColor Green
        } catch {
            Write-Host "API ERROR - Not responding" -ForegroundColor Red
        }
    } else {
        Write-Host "Backend NOT RUNNING" -ForegroundColor Red
    }
}

if ($Action -eq "stop") {
    $process = Get-Process -Name $ProcessName -ErrorAction SilentlyContinue
    if ($process) {
        Stop-Process -Name $ProcessName -Force
        Write-Host "Backend stopped" -ForegroundColor Yellow
    } else {
        Write-Host "Backend was not running" -ForegroundColor Yellow
    }
}

if ($Action -eq "start") {
    $process = Get-Process -Name $ProcessName -ErrorAction SilentlyContinue
    if ($process) {
        Write-Host "Backend already running" -ForegroundColor Yellow
    } else {
        $proc = Start-Process -FilePath "C:\shop\server\RetailPointBackend.exe" -ArgumentList "--urls", "http://0.0.0.0:5273" -WorkingDirectory "C:\shop\server" -WindowStyle Hidden -PassThru
        Write-Host "Backend started - PID: $($proc.Id)" -ForegroundColor Green
    }
}