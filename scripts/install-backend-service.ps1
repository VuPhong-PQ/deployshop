<#
.SYNOPSIS
    Install RetailPointBackend as Windows Service using NSSM.

.EXAMPLE
    .\install-backend-service.ps1
    .\install-backend-service.ps1 -BackendExePath 'C:\inetpub\wwwroot\RetailPointBackend\RetailPointBackend.exe'

.NOTES
    Requires Administrator privileges.
#>

param(
    [string]$BackendExePath,
    [string]$ServiceName = 'RetailPointBackend',
    [string]$ServiceDisplayName = 'RetailPoint Backend API',
    [string]$ServiceDescription = 'RetailPoint POS Backend API Service - Auto start and monitor',
    [string]$NssmPath = 'C:\tools\nssm\nssm.exe'
)

$ErrorActionPreference = 'Stop'

function Write-Info($m){ Write-Host "[INFO] $m" -ForegroundColor Cyan }
function Write-Ok($m){ Write-Host "[OK]   $m" -ForegroundColor Green }
function Write-Warn($m){ Write-Host "[WARN] $m" -ForegroundColor Yellow }
function Write-Err($m){ Write-Host "[ERROR] $m" -ForegroundColor Red }

# Check admin
$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Err "This script requires Administrator privileges!"
    Write-Host "Please run PowerShell as Administrator and try again."
    exit 1
}

# Find exe path
$defaultPaths = @(
    'C:\shop\backend-deploy\RetailPointBackend.exe',
    'C:\inetpub\wwwroot\RetailPointBackend\RetailPointBackend.exe',
    'C:\inetpub\wwwroot\retailpoint-api\RetailPointBackend.exe',
    'C:\shop\Backend\RetailPointBackend\bin\Release\net9.0\publish\RetailPointBackend.exe',
    'C:\shop\Deploy\Backend\RetailPointBackend.exe'
)

if (-not $BackendExePath) {
    foreach ($p in $defaultPaths) {
        if (Test-Path $p) { $BackendExePath = $p; break }
    }
}

if (-not $BackendExePath -or -not (Test-Path $BackendExePath)) {
    Write-Err "RetailPointBackend.exe not found!"
    Write-Host "Paths checked:"
    $defaultPaths | ForEach-Object { Write-Host "  $_" }
    Write-Host ""
    Write-Host "Please provide path: -BackendExePath 'C:\path\to\RetailPointBackend.exe'"
    exit 2
}

$BackendDir = Split-Path $BackendExePath -Parent
Write-Info "Using exe: $BackendExePath"
Write-Info "Working directory: $BackendDir"

# ========== STEP 1: Check/Download NSSM ==========
Write-Host ""
Write-Host "========== STEP 1: Check NSSM ==========" -ForegroundColor Yellow

if (-not (Test-Path $NssmPath)) {
    Write-Info "NSSM not found at $NssmPath. Downloading..."
    
    $nssmDir = Split-Path $NssmPath -Parent
    if (-not (Test-Path $nssmDir)) { New-Item -ItemType Directory -Path $nssmDir -Force | Out-Null }
    
    $nssmUrl = 'https://nssm.cc/release/nssm-2.24.zip'
    $zipPath = Join-Path $env:TEMP 'nssm.zip'
    $extractPath = Join-Path $env:TEMP 'nssm-extract'
    
    try {
        Write-Info "Downloading from $nssmUrl..."
        [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
        Invoke-WebRequest -Uri $nssmUrl -OutFile $zipPath -UseBasicParsing
        
        Write-Info "Extracting..."
        if (Test-Path $extractPath) { Remove-Item $extractPath -Recurse -Force }
        Expand-Archive -Path $zipPath -DestinationPath $extractPath -Force
        
        $nssmExe = Get-ChildItem -Path $extractPath -Recurse -Filter 'nssm.exe' | Where-Object { $_.Directory.Name -eq 'win64' } | Select-Object -First 1
        if (-not $nssmExe) {
            $nssmExe = Get-ChildItem -Path $extractPath -Recurse -Filter 'nssm.exe' | Select-Object -First 1
        }
        
        if ($nssmExe) {
            Copy-Item -Path $nssmExe.FullName -Destination $NssmPath -Force
            Write-Ok "NSSM installed at $NssmPath"
        } else {
            Write-Err "nssm.exe not found in zip"
            exit 3
        }
        
        Remove-Item $zipPath -Force -ErrorAction SilentlyContinue
        Remove-Item $extractPath -Recurse -Force -ErrorAction SilentlyContinue
        
    } catch {
        Write-Err "Cannot download NSSM: $($_.Exception.Message)"
        Write-Host ""
        Write-Host "Manual download: https://nssm.cc/download"
        Write-Host "Then place nssm.exe at: $NssmPath"
        exit 4
    }
} else {
    Write-Ok "NSSM found at $NssmPath"
}

# ========== STEP 2: Remove old service ==========
Write-Host ""
Write-Host "========== STEP 2: Check existing service ==========" -ForegroundColor Yellow

$existingService = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if ($existingService) {
    Write-Warn "Service '$ServiceName' exists (Status: $($existingService.Status))"
    Write-Info "Stopping and removing old service..."
    
    if ($existingService.Status -eq 'Running') {
        Stop-Service -Name $ServiceName -Force -ErrorAction SilentlyContinue
        Start-Sleep -Seconds 2
    }
    
    & $NssmPath remove $ServiceName confirm 2>$null
    Start-Sleep -Seconds 1
    Write-Ok "Old service removed"
} else {
    Write-Info "Service '$ServiceName' not found. Will create new."
}

# ========== STEP 3: Install new service ==========
Write-Host ""
Write-Host "========== STEP 3: Install new service ==========" -ForegroundColor Yellow

Write-Info "Installing service..."
& $NssmPath install $ServiceName $BackendExePath
if ($LASTEXITCODE -ne 0) {
    Write-Err "Cannot install service"
    exit 5
}

Write-Info "Configuring service..."

& $NssmPath set $ServiceName AppDirectory $BackendDir
& $NssmPath set $ServiceName DisplayName $ServiceDisplayName
& $NssmPath set $ServiceName Description $ServiceDescription
& $NssmPath set $ServiceName Start SERVICE_AUTO_START
& $NssmPath set $ServiceName AppExit Default Restart
& $NssmPath set $ServiceName AppRestartDelay 5000

$logDir = Join-Path $BackendDir 'logs'
if (-not (Test-Path $logDir)) { New-Item -ItemType Directory -Path $logDir -Force | Out-Null }
& $NssmPath set $ServiceName AppStdout (Join-Path $logDir 'service-stdout.log')
& $NssmPath set $ServiceName AppStderr (Join-Path $logDir 'service-stderr.log')
& $NssmPath set $ServiceName AppStdoutCreationDisposition 4
& $NssmPath set $ServiceName AppStderrCreationDisposition 4
& $NssmPath set $ServiceName AppRotateFiles 1
& $NssmPath set $ServiceName AppRotateBytes 10485760

Write-Ok "Service configured"

# ========== STEP 4: Start service ==========
Write-Host ""
Write-Host "========== STEP 4: Start service ==========" -ForegroundColor Yellow

Write-Info "Starting service..."
Start-Service -Name $ServiceName -ErrorAction SilentlyContinue
Start-Sleep -Seconds 3

$svc = Get-Service -Name $ServiceName
if ($svc.Status -eq 'Running') {
    Write-Ok "Service '$ServiceName' is running!"
} else {
    Write-Warn "Service status: $($svc.Status). Check logs at: $logDir"
}

# ========== DONE ==========
Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "     INSTALLATION COMPLETE!            " -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Service info:" -ForegroundColor Yellow
Write-Host "  Name         : $ServiceName"
Write-Host "  Display      : $ServiceDisplayName"
Write-Host "  Executable   : $BackendExePath"
Write-Host "  Log folder   : $logDir"
Write-Host ""
Write-Host "Management commands:" -ForegroundColor Yellow
Write-Host "  Status  : Get-Service -Name '$ServiceName'"
Write-Host "  Stop    : Stop-Service -Name '$ServiceName'"
Write-Host "  Start   : Start-Service -Name '$ServiceName'"
Write-Host "  Restart : Restart-Service -Name '$ServiceName'"
Write-Host "  Remove  : $NssmPath remove $ServiceName confirm"
Write-Host ""
