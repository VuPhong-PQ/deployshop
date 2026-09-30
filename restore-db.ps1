$bakFile = "C:\shop\server\Backups\Auto\RetailPoint_auto_backup_20260909_101200.bak"
if (-not (Test-Path $bakFile)) {
    Write-Host "File backup not found: $bakFile" -ForegroundColor Red
    exit 1
}

$connStr = "Server=localhost;Database=master;Integrated Security=true;TrustServerCertificate=True;"
$conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
try {
    $conn.Open()
    Write-Host "Connected to SQL Server master!" -ForegroundColor Green

    # Get SQL Server data/log paths
    $cmdPath = $conn.CreateCommand()
    $cmdPath.CommandText = "SELECT CAST(SERVERPROPERTY('InstanceDefaultDataPath') AS NVARCHAR(MAX)) AS DataPath, CAST(SERVERPROPERTY('InstanceDefaultLogPath') AS NVARCHAR(MAX)) AS LogPath"
    $reader = $cmdPath.ExecuteReader()
    $dataPath = ""
    $logPath = ""
    if ($reader.Read()) {
        $dataPath = $reader["DataPath"]
        $logPath = $reader["LogPath"]
    }
    $reader.Close()

    if (-not $dataPath) {
        $dataPath = "C:\Program Files\Microsoft SQL Server\MSSQL15.MSSQLSERVER\MSSQL\DATA\"
        $logPath = $dataPath
    }

    $mdfPath = Join-Path $dataPath "RetailPoint.mdf"
    $ldfPath = Join-Path $logPath "RetailPoint_log.ldf"

    Write-Host "MDF Path: $mdfPath" -ForegroundColor Cyan
    Write-Host "LDF Path: $ldfPath" -ForegroundColor Cyan

    Write-Host "Restoring RetailPoint database from: $bakFile ..." -ForegroundColor Yellow
    $restoreCmd = $conn.CreateCommand()
    $restoreCmd.CommandTimeout = 300
    $sql = "RESTORE DATABASE [RetailPoint] FROM DISK = N'$bakFile' WITH MOVE N'RetailPoint' TO N'$mdfPath', MOVE N'RetailPoint_log' TO N'$ldfPath', RECOVERY, REPLACE;"
    $restoreCmd.CommandText = $sql
    $restoreCmd.ExecuteNonQuery() | Out-Null
    Write-Host "RetailPoint database restored successfully!" -ForegroundColor Green

} catch {
    Write-Host "SQL Restore Error: $($_.Exception.Message)" -ForegroundColor Red
} finally {
    if ($conn.State -eq 'Open') { $conn.Close() }
}
