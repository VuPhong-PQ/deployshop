-- Script để test tính năng Currency
-- Cập nhật một vài đơn hàng banktransfer hiện có để test

-- Kiểm tra đơn hàng banktransfer hiện có
SELECT OrderId, PaymentMethod, Currency, TotalAmount, CustomerName 
FROM Orders 
WHERE PaymentMethod = 'banktransfer'
ORDER BY CreatedAt DESC;

-- Cập nhật vài đơn hàng để test
-- Cập nhật đơn hàng đầu tiên thành USD
UPDATE TOP(2) Orders 
SET Currency = 'USD' 
WHERE PaymentMethod = 'banktransfer' 
  AND Currency IS NULL;

-- Cập nhật đơn hàng tiếp theo thành EUR  
UPDATE TOP(2) Orders 
SET Currency = 'EUR' 
WHERE PaymentMethod = 'banktransfer' 
  AND Currency IS NULL;

-- Kiểm tra lại sau khi cập nhật
SELECT OrderId, PaymentMethod, Currency, TotalAmount, CustomerName 
FROM Orders 
WHERE PaymentMethod = 'banktransfer'
ORDER BY CreatedAt DESC;