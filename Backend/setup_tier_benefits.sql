-- Tạo các hạng khách hàng mẫu với quyền lợi điểm thưởng

-- Xóa dữ liệu cũ nếu có
DELETE FROM CustomerTiers;

-- Thêm các hạng khách hàng với quyền lợi
INSERT INTO CustomerTiers (TierName, MinSpent, MinPoints, PointsMultiplier, DiscountPercentage, Description, TierColor, IsActive, CreatedAt) VALUES
('Thường', 0, 0, 1.0, 0, 'Hạng khách hàng cơ bản', '#808080', 1, GETDATE()),
('Bạc', 1000000, 100, 1.2, 5, 'Hạng Bạc: +20% điểm thưởng, 50 điểm bonus >= 100k, gấp đôi điểm sinh nhật', '#C0C0C0', 1, GETDATE()),
('Vàng', 5000000, 500, 1.5, 10, 'Hạng Vàng: +50% điểm thưởng, 100 điểm bonus >= 200k, milestone 200 điểm/500k, gấp 2.5 lần điểm sinh nhật', '#FFD700', 1, GETDATE()),
('Kim Cương', 10000000, 1000, 2.0, 15, 'Hạng Kim Cương: Gấp đôi điểm thưởng, 300 điểm cố định, milestone 250 điểm/300k, gấp đôi bonus cuối tuần, gấp 3 lần điểm sinh nhật', '#B9F2FF', 1, GETDATE());

-- Kiểm tra dữ liệu đã insert
SELECT * FROM CustomerTiers ORDER BY MinSpent;

-- Cập nhật một số khách hàng để test
UPDATE Customers SET TierId = 2, LoyaltyPoints = 150 WHERE CustomerId IN (SELECT TOP 2 CustomerId FROM Customers);
UPDATE Customers SET TierId = 3, LoyaltyPoints = 750 WHERE CustomerId IN (SELECT TOP 2 CustomerId FROM Customers WHERE TierId IS NULL OR TierId = 1);
UPDATE Customers SET TierId = 4, LoyaltyPoints = 1500 WHERE CustomerId IN (SELECT TOP 1 CustomerId FROM Customers WHERE TierId IS NULL OR TierId = 1);

-- Kiểm tra khách hàng đã cập nhật
SELECT c.CustomerId, c.HoTen, c.TierId, ct.TierName, c.LoyaltyPoints, c.TotalSpent 
FROM Customers c 
LEFT JOIN CustomerTiers ct ON c.TierId = ct.TierId 
WHERE c.TierId IS NOT NULL;