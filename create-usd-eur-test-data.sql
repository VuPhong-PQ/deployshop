-- Script tạo test data cho USD và EUR
-- Chỉ chạy script này nếu bạn muốn tạo data test

INSERT INTO Orders (OrderNumber, CustomerName, PaymentMethod, Currency, PaymentStatus, Status, TotalAmount, CreatedAt, UpdatedAt)
VALUES 
('TEST-USD-001', 'Khách test USD', 'banktransfer', 'USD', 'paid', 'completed', 720000, GETDATE(), GETDATE()),
('TEST-USD-002', 'Khách test USD 2', 'banktransfer', 'USD', 'paid', 'completed', 480000, GETDATE(), GETDATE()),
('TEST-EUR-001', 'Khách test EUR', 'banktransfer', 'EUR', 'paid', 'completed', 160000, GETDATE(), GETDATE()),
('TEST-EUR-002', 'Khách test EUR 2', 'banktransfer', 'EUR', 'paid', 'completed', 160000, GETDATE(), GETDATE()),
('TEST-CASH-001', 'Khách test tiền mặt', 'cash', NULL, 'paid', 'completed', 480000, GETDATE(), GETDATE());

-- Lấy OrderId của các đơn vừa tạo để tạo OrderItem
DECLARE @OrderId1 INT = (SELECT OrderId FROM Orders WHERE OrderNumber = 'TEST-USD-001');
DECLARE @OrderId2 INT = (SELECT OrderId FROM Orders WHERE OrderNumber = 'TEST-USD-002'); 
DECLARE @OrderId3 INT = (SELECT OrderId FROM Orders WHERE OrderNumber = 'TEST-EUR-001');
DECLARE @OrderId4 INT = (SELECT OrderId FROM Orders WHERE OrderNumber = 'TEST-EUR-002');
DECLARE @OrderId5 INT = (SELECT OrderId FROM Orders WHERE OrderNumber = 'TEST-CASH-001');

-- Tạo OrderItem cho các đơn hàng test
INSERT INTO OrderItems (OrderId, ProductId, ProductName, Quantity, Price, TotalPrice)
VALUES 
(@OrderId1, 1, 'Sản phẩm test USD 1', 5, 144000, 720000),
(@OrderId2, 2, 'Sản phẩm test USD 2', 6, 80000, 480000),
(@OrderId3, 3, 'Sản phẩm test EUR 1', 2, 80000, 160000),
(@OrderId4, 4, 'Sản phẩm test EUR 2', 2, 80000, 160000),
(@OrderId5, 5, 'Sản phẩm test tiền mặt', 6, 80000, 480000);

PRINT 'Đã tạo test data thành công!';
PRINT 'Bây giờ hãy vào báo cáo để xem USD và EUR hiển thị riêng biệt';