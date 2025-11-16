-- ⚠️⚠️⚠️ CẢNH BÁO: SCRIPT XÓA TẤT CẢ DỮ LIỆU ⚠️⚠️⚠️
-- SCRIPT NÀY SẼ XÓA TOÀN BỘ DỮ LIỆU TRONG DATABASE
-- KHÔNG THỂ KHÔI PHỤC SAU KHI THỰC HIỆN!
-- HÃY BACKUP TRƯỚC KHI CHẠY!

USE RetailPointDB;

-- Tắt tất cả foreign key constraints để tránh lỗi khi xóa
EXEC sp_MSforeachtable "ALTER TABLE ? NOCHECK CONSTRAINT all"

-- Xóa dữ liệu theo thứ tự (từ child tables đến parent tables)

-- Xóa dữ liệu chi tiết trước
DELETE FROM OrderItems;
DELETE FROM EInvoiceItems;
DELETE FROM OrderDiscounts;
DELETE FROM RolePermissions;
DELETE FROM PaymentTransactions;
DELETE FROM InventoryTransactions;
DELETE FROM InventoryMovements;
DELETE FROM Notifications;
DELETE FROM ActivityLogs;
DELETE FROM AuditLogs;

-- Xóa dữ liệu chính
DELETE FROM Orders;
DELETE FROM EInvoices;
DELETE FROM Customers;
DELETE FROM Products;
DELETE FROM ProductGroups;
DELETE FROM Discounts;
DELETE FROM Staffs;
DELETE FROM BackupHistories;
DELETE FROM SalesReports;
DELETE FROM DailySalesReports;
DELETE FROM MonthlySalesReports;
DELETE FROM ProductSalesReports;
DELETE FROM PaymentStats;
DELETE FROM Stores;
DELETE FROM StaffStores;

-- Xóa hệ thống permissions và roles (CẨN THẬN!)
-- DELETE FROM Permissions;
-- DELETE FROM Roles;

-- Reset identity columns về 1
DBCC CHECKIDENT ('Orders', RESEED, 0);
DBCC CHECKIDENT ('OrderItems', RESEED, 0);
DBCC CHECKIDENT ('Customers', RESEED, 0);
DBCC CHECKIDENT ('Products', RESEED, 0);
DBCC CHECKIDENT ('ProductGroups', RESEED, 0);
DBCC CHECKIDENT ('Discounts', RESEED, 0);
DBCC CHECKIDENT ('Staffs', RESEED, 0);
DBCC CHECKIDENT ('EInvoices', RESEED, 0);
DBCC CHECKIDENT ('EInvoiceItems', RESEED, 0);
DBCC CHECKIDENT ('Stores', RESEED, 0);

-- Bật lại tất cả foreign key constraints
EXEC sp_MSforeachtable "ALTER TABLE ? WITH CHECK CHECK CONSTRAINT all"

-- Kiểm tra số lượng records sau khi xóa
SELECT 
    'Orders' AS TableName, COUNT(*) AS RecordCount FROM Orders
UNION ALL
SELECT 'OrderItems', COUNT(*) FROM OrderItems
UNION ALL  
SELECT 'Customers', COUNT(*) FROM Customers
UNION ALL
SELECT 'Products', COUNT(*) FROM Products
UNION ALL
SELECT 'Staffs', COUNT(*) FROM Staffs
UNION ALL
SELECT 'Permissions', COUNT(*) FROM Permissions
UNION ALL
SELECT 'Roles', COUNT(*) FROM Roles;

PRINT '⚠️ HOÀN THÀNH XÓA TẤT CẢ DỮ LIỆU!';