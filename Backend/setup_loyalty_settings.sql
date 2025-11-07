-- Seed default LoyaltySettings data
INSERT INTO LoyaltySettings (
    IsPointsEnabled, 
    IsRedemptionEnabled, 
    PointsPerVnd, 
    VndPerPoint, 
    MinOrderForPoints, 
    MaxRedemptionPercent, 
    PointsExpiryDays,
    CreatedAt, 
    UpdatedAt
) VALUES (
    1,              -- IsPointsEnabled = true
    1,              -- IsRedemptionEnabled = true  
    1000,           -- PointsPerVnd = 1000 (1000 VNĐ = 1 điểm)
    1000,           -- VndPerPoint = 1000 (1 điểm = 1000 VNĐ)
    50000,          -- MinOrderForPoints = 50,000 VNĐ
    50,             -- MaxRedemptionPercent = 50%
    365,            -- PointsExpiryDays = 365 ngày (1 năm)
    GETDATE(),      -- CreatedAt
    GETDATE()       -- UpdatedAt
);

-- Verify the inserted data
SELECT * FROM LoyaltySettings;