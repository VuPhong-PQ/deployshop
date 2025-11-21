-- Insert trực tiếp test data với "ngoại tệ" payment method
-- Chạy script này trong SQL Server Management Studio

USE RetailPoint;

-- Insert orders với "ngoại tệ" payment method
DECLARE @timestamp NVARCHAR(20) = FORMAT(GETDATE(), 'yyyyMMddHHmmss');

INSERT INTO Orders (OrderNumber, CustomerName, PaymentMethod, Currency, PaymentStatus, Status, TotalAmount, CreatedAt, UpdatedAt)
VALUES 
('NGOAI-TE-USD-' + @timestamp + '-1', 'Khách test ngoại tệ USD 1', 'ngoại tệ', 'USD', 'paid', 'completed', 720000, GETDATE(), GETDATE()),
('NGOAI-TE-USD-' + @timestamp + '-2', 'Khách test ngoại tệ USD 2', 'ngoại tệ', 'USD', 'paid', 'completed', 480000, GETDATE(), GETDATE()),
('NGOAI-TE-EUR-' + @timestamp + '-1', 'Khách test ngoại tệ EUR 1', 'ngoại tệ', 'EUR', 'paid', 'completed', 160000, GETDATE(), GETDATE()),
('NGOAI-TE-EUR-' + @timestamp + '-2', 'Khách test ngoại tệ EUR 2', 'ngoại tệ', 'EUR', 'paid', 'completed', 160000, GETDATE(), GETDATE());

-- Lấy OrderId của các đơn vừa tạo
DECLARE @UsdOrder1 INT = (SELECT TOP 1 OrderId FROM Orders WHERE OrderNumber LIKE 'NGOAI-TE-USD-' + @timestamp + '-1%' ORDER BY CreatedAt DESC);
DECLARE @UsdOrder2 INT = (SELECT TOP 1 OrderId FROM Orders WHERE OrderNumber LIKE 'NGOAI-TE-USD-' + @timestamp + '-2%' ORDER BY CreatedAt DESC);
DECLARE @EurOrder1 INT = (SELECT TOP 1 OrderId FROM Orders WHERE OrderNumber LIKE 'NGOAI-TE-EUR-' + @timestamp + '-1%' ORDER BY CreatedAt DESC);
DECLARE @EurOrder2 INT = (SELECT TOP 1 OrderId FROM Orders WHERE OrderNumber LIKE 'NGOAI-TE-EUR-' + @timestamp + '-2%' ORDER BY CreatedAt DESC);

-- Tạo OrderItems
INSERT INTO OrderItems (OrderId, ProductId, ProductName, Quantity, Price, TotalPrice)
VALUES 
(@UsdOrder1, 1, 'Sản phẩm ngoại tệ USD Test 1', 5, 144000, 720000),
(@UsdOrder2, 2, 'Sản phẩm ngoại tệ USD Test 2', 6, 80000, 480000),
(@EurOrder1, 3, 'Sản phẩm ngoại tệ EUR Test 1', 2, 80000, 160000),
(@EurOrder2, 4, 'Sản phẩm ngoại tệ EUR Test 2', 2, 80000, 160000);

-- Kiểm tra kết quả
SELECT 'Dữ liệu sau khi thêm test ngoại tệ' as Info;
SELECT 
    PaymentMethod, 
    Currency, 
    COUNT(*) as OrderCount, 
    SUM(TotalAmount) as TotalAmount
FROM Orders 
WHERE Status = 'completed' AND PaymentStatus = 'paid'
GROUP BY PaymentMethod, Currency
ORDER BY PaymentMethod, Currency;

PRINT 'Đã thêm test data ngoại tệ thành công!';
PRINT 'Bây giờ restart IIS và kiểm tra báo cáo để xem USD/EUR hiển thị riêng biệt';