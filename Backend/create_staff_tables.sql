-- Create Staff Management Tables manually
-- Run this script in SQL Server Management Studio or via sqlcmd

-- Create Roles table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Roles' AND xtype='U')
BEGIN
    CREATE TABLE [dbo].[Roles] (
        [RoleId] int IDENTITY(1,1) NOT NULL,
        [RoleName] nvarchar(50) NOT NULL,
        [Description] nvarchar(200) NULL,
        [CreatedAt] datetime2(7) NOT NULL DEFAULT GETDATE(),
        CONSTRAINT [PK_Roles] PRIMARY KEY ([RoleId])
    );
END

-- Create Permissions table  
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Permissions' AND xtype='U')
BEGIN
    CREATE TABLE [dbo].[Permissions] (
        [PermissionId] int IDENTITY(1,1) NOT NULL,
        [PermissionName] nvarchar(50) NOT NULL,
        [Description] nvarchar(200) NULL,
        [Category] nvarchar(100) NULL,
        [CreatedAt] datetime2(7) NOT NULL DEFAULT GETDATE(),
        CONSTRAINT [PK_Permissions] PRIMARY KEY ([PermissionId])
    );
END

-- Create RolePermissions table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='RolePermissions' AND xtype='U')
BEGIN
    CREATE TABLE [dbo].[RolePermissions] (
        [RolePermissionId] int IDENTITY(1,1) NOT NULL,
        [RoleId] int NOT NULL,
        [PermissionId] int NOT NULL,
        [CreatedAt] datetime2(7) NOT NULL DEFAULT GETDATE(),
        CONSTRAINT [PK_RolePermissions] PRIMARY KEY ([RolePermissionId]),
        CONSTRAINT [FK_RolePermissions_Roles] FOREIGN KEY ([RoleId]) REFERENCES [dbo].[Roles] ([RoleId]) ON DELETE CASCADE,
        CONSTRAINT [FK_RolePermissions_Permissions] FOREIGN KEY ([PermissionId]) REFERENCES [dbo].[Permissions] ([PermissionId]) ON DELETE CASCADE
    );
END

-- Create Staffs table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Staffs' AND xtype='U')
BEGIN
    CREATE TABLE [dbo].[Staffs] (
        [StaffId] int IDENTITY(1,1) NOT NULL,
        [FullName] nvarchar(100) NOT NULL,
        [Username] nvarchar(50) NOT NULL,
        [PasswordHash] nvarchar(256) NOT NULL,
        [Email] nvarchar(100) NULL,
        [PhoneNumber] nvarchar(20) NULL,
        [RoleId] int NOT NULL,
        [IsActive] bit NOT NULL DEFAULT 1,
        [CreatedAt] datetime2(7) NOT NULL DEFAULT GETDATE(),
        [LastLogin] datetime2(7) NULL,
        [Notes] nvarchar(500) NULL,
        CONSTRAINT [PK_Staffs] PRIMARY KEY ([StaffId]),
        CONSTRAINT [FK_Staffs_Roles] FOREIGN KEY ([RoleId]) REFERENCES [dbo].[Roles] ([RoleId]) ON DELETE NO ACTION
    );
END

-- Add StaffId column to Orders table if not exists
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Orders') AND name = 'StaffId')
BEGIN
    ALTER TABLE [Orders] ADD [StaffId] int NULL;
END

-- Add foreign key constraint from Orders to Staffs if not exists
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Orders_Staffs')
BEGIN
    ALTER TABLE [Orders] ADD CONSTRAINT [FK_Orders_Staffs] FOREIGN KEY ([StaffId]) REFERENCES [dbo].[Staffs] ([StaffId]) ON DELETE SET NULL;
END

PRINT 'Staff Management Tables created successfully!'