using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RetailPointBackend.Models;
using RetailPointBackend.Services;

namespace RetailPointBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DevController : ControllerBase
    {
        private readonly AppDbContext _context;
        
        public DevController(AppDbContext context)
        {
            _context = context;
        }

        // Temporary endpoint to add Data Management permissions to admin role
        [HttpPost("add-data-management-permissions")]
        public async Task<IActionResult> AddDataManagementPermissions()
        {
            try
            {
                // Get admin role (assuming roleId = 1 or first role)
                var adminRole = await _context.Roles.FirstOrDefaultAsync();
                if (adminRole == null)
                {
                    return BadRequest("No role found");
                }

                // Get Data Management permissions
                var dataManagementPermissions = await _context.Permissions
                    .Where(p => p.Category == "DataManagement")
                    .ToListAsync();

                if (!dataManagementPermissions.Any())
                {
                    return BadRequest("Data Management permissions not found. Make sure SeedDataService has run.");
                }

                // Add permissions to admin role if not already exists
                var addedPermissions = new List<string>();
                
                foreach (var permission in dataManagementPermissions)
                {
                    var exists = await _context.RolePermissions
                        .AnyAsync(rp => rp.RoleId == adminRole.RoleId && rp.PermissionId == permission.PermissionId);
                    
                    if (!exists)
                    {
                        _context.RolePermissions.Add(new RolePermission
                        {
                            RoleId = adminRole.RoleId,
                            PermissionId = permission.PermissionId
                        });
                        addedPermissions.Add(permission.PermissionName);
                    }
                }

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Data Management permissions added successfully",
                    roleName = adminRole.RoleName,
                    addedPermissions = addedPermissions,
                    allDataManagementPermissions = dataManagementPermissions.Select(p => p.PermissionName).ToList()
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error adding permissions", error = ex.Message });
            }
        }

        // Reseed permissions (add new ones if missing)
        [HttpPost("reseed-permissions")]
        public async Task<IActionResult> ReseedPermissions()
        {
            try
            {
                var seedDataService = new SeedDataService(_context);
                await seedDataService.SeedAsync();

                return Ok(new { message = "Permissions reseeded successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error reseeding permissions", error = ex.Message });
            }
        }

        // Add store management permissions to admin role
        [HttpPost("add-store-permissions")]
        public async Task<IActionResult> AddStorePermissions()
        {
            try
            {
                // Get admin role
                var adminRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Admin");
                if (adminRole == null)
                {
                    return BadRequest("Admin role not found");
                }

                // Get store management permissions
                var storePermissions = await _context.Permissions
                    .Where(p => p.Category == "Stores")
                    .ToListAsync();

                if (!storePermissions.Any())
                {
                    return BadRequest("Store permissions not found. Make sure SeedDataService has run.");
                }

                var addedPermissions = new List<string>();
                
                foreach (var permission in storePermissions)
                {
                    var exists = await _context.RolePermissions
                        .AnyAsync(rp => rp.RoleId == adminRole.RoleId && rp.PermissionId == permission.PermissionId);
                    
                    if (!exists)
                    {
                        _context.RolePermissions.Add(new RolePermission
                        {
                            RoleId = adminRole.RoleId,
                            PermissionId = permission.PermissionId
                        });
                        addedPermissions.Add(permission.PermissionName);
                    }
                }

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Store permissions added to Admin role successfully",
                    addedPermissions = addedPermissions,
                    allStorePermissions = storePermissions.Select(p => p.PermissionName).ToList()
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error adding store permissions", error = ex.Message });
            }
        }

        // Add ViewDashboard permission to Admin role
        [HttpPost("add-dashboard-permission")]
        public async Task<IActionResult> AddDashboardPermission()
        {
            try
            {
                // Get admin role
                var adminRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Admin");
                if (adminRole == null)
                {
                    return BadRequest("Admin role not found");
                }

                // Get ViewDashboard permission
                var dashboardPermission = await _context.Permissions
                    .FirstOrDefaultAsync(p => p.PermissionName == "ViewDashboard");
                if (dashboardPermission == null)
                {
                    return BadRequest("ViewDashboard permission not found");
                }

                // Check if already exists
                var exists = await _context.RolePermissions
                    .AnyAsync(rp => rp.RoleId == adminRole.RoleId && rp.PermissionId == dashboardPermission.PermissionId);

                if (!exists)
                {
                    _context.RolePermissions.Add(new RolePermission
                    {
                        RoleId = adminRole.RoleId,
                        PermissionId = dashboardPermission.PermissionId
                    });
                    await _context.SaveChangesAsync();
                    
                    return Ok(new { message = "ViewDashboard permission added to Admin role successfully" });
                }
                else
                {
                    return Ok(new { message = "ViewDashboard permission already exists for Admin role" });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error adding dashboard permission", error = ex.Message });
            }
        }

        // Get current user permissions for debugging
        [HttpGet("user-permissions/{staffId}")]
        public async Task<IActionResult> GetUserPermissions(int staffId)
        {
            try
            {
                var staff = await _context.Staffs
                    .Include(s => s.Role)
                        .ThenInclude(r => r.RolePermissions)
                            .ThenInclude(rp => rp.Permission)
                    .FirstOrDefaultAsync(s => s.StaffId == staffId);

                if (staff == null)
                {
                    return NotFound("Staff not found");
                }

                object permissions;
                if (staff.Role?.RolePermissions != null)
                {
                    permissions = staff.Role.RolePermissions
                        .Select(rp => new
                        {
                            rp.Permission.PermissionName,
                            rp.Permission.Category,
                            rp.Permission.Description
                        })
                        .ToList();
                }
                else
                {
                    permissions = new List<object>();
                }

                return Ok(new
                {
                    staffId = staff.StaffId,
                    username = staff.Username,
                    roleName = staff.Role?.RoleName,
                    permissions = permissions
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error getting permissions", error = ex.Message });
            }
        }

        // Create test data for discount system
        [HttpPost("create-discount-test-data")]
        public async Task<IActionResult> CreateDiscountTestData()
        {
            try
            {
                // Create sample orders with discounts
                var orders = new List<Order>();
                var orderItems = new List<OrderItem>();
                var orderDiscounts = new List<OrderDiscount>();
                
                // Order 1: 10% discount on total
                var order1 = new Order
                {
                    CustomerName = "Khách hàng test 1",
                    PaymentMethod = "cash",
                    PaymentStatus = "paid",
                    Status = "completed",
                    SubTotal = 500000,
                    TaxAmount = 0,
                    DiscountAmount = 50000,
                    TotalAmount = 450000,
                    CreatedAt = DateTime.Now.AddDays(-5)
                };
                orders.Add(order1);

                // Order 2: Fixed amount discount
                var order2 = new Order
                {
                    CustomerName = "Khách hàng test 2",
                    PaymentMethod = "card",
                    PaymentStatus = "paid",
                    Status = "completed",
                    SubTotal = 800000,
                    TaxAmount = 0,
                    DiscountAmount = 100000,
                    TotalAmount = 700000,
                    CreatedAt = DateTime.Now.AddDays(-3)
                };
                orders.Add(order2);

                // Order 3: No discount
                var order3 = new Order
                {
                    CustomerName = "Khách hàng test 3",
                    PaymentMethod = "cash",
                    PaymentStatus = "paid",
                    Status = "completed", 
                    SubTotal = 300000,
                    TaxAmount = 0,
                    DiscountAmount = 0,
                    TotalAmount = 300000,
                    CreatedAt = DateTime.Now.AddDays(-1)
                };
                orders.Add(order3);

                // Add orders to context first to get IDs
                _context.Orders.AddRange(orders);
                await _context.SaveChangesAsync();

                // Create OrderItems
                var orderItem1_1 = new OrderItem
                {
                    OrderId = order1.OrderId,
                    ProductName = "Sản phẩm A",
                    Price = 300000,
                    Quantity = 1,
                    TotalPrice = 300000,
                    FinalPrice = 270000,
                    DiscountAmount = 30000
                };
                
                var orderItem1_2 = new OrderItem
                {
                    OrderId = order1.OrderId,
                    ProductName = "Sản phẩm B",
                    Price = 200000,
                    Quantity = 1,
                    TotalPrice = 200000,
                    FinalPrice = 180000,
                    DiscountAmount = 20000
                };

                var orderItem2_1 = new OrderItem
                {
                    OrderId = order2.OrderId,
                    ProductName = "Sản phẩm C",
                    Price = 800000,
                    Quantity = 1,
                    TotalPrice = 800000,
                    FinalPrice = 700000,
                    DiscountAmount = 100000
                };

                var orderItem3_1 = new OrderItem
                {
                    OrderId = order3.OrderId,
                    ProductName = "Sản phẩm D",
                    Price = 300000,
                    Quantity = 1,
                    TotalPrice = 300000,
                    FinalPrice = 300000,
                    DiscountAmount = 0
                };

                orderItems.AddRange(new[] { orderItem1_1, orderItem1_2, orderItem2_1, orderItem3_1 });
                _context.OrderItems.AddRange(orderItems);
                await _context.SaveChangesAsync();

                // Get admin staff ID
                var adminStaff = await _context.Staffs.FirstOrDefaultAsync(s => s.Username == "admin");
                int? adminStaffId = adminStaff?.StaffId;

                // Create OrderDiscounts for orders with discounts (first need to create Discount entities)
                var discount1 = new Discount
                {
                    Name = "Giảm giá 10% tổng bill",
                    Type = DiscountType.PercentageTotal,
                    Value = 10,
                    IsActive = true,
                    CreatedAt = DateTime.Now.AddDays(-10),
                    CreatedBy = adminStaffId
                };

                var discount2 = new Discount
                {
                    Name = "Giảm giá 100k cho tổng bill",
                    Type = DiscountType.FixedAmountTotal,
                    Value = 100000,
                    IsActive = true,
                    CreatedAt = DateTime.Now.AddDays(-10),
                    CreatedBy = adminStaffId
                };

                _context.Discounts.AddRange(new[] { discount1, discount2 });
                await _context.SaveChangesAsync();

                var orderDiscount1 = new OrderDiscount
                {
                    OrderId = order1.OrderId,
                    DiscountId = discount1.DiscountId,
                    DiscountType = DiscountType.PercentageTotal,
                    DiscountValue = 10,
                    DiscountAmount = 50000,
                    DiscountName = "Giảm giá 10% tổng bill",
                    AppliedBy = adminStaffId,
                    AppliedAt = order1.CreatedAt
                };

                var orderDiscount2 = new OrderDiscount
                {
                    OrderId = order2.OrderId,
                    DiscountId = discount2.DiscountId,
                    DiscountType = DiscountType.FixedAmountTotal,
                    DiscountValue = 100000,
                    DiscountAmount = 100000,
                    DiscountName = "Giảm giá 100k cho tổng bill",
                    AppliedBy = adminStaffId,
                    AppliedAt = order2.CreatedAt
                };

                _context.OrderDiscounts.AddRange(new[] { orderDiscount1, orderDiscount2 });
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Đã tạo dữ liệu test thành công",
                    ordersCreated = orders.Count,
                    orderItemsCreated = orderItems.Count,
                    discountsCreated = 2,
                    orderDiscountsCreated = 2,
                    orderIds = orders.Select(o => o.OrderId).ToList()
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi tạo dữ liệu test", error = ex.Message });
            }
        }
    }
}