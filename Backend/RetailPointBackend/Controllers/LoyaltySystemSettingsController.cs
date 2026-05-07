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
    public class LoyaltySystemSettingsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILoyaltyService _loyaltyService;
        private readonly ILogger<LoyaltySystemSettingsController> _logger;

        public LoyaltySystemSettingsController(AppDbContext context, ILoyaltyService loyaltyService, ILogger<LoyaltySystemSettingsController> logger)
        {
            _context = context;
            _loyaltyService = loyaltyService;
            _logger = logger;
        }

        /// <summary>
        /// Láº¥y cÃ i Ä‘áº·t tÃ­ch Ä‘iá»ƒm thÆ°á»Ÿng hiá»‡n táº¡i
        /// </summary>
        [HttpGet("settings")]
        public async Task<ActionResult<LoyaltySettings>> GetLoyaltySettings()
        {
            try
            {
                var settings = await _context.LoyaltySettings.FirstOrDefaultAsync();
                
                if (settings == null)
                {
                    // Táº¡o settings máº·c Ä‘á»‹nh náº¿u chÆ°a cÃ³
                    settings = new LoyaltySettings
                    {
                        IsPointsEnabled = true,
                        PointsRate = 1000,
                        IsRedemptionEnabled = true,
                        RedemptionRate = 1000,
                        MinOrderAmount = 50000,
                        MaxRedemptionPercentage = 50,
                        MaxPointsPerOrder = 0,
                        PointsExpirationDays = 365,
                        Notes = "CÃ i Ä‘áº·t máº·c Ä‘á»‹nh Ä‘Æ°á»£c táº¡o tá»± Ä‘á»™ng"
                    };
                    
                    _context.LoyaltySettings.Add(settings);
                    await _context.SaveChangesAsync();
                }

                return Ok(settings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lá»—i khi láº¥y cÃ i Ä‘áº·t loyalty");
                return StatusCode(500, new { message = "Lá»—i server khi láº¥y cÃ i Ä‘áº·t", error = ex.Message });
            }
        }

        /// <summary>
        /// Cáº­p nháº­t cÃ i Ä‘áº·t tÃ­ch Ä‘iá»ƒm thÆ°á»Ÿng
        /// </summary>
        [HttpPut("settings")]
        public async Task<ActionResult<LoyaltySettings>> UpdateLoyaltySettings([FromBody] LoyaltySettingsUpdateRequest request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest(new { message = "Dá»¯ liá»‡u cÃ i Ä‘áº·t khÃ´ng há»£p lá»‡" });
                }

                // Validate input
                var validationResult = ValidateSettings(request);
                if (!validationResult.IsValid)
                {
                    return BadRequest(new { message = "Dá»¯ liá»‡u khÃ´ng há»£p lá»‡", errors = validationResult.Errors });
                }

                var settings = await _context.LoyaltySettings.FirstOrDefaultAsync();
                
                if (settings == null)
                {
                    // Táº¡o má»›i náº¿u chÆ°a cÃ³
                    settings = new LoyaltySettings();
                    _context.LoyaltySettings.Add(settings);
                }

                // Cáº­p nháº­t cÃ¡c giÃ¡ trá»‹
                settings.IsPointsEnabled = request.IsPointsEnabled;
                settings.PointsRate = request.PointsRate;
                settings.IsRedemptionEnabled = request.IsRedemptionEnabled;
                settings.RedemptionRate = request.RedemptionRate;
                settings.MinOrderAmount = request.MinOrderAmount;
                settings.MaxRedemptionPercentage = request.MaxRedemptionPercentage;
                settings.MaxPointsPerOrder = request.MaxPointsPerOrder;
                settings.PointsExpirationDays = request.PointsExpirationDays;
                settings.Notes = request.Notes;
                settings.UpdatedAt = DateTime.UtcNow;
                settings.UpdatedBy = User?.Identity?.Name ?? "System";

                await _context.SaveChangesAsync();

                _logger.LogInformation("CÃ i Ä‘áº·t loyalty Ä‘Ã£ Ä‘Æ°á»£c cáº­p nháº­t bá»Ÿi {User}", settings.UpdatedBy);

                return Ok(settings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lá»—i khi cáº­p nháº­t cÃ i Ä‘áº·t loyalty");
                return StatusCode(500, new { message = "Lá»—i server khi cáº­p nháº­t cÃ i Ä‘áº·t", error = ex.Message });
            }
        }

        /// <summary>
        /// Reset vá» cÃ i Ä‘áº·t máº·c Ä‘á»‹nh
        /// </summary>
        [HttpPost("reset-defaults")]
        public async Task<ActionResult<LoyaltySettings>> ResetToDefaults()
        {
            try
            {
                var settings = await _context.LoyaltySettings.FirstOrDefaultAsync();
                
                if (settings == null)
                {
                    settings = new LoyaltySettings();
                    _context.LoyaltySettings.Add(settings);
                }

                // Reset vá» giÃ¡ trá»‹ máº·c Ä‘á»‹nh
                settings.IsPointsEnabled = true;
                settings.PointsRate = 1000;
                settings.IsRedemptionEnabled = true;
                settings.RedemptionRate = 1000;
                settings.MinOrderAmount = 50000;
                settings.MaxRedemptionPercentage = 50;
                settings.MaxPointsPerOrder = 0;
                settings.PointsExpirationDays = 365;
                settings.Notes = "ÄÃ£ reset vá» cÃ i Ä‘áº·t máº·c Ä‘á»‹nh";
                settings.UpdatedAt = DateTime.UtcNow;
                settings.UpdatedBy = User?.Identity?.Name ?? "System";

                await _context.SaveChangesAsync();

                _logger.LogInformation("CÃ i Ä‘áº·t loyalty Ä‘Ã£ Ä‘Æ°á»£c reset vá» máº·c Ä‘á»‹nh bá»Ÿi {User}", settings.UpdatedBy);

                return Ok(settings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lá»—i khi reset cÃ i Ä‘áº·t loyalty");
                return StatusCode(500, new { message = "Lá»—i server khi reset cÃ i Ä‘áº·t", error = ex.Message });
            }
        }

        /// <summary>
        /// Kiá»ƒm tra tráº¡ng thÃ¡i tÃ­ch Ä‘iá»ƒm cÃ³ hoáº¡t Ä‘á»™ng hay khÃ´ng
        /// </summary>
        [HttpGet("status")]
        public async Task<ActionResult<object>> GetLoyaltyStatus()
        {
            try
            {
                var settings = await _context.LoyaltySettings.FirstOrDefaultAsync();
                var totalCustomers = await _context.Customers.CountAsync();
                var activeCustomersWithPoints = await _context.Customers
                    .CountAsync(c => c.LoyaltyPoints > 0);

                var loyaltyTransactions = await _context.LoyaltyTransactions
                    .Where(lt => lt.ProcessedAt >= DateTime.UtcNow.AddDays(-30))
                    .CountAsync();

                return Ok(new
                {
                    IsEnabled = settings?.IsPointsEnabled ?? false,
                    IsRedemptionEnabled = settings?.IsRedemptionEnabled ?? false,
                    TotalCustomers = totalCustomers,
                    ActiveCustomersWithPoints = activeCustomersWithPoints,
                    TransactionsLast30Days = loyaltyTransactions,
                    CurrentSettings = settings
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lá»—i khi láº¥y tráº¡ng thÃ¡i loyalty");
                return StatusCode(500, new { message = "Lá»—i server", error = ex.Message });
            }
        }

        /// <summary>
        /// Simulate tÃ­ch Ä‘iá»ƒm cho má»™t Ä‘Æ¡n hÃ ng
        /// </summary>
        [HttpPost("simulate-points")]
        public ActionResult<object> SimulatePointsCalculation([FromBody] SimulatePointsRequest request)
        {
            try
            {
                if (request?.OrderAmount <= 0)
                {
                    return BadRequest(new { message = "Sá»‘ tiá»n Ä‘Æ¡n hÃ ng pháº£i > 0" });
                }

                var settings = _context.LoyaltySettings.FirstOrDefault();
                if (settings == null || !settings.IsPointsEnabled)
                {
                    return Ok(new
                    {
                        OrderAmount = request.OrderAmount,
                        EarnedPoints = 0,
                        Message = "Há»‡ thá»‘ng tÃ­ch Ä‘iá»ƒm Ä‘ang táº¯t"
                    });
                }

                // Kiá»ƒm tra Ä‘Æ¡n hÃ ng tá»‘i thiá»ƒu
                if (request.OrderAmount < settings.MinOrderAmount)
                {
                    return Ok(new
                    {
                        OrderAmount = request.OrderAmount,
                        EarnedPoints = 0,
                        Message = $"ÄÆ¡n hÃ ng pháº£i >= {settings.MinOrderAmount:N0} VNÄ Ä‘á»ƒ Ä‘Æ°á»£c tÃ­ch Ä‘iá»ƒm"
                    });
                }

                // TÃ­nh Ä‘iá»ƒm
                var basePoints = (int)(request.OrderAmount / settings.PointsRate);
                
                // Ãp dá»¥ng giá»›i háº¡n Ä‘iá»ƒm tá»‘i Ä‘a/Ä‘Æ¡n hÃ ng
                var finalPoints = settings.MaxPointsPerOrder > 0 
                    ? Math.Min(basePoints, settings.MaxPointsPerOrder)
                    : basePoints;

                return Ok(new
                {
                    OrderAmount = request.OrderAmount,
                    EarnedPoints = finalPoints,
                    BasePoints = basePoints,
                    PointsRate = settings.PointsRate,
                    MaxPointsApplied = settings.MaxPointsPerOrder > 0 && basePoints > settings.MaxPointsPerOrder,
                    Message = "TÃ­nh toÃ¡n thÃ nh cÃ´ng"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lá»—i khi simulate tÃ­ch Ä‘iá»ƒm");
                return StatusCode(500, new { message = "Lá»—i server", error = ex.Message });
            }
        }

        /// <summary>
        /// Validate dá»¯ liá»‡u cÃ i Ä‘áº·t
        /// </summary>
        private ValidationResult ValidateSettings(LoyaltySettingsUpdateRequest request)
        {
            var result = new ValidationResult { IsValid = true, Errors = new List<string>() };

            if (request.PointsRate <= 0)
            {
                result.Errors.Add("Tá»· lá»‡ tÃ­ch Ä‘iá»ƒm pháº£i > 0");
            }

            if (request.RedemptionRate <= 0)
            {
                result.Errors.Add("GiÃ¡ trá»‹ Ä‘iá»ƒm pháº£i > 0");
            }

            if (request.MinOrderAmount < 0)
            {
                result.Errors.Add("ÄÆ¡n hÃ ng tá»‘i thiá»ƒu khÃ´ng Ä‘Æ°á»£c Ã¢m");
            }

            if (request.MaxRedemptionPercentage < 0 || request.MaxRedemptionPercentage > 100)
            {
                result.Errors.Add("Pháº§n trÄƒm Ä‘á»•i Ä‘iá»ƒm tá»‘i Ä‘a pháº£i tá»« 0-100");
            }

            if (request.MaxPointsPerOrder < 0)
            {
                result.Errors.Add("Äiá»ƒm tá»‘i Ä‘a/Ä‘Æ¡n hÃ ng khÃ´ng Ä‘Æ°á»£c Ã¢m");
            }

            if (request.PointsExpirationDays < 0)
            {
                result.Errors.Add("Sá»‘ ngÃ y háº¿t háº¡n Ä‘iá»ƒm khÃ´ng Ä‘Æ°á»£c Ã¢m");
            }

            result.IsValid = !result.Errors.Any();
            return result;
        }
    }

    /// <summary>
    /// Request model cho cáº­p nháº­t cÃ i Ä‘áº·t
    /// </summary>
    public class LoyaltySettingsUpdateRequest
    {
        public bool IsPointsEnabled { get; set; }
        public decimal PointsRate { get; set; }
        public bool IsRedemptionEnabled { get; set; }
        public decimal RedemptionRate { get; set; }
        public decimal MinOrderAmount { get; set; }
        public int MaxRedemptionPercentage { get; set; }
        public int MaxPointsPerOrder { get; set; }
        public int PointsExpirationDays { get; set; }
        public string? Notes { get; set; }
    }

    /// <summary>
    /// Request model cho simulate tÃ­ch Ä‘iá»ƒm
    /// </summary>
    public class SimulatePointsRequest
    {
        public decimal OrderAmount { get; set; }
    }

    /// <summary>
    /// Káº¿t quáº£ validation
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
    }
}
