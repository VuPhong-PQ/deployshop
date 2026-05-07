using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using RetailPointBackend.Models;
using System.Security.Claims;

namespace RetailPointBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(AppDbContext context, ILogger<DashboardController> logger)
        {
            _context = context;
            _logger = logger;
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

                // Base query for orders - xá»­ lÃ½ trÆ°á»ng há»£p StoreId cÃ³ thá» null
                var ordersQuery = _context.Orders.AsQueryable();
                if (!string.IsNullOrEmpty(storeId))
                {
                    ordersQuery = ordersQuery.Where(o => o.StoreId == storeId);
                }

                // TÃ­nh toÃ¡n doanh thu hÃ´m nay
                var todayOrders = ordersQuery
                    .Where(o => o.CreatedAt.Date == today && o.PaymentStatus == "paid" && o.Status != "cancelled")
                    .ToList();
                var todayRevenue = todayOrders.Sum(o => o.TotalAmount);

                // TÃ­nh toÃ¡n doanh thu hÃ´m qua Äá» so sÃ¡nh
                var yesterdayOrders = ordersQuery
                    .Where(o => o.CreatedAt.Date == yesterday && o.PaymentStatus == "paid" && o.Status != "cancelled")
                    .ToList();
                var yesterdayRevenue = yesterdayOrders.Sum(o => o.TotalAmount);

                // TÃ­nh % tÄng trÆ°á»ng doanh thu
                var revenueGrowth = yesterdayRevenue > 0 
                    ? ((todayRevenue - yesterdayRevenue) / yesterdayRevenue * 100).ToString("F1") + "%"
                    : "N/A";

                // TÃ­nh tá»ng sá» ÄÆ¡n hÃ ng (all time)
                var totalOrders = _context.Orders.Count();

                // TÃ­nh sá» ÄÆ¡n hÃ ng hÃ´m nay vs hÃ´m qua
                var todayOrdersCount = todayOrders.Count;
                var yesterdayOrdersCount = yesterdayOrders.Count;
                var ordersGrowth = yesterdayOrdersCount > 0
                    ? ((double)(todayOrdersCount - yesterdayOrdersCount) / yesterdayOrdersCount * 100).ToString("F1") + "%"
                    : "N/A";

                // TÃ­nh sá» khÃ¡ch hÃ ng má»i hÃ´m nay (giáº£ sá»­ customers cÃ³ CreatedAt field)
                var newCustomersToday = _context.Customers.Count(); // Simplified for now

                // TÃ­nh sá» sáº£n pháº©m sáº¯p háº¿t hÃ ng
                var lowStockItems = _context.Products
                    .Where(p => p.StockQuantity <= p.MinStockLevel)
                    .Count();

                // TÃ­nh doanh thu thÃ¡ng nÃ y
                var thisMonthOrders = _context.Orders
                    .Where(o => o.CreatedAt >= thisMonth && o.PaymentStatus == "paid" && o.Status != "cancelled")
                    .ToList();
                var thisMonthRevenue = thisMonthOrders.Sum(o => o.TotalAmount);

                // TÃ­nh doanh thu thÃ¡ng trÆ°á»c
                var lastMonthOrders = _context.Orders
                    .Where(o => o.CreatedAt >= lastMonth && o.CreatedAt < thisMonth && o.PaymentStatus == "paid" && o.Status != "cancelled")
                    .ToList();
                var lastMonthRevenue = lastMonthOrders.Sum(o => o.TotalAmount);

                var monthGrowth = lastMonthRevenue > 0
                    ? ((thisMonthRevenue - lastMonthRevenue) / lastMonthRevenue * 100).ToString("F1") + "%"
                    : "N/A";

                // TÃ­nh tá»ng sá» khÃ¡ch hÃ ng
                var totalCustomers = _context.Customers.Count();

                // Láº¥y danh sÃ¡ch sáº£n pháº©m sáº¯p háº¿t hÃ ng
                var lowStockProductsList = _context.Products
                    .Where(p => p.StockQuantity <= 10)
                    .OrderBy(p => p.StockQuantity)
                    .Take(10)
                    .Select(p => new 
                    {
                        id = p.ProductId,
                        name = p.Name,
                        stockQuantity = p.StockQuantity,
                        price = p.Price,
                        category = p.CategoryId != null ? p.CategoryId.ToString() : "ChÆ°a phÃ¢n loáº¡i"
                    })
                    .ToList();

                var response = new
                {
                    todayRevenue = todayRevenue.ToString("N0") + "₫",
                    todayGrowth = revenueGrowth.StartsWith("-") ? revenueGrowth : "+" + revenueGrowth,
                    monthRevenue = thisMonthRevenue.ToString("N0") + "₫", 
                    monthGrowth = monthGrowth.StartsWith("-") ? monthGrowth : "+" + monthGrowth,
                    ordersCount = totalOrders,
                    todayOrders = todayOrdersCount, // ThÃªm ÄÆ¡n hÃ ng hÃ´m nay
                    ordersGrowth = ordersGrowth.StartsWith("-") ? ordersGrowth : "+" + ordersGrowth,
                    newCustomers = newCustomersToday,
                    totalCustomers = totalCustomers, // ThÃªm tá»ng khÃ¡ch hÃ ng
                    customersGrowth = "+0%", // Simplified
                    lowStockItems = lowStockItems,
                    lowStockProductsList = lowStockProductsList,
                    // ThÃªm thá»ng kÃª chi tiáº¿t vá» tráº¡ng thÃ¡i ÄÆ¡n hÃ ng
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
                return StatusCode(500, new { message = "Lá»i khi táº£i thá»ng kÃª dashboard", error = ex.Message });
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
                    .ToList() // ÄÆ°a vá» memory trÆ°á»c Äá» trÃ¡nh lá»i expression tree
                    .Select(o => new
                    {
                        id = o.OrderId,
                        orderNumber = "#" + o.OrderId,
                        customer = o.CustomerName ?? o.Customer?.HoTen ?? "KhÃ¡ch láº»",
                        total = o.TotalAmount.ToString("N0") + "₫",
                        status = o.PaymentStatus == "paid" && o.Status == "completed" ? "HoÃ n thÃ nh"
                               : o.PaymentStatus == "pending" ? "Chá» thanh toÃ¡n"
                               : o.Status == "pending" ? "Äang xá»­ lÃ½"
                               : "KhÃ¡c",
                        time = GetTimeAgo(o.CreatedAt)
                    })
                    .ToList();

                return Ok(recentOrders);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lá»i khi táº£i ÄÆ¡n hÃ ng gáº§n ÄÃ¢y", error = ex.Message });
            }
        }

        private string GetTimeAgo(DateTime createdAt)
        {
            var timeSpan = DateTime.Now - createdAt;
            
            if (timeSpan.TotalMinutes < 1)
                return "Vá»«a xong";
            if (timeSpan.TotalMinutes < 60)
                return $"{(int)timeSpan.TotalMinutes} phÃºt trÆ°á»c";
            if (timeSpan.TotalHours < 24)
                return $"{(int)timeSpan.TotalHours} giá» trÆ°á»c";
            if (timeSpan.TotalDays < 30)
                return $"{(int)timeSpan.TotalDays} ngÃ y trÆ°á»c";
            
            return createdAt.ToString("dd/MM/yyyy");
        }

        [HttpGet("metrics/stores")]
        public async Task<IActionResult> GetStoreMetrics()
        {
            try
            {
                // Láº¥y thÃ´ng tin user tá»« JWT claim
                var username = User.FindFirstValue(ClaimTypes.Name) ?? "admin";
                
                var staff = await _context.Staffs
                    .Include(s => s.Role)
                    .FirstOrDefaultAsync(s => s.Username == username && s.IsActive);
                    
                if (staff == null)
                {
                    return Unauthorized("KhÃ´ng tÃ¬m tháº¥y thÃ´ng tin nhÃ¢n viÃªn");
                }

                // Náº¿u lÃ  Admin thÃ¬ cÃ³ quyá»n truy cáº­p táº¥t cáº£ stores
                IQueryable<Store> storesQuery = _context.Stores.Where(s => s.IsActive);
                
                if (staff.Role.RoleName != "Admin")
                {
                    // Lá»c chá» stores ÄÆ°á»£c assign cho staff nÃ y
                    var assignedStoreIds = await _context.StaffStores
                        .Where(ss => ss.StaffId == staff.StaffId)
                        .Select(ss => ss.StoreId)
                        .ToListAsync();
                    
                    storesQuery = storesQuery.Where(s => assignedStoreIds.Contains(s.StoreId));
                }

                // Láº¥y thÃ´ng tin cÃ¡c cá»­a hÃ ng ÄÆ°á»£c phÃ©p truy cáº­p vÃ  thá»ng kÃª
                var stores = await storesQuery
                    .Select(s => new
                    {
                        id = s.StoreId,
                        name = s.Name,
                        address = s.Address,
                        isActive = s.IsActive,
                        // Thá»ng kÃª doanh thu theo cá»­a hÃ ng - convert int StoreId to string for comparison
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

                // Náº¿u khÃ´ng cÃ³ cá»­a hÃ ng nÃ o, táº¡o dá»¯ liá»u máº·c Äá»nh
                if (!stores.Any())
                {
                    return Ok(new List<object>
                    {
                        new
                        {
                            id = 1,
                            name = "Cá»­a hÃ ng chÃ­nh",
                            address = "ChÆ°a cáº­p nháº­t Äá»a chá»",
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
                return StatusCode(500, new { message = "Lá»i khi táº£i thÃ´ng tin cá»­a hÃ ng", error = ex.Message });
            }
        }

        [HttpGet("low-stock-products")]
        public async Task<IActionResult> GetLowStockProducts([FromQuery] string? storeId = null)
        {
            try
            {
                // Query products from the same context as metrics
                var lowStockProducts = await _context.Products
                    .Where(p => p.StockQuantity <= 10)
                    .Select(p => new 
                    {
                        id = p.ProductId,
                        name = p.Name,
                        stockQuantity = p.StockQuantity,
                        price = p.Price,
                        category = p.CategoryId != null ? p.CategoryId.ToString() : "ChÆ°a phÃ¢n loáº¡i"
                    })
                    .OrderBy(p => p.stockQuantity)
                    .Take(20)
                    .ToListAsync();

                return Ok(lowStockProducts);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting low stock products: {ex.Message}");
                return BadRequest(new { error = ex.Message });
            }
        }

    }
}