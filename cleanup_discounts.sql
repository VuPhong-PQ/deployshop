-- Xem tất cả discounts hiện có
SELECT * FROM Discounts;

-- Xóa discount không sử dụng (chỉ giữ lại giảm giá thủ công)
DELETE FROM Discounts WHERE Name NOT LIKE '%thủ công%' AND Name NOT LIKE '%manual%';