-- Create PaymentMethodConfigs table with foreign currency support
-- Date: 2025-11-21

USE RetailPoint;

-- Create PaymentMethodConfigs table if it doesn't exist
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='PaymentMethodConfigs' AND xtype='U')
BEGIN
    CREATE TABLE [dbo].[PaymentMethodConfigs] (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        EnableCash BIT NOT NULL DEFAULT 1,
        EnableBankCard BIT NOT NULL DEFAULT 1,
        EnableQRCode BIT NOT NULL DEFAULT 1,
        EnableEWallet BIT NOT NULL DEFAULT 1,
        EnableBankTransfer BIT NOT NULL DEFAULT 1,
        EnableForeignUSD BIT NOT NULL DEFAULT 0,
        EnableForeignEUR BIT NOT NULL DEFAULT 0,
        EnablePartialPayment BIT NOT NULL DEFAULT 0,
        EnableDrawer BIT NOT NULL DEFAULT 1,
        DefaultMethod NVARCHAR(50) NOT NULL DEFAULT 'cash'
    );
    
    PRINT 'Created PaymentMethodConfigs table';
    
    -- Insert default configuration
    INSERT INTO [dbo].[PaymentMethodConfigs] (
        EnableCash, EnableBankCard, EnableQRCode, EnableEWallet, EnableBankTransfer,
        EnableForeignUSD, EnableForeignEUR, EnablePartialPayment, EnableDrawer, DefaultMethod
    ) VALUES (
        1, 1, 1, 0, 1, 1, 1, 0, 1, 'cash'
    );
    
    PRINT 'Inserted default configuration with USD and EUR enabled';
END
ELSE
BEGIN
    -- Table exists, check if foreign currency columns exist
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PaymentMethodConfigs]') AND name = 'EnableForeignUSD')
    BEGIN
        ALTER TABLE [dbo].[PaymentMethodConfigs] ADD EnableForeignUSD BIT NOT NULL DEFAULT 0;
        PRINT 'Added EnableForeignUSD column';
    END
    
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PaymentMethodConfigs]') AND name = 'EnableForeignEUR')
    BEGIN
        ALTER TABLE [dbo].[PaymentMethodConfigs] ADD EnableForeignEUR BIT NOT NULL DEFAULT 0;
        PRINT 'Added EnableForeignEUR column';
    END
    
    -- Update existing records to enable foreign currencies
    UPDATE [dbo].[PaymentMethodConfigs] 
    SET EnableForeignUSD = 1, EnableForeignEUR = 1;
    
    PRINT 'Updated existing configuration to enable USD and EUR';
END

-- Show final configuration
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

PRINT 'PaymentMethodConfigs setup completed!';