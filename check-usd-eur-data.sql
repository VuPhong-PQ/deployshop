-- Kiểm tra dữ liệu Orders trong database RetailPoint
-- Xem có đơn hàng nào với USD/EUR không

SELECT 
    OrderId,
    OrderNumber,
    CustomerName,
    PaymentMethod,
    Currency,
    PaymentStatus,
    Status,
    TotalAmount,
    CreatedAt
FROM Orders 
WHERE PaymentMethod = 'banktransfer' 
    AND Currency IN ('USD', 'EUR')
    AND Status = 'completed'
    AND PaymentStatus = 'paid'
ORDER BY CreatedAt DESC;

-- Kiểm tra tất cả đơn hàng gần nhất để debug
SELECT TOP 20
    OrderId,
    OrderNumber,
    PaymentMethod,
    Currency,
    PaymentStatus,
    Status,
    TotalAmount,
    CreatedAt
FROM Orders 
ORDER BY CreatedAt DESC;

-- Kiểm tra các PaymentMethod khác nhau trong database
SELECT DISTINCT 
    PaymentMethod,
    Currency,
    COUNT(*) as OrderCount
FROM Orders 
WHERE Status = 'completed' AND PaymentStatus = 'paid'
GROUP BY PaymentMethod, Currency
ORDER BY PaymentMethod, Currency;