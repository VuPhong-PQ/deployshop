using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RetailPointBackend.Models;
using RetailPointBackend.DTOs;
using RetailPointBackend.Services;

namespace RetailPointBackend.Controllers
{
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
                return StatusCode(500, new { message = "Lỗi server", error = ex.Message });
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
                    // Cập nhật LoyaltyConfig
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

                    // Cập nhật CustomerTiers
                    var existingTiers = await _context.CustomerTiers.ToListAsync();
                    
                    // Xóa (soft delete) các tier không còn trong danh sách
                    foreach (var existingTier in existingTiers)
                    {
                        if (!settingsDto.Tiers.Any(t => t.TierId == existingTier.TierId))
                        {
                            existingTier.IsActive = false;
                        }
                    }

                    // Cập nhật hoặc thêm mới các tier
                    foreach (var tierDto in settingsDto.Tiers)
                    {
                        if (tierDto.TierId == 0)
                        {
                            // Thêm mới
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
                            // Cập nhật
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

                    // Cập nhật lại hạng cho tất cả khách hàng
                    _ = Task.Run(async () => await _loyaltyService.CheckAndUpdateAllCustomerTiersAsync());

                    await transaction.CommitAsync();

                    _logger.LogInformation("Loyalty settings updated successfully");
                    return Ok(new { message = "Cập nhật cài đặt tích điểm thành công" });
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
                return StatusCode(500, new { message = "Lỗi server", error = ex.Message });
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
                return StatusCode(500, new { message = "Lỗi server", error = ex.Message });
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
                        Message = "Hệ thống tích điểm chưa được kích hoạt" 
                    });
                }

                if (amount < config.MinOrderAmountForPoints)
                {
                    return Ok(new PointsCalculationDto { 
                        Points = 0, 
                        Message = $"Đơn hàng tối thiểu {config.MinOrderAmountForPoints:C0} để tích điểm" 
                    });
                }

                var basePoints = (int)(amount / config.PointsPerCurrency);
                var multiplier = 1.0m;
                var bonusInfo = new List<string>();

                // Kiểm tra Happy Hour
                if (config.HappyHourEnabled)
                {
                    var currentTime = DateTime.Now.TimeOfDay;
                    if (currentTime >= config.HappyHourStartTime && currentTime <= config.HappyHourEndTime)
                    {
                        multiplier *= config.HappyHourMultiplier;
                        bonusInfo.Add($"Giờ vàng x{config.HappyHourMultiplier}");
                    }
                }

                // Kiểm tra Weekend Bonus
                if (config.WeekendBonusEnabled && (DateTime.Now.DayOfWeek == DayOfWeek.Saturday || DateTime.Now.DayOfWeek == DayOfWeek.Sunday))
                {
                    multiplier *= config.WeekendMultiplier;
                    bonusInfo.Add($"Cuối tuần x{config.WeekendMultiplier}");
                }

                // Kiểm tra Customer Tier nếu có customerId
                if (customerId.HasValue)
                {
                    var customer = await _context.Customers.Include(c => c.CustomerTier).FirstOrDefaultAsync(c => c.CustomerId == customerId);
                    if (customer?.CustomerTier != null)
                    {
                        multiplier *= customer.CustomerTier.PointsMultiplier;
                        bonusInfo.Add($"Hạng {customer.CustomerTier.TierName} x{customer.CustomerTier.PointsMultiplier}");
                    }
                }

                var finalPoints = (int)(basePoints * multiplier);

                // Áp dụng giới hạn điểm tối đa
                if (config.MaxPointsPerOrder.HasValue && finalPoints > config.MaxPointsPerOrder.Value)
                {
                    finalPoints = config.MaxPointsPerOrder.Value;
                    bonusInfo.Add($"Giới hạn tối đa {config.MaxPointsPerOrder.Value} điểm/đơn");
                }

                return Ok(new PointsCalculationDto
                {
                    Points = finalPoints,
                    BasePoints = basePoints,
                    Multiplier = multiplier,
                    BonusInfo = bonusInfo,
                    Formula = $"{amount:C0} ÷ {config.PointsPerCurrency:C0} × {multiplier} = {finalPoints} điểm"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating points");
                return StatusCode(500, new { message = "Lỗi server", error = ex.Message });
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
                    return Ok(new { message = "Đã cập nhật hạng cho tất cả khách hàng" });
                }
                return StatusCode(500, new { message = "Có lỗi khi cập nhật hạng khách hàng" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating all customer tiers");
                return StatusCode(500, new { message = "Lỗi server", error = ex.Message });
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
                            "Kim cương" => CustomerRank.Platinum,  // Cao nhất (3)
                            "Vàng" => CustomerRank.VIP,            // Cao (2)
                            "Bạc" => CustomerRank.Premium,         // Trung bình (1)
                            "Đồng" => CustomerRank.Thuong,        // Thấp nhất (0)
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
                    message = $"Đã sửa enum mapping cho {fixedCount} khách hàng",
                    fixedCount = fixedCount,
                    totalCustomers = customers.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fixing enum mapping");
                return StatusCode(500, new { message = "Lỗi server", error = ex.Message });
            }
        }
    }
}