<#
Diagnose RetailPointBackend health and environment.
Creates a timestamped folder under C:\shop\diagnose-output and writes a set of checks there,
then zips the folder into C:\shop\diagnose-backend-<timestamp>.zip.

Run as: PowerShell (may require Administrator for some checks)
Usage: .\scripts\diagnose-backend.ps1
#>

param(
    [int]$SqlTestTimeoutSec = 5
)

function Write-Log($path, $text) {
    $text | Out-File -FilePath $path -Append -Encoding UTF8
}

$root = 'C:\shop'
$ts = Get-Date -Format 'yyyyMMdd-HHmmss'
$outDir = Join-Path $root "diagnose-output-$ts"
New-Item -ItemType Directory -Path $outDir -Force | Out-Null

$report = Join-Path $outDir 'report.txt'
"Diagnosis run at: $(Get-Date -Format 'u')" | Out-File $report -Encoding UTF8

Write-Log $report "Host: $env:COMPUTERNAME"
Write-Log $report "User: $env:USERNAME"

Write-Log $report "`n--- Processes: RetailPointBackend ---`n"
Get-Process -Name RetailPointBackend -ErrorAction SilentlyContinue | Select-Object Id, ProcessName, Path, StartTime, CPU, WS | Format-List | Out-String | Write-Log $report

Write-Log $report "`n--- netstat (listening on 5273) ---`n"
netstat -ano | findstr 5273 | Out-String | Write-Log $report

Write-Log $report "`n--- Health endpoint (127.0.0.1:5273) ---`n"
try {
    $r = Invoke-RestMethod -Uri 'http://127.0.0.1:5273/weatherforecast' -Method Get -TimeoutSec 5 -ErrorAction Stop
    $r | ConvertTo-Json -Depth 3 | Out-File (Join-Path $outDir 'health_local.json') -Encoding UTF8
    Write-Log $report "Local health: OK"
} catch {
    Write-Log $report "Local health failed: $($_.Exception.Message)"
}

Write-Log $report "`n--- Health endpoint (external IP) ---`n"
# try to detect external IP from config (fallback to 101.53.9.76)
$externalIp = '101.53.9.76'
try { $r2 = Invoke-RestMethod -Uri "http://$externalIp:5273/weatherforecast" -Method Get -TimeoutSec 5 -ErrorAction Stop; $r2 | ConvertTo-Json -Depth 3 | Out-File (Join-Path $outDir 'health_external.json') -Encoding UTF8; Write-Log $report "External health: OK" } catch { Write-Log $report "External health failed: $($_.Exception.Message)" }

# Logs
Write-Log $report "`n--- Backend logs ---`n"
$logDirs = @('C:\shop\backend-deploy\logs','C:\shop\server\logs','C:\shop\backend-deploy')
foreach ($d in $logDirs) {
    if (Test-Path $d) {
        Write-Log $report "Listing: $d"
        Get-ChildItem -Path $d -File | Sort-Object LastWriteTime -Descending | Select-Object Name,Length,LastWriteTime | Out-String | Write-Log $report
        # copy last 500 lines of any stderr/stdout logs
        Get-ChildItem -Path $d -File | Where-Object { $_.Name -match 'stderr|stderr|service-stderr|stdout|service-stdout' } | ForEach-Object {
            $target = Join-Path $outDir $_.Name
            Write-Log $report "Copying tail of $_ to $target"
            try { Get-Content $_.FullName -Tail 500 | Out-File $target -Encoding UTF8 -Force } catch { Get-Content $_.FullName -Encoding UTF8 | Out-File $target -Encoding UTF8 -Force }
        }
    } else {
        Write-Log $report "Path not found: $d"
    }
}

# appsettings reading (published path)
Write-Log $report "`n--- appsettings.json (published) ---`n"
$published = 'C:\shop\Backend\Published\appsettings.json'
if (Test-Path $published) {
    Get-Content $published | Out-File (Join-Path $outDir 'appsettings.published.json') -Encoding UTF8 -Force
    try {
        $cfg = Get-Content $published -Raw | ConvertFrom-Json
        if ($cfg.ConnectionStrings.DefaultConnection) {
            $cs = $cfg.ConnectionStrings.DefaultConnection
            Write-Log $report "Found DefaultConnection in published appsettings"
            Write-Log $report $cs
        } else { Write-Log $report "No DefaultConnection found in published appsettings" }
    } catch { Write-Log $report "Failed to parse published appsettings: $($_.Exception.Message)" }
} else { Write-Log $report "Published appsettings not found at $published" }

# SQL service and connectivity
Write-Log $report "`n--- SQL Services ---`n"
Get-Service | Where-Object { $_.Name -match 'SQL' -or $_.DisplayName -match 'SQL' } | Select-Object Name,DisplayName,Status | Out-String | Write-Log $report

if ($cs) {
    Write-Log $report "`n--- Testing SQL Connection (from DefaultConnection) ---`n"
    try {
        $sqlConn = New-Object System.Data.SqlClient.SqlConnection $cs
        $sqlConn.Open()
        Write-Log $report "SQL connection succeeded"
        $sqlConn.Close()
    } catch {
        Write-Log $report "SQL connection failed: $($_.Exception.Message)"
    }
} else {
    Write-Log $report "No connection string to test."
}

# Firewall check for port 5273 (requires Get-NetFirewallRule availability)
Write-Log $report "`n--- Firewall rules for port 5273 ---`n"
try {
    if (Get-Command Get-NetFirewallRule -ErrorAction SilentlyContinue) {
        Get-NetFirewallRule -Direction Inbound -Action Allow | Where-Object { ($_ | Get-NetFirewallPortFilter).LocalPort -contains 5273 } | Select-Object Name,DisplayName,Enabled | Out-String | Write-Log $report
    } else {
        Write-Log $report "Get-NetFirewallRule not available on this system"
    }
} catch { Write-Log $report "Firewall query failed: $($_.Exception.Message)" }

# Event Log (Application) last 2 hours for keywords
Write-Log $report "`n--- Recent Application Event Log (2 hours) matching keywords ---`n"
try {
    $start = (Get-Date).AddHours(-2)
    Get-WinEvent -FilterHashtable @{LogName='Application'; StartTime=$start} | Where-Object { $_.Message -match 'ANCM|IIS|RetailPointBackend|SocketException|Kestrel' } | Select-Object TimeCreated, ProviderName, Id, LevelDisplayName, @{n='Message';e={$_.Message -replace "`r`n","\n"}} | Out-String -Width 200 | Write-Log $report
} catch { Write-Log $report "Event log query failed: $($_.Exception.Message)" }

# Save a quick summary to separate file
$summary = Join-Path $outDir 'summary.txt'
Get-Content $report | Select-String -Pattern 'Local health:','External health:','SQL connection','SocketException','Now listening on' -Context 0,2 | Out-File $summary -Encoding UTF8

# Create zip
$zip = Join-Path $root "diagnose-backend-$ts.zip"
try { Compress-Archive -Path $outDir\* -DestinationPath $zip -Force; Write-Host "Created zip: $zip" -ForegroundColor Green } catch { Write-Host "Failed to create zip: $($_.Exception.Message)" -ForegroundColor Red }

Write-Host "Diagnosis complete. Output directory: $outDir" -ForegroundColor Cyan
Write-Host "ZIP: $zip" -ForegroundColor Cyan
Write-Host "Please attach the zip if you want me to analyze further." -ForegroundColor Yellow
