-- Script tạo dữ liệu test cho discount system
USE RetailPoint;
GO

-- Tạo discount records trước
INSERT INTO Discounts (Name, Type, Value, IsActive, IsDeleted, CreatedAt, CreatedBy)
VALUES 
(N'Giảm giá 10%', 0, 10, 1, 0, GETDATE(), 1),
(N'Giảm giá 100k', 2, 100000, 1, 0, GETDATE(), 1),
(N'Giảm giá item 50k', 1, 50000, 1, 0, GETDATE(), 1);

-- Thêm đơn hàng test 1 (có giảm giá 10% tổng bill)
INSERT INTO Orders (CustomerName, PaymentMethod, PaymentStatus, Status, SubTotal, TaxAmount, DiscountAmount, TotalAmount, CreatedAt)
VALUES (N'Khách hàng test 1', 'cash', 'paid', 'completed', 500000, 0, 50000, 450000, DATEADD(day, -5, GETDATE()));

DECLARE @Order1Id INT = SCOPE_IDENTITY();

-- Thêm order items cho đơn hàng 1
INSERT INTO OrderItems (OrderId, ProductId, ProductName, Price, Quantity, TotalPrice, FinalPrice, DiscountAmount)
VALUES 
(@Order1Id, 1, N'Sản phẩm A', 300000, 1, 300000, 270000, 30000),
(@Order1Id, 1, N'Sản phẩm B', 200000, 1, 200000, 180000, 20000);

-- Thêm order discount cho đơn hàng 1
INSERT INTO OrderDiscounts (OrderId, DiscountId, DiscountType, DiscountValue, DiscountAmount, DiscountName, AppliedBy, AppliedAt)
VALUES (@Order1Id, 1, 0, 10, 50000, N'Giảm giá 10% tổng bill', 1, DATEADD(day, -5, GETDATE()));

-- Thêm đơn hàng test 2 (có giảm giá cố định 100k)
INSERT INTO Orders (CustomerName, PaymentMethod, PaymentStatus, Status, SubTotal, TaxAmount, DiscountAmount, TotalAmount, CreatedAt)
VALUES (N'Khách hàng test 2', 'card', 'paid', 'completed', 800000, 0, 100000, 700000, DATEADD(day, -3, GETDATE()));

DECLARE @Order2Id INT = SCOPE_IDENTITY();

-- Thêm order items cho đơn hàng 2
INSERT INTO OrderItems (OrderId, ProductId, ProductName, Price, Quantity, TotalPrice, FinalPrice, DiscountAmount)
VALUES (@Order2Id, 1, N'Sản phẩm C', 800000, 1, 800000, 700000, 100000);

-- Thêm order discount cho đơn hàng 2
INSERT INTO OrderDiscounts (OrderId, DiscountId, DiscountType, DiscountValue, DiscountAmount, DiscountName, AppliedBy, AppliedAt)
VALUES (@Order2Id, 2, 2, 100000, 100000, N'Giảm giá 100k cho tổng bill', 1, DATEADD(day, -3, GETDATE()));

-- Thêm đơn hàng test 3 (không có giảm giá)
INSERT INTO Orders (CustomerName, PaymentMethod, PaymentStatus, Status, SubTotal, TaxAmount, DiscountAmount, TotalAmount, CreatedAt)
VALUES (N'Khách hàng test 3', 'cash', 'paid', 'completed', 300000, 0, 0, 300000, DATEADD(day, -1, GETDATE()));

DECLARE @Order3Id INT = SCOPE_IDENTITY();

-- Thêm order items cho đơn hàng 3
INSERT INTO OrderItems (OrderId, ProductId, ProductName, Price, Quantity, TotalPrice, FinalPrice, DiscountAmount)
VALUES (@Order3Id, 1, N'Sản phẩm D', 300000, 1, 300000, 300000, 0);

-- Thêm đơn hàng test 4 (có giảm giá item cố định)
INSERT INTO Orders (CustomerName, PaymentMethod, PaymentStatus, Status, SubTotal, TaxAmount, DiscountAmount, TotalAmount, CreatedAt)
VALUES (N'Khách hàng test 4', 'cash', 'paid', 'completed', 600000, 0, 50000, 550000, DATEADD(day, -2, GETDATE()));

DECLARE @Order4Id INT = SCOPE_IDENTITY();

-- Thêm order items cho đơn hàng 4
INSERT INTO OrderItems (OrderId, ProductId, ProductName, Price, Quantity, TotalPrice, FinalPrice, DiscountAmount)
VALUES 
(@Order4Id, 1, N'Sản phẩm E', 400000, 1, 400000, 370000, 30000),
(@Order4Id, 1, N'Sản phẩm F', 200000, 1, 200000, 180000, 20000);

-- Thêm order discount cho đơn hàng 4
INSERT INTO OrderDiscounts (OrderId, DiscountId, DiscountType, DiscountValue, DiscountAmount, DiscountName, AppliedBy, AppliedAt)
VALUES (@Order4Id, 3, 1, 50000, 50000, N'Giảm giá 50k cho từng item', 1, DATEADD(day, -2, GETDATE()));

PRINT 'Đã tạo thành công 4 đơn hàng test với các loại giảm giá khác nhau';
PRINT 'Order 1: Giảm giá 10% tổng bill';
PRINT 'Order 2: Giảm giá cố định 100k tổng bill';
PRINT 'Order 3: Không có giảm giá';
PRINT 'Order 4: Giảm giá cố định cho từng item';