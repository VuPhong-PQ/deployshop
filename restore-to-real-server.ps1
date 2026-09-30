$bakFile = "C:\shop\server\Backups\Auto\RetailPoint_auto_backup_20260909_101200.bak"
$serverInstance = "W-BWP-IT-ITM\SQLEXPRESS"
$databaseName = "RetailPoint"

Write-Host "=========================================================================" -ForegroundColor Cyan
Write-Host " KHOI PHUC DATABASE TU BACKUP MOI NHAT: $bakFile " -ForegroundColor Cyan
Write-Host " Server Instance: $serverInstance " -ForegroundColor Cyan
Write-Host " Database Target: $databaseName " -ForegroundColor Cyan
Write-Host "=========================================================================" -ForegroundColor Cyan

if (-not (Test-Path $bakFile)) {
    Write-Host "Loi: File backup khong ton tai" -ForegroundColor Red
    exit 1
}

$connStr = "Data Source=$serverInstance;Initial Catalog=master;Integrated Security=True;TrustServerCertificate=True;"
$conn = New-Object System.Data.SqlClient.SqlConnection($connStr)

try {
    $conn.Open()
    Write-Host "Ket noi thanh cong toi SQL Server: $serverInstance" -ForegroundColor Green

    # 1. Get logical file names
    $cmdFileList = $conn.CreateCommand()
    $cmdFileList.CommandText = "RESTORE FILELISTONLY FROM DISK = N'$bakFile'"
    $reader = $cmdFileList.ExecuteReader()

    $logicalData = "RetailPoint"
    $logicalLog = "RetailPoint_log"

    while ($reader.Read()) {
        $logicalName = $reader["LogicalName"]
        $type = $reader["Type"]
        if ($type -eq "D") {
            $logicalData = $logicalName
        } elseif ($type -eq "L") {
            $logicalLog = $logicalName
        }
    }
    $reader.Close()

    Write-Host " - Logical Data File: $logicalData" -ForegroundColor Yellow
    Write-Host " - Logical Log File:  $logicalLog" -ForegroundColor Yellow

    # 2. Get default data path
    $cmdPath = $conn.CreateCommand()
    $cmdPath.CommandText = "SELECT CAST(SERVERPROPERTY('InstanceDefaultDataPath') AS NVARCHAR(MAX)) AS DataPath, CAST(SERVERPROPERTY('InstanceDefaultLogPath') AS NVARCHAR(MAX)) AS LogPath"
    $rPath = $cmdPath.ExecuteReader()
    $dataPath = ""
    $logPath = ""
    if ($rPath.Read()) {
        $dataPath = $rPath["DataPath"]
        $logPath = $rPath["LogPath"]
    }
    $rPath.Close()

    if (-not $dataPath) {
        $dataPath = "C:\Program Files\Microsoft SQL Server\MSSQL16.SQLEXPRESS\MSSQL\DATA\"
        $logPath = $dataPath
    }

    $mdfPath = Join-Path $dataPath "$databaseName.mdf"
    $ldfPath = Join-Path $logPath "${databaseName}_log.ldf"

    Write-Host " Target MDF: $mdfPath" -ForegroundColor Gray
    Write-Host " Target LDF: $ldfPath" -ForegroundColor Gray

    # 3. Set SINGLE_USER if database exists
    Write-Host "Dang chuyen Database '$databaseName' sang SINGLE_USER..." -ForegroundColor Yellow
    $cmdSingle = $conn.CreateCommand()
    $cmdSingle.CommandText = "IF EXISTS (SELECT name FROM sys.databases WHERE name = '$databaseName') BEGIN ALTER DATABASE [$databaseName] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; END"
    $cmdSingle.ExecuteNonQuery() | Out-Null

    # 4. RESTORE DATABASE
    Write-Host "Dang khoi phuc du lieu..." -ForegroundColor Yellow
    $cmdRestore = $conn.CreateCommand()
    $cmdRestore.CommandTimeout = 600
    $cmdRestore.CommandText = "RESTORE DATABASE [$databaseName] FROM DISK = N'$bakFile' WITH MOVE N'$logicalData' TO N'$mdfPath', MOVE N'$logicalLog' TO N'$ldfPath', RECOVERY, REPLACE; ALTER DATABASE [$databaseName] SET MULTI_USER;"
    $cmdRestore.ExecuteNonQuery() | Out-Null
    Write-Host "RESTORE DATABASE [$databaseName] THANH CONG 100%!" -ForegroundColor Green

    # 5. Check row counts
    $connDbStr = "Data Source=$serverInstance;Initial Catalog=$databaseName;Integrated Security=True;TrustServerCertificate=True;"
    $connDb = New-Object System.Data.SqlClient.SqlConnection($connDbStr)
    $connDb.Open()
    
    $cmdCheck = $connDb.CreateCommand()
    $cmdCheck.CommandText = "SELECT (SELECT COUNT(*) FROM Orders) AS OrderCount, (SELECT COUNT(*) FROM Products) AS ProductCount, (SELECT COUNT(*) FROM Staffs) AS StaffCount"
    $rCheck = $cmdCheck.ExecuteReader()
    if ($rCheck.Read()) {
        $oCount = $rCheck["OrderCount"]
        $pCount = $rCheck["ProductCount"]
        $sCount = $rCheck["StaffCount"]
        Write-Host "=========================================================================" -ForegroundColor Green
        Write-Host " THONG KE DU LIEU DA KHOI PHUC MOI NHAT (09/09/2026):" -ForegroundColor Green
        Write-Host " - So luong don hang (Orders):  $oCount don hang" -ForegroundColor White
        Write-Host " - So luong san pham (Products): $pCount san pham" -ForegroundColor White
        Write-Host " - So luong nhan vien (Staffs):  $sCount nhan vien" -ForegroundColor White
        Write-Host "=========================================================================" -ForegroundColor Green
    }
    $connDb.Close()

} catch {
    Write-Host "Loi khi Restore Database: $($_.Exception.Message)" -ForegroundColor Red
} finally {
    if ($conn.State -eq 'Open') { $conn.Close() }
}
