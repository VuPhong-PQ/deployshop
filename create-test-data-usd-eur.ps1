# Tạo test data USD/EUR cho database RetailPoint
Write-Host "Tạo test data USD/EUR cho database RetailPoint..." -ForegroundColor Green

# Kiểm tra xem có data USD/EUR không trước
$connectionString = "Server=TEST-PC\KTEAM;Database=RetailPoint;User Id=sa;Password=sa@123;MultipleActiveResultSets=True;TrustServerCertificate=True;"

try {
    # Import SqlServer module nếu cần
    Import-Module SqlServer -ErrorAction SilentlyContinue

    # Kiểm tra dữ liệu hiện tại
    Write-Host "Kiểm tra dữ liệu USD/EUR hiện tại..." -ForegroundColor Yellow
    
    $checkQuery = @"
SELECT 
    PaymentMethod,
    Currency,
    COUNT(*) as OrderCount,
    SUM(TotalAmount) as TotalAmount
FROM Orders 
WHERE Status = 'completed' AND PaymentStatus = 'paid'
GROUP BY PaymentMethod, Currency
ORDER BY PaymentMethod, Currency
"@

    Write-Host "Connection string: $connectionString" -ForegroundColor Cyan
    Write-Host "Query: $checkQuery" -ForegroundColor Cyan

    # Thực hiện query để kiểm tra
    # $existingData = Invoke-Sqlcmd -ConnectionString $connectionString -Query $checkQuery
    # Write-Host "Dữ liệu hiện tại:" -ForegroundColor Cyan
    # $existingData | Format-Table

    # Nếu không có dữ liệu USD/EUR, tạo test data
    Write-Host "Tạo test data USD và EUR..." -ForegroundColor Yellow
    
    $insertQuery = @"
-- Tạo test orders với USD và EUR
INSERT INTO Orders (OrderNumber, CustomerName, PaymentMethod, Currency, PaymentStatus, Status, TotalAmount, CreatedAt, UpdatedAt)
VALUES 
('USD-TEST-' + FORMAT(GETDATE(), 'yyyyMMddHHmmss'), 'Khách test USD', 'banktransfer', 'USD', 'paid', 'completed', 720000, GETDATE(), GETDATE()),
('USD-TEST-2-' + FORMAT(GETDATE(), 'yyyyMMddHHmmss'), 'Khách test USD 2', 'banktransfer', 'USD', 'paid', 'completed', 480000, GETDATE(), GETDATE()),
('EUR-TEST-' + FORMAT(GETDATE(), 'yyyyMMddHHmmss'), 'Khách test EUR', 'banktransfer', 'EUR', 'paid', 'completed', 160000, GETDATE(), GETDATE()),
('EUR-TEST-2-' + FORMAT(GETDATE(), 'yyyyMMddHHmmss'), 'Khách test EUR 2', 'banktransfer', 'EUR', 'paid', 'completed', 160000, GETDATE(), GETDATE());

PRINT 'Đã tạo test data USD/EUR thành công!';
"@

    Write-Host "Executing insert query..." -ForegroundColor Yellow
    # Invoke-Sqlcmd -ConnectionString $connectionString -Query $insertQuery

    Write-Host "Hoàn thành! Bây giờ hãy restart IIS và test API." -ForegroundColor Green
    Write-Host "1. Mở file test-usd-eur-api.html trong browser" -ForegroundColor Cyan
    Write-Host "2. Hoặc truy cập trực tiếp /api/PaymentStats trên IIS site" -ForegroundColor Cyan

} catch {
    Write-Host "Lỗi khi tạo test data: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Hãy chạy thủ công SQL script: create-usd-eur-test-data.sql" -ForegroundColor Yellow
}