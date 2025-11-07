-- Tạo dữ liệu mẫu CustomerTiers cho hệ thống hạng khách hàng
-- Xóa dữ liệu cũ nếu có
DELETE FROM CustomerTiers;

-- Reset identity seed
DBCC CHECKIDENT ('CustomerTiers', RESEED, 0);

-- Thêm các hạng khách hàng mới
INSERT INTO CustomerTiers (TierName, MinSpent, MinPoints, PointsMultiplier, DiscountPercentage, Description, TierColor, IsActive, CreatedAt, UpdatedAt) VALUES
-- Hạng Đồng (mặc định)
('Đồng', 0, 0, 1.0, 0, 'Hạng cơ bản dành cho khách hàng mới', '#CD7F32', 1, GETDATE(), GETDATE()),

-- Hạng Bạc 
('Bạc', 1000000, 100, 1.2, 5, 'Tích điểm 1.2x, giảm giá 5% cho khách hàng thân thiết', '#C0C0C0', 1, GETDATE(), GETDATE()),

-- Hạng Vàng
('Vàng', 5000000, 500, 1.5, 10, 'Tích điểm 1.5x, giảm giá 10% cho khách hàng VIP', '#FFD700', 1, GETDATE(), GETDATE()),

-- Hạng Kim cương  
('Kim cương', 20000000, 2000, 2.0, 15, 'Tích điểm 2x, giảm giá 15% cho khách hàng VVIP', '#B9F2FF', 1, GETDATE(), GETDATE()),

-- Hạng Platinum (cao nhất)
('Platinum', 50000000, 5000, 2.5, 20, 'Tích điểm 2.5x, giảm giá 20% cho khách hàng Diamond', '#E5E4E2', 1, GETDATE(), GETDATE());

-- Xem kết quả
SELECT * FROM CustomerTiers ORDER BY MinSpent;

-- Cập nhật một số khách hàng để test
-- Cập nhật khách hàng có ID 1 lên hạng Vàng
UPDATE Customers 
SET TotalSpent = 6000000, LoyaltyPoints = 600, Rank = 2 
WHERE CustomerID = 1;

-- Cập nhật khách hàng có ID 2 lên hạng Bạc  
UPDATE Customers 
SET TotalSpent = 1500000, LoyaltyPoints = 150, Rank = 1 
WHERE CustomerID = 2;

-- Cập nhật khách hàng có ID 3 lên hạng Kim cương
UPDATE Customers 
SET TotalSpent = 25000000, LoyaltyPoints = 2500, Rank = 3 
WHERE CustomerID = 3;

SELECT CustomerID, CustomerName, TotalSpent, LoyaltyPoints, Rank 
FROM Customers 
WHERE CustomerID IN (1, 2, 3);