using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using RetailPointBackend.Models;
using System.Globalization;

namespace RetailPointBackend.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ReportsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ReportsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("sales-summary")]
        public async Task<IActionResult> GetSalesSummary([FromQuery] string startDate, [FromQuery] string endDate, [FromQuery] int? storeId = null)
        {
            try
            {
                var start = DateTime.Parse(startDate);
                var end = DateTime.Parse(endDate).AddDays(1); // Include end date

                // Láº¥y Ä‘Æ¡n hÃ ng trong khoáº£ng thá»i gian Ä‘Ã£ thanh toÃ¡n vÃ  chÆ°a bá»‹ há»§y
                var ordersQuery = _context.Orders
                    .Where(o => o.CreatedAt >= start && o.CreatedAt < end && 
                           (o.PaymentStatus == "paid" || o.PaymentStatus == "completed") &&
                           o.Status != "cancelled"); // Loáº¡i trá»« Ä‘Æ¡n hÃ ng Ä‘Ã£ há»§y
                
                // Filter by storeId if provided
                if (storeId.HasValue)
                {
                    ordersQuery = ordersQuery.Where(o => o.StoreId == storeId.Value.ToString());
                }

                var orders = await ordersQuery.Include(o => o.Items).ToListAsync();

                // TÃ­nh tá»•ng doanh thu
                var totalRevenue = orders.Sum(o => o.TotalAmount);

                // Tá»•ng sá»‘ Ä‘Æ¡n hÃ ng
                var totalOrders = orders.Count;

                // Tá»•ng sá»‘ khÃ¡ch hÃ ng unique - filter by store if needed
                var customersQuery = _context.Customers.AsQueryable();
                if (storeId.HasValue)
                {
                    customersQuery = customersQuery.Where(c => c.StoreId == storeId.Value);
                }
                var totalCustomers = await customersQuery.CountAsync();

                // TÃ­nh tá»•ng sá»‘ sáº£n pháº©m bÃ¡n ra tá»« OrderItems (loáº¡i trá»« Ä‘Æ¡n hÃ ng Ä‘Ã£ há»§y)
                var orderItemsQuery = _context.OrderItems
                    .Where(oi => oi.Order != null && oi.Order.CreatedAt >= start && oi.Order.CreatedAt < end && 
                                (oi.Order.PaymentStatus == "paid" || oi.Order.PaymentStatus == "completed") &&
                                oi.Order.Status != "cancelled"); // Loáº¡i trá»« Ä‘Æ¡n hÃ ng Ä‘Ã£ há»§y
                
                // Filter by storeId if provided
                if (storeId.HasValue)
                {
                    orderItemsQuery = orderItemsQuery.Where(oi => oi.Order != null && oi.Order.StoreId == storeId.Value.ToString());
                }

                var totalProductsSold = await orderItemsQuery.SumAsync(oi => oi.Quantity);

                // TÃ­nh tá»•ng sá»‘ tiá»n giáº£m giÃ¡
                var totalDiscountAmount = await _context.OrderDiscounts
                    .Where(od => od.AppliedAt >= start && od.AppliedAt < end &&
                                od.Order != null &&
                                (od.Order.PaymentStatus == "paid" || od.Order.PaymentStatus == "completed") &&
                                od.Order.Status != "cancelled")
                    .SumAsync(od => od.DiscountAmount);

                // TÃ­nh sá»‘ lÆ°á»£ng giáº£m giÃ¡ Ä‘Ã£ sá»­ dá»¥ng
                var totalDiscountUsage = await _context.OrderDiscounts
                    .Where(od => od.AppliedAt >= start && od.AppliedAt < end &&
                                od.Order != null &&
                                (od.Order.PaymentStatus == "paid" || od.Order.PaymentStatus == "completed") &&
                                od.Order.Status != "cancelled")
                    .CountAsync();

                var response = new
                {
                    totalRevenue = totalRevenue.ToString("N0") + "â‚«",
                    totalOrders = totalOrders,
                    totalCustomers = totalCustomers,
                    totalProductsSold = totalProductsSold,
                    totalDiscountAmount = totalDiscountAmount.ToString("N0") + "â‚«",
                    totalDiscountUsage = totalDiscountUsage,
                    discountRate = totalRevenue > 0 ? Math.Round((totalDiscountAmount / (totalRevenue + totalDiscountAmount)) * 100, 2) : 0
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lá»—i khi táº£i bÃ¡o cÃ¡o tá»•ng quan", error = ex.Message });
            }
        }

        [HttpGet("product-performance")]
        public async Task<IActionResult> GetProductPerformance([FromQuery] string startDate, [FromQuery] string endDate, [FromQuery] int? storeId = null)
        {
            try
            {
                var start = DateTime.Parse(startDate);
                var end = DateTime.Parse(endDate).AddDays(1);

                // Láº¥y cÃ¡c sáº£n pháº©m bÃ¡n cháº¡y tá»« OrderItems vá»›i thÃ´ng tin cost (loáº¡i trá»« Ä‘Æ¡n hÃ ng Ä‘Ã£ há»§y)
                var orderItemsQuery = _context.OrderItems
                    .Where(oi => oi.Order != null && oi.Order.CreatedAt >= start && oi.Order.CreatedAt < end && 
                                (oi.Order.PaymentStatus == "paid" || oi.Order.PaymentStatus == "completed") &&
                                oi.Order.Status != "cancelled"); // Loáº¡i trá»« Ä‘Æ¡n hÃ ng Ä‘Ã£ há»§y
                
                // Filter by storeId if provided
                if (storeId.HasValue)
                {
                    orderItemsQuery = orderItemsQuery.Where(oi => oi.Order != null && oi.Order.StoreId == storeId.Value.ToString());
                }

                var orderItems = await orderItemsQuery.Include(oi => oi.Order).ToListAsync();

                // Filter products by store if needed
                var productsQuery = _context.Products.AsQueryable();
                if (storeId.HasValue)
                {
                    productsQuery = productsQuery.Where(p => p.StoreId == storeId.Value);
                }
                var products = await productsQuery.ToListAsync();
                
                var productStats = orderItems
                    .GroupBy(oi => new { oi.ProductId, oi.ProductName })
                    .Select(g => {
                        var product = products.FirstOrDefault(p => p.ProductId == g.Key.ProductId);
                        var costPrice = product?.CostPrice ?? (g.First().Price * 0.6m); // Fallback to 60% if no cost
                        
                        return new
                        {
                            productId = g.Key.ProductId,
                            name = g.Key.ProductName ?? "Sáº£n pháº©m #" + g.Key.ProductId,
                            totalSold = g.Sum(oi => oi.Quantity),
                            revenue = g.Sum(oi => oi.TotalPrice),
                            // TÃ­nh lá»£i nhuáº­n Ä‘Ãºng: (GiÃ¡ bÃ¡n - GiÃ¡ nháº­p) Ã— Sá»‘ lÆ°á»£ng
                            profit = g.Sum(oi => oi.Quantity * (oi.Price - costPrice))
                        };
                    })
                    .OrderByDescending(p => p.totalSold)
                    .Take(10)
                    .ToList();

                var topProducts = productStats.Select(p => new
                {
                    name = p.name,
                    totalSold = p.totalSold,
                    revenue = p.revenue.ToString("N0") + "â‚«",
                    profit = p.profit.ToString("N0") + "â‚«"
                }).ToList();

                // TÃ­nh tá»•ng sáº£n pháº©m bÃ¡n ra (loáº¡i trá»« Ä‘Æ¡n hÃ ng Ä‘Ã£ há»§y)
                var totalProductsSold = await _context.OrderItems
                    .Where(oi => oi.Order != null && oi.Order.CreatedAt >= start && oi.Order.CreatedAt < end && 
                                (oi.Order.PaymentStatus == "paid" || oi.Order.PaymentStatus == "completed") &&
                                oi.Order.Status != "cancelled") // Loáº¡i trá»« Ä‘Æ¡n hÃ ng Ä‘Ã£ há»§y
                    .SumAsync(oi => oi.Quantity);

                // Sáº£n pháº©m phá»• biáº¿n nháº¥t
                var mostPopularProduct = topProducts.FirstOrDefault()?.name ?? "N/A";

                var response = new
                {
                    topProducts = topProducts,
                    totalProductsSold = totalProductsSold,
                    mostPopularProduct = mostPopularProduct
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lá»—i khi táº£i bÃ¡o cÃ¡o sáº£n pháº©m", error = ex.Message });
            }
        }

        [HttpGet("customer-analytics")]
        public async Task<IActionResult> GetCustomerAnalytics([FromQuery] string startDate, [FromQuery] string endDate, [FromQuery] int? storeId = null)
        {
            try
            {
                var start = DateTime.Parse(startDate);
                var end = DateTime.Parse(endDate).AddDays(1);

                // Tá»•ng sá»‘ khÃ¡ch hÃ ng - filter by store if needed
                var customersQuery = _context.Customers.AsQueryable();
                if (storeId.HasValue)
                {
                    customersQuery = customersQuery.Where(c => c.StoreId == storeId.Value);
                }
                var totalCustomers = await customersQuery.CountAsync();

                // KhÃ¡ch hÃ ng má»›i trong ká»³ (náº¿u cÃ³ trÆ°á»ng CreatedAt)
                var newCustomers = 0; // Táº¡m thá»i = 0 vÃ¬ Customer model chÆ°a cÃ³ CreatedAt

                // Tá»•ng sá»‘ Ä‘Æ¡n hÃ ng trong ká»³ (bao gá»“m cáº£ khÃ¡ch láº», loáº¡i trá»« Ä‘Æ¡n hÃ ng Ä‘Ã£ há»§y)
                var ordersQuery = _context.Orders
                    .Where(o => o.CreatedAt >= start && o.CreatedAt < end && 
                               (o.PaymentStatus == "paid" || o.PaymentStatus == "completed") &&
                               o.Status != "cancelled"); // Loáº¡i trá»« Ä‘Æ¡n hÃ ng Ä‘Ã£ há»§y
                
                // Filter by storeId if provided
                if (storeId.HasValue)
                {
                    ordersQuery = ordersQuery.Where(o => o.StoreId == storeId.Value.ToString());
                }

                var totalOrdersInPeriod = await ordersQuery.CountAsync();

                // KhÃ¡ch hÃ ng cÃ³ Ä‘Æ¡n hÃ ng trong ká»³ (chá»‰ tÃ­nh nhá»¯ng Ä‘Æ¡n cÃ³ CustomerId, loáº¡i trá»« Ä‘Æ¡n hÃ ng Ä‘Ã£ há»§y)
                var activeCustomersQuery = _context.Orders
                    .Where(o => o.CreatedAt >= start && o.CreatedAt < end && 
                               (o.PaymentStatus == "paid" || o.PaymentStatus == "completed") && 
                               o.CustomerId.HasValue &&
                               o.Status != "cancelled"); // Loáº¡i trá»« Ä‘Æ¡n hÃ ng Ä‘Ã£ há»§y
                
                // Filter by storeId if provided
                if (storeId.HasValue)
                {
                    activeCustomersQuery = activeCustomersQuery.Where(o => o.StoreId == storeId.Value.ToString());
                }

                var activeCustomers = await activeCustomersQuery
                    .Select(o => o.CustomerId)
                    .Distinct()
                    .CountAsync();

                // KhÃ¡ch hÃ ng quay láº¡i (cÃ³ > 1 Ä‘Æ¡n hÃ ng, loáº¡i trá»« Ä‘Æ¡n hÃ ng Ä‘Ã£ há»§y)
                var returningCustomersQuery = _context.Orders
                    .Where(o => (o.PaymentStatus == "paid" || o.PaymentStatus == "completed") && 
                               o.CustomerId.HasValue &&
                               o.Status != "cancelled"); // Loáº¡i trá»« Ä‘Æ¡n hÃ ng Ä‘Ã£ há»§y
                
                // Filter by storeId if provided
                if (storeId.HasValue)
                {
                    returningCustomersQuery = returningCustomersQuery.Where(o => o.StoreId == storeId.Value.ToString());
                }

                var returningCustomers = await returningCustomersQuery
                    .GroupBy(o => o.CustomerId)
                    .Where(g => g.Count() > 1)
                    .CountAsync();

                // Top khÃ¡ch hÃ ng VIP (loáº¡i trá»« Ä‘Æ¡n hÃ ng Ä‘Ã£ há»§y)
                var topCustomersQuery = _context.Orders
                    .Where(o => o.CreatedAt >= start && o.CreatedAt < end && 
                               (o.PaymentStatus == "paid" || o.PaymentStatus == "completed") && 
                               o.CustomerId.HasValue &&
                               o.Status != "cancelled"); // Loáº¡i trá»« Ä‘Æ¡n hÃ ng Ä‘Ã£ há»§y
                
                // Filter by storeId if provided
                if (storeId.HasValue)
                {
                    topCustomersQuery = topCustomersQuery.Where(o => o.StoreId == storeId.Value.ToString());
                }

                var topCustomers = await topCustomersQuery
                    .Include(o => o.Customer)
                    .GroupBy(o => new { o.CustomerId, CustomerName = o.Customer != null ? o.Customer.HoTen : null })
                    .Select(g => new
                    {
                        customerId = g.Key.CustomerId,
                        name = g.Key.CustomerName ?? "KhÃ¡ch hÃ ng #" + g.Key.CustomerId,
                        orders = g.Count(),
                        totalSpent = g.Sum(o => o.TotalAmount)
                    })
                    .OrderByDescending(c => c.totalSpent)
                    .Take(5)
                    .ToListAsync();

                // Trung bÃ¬nh Ä‘Æ¡n hÃ ng trÃªn khÃ¡ch (tÃ­nh cáº£ khÃ¡ch láº»)
                var averageOrdersPerCustomer = totalCustomers > 0 
                    ? Math.Round((double)totalOrdersInPeriod / Math.Max(totalCustomers, 1), 1)
                    : totalOrdersInPeriod;

                var formattedTopCustomers = topCustomers.Select(c => new
                {
                    name = c.name,
                    orders = c.orders,
                    totalSpent = c.totalSpent.ToString("N0") + "â‚«"
                }).ToList();

                var response = new
                {
                    totalCustomers = totalCustomers,
                    newCustomers = newCustomers,
                    returningCustomers = returningCustomers,
                    activeCustomers = activeCustomers, // ThÃªm sá»‘ khÃ¡ch hÃ ng cÃ³ Ä‘Æ¡n hÃ ng trong ká»³
                    totalOrdersInPeriod = totalOrdersInPeriod, // ThÃªm tá»•ng sá»‘ Ä‘Æ¡n hÃ ng
                    averageOrdersPerCustomer = averageOrdersPerCustomer.ToString("F1"),
                    topCustomers = formattedTopCustomers
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lá»—i khi táº£i phÃ¢n tÃ­ch khÃ¡ch hÃ ng", error = ex.Message });
            }
        }

        [HttpGet("profit-analysis")]
        public async Task<IActionResult> GetProfitAnalysis([FromQuery] string startDate, [FromQuery] string endDate, [FromQuery] int? storeId = null)
        {
            try
            {
                var start = DateTime.Parse(startDate);
                var end = DateTime.Parse(endDate).AddDays(1);

                // Láº¥y Ä‘Æ¡n hÃ ng trong ká»³ vá»›i OrderItems (loáº¡i trá»« Ä‘Æ¡n hÃ ng Ä‘Ã£ há»§y)
                var ordersQuery = _context.Orders
                    .Where(o => o.CreatedAt >= start && o.CreatedAt < end && 
                               (o.PaymentStatus == "paid" || o.PaymentStatus == "completed") &&
                               o.Status != "cancelled"); // Loáº¡i trá»« Ä‘Æ¡n hÃ ng Ä‘Ã£ há»§y
                
                // Filter by storeId if provided
                if (storeId.HasValue)
                {
                    ordersQuery = ordersQuery.Where(o => o.StoreId == storeId.Value.ToString());
                }

                var orders = await ordersQuery.Include(o => o.Items).ToListAsync();

                // Láº¥y thÃ´ng tin sáº£n pháº©m Ä‘á»ƒ cÃ³ CostPrice - filter by store if needed
                var productsQuery = _context.Products.AsQueryable();
                if (storeId.HasValue)
                {
                    productsQuery = productsQuery.Where(p => p.StoreId == storeId.Value);
                }
                var products = await productsQuery.ToListAsync();

                // Láº¥y cáº¥u hÃ¬nh thuáº¿
                var taxConfig = await _context.TaxConfigs.FirstOrDefaultAsync() ?? new TaxConfig();

                // TÃ­nh tá»•ng doanh thu (bao gá»“m thuáº¿) - Ä‘Ã¢y lÃ  TotalAmount trong Orders
                var totalRevenueIncludingTax = orders.Sum(o => o.TotalAmount);

                // TÃ­nh thuáº¿ VAT vÃ  doanh thu chÆ°a thuáº¿
                decimal totalTax = 0;
                decimal totalRevenueExcludingTax = totalRevenueIncludingTax;

                if (taxConfig.EnableVAT)
                {
                    // TÃ­nh tá»•ng tax amount tá»« orders (vÃ¬ sales page Ä‘Ã£ tÃ­nh sáºµn)
                    totalTax = orders.Sum(o => o.TaxAmount);
                    
                    // Doanh thu chÆ°a thuáº¿ = Tá»•ng tiá»n - Thuáº¿
                    totalRevenueExcludingTax = totalRevenueIncludingTax - totalTax;
                }

                var totalRevenue = totalRevenueExcludingTax;

                // TÃ­nh chi phÃ­ hÃ ng bÃ¡n thá»±c táº¿ dá»±a trÃªn CostPrice
                decimal costOfGoodsSold = 0;
                decimal totalLoss = 0; // Tá»•ng sá»‘ tiá»n lá»—
                
                foreach (var order in orders)
                {
                    foreach (var item in order.Items)
                    {
                        var product = products.FirstOrDefault(p => p.ProductId == item.ProductId);
                        var costPrice = product?.CostPrice ?? (item.Price * 0.6m); // Fallback 60% náº¿u khÃ´ng cÃ³ CostPrice
                        var itemCost = item.Quantity * costPrice;
                        var itemRevenue = item.Quantity * item.Price;
                        
                        costOfGoodsSold += itemCost;
                        
                        // TÃ­nh lá»— (náº¿u giÃ¡ bÃ¡n tháº¥p hÆ¡n giÃ¡ vá»‘n)
                        if (item.Price < costPrice)
                        {
                            totalLoss += item.Quantity * (costPrice - item.Price);
                        }
                    }
                }

                // Lá»£i nhuáº­n trÆ°á»›c thuáº¿ = Doanh thu (chÆ°a thuáº¿) - Chi phÃ­ hÃ ng bÃ¡n
                var profitBeforeTax = totalRevenue - costOfGoodsSold;

                // Lá»£i nhuáº­n sau thuáº¿ = Lá»£i nhuáº­n trÆ°á»›c thuáº¿ - Thuáº¿ VAT
                var profitAfterTax = profitBeforeTax - totalTax;

                // TÃ­nh tá»•ng giáº£m giÃ¡
                var totalDiscountAmount = await _context.OrderDiscounts
                    .Where(od => od.AppliedAt >= start && od.AppliedAt < end &&
                                od.Order != null &&
                                (od.Order.PaymentStatus == "paid" || od.Order.PaymentStatus == "completed") &&
                                od.Order.Status != "cancelled")
                    .SumAsync(od => od.DiscountAmount);

                // Lá»£i nhuáº­n thá»±c táº¿ (bao gá»“m cáº£ giáº£m giÃ¡) = Lá»£i nhuáº­n sau thuáº¿ - Giáº£m giÃ¡
                var actualProfit = profitAfterTax - totalDiscountAmount;

                // Tá»· suáº¥t lá»£i nhuáº­n = Lá»£i nhuáº­n sau thuáº¿ / Doanh thu (chÆ°a thuáº¿) * 100
                var profitMargin = totalRevenue > 0 ? (profitAfterTax / totalRevenue * 100) : 0;

                // Tá»· suáº¥t lá»£i nhuáº­n thá»±c táº¿ (bao gá»“m giáº£m giÃ¡)
                var actualProfitMargin = totalRevenue > 0 ? (actualProfit / totalRevenue * 100) : 0;

                // Top sáº£n pháº©m cÃ³ lá»£i nhuáº­n cao tá»« OrderItems vá»›i thÃ´ng tin chi tiáº¿t (loáº¡i trá»« Ä‘Æ¡n hÃ ng Ä‘Ã£ há»§y)
                var orderItems = await _context.OrderItems
                    .Where(oi => oi.Order != null && oi.Order.CreatedAt >= start && oi.Order.CreatedAt < end && 
                                (oi.Order.PaymentStatus == "paid" || oi.Order.PaymentStatus == "completed") &&
                                oi.Order.Status != "cancelled") // Loáº¡i trá»« Ä‘Æ¡n hÃ ng Ä‘Ã£ há»§y
                    .ToListAsync();

                var profitableProducts = orderItems
                    .GroupBy(oi => oi.ProductName)
                    .Select(g => {
                        var firstItem = g.First();
                        var product = products.FirstOrDefault(p => p.ProductId == firstItem.ProductId);
                        var costPrice = product?.CostPrice ?? (firstItem.Price * 0.6m);
                        var totalSold = g.Sum(oi => oi.Quantity);
                        var totalRevenue = g.Sum(oi => oi.TotalPrice);
                        var totalProfit = g.Sum(oi => oi.Quantity * (oi.Price - costPrice));
                        var profitMarginPercent = firstItem.Price > 0 
                            ? ((firstItem.Price - costPrice) / firstItem.Price * 100) 
                            : 0;
                        
                        return new
                        {
                            name = g.Key ?? "Sáº£n pháº©m khÃ´ng tÃªn",
                            totalSold = totalSold,
                            revenue = totalRevenue,
                            profit = totalProfit,
                            profitPerUnit = totalSold > 0 ? totalProfit / totalSold : 0,
                            margin = profitMarginPercent.ToString("F1") + "%",
                            costPrice = costPrice,
                            sellPrice = firstItem.Price
                        };
                    })
                    .OrderByDescending(p => p.profit) // Sáº¯p xáº¿p theo tá»•ng lá»£i nhuáº­n
                    .Take(10)
                    .Select(p => new
                    {
                        name = p.name,
                        totalSold = p.totalSold,
                        revenue = p.revenue.ToString("N0") + "â‚«",
                        profit = p.profit.ToString("N0") + "â‚«",
                        profitPerUnit = p.profitPerUnit.ToString("N0") + "â‚«",
                        margin = p.margin,
                        costPrice = p.costPrice.ToString("N0") + "â‚«",
                        sellPrice = p.sellPrice.ToString("N0") + "â‚«"
                    })
                    .ToList();

                // Xu hÆ°á»›ng lá»£i nhuáº­n theo thÃ¡ng (6 thÃ¡ng gáº§n nháº¥t)
                var monthlyTrend = new List<object>();
                for (int i = 5; i >= 0; i--)
                {
                    var monthStart = DateTime.Now.AddMonths(-i).Date.AddDays(1 - DateTime.Now.AddMonths(-i).Day);
                    var monthEnd = monthStart.AddMonths(1);
                    
                    // Láº¥y Ä‘Æ¡n hÃ ng trong thÃ¡ng (loáº¡i trá»« Ä‘Æ¡n hÃ ng Ä‘Ã£ há»§y)
                    var monthOrders = await _context.Orders
                        .Where(o => o.CreatedAt >= monthStart && o.CreatedAt < monthEnd && 
                                   (o.PaymentStatus == "paid" || o.PaymentStatus == "completed") &&
                                   o.Status != "cancelled") // Loáº¡i trá»« Ä‘Æ¡n hÃ ng Ä‘Ã£ há»§y
                        .Include(o => o.Items)
                        .ToListAsync();

                    var monthTotalRevenue = monthOrders.Sum(o => o.TotalAmount);
                    
                    // TÃ­nh chi phÃ­ hÃ ng bÃ¡n cho thÃ¡ng nÃ y
                    decimal monthCostOfGoodsSold = 0;
                    decimal monthTax = 0;
                    
                    foreach (var order in monthOrders)
                    {
                        monthTax += order.TaxAmount;
                        foreach (var item in order.Items)
                        {
                            var product = products.FirstOrDefault(p => p.ProductId == item.ProductId);
                            var costPrice = product?.CostPrice ?? (item.Price * 0.6m); // Fallback 60% náº¿u khÃ´ng cÃ³ CostPrice
                            monthCostOfGoodsSold += item.Quantity * costPrice;
                        }
                    }
                    
                    // TÃ­nh lá»£i nhuáº­n thá»±c táº¿ cho thÃ¡ng
                    var monthRevenueExcludingTax = monthTotalRevenue - monthTax;
                    var monthProfit = monthRevenueExcludingTax - monthCostOfGoodsSold;
                    var monthProfitMargin = monthRevenueExcludingTax > 0 ? (monthProfit / monthRevenueExcludingTax * 100) : 0;
                    
                    monthlyTrend.Add(new
                    {
                        month = monthStart.ToString("MM/yyyy"),
                        profit = monthProfit.ToString("N0") + "â‚«",
                        margin = monthProfitMargin.ToString("F1") + "%"
                    });
                }

                var response = new
                {
                    // Doanh thu vÃ  thuáº¿
                    totalRevenueIncludingTax = totalRevenueIncludingTax.ToString("N0") + "â‚«",
                    totalRevenueExcludingTax = totalRevenueExcludingTax.ToString("N0") + "â‚«",
                    totalTax = totalTax.ToString("N0") + "â‚«",
                    vatRate = taxConfig.EnableVAT ? taxConfig.VATRate.ToString("F1") + "%" : "0%",
                    
                    // Lá»£i nhuáº­n Ä‘Æ¡n giáº£n
                    costOfGoodsSold = costOfGoodsSold.ToString("N0") + "â‚«",
                    profitBeforeTax = profitBeforeTax.ToString("N0") + "â‚«",
                    profitAfterTax = profitAfterTax.ToString("N0") + "â‚«",
                    profitMargin = profitMargin.ToString("F1") + "%",
                    totalLoss = totalLoss.ToString("N0") + "â‚«",
                    
                    // ThÃ´ng tin giáº£m giÃ¡
                    totalDiscountAmount = totalDiscountAmount.ToString("N0") + "â‚«",
                    actualProfit = actualProfit.ToString("N0") + "â‚«",
                    actualProfitMargin = actualProfitMargin.ToString("F1") + "%",
                    discountImpact = totalRevenue > 0 ? (totalDiscountAmount / totalRevenue * 100).ToString("F1") + "%" : "0%",
                    
                    // Giá»¯ láº¡i cho tÆ°Æ¡ng thÃ­ch
                    totalProfit = profitAfterTax.ToString("N0") + "â‚«",
                    profitableProducts = profitableProducts,
                    topProfitableProducts = profitableProducts, // Alias cho frontend
                    monthlyTrend = monthlyTrend
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lá»—i khi táº£i phÃ¢n tÃ­ch lá»£i nhuáº­n", error = ex.Message });
            }
        }

        [HttpGet("cancelled-orders")]
        public async Task<IActionResult> GetCancelledOrdersReport([FromQuery] string? startDate = null, [FromQuery] string? endDate = null, [FromQuery] string? orderId = null)
        {
            try
            {
                var query = _context.Orders
                    .Include(o => o.Items)
                    .Include(o => o.Customer)
                    .Where(o => o.Status == "cancelled");

                // Filter by date range if provided
                if (!string.IsNullOrEmpty(startDate) && DateTime.TryParse(startDate, out var start))
                {
                    query = query.Where(o => o.CreatedAt >= start);
                }

                if (!string.IsNullOrEmpty(endDate) && DateTime.TryParse(endDate, out var end))
                {
                    var endWithTime = end.AddDays(1); // Include end date
                    query = query.Where(o => o.CreatedAt < endWithTime);
                }

                // Filter by order ID if provided
                if (!string.IsNullOrEmpty(orderId) && int.TryParse(orderId, out var orderIdInt))
                {
                    query = query.Where(o => o.OrderId == orderIdInt);
                }

                var cancelledOrders = await query
                    .OrderByDescending(o => o.CreatedAt)
                    .ToListAsync();

                var report = cancelledOrders.Select(order => new
                {
                    OrderId = order.OrderId,
                    OrderNumber = order.OrderNumber,
                    CustomerName = order.CustomerName ?? order.Customer?.HoTen ?? "KhÃ¡ch láº»",
                    CreatedAt = order.CreatedAt,
                    CancelledAt = order.CreatedAt, // Assuming cancellation time is tracked in CreatedAt for now
                    CancellationReason = order.CancellationReason ?? "KhÃ´ng cÃ³ lÃ½ do",
                    TotalAmount = order.TotalAmount,
                    SubTotal = order.SubTotal,
                    TaxAmount = order.TaxAmount,
                    DiscountAmount = order.DiscountAmount,
                    PaymentMethod = order.PaymentMethod ?? "cash",
                    Items = order.Items.Select(item => new
                    {
                        ProductId = item.ProductId,
                        ProductName = item.ProductName,
                        Quantity = item.Quantity,
                        UnitPrice = item.Price,
                        TotalPrice = item.TotalPrice,
                        // Calculate loss per item (unit price * quantity)
                        LossAmount = item.TotalPrice
                    }).ToList(),
                    // Summary calculations
                    TotalQuantityCancelled = order.Items.Sum(i => i.Quantity),
                    TotalLossAmount = order.TotalAmount
                }).ToList();

                // Calculate summary statistics
                var totalOrders = report.Count;
                var totalQuantityCancelled = report.Sum(r => r.TotalQuantityCancelled);
                var totalLossAmount = report.Sum(r => r.TotalLossAmount);
                var averageLossPerOrder = totalOrders > 0 ? totalLossAmount / totalOrders : 0;

                var response = new
                {
                    Summary = new
                    {
                        TotalCancelledOrders = totalOrders,
                        TotalQuantityCancelled = totalQuantityCancelled,
                        TotalLossAmount = totalLossAmount,
                        AverageLossPerOrder = averageLossPerOrder,
                        ReportPeriod = new
                        {
                            StartDate = startDate,
                            EndDate = endDate
                        }
                    },
                    Orders = report
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lá»—i khi táº£i bÃ¡o cÃ¡o há»§y hÃ ng", error = ex.Message });
            }
        }
    }
}
