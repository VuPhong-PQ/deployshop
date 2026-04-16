<#
Check SQL Server connectivity (using connection string from appsettings.json or provided string), inspect SQL service status, and optionally restart SQL Server service and backend exe.

Usage examples:
  # Read connection string from backend appsettings.json and test connection
  .\check-sql-and-restart.ps1 -AppSettingsPath 'C:\inetpub\wwwroot\RetailPointBackend\appsettings.json'

  # Provide connection string explicitly and attempt to restart SQL service if not running
  .\check-sql-and-restart.ps1 -ConnectionString "Server=MYHOST\\KTEAM;Database=RetailPoint;User Id=sa;Password=secret;"

  # Restart SQL service and backend exe if connectivity fails
  .\check-sql-and-restart.ps1 -AppSettingsPath 'C:\inetpub\wwwroot\RetailPointBackend\appsettings.json' -RestartSqlService -RestartBackend -BackendExePath 'C:\inetpub\wwwroot\RetailPointBackend\RetailPointBackend.exe'

Parameters:
  -AppSettingsPath : path to appsettings.json to extract ConnectionStrings:DefaultConnection
  -ConnectionString: explicit SQL connection string; if provided it overrides appsettings
  -RestartSqlService: if specified, script will attempt to start/restart the SQL service when it is stopped
  -RestartBackend  : if specified, script will attempt to restart the backend process (stops and starts exe)
  -BackendExePath  : required if -RestartBackend is provided
  -SqlServiceTimeoutSeconds : wait timeout for service operations (default 30)
#>

param(
    [string]$AppSettingsPath,
    [string]$ConnectionString,
    [switch]$RestartSqlService,
    [switch]$RestartBackend,
    [string]$BackendExePath,
    [int]$SqlServiceTimeoutSeconds = 30
)

function Write-Info($m){ Write-Host "[INFO] $m" -ForegroundColor Cyan }
function Write-Ok($m){ Write-Host "[OK]   $m" -ForegroundColor Green }
function Write-Warn($m){ Write-Host "[WARN] $m" -ForegroundColor Yellow }
function Write-Err($m){ Write-Host "[ERROR] $m" -ForegroundColor Red }

if (-not $ConnectionString -and $AppSettingsPath) {
    if (-not (Test-Path $AppSettingsPath)) {
        Write-Err "appsettings.json not found at $AppSettingsPath"
        exit 2
    }
    try {
        $json = Get-Content $AppSettingsPath -Raw | ConvertFrom-Json
        $ConnectionString = $json.ConnectionStrings.DefaultConnection
        if (-not $ConnectionString) { Write-Err "DefaultConnection not found in appsettings.json"; exit 3 }
        Write-Info "Loaded connection string from $AppSettingsPath"
    } catch {
        Write-Err "Failed to read appsettings.json: $($_.Exception.Message)"
        exit 4
    }
}

if (-not $ConnectionString) {
    Write-Err "No connection string provided. Use -ConnectionString or -AppSettingsPath."
    exit 5
}

Write-Info "Connection string: $ConnectionString"

# Parse server name and instance
# Expect formats: Server=HOST\INSTANCE;...  or Server=HOST,PORT;...
$server = $null
try {
    $parts = $ConnectionString -split ';' | Where-Object { $_ -match '=' }
    foreach ($p in $parts) {
        if ($p.Trim().StartsWith('Server=', [System.StringComparison]::InvariantCultureIgnoreCase)) {
            $server = $p.Substring($p.IndexOf('=')+1).Trim()
            break
        }
    }
} catch { }

if (-not $server) { Write-Warn "Could not parse server from connection string. We'll make a best-effort check." }
else { Write-Info "Parsed server: $server" }

# Determine Windows service name for SQL Server
$svcName = $null
$hostOnly = $server
if ($server -and $server -match '\\') {
    $parts = $server.Split('\\')
    $hostOnly = $parts[0]
    $instance = $parts[1]
    $svcName = "MSSQL$$instance"  # e.g. MSSQL$KTEAM
} elseif ($server -and $server -match ',') {
    # server,port
    $hostOnly = $server.Split(',')[0]
    $svcName = 'MSSQLSERVER'
} elseif ($server) {
    $svcName = 'MSSQLSERVER'
}

if ($svcName) { Write-Info "Assumed SQL service name: $svcName (host: $hostOnly)" }

# Check service status locally (only works when run on SQL server host)
$localService = $null
try {
    $localService = Get-Service -Name $svcName -ErrorAction SilentlyContinue
} catch { }

if ($localService) {
    Write-Info "Service '$svcName' status: $($localService.Status)"
} else {
    Write-Warn "Service '$svcName' not found on this machine (this script should run on the DB server to manage the service)."
}

# Test network connectivity to host (ping + port 1433) - note named instances might use dynamic ports
if ($hostOnly) {
    Write-Info "Testing network reachability to host: $hostOnly"
    $ping = Test-Connection -ComputerName $hostOnly -Count 1 -Quiet -ErrorAction SilentlyContinue
    if ($ping) { Write-Ok "Host $hostOnly is reachable (ping)" } else { Write-Warn "Host $hostOnly did not respond to ping" }

    Write-Info "Testing TCP port 1433 to $hostOnly (common default port)"
    try {
        $tnc = Test-NetConnection -ComputerName $hostOnly -Port 1433 -WarningAction SilentlyContinue
        if ($tnc.TcpTestSucceeded) { Write-Ok "TCP 1433 is open" } else { Write-Warn "TCP 1433 is not open or blocked (named instance may use different port)" }
    } catch { Write-Warn "Test-NetConnection failed: $($_.Exception.Message)" }
}

# Attempt to open SQL connection using System.Data.SqlClient
Write-Info "Attempting SQL connection..."
$connectionSucceeded = $false
try {
    Add-Type -AssemblyName System.Data
    $sqlConn = New-Object System.Data.SqlClient.SqlConnection $ConnectionString
    $sqlConn.Open()
    $cmd = $sqlConn.CreateCommand()
    $cmd.CommandText = 'SELECT 1'
    $res = $cmd.ExecuteScalar()
    if ($res -eq 1) {
        Write-Ok "SQL connection successful and query returned 1"
        $connectionSucceeded = $true
    } else {
        Write-Warn "SQL connected but query returned unexpected result: $res"
    }
    $sqlConn.Close()
} catch {
    Write-Err "SQL connection failed: $($_.Exception.Message)"
}

# If connection failed and we have local service info and -RestartSqlService, try to start/restart
if (-not $connectionSucceeded -and $localService) {
    if ($RestartSqlService) {
        Write-Info "Attempting to start/restart service $svcName..."
        try {
            if ($localService.Status -ne 'Running') {
                Start-Service -Name $svcName -ErrorAction Stop
                Write-Info "Waiting up to $SqlServiceTimeoutSeconds seconds for service to be Running..."
                $sw = [Diagnostics.Stopwatch]::StartNew()
                while ($sw.Elapsed.TotalSeconds -lt $SqlServiceTimeoutSeconds) {
                    $localService = Get-Service -Name $svcName
                    if ($localService.Status -eq 'Running') { break }
                    Start-Sleep -Seconds 1
                }
                $localService = Get-Service -Name $svcName
                Write-Info "Service status: $($localService.Status)"
            } else {
                Write-Info "Service already running. Attempting Restart-Service..."
                Restart-Service -Name $svcName -Force -ErrorAction Stop
                Write-Info "Service restarted"
            }

            # Try SQL connection again
            Write-Info "Re-testing SQL connection after service start..."
            Add-Type -AssemblyName System.Data
            $sqlConn = New-Object System.Data.SqlClient.SqlConnection $ConnectionString
            $sqlConn.Open()
            $cmd = $sqlConn.CreateCommand()
            $cmd.CommandText = 'SELECT 1'
            $res = $cmd.ExecuteScalar()
            if ($res -eq 1) { Write-Ok "SQL connection successful after restart"; $connectionSucceeded = $true } else { Write-Warn "After restart, SQL query returned: $res" }
            $sqlConn.Close()
        } catch {
            Write-Err "Failed to start/restart service: $($_.Exception.Message)"
        }
    } else {
        Write-Warn "SQL connection failed and -RestartSqlService not specified. To attempt restart, re-run with -RestartSqlService"
    }
}

# Optionally restart backend if SQL is now OK (or still failing but you want to restart backend)
if ($RestartBackend) {
    if (-not $BackendExePath) { Write-Err "-BackendExePath is required when using -RestartBackend"; exit 10 }
    if (-not (Test-Path $BackendExePath)) { Write-Err "Backend exe not found: $BackendExePath"; exit 11 }

    $exeName = [System.IO.Path]::GetFileNameWithoutExtension($BackendExePath)
    $procs = Get-Process -Name $exeName -ErrorAction SilentlyContinue | Where-Object { $_.Path -eq $BackendExePath }
    if ($procs) {
        foreach ($p in $procs) {
            Write-Info "Stopping backend process PID $($p.Id)..."
            try { Stop-Process -Id $p.Id -Force -ErrorAction Stop; Write-Info "Stopped PID $($p.Id)" } catch { Write-Warn "Failed to stop PID $($p.Id): $($_.Exception.Message)" }
        }
        Start-Sleep -Seconds 1
    } else { Write-Info "No backend process running for $BackendExePath" }

    Write-Info "Starting backend exe: $BackendExePath"
    Start-Process -FilePath $BackendExePath -WorkingDirectory (Split-Path $BackendExePath) -WindowStyle Hidden
    Write-Info "Started backend. Give it a few seconds to initialize."
}

if ($connectionSucceeded) { Write-Ok "Overall result: SQL connectivity OK"; exit 0 } else { Write-Err "Overall result: SQL connectivity FAILED"; exit 20 }
