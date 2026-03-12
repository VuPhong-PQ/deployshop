-- =============================================
-- Script: Thêm permission ViewReSales cho tính năng Bán hàng bổ sung
-- Ngày tạo: 2026-03-11
-- Mô tả: Thêm quyền ViewReSales vào bảng Permissions và gán cho role Admin
-- =============================================

-- 1. Thêm permission ViewReSales nếu chưa tồn tại
IF NOT EXISTS (SELECT 1 FROM Permissions WHERE PermissionName = 'ViewReSales')
BEGIN
    INSERT INTO Permissions (PermissionName, Description, Category)
    VALUES ('ViewReSales', N'Xem và sử dụng trang Bán hàng bổ sung (Re-Sales)', 'Orders');
    PRINT N'Đã thêm permission ViewReSales thành công.';
END
ELSE
BEGIN
    PRINT N'Permission ViewReSales đã tồn tại.';
END;

-- 2. Gán permission ViewReSales cho role Admin (nếu chưa có)
DECLARE @AdminRoleId INT = (SELECT RoleId FROM Roles WHERE RoleName = 'Admin');
DECLARE @ViewReSalesPermissionId INT = (SELECT PermissionId FROM Permissions WHERE PermissionName = 'ViewReSales');

IF @AdminRoleId IS NOT NULL AND @ViewReSalesPermissionId IS NOT NULL
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM RolePermissions 
        WHERE RoleId = @AdminRoleId AND PermissionId = @ViewReSalesPermissionId
    )
    BEGIN
        INSERT INTO RolePermissions (RoleId, PermissionId)
        VALUES (@AdminRoleId, @ViewReSalesPermissionId);
        PRINT N'Đã gán permission ViewReSales cho role Admin.';
    END
    ELSE
    BEGIN
        PRINT N'Role Admin đã có permission ViewReSales.';
    END
END
ELSE
BEGIN
    IF @AdminRoleId IS NULL
        PRINT N'Không tìm thấy role Admin.';
    IF @ViewReSalesPermissionId IS NULL
        PRINT N'Không tìm thấy permission ViewReSales.';
END;

-- 3. Kiểm tra kết quả
PRINT N'';
PRINT N'=== KẾT QUẢ ===';

SELECT PermissionId, PermissionName, Description, Category
FROM Permissions
WHERE PermissionName = 'ViewReSales';

SELECT r.RoleName, p.PermissionName
FROM RolePermissions rp
JOIN Roles r ON rp.RoleId = r.RoleId
JOIN Permissions p ON rp.PermissionId = p.PermissionId
WHERE p.PermissionName = 'ViewReSales';
