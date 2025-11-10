using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RetailPointBackend.Models;
using RetailPointBackend.DTOs;
using RetailPointBackend.Services;
using RetailPointBackend.Validators;

namespace RetailPointBackend.Controllers
{
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
                return StatusCode(500, new { message = "Lỗi server", error = ex.Message });
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
                        message = "Cấu hình không hợp lệ", 
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
                            message = "Cập nhật cấu hình hạng khách hàng thành công",
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
                return StatusCode(500, new { message = "Lỗi server", error = ex.Message });
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
                        message = "Cần xác nhận để reset về cấu hình mặc định",
                        instruction = "Thêm header X-Confirm-Reset: true để xác nhận"
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
                                TierName = "Đồng",
                                MinSpent = 0,
                                MinPoints = 0,
                                PointsMultiplier = 1.0m,
                                DiscountPercentage = 0,
                                Description = "Hạng khách hàng cơ bản",
                                TierColor = "#CD7F32",
                                IsActive = true,
                                CreatedAt = DateTime.Now
                            },
                            new CustomerTier
                            {
                                TierName = "Bạc",
                                MinSpent = 5000000,
                                MinPoints = 500,
                                PointsMultiplier = 1.2m,
                                DiscountPercentage = 2,
                                Description = "Khách hàng thân thiết",
                                TierColor = "#C0C0C0",
                                IsActive = true,
                                CreatedAt = DateTime.Now
                            },
                            new CustomerTier
                            {
                                TierName = "Vàng",
                                MinSpent = 20000000,
                                MinPoints = 2000,
                                PointsMultiplier = 1.5m,
                                DiscountPercentage = 5,
                                Description = "Khách hàng VIP",
                                TierColor = "#FFD700",
                                IsActive = true,
                                CreatedAt = DateTime.Now
                            },
                            new CustomerTier
                            {
                                TierName = "Kim cương",
                                MinSpent = 50000000,
                                MinPoints = 5000,
                                PointsMultiplier = 2.0m,
                                DiscountPercentage = 10,
                                Description = "Khách hàng VVIP",
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
                            message = "Đã reset về cấu hình hạng mặc định",
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
                return StatusCode(500, new { message = "Lỗi server", error = ex.Message });
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
                        "Đảm bảo các hạng cao hơn có quyền lợi tốt hơn",
                        "Xem xét khoảng cách hợp lý giữa các hạng",
                        "Kiểm tra tính khả thi của điều kiện đạt hạng",
                        "Nên có hạng cơ bản cho khách hàng mới (chi tiêu = 0)"
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating tier configuration");
                return StatusCode(500, new { message = "Lỗi server", error = ex.Message });
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
                        message = "Không có hạng nào bị vô hiệu hóa để xóa",
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
                        tiersWithCustomers.Add($"{tier.TierName} ({customerCount} khách hàng)");
                    }
                }

                if (tiersWithCustomers.Any())
                {
                    return BadRequest(new
                    {
                        message = "Không thể xóa một số hạng vì vẫn có khách hàng sử dụng",
                        tiersWithCustomers = tiersWithCustomers,
                        suggestion = "Hãy chuyển khách hàng sang hạng khác trước khi xóa"
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
                    message = $"Đã xóa {disabledTiers.Count} hạng vô hiệu hóa",
                    deletedTiers = disabledTiers.Count,
                    deletedTierNames = tierNames
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up disabled tiers");
                return StatusCode(500, new { message = "Lỗi server", error = ex.Message });
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
                    return BadRequest(new { message = "TierId phải lớn hơn 0" });
                }

                if (newMinSpent < 0)
                {
                    return BadRequest(new { message = "Chi tiêu tối thiểu không được âm" });
                }

                if (newMinPoints < 0)
                {
                    return BadRequest(new { message = "Điểm tối thiểu không được âm" });
                }

                var tier = await _context.CustomerTiers.FindAsync(tierId);
                if (tier == null)
                {
                    return NotFound(new { message = "Không tìm thấy hạng khách hàng" });
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
                return StatusCode(500, new { message = "Lỗi server", error = ex.Message });
            }
        }
    }
}