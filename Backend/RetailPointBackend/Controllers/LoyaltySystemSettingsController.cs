using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RetailPointBackend.Models;
using RetailPointBackend.Services;

namespace RetailPointBackend.Controllers
{
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
        /// Lấy cài đặt tích điểm thưởng hiện tại
        /// </summary>
        [HttpGet("settings")]
        public async Task<ActionResult<LoyaltySettings>> GetLoyaltySettings()
        {
            try
            {
                var settings = await _context.LoyaltySettings.FirstOrDefaultAsync();
                
                if (settings == null)
                {
                    // Tạo settings mặc định nếu chưa có
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
                        Notes = "Cài đặt mặc định được tạo tự động"
                    };
                    
                    _context.LoyaltySettings.Add(settings);
                    await _context.SaveChangesAsync();
                }

                return Ok(settings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy cài đặt loyalty");
                return StatusCode(500, new { message = "Lỗi server khi lấy cài đặt", error = ex.Message });
            }
        }

        /// <summary>
        /// Cập nhật cài đặt tích điểm thưởng
        /// </summary>
        [HttpPut("settings")]
        public async Task<ActionResult<LoyaltySettings>> UpdateLoyaltySettings([FromBody] LoyaltySettingsUpdateRequest request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest(new { message = "Dữ liệu cài đặt không hợp lệ" });
                }

                // Validate input
                var validationResult = ValidateSettings(request);
                if (!validationResult.IsValid)
                {
                    return BadRequest(new { message = "Dữ liệu không hợp lệ", errors = validationResult.Errors });
                }

                var settings = await _context.LoyaltySettings.FirstOrDefaultAsync();
                
                if (settings == null)
                {
                    // Tạo mới nếu chưa có
                    settings = new LoyaltySettings();
                    _context.LoyaltySettings.Add(settings);
                }

                // Cập nhật các giá trị
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

                _logger.LogInformation("Cài đặt loyalty đã được cập nhật bởi {User}", settings.UpdatedBy);

                return Ok(settings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật cài đặt loyalty");
                return StatusCode(500, new { message = "Lỗi server khi cập nhật cài đặt", error = ex.Message });
            }
        }

        /// <summary>
        /// Reset về cài đặt mặc định
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

                // Reset về giá trị mặc định
                settings.IsPointsEnabled = true;
                settings.PointsRate = 1000;
                settings.IsRedemptionEnabled = true;
                settings.RedemptionRate = 1000;
                settings.MinOrderAmount = 50000;
                settings.MaxRedemptionPercentage = 50;
                settings.MaxPointsPerOrder = 0;
                settings.PointsExpirationDays = 365;
                settings.Notes = "Đã reset về cài đặt mặc định";
                settings.UpdatedAt = DateTime.UtcNow;
                settings.UpdatedBy = User?.Identity?.Name ?? "System";

                await _context.SaveChangesAsync();

                _logger.LogInformation("Cài đặt loyalty đã được reset về mặc định bởi {User}", settings.UpdatedBy);

                return Ok(settings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi reset cài đặt loyalty");
                return StatusCode(500, new { message = "Lỗi server khi reset cài đặt", error = ex.Message });
            }
        }

        /// <summary>
        /// Kiểm tra trạng thái tích điểm có hoạt động hay không
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
                _logger.LogError(ex, "Lỗi khi lấy trạng thái loyalty");
                return StatusCode(500, new { message = "Lỗi server", error = ex.Message });
            }
        }

        /// <summary>
        /// Simulate tích điểm cho một đơn hàng
        /// </summary>
        [HttpPost("simulate-points")]
        public ActionResult<object> SimulatePointsCalculation([FromBody] SimulatePointsRequest request)
        {
            try
            {
                if (request?.OrderAmount <= 0)
                {
                    return BadRequest(new { message = "Số tiền đơn hàng phải > 0" });
                }

                var settings = _context.LoyaltySettings.FirstOrDefault();
                if (settings == null || !settings.IsPointsEnabled)
                {
                    return Ok(new
                    {
                        OrderAmount = request.OrderAmount,
                        EarnedPoints = 0,
                        Message = "Hệ thống tích điểm đang tắt"
                    });
                }

                // Kiểm tra đơn hàng tối thiểu
                if (request.OrderAmount < settings.MinOrderAmount)
                {
                    return Ok(new
                    {
                        OrderAmount = request.OrderAmount,
                        EarnedPoints = 0,
                        Message = $"Đơn hàng phải >= {settings.MinOrderAmount:N0} VNĐ để được tích điểm"
                    });
                }

                // Tính điểm
                var basePoints = (int)(request.OrderAmount / settings.PointsRate);
                
                // Áp dụng giới hạn điểm tối đa/đơn hàng
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
                    Message = "Tính toán thành công"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi simulate tích điểm");
                return StatusCode(500, new { message = "Lỗi server", error = ex.Message });
            }
        }

        /// <summary>
        /// Validate dữ liệu cài đặt
        /// </summary>
        private ValidationResult ValidateSettings(LoyaltySettingsUpdateRequest request)
        {
            var result = new ValidationResult { IsValid = true, Errors = new List<string>() };

            if (request.PointsRate <= 0)
            {
                result.Errors.Add("Tỷ lệ tích điểm phải > 0");
            }

            if (request.RedemptionRate <= 0)
            {
                result.Errors.Add("Giá trị điểm phải > 0");
            }

            if (request.MinOrderAmount < 0)
            {
                result.Errors.Add("Đơn hàng tối thiểu không được âm");
            }

            if (request.MaxRedemptionPercentage < 0 || request.MaxRedemptionPercentage > 100)
            {
                result.Errors.Add("Phần trăm đổi điểm tối đa phải từ 0-100");
            }

            if (request.MaxPointsPerOrder < 0)
            {
                result.Errors.Add("Điểm tối đa/đơn hàng không được âm");
            }

            if (request.PointsExpirationDays < 0)
            {
                result.Errors.Add("Số ngày hết hạn điểm không được âm");
            }

            result.IsValid = !result.Errors.Any();
            return result;
        }
    }

    /// <summary>
    /// Request model cho cập nhật cài đặt
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
    /// Request model cho simulate tích điểm
    /// </summary>
    public class SimulatePointsRequest
    {
        public decimal OrderAmount { get; set; }
    }

    /// <summary>
    /// Kết quả validation
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
    }
}