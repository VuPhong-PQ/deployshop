using Microsoft.AspNetCore.Mvc;
using RetailPointBackend.Models;
using RetailPointBackend.Services;
using Microsoft.EntityFrameworkCore;

namespace RetailPointBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LoyaltyProcessController : ControllerBase
    {
        private readonly ILoyaltyService _loyaltyService;
        private readonly AppDbContext _context;
        private readonly ILogger<LoyaltyProcessController> _logger;

        public LoyaltyProcessController(ILoyaltyService loyaltyService, AppDbContext context, ILogger<LoyaltyProcessController> logger)
        {
            _loyaltyService = loyaltyService;
            _context = context;
            _logger = logger;
        }

        [HttpPost("process-order/{orderId}")]
        public async Task<IActionResult> ProcessOrderPoints(int orderId)
        {
            try
            {
                var result = await _loyaltyService.ProcessOrderPointsAsync(orderId);
                if (result)
                {
                    return Ok(new { success = true, message = $"Points processed for order {orderId}" });
                }
                return BadRequest(new { success = false, message = "Failed to process points" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing points for order {OrderId}", orderId);
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPost("process-all-completed-orders")]
        public async Task<IActionResult> ProcessAllCompletedOrders()
        {
            try
            {
                var completedOrders = await _context.Orders
                    .Where(o => o.Status == "completed" && o.CustomerId != null)
                    .ToListAsync();

                int processed = 0;
                int skipped = 0;

                foreach (var order in completedOrders)
                {
                    // Check if already processed
                    var hasTransaction = await _context.LoyaltyTransactions
                        .AnyAsync(t => t.OrderId == order.OrderId && t.TransactionType == LoyaltyTransactionType.EARN);

                    if (!hasTransaction)
                    {
                        var result = await _loyaltyService.ProcessOrderPointsAsync(order.OrderId);
                        if (result) processed++;
                    }
                    else
                    {
                        skipped++;
                    }
                }

                return Ok(new { 
                    success = true, 
                    message = $"Processed {processed} orders, skipped {skipped} already processed orders",
                    processed,
                    skipped,
                    total = completedOrders.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing all completed orders");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("calculate-points/{customerId}/{amount}")]
        public async Task<IActionResult> CalculatePoints(int customerId, decimal amount)
        {
            try
            {
                var points = await _loyaltyService.CalculatePointsForOrderAsync(customerId, amount, 0);
                return Ok(new { customerId, amount, points });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating points for customer {CustomerId}", customerId);
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }
    }
}