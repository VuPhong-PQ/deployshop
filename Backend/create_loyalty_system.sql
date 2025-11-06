-- Tạo hệ thống tích điểm thưởng cho khách hàng
-- Script này tạo các bảng cần thiết cho hệ thống tích điểm

USE RetailPoint;
GO

-- 1. Bảng cấu hình chương trình tích điểm
CREATE TABLE LoyaltyConfigs (
    LoyaltyConfigId INT PRIMARY KEY IDENTITY(1,1),
    IsEnabled BIT NOT NULL DEFAULT 1,
    PointsPerCurrency DECIMAL(10,2) NOT NULL DEFAULT 1.0, -- 1000 VND = 1 điểm
    MinOrderAmountForPoints DECIMAL(18,2) DEFAULT 0, -- Đơn tối thiểu để tích điểm
    MaxPointsPerOrder INT DEFAULT NULL, -- Tối đa điểm/đơn hàng
    PointExpiryDays INT DEFAULT 365, -- Điểm hết hạn sau bao nhiêu ngày
    AllowPointRedemption BIT NOT NULL DEFAULT 1, -- Cho phép đổi điểm
    PointValue DECIMAL(10,2) NOT NULL DEFAULT 100.0, -- 100 điểm = 1000 VND
    MaxRedemptionPercentage DECIMAL(5,2) DEFAULT 50.0, -- Tối đa 50% hóa đơn dùng điểm
    
    -- Tích điểm theo thời gian
    HappyHourEnabled BIT DEFAULT 0,
    HappyHourStartTime TIME DEFAULT '17:00:00',
    HappyHourEndTime TIME DEFAULT '19:00:00',
    HappyHourMultiplier DECIMAL(3,2) DEFAULT 2.0,
    
    WeekendBonusEnabled BIT DEFAULT 0,
    WeekendMultiplier DECIMAL(3,2) DEFAULT 1.5,
    
    BirthdayBonusEnabled BIT DEFAULT 0,
    BirthdayMultiplier DECIMAL(3,2) DEFAULT 3.0,
    BirthdayValidDays INT DEFAULT 7, -- Áp dụng trong vòng 7 ngày sinh nhật
    
    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    UpdatedAt DATETIME2 NULL,
    CreatedBy INT NULL
);

-- 2. Bảng cấp độ khách hàng (Customer Tiers)
CREATE TABLE CustomerTiers (
    TierId INT PRIMARY KEY IDENTITY(1,1),
    TierName NVARCHAR(50) NOT NULL, -- Đồng, Bạc, Vàng, Kim cương
    MinSpent DECIMAL(18,2) NOT NULL DEFAULT 0, -- Tổng tiền tối thiểu
    MinPoints INT NOT NULL DEFAULT 0, -- Điểm tối thiểu
    PointsMultiplier DECIMAL(3,2) NOT NULL DEFAULT 1.0, -- Hệ số nhân điểm
    DiscountPercentage DECIMAL(5,2) DEFAULT 0, -- Giảm giá % cho cấp này
    Description NVARCHAR(255),
    TierColor NVARCHAR(7) DEFAULT '#808080', -- Màu hiển thị
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE()
);

-- 3. Bảng tích điểm theo danh mục sản phẩm
CREATE TABLE CategoryLoyaltyRules (
    RuleId INT PRIMARY KEY IDENTITY(1,1),
    CategoryId INT NOT NULL,
    PointsMultiplier DECIMAL(3,2) NOT NULL DEFAULT 1.0, -- Hệ số nhân cho danh mục
    BonusPoints INT DEFAULT 0, -- Điểm thưởng cố định
    IsActive BIT NOT NULL DEFAULT 1,
    ValidFrom DATETIME2 DEFAULT GETDATE(),
    ValidTo DATETIME2 DEFAULT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    
    FOREIGN KEY (CategoryId) REFERENCES Category(CategoryId)
);

-- 4. Bảng tích điểm theo sản phẩm cụ thể
CREATE TABLE ProductLoyaltyRules (
    RuleId INT PRIMARY KEY IDENTITY(1,1),
    ProductId INT NOT NULL,
    PointsMultiplier DECIMAL(3,2) NOT NULL DEFAULT 1.0,
    BonusPoints INT DEFAULT 0,
    MinQuantity INT DEFAULT 1, -- Số lượng tối thiểu để có thưởng
    IsActive BIT NOT NULL DEFAULT 1,
    ValidFrom DATETIME2 DEFAULT GETDATE(),
    ValidTo DATETIME2 DEFAULT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    
    FOREIGN KEY (ProductId) REFERENCES Products(ProductId)
);

-- 5. Bảng lịch sử tích điểm
CREATE TABLE LoyaltyTransactions (
    TransactionId INT PRIMARY KEY IDENTITY(1,1),
    CustomerId INT NOT NULL,
    OrderId INT NULL, -- NULL nếu là điều chỉnh thủ công
    TransactionType NVARCHAR(20) NOT NULL, -- 'EARN', 'REDEEM', 'EXPIRE', 'ADJUST'
    Points INT NOT NULL, -- Có thể âm (trừ điểm) hoặc dương (cộng điểm)
    PointsBalance INT NOT NULL, -- Số dư điểm sau giao dịch
    Reason NVARCHAR(255), -- Lý do tích/trừ điểm
    ExpiryDate DATETIME2 NULL, -- Ngày hết hạn điểm (cho điểm tích)
    ProcessedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    ProcessedBy INT NULL, -- Staff xử lý (nếu thủ công)
    
    FOREIGN KEY (CustomerId) REFERENCES Customers(CustomerId),
    FOREIGN KEY (OrderId) REFERENCES Orders(OrderId),
    FOREIGN KEY (ProcessedBy) REFERENCES Staff(StaffId)
);

-- 6. Bảng chương trình khuyến mãi đặc biệt
CREATE TABLE LoyaltyPromotions (
    PromotionId INT PRIMARY KEY IDENTITY(1,1),
    Name NVARCHAR(100) NOT NULL,
    Description NVARCHAR(500),
    PromotionType NVARCHAR(20) NOT NULL, -- 'BONUS_POINTS', 'MULTIPLIER', 'MILESTONE'
    
    -- Điều kiện
    MinOrderAmount DECIMAL(18,2) DEFAULT 0,
    MinQuantity INT DEFAULT 1,
    TargetCustomerTier INT NULL, -- Áp dụng cho cấp độ nào
    
    -- Phần thưởng
    BonusPoints INT DEFAULT 0,
    PointsMultiplier DECIMAL(3,2) DEFAULT 1.0,
    
    -- Thời gian
    StartDate DATETIME2 NOT NULL,
    EndDate DATETIME2 NOT NULL,
    
    -- Giới hạn
    MaxUsagePerCustomer INT DEFAULT NULL,
    MaxTotalUsage INT DEFAULT NULL,
    CurrentUsage INT DEFAULT 0,
    
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    CreatedBy INT NULL,
    
    FOREIGN KEY (TargetCustomerTier) REFERENCES CustomerTiers(TierId),
    FOREIGN KEY (CreatedBy) REFERENCES Staff(StaffId)
);

-- 7. Bảng theo dõi sử dụng khuyến mãi của khách hàng
CREATE TABLE CustomerPromotionUsage (
    UsageId INT PRIMARY KEY IDENTITY(1,1),
    CustomerId INT NOT NULL,
    PromotionId INT NOT NULL,
    OrderId INT NOT NULL,
    PointsEarned INT NOT NULL,
    UsedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    
    FOREIGN KEY (CustomerId) REFERENCES Customers(CustomerId),
    FOREIGN KEY (PromotionId) REFERENCES LoyaltyPromotions(PromotionId),
    FOREIGN KEY (OrderId) REFERENCES Orders(OrderId)
);

-- Chèn dữ liệu mặc định
-- Cấu hình tích điểm mặc định
INSERT INTO LoyaltyConfigs (
    IsEnabled, PointsPerCurrency, MinOrderAmountForPoints, PointValue, 
    MaxRedemptionPercentage, HappyHourEnabled, WeekendBonusEnabled, BirthdayBonusEnabled
) VALUES (
    1, 1000.0, 50000, 1000.0, 50.0, 0, 0, 0
);

-- Các cấp độ khách hàng mặc định
INSERT INTO CustomerTiers (TierName, MinSpent, MinPoints, PointsMultiplier, DiscountPercentage, Description, TierColor) VALUES
('Đồng', 0, 0, 1.0, 0, 'Khách hàng mới', '#CD7F32'),
('Bạc', 5000000, 500, 1.2, 2, 'Khách hàng thân thiết', '#C0C0C0'),
('Vàng', 20000000, 2000, 1.5, 5, 'Khách hàng VIP', '#FFD700'),
('Kim cương', 50000000, 5000, 2.0, 10, 'Khách hàng VVIP', '#B9F2FF');

-- Tạo indexes để tăng performance
CREATE INDEX IX_LoyaltyTransactions_CustomerId ON LoyaltyTransactions(CustomerId);
CREATE INDEX IX_LoyaltyTransactions_OrderId ON LoyaltyTransactions(OrderId);
CREATE INDEX IX_LoyaltyTransactions_TransactionType ON LoyaltyTransactions(TransactionType);
CREATE INDEX IX_LoyaltyTransactions_ExpiryDate ON LoyaltyTransactions(ExpiryDate);
CREATE INDEX IX_CategoryLoyaltyRules_CategoryId ON CategoryLoyaltyRules(CategoryId);
CREATE INDEX IX_ProductLoyaltyRules_ProductId ON ProductLoyaltyRules(ProductId);

-- Cập nhật bảng Customers để có thêm thông tin cấp độ
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Customers') AND name = 'TierId')
BEGIN
    ALTER TABLE Customers ADD TierId INT DEFAULT 1;
    ALTER TABLE Customers ADD FOREIGN KEY (TierId) REFERENCES CustomerTiers(TierId);
END

PRINT 'Hệ thống tích điểm đã được tạo thành công!';