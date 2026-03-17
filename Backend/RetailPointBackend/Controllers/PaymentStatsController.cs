using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RetailPointBackend.Models;
using System.Globalization;
using System.Text.Json;

namespace RetailPointBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentStatsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PaymentStatsController(AppDbContext context)
        {
            _context = context;
        }

        // Helper class for split payment parsing
        private class SplitPaymentEntry
        {
            public string method { get; set; } = "";
            public string methodName { get; set; } = "";
            public decimal amount { get; set; }
        }

        // Helper class for building payment stats
        private class PaymentStatBuilder
        {
            public string PaymentMethodId { get; set; } = "";
            public string PaymentMethod { get; set; } = "";
            public decimal TotalAmount { get; set; }
            public int OrderCount { get; set; }
            public List<OrderStatEntry> Orders { get; set; } = new();
        }

        private class OrderStatEntry
        {
            public int OrderId { get; set; }
            public string? OrderNumber { get; set; }
            public string CustomerName { get; set; } = "Khách lẻ";
            public decimal TotalAmount { get; set; }
            public DateTime CreatedAt { get; set; }
            public string? Currency { get; set; }
            public string? SplitPaymentDetails { get; set; }
            public decimal? SplitAmount { get; set; } // Amount for this specific payment method in split
            public List<OrderItemEntry> Items { get; set; } = new();
        }

        private class OrderItemEntry
        {
            public string? ProductName { get; set; }
            public int Quantity { get; set; }
            public decimal Price { get; set; }
            public decimal TotalPrice { get; set; }
        }

        // GET: api/PaymentStats
        [HttpGet]
        public async Task<IActionResult> GetPaymentStats([FromQuery] DateTime? fromDate = null, [FromQuery] DateTime? toDate = null)
        {
            try
            {
                var startDate = fromDate ?? DateTime.Now.AddDays(-30).Date;
                var endDate = toDate ?? DateTime.Now.Date.AddDays(1).AddTicks(-1);

                var orders = await _context.Orders
                    .Include(o => o.Items)
                    .Where(o => o.Status == "completed" && 
                               o.PaymentStatus == "paid" &&
                               o.CreatedAt >= startDate && 
                               o.CreatedAt <= endDate)
                    .ToListAsync();

                // Build payment stats with split payment support
                var statsByMethod = new Dictionary<string, PaymentStatBuilder>();

                foreach (var order in orders)
                {
                    var orderItems = order.Items.Select(item => new OrderItemEntry
                    {
                        ProductName = item.ProductName,
                        Quantity = item.Quantity,
                        Price = item.Price,
                        TotalPrice = item.TotalPrice
                    }).ToList();

                    // Check if order has split payment details
                    if (!string.IsNullOrEmpty(order.SplitPaymentDetails))
                    {
                        try
                        {
                            var splits = JsonSerializer.Deserialize<List<SplitPaymentEntry>>(order.SplitPaymentDetails);
                            if (splits != null && splits.Count > 0)
                            {
                                foreach (var split in splits)
                                {
                                    var key = split.method;
                                    if (!statsByMethod.ContainsKey(key))
                                    {
                                        statsByMethod[key] = new PaymentStatBuilder
                                        {
                                            PaymentMethodId = key,
                                            PaymentMethod = FormatPaymentMethodName(key)
                                        };
                                    }

                                    statsByMethod[key].TotalAmount += split.amount;
                                    statsByMethod[key].OrderCount++;
                                    statsByMethod[key].Orders.Add(new OrderStatEntry
                                    {
                                        OrderId = order.OrderId,
                                        OrderNumber = order.OrderNumber,
                                        CustomerName = order.CustomerName ?? "Khách lẻ",
                                        TotalAmount = order.TotalAmount,
                                        CreatedAt = order.CreatedAt,
                                        Currency = order.Currency,
                                        SplitPaymentDetails = order.SplitPaymentDetails,
                                        SplitAmount = split.amount,
                                        Items = orderItems
                                    });
                                }
                                continue; // Skip normal processing for split orders
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error parsing SplitPaymentDetails for order {order.OrderId}: {ex.Message}");
                            // Fall through to normal processing
                        }
                    }

                    // Normal (non-split) order processing
                    var methodKey = GetPaymentMethodKey(order.PaymentMethod, order.Currency);
                    if (!statsByMethod.ContainsKey(methodKey))
                    {
                        statsByMethod[methodKey] = new PaymentStatBuilder
                        {
                            PaymentMethodId = methodKey,
                            PaymentMethod = FormatPaymentMethodName(methodKey)
                        };
                    }

                    statsByMethod[methodKey].TotalAmount += order.TotalAmount;
                    statsByMethod[methodKey].OrderCount++;
                    statsByMethod[methodKey].Orders.Add(new OrderStatEntry
                    {
                        OrderId = order.OrderId,
                        OrderNumber = order.OrderNumber,
                        CustomerName = order.CustomerName ?? "Khách lẻ",
                        TotalAmount = order.TotalAmount,
                        CreatedAt = order.CreatedAt,
                        Currency = order.Currency,
                        Items = orderItems
                    });
                }

                var totalRevenue = statsByMethod.Values.Sum(x => x.TotalAmount);
                
                var paymentStats = statsByMethod.Values
                    .OrderByDescending(x => x.TotalAmount)
                    .Select(stat => new
                    {
                        stat.PaymentMethod,
                        stat.PaymentMethodId,
                        stat.TotalAmount,
                        stat.OrderCount,
                        Percentage = totalRevenue > 0 ? Math.Round((stat.TotalAmount / totalRevenue) * 100, 1) : 0,
                        Orders = stat.Orders.OrderByDescending(x => x.CreatedAt).Select(o => new
                        {
                            o.OrderId,
                            o.OrderNumber,
                            o.CustomerName,
                            o.TotalAmount,
                            o.CreatedAt,
                            o.Currency,
                            o.SplitPaymentDetails,
                            o.SplitAmount,
                            o.Items
                        }).ToList()
                    }).ToList();

                return Ok(new
                {
                    FromDate = startDate.ToString("yyyy-MM-dd"),
                    ToDate = endDate.ToString("yyyy-MM-dd"),
                    TotalRevenue = totalRevenue,
                    TotalOrders = orders.Count,
                    PaymentStats = paymentStats
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lấy thống kê thanh toán", error = ex.Message });
            }
        }

        // GET: api/PaymentStats/summary
        [HttpGet("summary")]
        public async Task<IActionResult> GetPaymentSummary([FromQuery] DateTime? fromDate = null, [FromQuery] DateTime? toDate = null)
        {
            try
            {
                var startDate = fromDate ?? DateTime.Now.AddDays(-7).Date;
                var endDate = toDate ?? DateTime.Now.Date.AddDays(1).AddTicks(-1);

                var orders = await _context.Orders
                    .Where(o => o.Status == "completed" && 
                               o.PaymentStatus == "paid" &&
                               o.CreatedAt >= startDate && 
                               o.CreatedAt <= endDate)
                    .ToListAsync();

                var summary = orders
                    .GroupBy(o => GetPaymentMethodKey(o.PaymentMethod, o.Currency))
                    .Select(g => new
                    {
                        Method = FormatPaymentMethodName(g.Key),
                        Count = g.Count(),
                        Amount = g.Sum(o => o.TotalAmount)
                    })
                    .OrderByDescending(x => x.Amount)
                    .Take(5) // Top 5 phương thức
                    .ToList();

                return Ok(summary);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lấy tóm tắt thanh toán", error = ex.Message });
            }
        }

        private string FormatPaymentMethodName(string method)
        {
            return method switch
            {
                "cash" => "Tiền mặt",
                "card" => "Thẻ ngân hàng",
                "qr" => "QR Code",
                "ewallet" => "Ví điện tử",
                "banktransfer" => "Chuyển khoản",
                "foreignusd" => "Ngoại tệ USD",
                "foreigneur" => "Ngoại tệ EUR",
                "banktransfer_USD" => "Ngoại tệ USD",
                "banktransfer_EUR" => "Ngoại tệ EUR",
                "ngoại tệ" => "Ngoại tệ",
                "ngoại tệ_USD" => "Ngoại tệ USD",
                "ngoại tệ_EUR" => "Ngoại tệ EUR",
                "split" => "Thanh toán chia nhỏ",
                _ => "Tiền mặt"
            };
        }

        private string GetPaymentMethodKey(string? paymentMethod, string? currency)
        {
            var method = paymentMethod ?? "cash";

            // Debug log để kiểm tra
            Console.WriteLine($"GetPaymentMethodKey: method='{method}', currency='{currency}'");

            // Nếu là chuyển khoản / ngoại tệ và có currency, map sang các key chuẩn
            if ((method == "banktransfer" || method == "ngoại tệ") && !string.IsNullOrEmpty(currency))
            {
                var cur = currency.ToUpperInvariant();
                if (cur.StartsWith("USD"))
                {
                    Console.WriteLine("  -> Returning: 'foreignusd'");
                    return "foreignusd";
                }
                if (cur.StartsWith("EUR"))
                {
                    Console.WriteLine("  -> Returning: 'foreigneur'");
                    return "foreigneur";
                }

                // Unknown currency: fall back to banktransfer_{CURRENCY}
                var result = $"banktransfer_{cur}";
                Console.WriteLine($"  -> Returning: '{result}'");
                return result;
            }

            Console.WriteLine($"  -> Returning: '{method}'");
            return method;
        }
    }
}