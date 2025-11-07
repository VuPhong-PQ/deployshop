-- Tạo sample CustomerTiers cho các hạng Bạc, Vàng, Kim cương
-- Với quyền lợi điểm thưởng khác nhau

-- Xóa dữ liệu cũ nếu có
DELETE FROM CustomerTiers;

-- Thêm các hạng khách hàng mẫu
INSERT INTO CustomerTiers (
    TierName, 
    MinSpent, 
    MinPoints, 
    PointsMultiplier, 
    DiscountPercentage, 
    Description, 
    TierColor, 
    IsActive, 
    CreatedAt
) VALUES 
-- Hạng Đồng (Bronze) - Hạng cơ bản
('Đồng', 0, 0, 1.0, 0, 'Hạng khách hàng cơ bản', '#CD7F32', 1, GETDATE()),

-- Hạng Bạc (Silver) - Tích điểm x1.2, giảm giá 5%
('Bạc', 1000000, 100, 1.2, 5, 'Tích điểm x1.2, giảm giá 5% mọi đơn hàng', '#C0C0C0', 1, GETDATE()),

-- Hạng Vàng (Gold) - Tích điểm x1.5, giảm giá 10% 
('Vàng', 5000000, 500, 1.5, 10, 'Tích điểm x1.5, giảm giá 10%, bonus điểm sinh nhật', '#FFD700', 1, GETDATE()),

-- Hạng Kim cương (Diamond) - Tích điểm x2.0, giảm giá 15%
('Kim cương', 20000000, 2000, 2.0, 15, 'Tích điểm x2.0, giảm giá 15%, bonus điểm cuối tuần và sinh nhật', '#B9F2FF', 1, GETDATE()),

-- Hạng VIP (Platinum) - Tích điểm x3.0, giảm giá 20%
('VIP', 50000000, 5000, 3.0, 20, 'Tích điểm x3.0, giảm giá 20%, tất cả bonus điểm đặc biệt', '#E5E4E2', 1, GETDATE());

-- Kiểm tra dữ liệu đã tạo
SELECT 
    TierId,
    TierName, 
    FORMAT(MinSpent, 'N0') as 'Chi tiêu tối thiểu (VNĐ)',
    MinPoints as 'Điểm tối thiểu',
    PointsMultiplier as 'Hệ số tích điểm',
    DiscountPercentage as 'Giảm giá (%)',
    Description as 'Mô tả quyền lợi',
    TierColor as 'Màu sắc',
    IsActive as 'Kích hoạt'
FROM CustomerTiers 
ORDER BY MinSpent;