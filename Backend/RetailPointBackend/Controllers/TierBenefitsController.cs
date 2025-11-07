using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RetailPointBackend.Models;
using RetailPointBackend.Services;

namespace RetailPointBackend.Controllers
{
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
        /// Lấy danh sách quyền lợi của tất cả các hạng
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
                    
                    // Chi tiết quyền lợi điểm thưởng
                    BonusBenefits = GetTierBonusBenefits(tier.TierName),
                    SpecialBenefits = GetTierSpecialBenefits(tier.TierName)
                }).ToList();

                return Ok(new
                {
                    Success = true,
                    Data = tierBenefits,
                    Message = "Lấy thông tin quyền lợi thành công"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tier benefits");
                return StatusCode(500, new { Success = false, Message = "Lỗi hệ thống khi lấy quyền lợi" });
            }
        }

        /// <summary>
        /// Lấy quyền lợi của một hạng cụ thể
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
                    return NotFound(new { Success = false, Message = "Không tìm thấy hạng khách hàng" });
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
                    
                    // Chi tiết quyền lợi
                    BonusBenefits = GetTierBonusBenefits(tier.TierName),
                    SpecialBenefits = GetTierSpecialBenefits(tier.TierName),
                    ExampleCalculation = GetExampleCalculation(tier.TierName)
                };

                return Ok(new
                {
                    Success = true,
                    Data = benefits,
                    Message = "Lấy thông tin quyền lợi thành công"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tier benefits for tier {TierId}", tierId);
                return StatusCode(500, new { Success = false, Message = "Lỗi hệ thống khi lấy quyền lợi" });
            }
        }

        /// <summary>
        /// Mô phỏng tính điểm cho một đơn hàng theo hạng cụ thể
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
                    return NotFound(new { Success = false, Message = "Không tìm thấy hạng khách hàng" });
                }

                var settings = await _loyaltyService.GetLoyaltySettingsAsync();
                
                // Tính điểm cơ bản
                var basePoints = (int)(request.OrderAmount / settings.PointsRate);
                
                // Áp dụng hệ số hạng
                var tierPoints = (int)(basePoints * tier.PointsMultiplier);
                
                // Tính bonus points
                var bonusPoints = CalculateTierBonusPoints(tier.TierName, request.OrderAmount, basePoints);
                
                // Special day bonus nếu có
                var specialBonus = 0;
                if (request.IsBirthday)
                {
                    if (tier.TierName.ToLower().Contains("bạc") || tier.TierName.ToLower().Contains("silver"))
                        specialBonus = basePoints; // Gấp đôi
                    else if (tier.TierName.ToLower().Contains("vàng") || tier.TierName.ToLower().Contains("gold"))
                        specialBonus = (int)(basePoints * 1.5m); // Gấp 2.5 lần
                    else if (tier.TierName.ToLower().Contains("kim cương") || tier.TierName.ToLower().Contains("diamond") || tier.TierName.ToLower().Contains("platinum"))
                        specialBonus = basePoints * 2; // Gấp 3 lần
                }

                if (request.IsHoliday)
                {
                    specialBonus += (int)(basePoints * 0.5m); // +50% trong ngày lễ
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
                return StatusCode(500, new { Success = false, Message = "Lỗi hệ thống khi mô phỏng" });
            }
        }

        private object GetTierBonusBenefits(string tierName)
        {
            var tierNameLower = tierName.ToLower();

            if (tierNameLower.Contains("bạc") || tierNameLower.Contains("silver"))
            {
                return new
                {
                    PointsBonus = "+20% điểm thưởng",
                    FixedBonus = "50 điểm cố định cho đơn hàng >= 100k",
                    BirthdayBonus = "Gấp đôi điểm trong tuần sinh nhật",
                    Color = "#C0C0C0"
                };
            }
            else if (tierNameLower.Contains("vàng") || tierNameLower.Contains("gold"))
            {
                return new
                {
                    PointsBonus = "+50% điểm thưởng",
                    FixedBonus = "100 điểm cố định cho đơn hàng >= 200k",
                    MilestoneBonus = "200 điểm thêm mỗi 500k",
                    BirthdayBonus = "Gấp 2.5 lần điểm trong tuần sinh nhật",
                    Color = "#FFD700"
                };
            }
            else if (tierNameLower.Contains("kim cương") || tierNameLower.Contains("diamond") || tierNameLower.Contains("platinum"))
            {
                return new
                {
                    PointsBonus = "+100% điểm thưởng (gấp đôi)",
                    FixedBonus = "300 điểm cố định mọi đơn hàng",
                    MilestoneBonus = "250 điểm thêm mỗi 300k",
                    WeekendBonus = "Gấp đôi toàn bộ bonus cuối tuần",
                    BirthdayBonus = "Gấp 3 lần điểm trong tuần sinh nhật",
                    Color = "#B9F2FF"
                };
            }

            return new
            {
                PointsBonus = "Không có bonus đặc biệt",
                Color = "#808080"
            };
        }

        private object GetTierSpecialBenefits(string tierName)
        {
            var tierNameLower = tierName.ToLower();

            var commonBenefits = new List<string>
            {
                "Tích điểm trên mọi giao dịch",
                "Đổi điểm thành tiền mặt",
                "Thông báo ưu đãi qua email/SMS"
            };

            if (tierNameLower.Contains("bạc") || tierNameLower.Contains("silver"))
            {
                return commonBenefits.Concat(new[]
                {
                    "Ưu tiên hỗ trợ khách hàng",
                    "Giảm giá sinh nhật đặc biệt",
                    "Thông báo sớm về khuyến mãi"
                }).ToArray();
            }
            else if (tierNameLower.Contains("vàng") || tierNameLower.Contains("gold"))
            {
                return commonBenefits.Concat(new[]
                {
                    "Miễn phí giao hàng toàn quốc",
                    "Tư vấn cá nhân hóa",
                    "Truy cập sớm sản phẩm mới",
                    "Quà tặng sinh nhật cao cấp",
                    "Hoàn tiền nhanh 24h"
                }).ToArray();
            }
            else if (tierNameLower.Contains("kim cương") || tierNameLower.Contains("diamond") || tierNameLower.Contains("platinum"))
            {
                return commonBenefits.Concat(new[]
                {
                    "Concierge service 24/7",
                    "Miễn phí giao hàng express",
                    "Truy cập VIP lounge",
                    "Personal shopper dedicated",
                    "Sự kiện độc quyền và preview",
                    "Quà tặng premium hàng tháng",
                    "Bảo hành và bảo dưỡng miễn phí"
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

            breakdown.Add(new { Step = "Điểm cơ bản", Amount = orderAmount, Points = basePoints, Description = $"{orderAmount:N0} VNĐ ÷ 1000 = {basePoints} điểm" });

            if (tierNameLower.Contains("bạc") || tierNameLower.Contains("silver"))
            {
                var bonus = (int)(basePoints * 0.2m);
                var fixedBonus = orderAmount >= 100000 ? 50 : 0;
                breakdown.Add(new { Step = "Hạng Bạc +20%", Points = bonus, Description = $"Bonus 20%: {basePoints} × 0.2 = {bonus} điểm" });
                if (fixedBonus > 0)
                    breakdown.Add(new { Step = "Bonus >= 100k", Points = fixedBonus, Description = "Cố định 50 điểm cho đơn >= 100k" });
            }
            else if (tierNameLower.Contains("vàng") || tierNameLower.Contains("gold"))
            {
                var bonus = (int)(basePoints * 0.5m);
                var fixedBonus = orderAmount >= 200000 ? 100 : 0;
                var milestoneBonus = (int)(orderAmount / 500000) * 200;
                
                breakdown.Add(new { Step = "Hạng Vàng +50%", Points = bonus, Description = $"Bonus 50%: {basePoints} × 0.5 = {bonus} điểm" });
                if (fixedBonus > 0)
                    breakdown.Add(new { Step = "Bonus >= 200k", Points = fixedBonus, Description = "Cố định 100 điểm cho đơn >= 200k" });
                if (milestoneBonus > 0)
                    breakdown.Add(new { Step = "Milestone", Points = milestoneBonus, Description = $"Mỗi 500k: {(int)(orderAmount / 500000)} × 200 = {milestoneBonus} điểm" });
            }
            else if (tierNameLower.Contains("kim cương") || tierNameLower.Contains("diamond") || tierNameLower.Contains("platinum"))
            {
                var bonus = basePoints; // 100%
                var fixedBonus = 300;
                var milestoneBonus = (int)(orderAmount / 300000) * 250;
                
                breakdown.Add(new { Step = "Hạng Kim cương +100%", Points = bonus, Description = $"Bonus 100%: {basePoints} × 1.0 = {bonus} điểm" });
                breakdown.Add(new { Step = "Bonus cố định", Points = fixedBonus, Description = "Cố định 300 điểm mọi đơn hàng" });
                if (milestoneBonus > 0)
                    breakdown.Add(new { Step = "Milestone", Points = milestoneBonus, Description = $"Mỗi 300k: {(int)(orderAmount / 300000)} × 250 = {milestoneBonus} điểm" });
            }

            if (isBirthday)
            {
                var birthdayBonus = 0;
                if (tierNameLower.Contains("bạc") || tierNameLower.Contains("silver"))
                    birthdayBonus = basePoints;
                else if (tierNameLower.Contains("vàng") || tierNameLower.Contains("gold"))
                    birthdayBonus = (int)(basePoints * 1.5m);
                else if (tierNameLower.Contains("kim cương") || tierNameLower.Contains("diamond") || tierNameLower.Contains("platinum"))
                    birthdayBonus = basePoints * 2;
                
                if (birthdayBonus > 0)
                    breakdown.Add(new { Step = "Sinh nhật", Points = birthdayBonus, Description = "Bonus tuần sinh nhật" });
            }

            if (isHoliday)
            {
                var holidayBonus = (int)(basePoints * 0.5m);
                breakdown.Add(new { Step = "Ngày lễ", Points = holidayBonus, Description = "Bonus 50% ngày lễ" });
            }

            return breakdown;
        }

        private int CalculateTierBonusPoints(string tierName, decimal orderAmount, int basePoints)
        {
            var bonusPoints = 0;
            var tierNameLower = tierName.ToLower();

            if (tierNameLower.Contains("bạc") || tierNameLower.Contains("silver"))
            {
                bonusPoints = (int)(basePoints * 0.2m);
                if (orderAmount >= 100000) bonusPoints += 50;
            }
            else if (tierNameLower.Contains("vàng") || tierNameLower.Contains("gold"))
            {
                bonusPoints = (int)(basePoints * 0.5m);
                if (orderAmount >= 200000) bonusPoints += 100;
                var milestoneBonus = (int)(orderAmount / 500000) * 200;
                bonusPoints += milestoneBonus;
            }
            else if (tierNameLower.Contains("kim cương") || tierNameLower.Contains("diamond") || tierNameLower.Contains("platinum"))
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