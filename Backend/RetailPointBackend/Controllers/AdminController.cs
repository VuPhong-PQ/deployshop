using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using RetailPointBackend.Models;

namespace RetailPointBackend.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AdminController(AppDbContext context)
        {
            _context = context;
        }

        // API Ä‘á»ƒ kiá»ƒm tra vÃ  sá»­a tráº¡ng thÃ¡i Ä‘Æ¡n hÃ ng
        [HttpGet("check-orders/{orderIds}")]
        public IActionResult CheckOrdersStatus(string orderIds)
        {
            try
            {
                var ids = orderIds.Split(',').Select(int.Parse).ToList();
                var orders = _context.Orders
                    .Where(o => ids.Contains(o.OrderId))
                    .Select(o => new
                    {
                        o.OrderId,
                        o.PaymentStatus,
                        o.Status,
                        o.PaymentMethod,
                        o.TotalAmount,
                        o.CreatedAt,
                        o.CustomerName
                    })
                    .ToList();

                return Ok(orders);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lá»—i khi kiá»ƒm tra Ä‘Æ¡n hÃ ng", error = ex.Message });
            }
        }

        // API Ä‘á»ƒ cáº­p nháº­t tráº¡ng thÃ¡i Ä‘Æ¡n hÃ ng
        [HttpPut("fix-order-status/{orderId}")]
        public IActionResult FixOrderStatus(int orderId, [FromForm] string paymentStatus, [FromForm] string status)
        {
            try
            {
                var order = _context.Orders.FirstOrDefault(o => o.OrderId == orderId);
                if (order == null)
                {
                    return NotFound(new { message = "KhÃ´ng tÃ¬m tháº¥y Ä‘Æ¡n hÃ ng" });
                }

                var oldPaymentStatus = order.PaymentStatus;
                var oldStatus = order.Status;

                order.PaymentStatus = paymentStatus;
                order.Status = status;

                _context.SaveChanges();

                return Ok(new
                {
                    message = "ÄÃ£ cáº­p nháº­t tráº¡ng thÃ¡i Ä‘Æ¡n hÃ ng thÃ nh cÃ´ng",
                    orderId = orderId,
                    changes = new
                    {
                        paymentStatus = new { from = oldPaymentStatus, to = paymentStatus },
                        status = new { from = oldStatus, to = status }
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lá»—i khi cáº­p nháº­t Ä‘Æ¡n hÃ ng", error = ex.Message });
            }
        }

        // API Ä‘á»ƒ láº¥y thá»‘ng kÃª tá»•ng quan
        [HttpGet("overview")]
        public IActionResult GetOverview()
        {
            try
            {
                var totalOrders = _context.Orders.Count();
                var ordersByPaymentStatus = new
                {
                    paid = _context.Orders.Count(o => o.PaymentStatus == "paid"),
                    pending = _context.Orders.Count(o => o.PaymentStatus == "pending"),
                    failed = _context.Orders.Count(o => o.PaymentStatus == "failed")
                };
                var ordersByStatus = new
                {
                    completed = _context.Orders.Count(o => o.Status == "completed"),
                    pending = _context.Orders.Count(o => o.Status == "pending"),
                    cancelled = _context.Orders.Count(o => o.Status == "cancelled")
                };

                return Ok(new
                {
                    totalOrders,
                    ordersByPaymentStatus,
                    ordersByStatus
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lá»—i khi láº¥y thá»‘ng kÃª", error = ex.Message });
            }
        }

        // API Ä‘á»ƒ kiá»ƒm tra gaps trong order sequence
        [HttpGet("check-sequence-gaps")]
        public IActionResult CheckSequenceGaps()
        {
            try
            {
                var orderIds = _context.Orders.Select(o => o.OrderId).OrderBy(id => id).ToList();
                var gaps = new List<int>();
                
                if (orderIds.Any())
                {
                    for (int i = orderIds.First(); i <= orderIds.Last(); i++)
                    {
                        if (!orderIds.Contains(i))
                        {
                            gaps.Add(i);
                        }
                    }
                }

                return Ok(new
                {
                    totalOrders = orderIds.Count,
                    minOrderId = orderIds.FirstOrDefault(),
                    maxOrderId = orderIds.LastOrDefault(),
                    missingOrderIds = gaps,
                    hasGaps = gaps.Any()
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lá»—i khi kiá»ƒm tra sequence", error = ex.Message });
            }
        }
    }
}
