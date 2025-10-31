using Microsoft.EntityFrameworkCore;
using RetailPointBackend.Models;
using BCrypt.Net;

namespace RetailPointBackend.Services
{
    public class SeedDataService
    {
        private readonly AppDbContext _context;

        public SeedDataService(AppDbContext context)
        {
            _context = context;
        }

        public async Task SeedAsync()
        {
            await SeedPermissionsAsync();
            await SeedRolesAsync();
            await SeedDefaultStaffAsync();
        }

        private async Task SeedPermissionsAsync()
        {
            // Define all permissions
            var permissions = new List<Permission>
            {
                // Dashboard
                new Permission { PermissionName = "ViewDashboard", Description = "Xem dashboard", Category = "Dashboard" },

                // Product Management
                new Permission { PermissionName = "ViewProducts", Description = "Xem danh sách sản phẩm", Category = "Products" },
                new Permission { PermissionName = "CreateProducts", Description = "Tạo sản phẩm mới", Category = "Products" },
                new Permission { PermissionName = "UpdateProducts", Description = "Cập nhật sản phẩm", Category = "Products" },
                new Permission { PermissionName = "DeleteProducts", Description = "Xóa sản phẩm", Category = "Products" },

                // Sales Management  
                new Permission { PermissionName = "ViewOrders", Description = "Xem đơn hàng", Category = "Sales" },
                new Permission { PermissionName = "CreateOrders", Description = "Tạo đơn hàng mới", Category = "Sales" },
                new Permission { PermissionName = "UpdateOrders", Description = "Cập nhật đơn hàng", Category = "Sales" },
                new Permission { PermissionName = "DeleteOrders", Description = "Xóa đơn hàng", Category = "Sales" },
                new Permission { PermissionName = "ProcessPayment", Description = "Xử lý thanh toán", Category = "Sales" },

                // Customer Management
                new Permission { PermissionName = "ViewCustomers", Description = "Xem khách hàng", Category = "Customers" },
                new Permission { PermissionName = "CreateCustomers", Description = "Tạo khách hàng mới", Category = "Customers" },
                new Permission { PermissionName = "UpdateCustomers", Description = "Cập nhật khách hàng", Category = "Customers" },
                new Permission { PermissionName = "DeleteCustomers", Description = "Xóa khách hàng", Category = "Customers" },

                // Reports
                new Permission { PermissionName = "ViewReports", Description = "Xem báo cáo", Category = "Reports" },
                new Permission { PermissionName = "ExportReports", Description = "Xuất báo cáo", Category = "Reports" },

                // Staff Management
                new Permission { PermissionName = "ViewStaff", Description = "Xem danh sách nhân viên", Category = "Staff" },
                new Permission { PermissionName = "CreateStaff", Description = "Tạo nhân viên mới", Category = "Staff" },
                new Permission { PermissionName = "UpdateStaff", Description = "Cập nhật nhân viên", Category = "Staff" },
                new Permission { PermissionName = "DeleteStaff", Description = "Xóa nhân viên", Category = "Staff" },

                // Role Management
                new Permission { PermissionName = "ViewRoles", Description = "Xem roles", Category = "Roles" },
                new Permission { PermissionName = "CreateRoles", Description = "Tạo roles mới", Category = "Roles" },
                new Permission { PermissionName = "UpdateRoles", Description = "Cập nhật roles", Category = "Roles" },
                new Permission { PermissionName = "DeleteRoles", Description = "Xóa roles", Category = "Roles" },

                // Store Management
                new Permission { PermissionName = "ViewStores", Description = "Xem danh sách cửa hàng", Category = "Stores" },
                new Permission { PermissionName = "CreateStores", Description = "Tạo cửa hàng mới", Category = "Stores" },
                new Permission { PermissionName = "UpdateStores", Description = "Cập nhật thông tin cửa hàng", Category = "Stores" },
                new Permission { PermissionName = "DeleteStores", Description = "Xóa/vô hiệu hóa cửa hàng", Category = "Stores" },
                new Permission { PermissionName = "ManageStores", Description = "Quản lý toàn bộ cửa hàng", Category = "Stores" },

                // System Settings
                new Permission { PermissionName = "ViewSettings", Description = "Xem cài đặt hệ thống", Category = "Settings" },
                new Permission { PermissionName = "UpdateSettings", Description = "Cập nhật cài đặt hệ thống", Category = "Settings" },
                new Permission { PermissionName = "ViewPaymentSettings", Description = "Xem cài đặt thanh toán", Category = "Settings" },
                new Permission { PermissionName = "UpdatePaymentSettings", Description = "Cập nhật cài đặt thanh toán", Category = "Settings" },

                // Data Management
                new Permission { PermissionName = "ViewDataManagement", Description = "Xem trang quản lý dữ liệu", Category = "DataManagement" },
                new Permission { PermissionName = "BackupDatabase", Description = "Sao lưu cơ sở dữ liệu", Category = "DataManagement" },
                new Permission { PermissionName = "RestoreDatabase", Description = "Phục hồi cơ sở dữ liệu", Category = "DataManagement" },
                new Permission { PermissionName = "DeleteSalesData", Description = "Xóa dữ liệu bán hàng", Category = "DataManagement" }
            };

            foreach (var permission in permissions)
            {
                if (!await _context.Permissions.AnyAsync(p => p.PermissionName == permission.PermissionName))
                {
                    _context.Permissions.Add(permission);
                }
            }

            await _context.SaveChangesAsync();
        }

        private async Task SeedRolesAsync()
        {
            // Create Admin role if not exists
            var adminRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Admin");
            if (adminRole == null)
            {
                adminRole = new Role
                {
                    RoleName = "Admin",
                    Description = "Quản trị viên - có tất cả quyền trong hệ thống"
                };
                _context.Roles.Add(adminRole);
                await _context.SaveChangesAsync();

                // Add all permissions to Admin role
                var allPermissions = await _context.Permissions.ToListAsync();
                foreach (var permission in allPermissions)
                {
                    _context.RolePermissions.Add(new RolePermission
                    {
                        RoleId = adminRole.RoleId,
                        PermissionId = permission.PermissionId
                    });
                }
            }

            // Create Cashier role if not exists  
            var cashierRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Thu ngân");
            if (cashierRole == null)
            {
                cashierRole = new Role
                {
                    RoleName = "Thu ngân",
                    Description = "Thu ngân - chỉ được phép bán hàng và quản lý khách hàng"
                };
                _context.Roles.Add(cashierRole);
                await _context.SaveChangesAsync();

                // Add limited permissions to Cashier role
                var cashierPermissions = new[]
                {
                    "ViewProducts",
                    "ViewOrders", "CreateOrders", "UpdateOrders", "ProcessPayment",
                    "ViewCustomers", "CreateCustomers", "UpdateCustomers"
                };

                var permissions = await _context.Permissions
                    .Where(p => cashierPermissions.Contains(p.PermissionName))
                    .ToListAsync();

                foreach (var permission in permissions)
                {
                    _context.RolePermissions.Add(new RolePermission
                    {
                        RoleId = cashierRole.RoleId,
                        PermissionId = permission.PermissionId
                    });
                }
            }

            await _context.SaveChangesAsync();
        }

        private async Task SeedDefaultStaffAsync()
        {
            // Create or update default admin
            var adminRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Admin");
            if (adminRole != null)
            {
                var existingAdmin = await _context.Staffs.FirstOrDefaultAsync(s => s.Username == "admin");
                if (existingAdmin == null)
                {
                    // Create new admin
                    var adminStaff = new Staff
                    {
                        FullName = "Quản trị viên",
                        Username = "admin",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("vuphong"),
                        Email = "admin@retailpoint.com",
                        RoleId = adminRole.RoleId,
                        IsActive = true,
                        Notes = "Tài khoản quản trị mặc định"
                    };

                    _context.Staffs.Add(adminStaff);
                }
                else
                {
                    // Update existing admin password
                    existingAdmin.PasswordHash = BCrypt.Net.BCrypt.HashPassword("vuphong");
                    existingAdmin.IsActive = true;
                    _context.Staffs.Update(existingAdmin);
                }
            }

            // Create default cashier if not exists
            if (!await _context.Staffs.AnyAsync(s => s.Username == "cashier"))
            {
                var cashierRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Thu ngân");
                if (cashierRole != null)
                {
                    var cashierStaff = new Staff
                    {
                        FullName = "Thu ngân",
                        Username = "cashier",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("cashier123"),
                        Email = "cashier@retailpoint.com",
                        RoleId = cashierRole.RoleId,
                        IsActive = true,
                        Notes = "Tài khoản thu ngân mặc định"
                    };

                    _context.Staffs.Add(cashierStaff);
                }
            }

            await _context.SaveChangesAsync();
        }
    }
}