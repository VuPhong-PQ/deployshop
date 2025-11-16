-- Script xóa dữ liệu đơn hàng và liên quan
-- Giữ lại cấu trúc bảng, chỉ xóa dữ liệu

USE RetailPointDB;

-- Tắt foreign key constraints tạm thời
EXEC sp_MSforeachtable "ALTER TABLE ? NOCHECK CONSTRAINT all";

-- Xóa dữ liệu theo thứ tự an toàn (từ child đến parent)

-- 1. Xóa chi tiết đơn hàng trước
DELETE FROM OrderItems;
PRINT 'Đã xóa OrderItems';

-- 2. Xóa thông báo liên quan đến đơn hàng
DELETE FROM Notifications WHERE OrderId IS NOT NULL;
PRINT 'Đã xóa Notifications';

-- 3. Xóa hóa đơn điện tử items
DELETE FROM EInvoiceItems;
PRINT 'Đã xóa EInvoiceItems';

-- 4. Xóa hóa đơn điện tử
DELETE FROM EInvoices;
PRINT 'Đã xóa EInvoices';

-- 5. Xóa giảm giá đơn hàng
DELETE FROM OrderDiscounts;
PRINT 'Đã xóa OrderDiscounts';

-- 6. Xóa giao dịch thanh toán
DELETE FROM PaymentTransactions;
PRINT 'Đã xóa PaymentTransactions';

-- 7. Xóa tất cả đơn hàng
DELETE FROM Orders;
PRINT 'Đã xóa tất cả Orders';

-- 8. Reset identity của các bảng về 0
DBCC CHECKIDENT ('Orders', RESEED, 0);
DBCC CHECKIDENT ('OrderItems', RESEED, 0);
DBCC CHECKIDENT ('EInvoices', RESEED, 0);
DBCC CHECKIDENT ('EInvoiceItems', RESEED, 0);
DBCC CHECKIDENT ('OrderDiscounts', RESEED, 0);
DBCC CHECKIDENT ('PaymentTransactions', RESEED, 0);
DBCC CHECKIDENT ('Notifications', RESEED, 0);
PRINT 'Đã reset identity counters';

-- Bật lại foreign key constraints
EXEC sp_MSforeachtable "ALTER TABLE ? WITH CHECK CHECK CONSTRAINT all";

-- Kiểm tra kết quả
SELECT 'Orders' as TableName, COUNT(*) as RecordCount FROM Orders
UNION ALL
SELECT 'OrderItems', COUNT(*) FROM OrderItems
UNION ALL
SELECT 'EInvoices', COUNT(*) FROM EInvoices
UNION ALL
SELECT 'PaymentTransactions', COUNT(*) FROM PaymentTransactions;

PRINT '✅ HOÀN THÀNH XÓA DỮ LIỆU ĐƠN HÀNG!';