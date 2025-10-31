using Microsoft.AspNetCore.Mvc;
using RetailPointBackend.Models;

namespace RetailPointBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly RetailPointContext _context;

        public AdminController(RetailPointContext context)
        {
            _context = context;
        }

        // API để kiểm tra và sửa trạng thái đơn hàng
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
                return StatusCode(500, new { message = "Lỗi khi kiểm tra đơn hàng", error = ex.Message });
            }
        }

        // API để cập nhật trạng thái đơn hàng
        [HttpPut("fix-order-status/{orderId}")]
        public IActionResult FixOrderStatus(int orderId, [FromForm] string paymentStatus, [FromForm] string status)
        {
            try
            {
                var order = _context.Orders.FirstOrDefault(o => o.OrderId == orderId);
                if (order == null)
                {
                    return NotFound(new { message = "Không tìm thấy đơn hàng" });
                }

                var oldPaymentStatus = order.PaymentStatus;
                var oldStatus = order.Status;

                order.PaymentStatus = paymentStatus;
                order.Status = status;

                _context.SaveChanges();

                return Ok(new
                {
                    message = "Đã cập nhật trạng thái đơn hàng thành công",
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
                return StatusCode(500, new { message = "Lỗi khi cập nhật đơn hàng", error = ex.Message });
            }
        }

        // API để lấy thống kê tổng quan
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
                return StatusCode(500, new { message = "Lỗi khi lấy thống kê", error = ex.Message });
            }
        }

        // API để kiểm tra gaps trong order sequence
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
                return StatusCode(500, new { message = "Lỗi khi kiểm tra sequence", error = ex.Message });
            }
        }
    }
}