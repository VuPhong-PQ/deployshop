-- Script đơn giản tạo dữ liệu test cho discount system  
USE RetailPoint;
GO

-- Thêm đơn hàng test 1 (có giảm giá 10% tổng bill)
INSERT INTO Orders (CustomerName, PaymentMethod, PaymentStatus, Status, SubTotal, TaxAmount, DiscountAmount, TotalAmount, CreatedAt)
VALUES (N'Khách hàng test 1', 'cash', 'paid', 'completed', 500000, 0, 50000, 450000, DATEADD(day, -5, GETDATE()));

DECLARE @Order1Id INT = SCOPE_IDENTITY();

-- Thêm order items cho đơn hàng 1
INSERT INTO OrderItems (OrderId, ProductId, ProductName, Price, Quantity, TotalPrice, FinalPrice, DiscountAmount)
VALUES 
(@Order1Id, 1, N'Sản phẩm A', 300000, 1, 300000, 270000, 30000),
(@Order1Id, 1, N'Sản phẩm B', 200000, 1, 200000, 180000, 20000);

-- Thêm order discount cho đơn hàng 1 (không dùng foreign key DiscountId)
INSERT INTO OrderDiscounts (OrderId, DiscountType, DiscountValue, DiscountAmount, DiscountName, AppliedBy, AppliedAt)
VALUES (@Order1Id, 0, 10, 50000, N'Giảm giá 10% tổng bill', 1, DATEADD(day, -5, GETDATE()));

-- Thêm đơn hàng test 2 (có giảm giá cố định 100k)
INSERT INTO Orders (CustomerName, PaymentMethod, PaymentStatus, Status, SubTotal, TaxAmount, DiscountAmount, TotalAmount, CreatedAt)
VALUES (N'Khách hàng test 2', 'card', 'paid', 'completed', 800000, 0, 100000, 700000, DATEADD(day, -3, GETDATE()));

DECLARE @Order2Id INT = SCOPE_IDENTITY();

-- Thêm order items cho đơn hàng 2
INSERT INTO OrderItems (OrderId, ProductId, ProductName, Price, Quantity, TotalPrice, FinalPrice, DiscountAmount)
VALUES (@Order2Id, 1, N'Sản phẩm C', 800000, 1, 800000, 700000, 100000);

-- Thêm order discount cho đơn hàng 2
INSERT INTO OrderDiscounts (OrderId, DiscountType, DiscountValue, DiscountAmount, DiscountName, AppliedBy, AppliedAt)
VALUES (@Order2Id, 2, 100000, 100000, N'Giảm giá 100k cho tổng bill', 1, DATEADD(day, -3, GETDATE()));

-- Thêm đơn hàng test 3 (không có giảm giá)
INSERT INTO Orders (CustomerName, PaymentMethod, PaymentStatus, Status, SubTotal, TaxAmount, DiscountAmount, TotalAmount, CreatedAt)
VALUES (N'Khách hàng test 3', 'cash', 'paid', 'completed', 300000, 0, 0, 300000, DATEADD(day, -1, GETDATE()));

DECLARE @Order3Id INT = SCOPE_IDENTITY();

-- Thêm order items cho đơn hàng 3
INSERT INTO OrderItems (OrderId, ProductId, ProductName, Price, Quantity, TotalPrice, FinalPrice, DiscountAmount)
VALUES (@Order3Id, 1, N'Sản phẩm D', 300000, 1, 300000, 300000, 0);

PRINT 'Đã tạo thành công 3 đơn hàng test với các loại giảm giá khác nhau';
PRINT 'Order 1: Giảm giá 10% tổng bill';
PRINT 'Order 2: Giảm giá cố định 100k tổng bill';
PRINT 'Order 3: Không có giảm giá';