-- Script to test featured products functionality
-- Check current featured products
SELECT ProductId, Name, Price, StockQuantity, IsFeatured 
FROM Products 
WHERE IsFeatured = 1;

-- If no featured products exist, let's set some products as featured
-- This will set the first 5 products with stock > 0 as featured
UPDATE Products 
SET IsFeatured = 1 
WHERE ProductId IN (
    SELECT TOP 5 ProductId 
    FROM Products 
    WHERE StockQuantity > 0 
    ORDER BY ProductId
);

-- Check again after update
SELECT ProductId, Name, Price, StockQuantity, IsFeatured 
FROM Products 
WHERE IsFeatured = 1
ORDER BY ProductId;

-- Show all products with their featured status
SELECT ProductId, Name, Price, StockQuantity, IsFeatured,
       CASE WHEN IsFeatured = 1 THEN 'Hay bán' ELSE 'Thường' END as Status
FROM Products 
ORDER BY IsFeatured DESC, ProductId;