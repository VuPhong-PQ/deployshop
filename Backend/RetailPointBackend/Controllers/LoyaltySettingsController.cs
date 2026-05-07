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
    public class LoyaltySettingsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILoyaltyService _loyaltyService;
        private readonly ILogger<LoyaltySettingsController> _logger;

        public LoyaltySettingsController(AppDbContext context, ILoyaltyService loyaltyService, ILogger<LoyaltySettingsController> logger)
        {
            _context = context;
            _loyaltyService = loyaltyService;
            _logger = logger;
        }

        // GET: api/LoyaltySettings
        [HttpGet]
        public async Task<ActionResult<LoyaltySettingsDto>> GetLoyaltySettings()
        {
            try
            {
                var config = await _context.LoyaltyConfigs.FirstOrDefaultAsync();
                var tiers = await _context.CustomerTiers
                    .Where(t => t.IsActive)
                    .OrderBy(t => t.MinSpent)
                    .ToListAsync();

                var result = new LoyaltySettingsDto
                {
                    Config = config == null ? new LoyaltyConfigDto() : new LoyaltyConfigDto
                    {
                        LoyaltyConfigId = config.LoyaltyConfigId,
                        IsEnabled = config.IsEnabled,
                        PointsPerCurrency = config.PointsPerCurrency,
                        MinOrderAmountForPoints = config.MinOrderAmountForPoints,
                        MaxPointsPerOrder = config.MaxPointsPerOrder,
                        PointExpiryDays = config.PointExpiryDays,
                        AllowPointRedemption = config.AllowPointRedemption,
                        PointValue = config.PointValue,
                        MaxRedemptionPercentage = config.MaxRedemptionPercentage,
                        HappyHourEnabled = config.HappyHourEnabled,
                        HappyHourStartTime = config.HappyHourStartTime,
                        HappyHourEndTime = config.HappyHourEndTime,
                        HappyHourMultiplier = config.HappyHourMultiplier,
                        WeekendBonusEnabled = config.WeekendBonusEnabled,
                        WeekendMultiplier = config.WeekendMultiplier,
                        BirthdayBonusEnabled = config.BirthdayBonusEnabled,
                        BirthdayMultiplier = config.BirthdayMultiplier,
                        BirthdayValidDays = config.BirthdayValidDays
                    },
                    Tiers = tiers.Select(t => new CustomerTierDto
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
                    }).ToList()
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting loyalty settings");
                return StatusCode(500, new { message = "Lá»—i server", error = ex.Message });
            }
        }

        // PUT: api/LoyaltySettings
        [HttpPut]
        public async Task<IActionResult> UpdateLoyaltySettings([FromBody] LoyaltySettingsDto settingsDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    // Cáº­p nháº­t LoyaltyConfig
                    var existingConfig = await _context.LoyaltyConfigs.FirstOrDefaultAsync();
                    if (existingConfig == null)
                    {
                        var newConfig = new LoyaltyConfig
                        {
                            IsEnabled = settingsDto.Config.IsEnabled,
                            PointsPerCurrency = settingsDto.Config.PointsPerCurrency,
                            MinOrderAmountForPoints = settingsDto.Config.MinOrderAmountForPoints,
                            MaxPointsPerOrder = settingsDto.Config.MaxPointsPerOrder,
                            PointExpiryDays = settingsDto.Config.PointExpiryDays,
                            AllowPointRedemption = settingsDto.Config.AllowPointRedemption,
                            PointValue = settingsDto.Config.PointValue,
                            MaxRedemptionPercentage = settingsDto.Config.MaxRedemptionPercentage,
                            HappyHourEnabled = settingsDto.Config.HappyHourEnabled,
                            HappyHourStartTime = settingsDto.Config.HappyHourStartTime ?? new TimeSpan(17, 0, 0),
                            HappyHourEndTime = settingsDto.Config.HappyHourEndTime ?? new TimeSpan(19, 0, 0),
                            HappyHourMultiplier = settingsDto.Config.HappyHourMultiplier,
                            WeekendBonusEnabled = settingsDto.Config.WeekendBonusEnabled,
                            WeekendMultiplier = settingsDto.Config.WeekendMultiplier,
                            BirthdayBonusEnabled = settingsDto.Config.BirthdayBonusEnabled,
                            BirthdayMultiplier = settingsDto.Config.BirthdayMultiplier,
                            BirthdayValidDays = settingsDto.Config.BirthdayValidDays,
                            CreatedAt = DateTime.Now
                        };
                        _context.LoyaltyConfigs.Add(newConfig);
                    }
                    else
                    {
                        existingConfig.IsEnabled = settingsDto.Config.IsEnabled;
                        existingConfig.PointsPerCurrency = settingsDto.Config.PointsPerCurrency;
                        existingConfig.MinOrderAmountForPoints = settingsDto.Config.MinOrderAmountForPoints;
                        existingConfig.MaxPointsPerOrder = settingsDto.Config.MaxPointsPerOrder;
                        existingConfig.PointExpiryDays = settingsDto.Config.PointExpiryDays;
                        existingConfig.AllowPointRedemption = settingsDto.Config.AllowPointRedemption;
                        existingConfig.PointValue = settingsDto.Config.PointValue;
                        existingConfig.MaxRedemptionPercentage = settingsDto.Config.MaxRedemptionPercentage;
                        existingConfig.HappyHourEnabled = settingsDto.Config.HappyHourEnabled;
                        existingConfig.HappyHourStartTime = settingsDto.Config.HappyHourStartTime ?? new TimeSpan(17, 0, 0);
                        existingConfig.HappyHourEndTime = settingsDto.Config.HappyHourEndTime ?? new TimeSpan(19, 0, 0);
                        existingConfig.HappyHourMultiplier = settingsDto.Config.HappyHourMultiplier;
                        existingConfig.WeekendBonusEnabled = settingsDto.Config.WeekendBonusEnabled;
                        existingConfig.WeekendMultiplier = settingsDto.Config.WeekendMultiplier;
                        existingConfig.BirthdayBonusEnabled = settingsDto.Config.BirthdayBonusEnabled;
                        existingConfig.BirthdayMultiplier = settingsDto.Config.BirthdayMultiplier;
                        existingConfig.BirthdayValidDays = settingsDto.Config.BirthdayValidDays;
                        existingConfig.UpdatedAt = DateTime.Now;
                    }

                    // Cáº­p nháº­t CustomerTiers
                    var existingTiers = await _context.CustomerTiers.ToListAsync();
                    
                    // XÃ³a (soft delete) cÃ¡c tier khÃ´ng cÃ²n trong danh sÃ¡ch
                    foreach (var existingTier in existingTiers)
                    {
                        if (!settingsDto.Tiers.Any(t => t.TierId == existingTier.TierId))
                        {
                            existingTier.IsActive = false;
                        }
                    }

                    // Cáº­p nháº­t hoáº·c thÃªm má»›i cÃ¡c tier
                    foreach (var tierDto in settingsDto.Tiers)
                    {
                        if (tierDto.TierId == 0)
                        {
                            // ThÃªm má»›i
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
                        }
                        else
                        {
                            // Cáº­p nháº­t
                            var existingTier = existingTiers.FirstOrDefault(t => t.TierId == tierDto.TierId);
                            if (existingTier != null)
                            {
                                existingTier.TierName = tierDto.TierName;
                                existingTier.MinSpent = tierDto.MinSpent;
                                existingTier.MinPoints = tierDto.MinPoints;
                                existingTier.PointsMultiplier = tierDto.PointsMultiplier;
                                existingTier.DiscountPercentage = tierDto.DiscountPercentage;
                                existingTier.Description = tierDto.Description;
                                existingTier.TierColor = tierDto.TierColor;
                                existingTier.IsActive = tierDto.IsActive;
                            }
                        }
                    }

                    await _context.SaveChangesAsync();

                    // Cáº­p nháº­t láº¡i háº¡ng cho táº¥t cáº£ khÃ¡ch hÃ ng
                    _ = Task.Run(async () => await _loyaltyService.CheckAndUpdateAllCustomerTiersAsync());

                    await transaction.CommitAsync();

                    _logger.LogInformation("Loyalty settings updated successfully");
                    return Ok(new { message = "Cáº­p nháº­t cÃ i Ä‘áº·t tÃ­ch Ä‘iá»ƒm thÃ nh cÃ´ng" });
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating loyalty settings");
                return StatusCode(500, new { message = "Lá»—i server", error = ex.Message });
            }
        }

        // GET: api/LoyaltySettings/customer-status/{customerId}
        [HttpGet("customer-status/{customerId}")]
        public async Task<ActionResult<CustomerLoyaltyStatusDto>> GetCustomerLoyaltyStatus(int customerId)
        {
            try
            {
                var result = await _loyaltyService.GetCustomerLoyaltyStatusAsync(customerId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting customer loyalty status for {CustomerId}", customerId);
                return StatusCode(500, new { message = "Lá»—i server", error = ex.Message });
            }
        }

        // POST: api/LoyaltySettings/calculate-points
        [HttpPost("calculate-points")]
        public async Task<ActionResult<PointsCalculationDto>> CalculatePoints([FromBody] dynamic request)
        {
            try
            {
                decimal amount = request.amount;
                int? customerId = request.customerId;

                var config = await _loyaltyService.GetLoyaltyConfigAsync();
                if (!config.IsEnabled)
                {
                    return Ok(new PointsCalculationDto { 
                        Points = 0, 
                        Message = "Há»‡ thá»‘ng tÃ­ch Ä‘iá»ƒm chÆ°a Ä‘Æ°á»£c kÃ­ch hoáº¡t" 
                    });
                }

                if (amount < config.MinOrderAmountForPoints)
                {
                    return Ok(new PointsCalculationDto { 
                        Points = 0, 
                        Message = $"ÄÆ¡n hÃ ng tá»‘i thiá»ƒu {config.MinOrderAmountForPoints:C0} Ä‘á»ƒ tÃ­ch Ä‘iá»ƒm" 
                    });
                }

                var basePoints = (int)(amount / config.PointsPerCurrency);
                var multiplier = 1.0m;
                var bonusInfo = new List<string>();

                // Kiá»ƒm tra Happy Hour
                if (config.HappyHourEnabled)
                {
                    var currentTime = DateTime.Now.TimeOfDay;
                    if (currentTime >= config.HappyHourStartTime && currentTime <= config.HappyHourEndTime)
                    {
                        multiplier *= config.HappyHourMultiplier;
                        bonusInfo.Add($"Giá» vÃ ng x{config.HappyHourMultiplier}");
                    }
                }

                // Kiá»ƒm tra Weekend Bonus
                if (config.WeekendBonusEnabled && (DateTime.Now.DayOfWeek == DayOfWeek.Saturday || DateTime.Now.DayOfWeek == DayOfWeek.Sunday))
                {
                    multiplier *= config.WeekendMultiplier;
                    bonusInfo.Add($"Cuá»‘i tuáº§n x{config.WeekendMultiplier}");
                }

                // Kiá»ƒm tra Customer Tier náº¿u cÃ³ customerId
                if (customerId.HasValue)
                {
                    var customer = await _context.Customers.Include(c => c.CustomerTier).FirstOrDefaultAsync(c => c.CustomerId == customerId);
                    if (customer?.CustomerTier != null)
                    {
                        multiplier *= customer.CustomerTier.PointsMultiplier;
                        bonusInfo.Add($"Háº¡ng {customer.CustomerTier.TierName} x{customer.CustomerTier.PointsMultiplier}");
                    }
                }

                var finalPoints = (int)(basePoints * multiplier);

                // Ãp dá»¥ng giá»›i háº¡n Ä‘iá»ƒm tá»‘i Ä‘a
                if (config.MaxPointsPerOrder.HasValue && finalPoints > config.MaxPointsPerOrder.Value)
                {
                    finalPoints = config.MaxPointsPerOrder.Value;
                    bonusInfo.Add($"Giá»›i háº¡n tá»‘i Ä‘a {config.MaxPointsPerOrder.Value} Ä‘iá»ƒm/Ä‘Æ¡n");
                }

                return Ok(new PointsCalculationDto
                {
                    Points = finalPoints,
                    BasePoints = basePoints,
                    Multiplier = multiplier,
                    BonusInfo = bonusInfo,
                    Formula = $"{amount:C0} Ã· {config.PointsPerCurrency:C0} Ã— {multiplier} = {finalPoints} Ä‘iá»ƒm"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating points");
                return StatusCode(500, new { message = "Lá»—i server", error = ex.Message });
            }
        }

        // POST: api/LoyaltySettings/update-all-tiers
        [HttpPost("update-all-tiers")]
        public async Task<IActionResult> UpdateAllCustomerTiers()
        {
            try
            {
                var success = await _loyaltyService.CheckAndUpdateAllCustomerTiersAsync();
                if (success)
                {
                    return Ok(new { message = "ÄÃ£ cáº­p nháº­t háº¡ng cho táº¥t cáº£ khÃ¡ch hÃ ng" });
                }
                return StatusCode(500, new { message = "CÃ³ lá»—i khi cáº­p nháº­t háº¡ng khÃ¡ch hÃ ng" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating all customer tiers");
                return StatusCode(500, new { message = "Lá»—i server", error = ex.Message });
            }
        }

        // POST: api/LoyaltySettings/fix-enum-mapping
        [HttpPost("fix-enum-mapping")]
        public async Task<IActionResult> FixEnumMapping()
        {
            try
            {
                var customers = await _context.Customers
                    .Include(c => c.CustomerTier)
                    .Where(c => c.TierId.HasValue)
                    .ToListAsync();

                int fixedCount = 0;
                foreach (var customer in customers)
                {
                    if (customer.CustomerTier != null)
                    {
                        var correctEnum = customer.CustomerTier.TierName switch
                        {
                            "Kim cÆ°Æ¡ng" => CustomerRank.Platinum,  // Cao nháº¥t (3)
                            "VÃ ng" => CustomerRank.VIP,            // Cao (2)
                            "Báº¡c" => CustomerRank.Premium,         // Trung bÃ¬nh (1)
                            "Äá»“ng" => CustomerRank.Thuong,        // Tháº¥p nháº¥t (0)
                            _ => CustomerRank.Thuong
                        };

                        if (customer.HangKhachHang != correctEnum)
                        {
                            customer.HangKhachHang = correctEnum;
                            fixedCount++;
                            _logger.LogInformation("Fixed enum mapping for customer {CustomerId}: {TierName} -> {EnumValue}", 
                                customer.CustomerId, customer.CustomerTier.TierName, correctEnum);
                        }
                    }
                }

                await _context.SaveChangesAsync();
                
                return Ok(new { 
                    message = $"ÄÃ£ sá»­a enum mapping cho {fixedCount} khÃ¡ch hÃ ng",
                    fixedCount = fixedCount,
                    totalCustomers = customers.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fixing enum mapping");
                return StatusCode(500, new { message = "Lá»—i server", error = ex.Message });
            }
        }
    }
}
