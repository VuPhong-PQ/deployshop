-- Script xóa tất cả đơn hàng và dữ liệu liên quan
-- ⚠️ CẨN THẬN: Script này sẽ xóa vĩnh viễn tất cả đơn hàng!

USE RetailPointDB;

-- Tắt tất cả foreign key constraints tạm thời
EXEC sp_MSforeachtable "ALTER TABLE ? NOCHECK CONSTRAINT all";

-- Xóa dữ liệu theo thứ tự an toàn (từ child tables đến parent tables)

-- 1. Xóa chi tiết đơn hàng
DELETE FROM OrderItems;
PRINT '✅ Đã xóa tất cả OrderItems';

-- 2. Xóa giảm giá đơn hàng
DELETE FROM OrderDiscounts;
PRINT '✅ Đã xóa tất cả OrderDiscounts';

-- 3. Xóa thông báo liên quan đến đơn hàng
DELETE FROM Notifications WHERE OrderId IS NOT NULL;
PRINT '✅ Đã xóa Notifications liên quan đến đơn hàng';

-- 4. Xóa hóa đơn điện tử items
DELETE FROM EInvoiceItems;
PRINT '✅ Đã xóa tất cả EInvoiceItems';

-- 5. Xóa hóa đơn điện tử
DELETE FROM EInvoices;
PRINT '✅ Đã xóa tất cả EInvoices';

-- 6. Xóa giao dịch thanh toán
DELETE FROM PaymentTransactions WHERE OrderId IS NOT NULL;
PRINT '✅ Đã xóa PaymentTransactions liên quan đến đơn hàng';

-- 7. Cuối cùng xóa tất cả đơn hàng
DELETE FROM Orders;
PRINT '✅ Đã xóa tất cả Orders';

-- 8. Reset identity counters về 0
DBCC CHECKIDENT ('Orders', RESEED, 0);
DBCC CHECKIDENT ('OrderItems', RESEED, 0);
DBCC CHECKIDENT ('OrderDiscounts', RESEED, 0);
DBCC CHECKIDENT ('EInvoices', RESEED, 0);
DBCC CHECKIDENT ('EInvoiceItems', RESEED, 0);
DBCC CHECKIDENT ('PaymentTransactions', RESEED, 0);
PRINT '✅ Đã reset tất cả identity counters';

-- Bật lại tất cả foreign key constraints
EXEC sp_MSforeachtable "ALTER TABLE ? WITH CHECK CHECK CONSTRAINT all";

-- Kiểm tra kết quả
SELECT 
    'Orders' as TableName, 
    COUNT(*) as RecordCount 
FROM Orders
UNION ALL
SELECT 'OrderItems', COUNT(*) FROM OrderItems
UNION ALL
SELECT 'OrderDiscounts', COUNT(*) FROM OrderDiscounts
UNION ALL
SELECT 'EInvoices', COUNT(*) FROM EInvoices
UNION ALL
SELECT 'PaymentTransactions', COUNT(*) FROM PaymentTransactions;

PRINT '🎉 HOÀN THÀNH! Đã xóa tất cả đơn hàng và dữ liệu liên quan.';