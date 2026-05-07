using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using RetailPointBackend.Models;
using RetailPointBackend.DTOs;
using RetailPointBackend.Services;
using RetailPointBackend.Validators;

namespace RetailPointBackend.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class TierConfigurationController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILoyaltyService _loyaltyService;
        private readonly ILogger<TierConfigurationController> _logger;

        public TierConfigurationController(AppDbContext context, ILoyaltyService loyaltyService, ILogger<TierConfigurationController> logger)
        {
            _context = context;
            _loyaltyService = loyaltyService;
            _logger = logger;
        }

        // GET: api/TierConfiguration/settings
        [HttpGet("settings")]
        public async Task<ActionResult<object>> GetTierSettings()
        {
            try
            {
                var tiers = await _context.CustomerTiers
                    .Where(t => t.IsActive)
                    .OrderBy(t => t.MinSpent)
                    .Select(t => new
                    {
                        tierId = t.TierId,
                        tierName = t.TierName,
                        minSpent = t.MinSpent,
                        minPoints = t.MinPoints,
                        pointsMultiplier = t.PointsMultiplier,
                        discountPercentage = t.DiscountPercentage,
                        description = t.Description,
                        tierColor = t.TierColor,
                        isActive = t.IsActive
                    })
                    .ToListAsync();

                var loyaltyConfig = await _context.LoyaltyConfigs
                    .FirstOrDefaultAsync();

                return Ok(new
                {
                    tiers = tiers,
                    config = loyaltyConfig != null ? new
                    {
                        isEnabled = loyaltyConfig.IsEnabled,
                        pointsPerCurrency = loyaltyConfig.PointsPerCurrency,
                        minOrderAmountForPoints = loyaltyConfig.MinOrderAmountForPoints,
                        pointExpiryDays = loyaltyConfig.PointExpiryDays,
                        maxRedemptionPercentage = loyaltyConfig.MaxRedemptionPercentage
                    } : null,
                    statistics = new
                    {
                        totalCustomers = await _context.Customers.CountAsync(c => c.IsActive),
                        activeTiers = tiers.Count
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tier configuration");
                return StatusCode(500, new { message = "Lá»—i server", error = ex.Message });
            }
        }

        // PUT: api/TierConfiguration/batch-update
        [HttpPut("batch-update")]
        public async Task<ActionResult> BatchUpdateTiers([FromBody] List<CustomerTierDto> tierUpdates)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Use comprehensive validation
                var (isValid, errors, warnings) = TierConfigurationValidator.ValidateConfiguration(tierUpdates);
                
                if (!isValid)
                {
                    return BadRequest(new 
                    { 
                        message = "Cáº¥u hÃ¬nh khÃ´ng há»£p lá»‡", 
                        errors = errors,
                        warnings = warnings
                    });
                }

                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        foreach (var tierDto in tierUpdates)
                        {
                            var tier = await _context.CustomerTiers.FindAsync(tierDto.TierId);
                            if (tier != null)
                            {
                                tier.MinSpent = tierDto.MinSpent;
                                tier.MinPoints = tierDto.MinPoints;
                                tier.PointsMultiplier = tierDto.PointsMultiplier;
                                tier.DiscountPercentage = tierDto.DiscountPercentage;
                                tier.Description = tierDto.Description;
                                tier.TierColor = tierDto.TierColor;
                                tier.IsActive = tierDto.IsActive;
                            }
                        }

                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();

                        // Update customers tiers in background
                        _ = Task.Run(async () => await _loyaltyService.CheckAndUpdateAllCustomerTiersAsync());

                        return Ok(new
                        {
                            message = "Cáº­p nháº­t cáº¥u hÃ¬nh háº¡ng khÃ¡ch hÃ ng thÃ nh cÃ´ng",
                            updatedTiers = tierUpdates.Count,
                            warnings = warnings.Any() ? warnings : null
                        });
                    }
                    catch (Exception)
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error batch updating tiers");
                return StatusCode(500, new { message = "Lá»—i server", error = ex.Message });
            }
        }

        // POST: api/TierConfiguration/reset-defaults
        [HttpPost("reset-defaults")]
        public async Task<ActionResult> ResetToDefaults()
        {
            try
            {
                var confirmation = Request.Headers["X-Confirm-Reset"].FirstOrDefault();
                if (confirmation != "true")
                {
                    return BadRequest(new
                    {
                        message = "Cáº§n xÃ¡c nháº­n Ä‘á»ƒ reset vá» cáº¥u hÃ¬nh máº·c Ä‘á»‹nh",
                        instruction = "ThÃªm header X-Confirm-Reset: true Ä‘á»ƒ xÃ¡c nháº­n"
                    });
                }

                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        // Deactivate existing tiers
                        var existingTiers = await _context.CustomerTiers.ToListAsync();
                        foreach (var tier in existingTiers)
                        {
                            tier.IsActive = false;
                        }

                        // Create default tiers
                        var defaultTiers = new[]
                        {
                            new CustomerTier
                            {
                                TierName = "Äá»“ng",
                                MinSpent = 0,
                                MinPoints = 0,
                                PointsMultiplier = 1.0m,
                                DiscountPercentage = 0,
                                Description = "Háº¡ng khÃ¡ch hÃ ng cÆ¡ báº£n",
                                TierColor = "#CD7F32",
                                IsActive = true,
                                CreatedAt = DateTime.Now
                            },
                            new CustomerTier
                            {
                                TierName = "Báº¡c",
                                MinSpent = 5000000,
                                MinPoints = 500,
                                PointsMultiplier = 1.2m,
                                DiscountPercentage = 2,
                                Description = "KhÃ¡ch hÃ ng thÃ¢n thiáº¿t",
                                TierColor = "#C0C0C0",
                                IsActive = true,
                                CreatedAt = DateTime.Now
                            },
                            new CustomerTier
                            {
                                TierName = "VÃ ng",
                                MinSpent = 20000000,
                                MinPoints = 2000,
                                PointsMultiplier = 1.5m,
                                DiscountPercentage = 5,
                                Description = "KhÃ¡ch hÃ ng VIP",
                                TierColor = "#FFD700",
                                IsActive = true,
                                CreatedAt = DateTime.Now
                            },
                            new CustomerTier
                            {
                                TierName = "Kim cÆ°Æ¡ng",
                                MinSpent = 50000000,
                                MinPoints = 5000,
                                PointsMultiplier = 2.0m,
                                DiscountPercentage = 10,
                                Description = "KhÃ¡ch hÃ ng VVIP",
                                TierColor = "#B9F2FF",
                                IsActive = true,
                                CreatedAt = DateTime.Now
                            }
                        };

                        await _context.CustomerTiers.AddRangeAsync(defaultTiers);
                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();

                        // Update customer tiers
                        _ = Task.Run(async () => await _loyaltyService.CheckAndUpdateAllCustomerTiersAsync());

                        return Ok(new
                        {
                            message = "ÄÃ£ reset vá» cáº¥u hÃ¬nh háº¡ng máº·c Ä‘á»‹nh",
                            defaultTiers = defaultTiers.Select(t => new
                            {
                                tierName = t.TierName,
                                minSpent = t.MinSpent,
                                minPoints = t.MinPoints,
                                pointsMultiplier = t.PointsMultiplier,
                                discountPercentage = t.DiscountPercentage
                            }).ToList()
                        });
                    }
                    catch (Exception)
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting to default tiers");
                return StatusCode(500, new { message = "Lá»—i server", error = ex.Message });
            }
        }

        // POST: api/TierConfiguration/validate
        [HttpPost("validate")]
        public ActionResult ValidateTierConfiguration([FromBody] List<CustomerTierDto> tierConfigs)
        {
            try
            {
                var (isValid, errors, warnings) = TierConfigurationValidator.ValidateConfiguration(tierConfigs);

                return Ok(new
                {
                    isValid = isValid,
                    errors = errors,
                    warnings = warnings,
                    suggestions = new[]
                    {
                        "Äáº£m báº£o cÃ¡c háº¡ng cao hÆ¡n cÃ³ quyá»n lá»£i tá»‘t hÆ¡n",
                        "Xem xÃ©t khoáº£ng cÃ¡ch há»£p lÃ½ giá»¯a cÃ¡c háº¡ng",
                        "Kiá»ƒm tra tÃ­nh kháº£ thi cá»§a Ä‘iá»u kiá»‡n Ä‘áº¡t háº¡ng",
                        "NÃªn cÃ³ háº¡ng cÆ¡ báº£n cho khÃ¡ch hÃ ng má»›i (chi tiÃªu = 0)"
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating tier configuration");
                return StatusCode(500, new { message = "Lá»—i server", error = ex.Message });
            }
        }

        // POST: api/TierConfiguration/cleanup-disabled
        [HttpPost("cleanup-disabled")]
        public async Task<ActionResult> CleanupDisabledTiers()
        {
            try
            {
                // Find all disabled tiers
                var disabledTiers = await _context.CustomerTiers
                    .Where(t => !t.IsActive)
                    .ToListAsync();

                if (!disabledTiers.Any())
                {
                    return Ok(new
                    {
                        message = "KhÃ´ng cÃ³ háº¡ng nÃ o bá»‹ vÃ´ hiá»‡u hÃ³a Ä‘á»ƒ xÃ³a",
                        deletedTiers = 0
                    });
                }

                // Check if any customers are still using these disabled tiers
                var tiersWithCustomers = new List<string>();
                foreach (var tier in disabledTiers)
                {
                    var customerCount = await _context.Customers
                        .CountAsync(c => c.TierId == tier.TierId);
                    
                    if (customerCount > 0)
                    {
                        tiersWithCustomers.Add($"{tier.TierName} ({customerCount} khÃ¡ch hÃ ng)");
                    }
                }

                if (tiersWithCustomers.Any())
                {
                    return BadRequest(new
                    {
                        message = "KhÃ´ng thá»ƒ xÃ³a má»™t sá»‘ háº¡ng vÃ¬ váº«n cÃ³ khÃ¡ch hÃ ng sá»­ dá»¥ng",
                        tiersWithCustomers = tiersWithCustomers,
                        suggestion = "HÃ£y chuyá»ƒn khÃ¡ch hÃ ng sang háº¡ng khÃ¡c trÆ°á»›c khi xÃ³a"
                    });
                }

                // Safe to delete - no customers are using these tiers
                var tierNames = disabledTiers.Select(t => t.TierName).ToList();
                
                _context.CustomerTiers.RemoveRange(disabledTiers);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Cleaned up {Count} disabled tiers: {TierNames}", 
                    disabledTiers.Count, string.Join(", ", tierNames));

                return Ok(new
                {
                    message = $"ÄÃ£ xÃ³a {disabledTiers.Count} háº¡ng vÃ´ hiá»‡u hÃ³a",
                    deletedTiers = disabledTiers.Count,
                    deletedTierNames = tierNames
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up disabled tiers");
                return StatusCode(500, new { message = "Lá»—i server", error = ex.Message });
            }
        }

        // GET: api/TierConfiguration/preview-impact/{tierId}
        [HttpGet("preview-impact/{tierId}")]
        public async Task<ActionResult> PreviewTierImpact(int tierId, [FromQuery] decimal newMinSpent = 0, [FromQuery] int newMinPoints = 0)
        {
            try
            {
                // Validate parameters
                if (tierId <= 0)
                {
                    return BadRequest(new { message = "TierId pháº£i lá»›n hÆ¡n 0" });
                }

                if (newMinSpent < 0)
                {
                    return BadRequest(new { message = "Chi tiÃªu tá»‘i thiá»ƒu khÃ´ng Ä‘Æ°á»£c Ã¢m" });
                }

                if (newMinPoints < 0)
                {
                    return BadRequest(new { message = "Äiá»ƒm tá»‘i thiá»ƒu khÃ´ng Ä‘Æ°á»£c Ã¢m" });
                }

                var tier = await _context.CustomerTiers.FindAsync(tierId);
                if (tier == null)
                {
                    return NotFound(new { message = "KhÃ´ng tÃ¬m tháº¥y háº¡ng khÃ¡ch hÃ ng" });
                }

                // Current customers in this tier
                var currentCustomers = await _context.Customers
                    .Where(c => c.TierId == tierId && c.IsActive)
                    .CountAsync();

                // Customers who would qualify for new criteria
                var qualifiedCustomers = await _context.Customers
                    .Where(c => c.TotalSpent >= newMinSpent && c.LoyaltyPoints >= newMinPoints && c.IsActive)
                    .CountAsync();

                // Customers who would lose this tier
                var customersWouldLose = await _context.Customers
                    .Where(c => c.TierId == tierId && 
                               (c.TotalSpent < newMinSpent || c.LoyaltyPoints < newMinPoints) && 
                               c.IsActive)
                    .CountAsync();

                return Ok(new
                {
                    tierName = tier.TierName,
                    currentCriteria = new
                    {
                        minSpent = tier.MinSpent,
                        minPoints = tier.MinPoints
                    },
                    newCriteria = new
                    {
                        minSpent = newMinSpent,
                        minPoints = newMinPoints
                    },
                    impact = new
                    {
                        currentCustomers = currentCustomers,
                        qualifiedForNew = qualifiedCustomers,
                        wouldLoseTier = customersWouldLose,
                        netChange = qualifiedCustomers - currentCustomers
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error previewing tier impact");
                return StatusCode(500, new { message = "Lá»—i server", error = ex.Message });
            }
        }
    }
}
