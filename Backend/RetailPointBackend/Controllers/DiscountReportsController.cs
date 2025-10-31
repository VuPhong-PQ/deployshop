using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RetailPointBackend.Models;

namespace RetailPointBackend.Controllers
{
    [ApiController]
    [Route("api/discount-reports")]
    public class DiscountReportsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public DiscountReportsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/discountreports/summary?startDate=2023-01-01&endDate=2023-12-31
        [HttpGet("summary")]
        public async Task<ActionResult<DiscountSummaryReport>> GetDiscountSummary(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                // Sử dụng Date comparison để tránh timezone issues
                var start = startDate?.Date ?? DateTime.Today.AddDays(-30);
                var end = (endDate?.Date.AddDays(1) ?? DateTime.Today.AddDays(1));

                // Debug log
                Console.WriteLine($"Summary - Date params: startDate={startDate}, endDate={endDate}");
                Console.WriteLine($"Summary filter range: {start:yyyy-MM-dd} to {end:yyyy-MM-dd}");

                // Nếu không có date filter, lấy tất cả để test
                var query = _context.OrderDiscounts.AsQueryable();
                
                // Chỉ apply date filter khi có startDate AND endDate
                if (startDate.HasValue && endDate.HasValue)
                {
                    query = query.Where(od => od.AppliedAt.Date >= start.Date && od.AppliedAt.Date <= end.Date);
                    Console.WriteLine($"Applied date filter: {start:yyyy-MM-dd} to {end:yyyy-MM-dd}");
                }
                else 
                {
                    Console.WriteLine("No date filter applied - getting all records");
                }

                var discounts = await query.ToListAsync();
                Console.WriteLine($"Summary found {discounts.Count} discount records");

                if (!discounts.Any())
                {
                    return Ok(new DiscountSummaryReport
                    {
                        TotalDiscountAmount = 0,
                        TotalDiscountApplications = 0,
                        UniqueOrdersWithDiscount = 0,
                        AverageDiscountPerOrder = 0,
                        DiscountsByType = new List<DiscountTypeReport>(),
                        TopDiscounts = new List<TopDiscountReport>(),
                        DailyDiscounts = new List<DailyDiscountReport>()
                    });
                }

                var summary = new DiscountSummaryReport
                {
                    TotalDiscountAmount = discounts.Sum(d => d.DiscountAmount),
                    TotalDiscountApplications = discounts.Count,
                    UniqueOrdersWithDiscount = discounts.Select(d => d.OrderId).Distinct().Count(),
                    AverageDiscountPerOrder = discounts.Any() ? 
                        (discounts.GroupBy(d => d.OrderId).Any() ? 
                            discounts.GroupBy(d => d.OrderId).Average(g => g.Sum(d => d.DiscountAmount)) : 0) : 0,
                    
                    DiscountsByType = discounts.GroupBy(d => d.DiscountType)
                        .Select(g => new DiscountTypeReport
                        {
                            Type = g.Key,
                            TypeName = GetDiscountTypeName(g.Key),
                            Count = g.Count(),
                            TotalAmount = g.Sum(d => d.DiscountAmount),
                            AverageAmount = g.Any() ? g.Average(d => d.DiscountAmount) : 0
                        }).ToList(),

                    TopDiscounts = discounts.GroupBy(d => new { d.DiscountId, d.DiscountName })
                        .Select(g => new TopDiscountReport
                        {
                            DiscountId = g.Key.DiscountId,
                            DiscountName = g.Key.DiscountName ?? "Giảm giá thủ công",
                            UsageCount = g.Count(),
                            TotalAmount = g.Sum(d => d.DiscountAmount),
                            AverageAmount = g.Any() ? g.Average(d => d.DiscountAmount) : 0
                        })
                        .OrderByDescending(t => t.TotalAmount)
                        .Take(10)
                        .ToList(),

                    DailyDiscounts = discounts.GroupBy(d => d.AppliedAt.Date)
                        .Select(g => new DailyDiscountReport
                        {
                            Date = g.Key,
                            Count = g.Count(),
                            TotalAmount = g.Sum(d => d.DiscountAmount),
                            UniqueOrders = g.Select(d => d.OrderId).Distinct().Count()
                        })
                        .OrderBy(d => d.Date)
                        .ToList()
                };

                Console.WriteLine($"Summary result: {summary.TotalDiscountAmount:C} total, {summary.UniqueOrdersWithDiscount} orders");
                return Ok(summary);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetDiscountSummary: {ex.Message}");
                return StatusCode(500, new { message = "Internal server error", detail = ex.Message });
            }
        }

        // GET: api/discount-reports/orders-v2 - Lấy từ Orders.DiscountAmount thay vì OrderDiscounts
        [HttpGet("orders-v2")]
        public async Task<ActionResult<DiscountOrdersResponse>> GetDiscountedOrdersV2(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                var start = startDate?.Date ?? DateTime.Today.AddDays(-30);
                var end = (endDate?.Date.AddDays(1) ?? DateTime.Today.AddDays(1));

                Console.WriteLine($"V2 API - Filter range: {start:yyyy-MM-dd} to {end:yyyy-MM-dd}");

                // Lấy Orders có DiscountAmount > 0 (đã được giảm giá)
                var discountedOrders = await _context.Orders
                    .Where(o => o.CreatedAt.Date >= start.Date && o.CreatedAt.Date < end.Date 
                               && o.DiscountAmount > 0)
                    .Include(o => o.Items) // Include OrderItems để xem item-level discounts
                    .OrderByDescending(o => o.CreatedAt)
                    .ToListAsync();

                Console.WriteLine($"Found {discountedOrders.Count} orders with discounts from Orders table");

                if (!discountedOrders.Any())
                {
                    return Ok(new DiscountOrdersResponse
                    {
                        Orders = new List<DiscountedOrderSummary>(),
                        TotalCount = 0,
                        Page = page,
                        PageSize = pageSize,
                        TotalPages = 0
                    });
                }

                var orderSummaries = discountedOrders.Select(order => new DiscountedOrderSummary
                {
                    OrderId = order.OrderId,
                    OrderNumber = order.OrderNumber ?? $"ORD-{order.OrderId}",
                    CustomerName = order.CustomerName ?? "Khách vãng lai",
                    OrderTotal = order.TotalAmount,
                    TotalDiscountAmount = order.DiscountAmount, // Từ Orders.DiscountAmount
                    DiscountCount = order.Items.Count(item => item.DiscountAmount > 0) + (order.DiscountAmount > 0 ? 1 : 0),
                    OrderDate = order.CreatedAt,
                    PaymentStatus = order.PaymentStatus ?? "",
                    PaymentMethod = order.PaymentMethod ?? "",
                    DiscountDetails = new List<OrderDiscountDetail>
                    {
                        // Order-level discount
                        new OrderDiscountDetail
                        {
                            DiscountName = "Giảm giá thủ công",
                            DiscountType = DiscountType.FixedAmountTotal,
                            DiscountTypeName = "Giảm tiền tổng bill",
                            DiscountValue = order.DiscountAmount,
                            DiscountAmount = order.DiscountAmount,
                            AppliedAt = order.CreatedAt
                        }
                    }
                    // TODO: Thêm item-level discounts nếu cần
                    .Concat(order.Items.Where(item => item.DiscountAmount > 0).Select(item => new OrderDiscountDetail
                    {
                        DiscountName = $"Giảm giá {item.ProductName}",
                        DiscountType = DiscountType.FixedAmountItem,
                        DiscountTypeName = "Giảm tiền mặt hàng",
                        DiscountValue = item.DiscountAmount,
                        DiscountAmount = item.DiscountAmount,
                        AppliedAt = order.CreatedAt
                    }))
                    .Where(d => d.DiscountAmount > 0)
                    .ToList()
                }).ToList();

                var totalCount = orderSummaries.Count;
                var pagedOrders = orderSummaries
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                var response = new DiscountOrdersResponse
                {
                    Orders = pagedOrders,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetDiscountedOrdersV2: {ex.Message}");
                return StatusCode(500, new { message = "Internal server error", detail = ex.Message });
            }
        }
        [HttpGet("test-date")]
        public async Task<ActionResult> TestDateFilter()
        {
            try
            {
                var today = DateTime.Today; // 2025-10-09 00:00:00
                var tomorrow = today.AddDays(1); // 2025-10-10 00:00:00
                
                Console.WriteLine($"Testing date filter: {today:yyyy-MM-dd} to {tomorrow:yyyy-MM-dd}");
                
                var records = await _context.OrderDiscounts
                    .Where(od => od.AppliedAt.Date >= today && od.AppliedAt.Date < tomorrow)
                    .Select(od => new { od.OrderId, od.AppliedAt })
                    .Take(5)
                    .ToListAsync();
                
                var totalToday = await _context.OrderDiscounts
                    .CountAsync(od => od.AppliedAt.Date == today);
                    
                return Ok(new { 
                    Today = today,
                    Tomorrow = tomorrow,
                    RecordsFound = records.Count,
                    TotalToday = totalToday,
                    SampleRecords = records 
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }
        [HttpGet("debug")]
        public async Task<ActionResult> DebugDiscountData()
        {
            try
            {
                var orderDiscounts = await _context.OrderDiscounts
                    .OrderByDescending(od => od.AppliedAt)
                    .Take(5)
                    .ToListAsync();

                var orderIds = orderDiscounts.Select(od => od.OrderId).ToList();
                var orders = await _context.Orders
                    .Where(o => orderIds.Contains(o.OrderId))
                    .ToListAsync();

                var result = orderDiscounts.Select(od => new {
                    od.OrderId,
                    od.DiscountName,
                    od.DiscountType,
                    od.DiscountAmount,
                    od.AppliedAt,
                    OrderExists = orders.Any(o => o.OrderId == od.OrderId),
                    OrderInfo = orders.FirstOrDefault(o => o.OrderId == od.OrderId)?.OrderNumber
                }).ToList();

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message, stackTrace = ex.StackTrace });
            }
        }

        // GET: api/discount-reports/test
        [HttpGet("test")]
        public async Task<ActionResult> TestOrderDiscounts()
        {
            try
            {
                // Test OrderDiscounts table
                var orderDiscountsCount = await _context.OrderDiscounts.CountAsync();
                var recentOrderDiscounts = await _context.OrderDiscounts
                    .OrderByDescending(od => od.AppliedAt)
                    .Take(3)
                    .Select(od => new {
                        od.OrderId,
                        od.DiscountName,
                        od.DiscountAmount,
                        od.AppliedAt
                    })
                    .ToListAsync();

                // Test Orders table for actual discount data
                var ordersWithDiscounts = await _context.Orders
                    .Where(o => o.DiscountAmount > 0)
                    .OrderByDescending(o => o.CreatedAt)
                    .Take(5)
                    .Select(o => new {
                        o.OrderId,
                        o.OrderNumber,
                        o.TotalAmount,
                        o.DiscountAmount,
                        o.CreatedAt
                    })
                    .ToListAsync();

                // Test OrderItems for item-level discounts
                var itemsWithDiscounts = await _context.OrderItems
                    .Where(oi => oi.DiscountAmount > 0)
                    .Take(5)
                    .Select(oi => new {
                        oi.OrderId,
                        oi.ProductName,
                        oi.DiscountAmount,
                        oi.Price,
                        oi.FinalPrice
                    })
                    .ToListAsync();

                return Ok(new { 
                    OrderDiscountsTable = new {
                        TotalCount = orderDiscountsCount,
                        RecentDiscounts = recentOrderDiscounts
                    },
                    OrdersWithDiscounts = new {
                        Count = ordersWithDiscounts.Count,
                        Orders = ordersWithDiscounts
                    },
                    OrderItemsWithDiscounts = new {
                        Count = itemsWithDiscounts.Count,
                        Items = itemsWithDiscounts
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message, stackTrace = ex.StackTrace });
            }
        }

        // GET: api/discount-reports/orders?startDate=2023-01-01&endDate=2023-12-31&page=1&pageSize=10
        [HttpGet("orders")]
        public async Task<ActionResult<DiscountOrdersResponse>> GetDiscountedOrders(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                // Sử dụng local timezone và đặt time cho đầu ngày và cuối ngày
                var start = startDate?.Date ?? DateTime.Today.AddDays(-30); // 00:00:00
                var end = (endDate?.Date.AddDays(1) ?? DateTime.Today.AddDays(1)); // 23:59:59 của ngày endDate

                // Debug log
                Console.WriteLine($"Date params - startDate: {startDate}, endDate: {endDate}");
                Console.WriteLine($"Filter range: {start:yyyy-MM-dd HH:mm:ss} to {end:yyyy-MM-dd HH:mm:ss}");

                // Lấy dữ liệu OrderDiscounts - chỉ filter khi có cả startDate và endDate
                var query = _context.OrderDiscounts.AsQueryable();
                
                if (startDate.HasValue && endDate.HasValue)
                {
                    query = query.Where(od => od.AppliedAt.Date >= start.Date && od.AppliedAt.Date <= end.Date);
                    Console.WriteLine($"Orders API - Applied date filter: {start:yyyy-MM-dd} to {end:yyyy-MM-dd}");
                }
                else 
                {
                    Console.WriteLine("Orders API - No date filter, getting all records");
                }
                
                var orderDiscounts = await query
                    .OrderByDescending(od => od.AppliedAt)
                    .ToListAsync();

                Console.WriteLine($"Found {orderDiscounts.Count} order discounts");

                if (!orderDiscounts.Any())
                {
                    // Kiểm tra tổng số records để debug
                    var totalRecords = await _context.OrderDiscounts.CountAsync();
                    Console.WriteLine($"Total OrderDiscounts in DB: {totalRecords}");
                    
                    return Ok(new DiscountOrdersResponse
                    {
                        Orders = new List<DiscountedOrderSummary>(),
                        TotalCount = 0,
                        Page = page,
                        PageSize = pageSize,
                        TotalPages = 0
                    });
                }

                // Lấy OrderIds unique
                var orderIds = orderDiscounts.Select(od => od.OrderId).Distinct().ToList();
                
                // Lấy thông tin Orders
                var orders = await _context.Orders
                    .Where(o => orderIds.Contains(o.OrderId))
                    .ToListAsync();

                // Tạo dictionary để lookup nhanh
                var orderDict = orders.ToDictionary(o => o.OrderId);

                // Group OrderDiscounts theo OrderId
                var groupedDiscounts = orderDiscounts.GroupBy(od => od.OrderId).ToList();

                var orderSummaries = new List<DiscountedOrderSummary>();

                foreach (var group in groupedDiscounts)
                {
                    var orderId = group.Key;
                    var order = orderDict.GetValueOrDefault(orderId);
                    
                    if (order != null)
                    {
                        var summary = new DiscountedOrderSummary
                        {
                            OrderId = orderId,
                            OrderNumber = order.OrderNumber ?? $"ORD-{orderId}",
                            CustomerName = order.CustomerName ?? "Khách vãng lai",
                            OrderTotal = order.TotalAmount,
                            TotalDiscountAmount = group.Sum(od => od.DiscountAmount),
                            DiscountCount = group.Count(),
                            OrderDate = order.CreatedAt,
                            PaymentStatus = order.PaymentStatus ?? "",
                            PaymentMethod = order.PaymentMethod ?? "",
                            DiscountDetails = group.Select(od => new OrderDiscountDetail
                            {
                                DiscountName = od.DiscountName ?? "Giảm giá thủ công",
                                DiscountType = od.DiscountType,
                                DiscountTypeName = GetDiscountTypeName(od.DiscountType),
                                DiscountValue = od.DiscountValue,
                                DiscountAmount = od.DiscountAmount,
                                AppliedAt = od.AppliedAt
                            }).ToList()
                        };

                        Console.WriteLine($"Order {orderId}: {summary.DiscountDetails.Count} discount details");
                        foreach (var detail in summary.DiscountDetails)
                        {
                            Console.WriteLine($"  - {detail.DiscountName}: {detail.DiscountAmount}");
                        }
                        
                        orderSummaries.Add(summary);
                    }
                }

                // Sort by date desc
                orderSummaries = orderSummaries.OrderByDescending(o => o.OrderDate).ToList();

                var totalCount = orderSummaries.Count;
                var pagedOrders = orderSummaries
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                var response = new DiscountOrdersResponse
                {
                    Orders = pagedOrders,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal server error", detail = ex.Message });
            }
        }

        // GET: api/discount-reports/orders/{orderId}
        [HttpGet("orders/{orderId}")]
        public async Task<ActionResult> GetOrderDiscountDetails(int orderId)
        {
            try
            {
                var discounts = await _context.OrderDiscounts
                    .Where(od => od.OrderId == orderId)
                    .ToListAsync();

                return Ok(new { OrderId = orderId, DiscountCount = discounts.Count, Discounts = discounts });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal server error", detail = ex.Message, stack = ex.StackTrace });
            }
        }

        private string GetDiscountTypeName(DiscountType type)
        {
            return type switch
            {
                DiscountType.PercentageTotal => "Giảm % tổng bill",
                DiscountType.FixedAmountItem => "Giảm tiền mặt hàng",
                DiscountType.FixedAmountTotal => "Giảm tiền tổng bill",
                _ => "Không xác định"
            };
        }
    }

    // DTOs for Discount Reports
    public class DiscountSummaryReport
    {
        public decimal TotalDiscountAmount { get; set; }
        public int TotalDiscountApplications { get; set; }
        public int UniqueOrdersWithDiscount { get; set; }
        public decimal AverageDiscountPerOrder { get; set; }
        public List<DiscountTypeReport> DiscountsByType { get; set; } = new();
        public List<TopDiscountReport> TopDiscounts { get; set; } = new();
        public List<DailyDiscountReport> DailyDiscounts { get; set; } = new();
    }

    public class DiscountTypeReport
    {
        public DiscountType Type { get; set; }
        public string TypeName { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal AverageAmount { get; set; }
    }

    public class TopDiscountReport
    {
        public int DiscountId { get; set; }
        public string DiscountName { get; set; } = string.Empty;
        public int UsageCount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal AverageAmount { get; set; }
    }

    public class DailyDiscountReport
    {
        public DateTime Date { get; set; }
        public int Count { get; set; }
        public decimal TotalAmount { get; set; }
        public int UniqueOrders { get; set; }
    }

    public class DiscountOrdersResponse
    {
        public List<DiscountedOrderSummary> Orders { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }

    public class DiscountedOrderSummary
    {
        public int OrderId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public decimal OrderTotal { get; set; }
        public decimal TotalDiscountAmount { get; set; }
        public int DiscountCount { get; set; }
        public DateTime OrderDate { get; set; }
        public string PaymentStatus { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
        public List<OrderDiscountDetail> DiscountDetails { get; set; } = new();
    }

    public class OrderDiscountDetail
    {
        public string DiscountName { get; set; } = string.Empty;
        public DiscountType DiscountType { get; set; }
        public string DiscountTypeName { get; set; } = string.Empty;
        public decimal DiscountValue { get; set; }
        public decimal DiscountAmount { get; set; }
        public DateTime AppliedAt { get; set; }
    }

    public class OrderDiscountDetails
    {
        public int OrderId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public decimal OrderTotal { get; set; }
        public decimal SubTotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal TotalDiscountAmount { get; set; }
        public DateTime OrderDate { get; set; }
        public string PaymentStatus { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
        public List<OrderItemSummary> Items { get; set; } = new();
        public List<OrderDiscountDetail> DiscountDetails { get; set; } = new();
    }

    public class OrderItemSummary
    {
        public int OrderItemId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal TotalPrice { get; set; }
    }
}