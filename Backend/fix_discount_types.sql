-- Sửa lại dữ liệu test để phù hợp với enum DiscountType
-- PercentageTotal = 1, FixedAmountItem = 2, FixedAmountTotal = 3

-- Cập nhật OrderDiscounts để sử dụng DiscountType đúng
UPDATE OrderDiscounts 
SET DiscountType = 1 
WHERE OrderDiscountId = 14; -- Đổi từ 0 thành 1 (PercentageTotal)

UPDATE OrderDiscounts 
SET DiscountType = 3 
WHERE OrderDiscountId = 15; -- Đổi từ 2 thành 3 (FixedAmountTotal)

SELECT 'Đã cập nhật DiscountType thành công' as Result;