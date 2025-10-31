-- Tạo user test cho đăng nhập
-- Đầu tiên tạo role Admin nếu chưa có
INSERT INTO Roles (RoleName, Description, IsActive) 
VALUES ('Admin', 'Quản trị viên hệ thống', 1)
WHERE NOT EXISTS (SELECT 1 FROM Roles WHERE RoleName = 'Admin');

-- Lấy RoleId của Admin
DECLARE @AdminRoleId INT = (SELECT RoleId FROM Roles WHERE RoleName = 'Admin');

-- Tạo user admin với password là "admin123"
-- Password hash cho "admin123" bằng BCrypt
INSERT INTO Staffs (FullName, Username, PasswordHash, Email, RoleId, IsActive, CreatedAt)
VALUES 
('Administrator', 'admin', '$2a$11$EixZaYVK1fsbw1ZfbX3OXePaWxn96p36WQoeG6Lruj3vjPjiCZ7QC', 'admin@retailpoint.com', @AdminRoleId, 1, GETDATE())
WHERE NOT EXISTS (SELECT 1 FROM Staffs WHERE Username = 'admin');

-- Tạo user test với password là "123456"  
-- Password hash cho "123456" bằng BCrypt
INSERT INTO Staffs (FullName, Username, PasswordHash, Email, RoleId, IsActive, CreatedAt)
VALUES 
('Test User', 'test', '$2a$11$EixZaYVK1fsbw1ZfbX3OXePaWxn96p36WQoeG6Lruj3vjPjiCZ7QC', 'test@retailpoint.com', @AdminRoleId, 1, GETDATE())
WHERE NOT EXISTS (SELECT 1 FROM Staffs WHERE Username = 'test');

-- Tạo một số permissions cơ bản
INSERT INTO Permissions (PermissionName, Description, IsActive) 
VALUES 
('ViewSales', 'Xem bán hàng', 1),
('ManageProducts', 'Quản lý sản phẩm', 1),
('ManageStaff', 'Quản lý nhân viên', 1),
('ViewReports', 'Xem báo cáo', 1)
WHERE NOT EXISTS (SELECT 1 FROM Permissions WHERE PermissionName IN ('ViewSales', 'ManageProducts', 'ManageStaff', 'ViewReports'));

-- Gán quyền cho role Admin
INSERT INTO RolePermissions (RoleId, PermissionId)
SELECT @AdminRoleId, PermissionId 
FROM Permissions 
WHERE PermissionName IN ('ViewSales', 'ManageProducts', 'ManageStaff', 'ViewReports')
AND NOT EXISTS (
    SELECT 1 FROM RolePermissions 
    WHERE RoleId = @AdminRoleId AND PermissionId = Permissions.PermissionId
);

-- Kiểm tra kết quả
SELECT 'Users created:' as Message;
SELECT StaffId, FullName, Username, Email, IsActive FROM Staffs WHERE Username IN ('admin', 'test');

SELECT 'Roles created:' as Message;
SELECT RoleId, RoleName, Description FROM Roles WHERE RoleName = 'Admin';

SELECT 'Permissions created:' as Message;
SELECT PermissionId, PermissionName, Description FROM Permissions;