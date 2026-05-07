using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using RetailPointBackend.Models;
using RetailPointBackend.DTOs;
using RetailPointBackend.Services;

namespace RetailPointBackend.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class CustomerTierManagementController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILoyaltyService _loyaltyService;
        private readonly ILogger<CustomerTierManagementController> _logger;

        public CustomerTierManagementController(AppDbContext context, ILoyaltyService loyaltyService, ILogger<CustomerTierManagementController> logger)
        {
            _context = context;
            _loyaltyService = loyaltyService;
            _logger = logger;
        }

        // GET: api/CustomerTierManagement
        [HttpGet]
        public async Task<ActionResult<List<CustomerTierDto>>> GetAllTiers()
        {
            try
            {
                var tiers = await _context.CustomerTiers
                    .OrderBy(t => t.MinSpent)
                    .Select(t => new CustomerTierDto
                    {
                        TierId = t.TierId,
                        TierName = t.TierName,
                        MinSpent = t.MinSpent,
                        MinPoints = t.MinPoints,
                        PointsMultiplier = t.PointsMultiplier,
                        DiscountPercentage = t.DiscountPercentage,
                        Description = t.Description,
                        TierColor = t.TierColor,
                        IsActive = t.IsActive
                    })
                    .ToListAsync();

                return Ok(tiers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting customer tiers");
                return StatusCode(500, new { message = "Lá»—i server", error = ex.Message });
            }
        }

        // GET: api/CustomerTierManagement/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<CustomerTierDto>> GetTier(int id)
        {
            try
            {
                var tier = await _context.CustomerTiers.FindAsync(id);
                if (tier == null)
                {
                    return NotFound(new { message = "KhÃ´ng tÃ¬m tháº¥y háº¡ng khÃ¡ch hÃ ng" });
                }

                var tierDto = new CustomerTierDto
                {
                    TierId = tier.TierId,
                    TierName = tier.TierName,
                    MinSpent = tier.MinSpent,
                    MinPoints = tier.MinPoints,
                    PointsMultiplier = tier.PointsMultiplier,
                    DiscountPercentage = tier.DiscountPercentage,
                    Description = tier.Description,
                    TierColor = tier.TierColor,
                    IsActive = tier.IsActive
                };

                return Ok(tierDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tier {TierId}", id);
                return StatusCode(500, new { message = "Lá»—i server", error = ex.Message });
            }
        }

        // POST: api/CustomerTierManagement
        [HttpPost]
        public async Task<ActionResult<CustomerTierDto>> CreateTier([FromBody] CustomerTierDto tierDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Validate tier name uniqueness
                var existingTier = await _context.CustomerTiers
                    .FirstOrDefaultAsync(t => t.TierName == tierDto.TierName && t.IsActive);
                if (existingTier != null)
                {
                    return BadRequest(new { message = "TÃªn háº¡ng Ä‘Ã£ tá»“n táº¡i" });
                }

                var newTier = new CustomerTier
                {
                    TierName = tierDto.TierName,
                    MinSpent = tierDto.MinSpent,
                    MinPoints = tierDto.MinPoints,
                    PointsMultiplier = tierDto.PointsMultiplier,
                    DiscountPercentage = tierDto.DiscountPercentage,
                    Description = tierDto.Description,
                    TierColor = tierDto.TierColor,
                    IsActive = tierDto.IsActive,
                    CreatedAt = DateTime.Now
                };

                _context.CustomerTiers.Add(newTier);
                await _context.SaveChangesAsync();

                // Update all customers to check for new tier eligibility
                _ = Task.Run(async () => await _loyaltyService.CheckAndUpdateAllCustomerTiersAsync());

                tierDto.TierId = newTier.TierId;
                return CreatedAtAction(nameof(GetTier), new { id = newTier.TierId }, tierDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating tier");
                return StatusCode(500, new { message = "Lá»—i server", error = ex.Message });
            }
        }

        // PUT: api/CustomerTierManagement/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTier(int id, [FromBody] CustomerTierDto tierDto)
        {
            try
            {
                if (id != tierDto.TierId)
                {
                    return BadRequest(new { message = "ID khÃ´ng khá»›p" });
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var tier = await _context.CustomerTiers.FindAsync(id);
                if (tier == null)
                {
                    return NotFound(new { message = "KhÃ´ng tÃ¬m tháº¥y háº¡ng khÃ¡ch hÃ ng" });
                }

                // Check name uniqueness (excluding current tier)
                var existingTier = await _context.CustomerTiers
                    .FirstOrDefaultAsync(t => t.TierName == tierDto.TierName && t.TierId != id && t.IsActive);
                if (existingTier != null)
                {
                    return BadRequest(new { message = "TÃªn háº¡ng Ä‘Ã£ tá»“n táº¡i" });
                }

                tier.TierName = tierDto.TierName;
                tier.MinSpent = tierDto.MinSpent;
                tier.MinPoints = tierDto.MinPoints;
                tier.PointsMultiplier = tierDto.PointsMultiplier;
                tier.DiscountPercentage = tierDto.DiscountPercentage;
                tier.Description = tierDto.Description;
                tier.TierColor = tierDto.TierColor;
                tier.IsActive = tierDto.IsActive;

                await _context.SaveChangesAsync();

                // Update all customers to recheck tier eligibility
                _ = Task.Run(async () => await _loyaltyService.CheckAndUpdateAllCustomerTiersAsync());

                return Ok(new { message = "Cáº­p nháº­t háº¡ng thÃ nh cÃ´ng" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating tier {TierId}", id);
                return StatusCode(500, new { message = "Lá»—i server", error = ex.Message });
            }
        }

        // DELETE: api/CustomerTierManagement/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTier(int id)
        {
            try
            {
                var tier = await _context.CustomerTiers.FindAsync(id);
                if (tier == null)
                {
                    return NotFound(new { message = "KhÃ´ng tÃ¬m tháº¥y háº¡ng khÃ¡ch hÃ ng" });
                }

                // Check if any customers are using this tier
                var customersCount = await _context.Customers.CountAsync(c => c.TierId == id);
                if (customersCount > 0)
                {
                    return BadRequest(new { 
                        message = $"KhÃ´ng thá»ƒ xÃ³a háº¡ng nÃ y vÃ¬ cÃ³ {customersCount} khÃ¡ch hÃ ng Ä‘ang sá»­ dá»¥ng. HÃ£y chuyá»ƒn khÃ¡ch hÃ ng sang háº¡ng khÃ¡c trÆ°á»›c." 
                    });
                }

                // Soft delete
                tier.IsActive = false;
                await _context.SaveChangesAsync();

                return Ok(new { message = "XÃ³a háº¡ng thÃ nh cÃ´ng" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting tier {TierId}", id);
                return StatusCode(500, new { message = "Lá»—i server", error = ex.Message });
            }
        }

        // GET: api/CustomerTierManagement/statistics
        [HttpGet("statistics")]
        public async Task<ActionResult> GetTierStatistics()
        {
            try
            {
                var tiers = await _context.CustomerTiers.Where(t => t.IsActive).ToListAsync();
                var statistics = new List<object>();

                foreach (var tier in tiers)
                {
                    var customerCount = await _context.Customers
                        .CountAsync(c => c.TierId == tier.TierId && c.IsActive);

                    var totalSpent = await _context.Customers
                        .Where(c => c.TierId == tier.TierId && c.IsActive)
                        .SumAsync(c => c.TotalSpent);

                    statistics.Add(new
                    {
                        tierId = tier.TierId,
                        tierName = tier.TierName,
                        customerCount = customerCount,
                        totalSpent = totalSpent,
                        averageSpent = customerCount > 0 ? totalSpent / customerCount : 0,
                        tierColor = tier.TierColor
                    });
                }

                return Ok(statistics.OrderBy(s => ((dynamic)s).tierName));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tier statistics");
                return StatusCode(500, new { message = "Lá»—i server", error = ex.Message });
            }
        }

        // POST: api/CustomerTierManagement/reorder
        [HttpPost("reorder")]
        public async Task<IActionResult> ReorderTiers([FromBody] List<int> tierIds)
        {
            try
            {
                // This could be used if you want to implement manual tier ordering
                // For now, tiers are automatically ordered by MinSpent
                return Ok(new { message = "Thá»© tá»± háº¡ng Ä‘Æ°á»£c sáº¯p xáº¿p theo chi tiÃªu tá»‘i thiá»ƒu" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reordering tiers");
                return StatusCode(500, new { message = "Lá»—i server", error = ex.Message });
            }
        }

        // POST: api/CustomerTierManagement/apply-discount/{tierId}
        [HttpPost("apply-discount/{tierId}")]
        public async Task<ActionResult> ApplyTierDiscountToOrders(int tierId, [FromQuery] DateTime? fromDate = null, [FromQuery] DateTime? toDate = null)
        {
            try
            {
                var tier = await _context.CustomerTiers.FindAsync(tierId);
                if (tier == null)
                {
                    return NotFound(new { message = "KhÃ´ng tÃ¬m tháº¥y háº¡ng khÃ¡ch hÃ ng" });
                }

                var query = _context.Orders.Where(o => o.Status == "pending" || o.Status == "processing");
                
                if (fromDate.HasValue)
                    query = query.Where(o => o.CreatedAt >= fromDate.Value);
                
                if (toDate.HasValue)
                    query = query.Where(o => o.CreatedAt <= toDate.Value);

                var orders = await query
                    .Include(o => o.Customer)
                    .Where(o => o.Customer.TierId == tierId)
                    .ToListAsync();

                int updatedCount = 0;
                foreach (var order in orders)
                {
                    var discountAmount = order.SubTotal * (tier.DiscountPercentage / 100);
                    order.DiscountAmount = discountAmount;
                    order.TotalAmount = order.SubTotal + order.TaxAmount - discountAmount;
                    updatedCount++;
                }

                await _context.SaveChangesAsync();

                return Ok(new { 
                    message = $"ÄÃ£ Ã¡p dá»¥ng giáº£m giÃ¡ {tier.DiscountPercentage}% cho {updatedCount} Ä‘Æ¡n hÃ ng",
                    updatedOrders = updatedCount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying tier discount");
                return StatusCode(500, new { message = "Lá»—i server", error = ex.Message });
            }
        }
    }
}
