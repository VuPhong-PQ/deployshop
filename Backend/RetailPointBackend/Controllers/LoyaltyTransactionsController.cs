using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RetailPointBackend.Models;

namespace RetailPointBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LoyaltyTransactionsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public LoyaltyTransactionsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/LoyaltyTransactions/customer/5
        [HttpGet("customer/{customerId}")]
        public async Task<ActionResult> GetCustomerTransactions(int customerId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                var transactions = await _context.LoyaltyTransactions
                    .Where(t => t.CustomerId == customerId)
                    .Include(t => t.Order)
                    .Include(t => t.ProcessedByStaff)
                    .OrderByDescending(t => t.ProcessedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var totalCount = await _context.LoyaltyTransactions
                    .CountAsync(t => t.CustomerId == customerId);

                return Ok(new
                {
                    transactions = transactions,
                    totalCount = totalCount,
                    page = page,
                    pageSize = pageSize,
                    totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi server", error = ex.Message });
            }
        }

        // POST: api/LoyaltyTransactions/process-order-points
        [HttpPost("process-order-points")]
        public async Task<ActionResult> ProcessOrderPoints([FromBody] ProcessOrderPointsRequest request)
        {
            try
            {
                var order = await _context.Orders
                    .Include(o => o.Customer)
                    .ThenInclude(c => c!.CustomerTier)
                    .FirstOrDefaultAsync(o => o.OrderId == request.OrderId);

                if (order == null)
                {
                    return NotFound(new { message = "Không tìm thấy đơn hàng" });
                }

                if (order.CustomerId == null)
                {
                    return BadRequest(new { message = "Đơn hàng không có thông tin khách hàng" });
                }

                var config = await _context.LoyaltyConfigs.FirstOrDefaultAsync();
                if (config == null || !config.IsEnabled)
                {
                    return BadRequest(new { message = "Hệ thống tích điểm chưa được kích hoạt" });
                }

                // Kiểm tra đã tích điểm cho đơn hàng này chưa
                var existingTransaction = await _context.LoyaltyTransactions
                    .FirstOrDefaultAsync(t => t.OrderId == request.OrderId && t.TransactionType == LoyaltyTransactionType.EARN);

                if (existingTransaction != null)
                {
                    return BadRequest(new { message = "Đơn hàng này đã được tích điểm" });
                }

                // Tính điểm
                var points = await CalculateOrderPoints(order, config);
                if (points <= 0)
                {
                    return Ok(new { message = "Đơn hàng không đủ điều kiện tích điểm", points = 0 });
                }

                // Cập nhật điểm khách hàng
                order.Customer!.LoyaltyPoints += points;

                // Tạo transaction
                var transaction = new LoyaltyTransaction
                {
                    CustomerId = order.CustomerId.Value,
                    OrderId = order.OrderId,
                    TransactionType = LoyaltyTransactionType.EARN,
                    Points = points,
                    PointsBalance = order.Customer.LoyaltyPoints,
                    Reason = $"Tích điểm từ đơn hàng #{order.OrderNumber}",
                    ExpiryDate = DateTime.Now.AddDays(config.PointExpiryDays),
                    ProcessedAt = DateTime.Now,
                    ProcessedBy = request.StaffId
                };

                _context.LoyaltyTransactions.Add(transaction);

                // Kiểm tra và cập nhật cấp độ khách hàng
                await UpdateCustomerTier(order.Customer);

                await _context.SaveChangesAsync();

                return Ok(new 
                { 
                    message = "Tích điểm thành công",
                    points = points,
                    newBalance = order.Customer.LoyaltyPoints,
                    transactionId = transaction.TransactionId
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi server", error = ex.Message });
            }
        }

        // POST: api/LoyaltyTransactions/redeem-points
        [HttpPost("redeem-points")]
        public async Task<ActionResult> RedeemPoints([FromBody] RedeemPointsRequest request)
        {
            try
            {
                var customer = await _context.Customers.FindAsync(request.CustomerId);
                if (customer == null)
                {
                    return NotFound(new { message = "Không tìm thấy khách hàng" });
                }

                var config = await _context.LoyaltyConfigs.FirstOrDefaultAsync();
                if (config == null || !config.AllowPointRedemption)
                {
                    return BadRequest(new { message = "Hệ thống không cho phép đổi điểm" });
                }

                if (customer.LoyaltyPoints < request.PointsToRedeem)
                {
                    return BadRequest(new { message = "Không đủ điểm để thực hiện giao dịch" });
                }

                // Tính giá trị quy đổi
                var redeemValue = request.PointsToRedeem * config.PointValue / 100;

                // Cập nhật điểm khách hàng
                customer.LoyaltyPoints -= request.PointsToRedeem;

                // Tạo transaction
                var transaction = new LoyaltyTransaction
                {
                    CustomerId = request.CustomerId,
                    OrderId = request.OrderId,
                    TransactionType = LoyaltyTransactionType.REDEEM,
                    Points = -request.PointsToRedeem,
                    PointsBalance = customer.LoyaltyPoints,
                    Reason = $"Đổi {request.PointsToRedeem} điểm = {redeemValue:C0}",
                    ProcessedAt = DateTime.Now,
                    ProcessedBy = request.StaffId
                };

                _context.LoyaltyTransactions.Add(transaction);
                await _context.SaveChangesAsync();

                return Ok(new 
                { 
                    message = "Đổi điểm thành công",
                    pointsRedeemed = request.PointsToRedeem,
                    redeemValue = redeemValue,
                    newBalance = customer.LoyaltyPoints
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi server", error = ex.Message });
            }
        }

        private async Task<int> CalculateOrderPoints(Order order, LoyaltyConfig config)
        {
            if (order.TotalAmount < config.MinOrderAmountForPoints)
            {
                return 0;
            }

            var basePoints = Math.Floor(order.TotalAmount / config.PointsPerCurrency);
            var multiplier = 1.0m;

            // Kiểm tra Happy Hour
            if (config.HappyHourEnabled)
            {
                var orderTime = order.CreatedAt.TimeOfDay;
                if (orderTime >= config.HappyHourStartTime && orderTime <= config.HappyHourEndTime)
                {
                    multiplier *= config.HappyHourMultiplier;
                }
            }

            // Kiểm tra Weekend Bonus
            if (config.WeekendBonusEnabled && 
                (order.CreatedAt.DayOfWeek == DayOfWeek.Saturday || order.CreatedAt.DayOfWeek == DayOfWeek.Sunday))
            {
                multiplier *= config.WeekendMultiplier;
            }

            // Kiểm tra Birthday Bonus
            if (config.BirthdayBonusEnabled && order.Customer?.DateOfBirth.HasValue == true)
            {
                var birthday = order.Customer.DateOfBirth.Value;
                var daysDiff = Math.Abs((order.CreatedAt - birthday.AddYears(order.CreatedAt.Year - birthday.Year)).Days);
                
                if (daysDiff <= config.BirthdayValidDays)
                {
                    multiplier *= config.BirthdayMultiplier;
                }
            }

            // Áp dụng hệ số cấp độ khách hàng
            if (order.Customer?.CustomerTier != null)
            {
                multiplier *= order.Customer.CustomerTier.PointsMultiplier;
            }

            var finalPoints = (int)Math.Floor(basePoints * multiplier);

            // Áp dụng giới hạn điểm tối đa
            if (config.MaxPointsPerOrder.HasValue && finalPoints > config.MaxPointsPerOrder.Value)
            {
                finalPoints = config.MaxPointsPerOrder.Value;
            }

            return finalPoints;
        }

        private async Task UpdateCustomerTier(Customer customer)
        {
            // Tính tổng chi tiêu
            var totalSpent = await _context.Orders
                .Where(o => o.CustomerId == customer.CustomerId && o.Status == "completed")
                .SumAsync(o => o.TotalAmount);

            // Tìm cấp độ phù hợp (chọn tier cao nhất mà khách hàng đáp ứng điều kiện)
            var appropriateTier = await _context.CustomerTiers
                .Where(t => t.IsActive && totalSpent >= t.MinSpent && customer.LoyaltyPoints >= t.MinPoints)
                .OrderByDescending(t => t.MinSpent)
                .ThenByDescending(t => t.MinPoints)
                .FirstOrDefaultAsync();

            if (appropriateTier != null && customer.TierId != appropriateTier.TierId)
            {
                customer.TierId = appropriateTier.TierId;
                
                // Cập nhật enum HangKhachHang theo tên tier (mapping đúng thứ tự)
                customer.HangKhachHang = appropriateTier.TierName switch
                {
                    "Kim cương" => CustomerRank.Platinum,  // Cao nhất (3)
                    "Vàng" => CustomerRank.VIP,            // Cao (2)
                    "Bạc" => CustomerRank.Premium,         // Trung bình (1)
                    "Đồng" => CustomerRank.Thuong,        // Thấp nhất (0)
                    _ => CustomerRank.Thuong
                };
            }
        }
    }

    public class ProcessOrderPointsRequest
    {
        public int OrderId { get; set; }
        public int? StaffId { get; set; }
    }

    public class RedeemPointsRequest
    {
        public int CustomerId { get; set; }
        public int PointsToRedeem { get; set; }
        public int? OrderId { get; set; }
        public int? StaffId { get; set; }
    }
}