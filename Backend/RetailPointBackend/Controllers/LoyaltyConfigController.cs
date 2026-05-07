using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using RetailPointBackend.Models;

namespace RetailPointBackend.Controllers
{
    [Authorize]
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
                    // Táº¡o config máº·c Ä‘á»‹nh náº¿u chÆ°a cÃ³
                    config = new LoyaltyConfig();
                    _context.LoyaltyConfigs.Add(config);
                    await _context.SaveChangesAsync();
                }

                return Ok(config);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lá»—i server", error = ex.Message });
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
                return Ok(new { message = "Cáº­p nháº­t cáº¥u hÃ¬nh tÃ­ch Ä‘iá»ƒm thÃ nh cÃ´ng" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lá»—i server", error = ex.Message });
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
                    return Ok(new { points = 0, message = "Há»‡ thá»‘ng tÃ­ch Ä‘iá»ƒm chÆ°a Ä‘Æ°á»£c kÃ­ch hoáº¡t" });
                }

                if (amount < config.MinOrderAmountForPoints)
                {
                    return Ok(new { points = 0, message = $"ÄÆ¡n hÃ ng tá»‘i thiá»ƒu {config.MinOrderAmountForPoints:C0} Ä‘á»ƒ tÃ­ch Ä‘iá»ƒm" });
                }

                var basePoints = Math.Floor(amount / config.PointsPerCurrency);
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

                var finalPoints = (int)Math.Floor(basePoints * multiplier);

                // Ãp dá»¥ng giá»›i háº¡n Ä‘iá»ƒm tá»‘i Ä‘a
                if (config.MaxPointsPerOrder.HasValue && finalPoints > config.MaxPointsPerOrder.Value)
                {
                    finalPoints = config.MaxPointsPerOrder.Value;
                    bonusInfo.Add($"Giá»›i háº¡n tá»‘i Ä‘a {config.MaxPointsPerOrder.Value} Ä‘iá»ƒm/Ä‘Æ¡n");
                }

                return Ok(new 
                { 
                    points = finalPoints,
                    basePoints = (int)basePoints,
                    multiplier = multiplier,
                    bonusInfo = bonusInfo,
                    formula = $"{amount:C0} Ã· {config.PointsPerCurrency:C0} Ã— {multiplier} = {finalPoints} Ä‘iá»ƒm"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lá»—i server", error = ex.Message });
            }
        }
    }
}
