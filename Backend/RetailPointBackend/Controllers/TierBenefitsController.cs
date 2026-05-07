using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using RetailPointBackend.Models;
using RetailPointBackend.Services;

namespace RetailPointBackend.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class TierBenefitsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILoyaltyService _loyaltyService;
        private readonly ILogger<TierBenefitsController> _logger;

        public TierBenefitsController(AppDbContext context, ILoyaltyService loyaltyService, ILogger<TierBenefitsController> logger)
        {
            _context = context;
            _loyaltyService = loyaltyService;
            _logger = logger;
        }

        /// <summary>
        /// Láº¥y danh sÃ¡ch quyá»n lá»£i cá»§a táº¥t cáº£ cÃ¡c háº¡ng
        /// </summary>
        [HttpGet("all-benefits")]
        public async Task<IActionResult> GetAllTierBenefits()
        {
            try
            {
                var tiers = await _context.CustomerTiers
                    .Where(t => t.IsActive)
                    .OrderBy(t => t.MinSpent)
                    .ToListAsync();

                var tierBenefits = tiers.Select(tier => new
                {
                    TierId = tier.TierId,
                    TierName = tier.TierName,
                    TierColor = tier.TierColor,
                    MinSpent = tier.MinSpent,
                    MinPoints = tier.MinPoints,
                    PointsMultiplier = tier.PointsMultiplier,
                    DiscountPercentage = tier.DiscountPercentage,
                    Description = tier.Description,
                    
                    // Chi tiáº¿t quyá»n lá»£i Ä‘iá»ƒm thÆ°á»Ÿng
                    BonusBenefits = GetTierBonusBenefits(tier.TierName),
                    SpecialBenefits = GetTierSpecialBenefits(tier.TierName)
                }).ToList();

                return Ok(new
                {
                    Success = true,
                    Data = tierBenefits,
                    Message = "Láº¥y thÃ´ng tin quyá»n lá»£i thÃ nh cÃ´ng"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tier benefits");
                return StatusCode(500, new { Success = false, Message = "Lá»—i há»‡ thá»‘ng khi láº¥y quyá»n lá»£i" });
            }
        }

        /// <summary>
        /// Láº¥y quyá»n lá»£i cá»§a má»™t háº¡ng cá»¥ thá»ƒ
        /// </summary>
        [HttpGet("{tierId}/benefits")]
        public async Task<IActionResult> GetTierBenefits(int tierId)
        {
            try
            {
                var tier = await _context.CustomerTiers
                    .Where(t => t.TierId == tierId && t.IsActive)
                    .FirstOrDefaultAsync();

                if (tier == null)
                {
                    return NotFound(new { Success = false, Message = "KhÃ´ng tÃ¬m tháº¥y háº¡ng khÃ¡ch hÃ ng" });
                }

                var customerCount = await _context.Customers.CountAsync(c => c.TierId == tierId);

                var benefits = new
                {
                    TierId = tier.TierId,
                    TierName = tier.TierName,
                    TierColor = tier.TierColor,
                    MinSpent = tier.MinSpent,
                    MinPoints = tier.MinPoints,
                    PointsMultiplier = tier.PointsMultiplier,
                    DiscountPercentage = tier.DiscountPercentage,
                    Description = tier.Description,
                    CustomerCount = customerCount,
                    
                    // Chi tiáº¿t quyá»n lá»£i
                    BonusBenefits = GetTierBonusBenefits(tier.TierName),
                    SpecialBenefits = GetTierSpecialBenefits(tier.TierName),
                    ExampleCalculation = GetExampleCalculation(tier.TierName)
                };

                return Ok(new
                {
                    Success = true,
                    Data = benefits,
                    Message = "Láº¥y thÃ´ng tin quyá»n lá»£i thÃ nh cÃ´ng"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tier benefits for tier {TierId}", tierId);
                return StatusCode(500, new { Success = false, Message = "Lá»—i há»‡ thá»‘ng khi láº¥y quyá»n lá»£i" });
            }
        }

        /// <summary>
        /// MÃ´ phá»ng tÃ­nh Ä‘iá»ƒm cho má»™t Ä‘Æ¡n hÃ ng theo háº¡ng cá»¥ thá»ƒ
        /// </summary>
        [HttpPost("simulate/{tierId}")]
        public async Task<IActionResult> SimulatePointsCalculation(int tierId, [FromBody] SimulateRequest request)
        {
            try
            {
                var tier = await _context.CustomerTiers
                    .Where(t => t.TierId == tierId && t.IsActive)
                    .FirstOrDefaultAsync();

                if (tier == null)
                {
                    return NotFound(new { Success = false, Message = "KhÃ´ng tÃ¬m tháº¥y háº¡ng khÃ¡ch hÃ ng" });
                }

                var settings = await _loyaltyService.GetLoyaltySettingsAsync();
                
                // TÃ­nh Ä‘iá»ƒm cÆ¡ báº£n
                var basePoints = (int)(request.OrderAmount / settings.PointsRate);
                
                // Ãp dá»¥ng há»‡ sá»‘ háº¡ng
                var tierPoints = (int)(basePoints * tier.PointsMultiplier);
                
                // TÃ­nh bonus points
                var bonusPoints = CalculateTierBonusPoints(tier.TierName, request.OrderAmount, basePoints);
                
                // Special day bonus náº¿u cÃ³
                var specialBonus = 0;
                if (request.IsBirthday)
                {
                    if (tier.TierName.ToLower().Contains("báº¡c") || tier.TierName.ToLower().Contains("silver"))
                        specialBonus = basePoints; // Gáº¥p Ä‘Ã´i
                    else if (tier.TierName.ToLower().Contains("vÃ ng") || tier.TierName.ToLower().Contains("gold"))
                        specialBonus = (int)(basePoints * 1.5m); // Gáº¥p 2.5 láº§n
                    else if (tier.TierName.ToLower().Contains("kim cÆ°Æ¡ng") || tier.TierName.ToLower().Contains("diamond") || tier.TierName.ToLower().Contains("platinum"))
                        specialBonus = basePoints * 2; // Gáº¥p 3 láº§n
                }

                if (request.IsHoliday)
                {
                    specialBonus += (int)(basePoints * 0.5m); // +50% trong ngÃ y lá»…
                }
                
                var totalPoints = tierPoints + bonusPoints + specialBonus;

                return Ok(new
                {
                    Success = true,
                    Data = new
                    {
                        OrderAmount = request.OrderAmount,
                        TierName = tier.TierName,
                        BasePoints = basePoints,
                        TierMultiplier = tier.PointsMultiplier,
                        TierPoints = tierPoints,
                        BonusPoints = bonusPoints,
                        SpecialDayBonus = specialBonus,
                        TotalPoints = totalPoints,
                        Breakdown = GetPointsBreakdown(tier.TierName, request.OrderAmount, basePoints, request.IsBirthday, request.IsHoliday)
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error simulating points for tier {TierId}", tierId);
                return StatusCode(500, new { Success = false, Message = "Lá»—i há»‡ thá»‘ng khi mÃ´ phá»ng" });
            }
        }

        private object GetTierBonusBenefits(string tierName)
        {
            var tierNameLower = tierName.ToLower();

            if (tierNameLower.Contains("báº¡c") || tierNameLower.Contains("silver"))
            {
                return new
                {
                    PointsBonus = "+20% Ä‘iá»ƒm thÆ°á»Ÿng",
                    FixedBonus = "50 Ä‘iá»ƒm cá»‘ Ä‘á»‹nh cho Ä‘Æ¡n hÃ ng >= 100k",
                    BirthdayBonus = "Gáº¥p Ä‘Ã´i Ä‘iá»ƒm trong tuáº§n sinh nháº­t",
                    Color = "#C0C0C0"
                };
            }
            else if (tierNameLower.Contains("vÃ ng") || tierNameLower.Contains("gold"))
            {
                return new
                {
                    PointsBonus = "+50% Ä‘iá»ƒm thÆ°á»Ÿng",
                    FixedBonus = "100 Ä‘iá»ƒm cá»‘ Ä‘á»‹nh cho Ä‘Æ¡n hÃ ng >= 200k",
                    MilestoneBonus = "200 Ä‘iá»ƒm thÃªm má»—i 500k",
                    BirthdayBonus = "Gáº¥p 2.5 láº§n Ä‘iá»ƒm trong tuáº§n sinh nháº­t",
                    Color = "#FFD700"
                };
            }
            else if (tierNameLower.Contains("kim cÆ°Æ¡ng") || tierNameLower.Contains("diamond") || tierNameLower.Contains("platinum"))
            {
                return new
                {
                    PointsBonus = "+100% Ä‘iá»ƒm thÆ°á»Ÿng (gáº¥p Ä‘Ã´i)",
                    FixedBonus = "300 Ä‘iá»ƒm cá»‘ Ä‘á»‹nh má»i Ä‘Æ¡n hÃ ng",
                    MilestoneBonus = "250 Ä‘iá»ƒm thÃªm má»—i 300k",
                    WeekendBonus = "Gáº¥p Ä‘Ã´i toÃ n bá»™ bonus cuá»‘i tuáº§n",
                    BirthdayBonus = "Gáº¥p 3 láº§n Ä‘iá»ƒm trong tuáº§n sinh nháº­t",
                    Color = "#B9F2FF"
                };
            }

            return new
            {
                PointsBonus = "KhÃ´ng cÃ³ bonus Ä‘áº·c biá»‡t",
                Color = "#808080"
            };
        }

        private object GetTierSpecialBenefits(string tierName)
        {
            var tierNameLower = tierName.ToLower();

            var commonBenefits = new List<string>
            {
                "TÃ­ch Ä‘iá»ƒm trÃªn má»i giao dá»‹ch",
                "Äá»•i Ä‘iá»ƒm thÃ nh tiá»n máº·t",
                "ThÃ´ng bÃ¡o Æ°u Ä‘Ã£i qua email/SMS"
            };

            if (tierNameLower.Contains("báº¡c") || tierNameLower.Contains("silver"))
            {
                return commonBenefits.Concat(new[]
                {
                    "Æ¯u tiÃªn há»— trá»£ khÃ¡ch hÃ ng",
                    "Giáº£m giÃ¡ sinh nháº­t Ä‘áº·c biá»‡t",
                    "ThÃ´ng bÃ¡o sá»›m vá» khuyáº¿n mÃ£i"
                }).ToArray();
            }
            else if (tierNameLower.Contains("vÃ ng") || tierNameLower.Contains("gold"))
            {
                return commonBenefits.Concat(new[]
                {
                    "Miá»…n phÃ­ giao hÃ ng toÃ n quá»‘c",
                    "TÆ° váº¥n cÃ¡ nhÃ¢n hÃ³a",
                    "Truy cáº­p sá»›m sáº£n pháº©m má»›i",
                    "QuÃ  táº·ng sinh nháº­t cao cáº¥p",
                    "HoÃ n tiá»n nhanh 24h"
                }).ToArray();
            }
            else if (tierNameLower.Contains("kim cÆ°Æ¡ng") || tierNameLower.Contains("diamond") || tierNameLower.Contains("platinum"))
            {
                return commonBenefits.Concat(new[]
                {
                    "Concierge service 24/7",
                    "Miá»…n phÃ­ giao hÃ ng express",
                    "Truy cáº­p VIP lounge",
                    "Personal shopper dedicated",
                    "Sá»± kiá»‡n Ä‘á»™c quyá»n vÃ  preview",
                    "QuÃ  táº·ng premium hÃ ng thÃ¡ng",
                    "Báº£o hÃ nh vÃ  báº£o dÆ°á»¡ng miá»…n phÃ­"
                }).ToArray();
            }

            return commonBenefits.ToArray();
        }

        private object GetExampleCalculation(string tierName)
        {
            return new
            {
                OrderAmount = 1000000,
                Examples = GetPointsBreakdown(tierName, 1000000, 1000, false, false)
            };
        }

        private object GetPointsBreakdown(string tierName, decimal orderAmount, int basePoints, bool isBirthday, bool isHoliday)
        {
            var breakdown = new List<object>();
            var tierNameLower = tierName.ToLower();

            breakdown.Add(new { Step = "Äiá»ƒm cÆ¡ báº£n", Amount = orderAmount, Points = basePoints, Description = $"{orderAmount:N0} VNÄ Ã· 1000 = {basePoints} Ä‘iá»ƒm" });

            if (tierNameLower.Contains("báº¡c") || tierNameLower.Contains("silver"))
            {
                var bonus = (int)(basePoints * 0.2m);
                var fixedBonus = orderAmount >= 100000 ? 50 : 0;
                breakdown.Add(new { Step = "Háº¡ng Báº¡c +20%", Points = bonus, Description = $"Bonus 20%: {basePoints} Ã— 0.2 = {bonus} Ä‘iá»ƒm" });
                if (fixedBonus > 0)
                    breakdown.Add(new { Step = "Bonus >= 100k", Points = fixedBonus, Description = "Cá»‘ Ä‘á»‹nh 50 Ä‘iá»ƒm cho Ä‘Æ¡n >= 100k" });
            }
            else if (tierNameLower.Contains("vÃ ng") || tierNameLower.Contains("gold"))
            {
                var bonus = (int)(basePoints * 0.5m);
                var fixedBonus = orderAmount >= 200000 ? 100 : 0;
                var milestoneBonus = (int)(orderAmount / 500000) * 200;
                
                breakdown.Add(new { Step = "Háº¡ng VÃ ng +50%", Points = bonus, Description = $"Bonus 50%: {basePoints} Ã— 0.5 = {bonus} Ä‘iá»ƒm" });
                if (fixedBonus > 0)
                    breakdown.Add(new { Step = "Bonus >= 200k", Points = fixedBonus, Description = "Cá»‘ Ä‘á»‹nh 100 Ä‘iá»ƒm cho Ä‘Æ¡n >= 200k" });
                if (milestoneBonus > 0)
                    breakdown.Add(new { Step = "Milestone", Points = milestoneBonus, Description = $"Má»—i 500k: {(int)(orderAmount / 500000)} Ã— 200 = {milestoneBonus} Ä‘iá»ƒm" });
            }
            else if (tierNameLower.Contains("kim cÆ°Æ¡ng") || tierNameLower.Contains("diamond") || tierNameLower.Contains("platinum"))
            {
                var bonus = basePoints; // 100%
                var fixedBonus = 300;
                var milestoneBonus = (int)(orderAmount / 300000) * 250;
                
                breakdown.Add(new { Step = "Háº¡ng Kim cÆ°Æ¡ng +100%", Points = bonus, Description = $"Bonus 100%: {basePoints} Ã— 1.0 = {bonus} Ä‘iá»ƒm" });
                breakdown.Add(new { Step = "Bonus cá»‘ Ä‘á»‹nh", Points = fixedBonus, Description = "Cá»‘ Ä‘á»‹nh 300 Ä‘iá»ƒm má»i Ä‘Æ¡n hÃ ng" });
                if (milestoneBonus > 0)
                    breakdown.Add(new { Step = "Milestone", Points = milestoneBonus, Description = $"Má»—i 300k: {(int)(orderAmount / 300000)} Ã— 250 = {milestoneBonus} Ä‘iá»ƒm" });
            }

            if (isBirthday)
            {
                var birthdayBonus = 0;
                if (tierNameLower.Contains("báº¡c") || tierNameLower.Contains("silver"))
                    birthdayBonus = basePoints;
                else if (tierNameLower.Contains("vÃ ng") || tierNameLower.Contains("gold"))
                    birthdayBonus = (int)(basePoints * 1.5m);
                else if (tierNameLower.Contains("kim cÆ°Æ¡ng") || tierNameLower.Contains("diamond") || tierNameLower.Contains("platinum"))
                    birthdayBonus = basePoints * 2;
                
                if (birthdayBonus > 0)
                    breakdown.Add(new { Step = "Sinh nháº­t", Points = birthdayBonus, Description = "Bonus tuáº§n sinh nháº­t" });
            }

            if (isHoliday)
            {
                var holidayBonus = (int)(basePoints * 0.5m);
                breakdown.Add(new { Step = "NgÃ y lá»…", Points = holidayBonus, Description = "Bonus 50% ngÃ y lá»…" });
            }

            return breakdown;
        }

        private int CalculateTierBonusPoints(string tierName, decimal orderAmount, int basePoints)
        {
            var bonusPoints = 0;
            var tierNameLower = tierName.ToLower();

            if (tierNameLower.Contains("báº¡c") || tierNameLower.Contains("silver"))
            {
                bonusPoints = (int)(basePoints * 0.2m);
                if (orderAmount >= 100000) bonusPoints += 50;
            }
            else if (tierNameLower.Contains("vÃ ng") || tierNameLower.Contains("gold"))
            {
                bonusPoints = (int)(basePoints * 0.5m);
                if (orderAmount >= 200000) bonusPoints += 100;
                var milestoneBonus = (int)(orderAmount / 500000) * 200;
                bonusPoints += milestoneBonus;
            }
            else if (tierNameLower.Contains("kim cÆ°Æ¡ng") || tierNameLower.Contains("diamond") || tierNameLower.Contains("platinum"))
            {
                bonusPoints = basePoints; // 100%
                bonusPoints += 300;
                var milestoneBonus = (int)(orderAmount / 300000) * 250;
                bonusPoints += milestoneBonus;
            }

            return bonusPoints;
        }
    }

    public class SimulateRequest
    {
        public decimal OrderAmount { get; set; }
        public bool IsBirthday { get; set; } = false;
        public bool IsHoliday { get; set; } = false;
    }
}
