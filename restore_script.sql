USE master;

-- Đóng tất cả kết nối đến database nếu tồn tại
IF EXISTS(SELECT name FROM sys.databases WHERE name = 'RetailPoint')
BEGIN
    ALTER DATABASE [RetailPoint] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
END

-- Restore database từ backup
RESTORE DATABASE [RetailPoint] 
FROM DISK = 'c:\temp\RetailPoint_backup_20251117_134653.bak'
WITH REPLACE,
     RECOVERY,
     STATS = 10;

-- Đặt lại database về multi-user mode
ALTER DATABASE [RetailPoint] SET MULTI_USER;

-- Kiểm tra database đã được restore thành công
SELECT 
    name as 'Database Name',
    create_date as 'Creation Date',
    collation_name as 'Collation',
    state_desc as 'State'
FROM sys.databases 
WHERE name = 'RetailPoint';

PRINT 'Database restore completed successfully!';