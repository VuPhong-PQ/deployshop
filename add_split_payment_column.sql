-- Add SplitPaymentDetails column to Orders table for multi-payment support
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Orders]') AND name = 'SplitPaymentDetails')
BEGIN
    ALTER TABLE [dbo].[Orders] ADD [SplitPaymentDetails] NVARCHAR(MAX) NULL;
    PRINT 'Column SplitPaymentDetails added successfully';
END
ELSE
BEGIN
    PRINT 'Column SplitPaymentDetails already exists';
END
GO
