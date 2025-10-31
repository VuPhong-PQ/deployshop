using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RetailPointBackend.Models;
using RetailPointBackend.Services;

namespace RetailPointBackend.Controllers
{
    [ApiController]
    [Route("api/orders")]
    public class OrderDiscountController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IDiscountService _discountService;

        public OrderDiscountController(AppDbContext context, IDiscountService discountService)
        {
            _context = context;
            _discountService = discountService;
        }

        // POST: api/orders/{id}/discounts/apply
        [HttpPost("{id}/discounts/apply")]
        public async Task<IActionResult> ApplyDiscountToOrder(int id, OrderDiscountApplyRequest request)
        {
            try
            {
                var order = await _context.Orders
                    .Include(o => o.Items)
                    .ThenInclude(i => i.Product)
                    .FirstOrDefaultAsync(o => o.OrderId == id);

                if (order == null)
                    return NotFound("Không tìm thấy đơn hàng");

                var orderDiscount = await _discountService.ApplyDiscountToOrderAsync(
                    id, request.DiscountId, order.Items.ToList(), order.SubTotal, request.StaffId);

                // Cập nhật tổng discount amount của order
                var totalDiscounts = await _context.OrderDiscounts
                    .Where(od => od.OrderId == id)
                    .SumAsync(od => od.DiscountAmount);

                order.DiscountAmount = totalDiscounts;
                order.TotalAmount = order.SubTotal + order.TaxAmount - order.DiscountAmount;
                _context.Orders.Update(order);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Áp dụng giảm giá thành công", orderDiscount });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // POST: api/orders/{id}/items/{itemId}/discounts/apply
        [HttpPost("{id}/items/{itemId}/discounts/apply")]
        public async Task<IActionResult> ApplyDiscountToOrderItem(int id, int itemId, OrderDiscountApplyRequest request)
        {
            try
            {
                var orderDiscount = await _discountService.ApplyDiscountToOrderItemAsync(
                    id, itemId, request.DiscountId, request.StaffId);

                // Cập nhật order total
                var order = await _context.Orders.FindAsync(id);
                if (order != null)
                {
                    var totalDiscount = await _context.OrderDiscounts
                        .Where(od => od.OrderId == id)
                        .SumAsync(od => od.DiscountAmount);

                    order.DiscountAmount = totalDiscount;
                    order.TotalAmount = order.SubTotal - totalDiscount + order.TaxAmount;

                    _context.Orders.Update(order);
                    await _context.SaveChangesAsync();
                }

                return Ok(new { message = "Áp dụng giảm giá cho sản phẩm thành công", orderDiscount });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // DELETE: api/orders/{id}/discounts/{discountId}
        [HttpDelete("{id}/discounts/{discountId}")]
        public async Task<IActionResult> RemoveDiscountFromOrder(int id, int discountId)
        {
            try
            {
                await _discountService.RemoveDiscountFromOrderAsync(id, discountId);

                // Cập nhật order total
                var order = await _context.Orders.FindAsync(id);
                if (order != null)
                {
                    var totalDiscount = await _context.OrderDiscounts
                        .Where(od => od.OrderId == id)
                        .SumAsync(od => od.DiscountAmount);

                    order.DiscountAmount = totalDiscount;
                    order.TotalAmount = order.SubTotal - totalDiscount + order.TaxAmount;

                    _context.Orders.Update(order);
                    await _context.SaveChangesAsync();
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // GET: api/orders/{id}/discounts
        [HttpGet("{id}/discounts")]
        public async Task<ActionResult<IEnumerable<OrderDiscount>>> GetOrderDiscounts(int id)
        {
            var discounts = await _context.OrderDiscounts
                .Include(od => od.Discount)
                .Include(od => od.OrderItem)
                .Include(od => od.AppliedByStaff)
                .Where(od => od.OrderId == id)
                .ToListAsync();

            return Ok(discounts);
        }

        // POST: api/orders/{id}/discounts/calculate
        [HttpPost("{id}/discounts/calculate")]
        public async Task<ActionResult<OrderDiscountCalculateResponse>> CalculateDiscountForOrder(int id, OrderDiscountCalculateRequest request)
        {
            try
            {
                var order = await _context.Orders
                    .Include(o => o.Items)
                    .ThenInclude(i => i.Product)
                    .FirstOrDefaultAsync(o => o.OrderId == id);

                if (order == null)
                    return NotFound("Không tìm thấy đơn hàng");

                var canApply = await _discountService.CanApplyDiscountAsync(request.DiscountId, order.Items.ToList(), order.SubTotal);
                
                if (!canApply)
                {
                    return Ok(new OrderDiscountCalculateResponse
                    {
                        CanApply = false,
                        DiscountAmount = 0,
                        Message = "Không thể áp dụng giảm giá này"
                    });
                }

                var discountAmount = await _discountService.CalculateDiscountAmountAsync(request.DiscountId, order.Items.ToList(), order.SubTotal);
                
                return Ok(new OrderDiscountCalculateResponse
                {
                    CanApply = true,
                    DiscountAmount = discountAmount,
                    FinalTotal = order.SubTotal - discountAmount + order.TaxAmount
                });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }

    // DTOs cho OrderDiscountController
    public class OrderDiscountApplyRequest
    {
        public int DiscountId { get; set; }
        public int? StaffId { get; set; }
    }

    public class OrderDiscountCalculateRequest
    {
        public int DiscountId { get; set; }
    }

    public class OrderDiscountCalculateResponse
    {
        public bool CanApply { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal FinalTotal { get; set; }
        public string? Message { get; set; }
    }
}