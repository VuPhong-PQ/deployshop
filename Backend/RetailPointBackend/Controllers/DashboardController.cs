using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RetailPointBackend.Models;

namespace RetailPointBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly AppDbContext _context;

        public DashboardController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("metrics")]
        public IActionResult GetDashboardMetrics([FromQuery] string? storeId = null)
        {
            try
            {
                var today = DateTime.Today;
                var yesterday = today.AddDays(-1);
                var thisMonth = new DateTime(today.Year, today.Month, 1);
                var lastMonth = thisMonth.AddMonths(-1);

                // Base query for orders - xử lý trường hợp StoreId có thể null
                var ordersQuery = _context.Orders.AsQueryable();
                if (!string.IsNullOrEmpty(storeId))
                {
                    ordersQuery = ordersQuery.Where(o => o.StoreId == storeId);
                }

                // Tính toán doanh thu hôm nay
                var todayOrders = ordersQuery
                    .Where(o => o.CreatedAt.Date == today && o.PaymentStatus == "paid" && o.Status != "cancelled")
                    .ToList();
                var todayRevenue = todayOrders.Sum(o => o.TotalAmount);

                // Tính toán doanh thu hôm qua để so sánh
                var yesterdayOrders = ordersQuery
                    .Where(o => o.CreatedAt.Date == yesterday && o.PaymentStatus == "paid" && o.Status != "cancelled")
                    .ToList();
                var yesterdayRevenue = yesterdayOrders.Sum(o => o.TotalAmount);

                // Tính % tăng trưởng doanh thu
                var revenueGrowth = yesterdayRevenue > 0 
                    ? ((todayRevenue - yesterdayRevenue) / yesterdayRevenue * 100).ToString("F1") + "%"
                    : "N/A";

                // Tính tổng số đơn hàng (all time)
                var totalOrders = _context.Orders.Count();

                // Tính số đơn hàng hôm nay vs hôm qua
                var todayOrdersCount = todayOrders.Count;
                var yesterdayOrdersCount = yesterdayOrders.Count;
                var ordersGrowth = yesterdayOrdersCount > 0
                    ? ((double)(todayOrdersCount - yesterdayOrdersCount) / yesterdayOrdersCount * 100).ToString("F1") + "%"
                    : "N/A";

                // Tính số khách hàng mới hôm nay (giả sử customers có CreatedAt field)
                var newCustomersToday = _context.Customers.Count(); // Simplified for now

                // Tính số sản phẩm sắp hết hàng
                var lowStockItems = _context.Products
                    .Where(p => p.StockQuantity <= p.MinStockLevel)
                    .Count();

                // Tính doanh thu tháng này
                var thisMonthOrders = _context.Orders
                    .Where(o => o.CreatedAt >= thisMonth && o.PaymentStatus == "paid" && o.Status != "cancelled")
                    .ToList();
                var thisMonthRevenue = thisMonthOrders.Sum(o => o.TotalAmount);

                // Tính doanh thu tháng trước
                var lastMonthOrders = _context.Orders
                    .Where(o => o.CreatedAt >= lastMonth && o.CreatedAt < thisMonth && o.PaymentStatus == "paid" && o.Status != "cancelled")
                    .ToList();
                var lastMonthRevenue = lastMonthOrders.Sum(o => o.TotalAmount);

                var monthGrowth = lastMonthRevenue > 0
                    ? ((thisMonthRevenue - lastMonthRevenue) / lastMonthRevenue * 100).ToString("F1") + "%"
                    : "N/A";

                var response = new
                {
                    todayRevenue = todayRevenue.ToString("N0") + "₫",
                    todayGrowth = revenueGrowth.StartsWith("-") ? revenueGrowth : "+" + revenueGrowth,
                    monthRevenue = thisMonthRevenue.ToString("N0") + "₫", 
                    monthGrowth = monthGrowth.StartsWith("-") ? monthGrowth : "+" + monthGrowth,
                    ordersCount = totalOrders,
                    ordersGrowth = ordersGrowth.StartsWith("-") ? ordersGrowth : "+" + ordersGrowth,
                    newCustomers = newCustomersToday,
                    customersGrowth = "+0%", // Simplified
                    lowStockItems = lowStockItems,
                    // Thêm thống kê chi tiết về trạng thái đơn hàng
                    ordersByStatus = new
                    {
                        total = totalOrders,
                        paid = ordersQuery.Count(o => o.PaymentStatus == "paid"),
                        pending = ordersQuery.Count(o => o.PaymentStatus == "pending"),
                        failed = ordersQuery.Count(o => o.PaymentStatus == "failed"),
                        completed = ordersQuery.Count(o => o.Status == "completed"),
                        processing = ordersQuery.Count(o => o.Status == "pending"),
                        cancelled = ordersQuery.Count(o => o.Status == "cancelled")
                    }
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi tải thống kê dashboard", error = ex.Message });
            }
        }

        [HttpGet("recent-orders")]
        public IActionResult GetRecentOrders()
        {
            try
            {
                var recentOrders = _context.Orders
                    .Include(o => o.Customer)
                    .OrderByDescending(o => o.CreatedAt)
                    .Take(5)
                    .ToList() // Đưa về memory trước để tránh lỗi expression tree
                    .Select(o => new
                    {
                        id = o.OrderId,
                        orderNumber = "#" + o.OrderId,
                        customer = o.CustomerName ?? o.Customer?.HoTen ?? "Khách lẻ",
                        total = o.TotalAmount.ToString("N0") + "₫",
                        status = o.PaymentStatus == "paid" && o.Status == "completed" ? "Hoàn thành"
                               : o.PaymentStatus == "pending" ? "Chờ thanh toán"
                               : o.Status == "pending" ? "Đang xử lý"
                               : "Khác",
                        time = GetTimeAgo(o.CreatedAt)
                    })
                    .ToList();

                return Ok(recentOrders);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi tải đơn hàng gần đây", error = ex.Message });
            }
        }

        private string GetTimeAgo(DateTime createdAt)
        {
            var timeSpan = DateTime.Now - createdAt;
            
            if (timeSpan.TotalMinutes < 1)
                return "Vừa xong";
            if (timeSpan.TotalMinutes < 60)
                return $"{(int)timeSpan.TotalMinutes} phút trước";
            if (timeSpan.TotalHours < 24)
                return $"{(int)timeSpan.TotalHours} giờ trước";
            if (timeSpan.TotalDays < 30)
                return $"{(int)timeSpan.TotalDays} ngày trước";
            
            return createdAt.ToString("dd/MM/yyyy");
        }

        [HttpGet("metrics/stores")]
        public async Task<IActionResult> GetStoreMetrics()
        {
            try
            {
                // Lấy thông tin user từ header
                var username = HttpContext.Request.Headers["Username"].FirstOrDefault() ?? "admin";
                
                var staff = await _context.Staffs
                    .Include(s => s.Role)
                    .FirstOrDefaultAsync(s => s.Username == username && s.IsActive);
                    
                if (staff == null)
                {
                    return Unauthorized("Không tìm thấy thông tin nhân viên");
                }

                // Nếu là Admin thì có quyền truy cập tất cả stores
                IQueryable<Store> storesQuery = _context.Stores.Where(s => s.IsActive);
                
                if (staff.Role.RoleName != "Admin")
                {
                    // Lọc chỉ stores được assign cho staff này
                    var assignedStoreIds = await _context.StaffStores
                        .Where(ss => ss.StaffId == staff.StaffId)
                        .Select(ss => ss.StoreId)
                        .ToListAsync();
                    
                    storesQuery = storesQuery.Where(s => assignedStoreIds.Contains(s.StoreId));
                }

                // Lấy thông tin các cửa hàng được phép truy cập và thống kê
                var stores = await storesQuery
                    .Select(s => new
                    {
                        id = s.StoreId,
                        name = s.Name,
                        address = s.Address,
                        isActive = s.IsActive,
                        // Thống kê doanh thu theo cửa hàng - convert int StoreId to string for comparison
                        totalRevenue = _context.Orders
                            .Where(o => o.StoreId == s.StoreId.ToString() && o.PaymentStatus == "paid" && o.Status != "cancelled")
                            .Sum(o => (decimal?)o.TotalAmount) ?? 0,
                        totalOrders = _context.Orders
                            .Where(o => o.StoreId == s.StoreId.ToString())
                            .Count(),
                        todayRevenue = _context.Orders
                            .Where(o => o.StoreId == s.StoreId.ToString()
                                && o.CreatedAt.Date == DateTime.Today 
                                && o.PaymentStatus == "paid" 
                                && o.Status != "cancelled")
                            .Sum(o => (decimal?)o.TotalAmount) ?? 0
                    })
                    .ToListAsync();

                // Nếu không có cửa hàng nào, tạo dữ liệu mặc định
                if (!stores.Any())
                {
                    return Ok(new List<object>
                    {
                        new
                        {
                            id = 1,
                            name = "Cửa hàng chính",
                            address = "Chưa cập nhật địa chỉ",
                            isActive = true,
                            totalRevenue = 0,
                            totalOrders = 0,
                            todayRevenue = 0
                        }
                    });
                }

                return Ok(stores);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi tải thông tin cửa hàng", error = ex.Message });
            }
        }
    }
}