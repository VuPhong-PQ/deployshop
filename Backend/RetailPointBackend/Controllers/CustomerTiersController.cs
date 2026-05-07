using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using RetailPointBackend.Models;

namespace RetailPointBackend.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class CustomerTiersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CustomerTiersController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/CustomerTiers
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CustomerTier>>> GetCustomerTiers()
        {
            try
            {
                var tiers = await _context.CustomerTiers
                    .Where(t => t.IsActive)
                    .OrderBy(t => t.MinSpent)
                    .ToListAsync();

                return Ok(tiers);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lá»—i server", error = ex.Message });
            }
        }

        // GET: api/CustomerTiers/5
        [HttpGet("{id}")]
        public async Task<ActionResult<CustomerTier>> GetCustomerTier(int id)
        {
            try
            {
                var tier = await _context.CustomerTiers.FindAsync(id);

                if (tier == null)
                {
                    return NotFound(new { message = "KhÃ´ng tÃ¬m tháº¥y cáº¥p Ä‘á»™ khÃ¡ch hÃ ng" });
                }

                return Ok(tier);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lá»—i server", error = ex.Message });
            }
        }

        // POST: api/CustomerTiers
        [HttpPost]
        public async Task<ActionResult<CustomerTier>> CreateCustomerTier([FromBody] CustomerTier tier)
        {
            try
            {
                tier.CreatedAt = DateTime.Now;
                _context.CustomerTiers.Add(tier);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetCustomerTier), new { id = tier.TierId }, tier);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lá»—i server", error = ex.Message });
            }
        }

        // PUT: api/CustomerTiers/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCustomerTier(int id, [FromBody] CustomerTier tier)
        {
            try
            {
                if (id != tier.TierId)
                {
                    return BadRequest(new { message = "ID khÃ´ng khá»›p" });
                }

                var existing = await _context.CustomerTiers.FindAsync(id);
                if (existing == null)
                {
                    return NotFound(new { message = "KhÃ´ng tÃ¬m tháº¥y cáº¥p Ä‘á»™ khÃ¡ch hÃ ng" });
                }

                existing.TierName = tier.TierName;
                existing.MinSpent = tier.MinSpent;
                existing.MinPoints = tier.MinPoints;
                existing.PointsMultiplier = tier.PointsMultiplier;
                existing.DiscountPercentage = tier.DiscountPercentage;
                existing.Description = tier.Description;
                existing.TierColor = tier.TierColor;
                existing.IsActive = tier.IsActive;

                await _context.SaveChangesAsync();
                return Ok(new { message = "Cáº­p nháº­t cáº¥p Ä‘á»™ khÃ¡ch hÃ ng thÃ nh cÃ´ng" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lá»—i server", error = ex.Message });
            }
        }

        // DELETE: api/CustomerTiers/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCustomerTier(int id)
        {
            try
            {
                var tier = await _context.CustomerTiers.FindAsync(id);
                if (tier == null)
                {
                    return NotFound(new { message = "KhÃ´ng tÃ¬m tháº¥y cáº¥p Ä‘á»™ khÃ¡ch hÃ ng" });
                }

                // Soft delete
                tier.IsActive = false;
                await _context.SaveChangesAsync();

                return Ok(new { message = "XÃ³a cáº¥p Ä‘á»™ khÃ¡ch hÃ ng thÃ nh cÃ´ng" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lá»—i server", error = ex.Message });
            }
        }

        // GET: api/CustomerTiers/evaluate-customer/5
        [HttpGet("evaluate-customer/{customerId}")]
        public async Task<ActionResult> EvaluateCustomerTier(int customerId)
        {
            try
            {
                var customer = await _context.Customers.FindAsync(customerId);
                if (customer == null)
                {
                    return NotFound(new { message = "KhÃ´ng tÃ¬m tháº¥y khÃ¡ch hÃ ng" });
                }

                // TÃ­nh tá»•ng chi tiÃªu
                var totalSpent = await _context.Orders
                    .Where(o => o.CustomerId == customerId && o.Status == "completed")
                    .SumAsync(o => o.TotalAmount);

                // TÃ­nh tá»•ng Ä‘iá»ƒm hiá»‡n táº¡i
                var totalPoints = await _context.LoyaltyTransactions
                    .Where(t => t.CustomerId == customerId)
                    .SumAsync(t => t.Points);

                // TÃ¬m cáº¥p Ä‘á»™ phÃ¹ há»£p
                var appropriateTier = await _context.CustomerTiers
                    .Where(t => t.IsActive && t.MinSpent <= totalSpent && t.MinPoints <= totalPoints)
                    .OrderByDescending(t => t.MinSpent)
                    .FirstOrDefaultAsync();

                if (appropriateTier == null)
                {
                    appropriateTier = await _context.CustomerTiers
                        .Where(t => t.IsActive)
                        .OrderBy(t => t.MinSpent)
                        .FirstOrDefaultAsync();
                }

                return Ok(new 
                { 
                    customerId = customerId,
                    totalSpent = totalSpent,
                    totalPoints = totalPoints,
                    currentTier = appropriateTier,
                    nextTier = await _context.CustomerTiers
                        .Where(t => t.IsActive && t.MinSpent > totalSpent)
                        .OrderBy(t => t.MinSpent)
                        .FirstOrDefaultAsync()
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lá»—i server", error = ex.Message });
            }
        }
    }
}
