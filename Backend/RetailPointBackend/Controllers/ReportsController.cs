using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RetailPointBackend.Models;
using System.Globalization;

namespace RetailPointBackend.Controllers
{
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

                // Lấy đơn hàng trong khoảng thời gian đã thanh toán và chưa bị hủy
                var ordersQuery = _context.Orders
                    .Where(o => o.CreatedAt >= start && o.CreatedAt < end && 
                           (o.PaymentStatus == "paid" || o.PaymentStatus == "completed") &&
                           o.Status != "cancelled"); // Loại trừ đơn hàng đã hủy
                
                // Filter by storeId if provided
                if (storeId.HasValue)
                {
                    ordersQuery = ordersQuery.Where(o => o.StoreId == storeId.Value.ToString());
                }

                var orders = await ordersQuery.Include(o => o.Items).ToListAsync();

                // Tính tổng doanh thu
                var totalRevenue = orders.Sum(o => o.TotalAmount);

                // Tổng số đơn hàng
                var totalOrders = orders.Count;

                // Tổng số khách hàng unique - filter by store if needed
                var customersQuery = _context.Customers.AsQueryable();
                if (storeId.HasValue)
                {
                    customersQuery = customersQuery.Where(c => c.StoreId == storeId.Value);
                }
                var totalCustomers = await customersQuery.CountAsync();

                // Tính tổng số sản phẩm bán ra từ OrderItems (loại trừ đơn hàng đã hủy)
                var orderItemsQuery = _context.OrderItems
                    .Where(oi => oi.Order != null && oi.Order.CreatedAt >= start && oi.Order.CreatedAt < end && 
                                (oi.Order.PaymentStatus == "paid" || oi.Order.PaymentStatus == "completed") &&
                                oi.Order.Status != "cancelled"); // Loại trừ đơn hàng đã hủy
                
                // Filter by storeId if provided
                if (storeId.HasValue)
                {
                    orderItemsQuery = orderItemsQuery.Where(oi => oi.Order != null && oi.Order.StoreId == storeId.Value.ToString());
                }

                var totalProductsSold = await orderItemsQuery.SumAsync(oi => oi.Quantity);

                // Tính tổng số tiền giảm giá
                var totalDiscountAmount = await _context.OrderDiscounts
                    .Where(od => od.AppliedAt >= start && od.AppliedAt < end &&
                                od.Order != null &&
                                (od.Order.PaymentStatus == "paid" || od.Order.PaymentStatus == "completed") &&
                                od.Order.Status != "cancelled")
                    .SumAsync(od => od.DiscountAmount);

                // Tính số lượng giảm giá đã sử dụng
                var totalDiscountUsage = await _context.OrderDiscounts
                    .Where(od => od.AppliedAt >= start && od.AppliedAt < end &&
                                od.Order != null &&
                                (od.Order.PaymentStatus == "paid" || od.Order.PaymentStatus == "completed") &&
                                od.Order.Status != "cancelled")
                    .CountAsync();

                var response = new
                {
                    totalRevenue = totalRevenue.ToString("N0") + "₫",
                    totalOrders = totalOrders,
                    totalCustomers = totalCustomers,
                    totalProductsSold = totalProductsSold,
                    totalDiscountAmount = totalDiscountAmount.ToString("N0") + "₫",
                    totalDiscountUsage = totalDiscountUsage,
                    discountRate = totalRevenue > 0 ? Math.Round((totalDiscountAmount / (totalRevenue + totalDiscountAmount)) * 100, 2) : 0
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi tải báo cáo tổng quan", error = ex.Message });
            }
        }

        [HttpGet("product-performance")]
        public async Task<IActionResult> GetProductPerformance([FromQuery] string startDate, [FromQuery] string endDate, [FromQuery] int? storeId = null)
        {
            try
            {
                var start = DateTime.Parse(startDate);
                var end = DateTime.Parse(endDate).AddDays(1);

                // Lấy các sản phẩm bán chạy từ OrderItems với thông tin cost (loại trừ đơn hàng đã hủy)
                var orderItemsQuery = _context.OrderItems
                    .Where(oi => oi.Order != null && oi.Order.CreatedAt >= start && oi.Order.CreatedAt < end && 
                                (oi.Order.PaymentStatus == "paid" || oi.Order.PaymentStatus == "completed") &&
                                oi.Order.Status != "cancelled"); // Loại trừ đơn hàng đã hủy
                
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
                            name = g.Key.ProductName ?? "Sản phẩm #" + g.Key.ProductId,
                            totalSold = g.Sum(oi => oi.Quantity),
                            revenue = g.Sum(oi => oi.TotalPrice),
                            // Tính lợi nhuận đúng: (Giá bán - Giá nhập) × Số lượng
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
                    revenue = p.revenue.ToString("N0") + "₫",
                    profit = p.profit.ToString("N0") + "₫"
                }).ToList();

                // Tính tổng sản phẩm bán ra (loại trừ đơn hàng đã hủy)
                var totalProductsSold = await _context.OrderItems
                    .Where(oi => oi.Order != null && oi.Order.CreatedAt >= start && oi.Order.CreatedAt < end && 
                                (oi.Order.PaymentStatus == "paid" || oi.Order.PaymentStatus == "completed") &&
                                oi.Order.Status != "cancelled") // Loại trừ đơn hàng đã hủy
                    .SumAsync(oi => oi.Quantity);

                // Sản phẩm phổ biến nhất
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
                return StatusCode(500, new { message = "Lỗi khi tải báo cáo sản phẩm", error = ex.Message });
            }
        }

        [HttpGet("customer-analytics")]
        public async Task<IActionResult> GetCustomerAnalytics([FromQuery] string startDate, [FromQuery] string endDate, [FromQuery] int? storeId = null)
        {
            try
            {
                var start = DateTime.Parse(startDate);
                var end = DateTime.Parse(endDate).AddDays(1);

                // Tổng số khách hàng - filter by store if needed
                var customersQuery = _context.Customers.AsQueryable();
                if (storeId.HasValue)
                {
                    customersQuery = customersQuery.Where(c => c.StoreId == storeId.Value);
                }
                var totalCustomers = await customersQuery.CountAsync();

                // Khách hàng mới trong kỳ (nếu có trường CreatedAt)
                var newCustomers = 0; // Tạm thời = 0 vì Customer model chưa có CreatedAt

                // Tổng số đơn hàng trong kỳ (bao gồm cả khách lẻ, loại trừ đơn hàng đã hủy)
                var ordersQuery = _context.Orders
                    .Where(o => o.CreatedAt >= start && o.CreatedAt < end && 
                               (o.PaymentStatus == "paid" || o.PaymentStatus == "completed") &&
                               o.Status != "cancelled"); // Loại trừ đơn hàng đã hủy
                
                // Filter by storeId if provided
                if (storeId.HasValue)
                {
                    ordersQuery = ordersQuery.Where(o => o.StoreId == storeId.Value.ToString());
                }

                var totalOrdersInPeriod = await ordersQuery.CountAsync();

                // Khách hàng có đơn hàng trong kỳ (chỉ tính những đơn có CustomerId, loại trừ đơn hàng đã hủy)
                var activeCustomersQuery = _context.Orders
                    .Where(o => o.CreatedAt >= start && o.CreatedAt < end && 
                               (o.PaymentStatus == "paid" || o.PaymentStatus == "completed") && 
                               o.CustomerId.HasValue &&
                               o.Status != "cancelled"); // Loại trừ đơn hàng đã hủy
                
                // Filter by storeId if provided
                if (storeId.HasValue)
                {
                    activeCustomersQuery = activeCustomersQuery.Where(o => o.StoreId == storeId.Value.ToString());
                }

                var activeCustomers = await activeCustomersQuery
                    .Select(o => o.CustomerId)
                    .Distinct()
                    .CountAsync();

                // Khách hàng quay lại (có > 1 đơn hàng, loại trừ đơn hàng đã hủy)
                var returningCustomersQuery = _context.Orders
                    .Where(o => (o.PaymentStatus == "paid" || o.PaymentStatus == "completed") && 
                               o.CustomerId.HasValue &&
                               o.Status != "cancelled"); // Loại trừ đơn hàng đã hủy
                
                // Filter by storeId if provided
                if (storeId.HasValue)
                {
                    returningCustomersQuery = returningCustomersQuery.Where(o => o.StoreId == storeId.Value.ToString());
                }

                var returningCustomers = await returningCustomersQuery
                    .GroupBy(o => o.CustomerId)
                    .Where(g => g.Count() > 1)
                    .CountAsync();

                // Top khách hàng VIP (loại trừ đơn hàng đã hủy)
                var topCustomersQuery = _context.Orders
                    .Where(o => o.CreatedAt >= start && o.CreatedAt < end && 
                               (o.PaymentStatus == "paid" || o.PaymentStatus == "completed") && 
                               o.CustomerId.HasValue &&
                               o.Status != "cancelled"); // Loại trừ đơn hàng đã hủy
                
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
                        name = g.Key.CustomerName ?? "Khách hàng #" + g.Key.CustomerId,
                        orders = g.Count(),
                        totalSpent = g.Sum(o => o.TotalAmount)
                    })
                    .OrderByDescending(c => c.totalSpent)
                    .Take(5)
                    .ToListAsync();

                // Trung bình đơn hàng trên khách (tính cả khách lẻ)
                var averageOrdersPerCustomer = totalCustomers > 0 
                    ? Math.Round((double)totalOrdersInPeriod / Math.Max(totalCustomers, 1), 1)
                    : totalOrdersInPeriod;

                var formattedTopCustomers = topCustomers.Select(c => new
                {
                    name = c.name,
                    orders = c.orders,
                    totalSpent = c.totalSpent.ToString("N0") + "₫"
                }).ToList();

                var response = new
                {
                    totalCustomers = totalCustomers,
                    newCustomers = newCustomers,
                    returningCustomers = returningCustomers,
                    activeCustomers = activeCustomers, // Thêm số khách hàng có đơn hàng trong kỳ
                    totalOrdersInPeriod = totalOrdersInPeriod, // Thêm tổng số đơn hàng
                    averageOrdersPerCustomer = averageOrdersPerCustomer.ToString("F1"),
                    topCustomers = formattedTopCustomers
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi tải phân tích khách hàng", error = ex.Message });
            }
        }

        [HttpGet("profit-analysis")]
        public async Task<IActionResult> GetProfitAnalysis([FromQuery] string startDate, [FromQuery] string endDate, [FromQuery] int? storeId = null)
        {
            try
            {
                var start = DateTime.Parse(startDate);
                var end = DateTime.Parse(endDate).AddDays(1);

                // Lấy đơn hàng trong kỳ với OrderItems (loại trừ đơn hàng đã hủy)
                var ordersQuery = _context.Orders
                    .Where(o => o.CreatedAt >= start && o.CreatedAt < end && 
                               (o.PaymentStatus == "paid" || o.PaymentStatus == "completed") &&
                               o.Status != "cancelled"); // Loại trừ đơn hàng đã hủy
                
                // Filter by storeId if provided
                if (storeId.HasValue)
                {
                    ordersQuery = ordersQuery.Where(o => o.StoreId == storeId.Value.ToString());
                }

                var orders = await ordersQuery.Include(o => o.Items).ToListAsync();

                // Lấy thông tin sản phẩm để có CostPrice - filter by store if needed
                var productsQuery = _context.Products.AsQueryable();
                if (storeId.HasValue)
                {
                    productsQuery = productsQuery.Where(p => p.StoreId == storeId.Value);
                }
                var products = await productsQuery.ToListAsync();

                // Lấy cấu hình thuế
                var taxConfig = await _context.TaxConfigs.FirstOrDefaultAsync() ?? new TaxConfig();

                // Tính tổng doanh thu (bao gồm thuế) - đây là TotalAmount trong Orders
                var totalRevenueIncludingTax = orders.Sum(o => o.TotalAmount);

                // Tính thuế VAT và doanh thu chưa thuế
                decimal totalTax = 0;
                decimal totalRevenueExcludingTax = totalRevenueIncludingTax;

                if (taxConfig.EnableVAT)
                {
                    // Tính tổng tax amount từ orders (vì sales page đã tính sẵn)
                    totalTax = orders.Sum(o => o.TaxAmount);
                    
                    // Doanh thu chưa thuế = Tổng tiền - Thuế
                    totalRevenueExcludingTax = totalRevenueIncludingTax - totalTax;
                }

                var totalRevenue = totalRevenueExcludingTax;

                // Tính chi phí hàng bán thực tế dựa trên CostPrice
                decimal costOfGoodsSold = 0;
                decimal totalLoss = 0; // Tổng số tiền lỗ
                
                foreach (var order in orders)
                {
                    foreach (var item in order.Items)
                    {
                        var product = products.FirstOrDefault(p => p.ProductId == item.ProductId);
                        var costPrice = product?.CostPrice ?? (item.Price * 0.6m); // Fallback 60% nếu không có CostPrice
                        var itemCost = item.Quantity * costPrice;
                        var itemRevenue = item.Quantity * item.Price;
                        
                        costOfGoodsSold += itemCost;
                        
                        // Tính lỗ (nếu giá bán thấp hơn giá vốn)
                        if (item.Price < costPrice)
                        {
                            totalLoss += item.Quantity * (costPrice - item.Price);
                        }
                    }
                }

                // Lợi nhuận trước thuế = Doanh thu (chưa thuế) - Chi phí hàng bán
                var profitBeforeTax = totalRevenue - costOfGoodsSold;

                // Lợi nhuận sau thuế = Lợi nhuận trước thuế - Thuế VAT
                var profitAfterTax = profitBeforeTax - totalTax;

                // Tính tổng giảm giá
                var totalDiscountAmount = await _context.OrderDiscounts
                    .Where(od => od.AppliedAt >= start && od.AppliedAt < end &&
                                od.Order != null &&
                                (od.Order.PaymentStatus == "paid" || od.Order.PaymentStatus == "completed") &&
                                od.Order.Status != "cancelled")
                    .SumAsync(od => od.DiscountAmount);

                // Lợi nhuận thực tế (bao gồm cả giảm giá) = Lợi nhuận sau thuế - Giảm giá
                var actualProfit = profitAfterTax - totalDiscountAmount;

                // Tỷ suất lợi nhuận = Lợi nhuận sau thuế / Doanh thu (chưa thuế) * 100
                var profitMargin = totalRevenue > 0 ? (profitAfterTax / totalRevenue * 100) : 0;

                // Tỷ suất lợi nhuận thực tế (bao gồm giảm giá)
                var actualProfitMargin = totalRevenue > 0 ? (actualProfit / totalRevenue * 100) : 0;

                // Top sản phẩm có lợi nhuận cao từ OrderItems với thông tin chi tiết (loại trừ đơn hàng đã hủy)
                var orderItems = await _context.OrderItems
                    .Where(oi => oi.Order != null && oi.Order.CreatedAt >= start && oi.Order.CreatedAt < end && 
                                (oi.Order.PaymentStatus == "paid" || oi.Order.PaymentStatus == "completed") &&
                                oi.Order.Status != "cancelled") // Loại trừ đơn hàng đã hủy
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
                            name = g.Key ?? "Sản phẩm không tên",
                            totalSold = totalSold,
                            revenue = totalRevenue,
                            profit = totalProfit,
                            profitPerUnit = totalSold > 0 ? totalProfit / totalSold : 0,
                            margin = profitMarginPercent.ToString("F1") + "%",
                            costPrice = costPrice,
                            sellPrice = firstItem.Price
                        };
                    })
                    .OrderByDescending(p => p.profit) // Sắp xếp theo tổng lợi nhuận
                    .Take(10)
                    .Select(p => new
                    {
                        name = p.name,
                        totalSold = p.totalSold,
                        revenue = p.revenue.ToString("N0") + "₫",
                        profit = p.profit.ToString("N0") + "₫",
                        profitPerUnit = p.profitPerUnit.ToString("N0") + "₫",
                        margin = p.margin,
                        costPrice = p.costPrice.ToString("N0") + "₫",
                        sellPrice = p.sellPrice.ToString("N0") + "₫"
                    })
                    .ToList();

                // Xu hướng lợi nhuận theo tháng (6 tháng gần nhất)
                var monthlyTrend = new List<object>();
                for (int i = 5; i >= 0; i--)
                {
                    var monthStart = DateTime.Now.AddMonths(-i).Date.AddDays(1 - DateTime.Now.AddMonths(-i).Day);
                    var monthEnd = monthStart.AddMonths(1);
                    
                    // Lấy đơn hàng trong tháng (loại trừ đơn hàng đã hủy)
                    var monthOrders = await _context.Orders
                        .Where(o => o.CreatedAt >= monthStart && o.CreatedAt < monthEnd && 
                                   (o.PaymentStatus == "paid" || o.PaymentStatus == "completed") &&
                                   o.Status != "cancelled") // Loại trừ đơn hàng đã hủy
                        .Include(o => o.Items)
                        .ToListAsync();

                    var monthTotalRevenue = monthOrders.Sum(o => o.TotalAmount);
                    
                    // Tính chi phí hàng bán cho tháng này
                    decimal monthCostOfGoodsSold = 0;
                    decimal monthTax = 0;
                    
                    foreach (var order in monthOrders)
                    {
                        monthTax += order.TaxAmount;
                        foreach (var item in order.Items)
                        {
                            var product = products.FirstOrDefault(p => p.ProductId == item.ProductId);
                            var costPrice = product?.CostPrice ?? (item.Price * 0.6m); // Fallback 60% nếu không có CostPrice
                            monthCostOfGoodsSold += item.Quantity * costPrice;
                        }
                    }
                    
                    // Tính lợi nhuận thực tế cho tháng
                    var monthRevenueExcludingTax = monthTotalRevenue - monthTax;
                    var monthProfit = monthRevenueExcludingTax - monthCostOfGoodsSold;
                    var monthProfitMargin = monthRevenueExcludingTax > 0 ? (monthProfit / monthRevenueExcludingTax * 100) : 0;
                    
                    monthlyTrend.Add(new
                    {
                        month = monthStart.ToString("MM/yyyy"),
                        profit = monthProfit.ToString("N0") + "₫",
                        margin = monthProfitMargin.ToString("F1") + "%"
                    });
                }

                var response = new
                {
                    // Doanh thu và thuế
                    totalRevenueIncludingTax = totalRevenueIncludingTax.ToString("N0") + "₫",
                    totalRevenueExcludingTax = totalRevenueExcludingTax.ToString("N0") + "₫",
                    totalTax = totalTax.ToString("N0") + "₫",
                    vatRate = taxConfig.EnableVAT ? taxConfig.VATRate.ToString("F1") + "%" : "0%",
                    
                    // Lợi nhuận đơn giản
                    costOfGoodsSold = costOfGoodsSold.ToString("N0") + "₫",
                    profitBeforeTax = profitBeforeTax.ToString("N0") + "₫",
                    profitAfterTax = profitAfterTax.ToString("N0") + "₫",
                    profitMargin = profitMargin.ToString("F1") + "%",
                    totalLoss = totalLoss.ToString("N0") + "₫",
                    
                    // Thông tin giảm giá
                    totalDiscountAmount = totalDiscountAmount.ToString("N0") + "₫",
                    actualProfit = actualProfit.ToString("N0") + "₫",
                    actualProfitMargin = actualProfitMargin.ToString("F1") + "%",
                    discountImpact = totalRevenue > 0 ? (totalDiscountAmount / totalRevenue * 100).ToString("F1") + "%" : "0%",
                    
                    // Giữ lại cho tương thích
                    totalProfit = profitAfterTax.ToString("N0") + "₫",
                    profitableProducts = profitableProducts,
                    topProfitableProducts = profitableProducts, // Alias cho frontend
                    monthlyTrend = monthlyTrend
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi tải phân tích lợi nhuận", error = ex.Message });
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
                    CustomerName = order.CustomerName ?? order.Customer?.HoTen ?? "Khách lẻ",
                    CreatedAt = order.CreatedAt,
                    CancelledAt = order.CreatedAt, // Assuming cancellation time is tracked in CreatedAt for now
                    CancellationReason = order.CancellationReason ?? "Không có lý do",
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
                return StatusCode(500, new { message = "Lỗi khi tải báo cáo hủy hàng", error = ex.Message });
            }
        }
    }
}