-- Migration: Add Foreign Currency Payment Methods (USD and EUR)
-- Date: 2025-01-21
-- Description: Add EnableForeignUSD and EnableForeignEUR columns to PaymentMethodConfigs table

USE YourDatabaseName; -- Replace with your actual database name

-- Add new columns to PaymentMethodConfigs table
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PaymentMethodConfigs]') AND name = 'EnableForeignUSD')
BEGIN
    ALTER TABLE [dbo].[PaymentMethodConfigs]
    ADD EnableForeignUSD BIT NOT NULL DEFAULT 0;
    
    PRINT 'Added EnableForeignUSD column to PaymentMethodConfigs table';
END
ELSE
BEGIN
    PRINT 'EnableForeignUSD column already exists in PaymentMethodConfigs table';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PaymentMethodConfigs]') AND name = 'EnableForeignEUR')
BEGIN
    ALTER TABLE [dbo].[PaymentMethodConfigs]
    ADD EnableForeignEUR BIT NOT NULL DEFAULT 0;
    
    PRINT 'Added EnableForeignEUR column to PaymentMethodConfigs table';
END
ELSE
BEGIN
    PRINT 'EnableForeignEUR column already exists in PaymentMethodConfigs table';
END

-- Update existing records to have foreign currency options disabled by default
UPDATE [dbo].[PaymentMethodConfigs] 
SET EnableForeignUSD = 0, EnableForeignEUR = 0 
WHERE EnableForeignUSD IS NULL OR EnableForeignEUR IS NULL;

PRINT 'Migration completed successfully: Foreign Currency Payment Methods added';

-- Verify the changes
SELECT 
    Id,
    EnableCash,
    EnableBankCard, 
    EnableQRCode,
    EnableEWallet,
    EnableBankTransfer,
    EnableForeignUSD,
    EnableForeignEUR,
    EnablePartialPayment,
    EnableDrawer,
    DefaultMethod
FROM [dbo].[PaymentMethodConfigs];