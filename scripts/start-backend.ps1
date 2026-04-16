<#
Start/Restart RetailPointBackend on a Windows host.

Usage examples:
  # Use default paths and port
  .\start-backend.ps1

  # Provide explicit executable path
  .\start-backend.ps1 -ExePath 'C:\inetpub\wwwroot\RetailPointBackend\RetailPointBackend.exe'

  # Run health check against remote host/port (if the app is bound to a specific IP)
  .\start-backend.ps1 -Host 101.53.9.76 -Port 5273

Parameters:
  -ExePath   : full path to RetailPointBackend.exe (optional)
  -Host      : host to call for health check (default: 127.0.0.1)
  -Port      : port to call for health check (default: 5273)
  -HealthPath: path to call for health (default: /weatherforecast)
  -Timeout   : seconds to wait for service to become healthy after start (default: 30)
  -ForceRestart: if specified, will stop running process and start again
#>
param(
    [string]$ExePath,
    [string]$Host = '127.0.0.1',
    [int]$Port = 5273,
    [string]$HealthPath = '/weatherforecast',
    [int]$Timeout = 30,
    [switch]$ForceRestart
)

function Write-Info($msg){ Write-Host "[INFO] $msg" -ForegroundColor Cyan }
function Write-Ok($msg){ Write-Host "[OK]   $msg" -ForegroundColor Green }
function Write-Warn($msg){ Write-Host "[WARN] $msg" -ForegroundColor Yellow }
function Write-Err($msg){ Write-Host "[ERROR] $msg" -ForegroundColor Red }

# Common publish locations to try if ExePath not provided
$defaultPaths = @(
    'C:\shop\backend-deploy\RetailPointBackend.exe',
    'C:\shop\Backend\RetailPointBackend\bin\Release\net9.0\publish\RetailPointBackend.exe',
    'C:\shop\Backend\RetailPointBackend\bin\Debug\net9.0\RetailPointBackend.exe',
    'C:\inetpub\wwwroot\RetailPointBackend\RetailPointBackend.exe',
    'C:\inetpub\wwwroot\retailpoint-api\RetailPointBackend.exe',
    'C:\shop\Deploy\Backend\RetailPointBackend.exe'
)

if (-not $ExePath) {
    foreach ($p in $defaultPaths) {
        if (Test-Path $p) { $ExePath = $p; break }
    }
}

if (-not $ExePath) {
    Write-Warn "Không tìm thấy đường dẫn tới RetailPointBackend.exe tự động. Vui lòng cung cấp tham số -ExePath."
    Write-Host "Các đường dẫn thử:"
    $defaultPaths | ForEach-Object { Write-Host "  $_" }
    exit 2
}

if (-not (Test-Path $ExePath)) {
    Write-Err "File executable không tồn tại: $ExePath"
    exit 3
}

$exeName = [System.IO.Path]::GetFileNameWithoutExtension($ExePath)
$healthUrl = "http://$Host:$Port$HealthPath"

Write-Info "Using exe: $ExePath"
Write-Info "Health check URL: $healthUrl"

# Helper to check health endpoint
function Test-Health($url){
    try {
        $response = Invoke-WebRequest -Uri $url -UseBasicParsing -Method Get -TimeoutSec 5 -ErrorAction Stop
        return $response.StatusCode -ge 200 -and $response.StatusCode -lt 300
    } catch {
        return $false
    }
}

# Check for existing process
$proc = Get-Process -Name $exeName -ErrorAction SilentlyContinue | Where-Object { $_.Path -eq $ExePath } 
if ($proc) {
    Write-Info "Process $exeName is running (PID: $($proc.Id)). Checking health..."
    if (Test-Health $healthUrl) {
        if ($ForceRestart) {
            Write-Info "Force restart requested. Stopping process PID $($proc.Id)..."
            Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
            Start-Sleep -Seconds 1
        } else {
            Write-Ok "Backend is running and healthy. No action needed."
            exit 0
        }
    } else {
        Write-Warn "Process running but health check failed. Will restart."
        try { Stop-Process -Id $proc.Id -Force -ErrorAction Stop; Write-Info "Stopped process." } catch { Write-Warn "Failed to stop process cleanly: $($_.Exception.Message)" }
        Start-Sleep -Seconds 1
    }
} else {
    # Maybe a process with same exe name but different path is running; warn
    $other = Get-Process -Name $exeName -ErrorAction SilentlyContinue | Where-Object { $_.Path -ne $ExePath }
    if ($other) {
        Write-Warn "Found a process named $exeName but with different path(s):"
        $other | ForEach-Object { Write-Host "  PID: $($_.Id) Path: $($_.Path)" }
        Write-Warn "Continuing and will start $ExePath anyway."
    } else {
        Write-Info "No running process named $exeName found. Starting..."
    }
}

# Start the executable
$logDir = Join-Path ([System.IO.Path]::GetDirectoryName($ExePath)) 'logs'
if (-not (Test-Path $logDir)) { New-Item -ItemType Directory -Path $logDir | Out-Null }
$stdout = Join-Path $logDir "stdout-$(Get-Date -Format 'yyyyMMdd-HHmmss').log"
$stderr = Join-Path $logDir "stderr-$(Get-Date -Format 'yyyyMMdd-HHmmss').log"

Write-Info "Starting executable... Logs: $stdout , $stderr"
$startInfo = @{
    FilePath = $ExePath
    WorkingDirectory = [System.IO.Path]::GetDirectoryName($ExePath)
    RedirectStandardOutput = $true
    RedirectStandardError = $true
    NoNewWindow = $true
}

# Start-Process doesn't support redirection to files directly when using -NoNewWindow
# We'll use Start-Process with -RedirectStandardOutput/ -RedirectStandardError only available in PowerShell 7+.
# For broad compatibility (Windows PowerShell 5.1), use Start-Process and redirect via cmd /c or use Start-Job to capture streams.

if ($PSVersionTable.PSVersion.Major -ge 7) {
    # PowerShell 7+ supports -RedirectStandardOutput
    $procInfo = Start-Process @startInfo -RedirectStandardOutput $stdout -RedirectStandardError $stderr -PassThru
} else {
    # Use cmd.exe to redirect output (works on Windows PowerShell 5.1)
    $cmd = "`"$ExePath`" > `"$stdout`" 2> `"$stderr`""
    $procInfo = Start-Process -FilePath 'cmd.exe' -ArgumentList "/c", $cmd -WorkingDirectory (Split-Path $ExePath) -WindowStyle Hidden -PassThru
}

if (-not $procInfo) {
    Write-Err "Không thể start process"
    exit 4
}

Write-Info "Started process (PID: $($procInfo.Id)). Waiting for health..."

$sw = [Diagnostics.Stopwatch]::StartNew()
$healthy = $false
while ($sw.Elapsed.TotalSeconds -lt $Timeout) {
    Start-Sleep -Seconds 1
    if (Test-Health $healthUrl) { $healthy = $true; break }
}

if ($healthy) {
    Write-Ok "Backend started and healthy."
    exit 0
} else {
    Write-Err "Backend did not become healthy within $Timeout seconds. Check logs: $stdout and $stderr"
    exit 5
}
