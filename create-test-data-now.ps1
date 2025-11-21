# Tạo test data USD/EUR trực tiếp vào SQL Server RetailPoint
Write-Host "Tạo test data USD/EUR cho database RetailPoint..." -ForegroundColor Green

$connectionString = "Server=TEST-PC\KTEAM;Database=RetailPoint;User Id=sa;Password=sa@123;TrustServerCertificate=True;"

try {
    # Import SQL Server PowerShell module
    Import-Module SqlServer -ErrorAction Stop
    
    # Tạo test data
    Write-Host "Đang tạo test data..." -ForegroundColor Yellow
    
    $sql = @"
-- Kiểm tra dữ liệu hiện tại
SELECT 'Dữ liệu hiện tại' as Info;
SELECT PaymentMethod, Currency, COUNT(*) as Count, SUM(TotalAmount) as Total
FROM Orders 
WHERE Status = 'completed' AND PaymentStatus = 'paid'
GROUP BY PaymentMethod, Currency;

-- Tạo test data với timestamp để tránh trùng
DECLARE @timestamp NVARCHAR(20) = FORMAT(GETDATE(), 'yyyyMMddHHmmss');

INSERT INTO Orders (OrderNumber, CustomerName, PaymentMethod, Currency, PaymentStatus, Status, TotalAmount, CreatedAt, UpdatedAt)
VALUES 
('USD-' + @timestamp + '-1', 'Khách test USD 1', 'banktransfer', 'USD', 'paid', 'completed', 720000, GETDATE(), GETDATE()),
('USD-' + @timestamp + '-2', 'Khách test USD 2', 'banktransfer', 'USD', 'paid', 'completed', 480000, GETDATE(), GETDATE()),
('EUR-' + @timestamp + '-1', 'Khách test EUR 1', 'banktransfer', 'EUR', 'paid', 'completed', 160000, GETDATE(), GETDATE()),
('EUR-' + @timestamp + '-2', 'Khách test EUR 2', 'banktransfer', 'EUR', 'paid', 'completed', 160000, GETDATE(), GETDATE()),
('CASH-' + @timestamp, 'Khách test tiền mặt', 'cash', NULL, 'paid', 'completed', 480000, GETDATE(), GETDATE());

-- Lấy OrderId của các đơn vừa tạo
DECLARE @UsdOrder1 INT = (SELECT TOP 1 OrderId FROM Orders WHERE OrderNumber LIKE 'USD-' + @timestamp + '-1%' ORDER BY CreatedAt DESC);
DECLARE @UsdOrder2 INT = (SELECT TOP 1 OrderId FROM Orders WHERE OrderNumber LIKE 'USD-' + @timestamp + '-2%' ORDER BY CreatedAt DESC);
DECLARE @EurOrder1 INT = (SELECT TOP 1 OrderId FROM Orders WHERE OrderNumber LIKE 'EUR-' + @timestamp + '-1%' ORDER BY CreatedAt DESC);
DECLARE @EurOrder2 INT = (SELECT TOP 1 OrderId FROM Orders WHERE OrderNumber LIKE 'EUR-' + @timestamp + '-2%' ORDER BY CreatedAt DESC);
DECLARE @CashOrder INT = (SELECT TOP 1 OrderId FROM Orders WHERE OrderNumber LIKE 'CASH-' + @timestamp + '%' ORDER BY CreatedAt DESC);

-- Tạo OrderItems
INSERT INTO OrderItems (OrderId, ProductId, ProductName, Quantity, Price, TotalPrice)
VALUES 
(@UsdOrder1, 1, 'Sản phẩm USD Test 1', 5, 144000, 720000),
(@UsdOrder2, 2, 'Sản phẩm USD Test 2', 6, 80000, 480000),
(@EurOrder1, 3, 'Sản phẩm EUR Test 1', 2, 80000, 160000),
(@EurOrder2, 4, 'Sản phẩm EUR Test 2', 2, 80000, 160000),
(@CashOrder, 5, 'Sản phẩm Cash Test', 6, 80000, 480000);

-- Kiểm tra kết quả sau khi tạo
SELECT 'Dữ liệu sau khi tạo' as Info;
SELECT PaymentMethod, Currency, COUNT(*) as Count, SUM(TotalAmount) as Total
FROM Orders 
WHERE Status = 'completed' AND PaymentStatus = 'paid'
GROUP BY PaymentMethod, Currency;

PRINT 'Đã tạo test data thành công!';
"@

    # Thực hiện SQL
    Invoke-Sqlcmd -ConnectionString $connectionString -Query $sql -Verbose
    
    Write-Host "✅ Đã tạo test data thành công!" -ForegroundColor Green
    Write-Host "Bây giờ hãy:" -ForegroundColor Cyan
    Write-Host "1. Truy cập http://101.53.9.76:5273/api/PaymentStats để kiểm tra API" -ForegroundColor White
    Write-Host "2. Hoặc mở file test-usd-eur-api.html trong browser" -ForegroundColor White
    Write-Host "3. Kiểm tra báo cáo trong ứng dụng frontend" -ForegroundColor White

} catch {
    Write-Host "❌ Lỗi: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Hãy thử chạy thủ công SQL trong SSMS:" -ForegroundColor Yellow
    Write-Host "- Mở SQL Server Management Studio" -ForegroundColor Gray
    Write-Host "- Kết nối đến TEST-PC\KTEAM" -ForegroundColor Gray
    Write-Host "- Chọn database RetailPoint" -ForegroundColor Gray
    Write-Host "- Chạy file create-usd-eur-test-data.sql" -ForegroundColor Gray
}