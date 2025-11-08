-- Cleanup CustomerTiers - Xóa tiers duplicate và encoding sai
-- Giữ lại 4 tiers chuẩn

-- Bước 1: Cập nhật tất cả khách hàng về tier ID chuẩn
UPDATE Customers SET TierId = 1 WHERE TierId IN (5, 6); -- Chuyển encoding sai về Đồng
UPDATE Customers SET TierId = 2 WHERE TierId = 6 AND LoyaltyPoints >= 100; -- Bạc
UPDATE Customers SET TierId = 3 WHERE TierId IN (7, 8) OR (LoyaltyPoints >= 2000 AND TotalSpent >= 20000000); -- Vàng/Kim cương
UPDATE Customers SET TierId = 4 WHERE LoyaltyPoints >= 5000 AND TotalSpent >= 50000000; -- Kim cương cao

-- Bước 2: Xóa tiers duplicate và encoding sai
DELETE FROM CustomerTiers WHERE TierId IN (5, 6, 7, 8, 9);

-- Bước 3: Cập nhật lại 4 tiers chuẩn với đúng thứ tự
UPDATE CustomerTiers SET 
    TierName = N'Đồng',
    MinSpent = 0,
    MinPoints = 0,
    PointsMultiplier = 1.0,
    DiscountPercentage = 0,
    Description = N'Hạng khách hàng cơ bản',
    TierColor = '#CD7F32',
    IsActive = 1
WHERE TierId = 1;

UPDATE CustomerTiers SET 
    TierName = N'Bạc',
    MinSpent = 1000000,
    MinPoints = 100,
    PointsMultiplier = 1.2,
    DiscountPercentage = 5,
    Description = N'Hạng khách hàng thân thiết',
    TierColor = '#C0C0C0',
    IsActive = 1
WHERE TierId = 2;

UPDATE CustomerTiers SET 
    TierName = N'Vàng',
    MinSpent = 5000000,
    MinPoints = 500,
    PointsMultiplier = 1.5,
    DiscountPercentage = 10,
    Description = N'Hạng khách hàng VIP',
    TierColor = '#FFD700',
    IsActive = 1
WHERE TierId = 3;

UPDATE CustomerTiers SET 
    TierName = N'Kim cương',
    MinSpent = 20000000,
    MinPoints = 2000,
    PointsMultiplier = 2.0,
    DiscountPercentage = 15,
    Description = N'Hạng khách hàng VVIP',
    TierColor = '#B9F2FF',
    IsActive = 1
WHERE TierId = 4;

-- Kiểm tra kết quả
SELECT * FROM CustomerTiers WHERE IsActive = 1 ORDER BY MinSpent;

-- Kiểm tra customers
SELECT CustomerId, HoTen, TierId, LoyaltyPoints, TotalSpent 
FROM Customers 
ORDER BY LoyaltyPoints DESC;