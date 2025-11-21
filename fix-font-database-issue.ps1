# Script để khắc phục lỗi font và database sau khi xóa nhầm bảng
# Thực hiện các bước khắc phục

Write-Host "=== KHẮC PHỤC LỖI FONT VÀ DATABASE ===" -ForegroundColor Green
Write-Host "Nguyên nhân: Xóa nhầm bảng ActivityLogs, AuditLogs không tồn tại" -ForegroundColor Yellow

# 1. Build backend mới với fix
Write-Host "`n1. Building backend..." -ForegroundColor Cyan
Set-Location "c:\shop\Backend\RetailPointBackend"
dotnet build --configuration Release
if ($LASTEXITCODE -eq 0) {
    Write-Host "✓ Backend build thành công" -ForegroundColor Green
} else {
    Write-Host "✗ Backend build thất bại" -ForegroundColor Red
    exit 1
}

# 2. Publish backend
Write-Host "`n2. Publishing backend..." -ForegroundColor Cyan
dotnet publish --configuration Release --output "bin/Release/publish"
if ($LASTEXITCODE -eq 0) {
    Write-Host "✓ Backend publish thành công" -ForegroundColor Green
} else {
    Write-Host "✗ Backend publish thất bại" -ForegroundColor Red
    exit 1
}

# 3. Kiểm tra và khôi phục database nếu cần
Write-Host "`n3. Kiểm tra database..." -ForegroundColor Cyan
$sqlScript = @"
-- Kiểm tra bảng Notifications
SELECT COUNT(*) as NotificationTableExists FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Notifications';

-- Kiểm tra collation database
SELECT name, collation_name FROM sys.databases WHERE name = 'RetailPoint';
"@

# Tạo file SQL tạm
$sqlScript | Out-File -FilePath "temp_check.sql" -Encoding UTF8
Write-Host "Script SQL đã được tạo: temp_check.sql"

Write-Host "`n4. Hướng dẫn tiếp theo:" -ForegroundColor Cyan
Write-Host "- Chạy script SQL: restore_missing_tables.sql trên SQL Server" -ForegroundColor White
Write-Host "- Copy file publish backend lên server IIS" -ForegroundColor White
Write-Host "- Restart IIS Application Pool" -ForegroundColor White
Write-Host "- Kiểm tra website tại: http://101.53.9.76" -ForegroundColor White

# 5. Tạo backup trước khi thay đổi
Write-Host "`n5. Backup current backend..." -ForegroundColor Cyan
$backupDir = "backup_$(Get-Date -Format 'yyyyMMdd_HHmmss')"
New-Item -ItemType Directory -Path $backupDir -Force
Copy-Item "bin/Release/publish/*" -Destination $backupDir -Recurse -Force
Write-Host "✓ Backup saved to: $backupDir" -ForegroundColor Green

Write-Host "`n=== SCRIPT HOÀN THÀNH ===" -ForegroundColor Green
Write-Host "Lỗi đã được khắc phục:" -ForegroundColor Yellow
Write-Host "- Removed reference to non-existent tables (ActivityLogs, AuditLogs)" -ForegroundColor White
Write-Host "- Fixed DataManagementController" -ForegroundColor White
Write-Host "- Created restoration script for Notifications table" -ForegroundColor White
Write-Host "`nHãy deploy backend publish folder lên server IIS để áp dụng thay đổi." -ForegroundColor Cyan