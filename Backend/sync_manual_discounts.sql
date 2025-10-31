-- Script để đồng bộ dữ liệu OrderDiscounts cho các đơn hàng có giảm giá thủ công
-- Tạo OrderDiscount records cho các đơn hàng có DiscountAmount > 0 nhưng chưa có trong OrderDiscounts

-- Đầu tiên, tạo Discount record cho "Giảm giá thủ công" nếu chưa có
IF NOT EXISTS (SELECT 1 FROM Discounts WHERE Name = N'Giảm giá thủ công')
BEGIN
    INSERT INTO Discounts (Name, Description, Type, Value, IsActive, UsageCount, CreatedAt, IsDeleted, CreatedBy)
    VALUES (N'Giảm giá thủ công', N'Giảm giá được áp dụng thủ công tại quầy', 3, 0, 1, 0, GETDATE(), 0, 1)
END

-- Lấy DiscountId của "Giảm giá thủ công"
DECLARE @ManualDiscountId INT
SELECT @ManualDiscountId = DiscountId FROM Discounts WHERE Name = N'Giảm giá thủ công'

-- Tạo OrderDiscount records cho các đơn hàng có DiscountAmount > 0 nhưng chưa có trong OrderDiscounts
INSERT INTO OrderDiscounts (OrderId, DiscountId, DiscountName, DiscountType, DiscountValue, DiscountAmount, OrderItemId, AppliedAt, AppliedBy)
SELECT 
    o.OrderId,
    @ManualDiscountId,
    N'Giảm giá thủ công',
    3, -- FixedAmountTotal
    o.DiscountAmount,
    o.DiscountAmount,
    NULL,
    o.CreatedAt,
    1 -- Default staff ID
FROM Orders o
WHERE o.DiscountAmount > 0 
  AND NOT EXISTS (SELECT 1 FROM OrderDiscounts od WHERE od.OrderId = o.OrderId)

-- Cập nhật UsageCount cho Discount record
UPDATE Discounts 
SET UsageCount = (SELECT COUNT(*) FROM OrderDiscounts WHERE DiscountId = @ManualDiscountId)
WHERE DiscountId = @ManualDiscountId

SELECT 'Đã đồng bộ dữ liệu OrderDiscounts thành công' as Result;