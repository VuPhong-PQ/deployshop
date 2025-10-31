-- Insert test discount data
INSERT INTO Discounts (Name, Description, DiscountType, Value, MinOrderAmount, IsActive, CreatedAt, CreatedBy)
VALUES 
    ('Giảm 10%', 'Giảm giá 10% cho tổng hóa đơn', 0, 10.0, 100000, 1, GETDATE(), '550e8400-e29b-41d4-a716-446655440001'),
    ('Giảm 50k', 'Giảm giá 50,000đ cho tổng hóa đơn', 2, 50000.0, 200000, 1, GETDATE(), '550e8400-e29b-41d4-a716-446655440001'),
    ('Giảm 20k/món', 'Giảm giá 20,000đ cho mỗi món', 1, 20000.0, null, 1, GETDATE(), '550e8400-e29b-41d4-a716-446655440001');

-- Insert some test order discounts if there are orders in the system
-- First, let's check if there are any orders
DECLARE @OrderCount INT;
SELECT @OrderCount = COUNT(*) FROM Orders WHERE PaymentStatus IN ('paid', 'completed') AND Status != 'cancelled';

IF @OrderCount > 0
BEGIN
    -- Get some order IDs
    DECLARE @OrderId1 INT, @OrderId2 INT, @OrderId3 INT;
    DECLARE @DiscountId1 INT, @DiscountId2 INT, @DiscountId3 INT;
    
    -- Get discount IDs
    SELECT @DiscountId1 = DiscountId FROM Discounts WHERE Name = 'Giảm 10%';
    SELECT @DiscountId2 = DiscountId FROM Discounts WHERE Name = 'Giảm 50k';
    SELECT @DiscountId3 = DiscountId FROM Discounts WHERE Name = 'Giảm 20k/món';
    
    -- Get some order IDs
    SELECT TOP 3 @OrderId1 = OrderId FROM Orders WHERE PaymentStatus IN ('paid', 'completed') AND Status != 'cancelled' ORDER BY CreatedAt DESC;
    SELECT @OrderId2 = OrderId FROM Orders WHERE PaymentStatus IN ('paid', 'completed') AND Status != 'cancelled' AND OrderId != @OrderId1 ORDER BY CreatedAt DESC OFFSET 1 ROWS FETCH NEXT 1 ROWS ONLY;
    SELECT @OrderId3 = OrderId FROM Orders WHERE PaymentStatus IN ('paid', 'completed') AND Status != 'cancelled' AND OrderId NOT IN (@OrderId1, @OrderId2) ORDER BY CreatedAt DESC OFFSET 2 ROWS FETCH NEXT 1 ROWS ONLY;
    
    -- Insert order discounts
    IF @OrderId1 IS NOT NULL AND @DiscountId1 IS NOT NULL
    BEGIN
        INSERT INTO OrderDiscounts (OrderId, DiscountId, DiscountName, DiscountType, DiscountValue, DiscountAmount, AppliedAt, AppliedBy)
        VALUES (@OrderId1, @DiscountId1, 'Giảm 10%', 0, 10.0, 19000, GETDATE() - 1, '550e8400-e29b-41d4-a716-446655440001');
    END
    
    IF @OrderId2 IS NOT NULL AND @DiscountId2 IS NOT NULL
    BEGIN
        INSERT INTO OrderDiscounts (OrderId, DiscountId, DiscountName, DiscountType, DiscountValue, DiscountAmount, AppliedAt, AppliedBy)
        VALUES (@OrderId2, @DiscountId2, 'Giảm 50k', 2, 50000.0, 50000, GETDATE() - 2, '550e8400-e29b-41d4-a716-446655440001');
    END
    
    IF @OrderId3 IS NOT NULL AND @DiscountId3 IS NOT NULL
    BEGIN
        INSERT INTO OrderDiscounts (OrderId, DiscountId, DiscountName, DiscountType, DiscountValue, DiscountAmount, AppliedAt, AppliedBy)
        VALUES (@OrderId3, @DiscountId3, 'Giảm 20k/món', 1, 20000.0, 40000, GETDATE() - 3, '550e8400-e29b-41d4-a716-446655440001');
    END
    
    PRINT 'Test discount data inserted successfully!';
END
ELSE
BEGIN
    PRINT 'No orders found. Please create some orders first, then run this script again.';
END