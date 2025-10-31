using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RetailPointBackend.Models;
using System.Globalization;

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

        // GET: api/PaymentStats
        [HttpGet]
        public async Task<IActionResult> GetPaymentStats([FromQuery] DateTime? fromDate = null, [FromQuery] DateTime? toDate = null)
        {
            try
            {
                // Mặc định lấy 30 ngày gần nhất nếu không có fromDate/toDate
                var startDate = fromDate ?? DateTime.Now.AddDays(-30).Date;
                var endDate = toDate ?? DateTime.Now.Date.AddDays(1).AddTicks(-1);

                // Lấy các đơn hàng đã hoàn thành trong khoảng thời gian với items (loại trừ đơn hàng đã hủy)
                var orders = await _context.Orders
                    .Include(o => o.Items)
                    .Where(o => o.Status == "completed" && 
                               o.PaymentStatus == "paid" &&
                               o.CreatedAt >= startDate && 
                               o.CreatedAt <= endDate)
                    .ToListAsync();

                // Debug: Log số lượng orders và payment methods
                Console.WriteLine($"Found {orders.Count} completed orders");
                foreach (var order in orders)
                {
                    Console.WriteLine($"Order {order.OrderId}: PaymentMethod = '{order.PaymentMethod}', Amount = {order.TotalAmount}");
                }

                // Nhóm theo phương thức thanh toán và tính tổng
                var paymentStats = orders
                    .GroupBy(o => o.PaymentMethod ?? "cash")
                    .Select(g => new
                    {
                        PaymentMethod = FormatPaymentMethodName(g.Key),
                        PaymentMethodId = g.Key,
                        TotalAmount = g.Sum(o => o.TotalAmount),
                        OrderCount = g.Count(),
                        Percentage = 0.0, // Sẽ tính sau
                        Orders = g.Select(order => new
                        {
                            OrderId = order.OrderId,
                            OrderNumber = order.OrderNumber,
                            CustomerName = order.CustomerName ?? "Khách lẻ",
                            TotalAmount = order.TotalAmount,
                            CreatedAt = order.CreatedAt,
                            Items = order.Items.Select(item => new
                            {
                                ProductName = item.ProductName,
                                Quantity = item.Quantity,
                                Price = item.Price,
                                TotalPrice = item.TotalPrice
                            }).ToList()
                        }).OrderByDescending(x => x.CreatedAt).ToList()
                    })
                    .OrderByDescending(x => x.TotalAmount)
                    .ToList();

                // Tính phần trăm
                var totalRevenue = paymentStats.Sum(x => x.TotalAmount);
                var statsWithPercentage = paymentStats.Select(stat => new
                {
                    stat.PaymentMethod,
                    stat.PaymentMethodId,
                    stat.TotalAmount,
                    stat.OrderCount,
                    stat.Orders,
                    Percentage = totalRevenue > 0 ? Math.Round((stat.TotalAmount / totalRevenue) * 100, 1) : 0
                }).ToList();

                return Ok(new
                {
                    FromDate = startDate.ToString("yyyy-MM-dd"),
                    ToDate = endDate.ToString("yyyy-MM-dd"),
                    TotalRevenue = totalRevenue,
                    TotalOrders = orders.Count,
                    PaymentStats = statsWithPercentage
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
                    .GroupBy(o => o.PaymentMethod ?? "cash")
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
                _ => "Tiền mặt"
            };
        }
    }
}