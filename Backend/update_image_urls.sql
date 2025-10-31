-- Script để cập nhật tất cả imageUrl từ localhost:5271 sang 101.53.9.76:5273

-- Cập nhật Products table
UPDATE Products 
SET ImageUrl = REPLACE(ImageUrl, 'http://localhost:5271', 'http://101.53.9.76:5273')
WHERE ImageUrl LIKE '%localhost:5271%';

-- Kiểm tra kết quả
SELECT ProductId, Name, ImageUrl 
FROM Products 
WHERE ImageUrl IS NOT NULL
ORDER BY ProductId;

-- Nếu có bảng khác chứa imageUrl, cũng cập nhật tương tự
-- UPDATE [TableName] 
-- SET [ImageColumnName] = REPLACE([ImageColumnName], 'http://localhost:5271', 'http://101.53.9.76:5273')
-- WHERE [ImageColumnName] LIKE '%localhost:5271%';