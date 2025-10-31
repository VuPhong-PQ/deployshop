-- Thêm permissions cho user admin (giả sử có staffId = 1)
-- Trước tiên kiểm tra xem permissions đã tồn tại chưa
SELECT * FROM Permissions WHERE Category = 'DataManagement';

-- Nếu chưa có, thêm permissions (đã có trong SeedDataService, chỉ cần chạy lại seed)
-- Kiểm tra role của admin user
SELECT s.StaffId, s.Username, r.RoleName, r.RoleId
FROM Staffs s
INNER JOIN Roles r ON s.RoleId = r.RoleId
WHERE s.Username = 'admin';

-- Thêm permissions vào role admin (giả sử roleId = 1)
-- Lấy permission IDs
DECLARE @ViewDataManagementId INT = (SELECT PermissionId FROM Permissions WHERE PermissionName = 'ViewDataManagement');
DECLARE @BackupDatabaseId INT = (SELECT PermissionId FROM Permissions WHERE PermissionName = 'BackupDatabase');
DECLARE @RestoreDatabaseId INT = (SELECT PermissionId FROM Permissions WHERE PermissionName = 'RestoreDatabase');
DECLARE @DeleteSalesDataId INT = (SELECT PermissionId FROM Permissions WHERE PermissionName = 'DeleteSalesData');
DECLARE @DeleteAllDataId INT = (SELECT PermissionId FROM Permissions WHERE PermissionName = 'DeleteAllData');

-- Thêm vào role admin (roleId = 1)
IF @ViewDataManagementId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM RolePermissions WHERE RoleId = 1 AND PermissionId = @ViewDataManagementId)
    INSERT INTO RolePermissions (RoleId, PermissionId) VALUES (1, @ViewDataManagementId);

IF @BackupDatabaseId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM RolePermissions WHERE RoleId = 1 AND PermissionId = @BackupDatabaseId)
    INSERT INTO RolePermissions (RoleId, PermissionId) VALUES (1, @BackupDatabaseId);

IF @RestoreDatabaseId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM RolePermissions WHERE RoleId = 1 AND PermissionId = @RestoreDatabaseId)
    INSERT INTO RolePermissions (RoleId, PermissionId) VALUES (1, @RestoreDatabaseId);

IF @DeleteSalesDataId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM RolePermissions WHERE RoleId = 1 AND PermissionId = @DeleteSalesDataId)
    INSERT INTO RolePermissions (RoleId, PermissionId) VALUES (1, @DeleteSalesDataId);

IF @DeleteAllDataId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM RolePermissions WHERE RoleId = 1 AND PermissionId = @DeleteAllDataId)
    INSERT INTO RolePermissions (RoleId, PermissionId) VALUES (1, @DeleteAllDataId);

-- Kiểm tra kết quả
SELECT r.RoleName, p.PermissionName, p.Category
FROM RolePermissions rp
INNER JOIN Roles r ON rp.RoleId = r.RoleId
INNER JOIN Permissions p ON rp.PermissionId = p.PermissionId
WHERE p.Category = 'DataManagement'
ORDER BY r.RoleName, p.PermissionName;