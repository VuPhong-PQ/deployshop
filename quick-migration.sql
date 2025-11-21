-- Quick Migration: Add Foreign Currency Payment Methods
-- Run this directly on your database

-- Add EnableForeignUSD column if not exists
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'PaymentMethodConfigs') AND name = 'EnableForeignUSD')
BEGIN
    ALTER TABLE PaymentMethodConfigs ADD EnableForeignUSD BIT NOT NULL DEFAULT 0;
    PRINT 'Added EnableForeignUSD column';
END

-- Add EnableForeignEUR column if not exists  
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'PaymentMethodConfigs') AND name = 'EnableForeignEUR')
BEGIN
    ALTER TABLE PaymentMethodConfigs ADD EnableForeignEUR BIT NOT NULL DEFAULT 0;
    PRINT 'Added EnableForeignEUR column';
END

-- Verify columns added
SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'PaymentMethodConfigs' 
AND COLUMN_NAME IN ('EnableForeignUSD', 'EnableForeignEUR');

PRINT 'Migration completed!';