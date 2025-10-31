-- Insert test discount data directly
-- First, get an existing order ID
DECLARE @OrderId INT;
SELECT TOP 1 @OrderId = OrderId FROM Orders WHERE PaymentStatus IN ('paid', 'completed') AND Status != 'cancelled' ORDER BY CreatedAt DESC;

IF @OrderId IS NOT NULL
BEGIN
    -- Insert a manual discount record
    INSERT INTO OrderDiscounts (OrderId, DiscountName, DiscountType, DiscountValue, DiscountAmount, AppliedAt, AppliedBy)
    VALUES 
        (@OrderId, 'Giảm giá thủ công 5%', 0, 5.0, 9500, GETDATE() - 1, '550e8400-e29b-41d4-a716-446655440001'),
        (@OrderId, 'Giảm giá thủ công 10k', 2, 10000.0, 10000, GETDATE() - 2, '550e8400-e29b-41d4-a716-446655440001');
    
    PRINT 'Test discount data inserted for OrderId: ' + CAST(@OrderId AS VARCHAR);
    
    -- Show the inserted data
    SELECT * FROM OrderDiscounts WHERE OrderId = @OrderId;
END
ELSE
BEGIN
    PRINT 'No orders found. Please create some orders first.';
END