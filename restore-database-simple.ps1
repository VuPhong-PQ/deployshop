# Script phục hồi database từ backup file - Phiên bản đơn giản
# File: c:\shop\restore-database-simple.ps1

param(
    [string]$BackupFilePath = "c:\temp\RetailPoint_backup_20251117_134653.bak",
    [string]$DatabaseName = "RetailPoint",
    [string]$ServerInstance = "TEST-PC\KTEAM",
    [string]$Username = "sa",
    [string]$Password = "sa@123"
)

Write-Host "=== PHỤC HỒI DATABASE RETAILPOINT ===" -ForegroundColor Green
Write-Host "Backup file: $BackupFilePath" -ForegroundColor Yellow
Write-Host "Target database: $DatabaseName" -ForegroundColor Yellow
Write-Host "SQL Server: $ServerInstance" -ForegroundColor Yellow

# Kiểm tra file backup có tồn tại không
if (!(Test-Path $BackupFilePath)) {
    Write-Host "❌ Lỗi: File backup không tồn tại: $BackupFilePath" -ForegroundColor Red
    exit 1
}

Write-Host "✅ File backup tồn tại" -ForegroundColor Green

# Tạo SQL script để restore
$RestoreSQL = @"
USE master;

-- Đóng tất cả kết nối đến database nếu tồn tại
IF EXISTS(SELECT name FROM sys.databases WHERE name = '$DatabaseName')
BEGIN
    ALTER DATABASE [$DatabaseName] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
END

-- Restore database từ backup
RESTORE DATABASE [$DatabaseName] 
FROM DISK = '$BackupFilePath'
WITH REPLACE,
     RECOVERY,
     STATS = 10;

-- Đặt lại database về multi-user mode
ALTER DATABASE [$DatabaseName] SET MULTI_USER;

-- Kiểm tra database đã được restore thành công
SELECT 
    name as 'Database Name',
    create_date as 'Creation Date',
    collation_name as 'Collation',
    state_desc as 'State'
FROM sys.databases 
WHERE name = '$DatabaseName';

PRINT 'Database restore completed successfully!';
"@

# Lưu SQL script vào file tạm
$TempSQLFile = "$env:TEMP\restore_retailpoint.sql"
$RestoreSQL | Out-File -FilePath $TempSQLFile -Encoding UTF8

Write-Host "📝 SQL script được tạo: $TempSQLFile" -ForegroundColor Cyan

Write-Host "🔄 Đang thực hiện restore database..." -ForegroundColor Yellow

# Thực hiện restore bằng SQLCMD
$sqlcmdArgs = @(
    "-S", $ServerInstance,
    "-U", $Username,
    "-P", $Password,
    "-i", $TempSQLFile,
    "-f", "65001"  # UTF-8 encoding
)

Write-Host "Chạy lệnh: sqlcmd $($sqlcmdArgs -join ' ')" -ForegroundColor Cyan

$result = Start-Process -FilePath "sqlcmd" -ArgumentList $sqlcmdArgs -Wait -PassThru -NoNewWindow -RedirectStandardOutput "$env:TEMP\restore_output.txt" -RedirectStandardError "$env:TEMP\restore_error.txt"

# Đọc kết quả
Write-Host "📄 Kết quả restore:" -ForegroundColor Cyan
if (Test-Path "$env:TEMP\restore_output.txt") {
    $output = Get-Content "$env:TEMP\restore_output.txt"
    $output | ForEach-Object { Write-Host $_ }
}

if (Test-Path "$env:TEMP\restore_error.txt") {
    $errors = Get-Content "$env:TEMP\restore_error.txt"
    if ($errors) {
        Write-Host "❌ Lỗi trong quá trình restore:" -ForegroundColor Red
        $errors | ForEach-Object { Write-Host $_ -ForegroundColor Red }
    }
}

if ($result.ExitCode -eq 0) {
    Write-Host "✅ Database đã được restore thành công!" -ForegroundColor Green
    
    # Kiểm tra kết nối đến database đã restore
    Write-Host "🔍 Kiểm tra kết nối đến database..." -ForegroundColor Yellow
    
    $TestConnectionSQL = "SELECT COUNT(*) as TableCount FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'"
    $testSqlFile = "$env:TEMP\test_connection.sql"
    $TestConnectionSQL | Out-File -FilePath $testSqlFile -Encoding UTF8
    
    $testArgs = @(
        "-S", $ServerInstance,
        "-U", $Username,
        "-P", $Password,
        "-d", $DatabaseName,
        "-i", $testSqlFile
    )
    
    $testResult = Start-Process -FilePath "sqlcmd" -ArgumentList $testArgs -Wait -PassThru -NoNewWindow -RedirectStandardOutput "$env:TEMP\test_output.txt"
    
    if (Test-Path "$env:TEMP\test_output.txt") {
        $testOutput = Get-Content "$env:TEMP\test_output.txt"
        Write-Host "📊 Kết quả kiểm tra: $($testOutput -join ' ')" -ForegroundColor Green
    }
} else {
    Write-Host "❌ Restore thất bại với exit code: $($result.ExitCode)" -ForegroundColor Red
}

# Dọn dẹp file tạm
if (Test-Path $TempSQLFile) { Remove-Item $TempSQLFile -Force }
if (Test-Path "$env:TEMP\restore_output.txt") { Remove-Item "$env:TEMP\restore_output.txt" -Force }
if (Test-Path "$env:TEMP\restore_error.txt") { Remove-Item "$env:TEMP\restore_error.txt" -Force }
if (Test-Path "$env:TEMP\test_connection.sql") { Remove-Item "$env:TEMP\test_connection.sql" -Force }
if (Test-Path "$env:TEMP\test_output.txt") { Remove-Item "$env:TEMP\test_output.txt" -Force }

Write-Host "=== HOÀN THÀNH PHỤC HỒI DATABASE ===" -ForegroundColor Green