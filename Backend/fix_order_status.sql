-- Script để sửa lại trạng thái các đơn hàng sai
-- Cập nhật đơn hàng #41 và #42 nếu chúng thực sự là đơn hàng chưa thanh toán

-- Kiểm tra trạng thái hiện tại của đơn hàng #41 và #42
SELECT OrderId, PaymentStatus, Status, PaymentMethod, TotalAmount, CreatedAt 
FROM Orders 
WHERE OrderId IN (41, 42);

-- Nếu cần, cập nhật trạng thái để đúng với thực tế
-- Uncomment các dòng dưới nếu cần sửa:

-- UPDATE Orders 
-- SET PaymentStatus = 'pending', Status = 'pending' 
-- WHERE OrderId = 41 AND PaymentStatus = 'paid';

-- UPDATE Orders 
-- SET PaymentStatus = 'pending', Status = 'pending' 
-- WHERE OrderId = 42 AND PaymentStatus = 'paid';

-- Kiểm tra lại sau khi cập nhật
SELECT OrderId, PaymentStatus, Status, PaymentMethod, TotalAmount, CreatedAt 
FROM Orders 
WHERE OrderId IN (41, 42);