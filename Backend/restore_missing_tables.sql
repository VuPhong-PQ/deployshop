-- Script để khôi phục bảng Notifications nếu bị mất
-- Thực thi script này nếu bảng Notifications bị lỗi hoặc mất dữ liệu

-- Kiểm tra xem bảng có tồn tại không
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Notifications' AND xtype='U')
BEGIN
    -- Tạo lại bảng Notifications
    CREATE TABLE [dbo].[Notifications] (
        [NotificationId] [int] IDENTITY(1,1) NOT NULL,
        [Type] [int] NOT NULL,
        [Title] [nvarchar](500) NOT NULL,
        [Message] [nvarchar](1000) NULL,
        [Status] [int] NOT NULL,
        [CreatedAt] [datetime2](7) NOT NULL,
        [ReadAt] [datetime2](7) NULL,
        [OrderId] [int] NULL,
        [ProductId] [int] NULL,
        [CustomerId] [int] NULL,
        [Metadata] [nvarchar](max) NULL,
        CONSTRAINT [PK_Notifications] PRIMARY KEY CLUSTERED ([NotificationId] ASC)
    );

    -- Tạo index
    CREATE INDEX [IX_Notifications_OrderId] ON [dbo].[Notifications] ([OrderId]);

    -- Tạo foreign key constraint nếu bảng Orders tồn tại
    IF EXISTS (SELECT * FROM sysobjects WHERE name='Orders' AND xtype='U')
    BEGIN
        ALTER TABLE [dbo].[Notifications] 
        ADD CONSTRAINT [FK_Notifications_Orders_OrderId] 
        FOREIGN KEY ([OrderId]) REFERENCES [dbo].[Orders] ([OrderId]);
    END

    PRINT 'Bảng Notifications đã được tạo lại thành công';
END
ELSE
BEGIN
    PRINT 'Bảng Notifications đã tồn tại';
END

-- Kiểm tra encoding của database
SELECT 
    name as DatabaseName,
    collation_name as Collation,
    SERVERPROPERTY('Collation') as ServerCollation
FROM sys.databases 
WHERE name = 'RetailPoint';

-- Kiểm tra collation của bảng Notifications nếu có
IF EXISTS (SELECT * FROM sysobjects WHERE name='Notifications' AND xtype='U')
BEGIN
    SELECT 
        COLUMN_NAME,
        DATA_TYPE,
        CHARACTER_SET_NAME,
        COLLATION_NAME
    FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'Notifications' 
    AND DATA_TYPE IN ('nvarchar', 'varchar', 'char', 'nchar', 'text', 'ntext');
END

PRINT 'Script hoàn thành!';