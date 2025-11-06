using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RetailPointBackend.Models;

namespace RetailPointBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LoyaltyConfigController : ControllerBase
    {
        private readonly AppDbContext _context;

        public LoyaltyConfigController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/LoyaltyConfig
        [HttpGet]
        public async Task<ActionResult<LoyaltyConfig>> GetLoyaltyConfig()
        {
            try
            {
                var config = await _context.LoyaltyConfigs.FirstOrDefaultAsync();
                
                if (config == null)
                {
                    // Tạo config mặc định nếu chưa có
                    config = new LoyaltyConfig();
                    _context.LoyaltyConfigs.Add(config);
                    await _context.SaveChangesAsync();
                }

                return Ok(config);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi server", error = ex.Message });
            }
        }

        // PUT: api/LoyaltyConfig
        [HttpPut]
        public async Task<IActionResult> UpdateLoyaltyConfig([FromBody] LoyaltyConfig config)
        {
            try
            {
                var existing = await _context.LoyaltyConfigs.FirstOrDefaultAsync();
                
                if (existing == null)
                {
                    _context.LoyaltyConfigs.Add(config);
                }
                else
                {
                    existing.IsEnabled = config.IsEnabled;
                    existing.PointsPerCurrency = config.PointsPerCurrency;
                    existing.MinOrderAmountForPoints = config.MinOrderAmountForPoints;
                    existing.MaxPointsPerOrder = config.MaxPointsPerOrder;
                    existing.PointExpiryDays = config.PointExpiryDays;
                    existing.AllowPointRedemption = config.AllowPointRedemption;
                    existing.PointValue = config.PointValue;
                    existing.MaxRedemptionPercentage = config.MaxRedemptionPercentage;
                    existing.HappyHourEnabled = config.HappyHourEnabled;
                    existing.HappyHourStartTime = config.HappyHourStartTime;
                    existing.HappyHourEndTime = config.HappyHourEndTime;
                    existing.HappyHourMultiplier = config.HappyHourMultiplier;
                    existing.WeekendBonusEnabled = config.WeekendBonusEnabled;
                    existing.WeekendMultiplier = config.WeekendMultiplier;
                    existing.BirthdayBonusEnabled = config.BirthdayBonusEnabled;
                    existing.BirthdayMultiplier = config.BirthdayMultiplier;
                    existing.BirthdayValidDays = config.BirthdayValidDays;
                    existing.UpdatedAt = DateTime.Now;
                }

                await _context.SaveChangesAsync();
                return Ok(new { message = "Cập nhật cấu hình tích điểm thành công" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi server", error = ex.Message });
            }
        }

        // GET: api/LoyaltyConfig/calculate-points?amount=100000
        [HttpGet("calculate-points")]
        public async Task<ActionResult> CalculatePoints([FromQuery] decimal amount, [FromQuery] int? customerId = null)
        {
            try
            {
                var config = await _context.LoyaltyConfigs.FirstOrDefaultAsync();
                if (config == null || !config.IsEnabled)
                {
                    return Ok(new { points = 0, message = "Hệ thống tích điểm chưa được kích hoạt" });
                }

                if (amount < config.MinOrderAmountForPoints)
                {
                    return Ok(new { points = 0, message = $"Đơn hàng tối thiểu {config.MinOrderAmountForPoints:C0} để tích điểm" });
                }

                var basePoints = Math.Floor(amount / config.PointsPerCurrency);
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

                var finalPoints = (int)Math.Floor(basePoints * multiplier);

                // Áp dụng giới hạn điểm tối đa
                if (config.MaxPointsPerOrder.HasValue && finalPoints > config.MaxPointsPerOrder.Value)
                {
                    finalPoints = config.MaxPointsPerOrder.Value;
                    bonusInfo.Add($"Giới hạn tối đa {config.MaxPointsPerOrder.Value} điểm/đơn");
                }

                return Ok(new 
                { 
                    points = finalPoints,
                    basePoints = (int)basePoints,
                    multiplier = multiplier,
                    bonusInfo = bonusInfo,
                    formula = $"{amount:C0} ÷ {config.PointsPerCurrency:C0} × {multiplier} = {finalPoints} điểm"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi server", error = ex.Message });
            }
        }
    }
}