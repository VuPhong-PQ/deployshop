using Microsoft.EntityFrameworkCore;
using RetailPointBackend.Models;

namespace RetailPointBackend.Services
{
    public class FixEncodingService
    {
        private readonly AppDbContext _context;

        public FixEncodingService(AppDbContext context)
        {
            _context = context;
        }

        public async Task FixPermissionEncodingAsync()
        {
            // Mapping of permission names to correct Vietnamese descriptions
            var permissionDescriptions = new Dictionary<string, string>
            {
                // Core permissions
                { "ViewDashboard", "Xem dashboard" },
                { "ManageProducts", "Quản lý sản phẩm" },
                { "ProcessSales", "Xử lý bán hàng" },
                { "ViewReports", "Xem báo cáo" },
                { "ManageStaff", "Quản lý nhân viên" },
                { "SystemSettings", "Cài đặt hệ thống" },
                
                // Product Management
                { "ViewProducts", "Xem danh sách sản phẩm" },
                { "CreateProducts", "Tạo sản phẩm mới" },
                { "UpdateProducts", "Cập nhật sản phẩm" },
                { "DeleteProducts", "Xóa sản phẩm" },

                // Sales Management  
                { "ViewOrders", "Xem đơn hàng" },
                { "CreateOrders", "Tạo đơn hàng mới" },
                { "UpdateOrders", "Cập nhật đơn hàng" },
                { "DeleteOrders", "Xóa đơn hàng" },
                { "ProcessPayment", "Xử lý thanh toán" },

                // Customer Management
                { "ViewCustomers", "Xem khách hàng" },
                { "CreateCustomers", "Tạo khách hàng mới" },
                { "UpdateCustomers", "Cập nhật khách hàng" },
                { "DeleteCustomers", "Xóa khách hàng" },

                // Reports
                { "ExportReports", "Xuất báo cáo" },

                // Staff Management
                { "ViewStaff", "Xem danh sách nhân viên" },
                { "CreateStaff", "Tạo nhân viên mới" },
                { "UpdateStaff", "Cập nhật nhân viên" },
                { "DeleteStaff", "Xóa nhân viên" },

                // Role Management
                { "ViewRoles", "Xem vai trò" },
                { "CreateRoles", "Tạo vai trò mới" },
                { "UpdateRoles", "Cập nhật vai trò" },
                { "DeleteRoles", "Xóa vai trò" },

                // Settings
                { "ViewSettings", "Xem cài đặt hệ thống" },
                { "UpdateSettings", "Cập nhật cài đặt hệ thống" },
                { "ViewPaymentSettings", "Xem cài đặt thanh toán" },
                { "UpdatePaymentSettings", "Cập nhật cài đặt thanh toán" },

                // Inventory Management
                { "ViewInventory", "Xem thông tin kho hàng" },
                { "UpdateInventory", "Cập nhật số lượng tồn kho" },
                { "ManageInventory", "Quản lý toàn bộ kho hàng" },
                { "ViewLowStock", "Xem sản phẩm sắp hết hàng" },

                // Category Management
                { "ViewCategories", "Xem danh mục sản phẩm" },
                { "CreateCategories", "Tạo danh mục mới" },
                { "UpdateCategories", "Cập nhật danh mục" },
                { "DeleteCategories", "Xóa danh mục" },

                // Product Groups
                { "ViewProductGroups", "Xem nhóm sản phẩm" },
                { "CreateProductGroups", "Tạo nhóm sản phẩm mới" },
                { "UpdateProductGroups", "Cập nhật nhóm sản phẩm" },
                { "DeleteProductGroups", "Xóa nhóm sản phẩm" },

                // Payment Management
                { "ViewPaymentStats", "Xem thống kê thanh toán" },
                { "ManagePaymentMethods", "Quản lý phương thức thanh toán" },
                { "ProcessRefunds", "Xử lý hoàn tiền" },
                { "ViewTransactions", "Xem lịch sử giao dịch" },

                // Notifications
                { "ViewNotifications", "Xem thông báo" },
                { "CreateNotifications", "Tạo thông báo mới" },
                { "DeleteNotifications", "Xóa thông báo" },
                { "ManageNotifications", "Quản lý toàn bộ thông báo" },

                // Print Configuration
                { "ViewPrintConfig", "Xem cấu hình in" },
                { "UpdatePrintConfig", "Cập nhật cấu hình in" },
                { "PrintReceipts", "In hóa đơn" },
                { "PrintReports", "In báo cáo" },

                // Tax Configuration
                { "ViewTaxConfig", "Xem cấu hình thuế" },
                { "UpdateTaxConfig", "Cập nhật cấu hình thuế" },
                { "ManageTaxRates", "Quản lý mức thuế suất" },

                // Store Information
                { "ViewStoreInfo", "Xem thông tin cửa hàng" },
                { "UpdateStoreInfo", "Cập nhật thông tin cửa hàng" },

                // Permission Management
                { "ViewPermissions", "Xem danh sách quyền hạn" },
                { "CreatePermissions", "Tạo quyền hạn mới" },
                { "UpdatePermissions", "Cập nhật quyền hạn" },
                { "DeletePermissions", "Xóa quyền hạn" },
                { "AssignPermissions", "Phân quyền cho vai trò" },

                // File Management
                { "UploadFiles", "Tải lên tập tin" },
                { "DeleteFiles", "Xóa tập tin" },
                { "ManageFiles", "Quản lý tập tin" },

                // Advanced Reports
                { "ViewSalesReports", "Xem báo cáo bán hàng" },
                { "ViewInventoryReports", "Xem báo cáo tồn kho" },
                { "ViewCustomerReports", "Xem báo cáo khách hàng" },
                { "ViewFinancialReports", "Xem báo cáo tài chính" },
                { "ViewStaffReports", "Xem báo cáo nhân viên" },

                // System Management
                { "ViewSystemLogs", "Xem nhật ký hệ thống" },
                { "BackupData", "Sao lưu dữ liệu" },
                { "RestoreData", "Khôi phục dữ liệu" },
                { "ManageDatabase", "Quản lý cơ sở dữ liệu" }
            };

            var permissions = await _context.Permissions.ToListAsync();
            
            foreach (var permission in permissions)
            {
                if (permissionDescriptions.ContainsKey(permission.PermissionName))
                {
                    permission.Description = permissionDescriptions[permission.PermissionName];
                }
            }

            await _context.SaveChangesAsync();
        }
    }
}