-- Thêm dữ liệu Store mẫu
INSERT INTO [Stores] ([Name], [Address], [Phone], [Email], [TaxCode], [Manager], [IsActive], [CreatedAt], [UpdatedAt], [Notes])
VALUES 
    (N'Cửa hàng Trung tâm', N'123 Lê Lợi, Quận 1, TP.HCM', '028-3824-1234', 'trungtam@retailpoint.com', '0123456789', N'Nguyễn Văn An', 1, GETDATE(), GETDATE(), N'Cửa hàng chính tại trung tâm thành phố'),
    (N'Cửa hàng Quận 7', N'456 Nguyễn Thị Thập, Quận 7, TP.HCM', '028-5412-5678', 'quan7@retailpoint.com', '0123456790', N'Trần Thị Bình', 1, GETDATE(), GETDATE(), N'Chi nhánh tại Quận 7, khu dân cư cao cấp'),
    (N'Cửa hàng Thủ Đức', N'789 Võ Văn Ngân, TP. Thủ Đức, TP.HCM', '028-7234-9012', 'thuduc@retailpoint.com', '0123456791', N'Lê Văn Cường', 1, GETDATE(), GETDATE(), N'Chi nhánh tại Thủ Đức, gần các trường đại học');

-- Cập nhật Admin để không thuộc store cụ thể nào (có thể quản lý tất cả)
-- StoreId = NULL cho Admin để có thể truy cập tất cả store

-- Cập nhật user cashier để thuộc store đầu tiên
UPDATE [Staffs] 
SET [StoreId] = 1 
WHERE [Username] = 'cashier' AND EXISTS (SELECT 1 FROM [Stores] WHERE [StoreId] = 1);

-- In thông tin để kiểm tra
SELECT 'Stores created:' as Info;
SELECT [StoreId], [Name], [Address], [Manager], [IsActive] FROM [Stores];

SELECT 'Staff assignments:' as Info;
SELECT s.[StaffId], s.[Username], s.[FullName], s.[StoreId], st.[Name] as StoreName, r.[RoleName]
FROM [Staffs] s 
LEFT JOIN [Stores] st ON s.[StoreId] = st.[StoreId]
INNER JOIN [Roles] r ON s.[RoleId] = r.[RoleId]
WHERE s.[IsActive] = 1;