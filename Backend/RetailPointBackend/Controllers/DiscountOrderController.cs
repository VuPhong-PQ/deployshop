using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using RetailPointBackend.Models;
using RetailPointBackend.Services;

namespace RetailPointBackend.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class DiscountOrderController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IDiscountService _discountService;

        public DiscountOrderController(AppDbContext context, IDiscountService discountService)
        {
            _context = context;
            _discountService = discountService;
        }

        // POST: api/discountorder/{orderId}/apply
        [HttpPost("{orderId}/apply")]
        public async Task<IActionResult> ApplyDiscountToOrder(int orderId, ApplyDiscountRequest request)
        {
            try
            {
                // Láº¥y order tá»« AppDbContext
                var order = await _context.Orders
                    .Include(o => o.Items)
                    .ThenInclude(i => i.Product)
                    .FirstOrDefaultAsync(o => o.OrderId == orderId);

                if (order == null)
                    return NotFound("KhÃ´ng tÃ¬m tháº¥y Ä‘Æ¡n hÃ ng");

                var orderDiscount = await _discountService.ApplyDiscountToOrderAsync(
                    orderId, request.DiscountId, order.Items, order.SubTotal, request.StaffId);

                // Cáº­p nháº­t tá»•ng discount amount cá»§a order
                var totalDiscounts = await _context.OrderDiscounts
                    .Where(od => od.OrderId == orderId)
                    .SumAsync(od => od.DiscountAmount);

                order.DiscountAmount = totalDiscounts;
                order.TotalAmount = order.SubTotal + order.TaxAmount - order.DiscountAmount;
                _context.Orders.Update(order);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Ãp dá»¥ng giáº£m giÃ¡ thÃ nh cÃ´ng", orderDiscount });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // POST: api/discountorder/{orderId}/items/{itemId}/apply
        [HttpPost("{orderId}/items/{itemId}/apply")]
        public async Task<IActionResult> ApplyDiscountToOrderItem(int orderId, int itemId, ApplyDiscountRequest request)
        {
            try
            {
                var orderDiscount = await _discountService.ApplyDiscountToOrderItemAsync(
                    orderId, itemId, request.DiscountId, request.StaffId);

                // Cáº­p nháº­t tá»•ng discount amount cá»§a order
                var order = await _context.Orders.FindAsync(orderId);
                if (order != null)
                {
                    var totalDiscounts = await _context.OrderDiscounts
                        .Where(od => od.OrderId == orderId)
                        .SumAsync(od => od.DiscountAmount);

                    order.DiscountAmount = totalDiscounts;
                    order.TotalAmount = order.SubTotal + order.TaxAmount - order.DiscountAmount;
                    _context.Orders.Update(order);
                    await _context.SaveChangesAsync();
                }

                return Ok(new { message = "Ãp dá»¥ng giáº£m giÃ¡ cho sáº£n pháº©m thÃ nh cÃ´ng", orderDiscount });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // DELETE: api/discountorder/{orderId}/discounts/{discountId}
        [HttpDelete("{orderId}/discounts/{discountId}")]
        public async Task<IActionResult> RemoveDiscountFromOrder(int orderId, int discountId)
        {
            try
            {
                await _discountService.RemoveDiscountFromOrderAsync(orderId, discountId);

                // Cáº­p nháº­t tá»•ng discount amount cá»§a order
                var order = await _context.Orders.FindAsync(orderId);
                if (order != null)
                {
                    var totalDiscounts = await _context.OrderDiscounts
                        .Where(od => od.OrderId == orderId)
                        .SumAsync(od => od.DiscountAmount);

                    order.DiscountAmount = totalDiscounts;
                    order.TotalAmount = order.SubTotal + order.TaxAmount - order.DiscountAmount;
                    _context.Orders.Update(order);
                    await _context.SaveChangesAsync();
                }

                return Ok(new { message = "XÃ³a giáº£m giÃ¡ thÃ nh cÃ´ng" });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // GET: api/discountorder/{orderId}/discounts
        [HttpGet("{orderId}/discounts")]
        public async Task<IActionResult> GetOrderDiscounts(int orderId)
        {
            var discounts = await _context.OrderDiscounts
                .Include(od => od.Discount)
                .Include(od => od.OrderItem)
                .Include(od => od.AppliedByStaff)
                .Where(od => od.OrderId == orderId)
                .ToListAsync();

            return Ok(discounts);
        }

        // GET: api/discountorder/{orderId}/calculate-total
        [HttpGet("{orderId}/calculate-total")]
        public async Task<IActionResult> CalculateOrderTotal(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
                return NotFound("KhÃ´ng tÃ¬m tháº¥y Ä‘Æ¡n hÃ ng");

            var discounts = await _context.OrderDiscounts
                .Where(od => od.OrderId == orderId)
                .ToListAsync();

            var finalTotal = await _discountService.CalculateOrderTotalWithDiscountsAsync(order.Items, discounts);

            return Ok(new { 
                orderId = orderId,
                subtotal = order.SubTotal,
                taxAmount = order.TaxAmount,
                discountAmount = discounts.Sum(d => d.DiscountAmount),
                finalTotal = finalTotal + order.TaxAmount
            });
        }
    }

    public class ApplyDiscountRequest
    {
        public int DiscountId { get; set; }
        public int? StaffId { get; set; }
    }
}
